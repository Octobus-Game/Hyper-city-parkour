using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OIO : MonoBehaviour
{
    public Transform player; // ѕерсонаж, за которым будем следовать
    public float followSpeed = 2f; // —корость следовани€

    private bool isFollowing = false; // ‘лаг, указывающий, следуем ли мы за персонажем

    void Update()
    {
        if (isFollowing)
        {
            FollowPlayer();
        }
    }

    private void FollowPlayer()
    {
        // ƒвигаемс€ к позиции персонажа с определенной скоростью
        transform.position = Vector3.Lerp(transform.position, player.position, followSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == player.gameObject)
        {
            isFollowing = true; // Ќачинаем следовать за персонажем при касании
        }
    }
}

