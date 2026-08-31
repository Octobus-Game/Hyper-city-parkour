using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player1 : NetworkBehaviour //даем системе понять, что это сетевой объект
{
    void Update()
    {
        if (isOwned) //проверяем, есть ли у нас права изменять этот объект
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float speed = 5f * Time.deltaTime;
            transform.Translate(new Vector2(h * speed, v * speed)); //делаем простейшее движение
        }
    }
}
