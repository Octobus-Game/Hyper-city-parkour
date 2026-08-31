using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Health_Manager : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    [Header("Game Over Settings")]
    [SerializeField] private GameObject gameOverPanel;
    private bool isDead = false;
    private void Start()
    {
        // Инициализация здоровья
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        // Убедимся что панель Game Over скрыта
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }
    private void Update()
    {
        // Проверяем нажатие Enter для рестарта сцены
        if (isDead && Input.GetKeyDown(KeyCode.Return))
        {
            RestartScene();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        // Проверяем столкновение с объектом с тэгом "damage"
        if (collision.gameObject.CompareTag("damage"))
        {
            DamageObject damageScript = collision.gameObject.GetComponent<DamageObject>();
            if (damageScript != null)
            {
                TakeDamage(damageScript.damageAmount);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // Альтернативный вариант для триггеров
        if (other.CompareTag("damage"))
        {
            DamageObject damageScript = other.GetComponent<DamageObject>();
            if (damageScript != null)
            {
                TakeDamage(damageScript.damageAmount);
            }
        }
    }
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        // Обновляем слайдер
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
        // Проверяем смерть
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        isDead = true;
        // Активируем панель Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        // Опционально: останавливаем игру
         Time.timeScale = 0f;
    }
    private void RestartScene()
    {
        // Восстанавливаем время если оно было остановлено
        Time.timeScale = 1f;
        // Перезагружаем текущую сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}