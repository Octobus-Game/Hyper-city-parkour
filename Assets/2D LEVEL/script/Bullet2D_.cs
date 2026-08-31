using UnityEngine;
using UnityEngine.SceneManagement;

public class Bullet2D_ : MonoBehaviour
{
    public float lifetime = 3f;
    public bool restartOnPlayerHit = true; // Отключить = false

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (restartOnPlayerHit)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Enemy") || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}