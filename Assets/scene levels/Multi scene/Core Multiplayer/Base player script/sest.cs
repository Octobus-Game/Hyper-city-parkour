using UnityEngine;

namespace MIROR_sidenie
{
    public class sest : MonoBehaviour
    {
        [Tooltip("Точка назначения для телепортации игрока")]
        public Transform teleportTarget;

        [Tooltip("Камера, которая активируется при телепортации")] 
        public GameObject activateCamera;

        [Tooltip("Объект, который можно вращать по Y после телепортации")] 
        public GameObject rotatableObject;

        private bool playerInTrigger = false;
        private GameObject player;
        private float? frozenYRotation = null;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInTrigger = true;
                player = other.gameObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInTrigger = false;
                player = null;
            }
        }

        private void Update()
        {
            if (playerInTrigger && player != null && Input.GetKeyDown(KeyCode.T))
            {
                if (teleportTarget != null)
                {
                    player.transform.position = teleportTarget.position;
                    var cc = player.GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        cc.enabled = false;
                    }
                    if (activateCamera != null)
                    {
                        activateCamera.SetActive(true);
                    }
                    // Замораживаем Y вращение
                    frozenYRotation = player.transform.eulerAngles.y;
                }
                else
                {
                    Debug.LogWarning("Не указана точка назначения для телепортации!");
                }
            }
            // Если заморожено вращение Y, поддерживаем его
            if (frozenYRotation.HasValue && player != null)
            {
                var euler = player.transform.eulerAngles;
                euler.y = frozenYRotation.Value;
                player.transform.eulerAngles = euler;
            }
            // Вращение объекта и игрока по Y при нажатии A/D
            if (rotatableObject != null && frozenYRotation.HasValue)
            {
                float rotateSpeed = 100f; // скорость вращения
                float input = 0f;
                if (Input.GetKey(KeyCode.A)) input -= 1f;
                if (Input.GetKey(KeyCode.D)) input += 1f;
                if (Mathf.Abs(input) > 0.01f)
                {
                    rotatableObject.transform.Rotate(0f, input * rotateSpeed * Time.deltaTime, 0f);
                    if (player != null)
                    {
                        player.transform.Rotate(0f, input * rotateSpeed * Time.deltaTime, 0f);
                        // обновляем замороженный угол Y, чтобы не "откатывало" назад
                        frozenYRotation = player.transform.eulerAngles.y;
                    }
                }
            }
        }
    }
}
