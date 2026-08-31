
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CameraPushTrigge_1r : MonoBehaviour
{
    [Header("Настройки отталкивания")]
    [SerializeField] private float pushForce = 10f;
    [SerializeField] private Camera targetCamera;

    [Header("Объекты для отталкивания")]
    [SerializeField] private List<GameObject> pushableObjects = new List<GameObject>();

    [Header("Совместимость с SELVEL8")]
    [SerializeField] private bool allowBulletPrefabs = true;
    [SerializeField] private string bulletPrefabNameContains = "bullet";
    [SerializeField] private bool pushBulletsAutomatically = true;
    [SerializeField] private float bulletPushMultiplier = 2f;

    [Header("Дополнительные настройки")]
    [SerializeField] private bool useMainCameraIfNull = true;
    [SerializeField] private bool pushOnlyOnce = false;
    [SerializeField] private float cooldownTime = 1f;

    [Header("Управление")]
    [SerializeField] private KeyCode pushKey = KeyCode.F;
    [SerializeField] private bool requireKeyPress = true;

    [Header("Звуковые эффекты")]
    [SerializeField] private AudioClip pushSound;
    [SerializeField] private bool playUniqueSound = false;
    [SerializeField] private AudioClip[] pushSounds;
    [SerializeField] private float soundVolume = 1f;
    [SerializeField] private bool randomizePitch = false;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;

    [Header("UI панель")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private float panelActiveDuration = 1f;
    [SerializeField] private bool extendTimeOnMultiplePushes = true;

    [Header("Анимация")]
    [SerializeField] private Animator pushAnimator;
    [SerializeField] private string pushAnimationTrigger = "Push";
    [SerializeField] private string pushAnimationBool = "";
    [SerializeField] private bool useTrigger = true;
    [SerializeField] private bool resetBoolAfterDelay = false;
    [SerializeField] private float boolResetDelay = 1f;
    [SerializeField] private bool playAnimationOnEachPush = true;
    [SerializeField] private bool separateAnimationsForBullets = false;
    [SerializeField] private string bulletPushAnimationTrigger = "BulletPush";

    private Dictionary<GameObject, float> lastPushTime = new Dictionary<GameObject, float>();
    private List<GameObject> objectsInTrigger = new List<GameObject>();
    private AudioSource audioSource;
    private Coroutine panelCoroutine;

    // Кэш для оптимизации проверок SELVEL8
    private SELVEL8 connectedWeapon;
    private int bulletPrefabNameHash;

    // Кэш для анимации (оптимизация)
    private int pushAnimationTriggerHash;
    private int bulletPushAnimationTriggerHash;
    private bool hasAnimator = false;
    private Coroutine boolResetCoroutine;

    private void Start()
    {
        InitializeCamera();
        InitializeAudio();
        InitializeUI();
        InitializeAnimation();
        InitializeSELVEL8Compatibility();
        ValidateCollider();
    }

    private void InitializeCamera()
    {
        if (targetCamera == null && useMainCameraIfNull)
        {
            targetCamera = Camera.main;
        }
    }

    private void InitializeAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
    }

    private void InitializeUI()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }

    private void InitializeAnimation()
    {
        // Проверяем наличие аниматора
        if (pushAnimator == null)
        {
            pushAnimator = GetComponent<Animator>();
        }

        hasAnimator = pushAnimator != null;

        if (hasAnimator)
        {
            // Кэшируем хэши анимационных триггеров для оптимизации
            if (!string.IsNullOrEmpty(pushAnimationTrigger))
            {
                pushAnimationTriggerHash = Animator.StringToHash(pushAnimationTrigger);
            }

            if (separateAnimationsForBullets && !string.IsNullOrEmpty(bulletPushAnimationTrigger))
            {
                bulletPushAnimationTriggerHash = Animator.StringToHash(bulletPushAnimationTrigger);
            }

            // Сбрасываем bool параметр при старте, если он используется
            if (!useTrigger && !string.IsNullOrEmpty(pushAnimationBool))
            {
                pushAnimator.SetBool(pushAnimationBool, false);
            }

            Debug.Log($"Анимация инициализирована. Используется: {(useTrigger ? "триггер" : "bool")} параметр");
        }
        else if (!string.IsNullOrEmpty(pushAnimationTrigger))
        {
            Debug.LogWarning("Animator не найден, но настроены параметры анимации!");
        }
    }

    private void InitializeSELVEL8Compatibility()
    {
        // Ищем компонент SELVEL8 в сцене для совместимости
        connectedWeapon = FindObjectOfType<SELVEL8>();

        // Кэшируем хэш имени для оптимизации
        if (!string.IsNullOrEmpty(bulletPrefabNameContains))
        {
            bulletPrefabNameHash = bulletPrefabNameContains.ToLower().GetHashCode();
        }

        // Если найден SELVEL8 и включено автоматическое добавление пуль
        if (connectedWeapon != null && allowBulletPrefabs && connectedWeapon.bulletPrefab != null)
        {
            AddPushableObject(connectedWeapon.bulletPrefab);
            Debug.Log($"Автоматически добавлен префаб пули {connectedWeapon.bulletPrefab.name} в список отталкиваемых объектов");
        }
    }

    private void ValidateCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"У объекта {gameObject.name} нет Collider компонента!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"У Collider объекта {gameObject.name} не включен IsTrigger!");
        }
    }

    private void Update()
    {
        if (requireKeyPress && Input.GetKeyDown(pushKey))
        {
            PushAllObjectsInTrigger();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, подходит ли объект для отталкивания
        if (!IsObjectPushable(other.gameObject))
            return;

        // Добавляем объект в список находящихся в триггере
        if (!objectsInTrigger.Contains(other.gameObject))
        {
            objectsInTrigger.Add(other.gameObject);
        }

        // Если не требуется нажатие клавиши, отталкиваем сразу
        if (!requireKeyPress)
        {
            if (CanPushObject(other.gameObject))
            {
                PushObject(other.gameObject);
                RecordPushTime(other.gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (objectsInTrigger.Contains(other.gameObject))
        {
            objectsInTrigger.Remove(other.gameObject);
        }
    }

    private bool IsObjectPushable(GameObject obj)
    {
        // Проверяем список разрешенных объектов
        if (pushableObjects.Contains(obj))
            return true;

        // Проверяем совместимость с SELVEL8 пулями
        if (allowBulletPrefabs && IsBulletFromSELVEL8(obj))
            return true;

        return false;
    }

    private bool IsBulletFromSELVEL8(GameObject obj)
    {
        // Проверяем по имени (оптимизированная версия)
        if (!string.IsNullOrEmpty(bulletPrefabNameContains))
        {
            string objNameLower = obj.name.ToLower();
            if (objNameLower.Contains(bulletPrefabNameContains.ToLower()))
                return true;
        }

        // Проверяем, является ли объект экземпляром пули SELVEL8
        if (connectedWeapon != null && connectedWeapon.bulletPrefab != null)
        {
            string bulletPrefabName = connectedWeapon.bulletPrefab.name;
            if (obj.name.Contains(bulletPrefabName) || obj.name.Replace("(Clone)", "").Trim() == bulletPrefabName)
                return true;
        }

        // Проверяем наличие компонентов, характерных для пуль
        return obj.GetComponent<Rigidbody>() != null &&
               (obj.GetComponent<Bulletdamage>() != null || obj.GetComponent<Collider>() != null);
    }

    private bool CanPushObject(GameObject obj)
    {
        if (!pushOnlyOnce) return true;

        if (lastPushTime.ContainsKey(obj))
        {
            return Time.time - lastPushTime[obj] >= cooldownTime;
        }

        return true;
    }

    private void RecordPushTime(GameObject obj)
    {
        if (pushOnlyOnce)
        {
            lastPushTime[obj] = Time.time;
        }
    }

    private void PushAllObjectsInTrigger()
    {
        foreach (GameObject obj in objectsInTrigger.ToArray())
        {
            if (CanPushObject(obj))
            {
                PushObject(obj);
                RecordPushTime(obj);
            }
        }
    }

    private void PushObject(GameObject obj)
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("Камера не назначена!");
            return;
        }

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"У объекта {obj.name} нет Rigidbody компонента!");
            return;
        }

        Vector3 cameraForward = targetCamera.transform.forward;
        float actualPushForce = pushForce;

        // Увеличиваем силу для пуль, если включен множитель
        if (IsBulletFromSELVEL8(obj))
        {
            actualPushForce *= bulletPushMultiplier;
        }

        // Проверяем, является ли объект пулей с компонентом Bulletdamage
        Bulletdamage bulletDamage = obj.GetComponent<Bulletdamage>();
        if (bulletDamage != null && bulletDamage.CanBePushed)
        {
            bulletDamage.OnCameraPush(cameraForward, actualPushForce);
        }
        else
        {
            // Для пуль SELVEL8 используем более подходящий способ
            if (IsBulletFromSELVEL8(obj))
            {
                // Добавляем силу к текущей скорости пули
                Vector3 currentVelocity = rb.velocity;
                Vector3 pushVelocity = cameraForward * actualPushForce;
                rb.velocity = currentVelocity + pushVelocity;
            }
            else
            {
                // Обычное отталкивание для других объектов
                rb.AddForce(cameraForward * actualPushForce, ForceMode.Impulse);
            }
        }

        PlayPushSound();
        PlayPushAnimation(obj);
        ActivatePanel();

        string pushType = IsBulletFromSELVEL8(obj) ? "пуля SELVEL8" : "объект";
        Debug.Log($"{pushType} {obj.name} оттолкнут в направлении камеры с силой {actualPushForce} (клавиша: {(requireKeyPress ? pushKey.ToString() : "авто")})");
    }

    private void PlayPushAnimation(GameObject pushedObject)
    {
        if (!hasAnimator || !playAnimationOnEachPush) return;

        bool isBullet = IsBulletFromSELVEL8(pushedObject);

        if (useTrigger)
        {
            // Используем триггер
            if (separateAnimationsForBullets && isBullet && bulletPushAnimationTriggerHash != 0)
            {
                pushAnimator.SetTrigger(bulletPushAnimationTriggerHash);
                Debug.Log($"Запущена анимация отталкивания пули: {bulletPushAnimationTrigger}");
            }
            else if (pushAnimationTriggerHash != 0)
            {
                pushAnimator.SetTrigger(pushAnimationTriggerHash);
                Debug.Log($"Запущена анимация отталкивания: {pushAnimationTrigger}");
            }
        }
        else
        {
            // Используем bool параметр
            if (!string.IsNullOrEmpty(pushAnimationBool))
            {
                pushAnimator.SetBool(pushAnimationBool, true);
                Debug.Log($"Активирован bool параметр анимации: {pushAnimationBool}");

                // Сбрасываем bool через заданное время
                if (resetBoolAfterDelay)
                {
                    if (boolResetCoroutine != null)
                    {
                        StopCoroutine(boolResetCoroutine);
                    }
                    boolResetCoroutine = StartCoroutine(ResetAnimationBoolAfterDelay());
                }
            }
        }
    }

    private System.Collections.IEnumerator ResetAnimationBoolAfterDelay()
    {
        yield return new WaitForSeconds(boolResetDelay);

        if (hasAnimator && !string.IsNullOrEmpty(pushAnimationBool))
        {
            pushAnimator.SetBool(pushAnimationBool, false);
            Debug.Log($"Сброшен bool параметр анимации: {pushAnimationBool}");
        }

        boolResetCoroutine = null;
    }

    private void PlayPushSound()
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = null;

        if (playUniqueSound && pushSounds != null && pushSounds.Length > 0)
        {
            clipToPlay = pushSounds[Random.Range(0, pushSounds.Length)];
        }
        else if (pushSound != null)
        {
            clipToPlay = pushSound;
        }

        if (clipToPlay != null)
        {
            audioSource.volume = soundVolume;

            if (randomizePitch)
            {
                audioSource.pitch = Random.Range(minPitch, maxPitch);
            }
            else
            {
                audioSource.pitch = 1f;
            }

            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private void ActivatePanel()
    {
        if (uiPanel == null) return;

        if (extendTimeOnMultiplePushes && uiPanel.activeInHierarchy)
        {
            if (panelCoroutine != null)
            {
                StopCoroutine(panelCoroutine);
            }
        }
        else
        {
            uiPanel.SetActive(true);
        }

        panelCoroutine = StartCoroutine(DeactivatePanelAfterDelay());
    }

    private System.Collections.IEnumerator DeactivatePanelAfterDelay()
    {
        yield return new WaitForSeconds(panelActiveDuration);

        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        panelCoroutine = null;
    }

    // Публичные методы для управления
    public void DeactivatePanel()
    {
        if (panelCoroutine != null)
        {
            StopCoroutine(panelCoroutine);
            panelCoroutine = null;
        }

        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }

    public void ForceActivatePanel(float duration = -1)
    {
        if (uiPanel == null) return;

        DeactivatePanel();
        uiPanel.SetActive(true);

        float actualDuration = duration > 0 ? duration : panelActiveDuration;
        panelCoroutine = StartCoroutine(DeactivatePanelAfterDelayCustom(actualDuration));
    }

    private System.Collections.IEnumerator DeactivatePanelAfterDelayCustom(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        panelCoroutine = null;
    }

    public void AddPushableObject(GameObject obj)
    {
        if (!pushableObjects.Contains(obj))
        {
            pushableObjects.Add(obj);
        }
    }

    public void RemovePushableObject(GameObject obj)
    {
        pushableObjects.Remove(obj);
    }

    public void ForcePush(GameObject obj)
    {
        if (IsObjectPushable(obj))
        {
            PushObject(obj);
        }
    }

    // Методы для совместимости с SELVEL8
    public void SetConnectedWeapon(SELVEL8 weapon)
    {
        connectedWeapon = weapon;
        if (weapon != null && weapon.bulletPrefab != null && allowBulletPrefabs)
        {
            AddPushableObject(weapon.bulletPrefab);
        }
    }

    public void SetBulletPushMultiplier(float multiplier)
    {
        bulletPushMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void ToggleBulletAutoDetection(bool enabled)
    {
        allowBulletPrefabs = enabled;
    }

    // Методы для управления анимацией
    public void SetPushAnimator(Animator animator)
    {
        pushAnimator = animator;
        hasAnimator = animator != null;

        if (hasAnimator)
        {
            InitializeAnimation();
        }
    }

    public void PlayPushAnimationManually(bool isBullet = false)
    {
        if (!hasAnimator) return;

        if (useTrigger)
        {
            if (separateAnimationsForBullets && isBullet && bulletPushAnimationTriggerHash != 0)
            {
                pushAnimator.SetTrigger(bulletPushAnimationTriggerHash);
            }
            else if (pushAnimationTriggerHash != 0)
            {
                pushAnimator.SetTrigger(pushAnimationTriggerHash);
            }
        }
        else if (!string.IsNullOrEmpty(pushAnimationBool))
        {
            pushAnimator.SetBool(pushAnimationBool, true);

            if (resetBoolAfterDelay)
            {
                if (boolResetCoroutine != null)
                {
                    StopCoroutine(boolResetCoroutine);
                }
                boolResetCoroutine = StartCoroutine(ResetAnimationBoolAfterDelay());
            }
        }
    }

    public void ResetAnimationBool()
    {
        if (hasAnimator && !string.IsNullOrEmpty(pushAnimationBool))
        {
            pushAnimator.SetBool(pushAnimationBool, false);

            if (boolResetCoroutine != null)
            {
                StopCoroutine(boolResetCoroutine);
                boolResetCoroutine = null;
            }
        }
    }

    public void ToggleAnimationPlayback(bool enabled)
    {
        playAnimationOnEachPush = enabled;
    }

    // Визуализация в редакторе
    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.DrawWireCube(capsule.center, new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2));
            }
        }

        // Показываем направление камеры
        if (targetCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.identity;
            Vector3 start = transform.position;
            Vector3 direction = targetCamera.transform.forward * 3f;
            Gizmos.DrawRay(start, direction);
            Gizmos.DrawWireSphere(start + direction, 0.2f);
        }

        // Показываем подключенное оружие SELVEL8
        if (connectedWeapon != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, connectedWeapon.transform.position);
            Gizmos.DrawWireSphere(connectedWeapon.transform.position, 0.5f);
        }
    }
}