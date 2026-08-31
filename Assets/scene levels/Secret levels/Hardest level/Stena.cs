using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stena : MonoBehaviour
{
    public GameObject targetObject; // Объект, который исчезнет
    private ulong pressCount = 0;
    private ulong targetCount = 1844674407370955161; /* yanderedev moment. Кстати, походу это единственная переменная которая делает игру 64-битной */

    private ulong littleBuff1 = 0;
    private ulong targetBuff1 = 1844674407370955161;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.D) ||
            Input.GetMouseButtonDown(0) || // Левая кнопка мыши
            Input.GetMouseButtonDown(1))   // Правая кнопка мыши
        {
            pressCount++;

            if (pressCount >= targetCount && targetObject != null)
            {
                pressCount = pressCount - pressCount;
                targetCount = targetCount - targetCount;
                littleBuff1++;


            }



            if (littleBuff1 >= targetBuff1)
                {
                    /*   pressCount = pressCount - pressCount;
                       targetObject.SetActive(false);
                       Debug.Log("Поздравляю ебать"); */

                    littleBuff1 = littleBuff1 - littleBuff1;
                    targetBuff1 = targetBuff1 - targetBuff1;
                littleBuff1++;
    
            }

        }
    }
}
