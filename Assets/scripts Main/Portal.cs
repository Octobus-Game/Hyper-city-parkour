using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Точка выхода")]
    public Transform exitPoint;

    [Header("Тег игрока")]
    public string playerTag = "Player";

    [Header("Настройки кулдауна")]
    public bool useCooldown = true;
    public float cooldownTime = 1f;

    private bool isOnCooldown = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!isOnCooldown && other.CompareTag(playerTag))
        {
            CharacterController controller = other.GetComponent<CharacterController>();
            if (controller != null)
            {
                // Временно отключить CharacterController
                controller.enabled = false;
            }

            // Телепортировать точно в позицию exitPoint
            other.transform.position = exitPoint.position;
            other.transform.rotation = exitPoint.rotation;

            if (controller != null)
            {
                // Снова включить CharacterController
                controller.enabled = true;
            }

            if (useCooldown)
                StartCoroutine(StartCooldown());
        }
    }

    private System.Collections.IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }
}
