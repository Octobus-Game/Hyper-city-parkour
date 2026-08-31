using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HIDEOBJECT : MonoBehaviour
{
    // Тэг объекта, с которым мы хотим столкнуться
    public string targetTag = "HIDE"; // замените на нужный тэг

    // Метод, вызываемый при столкновении с другим объектом
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, имеет ли другой объект нужный тэг
        if (other.CompareTag(targetTag))
        {
            // Делаем текущий объект невидимым
            GetComponent<Renderer>().enabled = false;
        }
    }
}
