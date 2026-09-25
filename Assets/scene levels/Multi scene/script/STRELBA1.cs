using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Mirror;
using TMPro;


public class SELVEL8 : MonoBehaviour
{
    // Health-компонент стрелка (владельца этого оружия). Нужен, чтобы
    // отправить Command на СВОЁМ объекте, а не на объекте цели.
    private Healt_mirror_Player _ownerHealth;
    private NetworkIdentity _ownerIdentity;

    [Header("Legacy Compatibility")]
    [Tooltip("Старое поле для совместимости с другими скриптами. При Raycast не используется.")]
    public GameObject bulletPrefab;

    // ============================================================
    // RAYCAST / СТРЕЛЬБА
    // ============================================================
    [Header("Стрельба (RayCast)")]
    [Tooltip("Камера игрока. Если не задана — используется Camera.main")]
    [SerializeField] private Camera _camera;

    [Tooltip("Откуда визуально идёт выстрел")]
    [SerializeField] private Transform _muzzlePoint;

    [Tooltip("Максимальная дальность луча")]
    [SerializeField] private float _range = 100f;

    [Tooltip("Урон за обычный выстрел")]
    [SerializeField] private float _damage = 25f;

    [Tooltip("Сила удара по Rigidbody")]
    [SerializeField] private float _impactForce = 30f;

    [Tooltip("Слои, по которым может попасть Raycast")]
    [SerializeField] private LayerMask _hitLayers = ~0;

    [Tooltip("Автоматический огонь")]
    [SerializeField] private bool _autoFire = false;

    [Tooltip("Выстрелов в минуту")]
    [SerializeField] private float _fireRate = 600f;

    public GameObject pause;

    // ============================================================
    // ПАТРОНЫ
    // ============================================================
    [Header("Патроны")]
    [SerializeField] private bool _useMagazine = true;
    [SerializeField] private int _magazineSize = 30;
    [SerializeField] private int _currentAmmo = 30;
    [SerializeField] private float _reloadTime = 2f;

    // ============================================================
    // РАЗБРОС
    // ============================================================
    [Header("Разброс")]
    [SerializeField] private bool _useSpread = false;
    [SerializeField] private float _spreadAngle = 2f;

    private void ApplyDamageToHitObject(RaycastHit hit, float amount)
    {
        // Урон отправляется на сервер только локальным владельцем оружия.
        // Reflection здесь больше не используется: Mirror weaver подменяет
        // тело [Command]-методов, и вызов TakeDamage(...) "как обычного
        // метода" через Invoke вёл себя по-разному на хосте и на клиенте
        // (двойное срабатывание у хоста, отсутствие урона у клиента).
        if (_ownerHealth == null || _ownerIdentity == null || !_ownerIdentity.isLocalPlayer)
            return;

        NetworkIdentity targetIdentity =
            hit.collider.GetComponentInParent<NetworkIdentity>();

        if (targetIdentity == null)
            return;

        // Не наносим урон самому себе (например, если рейкаст зацепил
        // собственный коллайдер стрелка).
        if (targetIdentity == _ownerIdentity)
            return;

        Healt_mirror_Player targetHealth =
            targetIdentity.GetComponent<Healt_mirror_Player>();

        if (targetHealth == null)
            return;

        // Command вызывается на объекте СТРЕЛКА (у него есть authority),
        // а не на объекте цели — так Mirror работает одинаково что у хоста,
        // что у обычного клиента. Третий аргумент — NetworkIdentity самого
        // стрелка, нужен на сервере только для текста "Вас убил [Ник]".
        _ownerHealth.CmdDealDamage(targetIdentity, amount, _ownerIdentity);
    }

    // ============================================================
    // CHARGED SHOT
    // ============================================================
    [Header("Заряженный выстрел")]
    [SerializeField] private bool _chargedShotEnabled = true;
    [Tooltip("Клавиша заряда. R оставлена для перезарядки.")]
    [SerializeField] private KeyCode _chargeKey = KeyCode.F;

    [SerializeField, Min(0.05f)] private float _chargeTime = 2f;
    [SerializeField, Min(1f)] private float _maxDamageMultiplier = 100f;
    [SerializeField] private AnimationCurve _chargeCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Эффекты заряда")]
    [SerializeField] private ParticleSystem _chargeEffect;
    [SerializeField] private AudioClip _chargeSound;
    [SerializeField] private AudioClip _fullyChargedSound;
    [SerializeField] private Color _chargeColor = Color.yellow;

    private bool _isCharging;
    private float _currentChargeTime;
    private float _currentDamageMultiplier = 1f;
    private bool _isFullyCharged;
    private AudioSource _chargeAudioSource;

    // ============================================================
    // ORBIT TRAP
    // ============================================================
    [Header("Orbit Trap")]
    [Tooltip("Клавиша запуска Orbit Trap")]
    [SerializeField] private KeyCode _orbitTrapKey = KeyCode.Q;
    public GameObject orbitTrapPrefab;
    public float orbitTrapSpeed = 15f;
    public float orbitTrapFireRate = 1f;
    public int maxOrbitTraps = 3;
    public float orbitTrapLifetime = 30f;
    public AudioClip orbitTrapShootSound;
    public string orbitTrapAnimationTrigger = "OrbitTrap";
    private float nextOrbitTrapTime;
    private readonly List<GameObject> activeOrbitTraps = new List<GameObject>();

    // ============================================================
    // ОТДАЧА
    // ============================================================
    [Header("Отдача")]
    [SerializeField] private bool _recoilEnabled = true;
    [SerializeField] private float _recoilPitchAmount = 2f;
    [SerializeField] private float _recoilYawAmount = 0.5f;
    [SerializeField] private float _recoilSnappiness = 20f;
    [SerializeField] private float _recoilReturnSpeed = 8f;
    [SerializeField] private float _maxRecoilPitch = 15f;

    [SerializeField] private Transform _weaponTransform;
    [SerializeField] private Vector3 _weaponKickback = new Vector3(0f, 0f, -0.15f);
    [SerializeField] private Vector3 _weaponKickRotation = new Vector3(-5f, 0f, 0f);
    [SerializeField] private float _weaponReturnSpeed = 10f;

