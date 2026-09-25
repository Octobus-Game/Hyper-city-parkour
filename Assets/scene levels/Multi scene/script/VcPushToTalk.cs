using Mirror;
using MetaVoiceChat;
using UnityEngine;

namespace MetaVoiceChat.Extras
{

    [RequireComponent(typeof(NetworkIdentity))]
    public class VcPushToTalk : NetworkBehaviour
    {
        [Header("Voice Chat")]
        [Tooltip("Reference to this player's MetaVc component.")]
        [SerializeField] private MetaVc metaVc;

        [Header("Input")]
        [Tooltip("Key that must be held down to talk.")]
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;

        [Header("Speaking Icon")]
        [Tooltip("Shown above the player's head ONLY on this client, when this is the local player talking. " +
            "If you don't want a separate local-only icon, leave this empty and just use Network Speaking Icon for everyone.")]
        [SerializeField] private GameObject localSpeakingIcon;

        [Tooltip("Shown above the player's head on ALL clients (including the owner), driven by the synced network state.")]
        [SerializeField] private GameObject networkSpeakingIcon;


        [SyncVar(hook = nameof(OnIsSpeakingChanged))]
        private bool isSpeakingNetworked;

        private bool wasKeyHeldLastFrame;

        private void Reset()
        {
            metaVc = GetComponent<MetaVc>();
        }

        private void Start()
        {
            if (metaVc == null)
            {
                metaVc = GetComponent<MetaVc>();
            }

            // Make sure input starts muted and isSpeaking starts false,
            // regardless of whatever MetaVc's own VAD logic thinks is happening.
            if (metaVc != null)
            {
                metaVc.isInputMuted.Value = true;
                metaVc.isSpeaking.Value = false;
            }

            SetLocalIconActive(false);
            SetNetworkIconActive(false);
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            // Explicitly use UnityEngine.Input to resolve the naming conflict
            bool isKeyHeld = UnityEngine.Input.GetKey(pushToTalkKey);

            if (isKeyHeld == wasKeyHeldLastFrame)
            {
                return;
            }

            wasKeyHeldLastFrame = isKeyHeld;
            SetTalking(isKeyHeld);
        }

        private void SetTalking(bool talking)
        {
            if (metaVc != null)
            {
                metaVc.isInputMuted.Value = !talking;

                // Drive isSpeaking directly from the push-to-talk key state.
                // MetaVc's own SendFrame logic sets isSpeaking based on whether
                // samples are non-null (i.e. any mic activity), which ignores
                // isInputMuted and causes isSpeaking to stay true even when
                // muted. Overriding it here ensures isSpeaking is only ever
                // true while the key is actually held down.
                metaVc.isSpeaking.Value = talking;
            }

            // Local-only icon reacts instantly to input, for the owner's own screen.
            SetLocalIconActive(talking);

            // Tell the server so it can replicate the state to everyone else.
            CmdSetSpeaking(talking);
        }

        [Command]
        private void CmdSetSpeaking(bool talking)
        {
            isSpeakingNetworked = talking;
        }

        // SyncVar hook: fires on every client (including the owner) whenever the server-replicated value changes.
        private void OnIsSpeakingChanged(bool oldValue, bool newValue)
        {
            SetNetworkIconActive(newValue);
        }

        private void SetLocalIconActive(bool active)
        {
            if (localSpeakingIcon != null)
            {
                localSpeakingIcon.SetActive(active);
            }
        }

        private void SetNetworkIconActive(bool active)
        {
            if (networkSpeakingIcon != null)
            {
                networkSpeakingIcon.SetActive(active);
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            // Make sure the mic doesn't stay open if this object is destroyed while talking.
            if (isLocalPlayer && metaVc != null)
            {
                metaVc.isInputMuted.Value = true;
                metaVc.isSpeaking.Value = false;
            }
        }
    }
}