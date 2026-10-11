using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TELEPORT23 : MonoBehaviour
{
    // Объект, к которому будет телепортироваться при столкновении
    [SerializeField] private Transform targetLocation;

    private void OnCollisionEnter(Collision collision)
    {
        // Проверяем, установлен ли целевой объект для телепортации
        if (targetLocation != null)
        {
            // Телепортируем объект, с которым произошло столкновение
            collision.transform.position = targetLocation.position;
            collision.transform.rotation = targetLocation.rotation;
        }
        else
        {
            Debug.LogWarning("Target location is not set in the inspector!");
        }
    }
}
