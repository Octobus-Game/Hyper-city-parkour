using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using mirrorplayer; // для доступа к playermirror (блокировка движения)

public class _MapVotingManager : NetworkBehaviour
{
    [System.Serializable]
    public class MapOption
    {
        [Tooltip("Название сцены (должна быть добавлена в Build Settings и в NetworkManager -> Scene)")]
        public string sceneName;

        [Tooltip("Кнопка голосования за эту карту")]
        public Button voteButton;

        [Tooltip("Текст, в котором отображается количество голосов")]
        public TMP_Text voteCountText;
    }

    [Header("Таймер до голосования")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float timeBeforeVoting = 120f; // 2:00 — задаётся в инспекторе

    [Header("Голосование")]
    [SerializeField] private GameObject votingPanelMap;
    [SerializeField] private float votingDuration = 15f;    // N секунд на голосование
    [SerializeField] private TMP_Text votingTimerText;      // опционально: таймер голосования
    [SerializeField] private MapOption[] maps;

    [Header("Переход на сцену")]
    [SerializeField] private float delayBeforeSceneChange = 2f;
    [Tooltip("Панель загрузки, которая активируется в момент перехода на новую сцену")]
    [SerializeField] private GameObject loadPanel;

    [Header("Курсор во время голосования")]
    [Tooltip("Показывать и разблокировать курсор мыши, пока идёт голосование (и подсчёт)")]
    [SerializeField] private bool showCursorDuringVoting = true;

    // Запоминаем состояние курсора, которое было ДО голосования, чтобы корректно вернуть его после
    private bool cursorStateSaved = false;
    private CursorLockMode savedLockState;
    private bool savedCursorVisible;

    // ---- Синхронизируемые данные ----
    [SyncVar(hook = nameof(OnTimerChanged))]
    private float currentTimer;

    [SyncVar(hook = nameof(OnStateChanged))]
    private VotingState state = VotingState.Waiting;

    // Количество голосов по каждой карте (индекс = индекс в массиве maps)
    private readonly SyncList<int> votes = new SyncList<int>();

    // Только на сервере: кто за какую карту проголосовал (connectionId -> mapIndex)
    private readonly Dictionary<int, int> playerVotes = new Dictionary<int, int>();

    public enum VotingState
    {
        Waiting,    // Идёт основной таймер
        Voting,     // Идёт голосование
        Counting    // Подсчёт и переход на сцену
    }

    // =========================================================
    //                        SERVER
    // =========================================================

    // Единственный экземпляр на сцене. Если этот компонент случайно оказался
    // на префабе игрока (или заспавнен больше одного раза), у каждого клиента
    // появится по несколько копий панели/таймера — отсюда "своё время" и
    // накладывающиеся панели голосования. Этот guard сразу подсвечивает проблему в консоли.
    private static _MapVotingManager instance;

    public override void OnStartServer()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("[Voting] Обнаружено больше одного _MapVotingManager на сцене! " +
                "Этот компонент должен быть ОДИН объект в сцене (не на префабе игрока), " +
                "иначе таймер/панель голосования будут дублироваться у каждого клиента.");
        }
        instance = this;

        votes.Clear();
        for (int i = 0; i < maps.Length; i++)
            votes.Add(0);

