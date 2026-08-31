using UnityEngine;

public class HealObject : MonoBehaviour
{
    [Header("Heal Settings")]
    [Tooltip("Количество восстановления здоровья")]
    public float healAmount = 10f;

    [Tooltip("Восстанавливать здоровье постоянно при контакте?")]
    public bool continuousHeal = false;

    [Tooltip("Увеличивать максимальное здоровье?")]
    public bool increaseMaxHealth = false;

    [Tooltip("Количество увеличения максимального здоровья")]
    public float maxHealthIncrease = 0f;

    [Header("Optional Settings")]
    [Tooltip("Уничтожить объект после использования? (только для одноразового лечения)")]
    public bool destroyAfterUse = false;

    [Tooltip("Звук при лечении")]
    public AudioClip healSound;

    [Tooltip("Эффект частиц при лечении")]
    public GameObject healEffect;

    private bool hasBeenUsed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (!continuousHeal && !hasBeenUsed)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                ApplyHeal(collision.gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!continuousHeal && !hasBeenUsed)
        {
            if (other.CompareTag("Player"))
            {
                ApplyHeal(other.gameObject);
            }
        }
    }

    private void ApplyHeal(GameObject player)
    {
        PlayerHealth_01 playerHealth = player.GetComponent<PlayerHealth_01>();
        if (playerHealth != null)
        {
            // Восстановить здоровье
            playerHealth.Heal(healAmount);

            // Увеличить максимальное здоровье, если нужно
            if (increaseMaxHealth && maxHealthIncrease > 0)
            {
                playerHealth.IncreaseMaxHealth(maxHealthIncrease);
            }

            // Воспроизвести звук
            PlayHealSound();

            // Создать эффект
            SpawnHealEffect();

            hasBeenUsed = true;

            // Уничтожить объект, если нужно
            if (destroyAfterUse)
            {
                Destroy(gameObject);
            }
        }
    }

    private void PlayHealSound()
    {
        if (healSound != null)
        {
            AudioSource.PlayClipAtPoint(healSound, transform.position);
        }
    }

    private void SpawnHealEffect()
    {
        if (healEffect != null)
        {
            GameObject effect = Instantiate(healEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Уничтожить эффект через 2 секунды
        }
    }
}