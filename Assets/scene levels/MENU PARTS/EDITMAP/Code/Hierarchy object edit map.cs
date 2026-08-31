using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Базовый класс для всех спавнящихся объектов
public class SpawnableObject : MonoBehaviour
{
    [SerializeField] protected string objectName = "Базовый объект";
    [SerializeField] protected string description = "Описание объекта";
    [SerializeField] protected Sprite icon;

    public string ObjectName => objectName;
    public string Description => description;
    public Sprite Icon => icon;

    // Виртуальный метод для инициализации объекта
    public virtual void Initialize()
    {
        Debug.Log($"Инициализирован базовый объект: {objectName}");
    }

    // Виртуальный метод для уничтожения объекта
    public virtual void DestroyObject()
    {
        Destroy(gameObject);
    }
}

// Конкретные классы объектов
public class Warrior : SpawnableObject
{
    [SerializeField] private int health = 100;
    [SerializeField] private int damage = 25;

    private void Awake()
    {
        objectName = "Воин";
        description = "Сильный боец ближнего боя";
    }

    public override void Initialize()
    {
        Debug.Log($"Создан {objectName} с {health} HP и {damage} урона");
        // Здесь можно добавить логику инициализации воина
    }
}

public class Mage : SpawnableObject
{
    [SerializeField] private int mana = 150;
    [SerializeField] private int spellPower = 35;

    private void Awake()
    {
        objectName = "Маг";
        description = "Магический боец дальнего боя";
    }

    public override void Initialize()
    {
        Debug.Log($"Создан {objectName} с {mana} маны и {spellPower} силы заклинаний");
        // Здесь можно добавить логику инициализации мага
    }
}

public class Archer : SpawnableObject
{
    [SerializeField] private int arrows = 30;
    [SerializeField] private float accuracy = 0.85f;

    private void Awake()
    {
        objectName = "Лучник";
        description = "Меткий стрелок";
    }

    public override void Initialize()
    {
        Debug.Log($"Создан {objectName} с {arrows} стрелами и {accuracy * 100}% точностью");
        // Здесь можно добавить логику инициализации лучника
    }
}

// Структура данных для хранения информации о префабе
[System.Serializable]
public class SpawnableObjectData
{
    public string name;
    public GameObject prefab;
    public Sprite buttonIcon;
    public int cost = 10;

    public SpawnableObjectData(string name, GameObject prefab, Sprite icon, int cost = 10)
    {
        this.name = name;
        this.prefab = prefab;
        this.buttonIcon = icon;
        this.cost = cost;
    }
}

// Менеджер спавна объектов
public class ObjectSpawnManager : MonoBehaviour
{
    [Header("Настройки спавна")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform uiParent; // Родительский объект для кнопок в UI
    [SerializeField] private GameObject buttonPrefab; // Префаб кнопки

    [Header("Доступные объекты для спавна")]
    [SerializeField] private SpawnableObjectData[] availableObjects;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<Button> spawnButtons = new List<Button>();

    private void Start()
    {
        CreateSpawnButtons();
    }

    // Создание кнопок для каждого доступного объекта
    private void CreateSpawnButtons()
    {
        foreach (var objectData in availableObjects)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, uiParent);
            Button button = buttonObj.GetComponent<Button>();

            // Настройка текста кнопки
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = $"{objectData.name}\n({objectData.cost} монет)";
            }

            // Настройка иконки кнопки (если есть Image компонент)
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null && objectData.buttonIcon != null)
            {
                buttonImage.sprite = objectData.buttonIcon;
            }

            // Добавление слушателя события
            int index = System.Array.IndexOf(availableObjects, objectData);
            button.onClick.AddListener(() => SpawnObject(index));

            spawnButtons.Add(button);
        }
    }

    // Спавн объекта по индексу
    public void SpawnObject(int index)
    {
        if (index < 0 || index >= availableObjects.Length)
        {
            Debug.LogError("Неверный индекс объекта для спавна!");
            return;
        }

        SpawnableObjectData objectData = availableObjects[index];

        // Проверка наличия префаба
        if (objectData.prefab == null)
        {
            Debug.LogError($"Префаб для {objectData.name} не назначен!");
            return;
        }

        // Создание объекта
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject spawnedObj = Instantiate(objectData.prefab, spawnPosition, Quaternion.identity);

        // Инициализация объекта
        SpawnableObject spawnableComponent = spawnedObj.GetComponent<SpawnableObject>();
        if (spawnableComponent != null)
        {
            spawnableComponent.Initialize();
        }

        spawnedObjects.Add(spawnedObj);

        Debug.Log($"Заспавнен объект: {objectData.name}");
    }

    // Очистка всех заспавненных объектов
    public void ClearAllSpawnedObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
    }

    // Получение количества заспавненных объектов определенного типа
    public int GetSpawnedObjectCount<T>() where T : SpawnableObject
    {
        int count = 0;
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null && obj.GetComponent<T>() != null)
            {
                count++;
            }
        }
        return count;
    }
}

// Дополнительный класс для управления UI
public class SpawnUI : MonoBehaviour
{
    [SerializeField] private ObjectSpawnManager spawnManager;
    [SerializeField] private Text infoText;
    [SerializeField] private Button clearAllButton;

    private void Start()
    {
        if (clearAllButton != null)
        {
            clearAllButton.onClick.AddListener(() => spawnManager.ClearAllSpawnedObjects());
        }
    }

    private void Update()
    {
        UpdateInfoText();
    }

    private void UpdateInfoText()
    {
        if (infoText != null && spawnManager != null)
        {
            int warriors = spawnManager.GetSpawnedObjectCount<Warrior>();
            int mages = spawnManager.GetSpawnedObjectCount<Mage>();
            int archers = spawnManager.GetSpawnedObjectCount<Archer>();

            infoText.text = $"Заспавнено:\nВоинов: {warriors}\nМагов: {mages}\nЛучников: {archers}";
        }
    }
}