using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dialoge : MonoBehaviour
{
    public GameObject panel;
    public float fadeSpeed = 2f;
    public bool useFadeEffect = false;
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;      // Скорость поворота к игроку
    public bool lockYAxis = true;         // Вращать только по оси Y (горизонтально)
    public bool rotateOnlyInTrigger = true; // Вращаться только когда игрок в триггере

    private bool playerInTrigger = false;
    private bool panelVisible = false;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    private Transform playerTransform;   // Ссылка на трансформ игрока

    private void Start()
    {
        if (panel != null)
        {
            panel.SetActive(false);

            if (useFadeEffect)
            {
                canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = panel.AddComponent<CanvasGroup>();
            }
        }

        // Ищем игрока заранее (опционально)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    private void Update()
    {
        if (playerInTrigger && Input.GetKeyDown(KeyCode.E))
            TogglePanel();

        // Вращаемся к игроку
        if (playerTransform != null)
        {
            bool shouldRotate = rotateOnlyInTrigger ? playerInTrigger : true;

            if (shouldRotate)
                RotateTowardsPlayer();
        }
    }

    private void RotateTowardsPlayer()
    {
        Vector3 direction = transform.position - playerTransform.position;
        
	if (lockYAxis)
            direction.y = 0f; // Игнорируем вертикальную разницу

        if (direction.sqrMagnitude < 0.001f) return; // Игрок стоит прямо на объекте

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
            playerTransform = other.transform; // Обновляем ссылку на игрока

            if (panel != null && !panelVisible)
                ShowPanel();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;

            if (panel != null && panelVisible)
                HidePanel();
        }
    }

    private void TogglePanel()
    {
        if (panelVisible) HidePanel();
        else ShowPanel();
    }

    private void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            panelVisible = true;

            if (useFadeEffect)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadeTo(1f));
            }
            else
            {
                if (canvasGroup != null) canvasGroup.alpha = 1f;
            }
        }
    }

    private void HidePanel()
    {
        if (panel != null && panelVisible)
        {
            panelVisible = false;

            if (useFadeEffect)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadeOut());
            }
            else
            {
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                panel.SetActive(false);
            }
        }
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator FadeOut()
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        panel.SetActive(false);
    }
}