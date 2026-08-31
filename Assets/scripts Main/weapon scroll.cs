
using UnityEngine;
using UnityEngine.UI;

public class WeaponScroll : MonoBehaviour
{
    [Header("Настройки оружия")]
    [SerializeField] private GameObject[] weapons;  // Массив объектов оружия
    [SerializeField] private int currentWeaponIndex = 0;

    [Header("UI - Массив иконок оружия")]
    [SerializeField] private Text weaponNameText;  // Текст для отображения названия (опционально)
    [SerializeField] private Image[] weaponIcons;  // Массив иконок оружия (соответствует массиву weapons)
    [SerializeField] private Color activeIconColor = Color.white;    // Цвет активной иконки
    [SerializeField] private Color inactiveIconColor = Color.gray;   // Цвет неактивных иконок
    [SerializeField] private float activeIconScale = 1.2f;          // Масштаб активной иконки
    [SerializeField] private float inactiveIconScale = 0.8f;        // Масштаб неактивных иконок

    [Header("Настройки")]
    [SerializeField] private float scrollSensitivity = 1f;
    [SerializeField] private bool enableKeyboardControl = true;

    [Header("Звуковые эффекты")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchSound;
    [SerializeField] private float soundVolume = 0.5f;

    private void Start()
    {
        // Инициализация - показываем только текущее оружие
        UpdateWeaponDisplay();

        // Инициализация AudioSource если не назначен
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // Создаем AudioSource автоматически
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D звук
            }
        }
    }

    private void Update()
    {
        // Обработка прокрутки колесика мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            // Прокрутка вверх - предыдущее оружие
            SwitchWeapon(-1);
        }
        else if (scroll < 0f)
        {
            // Прокрутка вниз - следующее оружие
            SwitchWeapon(1);
        }

        // Дополнительное управление клавишами (опционально)
        if (enableKeyboardControl)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                SwitchWeapon(-1);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                SwitchWeapon(1);
            }
        }
    }

    /// <summary>
    /// Переключение оружия
    /// </summary>
    /// <param name="direction">Направление: 1 = вперед, -1 = назад</param>
    private void SwitchWeapon(int direction)
    {
        if (weapons == null || weapons.Length == 0) return;

        // Деактивируем текущее оружие
        if (weapons[currentWeaponIndex] != null)
        {
            weapons[currentWeaponIndex].SetActive(false);
        }

        // Вычисляем новый индекс с зацикливанием
        currentWeaponIndex += direction;

        if (currentWeaponIndex >= weapons.Length)
        {
            currentWeaponIndex = 0;
        }
        else if (currentWeaponIndex < 0)
        {
            currentWeaponIndex = weapons.Length - 1;
        }

        // Обновляем отображение
        UpdateWeaponDisplay();
    }

    /// <summary>
    /// Обновление отображения текущего оружия
    /// </summary>
    private void UpdateWeaponDisplay()
    {
        if (weapons == null || weapons.Length == 0) return;

        // Деактивируем все оружие
        foreach (GameObject weapon in weapons)
        {
            if (weapon != null)
            {
                weapon.SetActive(false);
            }
        }

        // Активируем текущее оружие
        if (weapons[currentWeaponIndex] != null)
        {
            weapons[currentWeaponIndex].SetActive(true);
        }

        // Обновляем UI (если назначены элементы)
        UpdateUI();

        // Вызываем событие смены оружия (для других скриптов)
        OnWeaponChanged();
    }

    /// <summary>
    /// Обновление пользовательского интерфейса
    /// </summary>
    private void UpdateUI()
    {
        // Обновляем текст названия оружия
        if (weaponNameText != null && weapons[currentWeaponIndex] != null)
        {
            weaponNameText.text = weapons[currentWeaponIndex].name;
        }

        // Обновляем иконки оружия
        UpdateWeaponIcons();
    }

    /// <summary>
    /// Обновление иконок оружия в UI
    /// </summary>
    private void UpdateWeaponIcons()
    {
        if (weaponIcons == null || weaponIcons.Length == 0) return;

        // Проходим по всем иконкам
        for (int i = 0; i < weaponIcons.Length; i++)
        {
            if (weaponIcons[i] != null)
            {
                // Проверяем, соответствует ли индекс иконки текущему оружию
                if (i == currentWeaponIndex && i < weapons.Length)
                {
                    // Активная иконка - делаем её более заметной
                    weaponIcons[i].color = activeIconColor;
                    weaponIcons[i].transform.localScale = Vector3.one * activeIconScale;

                    // Включаем иконку
                    weaponIcons[i].gameObject.SetActive(true);
                }
                else if (i < weapons.Length)
                {
                    // Неактивная иконка - делаем её тусклой
                    weaponIcons[i].color = inactiveIconColor;
                    weaponIcons[i].transform.localScale = Vector3.one * inactiveIconScale;

                    // Включаем иконку
                    weaponIcons[i].gameObject.SetActive(true);
                }
                else
                {
                    // Если иконок больше чем оружия - отключаем лишние
                    weaponIcons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Событие смены оружия - может быть использовано другими скриптами
    /// </summary>
    private void OnWeaponChanged()
    {
        Debug.Log($"Switched to weapon: {weapons[currentWeaponIndex].name}");

        // Воспроизводим звук переключения
        PlaySwitchSound();

        // Здесь можно добавить дополнительную логику:
        // - Анимации
        // - Обновление статистик оружия
        // - Уведомления других компонентов
    }

    /// <summary>
    /// Воспроизведение звука переключения оружия
    /// </summary>
    private void PlaySwitchSound()
    {
        if (audioSource != null && switchSound != null)
        {
            audioSource.clip = switchSound;
            audioSource.volume = soundVolume;
            audioSource.pitch = Random.Range(0.9f, 1.1f); // Небольшая вариация тона
            audioSource.Play();
        }
    }

    /// <summary>
    /// Получить текущее активное оружие
    /// </summary>
    public GameObject GetCurrentWeapon()
    {
        if (weapons != null && currentWeaponIndex < weapons.Length)
        {
            return weapons[currentWeaponIndex];
        }
        return null;
    }

    /// <summary>
    /// Получить индекс текущего оружия
    /// </summary>
    public int GetCurrentWeaponIndex()
    {
        return currentWeaponIndex;
    }

    /// <summary>
    /// Установить конкретное оружие по индексу
    /// </summary>
    public void SetWeapon(int index)
    {
        if (weapons != null && index >= 0 && index < weapons.Length)
        {
            currentWeaponIndex = index;
            UpdateWeaponDisplay();
        }
    }
}

// Дополнительный скрипт для более сложной системы оружия с массивом UI
[System.Serializable]
public class WeaponData
{
    public string weaponName;
    public GameObject weaponPrefab;
    public Sprite weaponIcon;
    public int damage;
    public float fireRate;
    public string description;
}

public class AdvancedWeaponScroll : MonoBehaviour
{
    [Header("Система оружия")]
    [SerializeField] private WeaponData[] weaponDatabase;
    [SerializeField] private Transform weaponHolder;  // Родительский объект для оружия
    [SerializeField] private int currentWeaponIndex = 0;

    [Header("UI элементы")]
    [SerializeField] private Text weaponNameUI;
    [SerializeField] private Text weaponStatsUI;
    [SerializeField] private Image[] weaponIconsUI;  // Массив UI иконок
    [SerializeField] private Color activeUIColor = Color.white;
    [SerializeField] private Color inactiveUIColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

    [Header("Анимация UI")]
    [SerializeField] private float iconAnimationSpeed = 5f;
    [SerializeField] private float activeScale = 1.3f;
    [SerializeField] private float inactiveScale = 0.9f;

    [Header("Звуковые эффекты")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] switchSounds; // Массив звуков для разнообразия
    [SerializeField] private float soundVolume = 0.7f;

    private GameObject currentWeaponInstance;

    private void Start()
    {
        InitializeWeaponIcons();
        SpawnCurrentWeapon();
        InitializeAudio();
    }

    private void InitializeWeaponIcons()
    {
        // Инициализируем иконки оружия в UI
        if (weaponIconsUI != null && weaponDatabase != null)
        {
            for (int i = 0; i < weaponIconsUI.Length && i < weaponDatabase.Length; i++)
            {
                if (weaponIconsUI[i] != null && weaponDatabase[i].weaponIcon != null)
                {
                    weaponIconsUI[i].sprite = weaponDatabase[i].weaponIcon;
                }
            }
        }

        UpdateWeaponIconsUI();
    }

    private void InitializeAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            SwitchToWeapon(GetPreviousWeaponIndex());
        }
        else if (scroll < 0f)
        {
            SwitchToWeapon(GetNextWeaponIndex());
        }
    }

    private void SwitchToWeapon(int newIndex)
    {
        if (newIndex == currentWeaponIndex) return;

        // Уничтожаем старое оружие
        if (currentWeaponInstance != null)
        {
            DestroyImmediate(currentWeaponInstance);
        }

        currentWeaponIndex = newIndex;
        SpawnCurrentWeapon();
        UpdateUI();
    }

    private void SpawnCurrentWeapon()
    {
        if (weaponDatabase[currentWeaponIndex].weaponPrefab != null)
        {
            currentWeaponInstance = Instantiate(
                weaponDatabase[currentWeaponIndex].weaponPrefab,
                weaponHolder
            );
        }
    }

    private void UpdateUI()
    {
        WeaponData current = weaponDatabase[currentWeaponIndex];

        if (weaponNameUI != null)
            weaponNameUI.text = current.weaponName;

        if (weaponStatsUI != null)
            weaponStatsUI.text = $"Урон: {current.damage}\nСкорострельность: {current.fireRate}";

        // Обновляем иконки оружия в UI
        UpdateWeaponIconsUI();

        // Воспроизводим звук переключения
        PlayRandomSwitchSound();
    }

    /// <summary>
    /// Обновление состояния иконок оружия в UI
    /// </summary>
    private void UpdateWeaponIconsUI()
    {
        if (weaponIconsUI == null) return;

        for (int i = 0; i < weaponIconsUI.Length; i++)
        {
            if (weaponIconsUI[i] != null)
            {
                // Проверяем, соответствует ли индекс иконки текущему оружию
                if (i == currentWeaponIndex && i < weaponDatabase.Length)
                {
                    // Активная иконка
                    weaponIconsUI[i].color = activeUIColor;
                    weaponIconsUI[i].transform.localScale = Vector3.one * activeScale;
                    weaponIconsUI[i].gameObject.SetActive(true);
                }
                else if (i < weaponDatabase.Length)
                {
                    // Неактивная иконка
                    weaponIconsUI[i].color = inactiveUIColor;
                    weaponIconsUI[i].transform.localScale = Vector3.one * inactiveScale;
                    weaponIconsUI[i].gameObject.SetActive(true);
                }
                else
                {
                    // Лишние иконки отключаем
                    weaponIconsUI[i].gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Воспроизведение случайного звука переключения
    /// </summary>
    private void PlayRandomSwitchSound()
    {
        if (audioSource != null && switchSounds != null && switchSounds.Length > 0)
        {
            // Выбираем случайный звук из массива
            AudioClip randomSound = switchSounds[Random.Range(0, switchSounds.Length)];
            if (randomSound != null)
            {
                audioSource.clip = randomSound;
                audioSource.volume = soundVolume;
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.Play();
            }
        }
    }

    private int GetNextWeaponIndex()
    {
        return (currentWeaponIndex + 1) % weaponDatabase.Length;
    }

    private int GetPreviousWeaponIndex()
    {
        return (currentWeaponIndex - 1 + weaponDatabase.Length) % weaponDatabase.Length;
    }
}