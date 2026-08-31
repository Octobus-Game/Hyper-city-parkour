using UnityEngine;

namespace Labber_
{
    

    public class LadderController : MonoBehaviour
    {
        [Header("Настройки лестницы")]
        public float climbSpeed = 3f;           // Скорость подъема/спуска
        public float horizontalSpeed = 2f;      // Горизонтальная скорость на лестнице
        public LayerMask ladderLayer = 1;       // Слой лестницы

        [Header("Настройки обнаружения")]
        public float ladderCheckDistance = 0.5f; // Дистанция проверки лестницы
        public Transform ladderCheckPoint;       // Точка для проверки лестницы (обычно в центре персонажа)

        private CharacterController controller;
        private Vector3 moveDirection;
        private bool isOnLadder = false;
        private bool canClimb = false;
        private float gravity = -9.81f;
        private float verticalVelocity = 0f;

        [Header("Настройки управления")]
        public KeyCode climbKey = KeyCode.E;    // Клавиша для начала лазания

        void Start()
        {
            controller = GetComponent<CharacterController>();

            // Если точка проверки не назначена, создаем её в центре персонажа
            if (ladderCheckPoint == null)
            {
                GameObject checkPoint = new GameObject("LadderCheckPoint");
                checkPoint.transform.SetParent(transform);
                checkPoint.transform.localPosition = Vector3.zero;
                ladderCheckPoint = checkPoint.transform;
            }
        }

        void Update()
        {
            CheckForLadder();
            HandleLadderInput();
            HandleMovement();
        }

        void CheckForLadder()
        {
            // Проверяем наличие лестницы перед персонажем
            RaycastHit hit;
            Vector3 rayOrigin = ladderCheckPoint.position;
            Vector3 rayDirection = transform.forward;

            canClimb = Physics.Raycast(rayOrigin, rayDirection, out hit, ladderCheckDistance, ladderLayer);

            // Визуализация луча в редакторе
            Debug.DrawRay(rayOrigin, rayDirection * ladderCheckDistance, canClimb ? Color.green : Color.red);
        }

        void HandleLadderInput()
        {
            // Начинаем/прекращаем лазание по лестнице
            if (canClimb && Input.GetKeyDown(climbKey))
            {
                isOnLadder = !isOnLadder;

                if (isOnLadder)
                {
                    verticalVelocity = 0f; // Сбрасываем вертикальную скорость при начале лазания
                }
            }

            // Автоматически слезаем с лестницы, если она недоступна
            if (isOnLadder && !canClimb)
            {
                isOnLadder = false;
            }
        }

        void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (isOnLadder)
            {
                // Движение по лестнице
                HandleLadderMovement(horizontal, vertical);
            }
            else
            {
                // Обычное движение
                HandleNormalMovement(horizontal, vertical);
            }

            // Применяем движение
            controller.Move(moveDirection * Time.deltaTime);
        }

        void HandleLadderMovement(float horizontal, float vertical)
        {
            // Вертикальное движение (вверх/вниз по лестнице)
            Vector3 climbDirection = Vector3.up * vertical * climbSpeed;

            // Горизонтальное движение (влево/вправо на лестнице)
            Vector3 horizontalDirection = transform.right * horizontal * horizontalSpeed;

            // Комбинируем направления
            moveDirection = climbDirection + horizontalDirection;

            // На лестнице гравитация не действует
            verticalVelocity = 0f;
        }

        void HandleNormalMovement(float horizontal, float vertical)
        {
            // Обычное горизонтальное движение
            Vector3 horizontalMovement = (transform.right * horizontal + transform.forward * vertical) * horizontalSpeed;

            // Применяем гравитацию
            if (controller.isGrounded)
            {
                verticalVelocity = 0f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            // Комбинируем горизонтальное движение и гравитацию
            moveDirection = horizontalMovement + Vector3.up * verticalVelocity;
        }

        // Вспомогательные методы для внешних скриптов
        public bool IsOnLadder()
        {
            return isOnLadder;
        }

        public bool CanClimb()
        {
            return canClimb;
        }

        // Визуализация в редакторе
        void OnDrawGizmosSelected()
        {
            if (ladderCheckPoint != null)
            {
                Gizmos.color = canClimb ? Color.green : Color.red;
                Gizmos.DrawRay(ladderCheckPoint.position, transform.forward * ladderCheckDistance);
            }
        }
    }

    // Дополнительный скрипт для объектов лестницы
    [System.Serializable]
    public class Ladder : MonoBehaviour
    {
        [Header("Параметры лестницы")]
        public Transform topPoint;      // Верхняя точка лестницы
        public Transform bottomPoint;   // Нижняя точка лестницы
        public bool allowExit = true;   // Разрешить выход с лестницы

        void Start()
        {
            // Убеждаемся, что объект находится на правильном слое
            gameObject.layer = LayerMask.NameToLayer("Ladder");
        }

        void OnTriggerEnter(Collider other)
        {
            LadderController ladderController = other.GetComponent<LadderController>();
            if (ladderController != null)
            {
                // Можно добавить дополнительную логику при входе в зону лестницы
                Debug.Log("Игрок рядом с лестницей");
            }
        }

        void OnTriggerExit(Collider other)
        {
            LadderController ladderController = other.GetComponent<LadderController>();
            if (ladderController != null && allowExit)
            {
                // Автоматически слезаем с лестницы при выходе из зоны
                Debug.Log("Игрок покинул зону лестницы");
            }
        }
    }
}