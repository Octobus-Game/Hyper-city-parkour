
using UnityEngine;
using Mirror;

public class PlayerBinding : NetworkBehaviour
{
    [Header("Binding Settings")]
    [SerializeField] private float maxBindingDistance = 10f;
    [SerializeField] private float minMovementSpeed = 1f;
    [SerializeField] private float maxMovementSpeed = 5f;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Header("Components")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Rigidbody rb;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color debugLineColor = Color.red;

    // Синхронизированные переменные
    [SyncVar] private uint boundPlayerNetId;
    [SyncVar] private float currentMovementMultiplier = 1f;

    // Приватные переменные
    private PlayerBinding boundPlayer;
    private float currentSpeed;
    private Vector3 moveDirection;
    private bool useCharacterController;

    void Start()
    {
        // Определяем какой компонент движения использовать
        useCharacterController = characterController != null;

        if (!useCharacterController && rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        // Находим привязанного игрока если он существует
        if (boundPlayerNetId != 0)
        {
            FindBoundPlayer();
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        HandleInput();
        UpdateMovementMultiplier();
        MovePlayer();
    }

    void HandleInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (useCharacterController)
        {
            moveDirection = new Vector3(horizontal, 0, vertical);
            moveDirection = transform.TransformDirection(moveDirection);
        }
        else
        {
            moveDirection = new Vector3(horizontal, 0, vertical);
        }

        moveDirection.Normalize();
    }

    void UpdateMovementMultiplier()
    {
        if (boundPlayer == null) return;

        float distance = Vector3.Distance(transform.position, boundPlayer.transform.position);
        float normalizedDistance = Mathf.Clamp01(distance / maxBindingDistance);

        // Используем кривую для более гладкого изменения скорости
        float newMultiplier = speedCurve.Evaluate(normalizedDistance);
        newMultiplier = Mathf.Clamp(newMultiplier, 0.1f, 1f);

        // Обновляем на сервере
        if (isServer)
        {
            currentMovementMultiplier = newMultiplier;
        }
        else
        {
            CmdUpdateMovementMultiplier(newMultiplier);
        }
    }

    void MovePlayer()
    {
        currentSpeed = Mathf.Lerp(minMovementSpeed, maxMovementSpeed, currentMovementMultiplier);

        if (useCharacterController)
        {
            Vector3 move = moveDirection * currentSpeed * Time.deltaTime;
            move.y = -9.81f * Time.deltaTime; // Гравитация
            characterController.Move(move);
        }
        else if (rb != null)
        {
            Vector3 movement = moveDirection * currentSpeed;
            rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
        }
    }

    [Command]
    void CmdUpdateMovementMultiplier(float multiplier)
    {
        currentMovementMultiplier = multiplier;
    }

    [Server]
    public void BindToPlayer(PlayerBinding targetPlayer)
    {
        if (targetPlayer == null) return;

        boundPlayerNetId = targetPlayer.netId;
        boundPlayer = targetPlayer;

        // Вызываем RPC для всех клиентов
        RpcOnPlayerBound(boundPlayerNetId);
    }

    [ClientRpc]
    void RpcOnPlayerBound(uint netId)
    {
        boundPlayerNetId = netId;
        FindBoundPlayer();
    }

    void FindBoundPlayer()
    {
        if (boundPlayerNetId == 0) return;

        // Ищем объект по NetworkInstanceId
        if (NetworkClient.spawned.TryGetValue(boundPlayerNetId, out NetworkIdentity identity))
        {
            boundPlayer = identity.GetComponent<PlayerBinding>();
        }
    }

    [Server]
    public void UnbindPlayer()
    {
        boundPlayerNetId = 0;
        boundPlayer = null;
        currentMovementMultiplier = 1f;

        RpcOnPlayerUnbound();
    }

    [ClientRpc]
    void RpcOnPlayerUnbound()
    {
        boundPlayerNetId = 0;
        boundPlayer = null;
        currentMovementMultiplier = 1f;
    }

    // Геттеры для внешнего доступа
    public PlayerBinding GetBoundPlayer() => boundPlayer;
    public bool IsBound() => boundPlayer != null;
    public float GetCurrentSpeedMultiplier() => currentMovementMultiplier;
    public float GetDistanceToBoundPlayer()
    {
        if (boundPlayer == null) return 0f;
        return Vector3.Distance(transform.position, boundPlayer.transform.position);
    }

    // Отладочная информация
    void OnDrawGizmos()
    {
        if (!showDebugInfo || boundPlayer == null) return;

        Gizmos.color = debugLineColor;
        Gizmos.DrawLine(transform.position, boundPlayer.transform.position);

        // Показываем максимальное расстояние привязки
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(boundPlayer.transform.position, maxBindingDistance);

        // Показываем текущую скорость
        Gizmos.color = Color.Lerp(Color.red, Color.green, currentMovementMultiplier);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    void OnGUI()
    {
        if (!showDebugInfo || !isLocalPlayer) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Bound Player: {(boundPlayer != null ? boundPlayer.name : "None")}");
        GUILayout.Label($"Speed Multiplier: {currentMovementMultiplier:F2}");
        GUILayout.Label($"Current Speed: {currentSpeed:F2}");

        if (boundPlayer != null)
        {
            float distance = GetDistanceToBoundPlayer();
            GUILayout.Label($"Distance: {distance:F2}");
            GUILayout.Label($"Max Distance: {maxBindingDistance:F2}");
        }
        GUILayout.EndArea();
    }
}

// Менеджер для управления привязками игроков
public class PlayerBindingManager : NetworkBehaviour
{
    [Header("Auto Binding")]
    [SerializeField] private bool autoBindOnStart = true;
    [SerializeField] private float bindingRange = 15f;

    private PlayerBinding[] allPlayers;

    void Start()
    {
        if (isServer && autoBindOnStart)
        {
            Invoke(nameof(AutoBindPlayers), 1f); // Задержка для инициализации
        }
    }

    [Server]
    void AutoBindPlayers()
    {
        allPlayers = FindObjectsOfType<PlayerBinding>();

        if (allPlayers.Length < 2) return;

        // Простая привязка первого игрока ко второму
        if (allPlayers.Length >= 2)
        {
            float distance = Vector3.Distance(allPlayers[0].transform.position, allPlayers[1].transform.position);
            if (distance <= bindingRange)
            {
                allPlayers[0].BindToPlayer(allPlayers[1]);
            }
        }
    }

    [Server]
    public void BindPlayersById(uint player1NetId, uint player2NetId)
    {
        if (NetworkClient.spawned.TryGetValue(player1NetId, out NetworkIdentity identity1) &&
            NetworkClient.spawned.TryGetValue(player2NetId, out NetworkIdentity identity2))
        {
            PlayerBinding player1 = identity1.GetComponent<PlayerBinding>();
            PlayerBinding player2 = identity2.GetComponent<PlayerBinding>();

            if (player1 != null && player2 != null)
            {
                player1.BindToPlayer(player2);
            }
        }
    }

    [Server]
    public void UnbindPlayer(uint playerNetId)
    {
        if (NetworkClient.spawned.TryGetValue(playerNetId, out NetworkIdentity identity))
        {
            PlayerBinding player = identity.GetComponent<PlayerBinding>();
            player?.UnbindPlayer();
        }
    }

    // Команды для клиентов
    [Command(requiresAuthority = false)]
    public void CmdBindToNearestPlayer(NetworkConnectionToClient sender = null)
    {
        if (sender?.identity == null) return;

        PlayerBinding requestingPlayer = sender.identity.GetComponent<PlayerBinding>();
        if (requestingPlayer == null) return;

        allPlayers = FindObjectsOfType<PlayerBinding>();
        PlayerBinding nearestPlayer = null;
        float nearestDistance = float.MaxValue;

        foreach (var player in allPlayers)
        {
            if (player == requestingPlayer) continue;

            float distance = Vector3.Distance(requestingPlayer.transform.position, player.transform.position);
            if (distance < nearestDistance && distance <= bindingRange)
            {
                nearestDistance = distance;
                nearestPlayer = player;
            }
        }

        if (nearestPlayer != null)
        {
            requestingPlayer.BindToPlayer(nearestPlayer);
        }
    }
}