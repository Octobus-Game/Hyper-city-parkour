using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TRIGGERFALSE : MonoBehaviour
{
    // Укажи объект, с которым будет проверяться столкновение
    public GameObject targetObject;

    // Метод срабатывает при входе в коллизию
    private void OnCollisionEnter(Collision collision)
    {
        // Проверяем, что объект, с которым столкнулись, это указанный объект
        if (collision.gameObject == targetObject)
        {
            // Делаем текущий объект невидимым, отключая рендерер
            GetComponent<Renderer>().enabled = false;
        }
    }
}





