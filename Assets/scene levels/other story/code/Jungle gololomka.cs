
using UnityEngine;

public class MouseClickRotationHandler : MonoBehaviour
{
    [Header("Объекты для поворота")]
    public GameObject[] rotatingObjects = new GameObject[6];

    [Header("Объекты с источниками света")]
    public GameObject[] lightObjects;

    [Header("Материалы")]
    public Material normalMaterial;
    public Material activeMaterial;

    [Header("Настройки поворота")]
    public float rotationSpeed = 90f; // градусов в секунду

    private bool[] isRotating = new bool[6];
    private float[] targetRotation = new float[6];
    private float[] currentRotation = new float[6];

    void Start()
    {
        // Инициализация начальных значений поворота
        for (int i = 0; i < rotatingObjects.Length; i++)
        {
            if (rotatingObjects[i] != null)
            {
                currentRotation[i] = rotatingObjects[i].transform.rotation.eulerAngles.x;
                targetRotation[i] = currentRotation[i];
            }
        }
    }

    void Update()
    {
        // Проверка нажатия левой кнопки мыши
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }

        // Обновление поворота объектов
        UpdateObjectRotations();

        // Проверка и обновление материалов
        CheckAndUpdateMaterials();
    }

    void HandleMouseClick()
    {
        // Начинаем поворот всех 6 объектов
        for (int i = 0; i < rotatingObjects.Length; i++)
        {
            if (rotatingObjects[i] != null && !isRotating[i])
            {
                isRotating[i] = true;
                targetRotation[i] = -90f;
            }
        }
    }

    void UpdateObjectRotations()
    {
        for (int i = 0; i < rotatingObjects.Length; i++)
        {
            if (rotatingObjects[i] != null && isRotating[i])
            {
                // Плавный поворот к целевому углу
                currentRotation[i] = Mathf.MoveTowards(
                    currentRotation[i],
                    targetRotation[i],
                    rotationSpeed * Time.deltaTime
                );

                // Применяем поворот
                Vector3 newRotation = rotatingObjects[i].transform.rotation.eulerAngles;
                newRotation.x = currentRotation[i];
                rotatingObjects[i].transform.rotation = Quaternion.Euler(newRotation);

                // Проверяем, достигли ли целевого угла
                if (Mathf.Approximately(currentRotation[i], targetRotation[i]))
                {
                    isRotating[i] = false;
                }
            }
        }
    }

    void CheckAndUpdateMaterials()
    {
        // Проверяем, все ли объекты имеют X rotation = 0
        bool allObjectsRotated = true;

        for (int i = 0; i < rotatingObjects.Length; i++)
        {
            if (rotatingObjects[i] != null)
            {
                float xRotation = rotatingObjects[i].transform.rotation.eulerAngles.x;

                // Нормализуем угол
                if (xRotation > 180f)
                    xRotation -= 360f;

                if (!Mathf.Approximately(xRotation, 0f))
                {
                    allObjectsRotated = false;
                    break;
                }
            }
        }

        // Если все объекты повернуты на X = 0, меняем материалы у lightObjects
        if (allObjectsRotated)
        {
            ChangeLightObjectsMaterials();
        }
    }

    void ChangeLightObjectsMaterials()
    {
        if (lightObjects == null || activeMaterial == null)
            return;

        for (int i = 0; i < lightObjects.Length; i++)
        {
            if (lightObjects[i] != null)
            {
                Renderer renderer = lightObjects[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = activeMaterial;

                    // Опционально: включаем/выключаем источник света
                    Light light = lightObjects[i].GetComponent<Light>();
                    if (light != null)
                    {
                        light.enabled = true;
                    }
                }
            }
        }
    }

    // Метод для сброса состояния (опционально)
    [ContextMenu("Reset Objects")]
    public void ResetObjects()
    {
        for (int i = 0; i < rotatingObjects.Length; i++)
        {
            if (rotatingObjects[i] != null)
            {
                rotatingObjects[i].transform.rotation = Quaternion.identity;
                currentRotation[i] = 0f;
                targetRotation[i] = 0f;
                isRotating[i] = false;
            }
        }

        // Возвращаем исходные материалы
        if (lightObjects != null && normalMaterial != null)
        {
            for (int i = 0; i < lightObjects.Length; i++)
            {
                if (lightObjects[i] != null)
                {
                    Renderer renderer = lightObjects[i].GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = normalMaterial;
                    }

                    Light light = lightObjects[i].GetComponent<Light>();
                    if (light != null)
                    {
                        light.enabled = false;
                    }
                }
            }
        }
    }
}