using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class VoiceChatManager : NetworkBehaviour
{
    [Header("Voice Settings")]
    public bool enableVoiceChat = true;
    public float voiceVolume = 1f;
    public bool pushToTalk = true;
    public KeyCode pushToTalkKey = KeyCode.V;
    public bool voiceActivation = false;
    public float voiceThreshold = 0.01f;

    [Header("Audio Settings")]
    public int sampleRate = 16000;
    public int recordingLength = 1; // seconds

    [Header("UI Elements")]
    public Button muteButton;
    public Button deafenButton;
    public Slider volumeSlider;
    public Text statusText;
    public GameObject speakingIndicator;
    public Transform playersListParent;
    public GameObject playerVoiceUIPrefab;

    [Header("3D Audio")]
    public bool use3DAudio = true;
    public float maxDistance = 50f;
    public float minDistance = 1f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    [Header("Audio Compression")]
    [Range(0.1f, 1f)]
    public float audioQuality = 0.5f; // Custom quality setting for our compression

    // Voice state
    [SyncVar(hook = nameof(OnMuteStateChanged))]
    private bool isMuted = false;

    [SyncVar(hook = nameof(OnDeafenStateChanged))]
    private bool isDeafened = false;

    [SyncVar(hook = nameof(OnSpeakingStateChanged))]
    private bool isSpeaking = false;

    // Audio components
    private AudioSource audioSource;
    private AudioClip microphoneClip;
    private string selectedMicrophone;
    private bool isRecording = false;
    private float[] audioBuffer;
    private int lastSamplePosition = 0;

    // Network audio
    private Dictionary<uint, VoicePlayer> voicePlayers = new Dictionary<uint, VoicePlayer>();
    private Queue<VoiceData> voiceQueue = new Queue<VoiceData>();
    private const int MAX_VOICE_PACKET_SIZE = 1024;

    // FIX: a static handle to the LOCAL player's own VoiceChatManager instance.
    // We need this because RpcReceiveVoiceData runs on the SPEAKER's replica
    // object on every other client, so "this.isDeafened" inside that Rpc is
    // the speaker's deafened flag, not the listener's. To know whether WE
    // (the local listener) are deafened, we have to go through this reference.
    private static VoiceChatManager localPlayerInstance;

    void Start()
    {
        SetupAudioComponents();
        SetupUI();

        // FIX: SetupMicrophone() and RegisterVoiceHandlers() used to be called
        // here, gated by "if (isLocalPlayer)". isLocalPlayer is NOT guaranteed
        // to be true yet inside Start() - it depends on spawn/ownership timing.
        // If it was still false at this point, SetupMicrophone() never ran,
        // selectedMicrophone stayed null forever, and StartRecording() would
        // silently bail out every single time - meaning nothing was ever
        // recorded or sent, with no errors logged anywhere. That's the most
        // likely reason for "I talk and nothing happens at all".
        //
        // Moved both calls to OnStartLocalPlayer() below, which Mirror
        // guarantees fires once isLocalPlayer is actually true.
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        localPlayerInstance = this;
        SetupMicrophone();
        RegisterVoiceHandlers();
    }

    void SetupAudioComponents()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (use3DAudio)
        {
            audioSource.spatialBlend = 1f; // 3D
            audioSource.maxDistance = maxDistance;
            audioSource.minDistance = minDistance;
            audioSource.rolloffMode = rolloffMode;
        }
        else
        {
            audioSource.spatialBlend = 0f; // 2D
        }

        audioSource.volume = voiceVolume;
        audioSource.playOnAwake = false;
    }

    void SetupUI()
    {
        if (muteButton != null)
        {
            muteButton.onClick.AddListener(ToggleMute);
        }

        if (deafenButton != null)
        {
            deafenButton.onClick.AddListener(ToggleDeafen);
        }

        if (volumeSlider != null)
        {
            volumeSlider.value = voiceVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        UpdateUI();
    }

    void SetupMicrophone()
    {
        if (!isLocalPlayer) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphones found!");
            enableVoiceChat = false;
            return;
        }

        selectedMicrophone = Microphone.devices[0];
        Debug.Log($"Selected microphone: {selectedMicrophone}");

        audioBuffer = new float[sampleRate * recordingLength];
    }

    void RegisterVoiceHandlers()
    {
        NetworkClient.RegisterHandler<VoiceDataMessage>(OnReceiveVoiceData);
    }

    void Update()
    {
        if (!isLocalPlayer || !enableVoiceChat) return;

        HandleVoiceInput();
        ProcessVoiceRecording();
        UpdateUI();
    }

    void HandleVoiceInput()
    {
        bool shouldRecord = false;

        if (pushToTalk)
        {
            shouldRecord = Input.GetKey(pushToTalkKey) && !isMuted;
        }
        else if (voiceActivation)
        {
            shouldRecord = CheckVoiceActivation() && !isMuted;
        }

        if (shouldRecord && !isRecording)
        {
            StartRecording();
        }
        else if (!shouldRecord && isRecording)
        {
            StopRecording();
        }
    }

    bool CheckVoiceActivation()
    {
        if (microphoneClip == null) return false;

        float[] samples = new float[128];
        int micPosition = Microphone.GetPosition(selectedMicrophone);

        if (micPosition > 0)
        {
            microphoneClip.GetData(samples, micPosition - 128);
            float sum = samples.Sum(x => Mathf.Abs(x));
            float average = sum / samples.Length;

            return average > voiceThreshold;
        }

        return false;
    }

    void StartRecording()
    {
        if (string.IsNullOrEmpty(selectedMicrophone)) return;

        microphoneClip = Microphone.Start(selectedMicrophone, true, recordingLength, sampleRate);
        isRecording = true;
        lastSamplePosition = 0;

        CmdSetSpeakingState(true);
    }

    void StopRecording()
    {
        if (microphoneClip != null)
        {
            Microphone.End(selectedMicrophone);
            isRecording = false;
        }

        CmdSetSpeakingState(false);
    }

    void ProcessVoiceRecording()
    {
        if (!isRecording || microphoneClip == null) return;

        int currentPosition = Microphone.GetPosition(selectedMicrophone);
        if (currentPosition == lastSamplePosition) return;

        int clipSamples = microphoneClip.samples;
        int samplesAvailable = currentPosition - lastSamplePosition;
        if (samplesAvailable < 0)
        {
            samplesAvailable += clipSamples;
        }

        if (samplesAvailable <= 0) return;

        float[] samples = new float[samplesAvailable];

        // FIX: the microphone clip loops every `recordingLength` seconds, so a
        // read can wrap past the end of the buffer. The original code always
        // did a single GetData() call starting at lastSamplePosition, which
        // could try to read past the end of the clip once you'd held
        // push-to-talk longer than recordingLength seconds. Split the read in
        // two when that happens.
        if (lastSamplePosition + samplesAvailable <= clipSamples)
        {
            microphoneClip.GetData(samples, lastSamplePosition);
        }
        else
        {
            int samplesToEnd = clipSamples - lastSamplePosition;
            float[] endChunk = new float[samplesToEnd];
            microphoneClip.GetData(endChunk, lastSamplePosition);

            int remaining = samplesAvailable - samplesToEnd;
            float[] startChunk = new float[remaining];
            microphoneClip.GetData(startChunk, 0);

            endChunk.CopyTo(samples, 0);
            startChunk.CopyTo(samples, samplesToEnd);
        }

        byte[] compressedData = CompressAudioData(samples, out int effectiveSampleRate);
        if (compressedData.Length > 0)
        {
            CmdSendVoiceData(compressedData, effectiveSampleRate);
        }

        lastSamplePosition = currentPosition;
    }

    byte[] CompressAudioData(float[] samples, out int effectiveSampleRate)
    {
        int decimationFactor = Mathf.Max(1, Mathf.RoundToInt(1f / audioQuality));

        // FIX: dropping samples to "compress" means the data we send
        // represents audio at a LOWER effective sample rate. The original
        // code never told the receiving side this, so clips got rebuilt at
        // the full sampleRate and played back sped-up / pitch-shifted.
        effectiveSampleRate = Mathf.Max(1, sampleRate / decimationFactor);

        int compressedLength = samples.Length / decimationFactor;
        byte[] bytes = new byte[compressedLength * 2];

        for (int i = 0; i < compressedLength; i++)
        {
            int sourceIndex = i * decimationFactor;
            if (sourceIndex < samples.Length)
            {
                short sample = (short)(samples[sourceIndex] * 32767);
                bytes[i * 2] = (byte)(sample & 0xFF);
                bytes[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }
        }

        return bytes;
    }

    float[] DecompressAudioData(byte[] data)
    {
        float[] samples = new float[data.Length / 2];

        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
            samples[i] = sample / 32767f;
        }

        return samples;
    }

    [Command]
    void CmdSendVoiceData(byte[] voiceData, int dataSampleRate)
    {
        RpcReceiveVoiceData(netId, voiceData, dataSampleRate);
    }

    [ClientRpc(includeOwner = false)]
    void RpcReceiveVoiceData(uint senderId, byte[] voiceData, int dataSampleRate)
    {
        // FIX: this Rpc runs on the SPEAKER's replica object on every other
        // client, so "isDeafened" here would be the speaker's own deafened
        // flag, not the local listening player's. Check the local player's
        // instance instead.
        if (localPlayerInstance != null && localPlayerInstance.isDeafened) return;

        if (!voicePlayers.TryGetValue(senderId, out VoicePlayer voicePlayer))
        {
            if (NetworkClient.spawned.TryGetValue(senderId, out NetworkIdentity networkIdentity))
            {
                GameObject senderObject = networkIdentity.gameObject;
                voicePlayer = CreateVoicePlayer(senderObject);
                voicePlayers[senderId] = voicePlayer;
            }
            else
            {
                Debug.LogWarning($"Cannot find NetworkIdentity for sender {senderId}");
                return;
            }
        }

        voicePlayer.PlayVoiceData(voiceData, dataSampleRate);
    }

    VoicePlayer CreateVoicePlayer(GameObject playerObject)
    {
        VoicePlayer voicePlayer = playerObject.GetComponent<VoicePlayer>();
        if (voicePlayer == null)
        {
            voicePlayer = playerObject.AddComponent<VoicePlayer>();
        }

        voicePlayer.Initialize(use3DAudio, maxDistance, minDistance, rolloffMode);
        return voicePlayer;
    }

    void OnReceiveVoiceData(VoiceDataMessage message)
    {
        // NOTE: this message-based path is currently not wired into playback
        // anywhere - actual voice delivery goes through CmdSendVoiceData /
        // RpcReceiveVoiceData above. Left in place in case you want to use it
        // later, but it's not part of the live audio pipeline right now.
        voiceQueue.Enqueue(new VoiceData
        {
            senderId = message.senderId,
            data = message.data
        });
    }

    [Command]
    void CmdSetSpeakingState(bool speaking)
    {
        isSpeaking = speaking;
    }

    public void ToggleMute()
    {
        if (isLocalPlayer)
        {
            CmdSetMuteState(!isMuted);
        }
    }

    public void ToggleDeafen()
    {
        if (isLocalPlayer)
        {
            CmdSetDeafenState(!isDeafened);
        }
    }

    [Command]
    void CmdSetMuteState(bool muted)
    {
        isMuted = muted;
        if (muted && isRecording)
        {
            StopRecording();
        }
    }

    [Command]
    void CmdSetDeafenState(bool deafened)
    {
        isDeafened = deafened;
        if (deafened)
        {
            foreach (var voicePlayer in voicePlayers.Values)
            {
                voicePlayer.StopVoice();
            }
        }
    }

    public void SetVolume(float volume)
    {
        voiceVolume = volume;
        audioSource.volume = volume;
    }

    void OnMuteStateChanged(bool oldValue, bool newValue)
    {
        isMuted = newValue;
        UpdateUI();
    }

    void OnDeafenStateChanged(bool oldValue, bool newValue)
    {
        isDeafened = newValue;
        UpdateUI();
    }

    void OnSpeakingStateChanged(bool oldValue, bool newValue)
    {
        isSpeaking = newValue;
        if (speakingIndicator != null)
        {
            speakingIndicator.SetActive(newValue);
        }
    }

    void UpdateUI()
    {
        if (statusText != null)
        {
            string status = "";
            if (isMuted) status += "MUTED ";
            if (isDeafened) status += "DEAFENED ";
            if (isSpeaking) status += "SPEAKING ";

            statusText.text = status.Trim();
        }

        if (muteButton != null)
        {
            ColorBlock colors = muteButton.colors;
            colors.normalColor = isMuted ? Color.red : Color.white;
            muteButton.colors = colors;
        }

        if (deafenButton != null)
        {
            ColorBlock colors = deafenButton.colors;
            colors.normalColor = isDeafened ? Color.red : Color.white;
            deafenButton.colors = colors;
        }
    }

    void OnDestroy()
    {
        if (isRecording)
        {
            StopRecording();
        }

        if (localPlayerInstance == this)
        {
            localPlayerInstance = null;
        }
    }
}

