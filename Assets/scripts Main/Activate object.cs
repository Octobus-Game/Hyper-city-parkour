
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TriggerObjectActivator : MonoBehaviour
{
    [Header("Объекты для активации")]
    [SerializeField] private GameObject[] objectsToActivate;

    [Header("Объекты для отключения")]
    [SerializeField] private GameObject[] objectsToDeactivate; // Новый массив для отключения

    [Header("Настройки системы")]
    [SerializeField] private bool activateOnce = true; // Активировать только один раз
    [SerializeField] private bool deactivateOnExit = false; // Деактивировать при выходе из триггера
    [SerializeField] private float activationDelay = 0f; // Задержка активации в секундах
    [SerializeField] private float deactivationDelay = 0f; // Задержка деактивации в секундах
    [SerializeField] private bool randomOrder = false; // Активировать в случайном порядке
    [SerializeField] private float intervalBetweenActivations = 0.1f; // Интервал между активациями объектов

    [Header("Фильтр тегов")]
    [SerializeField] private bool useTagFilter = false;
    [SerializeField] private string requiredTag = "Player"; // Только объекты с этим тегом могут активировать

    [Header("Визуальные эффекты")]
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private ParticleSystem activationEffect; // Эффект частиц при активации
    [SerializeField] private AudioSource audioSource; // Звук при активации
    [SerializeField] private AudioClip activationSound;

    [Header("Skybox управление")]
    [SerializeField] private bool changeSkybox = false; // Включить переключение skybox
    [SerializeField] private Material newSkybox; // Новый skybox для активации
    [SerializeField] private Material exitSkybox; // Skybox для выхода из триггера (если null, возвращается исходный)
    [SerializeField] private bool smoothSkyboxTransition = false; // Плавный переход между skybox
    [SerializeField] private float skyboxTransitionDuration = 2f; // Продолжительность плавного перехода
    [SerializeField] private AnimationCurve skyboxTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Кривая перехода
    [SerializeField] private bool resetSkyboxOnDestruction = true; // Сбрасывать skybox при уничтожении контрольных объектов

    [Header("Контроль уничтожения")]
    [SerializeField] private GameObject[] destructionControlObjects; // Массив объектов для отслеживания уничтожения
    [SerializeField] private bool disableOnDestruction = true; // Отключать первый массив при уничтожении контрольных объектов
    [SerializeField] private bool checkAllDestroyed = true; // true = все должны быть уничтожены, false = достаточно одного
    [SerializeField] private float destructionCheckInterval = 1f; // Интервал проверки уничтожения в секундах
    [SerializeField] private bool showDestructionDebug = true; // Показывать отладку уничтожения

    [Header("Состояние системы")]
    [SerializeField] private bool isEnabled = true; // Включена ли система
    [SerializeField] private bool showGizmos = true; // Показывать гизмо триггера

    // Внутренние переменные
    private bool hasBeenActivated = false;
    private bool isCurrentlyActivating = false;
    private List<GameObject> originalActiveStates = new List<GameObject>();
    private List<GameObject> originalDeactiveStates = new List<GameObject>(); // Для сохранения состояний объектов отключения
    private bool isCheckingDestruction = false;
    private Coroutine destructionCheckCoroutine;

    // Skybox переменные
    private Material originalSkybox;
    private bool isSkyboxTransitioning = false;
    private Coroutine skyboxTransitionCoroutine;

    private void Start()
    {
        // Сохраняем исходные состояния объектов
        SaveOriginalStates();

        // Сохраняем исходный skybox
        if (changeSkybox)
        {
            SaveOriginalSkybox();
        }

        // Проверяем наличие Collider с триггером
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"TriggerObjectActivator на {gameObject.name}: отсутствует Collider компонент!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"TriggerObjectActivator на {gameObject.name}: Collider должен быть триггером!");
        }

        // Настройка аудио источника
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Начинаем отслеживание уничтожения объектов
        if (disableOnDestruction && destructionControlObjects != null && destructionControlObjects.Length > 0)
        {
            StartDestructionCheck();
        }
    }

    private void SaveOriginalStates()
    {
        originalActiveStates.Clear();
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
                originalActiveStates.Add(obj);
        }

        // Сохраняем исходные состояния объектов для отключения
        originalDeactiveStates.Clear();
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
                originalDeactiveStates.Add(obj);
        }
    }

    private void SaveOriginalSkybox()
    {
        originalSkybox = RenderSettings.skybox;
        if (showDebugMessages && originalSkybox != null)
            Debug.Log($"Сохранен исходный skybox: {originalSkybox.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isEnabled) return;
        if (activateOnce && hasBeenActivated) return;
        if (isCurrentlyActivating) return;

        // Проверка тега
        if (useTagFilter && !other.CompareTag(requiredTag))
            return;

        if (showDebugMessages)
            Debug.Log($"Триггер активирован объектом: {other.name}");

        StartCoroutine(ActivateObjects());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isEnabled) return;
        if (!deactivateOnExit) return;

        // Проверка тега
        if (useTagFilter && !other.CompareTag(requiredTag))
            return;

        if (showDebugMessages)
            Debug.Log($"Выход из триггера: {other.name}");

        StartCoroutine(DeactivateObjects());
    }

    private IEnumerator ActivateObjects()
    {
        isCurrentlyActivating = true;

        // Задержка активации
        if (activationDelay > 0)
            yield return new WaitForSeconds(activationDelay);

        // Создаем список индексов для активации
        List<int> activateIndices = new List<int>();
        for (int i = 0; i < objectsToActivate.Length; i++)
        {
            if (objectsToActivate[i] != null)
                activateIndices.Add(i);
        }

        // Создаем список индексов для отключения
        List<int> deactivateIndices = new List<int>();
        for (int i = 0; i < objectsToDeactivate.Length; i++)
        {
            if (objectsToDeactivate[i] != null)
                deactivateIndices.Add(i);
        }

        // Перемешиваем, если нужен случайный порядок
        if (randomOrder)
        {
            // Перемешиваем индексы для активации
            for (int i = 0; i < activateIndices.Count; i++)
            {
                int randomIndex = Random.Range(i, activateIndices.Count);
                int temp = activateIndices[i];
                activateIndices[i] = activateIndices[randomIndex];
                activateIndices[randomIndex] = temp;
            }

            // Перемешиваем индексы для отключения
            for (int i = 0; i < deactivateIndices.Count; i++)
            {
                int randomIndex = Random.Range(i, deactivateIndices.Count);
                int temp = deactivateIndices[i];
                deactivateIndices[i] = deactivateIndices[randomIndex];
                deactivateIndices[randomIndex] = temp;
            }
        }

        // Определяем максимальное количество операций
        int maxOperations = Mathf.Max(activateIndices.Count, deactivateIndices.Count);

        // Активируем и отключаем объекты одновременно
        for (int i = 0; i < maxOperations; i++)
        {
            // Активируем объект, если есть
            if (i < activateIndices.Count)
            {
                int activateIndex = activateIndices[i];
                if (objectsToActivate[activateIndex] != null)
                {
                    objectsToActivate[activateIndex].SetActive(true);

                    if (showDebugMessages)
                        Debug.Log($"Активирован объект: {objectsToActivate[activateIndex].name}");
                }
            }

            // Отключаем объект, если есть
            if (i < deactivateIndices.Count)
            {
                int deactivateIndex = deactivateIndices[i];
                if (objectsToDeactivate[deactivateIndex] != null)
                {
                    objectsToDeactivate[deactivateIndex].SetActive(false);

                    if (showDebugMessages)
                        Debug.Log($"Отключен объект: {objectsToDeactivate[deactivateIndex].name}");
                }
            }

            // Интервал между операциями
            if (intervalBetweenActivations > 0 && i < maxOperations - 1)
                yield return new WaitForSeconds(intervalBetweenActivations);
        }

        // Переключаем skybox
        if (changeSkybox && newSkybox != null)
        {
            ChangeSkybox(newSkybox);
        }

        // Воспроизводим эффекты
        PlayActivationEffects();

        hasBeenActivated = true;
        isCurrentlyActivating = false;
    }

    private IEnumerator DeactivateObjects()
    {
        // Задержка деактивации
        if (deactivationDelay > 0)
            yield return new WaitForSeconds(deactivationDelay);

        int maxOperations = Mathf.Max(objectsToActivate.Length, objectsToDeactivate.Length);

        for (int i = 0; i < maxOperations; i++)
        {
            // Отключаем объекты из массива активации
            if (i < objectsToActivate.Length && objectsToActivate[i] != null)
            {
                objectsToActivate[i].SetActive(false);

                if (showDebugMessages)
                    Debug.Log($"Деактивирован объект: {objectsToActivate[i].name}");
            }

            // Включаем обратно объекты из массива отключения
            if (i < objectsToDeactivate.Length && objectsToDeactivate[i] != null)
            {
                objectsToDeactivate[i].SetActive(true);

                if (showDebugMessages)
                    Debug.Log($"Активирован обратно объект: {objectsToDeactivate[i].name}");
            }

            // Интервал между деактивациями
            if (intervalBetweenActivations > 0 && i < maxOperations - 1)
                yield return new WaitForSeconds(intervalBetweenActivations);
        }

        // Возвращаем skybox при выходе
        if (changeSkybox && deactivateOnExit)
        {
            Material targetSkybox = exitSkybox != null ? exitSkybox : originalSkybox;
            if (targetSkybox != null)
            {
                ChangeSkybox(targetSkybox);
            }
        }
    }

    private void ChangeSkybox(Material targetSkybox)
    {
        if (isSkyboxTransitioning)
        {
            StopCoroutine(skyboxTransitionCoroutine);
            isSkyboxTransitioning = false;
        }

        if (smoothSkyboxTransition && skyboxTransitionDuration > 0)
        {
            skyboxTransitionCoroutine = StartCoroutine(SmoothSkyboxTransition(targetSkybox));
        }
        else
        {
            RenderSettings.skybox = targetSkybox;
            DynamicGI.UpdateEnvironment();

            if (showDebugMessages)
                Debug.Log($"Skybox изменен на: {targetSkybox.name}");
        }
    }

    private IEnumerator SmoothSkyboxTransition(Material targetSkybox)
    {
        isSkyboxTransitioning = true;
        Material startSkybox = RenderSettings.skybox;

        if (startSkybox == targetSkybox)
        {
            isSkyboxTransitioning = false;
            yield break;
        }

        // Создаем временный материал для блендинга
        Material blendMaterial = new Material(Shader.Find("Skybox/Blend"));
        if (blendMaterial == null)
        {
            // Если шейдер блендинга недоступен, просто сменим skybox мгновенно
            RenderSettings.skybox = targetSkybox;
            DynamicGI.UpdateEnvironment();
            isSkyboxTransitioning = false;
            yield break;
        }

        blendMaterial.SetTexture("_MainTex", startSkybox.GetTexture("_MainTex"));
        blendMaterial.SetTexture("_BlendTex", targetSkybox.GetTexture("_MainTex"));

        float elapsedTime = 0f;

        while (elapsedTime < skyboxTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = skyboxTransitionCurve.Evaluate(elapsedTime / skyboxTransitionDuration);

            blendMaterial.SetFloat("_Blend", progress);
            RenderSettings.skybox = blendMaterial;
            DynamicGI.UpdateEnvironment();

            yield return null;
        }

        // Устанавливаем финальный skybox
        RenderSettings.skybox = targetSkybox;
        DynamicGI.UpdateEnvironment();

        // Очищаем временный материал
        DestroyImmediate(blendMaterial);

        isSkyboxTransitioning = false;

        if (showDebugMessages)
            Debug.Log($"Плавный переход skybox завершен на: {targetSkybox.name}");
    }

    private void PlayActivationEffects()
    {
        // Эффект частиц
        if (activationEffect != null)
            activationEffect.Play();

        // Звуковой эффект
        if (audioSource != null && activationSound != null)
        {
            audioSource.clip = activationSound;
            audioSource.Play();
        }
    }

    // Методы для отслеживания уничтожения объектов
    private void StartDestructionCheck()
    {
        if (!isCheckingDestruction)
        {
            isCheckingDestruction = true;
            destructionCheckCoroutine = StartCoroutine(CheckDestructionLoop());

            if (showDestructionDebug)
                Debug.Log("Начато отслеживание уничтожения контрольных объектов");
        }
    }

    private void StopDestructionCheck()
    {
        if (isCheckingDestruction)
        {
            isCheckingDestruction = false;
            if (destructionCheckCoroutine != null)
            {
                StopCoroutine(destructionCheckCoroutine);
                destructionCheckCoroutine = null;
            }

            if (showDestructionDebug)
                Debug.Log("Остановлено отслеживание уничтожения контрольных объектов");
        }
    }

    private IEnumerator CheckDestructionLoop()
    {
        while (isCheckingDestruction && disableOnDestruction)
        {
            yield return new WaitForSeconds(destructionCheckInterval);

            if (CheckDestructionCondition())
            {
                OnDestructionConditionMet();
                break;
            }
        }
    }

    private bool CheckDestructionCondition()
    {
        if (destructionControlObjects == null || destructionControlObjects.Length == 0)
            return false;

        int destroyedCount = 0;
        int totalCount = destructionControlObjects.Length;

        foreach (GameObject obj in destructionControlObjects)
        {
            if (obj == null) // null означает, что объект был уничтожен
                destroyedCount++;
        }

        if (showDestructionDebug && destroyedCount > 0)
        {
            Debug.Log($"Уничтожено контрольных объектов: {destroyedCount}/{totalCount}");
        }

        // Проверяем условие в зависимости от настроек
        if (checkAllDestroyed)
        {
            return destroyedCount == totalCount; // Все объекты должны быть уничтожены
        }
        else
        {
            return destroyedCount > 0; // Достаточно уничтожить один объект
        }
    }

    private void OnDestructionConditionMet()
    {
        if (showDestructionDebug)
        {
            string condition = checkAllDestroyed ? "все контрольные объекты" : "один из контрольных объектов";
            Debug.Log($"Условие выполнено: {condition} уничтожены. Отключаем основные массивы объектов.");
        }

        // Отключаем все объекты из обоих массивов
        StartCoroutine(DeactivateObjectsOnDestruction());

        // Сбрасываем skybox при уничтожении контрольных объектов
        if (changeSkybox && resetSkyboxOnDestruction && originalSkybox != null)
        {
            ChangeSkybox(originalSkybox);
        }

        // Отключаем систему
        DisableSystem();

        // Останавливаем дальнейшую проверку
        StopDestructionCheck();
    }

    private IEnumerator DeactivateObjectsOnDestruction()
    {
        int maxOperations = Mathf.Max(objectsToActivate.Length, objectsToDeactivate.Length);

        for (int i = 0; i < maxOperations; i++)
        {
            // Отключаем объекты из массива активации
            if (i < objectsToActivate.Length && objectsToActivate[i] != null)
            {
                objectsToActivate[i].SetActive(false);

                if (showDestructionDebug)
                    Debug.Log($"Отключен объект из-за уничтожения контрольных объектов: {objectsToActivate[i].name}");
            }

            // Отключаем объекты из массива отключения (они могли быть включены обратно при выходе из триггера)
            if (i < objectsToDeactivate.Length && objectsToDeactivate[i] != null)
            {
                objectsToDeactivate[i].SetActive(false);

                if (showDestructionDebug)
                    Debug.Log($"Отключен объект отключения из-за уничтожения контрольных объектов: {objectsToDeactivate[i].name}");
            }

            // Небольшая задержка между отключениями для визуального эффекта
            if (intervalBetweenActivations > 0 && i < maxOperations - 1)
                yield return new WaitForSeconds(intervalBetweenActivations);
        }
    }

    // Публичные методы для управления системой
    public void EnableSystem()
    {
        isEnabled = true;
        if (showDebugMessages)
            Debug.Log("Система активации включена");

        // Возобновляем отслеживание уничтожения если нужно
        if (disableOnDestruction && destructionControlObjects != null && destructionControlObjects.Length > 0)
        {
            StartDestructionCheck();
        }
    }

    public void DisableSystem()
    {
        isEnabled = false;
        if (showDebugMessages)
            Debug.Log("Система активации отключена");

        // Останавливаем отслеживание уничтожения
        StopDestructionCheck();
    }

    public void ResetSystem()
    {
        hasBeenActivated = false;
        isCurrentlyActivating = false;
        if (showDebugMessages)
            Debug.Log("Система активации сброшена");

        // Перезапускаем отслеживание уничтожения
        StopDestructionCheck();
        if (disableOnDestruction && destructionControlObjects != null && destructionControlObjects.Length > 0)
        {
            StartDestructionCheck();
        }
    }

    public void ForceActivate()
    {
        if (!isCurrentlyActivating)
            StartCoroutine(ActivateObjects());
    }

    public void ForceDeactivate()
    {
        StartCoroutine(DeactivateObjects());
    }

    public void AddObjectToArray(GameObject obj)
    {
        if (obj != null)
        {
            System.Array.Resize(ref objectsToActivate, objectsToActivate.Length + 1);
            objectsToActivate[objectsToActivate.Length - 1] = obj;
        }
    }

    public void RemoveObjectFromArray(GameObject obj)
    {
        List<GameObject> tempList = new List<GameObject>(objectsToActivate);
        tempList.Remove(obj);
        objectsToActivate = tempList.ToArray();
    }

    // Новые методы для управления массивом объектов отключения
    public void AddObjectToDeactivateArray(GameObject obj)
    {
        if (obj != null)
        {
            System.Array.Resize(ref objectsToDeactivate, objectsToDeactivate.Length + 1);
            objectsToDeactivate[objectsToDeactivate.Length - 1] = obj;
        }
    }

    public void RemoveObjectFromDeactivateArray(GameObject obj)
    {
        List<GameObject> tempList = new List<GameObject>(objectsToDeactivate);
        tempList.Remove(obj);
        objectsToDeactivate = tempList.ToArray();
    }

    // Методы управления массивом контрольных объектов
    public void AddDestructionControlObject(GameObject obj)
    {
        if (obj != null)
        {
            System.Array.Resize(ref destructionControlObjects, destructionControlObjects.Length + 1);
            destructionControlObjects[destructionControlObjects.Length - 1] = obj;

            // Перезапускаем отслеживание с новым объектом
            if (disableOnDestruction && isEnabled)
            {
                StopDestructionCheck();
                StartDestructionCheck();
            }
        }
    }

    public void RemoveDestructionControlObject(GameObject obj)
    {
        List<GameObject> tempList = new List<GameObject>(destructionControlObjects);
        tempList.Remove(obj);
        destructionControlObjects = tempList.ToArray();

        // Перезапускаем отслеживание без удаленного объекта
        if (disableOnDestruction && isEnabled)
        {
            StopDestructionCheck();
            if (destructionControlObjects.Length > 0)
                StartDestructionCheck();
        }
    }

    public void ManualDestructionCheck()
    {
        if (CheckDestructionCondition())
        {
            OnDestructionConditionMet();
        }
    }

    // Новые методы для управления Skybox
    public void SetNewSkybox(Material skybox)
    {
        newSkybox = skybox;
    }

    public void SetExitSkybox(Material skybox)
    {
        exitSkybox = skybox;
    }

    public void ForceSkyboxChange(Material skybox)
    {
        if (skybox != null)
            ChangeSkybox(skybox);
    }

    public void RestoreOriginalSkybox()
    {
        if (originalSkybox != null)
            ChangeSkybox(originalSkybox);
    }

    public Material GetCurrentSkybox()
    {
        return RenderSettings.skybox;
    }

    public Material GetOriginalSkybox()
    {
        return originalSkybox;
    }

    // Методы для получения информации о состоянии
    public int GetDestroyedControlObjectsCount()
    {
        if (destructionControlObjects == null) return 0;

        int count = 0;
        foreach (GameObject obj in destructionControlObjects)
        {
            if (obj == null) count++;
        }
        return count;
    }

    public bool AreAllControlObjectsDestroyed()
    {
        if (destructionControlObjects == null || destructionControlObjects.Length == 0) return false;

        foreach (GameObject obj in destructionControlObjects)
        {
            if (obj != null) return false;
        }
        return true;
    }

    private void OnDestroy()
    {
        // Останавливаем все корутины при уничтожении объекта
        StopDestructionCheck();

        // Останавливаем переход skybox
        if (isSkyboxTransitioning && skyboxTransitionCoroutine != null)
        {
            StopCoroutine(skyboxTransitionCoroutine);
        }

        // Возвращаем исходный skybox при уничтожении компонента
        if (changeSkybox && originalSkybox != null && resetSkyboxOnDestruction)
        {
            RenderSettings.skybox = originalSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }

    // Гизмо для визуализации триггера в редакторе
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = isEnabled ? Color.green : Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider)
            {
                BoxCollider boxCol = col as BoxCollider;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphereCol = col as SphereCollider;
                Gizmos.DrawWireSphere(sphereCol.center, sphereCol.radius);
            }
        }
    }
}