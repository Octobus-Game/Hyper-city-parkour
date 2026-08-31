using UnityEngine;

public class DestroyOnContact : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Массив объектов, которые будут отключаться")]
    public GameObject[] destroyObjects;

    [Tooltip("Объект, который вызывает отключение (можно указать тег)")]
    public string targetTag = "Player";

    [Tooltip("Или конкретный объект")]
    public GameObject targetObject;

    [Header("Опции")]
    [Tooltip("Удалять объекты вместо отключения")]
    public bool destroyInsteadOfDisable = false;

    [Tooltip("Задержка перед отключением")]
    public float delay = 0f;

    private void OnCollisionEnter(Collision collision)
    {
        CheckAndDestroyObjects(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckAndDestroyObjects(other.gameObject);
    }

    private void CheckAndDestroyObjects(GameObject collidedObject)
    {
        // Проверяем по тегу или конкретному объекту
        bool isTargetObject = false;

        if (!string.IsNullOrEmpty(targetTag) && collidedObject.CompareTag(targetTag))
        {
            isTargetObject = true;
        }

        if (targetObject != null && collidedObject == targetObject)
        {
            isTargetObject = true;
        }

        // Если это нужный объект, отключаем все объекты из массива
        if (isTargetObject)
        {
            if (delay > 0)
            {
                Invoke(nameof(ProcessDestroyObjects), delay);
            }
            else
            {
                ProcessDestroyObjects();
            }
        }
    }

    private void ProcessDestroyObjects()
    {
        foreach (GameObject obj in destroyObjects)
        {
            if (obj != null)
            {
                if (destroyInsteadOfDisable)
                {
                    Destroy(obj);
                }
                else
                {
                    obj.SetActive(false);
                }
            }
        }
    }
}