        playerVotes.Clear();
        currentTimer = timeBeforeVoting;
        state = VotingState.Waiting;
    }

    [ServerCallback]
    private void Update()
    {
        switch (state)
        {
            case VotingState.Waiting:
                currentTimer -= Time.deltaTime;
                if (currentTimer <= 0f)
                {
                    currentTimer = votingDuration;
                    state = VotingState.Voting;
                }
                break;

            case VotingState.Voting:
                currentTimer -= Time.deltaTime;
                if (currentTimer <= 0f)
                {
                    currentTimer = 0f;
                    state = VotingState.Counting;
                    StartCoroutine(FinishVotingRoutine());
                }
                break;
        }
    }

    private System.Collections.IEnumerator FinishVotingRoutine()
    {
        string winner = GetWinnerScene();

        Debug.Log($"[Voting] Победила сцена: {winner}");

        yield return new WaitForSeconds(delayBeforeSceneChange);

        RpcShowLoadPanel();

        NetworkManager.singleton.ServerChangeScene(winner);
    }

    /// <summary>
    /// Показывает панель загрузки на всех клиентах в момент, когда сервер
    /// начинает переход на выигравшую сцену.
    /// </summary>
    [ClientRpc]
    private void RpcShowLoadPanel()
    {
        if (loadPanel != null)
            loadPanel.SetActive(true);
    }

    [Server]
    private string GetWinnerScene()
    {
        if (maps.Length == 0)
            return SceneManagerFallback();

        int maxVotes = votes.Max();

        // Все карты с максимальным числом голосов (для случая ничьей)
        List<int> leaders = new List<int>();
        for (int i = 0; i < votes.Count; i++)
        {
            if (votes[i] == maxVotes)
                leaders.Add(i);
        }

        int winnerIndex = leaders[Random.Range(0, leaders.Count)];
        return maps[winnerIndex].sceneName;
    }

    private string SceneManagerFallback()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// Клиент отправляет свой голос. requiresAuthority = false, чтобы
    /// голосовать мог любой клиент, даже не владеющий этим объектом.
    /// </summary>
    [Command(requiresAuthority = false)]
    private void CmdVote(int mapIndex, NetworkConnectionToClient sender = null)
    {
        if (state != VotingState.Voting) return;
        if (mapIndex < 0 || mapIndex >= maps.Length) return;
        if (sender == null) return;

        int connId = sender.connectionId;

        // Если игрок уже голосовал — снимаем старый голос (переголосование)
        if (playerVotes.TryGetValue(connId, out int previousVote))
        {
            if (previousVote == mapIndex) return; // тот же выбор — ничего не делаем
            votes[previousVote] = Mathf.Max(0, votes[previousVote] - 1);
        }

        playerVotes[connId] = mapIndex;
        votes[mapIndex] = votes[mapIndex] + 1;
    }

    // Если игрок вышел во время голосования — убираем его голос
    [ServerCallback]
    public void RemovePlayerVote(int connectionId)
    {
        if (playerVotes.TryGetValue(connectionId, out int mapIndex))
        {
            votes[mapIndex] = Mathf.Max(0, votes[mapIndex] - 1);
            playerVotes.Remove(connectionId);
        }
    }

    // =========================================================
    //                        CLIENT
    // =========================================================

    public override void OnStartClient()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("[Voting] Обнаружено больше одного _MapVotingManager на клиенте! " +
                "Проверь, что этот компонент размещён как ОДИН объект сцены (Scene Object), " +
                "а не заспавнен на каждом игроке — иначе UI голосования будет накладываться друг на друга.");
        }
        instance = this;

        // На случай, если панель загрузки лежит на persistent-канвасе (DontDestroyOnLoad)
        // и пережила смену сцены — на старте нового раунда она должна быть скрыта.
        if (loadPanel != null)
            loadPanel.SetActive(false);

        // Подписка на изменения списка голосов
        votes.Callback += OnVotesChanged;

        // Привязка кнопок
        for (int i = 0; i < maps.Length; i++)
        {
            int index = i; // замыкание
            if (maps[i].voteButton != null)
            {
                maps[i].voteButton.onClick.RemoveAllListeners();
                maps[i].voteButton.onClick.AddListener(() => CmdVote(index));
            }
        }

        // Первичное обновление UI (для клиентов, подключившихся позже)
        UpdateAllVoteTexts();
        UpdateTimerText(currentTimer);
        ApplyState(state);
    }

    public override void OnStopClient()
    {
        votes.Callback -= OnVotesChanged;

        // Возвращаем курсор в исходное состояние, если объект уничтожается прямо во время голосования
        SetCursorActive(false);

        if (instance == this)
            instance = null;
    }

    private void OnTimerChanged(float oldValue, float newValue)
    {
        UpdateTimerText(newValue);
    }

    private void OnStateChanged(VotingState oldState, VotingState newState)
    {
        ApplyState(newState);
    }

    private void ApplyState(VotingState newState)
    {
        bool voting = newState == VotingState.Voting;

        if (votingPanelMap != null)
            votingPanelMap.SetActive(newState != VotingState.Waiting);

        // Кнопки доступны только во время голосования
        foreach (var map in maps)
        {
            if (map.voteButton != null)
                map.voteButton.interactable = voting;
        }

        // Блокируем передвижение локального игрока на время голосования и подсчёта
        // (Counting включён намеренно — сразу после голосования идёт переход на сцену,
        // так что смысла снова отпускать игрока на пару секунд нет).
        SetLocalPlayerMovementLocked(newState != VotingState.Waiting);

        // Показываем/скрываем курсор синхронно с блокировкой движения
        SetCursorActive(newState != VotingState.Waiting);

        UpdateTimerText(currentTimer);
    }

    /// <summary>
    /// Блокирует/разблокирует движение ЛОКАЛЬНОГО игрока. Двигает только тот клиент,
    /// которому принадлежит объект (см. проверку isLocalPlayer в playermirror.Update()),
    /// поэтому синхронизировать это по сети не нужно — SyncVar-хук state уже вызывает
    /// ApplyState на каждом клиенте самостоятельно.
    /// </summary>
    private void SetLocalPlayerMovementLocked(bool locked)
    {
        NetworkIdentity localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) return;

        if (localPlayer.TryGetComponent(out playermirror player))
            player.SetCanMove(!locked);
    }

    /// <summary>
    /// Включает/выключает видимый разблокированный курсор на локальном клиенте.
    /// При активации запоминает предыдущее состояние (обычно Locked+Hidden в шутере/FPS-контроле),
    /// при деактивации — восстанавливает его, а не жёстко ставит Locked.
    /// </summary>
    private void SetCursorActive(bool active)
    {
        if (!showCursorDuringVoting) return;

        if (active)
        {
            // Сохраняем состояние курсора только один раз за "включение",
            // чтобы повторные вызовы ApplyState (например, при переподключении) не затёрли сохранённое состояние.
            if (!cursorStateSaved)
            {
                savedLockState = Cursor.lockState;
                savedCursorVisible = Cursor.visible;
                cursorStateSaved = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (cursorStateSaved)
            {
                Cursor.lockState = savedLockState;
                Cursor.visible = savedCursorVisible;
                cursorStateSaved = false;
            }
        }
    }

    private void OnVotesChanged(SyncList<int>.Operation op, int index, int oldItem, int newItem)
    {
        UpdateAllVoteTexts();
    }

    private void UpdateAllVoteTexts()
    {
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].voteCountText == null) continue;

            int count = i < votes.Count ? votes[i] : 0;
            maps[i].voteCountText.text = count.ToString();
        }
    }

    private void UpdateTimerText(float time)
    {
        time = Mathf.Max(0f, time);
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        string formatted = $"{minutes:00}:{seconds:00}";

        switch (state)
        {
            case VotingState.Waiting:
                if (timerText != null) timerText.text = formatted;
                break;

            case VotingState.Voting:
                if (votingTimerText != null) votingTimerText.text = formatted;
                else if (timerText != null) timerText.text = formatted;
                break;

            case VotingState.Counting:
                if (votingTimerText != null) votingTimerText.text = "00:00";
                if (timerText != null) timerText.text = "00:00";
                break;
        }
    }
}