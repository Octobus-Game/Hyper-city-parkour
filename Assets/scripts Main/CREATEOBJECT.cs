using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CREATEOBJECT : MonoBehaviour
{
    public GameObject objectPrefab; // Префаб добавляемого объекта (например, куб)
    public Transform objectsContainer; // Контейнер для хранения объектов
    public Button addButton; // Кнопка для добавления объекта
    public Button raiseButton; // Кнопка для поднятия объекта
    public float raiseHeight = 5.0f; // Высота подъема объекта
    public float spawnDistance = 5.0f; // Расстояние спавна от камеры

    private GameObject lastSpawnedObject;

    void Start()
    {
        // Привязка методов к событиям нажатия кнопок
        addButton.onClick.AddListener(AddObject);
        raiseButton.onClick.AddListener(RaiseObject);
    }

    void AddObject()
    {
        // Создание нового объекта из префаба
        GameObject newObject = Instantiate(objectPrefab, objectsContainer);

        // Установка позиции объекта перед камерой
        newObject.transform.position = GetSpawnPosition();

        // Добавление компонента для удаления объекта при клике
        newObject.AddComponent<ClickableObject>();

        // Устанавливаем новый объект как последний заспавненный
        lastSpawnedObject = newObject;
    }

    Vector3 GetSpawnPosition()
    {
        // Спавн объекта на определённом расстоянии перед камерой
        Camera mainCamera = Camera.main;
        Vector3 spawnPosition = mainCamera.transform.position + mainCamera.transform.forward * spawnDistance;
        return spawnPosition;
    }

    void RaiseObject()
    {
        if (lastSpawnedObject != null)
        {
            // Поднимаем объект на указанную высоту
            lastSpawnedObject.transform.position += new Vector3(0, raiseHeight, 0);
        }
    }
}

public class ClickableObject : MonoBehaviour
{
    void OnMouseDown()
    {
        // Удаление объекта при нажатии на него
        Destroy(gameObject);
    }
}
