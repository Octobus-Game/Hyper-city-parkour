using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MOVE : MonoBehaviour
{
    // Кнопка для поднятия объекта
    public Button raiseButton;

    // Высота на которую нужно поднять объект
    public float raiseHeight = 5.0f;

    // Последний заспавненный объект
    private GameObject lastSpawnedObject;

    private void Start()
    {
        // Привязываем метод RaiseObject к событию нажатия кнопки
        raiseButton.onClick.AddListener(RaiseObject);
    }

    // Метод для поднятия объекта
    private void RaiseObject()
    {
        if (lastSpawnedObject != null)
        {
            // Поднимаем объект на указанную высоту
            lastSpawnedObject.transform.position += new Vector3(0, raiseHeight, 0);
        }
    }

    // Метод для установки последнего заспавненного объекта
    public void SetLastSpawnedObject(GameObject spawnedObject)
    {
        lastSpawnedObject = spawnedObject;
    }
}