    private Vector3 _currentRecoil;
    private Vector3 _targetRecoil;
    private Vector3 _weaponCurrentKick;
    private Vector3 _weaponCurrentRotKick;
    private Quaternion _cameraOriginalLocalRotation;

    // ============================================================
    // CAMERA SHAKE
    // ============================================================
    [Header("Сдвиг камеры при выстреле")]
    [SerializeField] private bool _cameraShakeEnabled = true;
    [SerializeField] private float _shakeAmplitudeX = 0.02f;
    [SerializeField] private float _shakeAmplitudeY = 0.03f;
    [SerializeField] private float _shakeAmplitudeZ = 0.01f;
    [SerializeField] private float _shakeDamping = 12f;
    [SerializeField] private float _shakeFrequency = 25f;
    [SerializeField, Range(0f, 1f)] private float _shakeAccumulation = 0.3f;

    private Vector3 _camOriginalLocalPos;
    private Vector3 _currentShakeOffset;
    private Vector3 _targetShakeOffset;
    private float _shakeTimer;

    // ============================================================
    // НАГРЕВ ДУЛА
    // ============================================================
    [Header("Нагрев дула")]
    [SerializeField] private bool _muzzleHeatEnabled = true;
    [SerializeField] private Renderer _muzzleRenderer;
    [SerializeField] private int _muzzleMaterialIndex = 0;
    [SerializeField, Range(0f, 1f)] private float _heatPerShot = 0.18f;
    [SerializeField] private float _cooldownRate = 0.12f;
    [SerializeField] private float _heatRiseSpeed = 20f;
    [SerializeField, Range(0f, 1f)] private float _maxHeat = 1f;
    [SerializeField, Range(0f, 1f)] private float _overheatThreshold = 0.95f;
    [SerializeField] private bool _overheatEnabled = false;
    [SerializeField] private float _overheatCooldownTime = 3f;
    [SerializeField] private TextMeshProUGUI _heatText;
    [SerializeField] private float _maxDisplayTemp = 800f;

    private float _heatTarget;
    private float _currentHeat;
    private bool _isOverheated;
    private float _overheatTimer;
    private Material _heatMatInstance;
    private static readonly int _shaderPropHeat = Shader.PropertyToID("_HeatIntensity");

    // ============================================================
    // ANIMATOR
    // ============================================================
    [Header("Animator")]
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private string _animShoot = "Shoot";
    [SerializeField] private string _animReload = "Reload";
    [SerializeField] private string _animAim = "IsAiming";
    [SerializeField] private string _animWalk = "IsWalking";
    [SerializeField] private string _animSpeed = "MoveSpeed";
    [SerializeField] private string _animCharge = "Charge";

    private int _hashShoot;
    private int _hashReload;
    private int _hashAim;
    private int _hashWalk;
    private int _hashSpeed;
    private int _hashCharge;
    private int _hashOrbitTrap;

    // ============================================================
    // ADS
    // ============================================================
    [Header("Прицеливание (ADS)")]
    [SerializeField] private bool _aimingEnabled = true;
    [SerializeField] private Vector3 _adsPosition = new Vector3(0f, -0.05f, 0.1f);
    [SerializeField] private float _adsSpeed = 10f;
    private bool _isAiming;

    // ============================================================
    // IDLE / WALK BOB
    // ============================================================
    [Header("Idle Bob")]
    [SerializeField] private bool _idleBobEnabled = true;
    [SerializeField] private float _idleBobAmplY = 0.003f;
    [SerializeField] private float _idleBobAmplX = 0.002f;
    [SerializeField] private float _idleBobSpeed = 1.2f;
    [SerializeField] private float _idleSwayAmount = 0.3f;

    [Header("Walk Bob")]
    [SerializeField] private bool _walkBobEnabled = true;
    [SerializeField] private float _walkBobAmplY = 0.008f;
    [SerializeField] private float _walkBobAmplX = 0.005f;
    [SerializeField] private float _walkBobFrequency = 2f;
    [SerializeField] private float _bobBlendSpeed = 5f;

    private float _bobTimer;
    private float _currentBobBlend;
    private Vector3 _bobOffset;
    private Vector3 _weaponOriginalPos;
    private Quaternion _weaponOriginalRot;

    // ============================================================
    // ПРОЦЕДУРНАЯ ПЕРЕЗАРЯДКА
    // ============================================================
    [Header("Процедурная перезарядка")]
    [SerializeField] private bool _proceduralReload = true;
    [SerializeField] private Vector3 _reloadDropOffset = new Vector3(0f, -0.12f, 0.05f);
    [SerializeField] private Vector3 _reloadTiltAngle = new Vector3(30f, 15f, 0f);
    [SerializeField, Range(0.1f, 0.9f)] private float _reloadDropPhase = 0.4f;

    private float _reloadTimer;
    private bool _inProceduralReload;
    private Vector3 _reloadPosOffset;
    private Vector3 _reloadRotOffset;

    // ============================================================
    // PARTICLE SYSTEMS
    // ============================================================
    [Header("Частицы")]
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private ParticleSystem _impactEffect;
    [SerializeField] private ParticleSystem _bloodEffect;
    [SerializeField] private ParticleSystem _shellEjectEffect;

    // ============================================================
    // ЗВУК
    // ============================================================
    [Header("Звуки")]
    [SerializeField] public AudioMixerGroup _audiomixer;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _shootSounds;
    [SerializeField] private AudioClip _impactSound;
    [SerializeField] private AudioClip _hitSound;
    [SerializeField] private AudioClip _reloadSound;
    [SerializeField] private AudioClip _emptySound;
    [SerializeField] private float _shootVolume = 1f;
    [SerializeField] private float _impactVolume = 0.7f;

    // ============================================================
    // ДЕКАЛИ
    // ============================================================
    [Header("Декали")]
    [SerializeField] private GameObject _bulletHoleDecal;
    [SerializeField] private float _decalLifetime = 10f;

