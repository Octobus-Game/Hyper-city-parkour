using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Настройки эффекта")]
    [SerializeField] private Vector3 hoverOffset = new Vector3(5f, 5f, 0f);
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 originalPosition;
    private RectTransform rectTransform;
    private Coroutine currentAnimation;
    private bool isHovered = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isHovered)
        {
            isHovered = true;
            AnimateToPosition(originalPosition + hoverOffset);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isHovered)
        {
            isHovered = false;
            AnimateToPosition(originalPosition);
        }
    }

    void OnDisable()
    {
        // Возвращаем кнопку в исходное положение при отключении
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
        isHovered = false;

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }

    private void AnimateToPosition(Vector3 targetPosition)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        currentAnimation = StartCoroutine(AnimateCoroutine(targetPosition));
    }

    private IEnumerator AnimateCoroutine(Vector3 targetPosition)
    {
        Vector3 startPosition = rectTransform.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);

            rectTransform.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        currentAnimation = null;
    }

    // Метод для программного изменения оригинальной позиции
    public void UpdateOriginalPosition()
    {
        originalPosition = rectTransform.anchoredPosition;
    }

    // Метод для сброса позиции к оригинальной
    public void ResetToOriginalPosition()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        rectTransform.anchoredPosition = originalPosition;
        isHovered = false;
    }

    // Принудительное возвращение в исходное положение с анимацией
    public void ForceReturnToOriginal()
    {
        if (isHovered)
        {
            isHovered = false;
            AnimateToPosition(originalPosition);
        }
    }

    // Проверка, находится ли кнопка в состоянии hover
    public bool IsInHoverState()
    {
        return isHovered;
    }

    void OnDestroy()
    {
        // Очищаем корутину при уничтожении объекта
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
    }
}