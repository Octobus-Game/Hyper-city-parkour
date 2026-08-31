using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public enum GameState
{
    Waiting,
    Preparing,
    Playing,
    GameOver
}

public enum PlayerRole
{
    None,
    Hider,
    Hunter
}

// Главный менеджер игры
public class PropHuntGameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float prepareTime = 60f;
    [SerializeField] private float gameTime = 300f; // 5 минут игры
    [SerializeField] private int minPlayersToStart = 2;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] hiderSpawnPoints;
    [SerializeField] private Transform[] hunterSpawnPoints;

    [Header("Available Props")]
    [SerializeField] private GameObject[] availableProps;

    [SyncVar(hook = nameof(OnGameStateChanged))]
    public GameState currentGameState = GameState.Waiting;

    [SyncVar(hook = nameof(OnTimerChanged))]
    public float currentTimer;

    [SyncVar]
    public int huntersAlive;

    [SyncVar]
    public int hidersAlive;

    public static PropHuntGameManager Instance;

    private List<PropHuntPlayer> allPlayers = new List<PropHuntPlayer>();
    private Coroutine gameTimerCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        currentGameState = GameState.Waiting;
        StartCoroutine(CheckForGameStart());
    }

    private IEnumerator CheckForGameStart()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (currentGameState == GameState.Waiting &&
                NetworkServer.connections.Count >= minPlayersToStart)
            {
                StartGame();
            }
        }
    }

    [Server]
    public void StartGame()
    {
        if (currentGameState != GameState.Waiting) return;

        currentGameState = GameState.Preparing;
        AssignRoles();
        SpawnPlayers();

        gameTimerCoroutine = StartCoroutine(GameTimer());
        RpcGameStarted();
    }

    [Server]
    private void AssignRoles()
    {
        allPlayers = FindObjectsOfType<PropHuntPlayer>().ToList();

        if (allPlayers.Count == 0) return;

        // Выбираем случайного охотника
        int hunterIndex = Random.Range(0, allPlayers.Count);

        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (i == hunterIndex)
            {
                allPlayers[i].SetRole(PlayerRole.Hunter);
                huntersAlive++;
            }
            else
            {
                allPlayers[i].SetRole(PlayerRole.Hider);
                hidersAlive++;
            }
        }
    }

    [Server]
    private void SpawnPlayers()
    {
        foreach (var player in allPlayers)
        {
            if (player.playerRole == PlayerRole.Hunter && hunterSpawnPoints.Length > 0)
            {
                int spawnIndex = Random.Range(0, hunterSpawnPoints.Length);
                player.transform.position = hunterSpawnPoints[spawnIndex].position;
                player.transform.rotation = hunterSpawnPoints[spawnIndex].rotation;
            }
            else if (player.playerRole == PlayerRole.Hider && hiderSpawnPoints.Length > 0)
            {
                int spawnIndex = Random.Range(0, hiderSpawnPoints.Length);
                player.transform.position = hiderSpawnPoints[spawnIndex].position;
                player.transform.rotation = hiderSpawnPoints[spawnIndex].rotation;
            }
        }
    }

    private IEnumerator GameTimer()
    {
        // Фаза подготовки (прятки)
        currentTimer = prepareTime;

        while (currentTimer > 0 && currentGameState == GameState.Preparing)
        {
            currentTimer -= 1f;
            yield return new WaitForSeconds(1f);
        }

        if (currentGameState == GameState.Preparing)
        {
            currentGameState = GameState.Playing;
            RpcStartHunting();
        }

        // Основная игра
        currentTimer = gameTime;

        while (currentTimer > 0 && currentGameState == GameState.Playing)
        {
            currentTimer -= 1f;
            yield return new WaitForSeconds(1f);

            CheckWinConditions();
        }

        if (currentGameState == GameState.Playing)
        {
            EndGame(PlayerRole.Hider); // Прячущиеся выиграли по времени
        }
    }

    [Server]
    private void CheckWinConditions()
    {
        if (hidersAlive <= 0)
        {
            EndGame(PlayerRole.Hunter);
        }
        else if (huntersAlive <= 0)
        {
            EndGame(PlayerRole.Hider);
        }
    }

    [Server]
    private void EndGame(PlayerRole winner)
    {
        currentGameState = GameState.GameOver;
        RpcGameEnded(winner);

        if (gameTimerCoroutine != null)
            StopCoroutine(gameTimerCoroutine);

        StartCoroutine(RestartGameAfterDelay());
    }

    private IEnumerator RestartGameAfterDelay()
    {
        yield return new WaitForSeconds(10f);

        // Сброс состояния
        huntersAlive = 0;
        hidersAlive = 0;
        currentGameState = GameState.Waiting;

        // Сброс всех игроков
        foreach (var player in allPlayers)
        {
            player.ResetPlayer();
        }

        RpcResetGame();
    }

    [Server]
    public void OnPlayerDied(PropHuntPlayer player)
    {
        if (player.playerRole == PlayerRole.Hunter)
            huntersAlive--;
        else if (player.playerRole == PlayerRole.Hider)
            hidersAlive--;

        CheckWinConditions();
    }

    public GameObject[] GetAvailableProps()
    {
        return availableProps;
    }

    // Hooks и RPC
    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        Debug.Log($"Game State Changed: {oldState} -> {newState}");
    }

    private void OnTimerChanged(float oldTimer, float newTimer)
    {
        // UI обновление таймера
    }

    [ClientRpc]
    private void RpcGameStarted()
    {
        Debug.Log("Game Started!");
    }

    [ClientRpc]
    private void RpcStartHunting()
    {
        Debug.Log("Hunting Phase Started!");
    }

    [ClientRpc]
    private void RpcGameEnded(PlayerRole winner)
    {
        Debug.Log($"Game Ended! Winner: {winner}");
    }

    [ClientRpc]
    private void RpcResetGame()
    {
        Debug.Log("Game Reset!");
    }
}

