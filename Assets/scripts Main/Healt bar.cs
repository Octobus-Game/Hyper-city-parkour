using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Healtbar : MonoBehaviour
{
    [Header("Настройки здоровья")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI элементы")]
    public Image healthBarImage; // UI Image с Fill Mode = Filled

    [Header("Урон от объектов с тегами")]
    public string[] damagingTags;

    [Header("Урон при касании")]
    public float damageOnHit = 10f;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach (string tag in damagingTags)
        {
            if (other.CompareTag(tag))
            {
                TakeDamage(damageOnHit);
                break;
            }
        }
    }

    private void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarImage != null)
            healthBarImage.fillAmount = currentHealth / maxHealth;
    }

    private void Die()
    {
        Debug.Log("Игрок погиб.");
        // Здесь можно вызвать перезапуск уровня, смерть, анимацию и т.д.
        gameObject.SetActive(false);
    }
}