    // ============================================================
    // DEBUG
    // ============================================================
    [Header("Отладка")]
    [SerializeField] private bool _showRayInScene = true;
    [SerializeField] private float _debugRayDuration = 0.5f;

    // ============================================================
    // UI ПАТРОНЫ
    // ============================================================
    [Header("UI Патроны")]
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private TextMeshProUGUI _ammoStateText;

    [Header("Цвета патронов")]
    [SerializeField] private Color _colorFull = Color.white;
    [SerializeField] private Color _colorMid = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color _colorLow = new Color(1f, 0.2f, 0.1f, 1f);
    [SerializeField] private Color _colorReload = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField, Range(0f, 1f)] private float _midThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _lowThreshold = 0.25f;

    [Header("Анимация цифр")]
    [SerializeField] private bool _blinkWhenLow = true;
    [SerializeField] private float _blinkSpeed = 3f;

    // ============================================================
    // STATE
    // ============================================================
    private float _nextFireTime;
    private bool _isReloading;
    private Camera _cam;

    private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

    // ============================================================
    // AWAKE
    // ============================================================
    private void Awake()
    {
        _cam = _camera != null ? _camera : Camera.main;

        // Ищем NetworkIdentity/Healt_mirror_Player владельца оружия (обычно
        // это родительский объект игрока). Это нужно, чтобы стрельба слала
        // Command именно от имени стрелка, а не через reflection на цели.
        _ownerIdentity = GetComponentInParent<NetworkIdentity>();
        if (_ownerIdentity != null)
            _ownerHealth = _ownerIdentity.GetComponent<Healt_mirror_Player>();

        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f;
            _audioSource.playOnAwake = false;
        }

        if (_muzzlePoint == null)
            _muzzlePoint = transform;

        _currentAmmo = Mathf.Clamp(_currentAmmo, 0, _magazineSize);

        if (_weaponTransform != null)
        {
            _weaponOriginalPos = _weaponTransform.localPosition;
            _weaponOriginalRot = _weaponTransform.localRotation;
        }

        if (_weaponAnimator == null && _weaponTransform != null)
            _weaponAnimator = _weaponTransform.GetComponentInChildren<Animator>();

        _hashShoot = Animator.StringToHash(_animShoot);
        _hashReload = Animator.StringToHash(_animReload);
        _hashAim = Animator.StringToHash(_animAim);
        _hashWalk = Animator.StringToHash(_animWalk);
        _hashSpeed = Animator.StringToHash(_animSpeed);
        _hashCharge = Animator.StringToHash(_animCharge);
        _hashOrbitTrap = Animator.StringToHash(orbitTrapAnimationTrigger);

        if (_cam != null)
        {
            _camOriginalLocalPos = _cam.transform.localPosition;
            _cameraOriginalLocalRotation = _cam.transform.localRotation;
        }

        if (_chargeSound != null || _fullyChargedSound != null)
        {
            GameObject obj = new GameObject("ChargeAudioSource");
            obj.transform.SetParent(transform);
            _chargeAudioSource = obj.AddComponent<AudioSource>();
            _chargeAudioSource.playOnAwake = false;
            _chargeAudioSource.spatialBlend = 0f;
        }

        InitHeatMaterial();
        UpdateAmmoUI();
        UpdateHeatUI();
    }

    // ============================================================
    // UPDATE
    // ============================================================
    private void Update()
    {
        // Стрелять (и, соответственно, отправлять урон на сервер) должен
        // только локальный владелец оружия. Без этой проверки Update() на
        // копиях этого же оружия, которые Mirror создаёт на экранах ДРУГИХ
        // клиентов, тоже читал бы Input и пытался бы стрелять — это ещё одна
        // возможная причина "урон проходит не так, как ожидалось".
        if (_ownerIdentity != null && !_ownerIdentity.isLocalPlayer)
            return;

        CleanupDestroyedTraps();

        if (_isReloading)
        {
            UpdateAmmoUI();
            UpdateCameraShake();
            UpdateMuzzleHeat();
            UpdateProceduralReload();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(Reload());
            return;
        }

        HandleCharging();

        bool fireInput = _autoFire
            ? Input.GetMouseButton(0)
            : Input.GetMouseButtonDown(0);

        if (fireInput && (pause == null || !pause.activeSelf) && !_isCharging)
        {
            if (_useMagazine && _currentAmmo <= 0)
            {
                PlaySound(_emptySound, _shootVolume * 0.5f);
            }
            else if (_overheatEnabled && _isOverheated)
            {
                // Перегрев блокирует выстрел.
            }
            else if (Time.time >= _nextFireTime)
            {
                Shoot(1f);
            }
        }

        if (_chargedShotEnabled)
        {
            if (Input.GetKeyDown(_chargeKey) && CanShoot() && !_isCharging)
                StartCharging();
            else if (Input.GetKeyUp(_chargeKey) && _isCharging)
                FireChargedShot();
        }

        if (Input.GetKeyDown(_orbitTrapKey) && CanShootOrbitTrap())
            ShootOrbitTrap();

        UpdateAmmoUI();
        UpdateRecoil();
        UpdateCameraShake();
        UpdateMuzzleHeat();
        HandleAiming();
        UpdateIdleAndWalkBob();
        UpdateProceduralReload();
    }

    // ============================================================
    // RAYCAST SHOOT
    // ============================================================
    private void Shoot(float damageMultiplier = 1f)
    {
        _nextFireTime = Time.time + (60f / Mathf.Max(1f, _fireRate));

        if (_useMagazine)
            _currentAmmo--;

        PlayParticle(_muzzleFlash);
        PlayParticle(_shellEjectEffect);
        TriggerAnimation(_hashShoot);

        ApplyRecoil(Mathf.Clamp(damageMultiplier, 1f, 10f));
        ApplyCameraShake();
        AddMuzzleHeat();

        if (_shootSounds != null && _shootSounds.Length > 0)
        {
            AudioClip clip = _shootSounds[Random.Range(0, _shootSounds.Length)];
            PlaySound(clip, _shootVolume, Random.Range(0.95f, 1.05f));
        }

        Ray ray = GetShootRay();

        if (_showRayInScene)
            Debug.DrawRay(ray.origin, ray.direction * _range, Color.red, _debugRayDuration);

        if (Physics.Raycast(ray, out RaycastHit hit, _range, _hitLayers, QueryTriggerInteraction.Ignore))
            ProcessHit(hit, _damage * damageMultiplier);
    }

    private Ray GetShootRay()
    {
        Ray baseRay = _cam != null
            ? _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(_muzzlePoint.position, _muzzlePoint.forward);

        if (!_useSpread || _cam == null)
            return baseRay;

        float halfAngle = _spreadAngle * 0.5f * Mathf.Deg2Rad;
        float radius = Mathf.Tan(halfAngle);
        Vector2 circle = Random.insideUnitCircle * radius;

        Vector3 spreadDir = baseRay.direction
                          + _cam.transform.right * circle.x
                          + _cam.transform.up * circle.y;

        return new Ray(baseRay.origin, spreadDir.normalized);
    }

    private void ProcessHit(RaycastHit hit, float damage)
    {
        bool isEnemy =
            hit.collider.CompareTag("Enemy") ||
            hit.collider.CompareTag("Player");

        if (isEnemy)
            SpawnParticleAtHit(_bloodEffect, hit);
        else
            SpawnParticleAtHit(_impactEffect, hit);

        PlaySound(isEnemy ? _hitSound : _impactSound, _impactVolume);

        // Декаль спавнится на поверхности объекта после выстрела всегда,
        // независимо от того, попали ли мы во врага/игрока или в обычную
        // поверхность (стену, ящик и т.д.). Префаб указывается в инспекторе
        // в поле _bulletHoleDecal.
        if (_bulletHoleDecal != null)
        {
            GameObject decal = Instantiate(
                _bulletHoleDecal,
                hit.point + hit.normal * 0.01f,
                Quaternion.LookRotation(-hit.normal));

            // Родитель — трансформ объекта, в который попали, чтобы декаль
            // "ехала" вместе с поверхностью (например, если та движется).
            decal.transform.SetParent(hit.transform, true);
            Destroy(decal, _decalLifetime);
        }

        ApplyDamageToHitObject(hit, damage);

        Rigidbody rb = hit.rigidbody;
        if (rb != null)
            rb.AddForceAtPosition(-hit.normal * _impactForce, hit.point, ForceMode.Impulse);
    }

    // ============================================================
    // CHARGED SHOT
    // ============================================================
    private void HandleCharging()
    {
        if (!_isCharging)
            return;

        _currentChargeTime += Time.deltaTime;

        float progress = Mathf.Clamp01(_currentChargeTime / Mathf.Max(0.01f, _chargeTime));
        float curveValue = Mathf.Clamp01(_chargeCurve.Evaluate(progress));

        _currentDamageMultiplier =
            Mathf.Lerp(1f, _maxDamageMultiplier, curveValue);

        if (progress >= 1f && !_isFullyCharged)
        {
            _isFullyCharged = true;
            OnFullyCharged();
        }

        UpdateChargeEffects(progress);
    }

    private void StartCharging()
    {
        if (!_chargedShotEnabled || !CanShoot())
            return;

        if (_useMagazine && _currentAmmo <= 0)
        {
            PlaySound(_emptySound, _shootVolume * 0.5f);
            return;
        }

        _isCharging = true;
        _currentChargeTime = 0f;
        _currentDamageMultiplier = 1f;
        _isFullyCharged = false;

        PlayParticle(_chargeEffect);
        TriggerAnimation(_hashCharge);

        if (_chargeAudioSource != null && _chargeSound != null)
        {
            _chargeAudioSource.clip = _chargeSound;
            _chargeAudioSource.loop = true;
            _chargeAudioSource.volume = 0.3f;
            _chargeAudioSource.pitch = 1f;
            _chargeAudioSource.Play();
        }
    }

    private void OnFullyCharged()
    {
        if (_chargeAudioSource != null && _fullyChargedSound != null)
        {
            _chargeAudioSource.Stop();
            _chargeAudioSource.PlayOneShot(_fullyChargedSound);
        }
    }

    private void UpdateChargeEffects(float progress)
    {
        if (_chargeEffect != null)
        {
            var emission = _chargeEffect.emission;
            emission.rateOverTime = 10f + progress * 40f;

            var main = _chargeEffect.main;
            main.startColor = Color.Lerp(Color.white, _chargeColor, progress);
        }

        if (_chargeAudioSource != null && _chargeAudioSource.isPlaying)
        {
            _chargeAudioSource.volume = 0.3f + progress * 0.7f;
            _chargeAudioSource.pitch = 1f + progress * 0.5f;
        }
    }

    private void FireChargedShot()
    {
        if (!_isCharging)
            return;

        if (_overheatEnabled && _isOverheated)
        {
            StopCharging();
            return;
        }

        Shoot(_currentDamageMultiplier);
        float usedMultiplier = _currentDamageMultiplier;

        StopCharging();

        Debug.Log($"Charged shot! Damage x{usedMultiplier:F1}");
    }

    private void StopCharging()
    {
        _isCharging = false;
        _currentChargeTime = 0f;
        _currentDamageMultiplier = 1f;
        _isFullyCharged = false;

        if (_chargeEffect != null)
            _chargeEffect.Stop();

        if (_chargeAudioSource != null)
            _chargeAudioSource.Stop();
    }

    // ============================================================
    // CAN SHOOT
    // ============================================================
    private bool CanShoot()
    {
        return Time.time >= _nextFireTime &&
               !_isReloading &&
               (!_overheatEnabled || !_isOverheated);
    }

    // ============================================================
    // ORBIT TRAP
    // ============================================================
    private bool CanShootOrbitTrap()
    {
        return Time.time >= nextOrbitTrapTime &&
               orbitTrapPrefab != null &&
               activeOrbitTraps.Count < maxOrbitTraps;
    }

    private void ShootOrbitTrap()
    {
        if (!CanShootOrbitTrap())
            return;

        GameObject trap = Instantiate(
            orbitTrapPrefab,
            _muzzlePoint.position,
            _muzzlePoint.rotation);

        activeOrbitTraps.Add(trap);

        if (trap.TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.velocity = _muzzlePoint.forward * orbitTrapSpeed;

        Destroy(trap, orbitTrapLifetime);

        PlayParticle(_muzzleFlash);

        if (orbitTrapShootSound != null)
            PlaySound(orbitTrapShootSound, _shootVolume);
        else if (_shootSounds != null && _shootSounds.Length > 0)
            PlaySound(_shootSounds[Random.Range(0, _shootSounds.Length)], _shootVolume);

        TriggerAnimation(_hashOrbitTrap);

        ApplyRecoil(1.5f);
        ApplyCameraShake();

        nextOrbitTrapTime = Time.time + orbitTrapFireRate;
    }

    private void CleanupDestroyedTraps()
    {
        for (int i = activeOrbitTraps.Count - 1; i >= 0; i--)
        {
            if (activeOrbitTraps[i] == null)
                activeOrbitTraps.RemoveAt(i);
        }
    }

    // ============================================================
    // RELOAD
    // ============================================================
    private IEnumerator Reload()
    {
        if (_isReloading)
            yield break;

        if (!_useMagazine || _currentAmmo >= _magazineSize)
            yield break;

        _isReloading = true;
        StopCharging();

        PlaySound(_reloadSound, _shootVolume);
        TriggerAnimation(_hashReload);

        if (_proceduralReload && _weaponTransform != null)
        {
            _reloadTimer = 0f;
            _inProceduralReload = true;
        }

        yield return new WaitForSeconds(_reloadTime);

        _inProceduralReload = false;
        _reloadPosOffset = Vector3.zero;
        _reloadRotOffset = Vector3.zero;
        _currentAmmo = _magazineSize;
        _isReloading = false;
    }

    // ============================================================
    // CAMERA SHAKE
    // ============================================================
    private void ApplyCameraShake()
    {
        if (!_cameraShakeEnabled || _cam == null)
            return;

        Vector3 impulse = new Vector3(
            Random.Range(-_shakeAmplitudeX, _shakeAmplitudeX),
            Random.Range(-_shakeAmplitudeY, _shakeAmplitudeY),
            Random.Range(-_shakeAmplitudeZ, _shakeAmplitudeZ));

        _targetShakeOffset = Vector3.Lerp(
            impulse,
            _targetShakeOffset + impulse,
            _shakeAccumulation);

        _shakeTimer = 0f;
    }

    private void UpdateCameraShake()
    {
        if (!_cameraShakeEnabled || _cam == null)
            return;

        _shakeTimer += Time.deltaTime * _shakeFrequency;

        float envelope = Mathf.Exp(-_shakeDamping * Time.deltaTime);
        _targetShakeOffset *= envelope;

        Vector3 shake =
            _targetShakeOffset * Mathf.Sin(_shakeTimer);

        _currentShakeOffset = Vector3.Lerp(
            _currentShakeOffset,
            shake,
            Time.deltaTime * _shakeDamping);

        _cam.transform.localPosition =
            _camOriginalLocalPos + _currentShakeOffset;
    }

    // ============================================================
    // НАГРЕВ
    // ============================================================
    private void InitHeatMaterial()
    {
        if (!_muzzleHeatEnabled)
            return;

        if (_muzzleRenderer == null && _weaponTransform != null)
            _muzzleRenderer = _weaponTransform.GetComponentInChildren<Renderer>();

        if (_muzzleRenderer == null)
            return;

        Material[] mats = _muzzleRenderer.materials;

        if (_muzzleMaterialIndex < 0 || _muzzleMaterialIndex >= mats.Length)
            return;

        if (!mats[_muzzleMaterialIndex].HasProperty(_shaderPropHeat))
            return;

        _heatMatInstance = new Material(mats[_muzzleMaterialIndex]);
        mats[_muzzleMaterialIndex] = _heatMatInstance;
        _muzzleRenderer.materials = mats;

        _heatMatInstance.SetFloat(_shaderPropHeat, 0f);
    }

    private void AddMuzzleHeat()
    {
        if (!_muzzleHeatEnabled || _heatMatInstance == null)
            return;

        _heatTarget =
            Mathf.Clamp(_heatTarget + _heatPerShot, 0f, _maxHeat);

        if (_overheatEnabled &&
            !_isOverheated &&
            _heatTarget >= _overheatThreshold)
        {
            _isOverheated = true;
            _overheatTimer = _overheatCooldownTime;
        }
    }

    private void UpdateMuzzleHeat()
    {
        if (!_muzzleHeatEnabled || _heatMatInstance == null)
            return;

        if (_isOverheated)
        {
            _overheatTimer -= Time.deltaTime;

            if (_overheatTimer <= 0f)
                _isOverheated = false;
        }

        _heatTarget =
            Mathf.MoveTowards(
                _heatTarget,
                0f,
                _cooldownRate * Time.deltaTime);

        _currentHeat =
            Mathf.Lerp(
                _currentHeat,
                _heatTarget,
                Time.deltaTime * _heatRiseSpeed);

        _heatMatInstance.SetFloat(
            _shaderPropHeat,
            _currentHeat);

        UpdateHeatUI();
    }

    private void UpdateHeatUI()
    {
        if (_heatText == null)
            return;

        if (_isOverheated)
        {
            _heatText.text = "ПЕРЕГРЕВ!";
            _heatText.color = Color.red;
            return;
        }

        if (_currentHeat < 0.01f)
        {
            _heatText.text = "";
            return;
        }

        float tempC = _currentHeat * _maxDisplayTemp;

        Color col = Color.Lerp(
            Color.Lerp(
                Color.green,
                Color.yellow,
                Mathf.Clamp01(_currentHeat * 2f)),
            Color.red,
            Mathf.Clamp01(_currentHeat * 2f - 1f));

        _heatText.text = $"{tempC:F0}°C";
        _heatText.color = col;
    }

    // ============================================================
    // PARTICLES / SOUND
    // ============================================================
    private void PlayParticle(ParticleSystem ps)
    {
        if (ps == null)
            return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }

    private void SpawnParticleAtHit(
        ParticleSystem prefab,
        RaycastHit hit)
    {
        if (prefab == null)
            return;

        ParticleSystem ps = Instantiate(
            prefab,
            hit.point,
            Quaternion.LookRotation(hit.normal));

        ps.Play();

        Destroy(
            ps.gameObject,
            ps.main.duration + ps.main.startLifetime.constantMax);
    }

    private void PlaySound(
        AudioClip clip,
        float volume,
        float pitch = 1f)
    {
        if (clip == null || _audioSource == null)
            return;

        _audioSource.pitch = pitch;
        _audioSource.outputAudioMixerGroup = _audiomixer;
        _audioSource.PlayOneShot(clip, volume);
    }

    // ============================================================
    // RECOIL
    // ============================================================
    private void ApplyRecoil(float multiplier = 1f)
    {
        if (!_recoilEnabled)
            return;

        _targetRecoil += new Vector3(
            -_recoilPitchAmount * multiplier,
            Random.Range(
                -_recoilYawAmount,
                _recoilYawAmount) * multiplier,
            0f);

        _targetRecoil.x =
            Mathf.Clamp(
                _targetRecoil.x,
                -_maxRecoilPitch,
                0f);

        if (_weaponTransform != null)
        {
            _weaponCurrentKick +=
                _weaponKickback * multiplier;

            _weaponCurrentRotKick +=
                _weaponKickRotation * multiplier;
        }
    }

    private void UpdateRecoil()
    {
        if (!_recoilEnabled)
            return;

        _currentRecoil = Vector3.Lerp(
            _currentRecoil,
            _targetRecoil,
            Time.deltaTime * _recoilSnappiness);

        _targetRecoil = Vector3.Lerp(
            _targetRecoil,
            Vector3.zero,
            Time.deltaTime * _recoilReturnSpeed);

        if (_cam != null)
        {
            Quaternion recoilRotation =
                Quaternion.Euler(
                    _currentRecoil.x,
                    _currentRecoil.y,
                    0f);

            _cam.transform.localRotation =
                _cameraOriginalLocalRotation * recoilRotation;
        }

        if (_weaponTransform != null)
        {
            _weaponCurrentKick = Vector3.Lerp(
                _weaponCurrentKick,
                Vector3.zero,
                Time.deltaTime * _weaponReturnSpeed);

            _weaponCurrentRotKick = Vector3.Lerp(
                _weaponCurrentRotKick,
                Vector3.zero,
                Time.deltaTime * _weaponReturnSpeed);
        }
    }

    // ============================================================
    // IDLE / WALK BOB
    // ============================================================
    private void UpdateIdleAndWalkBob()
    {
        if (_weaponTransform == null ||
            _isReloading ||
            _isAiming)
            return;

        float moveSpeed = 0f;

        CharacterController cc =
            GetComponentInParent<CharacterController>();

        if (cc != null)
        {
            moveSpeed =
                new Vector3(
                    cc.velocity.x,
                    0f,
                    cc.velocity.z).magnitude;
        }

        bool isWalking = moveSpeed > 0.1f;

        SafeSetBool(_hashWalk, isWalking);
        SafeSetFloat(_hashSpeed, moveSpeed);

        _currentBobBlend = Mathf.Lerp(
            _currentBobBlend,
            isWalking ? 1f : 0f,
            Time.deltaTime * _bobBlendSpeed);

        float idleOffsetY = 0f;
        float idleOffsetX = 0f;
        float idleSwayY = 0f;

        if (_idleBobEnabled)
        {
            float t = Time.time * _idleBobSpeed;

            idleOffsetY = Mathf.Sin(t) * _idleBobAmplY;
            idleOffsetX = Mathf.Sin(t * 0.5f) * _idleBobAmplX;
            idleSwayY = Mathf.Sin(t * 0.7f) * _idleSwayAmount;
        }

        float walkOffsetY = 0f;
        float walkOffsetX = 0f;

        if (_walkBobEnabled && isWalking)
        {
            _bobTimer +=
                Time.deltaTime *
                _walkBobFrequency *
                Mathf.PI * 2f;

            walkOffsetY =
                Mathf.Sin(_bobTimer) * _walkBobAmplY;

            walkOffsetX =
                Mathf.Sin(_bobTimer * 0.5f) * _walkBobAmplX;
        }
        else
        {
            _bobTimer =
                Mathf.Lerp(
                    _bobTimer,
                    0f,
                    Time.deltaTime * _bobBlendSpeed);
        }

        Vector3 totalBobPos = new Vector3(
            Mathf.Lerp(
                idleOffsetX,
                walkOffsetX,
                _currentBobBlend),
            Mathf.Lerp(
                idleOffsetY,
                walkOffsetY,
                _currentBobBlend),
            0f);

        float totalSwayY =
            Mathf.Lerp(
                idleSwayY,
                0f,
                _currentBobBlend);

        _bobOffset = Vector3.Lerp(
            _bobOffset,
            totalBobPos,
            Time.deltaTime * _bobBlendSpeed * 2f);

        _weaponTransform.localPosition =
            _weaponOriginalPos +
            _weaponCurrentKick +
            _reloadPosOffset +
            _bobOffset;

        _weaponTransform.localRotation =
            _weaponOriginalRot *
            Quaternion.Euler(_weaponCurrentRotKick) *
            Quaternion.Euler(_reloadRotOffset) *
            Quaternion.Euler(0f, totalSwayY, 0f);
    }

    // ============================================================
    // PROCEDURAL RELOAD
    // ============================================================
    private void UpdateProceduralReload()
    {
        if (!_inProceduralReload ||
            _weaponTransform == null)
            return;

        if (_reloadTime <= 0.01f)
        {
            _reloadTimer = 1f;
        }
        else
        {
            _reloadTimer +=
                Time.deltaTime / _reloadTime;
        }

        _reloadTimer =
            Mathf.Clamp01(_reloadTimer);

        if (_reloadTimer <= _reloadDropPhase)
        {
            float t = EaseInOut(
                _reloadTimer / _reloadDropPhase);

            _reloadPosOffset = Vector3.Lerp(
                Vector3.zero,
                _reloadDropOffset,
                t);

            _reloadRotOffset = Vector3.Lerp(
                Vector3.zero,
                _reloadTiltAngle,
                t);
        }
        else
        {
            float t = EaseInOut(
                (_reloadTimer - _reloadDropPhase) /
                (1f - _reloadDropPhase));

            _reloadPosOffset = Vector3.Lerp(
                _reloadDropOffset,
                Vector3.zero,
                t);

            _reloadRotOffset = Vector3.Lerp(
                _reloadTiltAngle,
                Vector3.zero,
                t);
        }
    }

    private static float EaseInOut(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    // ============================================================
    // ADS
    // ============================================================
    private void HandleAiming()
    {
        if (!_aimingEnabled ||
            _weaponTransform == null)
            return;

        bool aimInput = Input.GetMouseButton(1);

        if (aimInput != _isAiming)
        {
            _isAiming = aimInput;
            SafeSetBool(_hashAim, _isAiming);
        }

        if (_isAiming)
        {
            _weaponTransform.localPosition =
                Vector3.Lerp(
                    _weaponTransform.localPosition,
                    _adsPosition,
                    Time.deltaTime * _adsSpeed);

            _weaponTransform.localRotation =
                Quaternion.Slerp(
                    _weaponTransform.localRotation,
                    _weaponOriginalRot,
                    Time.deltaTime * _adsSpeed);
        }
    }

    // ============================================================
    // ANIMATOR HELPERS
    // ============================================================
    private void TriggerAnimation(int hash)
    {
        if (_weaponAnimator == null)
            return;

        foreach (AnimatorControllerParameter p
                 in _weaponAnimator.parameters)
        {
            if (p.nameHash == hash &&
                p.type == AnimatorControllerParameterType.Trigger)
            {
                _weaponAnimator.ResetTrigger(hash);
                _weaponAnimator.SetTrigger(hash);
                return;
            }
        }
    }

    private void SafeSetBool(int hash, bool value)
    {
        if (_weaponAnimator == null)
            return;

        foreach (AnimatorControllerParameter p
                 in _weaponAnimator.parameters)
        {
            if (p.nameHash == hash &&
                p.type == AnimatorControllerParameterType.Bool)
            {
                _weaponAnimator.SetBool(hash, value);
                return;
            }
        }
    }

    private void SafeSetFloat(int hash, float value)
    {
        if (_weaponAnimator == null)
            return;

        foreach (AnimatorControllerParameter p
                 in _weaponAnimator.parameters)
        {
            if (p.nameHash == hash &&
                p.type == AnimatorControllerParameterType.Float)
            {
                _weaponAnimator.SetFloat(hash, value);
                return;
            }
        }
    }

    // ============================================================
    // UI AMMO
    // ============================================================
    private void UpdateAmmoUI()
    {
        if (_ammoText != null)
        {
            if (!_useMagazine)
            {
                _ammoText.text = "∞";
                _ammoText.color = _colorFull;
            }
            else
            {
                _ammoText.text =
                    $"{_currentAmmo} <size=70%>/ {_magazineSize}</size>";

                float ratio =
                    _magazineSize <= 0
                        ? 0f
                        : (float)_currentAmmo / _magazineSize;

                Color targetColor =
                    _isReloading ? _colorReload :
                    ratio <= _lowThreshold ? _colorLow :
                    ratio <= _midThreshold ? _colorMid :
                    _colorFull;

                if (_blinkWhenLow &&
                    ratio <= _lowThreshold &&
                    !_isReloading)
                {
                    targetColor.a =
                        Mathf.Lerp(
                            0.3f,
                            1f,
                            Mathf.Abs(
                                Mathf.Sin(
                                    Time.time * _blinkSpeed)));
                }
                else
                {
                    targetColor.a = 1f;
                }

                _ammoText.color = targetColor;
            }
        }

        if (_ammoStateText == null)
            return;

        if (_isReloading)
        {
            _ammoStateText.gameObject.SetActive(true);
            _ammoStateText.text = "ПЕРЕЗАРЯДКА...";
            _ammoStateText.color = _colorReload;
        }
        else if (_overheatEnabled && _isOverheated)
        {
            _ammoStateText.gameObject.SetActive(true);
            _ammoStateText.text = "ПЕРЕГРЕВ!";
            _ammoStateText.color = _colorLow;
        }
        else if (_useMagazine && _currentAmmo <= 0)
        {
            _ammoStateText.gameObject.SetActive(true);
            _ammoStateText.text = "ПУСТО";
            _ammoStateText.color = _colorLow;
        }
        else
        {
            _ammoStateText.gameObject.SetActive(false);
        }
    }

    // ============================================================
    // PUBLIC API — AMMO
    // ============================================================
    public int CurrentAmmo => _currentAmmo;
    public int MagazineSize => _magazineSize;
    public bool IsReloading => _isReloading;

    public void AddAmmo(int amount)
    {
        if (!_useMagazine)
            return;

        _currentAmmo =
            Mathf.Clamp(
                _currentAmmo + amount,
                0,
                _magazineSize);

        UpdateAmmoUI();
    }

    public void ForceReload()
    {
        if (!_useMagazine || _currentAmmo >= _magazineSize)
            return;

        StartCoroutine(Reload());
    }

    // ============================================================
    // PUBLIC API — CHARGE
    // ============================================================
    public bool IsCharging => _isCharging;

    public float ChargeProgress =>
        _isCharging
            ? Mathf.Clamp01(
                _currentChargeTime /
                Mathf.Max(0.01f, _chargeTime))
            : 0f;

    public float CurrentDamageMultiplier =>
        _currentDamageMultiplier;

    public bool IsFullyCharged =>
        _isFullyCharged;

    public void SetChargedShotEnabled(bool enabled)
    {
        _chargedShotEnabled = enabled;

        if (!enabled)
            StopCharging();
    }

    public void SetChargeTime(float time)
    {
        _chargeTime = Mathf.Max(0.05f, time);
    }

    public void SetMaxDamageMultiplier(float multiplier)
    {
        _maxDamageMultiplier = Mathf.Max(1f, multiplier);
    }

    // ============================================================
    // PUBLIC API — ORBIT TRAPS
    // ============================================================
    public int ActiveOrbitTrapsCount =>
        activeOrbitTraps.Count;

    public int MaxOrbitTraps =>
        maxOrbitTraps;

    public bool CanShootMoreTraps =>
        activeOrbitTraps.Count < maxOrbitTraps;

    public void DestroyAllOrbitTraps()
    {
        foreach (GameObject trap in activeOrbitTraps)
        {
            if (trap != null)
                Destroy(trap);
        }

        activeOrbitTraps.Clear();
    }

    public void SetMaxOrbitTraps(int maxTraps)
    {
        maxOrbitTraps = Mathf.Max(1, maxTraps);

        while (activeOrbitTraps.Count > maxOrbitTraps)
        {
            GameObject oldest = activeOrbitTraps[0];
            activeOrbitTraps.RemoveAt(0);

            if (oldest != null)
                Destroy(oldest);
        }
    }

    public void SetOrbitTrapSpeed(float speed)
    {
        orbitTrapSpeed = Mathf.Max(1f, speed);
    }

    public void SetOrbitTrapFireRate(float rate)
    {
        orbitTrapFireRate = Mathf.Max(0.1f, rate);
    }

    // ============================================================
    // PUBLIC API — RECOIL
    // ============================================================
    public void SetRecoilEnabled(
        bool enabled,
        bool rotationEnabled = true)
    {
        _recoilEnabled = enabled;
    }

    public void SetPositionRecoil(
        float kickback,
        float kickUp,
        float kickSide)
    {
        _weaponKickback = new Vector3(
            kickSide,
            kickUp,
            -Mathf.Max(0f, kickback));
    }

    public void SetRotationRecoil(
        float pitch,
        float yaw,
        float roll)
    {
        _weaponKickRotation =
            new Vector3(
                pitch,
                yaw,
                roll);
    }

    public void SetRecoilReturnSpeed(
        float positionSpeed,
        float rotationSpeed)
    {
        _weaponReturnSpeed =
            Mathf.Max(0.1f, positionSpeed);

        _recoilReturnSpeed =
            Mathf.Max(0.1f, rotationSpeed);
    }

    public Vector3 CurrentRecoil =>
        _currentRecoil;

    // ============================================================
    // PUBLIC API — HEAT
    // ============================================================
    public float CurrentHeat =>
        _currentHeat;

    public bool IsOverheated =>
        _isOverheated;

    public float CurrentDamage => _damage;
    public void SetDamage(float damage) => _damage = Mathf.Max(0f, damage);
    public void SetFireRate(float rate) => _fireRate = Mathf.Max(1f, rate);
    public void SetRange(float range) => _range = Mathf.Max(0f, range);

    // ============================================================
    // GIZMOS
    // ============================================================
    private void OnDrawGizmosSelected()
    {
        if (_muzzlePoint == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            _muzzlePoint.position,
            0.05f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(
            _muzzlePoint.position,
            _muzzlePoint.forward * _range);
    }

    // ============================================================
    // DISABLE / DESTROY
    // ============================================================
    private void OnDisable()
    {
        StopAllCoroutines();

        _isReloading = false;
        _inProceduralReload = false;
        _reloadTimer = 0f;
        _reloadPosOffset = Vector3.zero;
        _reloadRotOffset = Vector3.zero;

        StopCharging();

        if (_cam != null)
        {
            _cam.transform.localPosition = _camOriginalLocalPos;
            _cam.transform.localRotation = _cameraOriginalLocalRotation;
            _currentShakeOffset = Vector3.zero;
            _targetShakeOffset = Vector3.zero;
        }

        if (_weaponTransform != null)
        {
            _weaponTransform.localPosition = _weaponOriginalPos;
            _weaponTransform.localRotation = _weaponOriginalRot;
        }
    }

    private void OnDestroy()
    {
        if (_heatMatInstance != null)
            Destroy(_heatMatInstance);
    }

    // ============================================================
    // CONTEXT MENU
    // ============================================================
    [ContextMenu("Test Charged Shot")]
    private void TestChargedShot()
    {
        Debug.Log(
            _chargedShotEnabled
                ? $"Charged shot ON. Time: {_chargeTime}s, max damage: x{_maxDamageMultiplier}"
                : "Charged shot is disabled");
    }

    [ContextMenu("Force Full Charge")]
    private void ForceFullCharge()
    {
        if (!_chargedShotEnabled || _isReloading)
            return;

        if (!_isCharging)
            StartCharging();

        _currentChargeTime = _chargeTime;
        _currentDamageMultiplier = _maxDamageMultiplier;
        _isFullyCharged = true;
    }

    [ContextMenu("Test Orbit Trap")]
    private void TestOrbitTrap()
    {
        Debug.Log(
            orbitTrapPrefab != null
                ? $"Orbit trap ready. Active: {activeOrbitTraps.Count}/{maxOrbitTraps}"
                : "Orbit trap prefab is not assigned!");
    }

    [ContextMenu("Force Shoot Orbit Trap")]
    private void ForceShootOrbitTrap()
    {
        ShootOrbitTrap();
    }

    [ContextMenu("Destroy All Orbit Traps")]
    private void ContextDestroyAllOrbitTraps()
    {
        DestroyAllOrbitTraps();
        Debug.Log("All orbit traps destroyed!");
    }

    [ContextMenu("Test Recoil")]
    private void TestRecoil()
    {
        ApplyRecoil();
    }

    [ContextMenu("Test Charged Recoil")]
    private void TestChargedRecoil()
    {
        ApplyRecoil(2.5f);
    }
}