using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tornado : MonoBehaviour
{
    public Transform suctionPoint; // Точка, куда будет засасываться игрок
    public float suctionForce = 10f; // Сила засасывания
    public float holdDuration = 2f; // Время удержания внутри
    public float throwForce = 15f; // Сила, с которой игрок будет выброшен
    public Vector3 throwDirection = Vector3.up; // Направление выброса

    private bool isPlayerInside = false; // Флаг, находится ли игрок внутри триггера
    private GameObject player; // Ссылка на игрока
    private Rigidbody playerRb; // Rigidbody игрока

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPlayerInside)
        {
            player = other.gameObject;
            playerRb = player.GetComponent<Rigidbody>();

            if (playerRb != null)
            {
                StartCoroutine(SuctionSequence());
            }
        }
    }

    private System.Collections.IEnumerator SuctionSequence()
    {
        isPlayerInside = true;

        // Засасывание игрока
        while (Vector3.Distance(player.transform.position, suctionPoint.position) > 0.1f)
        {
            Vector3 direction = (suctionPoint.position - player.transform.position).normalized;
            playerRb.AddForce(direction * suctionForce, ForceMode.Acceleration);
            yield return null;
        }

        // Удержание внутри
        playerRb.velocity = Vector3.zero;
        yield return new WaitForSeconds(holdDuration);

        // Выбрасывание игрока
        playerRb.AddForce(throwDirection.normalized * throwForce, ForceMode.Impulse);

        // Сброс состояния
        isPlayerInside = false;
    }
}
