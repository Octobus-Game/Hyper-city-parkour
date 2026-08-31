using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Healt_mirror_Player : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    private float healthValue = 100f;

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Image healthBarImage;

    [Header("Death Settings")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject objectToActivate;
    [SerializeField] private float deathDelay = 10f;

    [Header("damage / Heal on Trigger")]
    [Tooltip("Сколько HP отнимать при касании объекта с тегом Damage (положительное число)")]
    [SerializeField] private float damageAmount = 10f;

    [Tooltip("Сколько HP прибавлять при касании объекта с тегом Heal (положительное число)")]
    [SerializeField] private float healAmount = 20f;

    [Tooltip("Минимальный интервал между срабатываниями одного и того же триггера (сек.)")]
    [SerializeField] private float triggerCooldown = 0.5f;

    private bool isDead = false;
    private float lastTriggerTime = -999f;

    // ── Жизненный цикл ──────────────────────────────────────────────────────
    private void Start()
    {
        if (playerObject == null)
            playerObject = gameObject;
        UpdateHealthBar();
    }

    // ── Триггеры (только сервер) ─────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer || isDead) return;
        if (Time.time - lastTriggerTime < triggerCooldown) return;

        if (other.CompareTag("damage"))
        {
            lastTriggerTime = Time.time;
            TakeDamage(damageAmount);
        }
        else if (other.CompareTag("Heal"))
        {
            lastTriggerTime = Time.time;
            Heal(healAmount);
        }
    }

    // При нахождении внутри зоны (например, пол-урона в секунду)
    private void OnTriggerStay(Collider other)
    {
        if (!isServer || isDead) return;
        if (Time.time - lastTriggerTime < triggerCooldown) return;

        if (other.CompareTag("Damage"))
        {
            lastTriggerTime = Time.time;
            TakeDamage(damageAmount);
        }
        else if (other.CompareTag("Heal"))
        {
            lastTriggerTime = Time.time;
            Heal(healAmount);
        }
    }

    // ── Получение урона (только сервер) ─────────────────────────────────────
    /// <summary>
    /// Вызывается напрямую на сервере (например из Mirror_Bullet_Damage.HandleHit).
    /// НЕ нужен Command — пуля уже обрабатывает коллизию только на сервере.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (!isServer || isDead) return;

        healthValue = Mathf.Max(healthValue - amount, 0f);
        Debug.Log($"[Server] {gameObject.name} took {amount} damage. HP: {healthValue}");

        if (healthValue <= 0f)
            HandleDeath();
    }

    // ── Лечение (только сервер) ──────────────────────────────────────────────
    public void Heal(float amount)
    {
        if (!isServer || isDead) return;

        healthValue = Mathf.Min(healthValue + amount, maxHealth);
        Debug.Log($"[Server] {gameObject.name} healed {amount}. HP: {healthValue}");
    }

    // ── Синхронизация полоски здоровья ──────────────────────────────────────
    void OnHealthChanged(float oldHealth, float newHealth)
    {
        UpdateHealthBar();
        Debug.Log($"[Client] HP: {oldHealth} -> {newHealth}");
    }

    void UpdateHealthBar()
    {
        if (healthBarImage != null)
            healthBarImage.fillAmount = healthValue / maxHealth;
    }

    // ── Смерть ──────────────────────────────────────────────────────────────
    void HandleDeath()
    {
        if (!isServer || isDead) return;
        isDead = true;
        Debug.Log($"[Server] {gameObject.name} died.");
        RpcOnDeath();
        StartCoroutine(DeactivateAfterDelay());
    }

    [ClientRpc]
    void RpcOnDeath()
    {
        Debug.Log("[Client] Player death event received.");
    }

    IEnumerator DeactivateAfterDelay()
    {
        yield return new WaitForSeconds(deathDelay);
        if (objectToActivate != null)
            RpcActivateObject();
        RpcDeactivatePlayer();
    }

    [ClientRpc]
    void RpcActivateObject()
    {
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
            Debug.Log("[Client] Respawn/death object activated.");
        }
    }

    [ClientRpc]
    void RpcDeactivatePlayer()
    {
        if (playerObject != null)
        {
            playerObject.SetActive(false);
            Debug.Log("[Client] Player deactivated.");
        }
    }

    // ── Геттеры ─────────────────────────────────────────────────────────────
    public float GetCurrentHealth() => healthValue;
    public float GetMaxHealth()     => maxHealth;
}