using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisableOnInteract : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Объект, который будет отключаться")]
    public GameObject targetObject;

    [Tooltip("Звук")]
    public AudioClip _AudioClip;
    
    
    [Tooltip("Клавиша для взаимодействия")]
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Настройки")]
    [Tooltip("UI Panel с подсказкой")]
    public GameObject hintPanel;

    [Tooltip("Текст на панели (TextMeshPro)")]
    public TextMeshProUGUI hintTextTMP;

    [Tooltip("Или обычный UI Text")]
    public Text hintTextUI;

    [Tooltip("Текст подсказки")]
    public string hintMessage = "Нажмите E для взаимодействия";

    [Header("Опции")]
    [Tooltip("Удалять объект вместо отключения")]
    public bool destroyInsteadOfDisable = false;

    [Tooltip("Скрыть панель после взаимодействия")]
    public bool hidePanelAfterInteract = true;

    // Внутренние переменные
    private bool playerInZone = false;
    private GameObject player;

    private void Start()
    {
        // Скрываем панель при старте
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Если игрок в зоне и нажата клавиша
        if (playerInZone && Input.GetKeyDown(interactKey))
        {
            DisableTarget();

            if (hidePanelAfterInteract && hintPanel != null)
            {
                hintPanel.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что это игрок
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            player = other.gameObject;

            // Показываем панель
            ShowHintPanel();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Игрок вышел из зоны
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            player = null;

            // Скрываем панель
            HideHintPanel();
        }
    }

    private void ShowHintPanel()
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(true);

            // Устанавливаем текст подсказки
            if (hintTextTMP != null)
            {
                hintTextTMP.text = hintMessage;
            }
            else if (hintTextUI != null)
            {
                hintTextUI.text = hintMessage;
            }
        }
        else
        {
            Debug.LogWarning("Hint Panel не назначен в инспекторе!");
        }
    }

    private void HideHintPanel()
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }
    }

    private void DisableTarget()
    {
        if (targetObject != null)
        {
            if (destroyInsteadOfDisable)
            {
                Destroy(targetObject);
            }
            else
            {
                targetObject.SetActive(false);
            }

            Debug.Log($"Объект {targetObject.name} был отключен");

            
        
        }
        else
        {
            Debug.LogWarning("Target Object не назначен в инспекторе!");
        }
    
    
    
    
    
    }

    private void OnDrawGizmos()
    {
        // Визуализация триггера в редакторе
        Gizmos.color = playerInZone ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}