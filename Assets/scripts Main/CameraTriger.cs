using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTriger : MonoBehaviour
{

    // Метод, который вызывается при столкновении с другим объектом
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли объект, с которым произошло столкновение, персонажем
        if (other.CompareTag("Player"))
        {
            // Вызов метода для открытия камеры
            OpenCamera();
        }
    }

    // Метод для открытия камеры (требует реализации)
    private void OpenCamera()
    {
#if UNITY_ANDROID
            // Код для открытия камеры на Android
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject cameraIntent = new AndroidJavaObject("android.content.Intent", "android.media.action.IMAGE_CAPTURE");
            currentActivity.Call("startActivity", cameraIntent);
#elif UNITY_IOS
            // Код для открытия камеры на iOS (нужен плагин или родной код)
#endif
    }
}