// Компонент игрока
public class PropHuntPlayer : NetworkBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private LayerMask groundLayer = 1;

    [SyncVar(hook = nameof(OnRoleChanged))]
    public PlayerRole playerRole = PlayerRole.None;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health = 100f;

    [SyncVar(hook = nameof(OnPropChanged))]
    public int currentPropIndex = -1;

    private Rigidbody rb;
    private Camera playerCamera;
    private GameObject originalModel;
    private GameObject currentPropModel;
    private bool isGrounded;

    // Охотник
    [Header("Hunter Settings")]
    [SerializeField] private float shootRange = 100f;
    [SerializeField] private float shootDamage = 25f;
    private float lastShootTime;
    private float shootCooldown = 0.5f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCamera = GetComponentInChildren<Camera>();
        originalModel = transform.GetChild(0).gameObject; // Предполагаем что модель первый child

        if (!isLocalPlayer)
        {
            playerCamera.enabled = false;
            playerCamera.GetComponent<AudioListener>().enabled = false;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleInput();
        CheckGrounded();
    }

    private void HandleInput()
    {
        if (PropHuntGameManager.Instance.currentGameState != GameState.Playing &&
            PropHuntGameManager.Instance.currentGameState != GameState.Preparing) return;

        // Движение
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = transform.right * horizontal + transform.forward * vertical;
        movement = movement.normalized * moveSpeed * Time.deltaTime;

        rb.MovePosition(transform.position + movement);

        // Прыжок
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Поворот камеры
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        transform.Rotate(Vector3.up * mouseX);
        playerCamera.transform.Rotate(Vector3.left * mouseY);

        // Специфичные для роли действия
        if (playerRole == PlayerRole.Hunter)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                CmdShoot();
            }
        }
        else if (playerRole == PlayerRole.Hider)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                CmdChangeProp();
            }
        }
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer);
    }

    [Command]
    private void CmdShoot()
    {
        if (Time.time - lastShootTime < shootCooldown) return;
        lastShootTime = Time.time;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            PropHuntPlayer hitPlayer = hit.collider.GetComponent<PropHuntPlayer>();
            if (hitPlayer != null && hitPlayer.playerRole == PlayerRole.Hider)
            {
                hitPlayer.TakeDamage(shootDamage);
            }
        }

        RpcShowShootEffect();
    }

    [Command]
    private void CmdChangeProp()
    {
        if (PropHuntGameManager.Instance.currentGameState != GameState.Preparing) return;

        GameObject[] availableProps = PropHuntGameManager.Instance.GetAvailableProps();
        if (availableProps.Length == 0) return;

        currentPropIndex = Random.Range(0, availableProps.Length);
        UpdatePropModel();
    }

    [Server]
    public void TakeDamage(float damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Die();
        }
    }

    [Server]
    private void Die()
    {
        PropHuntGameManager.Instance.OnPlayerDied(this);
        RpcPlayerDied();
    }

    [Server]
    public void SetRole(PlayerRole role)
    {
        playerRole = role;
        health = 100f;

        if (role == PlayerRole.Hider)
        {
            // Прячущийся может превращаться в объекты
            currentPropIndex = -1;
        }
        else if (role == PlayerRole.Hunter)
        {
            // Охотник остается в оригинальном виде
            currentPropIndex = -1;
            UpdatePropModel();
        }
    }

    [Server]
    public void ResetPlayer()
    {
        playerRole = PlayerRole.None;
        health = 100f;
        currentPropIndex = -1;
        UpdatePropModel();
    }

    private void UpdatePropModel()
    {
        // Удаляем старую модель пропа
        if (currentPropModel != null)
        {
            if (isServer)
                NetworkServer.Destroy(currentPropModel);
            else
                Destroy(currentPropModel);
        }

        if (currentPropIndex >= 0 && PropHuntGameManager.Instance != null)
        {
            GameObject[] availableProps = PropHuntGameManager.Instance.GetAvailableProps();
            if (currentPropIndex < availableProps.Length)
            {
                // Создаем новую модель пропа
                currentPropModel = Instantiate(availableProps[currentPropIndex], transform);
                currentPropModel.transform.localPosition = Vector3.zero;

                // Скрываем оригинальную модель
                originalModel.SetActive(false);
            }
        }
        else
        {
            // Показываем оригинальную модель
            originalModel.SetActive(true);
        }
    }

    // Hooks
    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        Debug.Log($"Player role changed to: {newRole}");

        if (isLocalPlayer)
        {
            if (newRole == PlayerRole.Hunter)
            {
                // UI для охотника
            }
            else if (newRole == PlayerRole.Hider)
            {
                // UI для прячущегося
            }
        }
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        // Обновление UI здоровья
        if (isLocalPlayer)
        {
            Debug.Log($"Health: {newHealth}");
        }
    }

    private void OnPropChanged(int oldPropIndex, int newPropIndex)
    {
        UpdatePropModel();
    }

    // RPC
    [ClientRpc]
    private void RpcShowShootEffect()
    {
        // Эффекты выстрела (звук, вспышка и т.д.)
        Debug.Log("BANG!");
    }

    [ClientRpc]
    private void RpcPlayerDied()
    {
        // Эффекты смерти
        Debug.Log("Player died!");

        if (isLocalPlayer)
        {
            // Переключить в режим наблюдателя
            playerCamera.enabled = false;
        }
    }
}