public class VoicePlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private Queue<AudioClip> audioQueue = new Queue<AudioClip>();
    private bool isPlaying = false;

    public void Initialize(bool use3D, float maxDist, float minDist, AudioRolloffMode rolloff)
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.spatialBlend = use3D ? 1f : 0f;
        audioSource.maxDistance = maxDist;
        audioSource.minDistance = minDist;
        audioSource.rolloffMode = rolloff;
        audioSource.playOnAwake = false;
    }

    public void PlayVoiceData(byte[] data, int sampleRate)
    {
        float[] samples = DecompressAudioData(data);
        AudioClip clip = AudioClip.Create("VoiceClip", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);

        audioQueue.Enqueue(clip);

        if (!isPlaying)
        {
            PlayNext();
        }
    }

    void PlayNext()
    {
        if (audioQueue.Count > 0)
        {
            AudioClip clip = audioQueue.Dequeue();
            audioSource.clip = clip;
            audioSource.Play();
            isPlaying = true;

            Invoke(nameof(OnClipFinished), clip.length);
        }
        else
        {
            isPlaying = false;
        }
    }

    void OnClipFinished()
    {
        PlayNext();
    }

    public void StopVoice()
    {
        audioSource.Stop();
        audioQueue.Clear();
        isPlaying = false;
        CancelInvoke(nameof(OnClipFinished));
    }

    float[] DecompressAudioData(byte[] data)
    {
        float[] samples = new float[data.Length / 2];

        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
            samples[i] = sample / 32767f;
        }

        return samples;
    }
}

public struct VoiceDataMessage : NetworkMessage
{
    public uint senderId;
    public byte[] data;
}

public struct VoiceData
{
    public uint senderId;
    public byte[] data;
}