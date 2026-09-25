using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class Healt_mirror_Player : NetworkBehaviour
{
    [Header("Health")]
    [SyncVar(hook = nameof(OnHealthChanged))]
    [SerializeField] private float healthValue = 100f;

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Image healthBarImage;

    [Tooltip("Текстовое отображение HP, например \"73 / 100\".")]
    [SerializeField] private TMP_Text healthText;

    [Tooltip("Скорость плавной анимации полосы/текста HP (единиц HP в секунду).")]
    [SerializeField] private float healthLerpSpeed = 120f;

    [Header("Health Bar Intermediate (с задержкой)")]
    [Tooltip("Вторая полоса HP («догоняющая»). Работает как Health Bar Image, " +
             "но начинает подтягиваться к новому значению HP только через " +
             "intermediateDelay секунд после изменения healthValue.")]
    [SerializeField] private Image healthBarImageIntermediate;

    [Tooltip("Задержка в секундах перед тем, как intermediate-полоса начнёт " +
             "двигаться к новому значению HP.")]
    [SerializeField] private float intermediateDelay = 1f;

    [Tooltip("Скорость плавной анимации intermediate-полосы (единиц HP в секунду). " +
             "Если 0 — используется healthLerpSpeed.")]
    [SerializeField] private float intermediateLerpSpeed = 0f;

    [Header("Damage Feedback")]
    [Tooltip("Particle system, проигрываемый на игроке при получении урона (кровь/искры).")]
    [SerializeField] private ParticleSystem damageParticles;

    [Tooltip("Панель-виньетка урона. Показывается только локальному игроку.")]
    [SerializeField] private CanvasGroup damagePanel;

    [Tooltip("Сколько секунд держать damage panel видимой после урона.")]
    [SerializeField] private float damagePanelDuration = 0.6f;

    [Tooltip("Скорость затухания damage panel (альфа в секунду).")]
    [SerializeField] private float damagePanelFadeSpeed = 3f;

    [Header("Death UI")]
    [Tooltip("Panel Death. Должен быть в Canvas локального игрока.")]
    [SerializeField] private GameObject deathPanel;

    [Tooltip("Текст \"Вас убил [Ник игрока]\". Заполняется только у локального " +
             "(погибшего) игрока в момент смерти, ник берётся из NetworkPlayerName убийцы.")]
    [SerializeField] private TMP_Text killText;

    [Tooltip("Шаблон текста об убийстве. {0} заменяется на ник убийцы.")]
    [SerializeField] private string killTextFormat = "Вас убил {0}";

    [Tooltip("Кнопка 'Выйти из сервера'.")]
    [SerializeField] private Button exitServerButton;

    [Tooltip("Кнопка 'Возродиться'.")]
    [SerializeField] private Button respawnButton;

    [Header("Respawn")]
    [Tooltip("Резервная точка возрождения, используется ТОЛЬКО если на сцене " +
             "нет ни одного объекта с компонентом NetworkStartPosition " +
             "(обычно тег такого объекта — \"Start Position\"). Если на сцене есть " +
             "хотя бы один NetworkStartPosition, респавн происходит в случайной " +
             "зарегистрированной точке (NetworkManager.startPositions), а это " +
             "поле игнорируется.")]
    [SerializeField] private Transform respawnPoint;

    [Header("Player")]
    [Tooltip("Игрок/корневой объект. Если не задан, используется GameObject с этим скриптом.")]
    [SerializeField] private GameObject playerObject;

    [Tooltip("Если включено, после смерти отключается CharacterController и STRELBA1/SELVEL8.")]
    [SerializeField] private bool disableControlOnDeath = true;

    [Tooltip("Если включено, после смерти отключаются CharacterController и все " +
             "Collider у дочерних объектов playerObject (труп не блокирует физику " +
             "и выстрелы), а при возрождении включаются обратно.")]
    [SerializeField] private bool disableCollidersOnDeath = true;

    [Tooltip("Если включено, после смерти отключаются все MeshRenderer и " +
             "SkinnedMeshRenderer у дочерних объектов playerObject (модель " +
             "становится невидимой для всех), а при возрождении включаются обратно.")]
    [SerializeField] private bool hideMeshOnDeath = true;

    [Header("Damage / Heal on Trigger")]
    [Tooltip("Сколько HP отнимать при касании объекта с тегом Damage.")]
    [SerializeField] private float damageAmount = 10f;

    [Tooltip("Сколько HP прибавлять при касании объекта с тегом Heal.")]
    [SerializeField] private float healAmount = 20f;

    [Tooltip("Минимальный интервал между срабатываниями триггеров.")]
    [SerializeField] private float triggerCooldown = 0.5f;

    [SyncVar]
    private bool isDead;

    private float lastTriggerTime = -999f;

    // Кэш рендереров модели (заполняется в Awake), чтобы не искать их
    // через GetComponentsInChildren каждый раз при смерти/респавне.
    private MeshRenderer[] meshRenderers;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;

    // Кэш коллайдеров тела (заполняется в Awake). CharacterController хранится
    // отдельно, т.к. отключается/включается по своей логике (в т.ч. на респавне).
    private Collider[] bodyColliders;
    private CharacterController characterController;

    // Плавно анимируемое отображаемое значение HP (для полосы и текста).
    // Реальное значение — healthValue (SyncVar), это только визуал.
    private float displayedHealth = -1f;

    // Плавно анимируемое значение для intermediate-полосы (та же логика,
    // что и displayedHealth, но с задержкой перед стартом движения).
    private float displayedHealthIntermediate = -1f;

    // Пока > 0, intermediate-полоса ждёт и не начинает двигаться к healthValue.
    private float intermediateDelayTimer;

    // Пока > 0, damagePanel остаётся видимой; затем плавно гаснет.
    private float damagePanelTimer;

    private void Awake()
    {
        if (playerObject == null)
            playerObject = gameObject;

        // Кэшируем все MeshRenderer/SkinnedMeshRenderer среди дочерних объектов
        // playerObject один раз, чтобы дальше просто переключать enabled.
        meshRenderers = playerObject.GetComponentsInChildren<MeshRenderer>(true);
        skinnedMeshRenderers = playerObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        // Кэшируем CharacterController и все обычные Collider среди дочерних
        // объектов playerObject (CharacterController тоже наследуется от Collider,
        // поэтому исключаем его из bodyColliders, чтобы не переключать дважды).
        characterController = playerObject.GetComponent<CharacterController>();
        bodyColliders = System.Array.FindAll(
            playerObject.GetComponentsInChildren<Collider>(true),
            c => !(c is CharacterController));

        // Panel Death должен быть выключен в начале.
        if (deathPanel != null)
            deathPanel.SetActive(false);

        // Damage panel в начале невидима.
        if (damagePanel != null)
        {
            damagePanel.alpha = 0f;
            damagePanel.blocksRaycasts = false;
            damagePanel.interactable = false;
        }

        if (exitServerButton != null)
            exitServerButton.onClick.AddListener(ExitServer);

        if (respawnButton != null)
            respawnButton.onClick.AddListener(RespawnPressed);
    }

    private void OnDestroy()
    {
        if (exitServerButton != null)
            exitServerButton.onClick.RemoveListener(ExitServer);

        if (respawnButton != null)
            respawnButton.onClick.RemoveListener(RespawnPressed);
    }

    private void Start()
    {
        // Стартуем анимацию сразу с текущим значением, без наплыва от 0.
        displayedHealth = healthValue;
        displayedHealthIntermediate = healthValue;
        UpdateHealthUI(displayedHealth);
        UpdateHealthUIIntermediate(displayedHealthIntermediate);

        if (isLocalPlayer)
            SetDeathUI(isDead);
    }

    private void Update()
    {
        UpdateDamagePanelFade();
        UpdateHealthDisplayLerp();
        UpdateHealthDisplayLerpIntermediate();
    }

    // ========================================================================
    // DAMAGE FROM STRELBA1.cs
    // ========================================================================

    /// <summary>
    /// Вызывается стрелком (SELVEL8) НА СВОЁМ ЖЕ объекте — то есть у вызывающего
    /// клиента есть authority над этим Healt_mirror_Player, поэтому обычный
    /// [Command] (без requiresAuthority = false) работает одинаково что у хоста,
    /// что у клиента. targetIdentity — это NetworkIdentity игрока, в которого
    /// попал выстрел; сервер сам найдёт на нём Healt_mirror_Player и применит урон.
    /// attackerIdentity — NetworkIdentity стреляющего (обычно connectionToClient.identity
    /// самого вызывающего), используется только для текста "Вас убил [Ник]".
    /// </summary>
    [Command]
    public void CmdDealDamage(NetworkIdentity targetIdentity, float amount, NetworkIdentity attackerIdentity)
    {
        if (targetIdentity == null)
            return;

        Healt_mirror_Player targetHealth =
            targetIdentity.GetComponent<Healt_mirror_Player>();

        if (targetHealth != null)
            targetHealth.ServerApplyDamage(amount, attackerIdentity);
    }

    /// <summary>
    /// Выполняется на сервере на объекте ЦЕЛИ. Не Command — вызывается сервером
    /// напрямую из CmdDealDamage стрелка, поэтому authority тут не при чём.
    /// attackerIdentity передаётся дальше в HandleDeath, чтобы отправить
    /// погибшему текст "Вас убил [Ник]".
    /// </summary>
    [Server]
    public void ServerApplyDamage(float amount, NetworkIdentity attackerIdentity = null)
    {
        if (isDead)
            return;

        amount = Mathf.Max(0f, amount);

        healthValue = Mathf.Max(healthValue - amount, 0f);

        Debug.Log(
            $"[Server] {gameObject.name} получил {amount} урона. " +
            $"HP: {healthValue}/{maxHealth}");

        if (healthValue <= 0f)
            HandleDeath(attackerIdentity);
    }

    // ========================================================================
    // HEAL
    // ========================================================================

    [Server]
    public void Heal(float amount)
    {
        if (isDead)
            return;

        amount = Mathf.Max(0f, amount);
        healthValue = Mathf.Min(healthValue + amount, maxHealth);

        Debug.Log(
            $"[Server] {gameObject.name} восстановил {amount} HP. " +
            $"HP: {healthValue}/{maxHealth}");
    }

    // ========================================================================
    // TRIGGERS
    // ========================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer || isDead)
            return;

        if (Time.time - lastTriggerTime < triggerCooldown)
            return;

        if (other.CompareTag("Damage") || other.CompareTag("damage"))
        {
            lastTriggerTime = Time.time;
            ServerApplyDamage(damageAmount);
        }
        else if (other.CompareTag("Heal"))
        {
            lastTriggerTime = Time.time;
            Heal(healAmount);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isServer || isDead)
            return;

        if (Time.time - lastTriggerTime < triggerCooldown)
            return;

        if (other.CompareTag("Damage") || other.CompareTag("damage"))
        {
            lastTriggerTime = Time.time;
            ServerApplyDamage(damageAmount);
        }
        else if (other.CompareTag("Heal"))
        {
            lastTriggerTime = Time.time;
            Heal(healAmount);
        }
    }

    // ========================================================================
    // HEALTH UI
    // ========================================================================

    // Хук SyncVar: срабатывает на всех клиентах (и на сервере, если это host)
    // при любом изменении healthValue. Сама плавная анимация полосы/текста
    // выполняется в Update() через displayedHealth — здесь только запускаем
    // одноразовые эффекты урона.
    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        // Если displayedHealth ещё не инициализирован (хук может прийти до Start
        // на удалённых клиентах), синхронизируем его с oldHealth, чтобы плавность
        // считалась от корректной точки, а не от 0.
        if (displayedHealth < 0f)
            displayedHealth = oldHealth;

        if (displayedHealthIntermediate < 0f)
            displayedHealthIntermediate = oldHealth;

        // Любое изменение HP (урон или лечение) заново запускает задержку —
        // intermediate-полоса «застывает» на старом значении на N секунд,
        // прежде чем начать плавно подтягиваться к новому.
        intermediateDelayTimer = intermediateDelay;

        bool tookDamage = newHealth < oldHealth;

        if (tookDamage)
            PlayDamageFeedback();
    }

    /// <summary>
    /// Одноразовые эффекты при получении урона: частицы на игроке (у всех,
    /// кто видит объект) и красная панель-виньетка (только у пострадавшего
    /// локального игрока).
    /// </summary>
    private void PlayDamageFeedback()
    {
        if (damageParticles != null)
            damageParticles.Play();

        if (isLocalPlayer && damagePanel != null)
        {
            damagePanel.alpha = 1f;
            damagePanelTimer = damagePanelDuration;
        }
    }

    /// <summary>
    /// Плавно подводит displayedHealth к реальному healthValue и обновляет UI.
    /// Вызывается каждый кадр из Update().
    /// </summary>
    private void UpdateHealthDisplayLerp()
    {
        if (displayedHealth < 0f)
            displayedHealth = healthValue;

        if (Mathf.Approximately(displayedHealth, healthValue))
            return;

        displayedHealth = Mathf.MoveTowards(
            displayedHealth,
            healthValue,
            healthLerpSpeed * Time.deltaTime);

        UpdateHealthUI(displayedHealth);
    }

    /// <summary>
    /// То же самое, что UpdateHealthDisplayLerp, но для «догоняющей»
    /// intermediate-полосы: сначала ждёт intermediateDelayTimer секунд
    /// (не двигаясь), и только потом плавно подводит displayedHealthIntermediate
    /// к текущему healthValue.
    /// </summary>
    private void UpdateHealthDisplayLerpIntermediate()
    {
        if (displayedHealthIntermediate < 0f)
            displayedHealthIntermediate = healthValue;

        // Пока идёт задержка — полоса стоит на месте, просто отсчитываем время.
        if (intermediateDelayTimer > 0f)
        {
            intermediateDelayTimer -= Time.deltaTime;
            return;
        }

        if (Mathf.Approximately(displayedHealthIntermediate, healthValue))
            return;

        float speed = intermediateLerpSpeed > 0f ? intermediateLerpSpeed : healthLerpSpeed;

        displayedHealthIntermediate = Mathf.MoveTowards(
            displayedHealthIntermediate,
            healthValue,
            speed * Time.deltaTime);

        UpdateHealthUIIntermediate(displayedHealthIntermediate);
    }

    /// <summary>
    /// Держит damage panel видимой damagePanelDuration секунд после урона,
    /// затем плавно гасит альфу до 0.
    /// </summary>
    private void UpdateDamagePanelFade()
    {
        if (!isLocalPlayer || damagePanel == null)
            return;

        if (damagePanelTimer > 0f)
        {
            damagePanelTimer -= Time.deltaTime;
            return;
        }

        if (damagePanel.alpha > 0f)
        {
            damagePanel.alpha = Mathf.MoveTowards(
                damagePanel.alpha,
                0f,
                damagePanelFadeSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Обновляет полосу HP (Image.fillAmount) и текст (TMP_Text) по переданному
    /// значению — используется как для мгновенной инициализации, так и для
    /// каждого шага плавной анимации.
    /// </summary>
    private void UpdateHealthUI(float shownHealth)
    {
        float max = Mathf.Max(0.01f, maxHealth);

        if (healthBarImage != null)
            healthBarImage.fillAmount = Mathf.Clamp01(shownHealth / max);

        if (healthText != null)
        {
            int shown = Mathf.CeilToInt(Mathf.Clamp(shownHealth, 0f, maxHealth));
            int maxInt = Mathf.CeilToInt(maxHealth);
            healthText.text = $"{shown} / {maxInt}";
        }
    }

    /// <summary>
    /// Обновляет только fillAmount intermediate-полосы HP. Текст HP не трогает —
    /// его показывает основная полоса через UpdateHealthUI.
    /// </summary>
    private void UpdateHealthUIIntermediate(float shownHealth)
    {
        if (healthBarImageIntermediate == null)
            return;

        float max = Mathf.Max(0.01f, maxHealth);
        healthBarImageIntermediate.fillAmount = Mathf.Clamp01(shownHealth / max);
    }

    // ========================================================================
    // DEATH
    // ========================================================================

    [Server]
    private void HandleDeath(NetworkIdentity attackerIdentity = null)
    {
        if (isDead)
            return;

        isDead = true;

        // Пытаемся достать ник убийцы из его NetworkPlayerName (см. NetworkPlayerName.cs).
        // Если атакующий не передан, это не игрок, или на нём нет NetworkPlayerName —
        // используем нейтральную подпись, чтобы текст не оставался пустым.
        string killerName = "неизвестный";

        if (attackerIdentity != null)
        {
            NetworkPlayerName attackerName =
                attackerIdentity.GetComponent<NetworkPlayerName>();

            if (attackerName != null && !string.IsNullOrEmpty(attackerName.nickname))
                killerName = attackerName.nickname;
        }

        Debug.Log($"[Server] {gameObject.name} умер. Убийца: {killerName}");

        RpcShowDeathPanel(killerName);
    }

    [ClientRpc]
    private void RpcShowDeathPanel(string killerName)
    {
        // Скрываем модель (Mesh/SkinnedMesh renderers) у ВСЕХ наблюдателей,
        // не только у локального игрока.
        SetMeshRenderersEnabled(false);

        // Отключаем коллизию (CharacterController + Collider) у ВСЕХ наблюдателей —
        // труп не должен блокировать движение или ловить выстрелы.
        SetCollidersEnabled(false);

        // Панель смерти и текст "Вас убил ..." нужны только погибшему локальному игроку.
        if (!isLocalPlayer)
            return;

        if (killText != null)
            killText.text = string.Format(killTextFormat, killerName);

        SetDeathUI(true);
    }

    private void SetDeathUI(bool show)
    {
        if (!isLocalPlayer)
            return;

        if (deathPanel != null)
            deathPanel.SetActive(show);

        SetPlayerControlEnabled(!show);
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void SetPlayerControlEnabled(bool enabled)
    {
        if (!disableControlOnDeath || playerObject == null)
            return;

        // CharacterController теперь отключается в SetCollidersEnabled (вызывается
        // для ВСЕХ наблюдателей через ClientRpc), здесь остаётся только оружие,
        // которое имеет смысл блокировать только локально у самого игрока.

        // STRELBA1.cs содержит класс SELVEL8.
        SELVEL8 weapon = playerObject.GetComponentInChildren<SELVEL8>(true);

        if (weapon != null && weapon != this)
            weapon.enabled = enabled;
    }

    /// <summary>
    /// Включает/выключает CharacterController и все обычные Collider среди
    /// дочерних объектов playerObject. Вызывается на всех клиентах (через
    /// ClientRpc), поэтому труп перестаёт физически мешать и ловить попадания
    /// у ВСЕХ наблюдателей, а не только у локального игрока.
    /// </summary>
    private void SetCollidersEnabled(bool enabled)
    {
        if (!disableCollidersOnDeath)
            return;

        if (characterController != null)
            characterController.enabled = enabled;

        if (bodyColliders != null)
        {
            foreach (Collider col in bodyColliders)
            {
                if (col != null)
                    col.enabled = enabled;
            }
        }
    }

    /// <summary>
    /// Включает/выключает все MeshRenderer и SkinnedMeshRenderer среди дочерних
    /// объектов playerObject. Вызывается на всех клиентах (через ClientRpc),
    /// поэтому модель скрывается/появляется у ВСЕХ наблюдателей, а не только
    /// у локального игрока.
    /// </summary>
    private void SetMeshRenderersEnabled(bool enabled)
    {
        if (!hideMeshOnDeath)
            return;

        if (meshRenderers != null)
        {
            foreach (MeshRenderer renderer in meshRenderers)
            {
                if (renderer != null)
                    renderer.enabled = enabled;
            }
        }

        if (skinnedMeshRenderers != null)
        {
            foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            {
                if (renderer != null)
                    renderer.enabled = enabled;
            }
        }
    }

    // ========================================================================
    // BUTTON: RESPawn
    // ========================================================================

    private void RespawnPressed()
    {
        if (!isLocalPlayer || !isDead)
            return;

        CmdRespawn();
    }

    [Command]
    private void CmdRespawn()
    {
        if (!isDead)
            return;

        healthValue = maxHealth;
        isDead = false;

        Transform spawnPoint = GetRespawnPoint();

        if (spawnPoint != null)
        {
            bool controllerWasEnabled = characterController != null && characterController.enabled;

            if (characterController != null)
                characterController.enabled = false;

            Transform target = playerObject != null
                ? playerObject.transform
                : transform;

            target.SetPositionAndRotation(
                spawnPoint.position,
                spawnPoint.rotation);

            if (characterController != null)
                characterController.enabled = controllerWasEnabled;
        }
        else
        {
            // Ни одной NetworkStartPosition на сцене, ни резервного respawnPoint —
            // игрок возрождается там, где умер (позиция не меняется).
            Debug.LogWarning(
                $"[Server] {gameObject.name}: не найдено ни одной " +
                "NetworkStartPosition на сцене и не задан резервный respawnPoint. " +
                "Игрок возродится на месте смерти.");
        }

        lastTriggerTime = -999f;

        Debug.Log($"[Server] {gameObject.name} возрождён.");
        RpcHideDeathPanel();
    }

    /// <summary>
    /// Возвращает точку возрождения. Приоритет — точки, зарегистрированные
    /// компонентами NetworkStartPosition (NetworkManager.startPositions):
    /// именно так respawn "работает только когда на сцене есть объект с тегом
    /// Start Position / компонентом NetworkStartPosition". Если на сцене нет ни
    /// одной такой точки, используется резервное поле respawnPoint (если задано).
    /// Если и его нет — возвращается null, и игрок остаётся на месте смерти.
    /// </summary>
    [Server]
    private Transform GetRespawnPoint()
    {
        // NetworkManager сам чистит startPositions в UnRegisterStartPosition при
        // уничтожении объекта, но на всякий случай отфильтруем null-ссылки.
        System.Collections.Generic.List<Transform> startPositions =
            NetworkManager.startPositions;

        if (startPositions != null && startPositions.Count > 0)
        {
            System.Collections.Generic.List<Transform> validPositions =
                startPositions.FindAll(t => t != null);

            if (validPositions.Count > 0)
                return validPositions[Random.Range(0, validPositions.Count)];
        }

        // Fallback: ни одной NetworkStartPosition на сцене нет.
        return respawnPoint;
    }

    [ClientRpc]
    private void RpcHideDeathPanel()
    {
        // Мгновенно показываем полное HP всем наблюдателям — плавная анимация
        // здесь не нужна, респавн и так резкое событие. Обе полосы (основная и
        // intermediate) сбрасываются мгновенно, без задержки intermediateDelay.
        displayedHealth = healthValue;
        displayedHealthIntermediate = healthValue;
        intermediateDelayTimer = 0f;
        UpdateHealthUI(displayedHealth);
        UpdateHealthUIIntermediate(displayedHealthIntermediate);

        // Возвращаем видимость модели ВСЕМ наблюдателям.
        SetMeshRenderersEnabled(true);

        // Возвращаем коллизию (CharacterController + Collider) ВСЕМ наблюдателям.
        SetCollidersEnabled(true);

        if (!isLocalPlayer)
            return;

        if (killText != null)
            killText.text = string.Empty;

        SetDeathUI(false);
    }

    // ========================================================================
    // BUTTON: EXIT SERVER
    // ========================================================================

    private void ExitServer()
    {
        if (!isLocalPlayer)
            return;

        // Для обычного клиента — выйти из подключения.
        // Для Host — остановить Host целиком.
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
    }

    // ========================================================================
    // PUBLIC API
    // ========================================================================

    public float GetCurrentHealth() => healthValue;

    public float GetMaxHealth() => maxHealth;

    public bool IsDead => isDead;
}