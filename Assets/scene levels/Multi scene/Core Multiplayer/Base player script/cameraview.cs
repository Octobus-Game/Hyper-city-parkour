
using System.Collections.Generic;
using UnityEngine;
using Mirror;


namespace mirror_camera_view
{
    public class cameraview : MonoBehaviour
    {
        public GameObject weaponPrefab;
        private GameObject weaponInstance;

        // Удалено "override", так как метод Player_01() не переопределяет метод базового класса  
        public void Player_01()
        {
            // Найти камеру игрока  
            var cameraTransform = Camera.main.transform;

            // Создать объект и прикрепить к камере  
            weaponInstance = Instantiate(weaponPrefab, cameraTransform);
            weaponInstance.transform.localPosition = new Vector3(0.5f, -0.5f, 1.2f);
        }
    }
}
