using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggablePanel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Настройки перетаскивания")]
    [SerializeField] private bool isDraggable = true;
    [SerializeField] private bool constrainToScreen = true;
    [SerializeField] private float smoothness = 0f; // 0 = мгновенно, больше = плавнее

    [Header("Ограничения (опционально)")]
    [SerializeField] private RectTransform boundaryRect; // ограничивающая область

    // Приватные переменные
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private bool isDragging = false;
    private Vector2 offset;
    private Vector2 targetPosition;

    void Start()
    {
        // Получаем компоненты
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Если нет CanvasGroup, добавляем для визуальных эффектов
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        targetPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // Плавное движение (если включено)
        if (smoothness > 0 && isDragging)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition,
                targetPosition,
                Time.deltaTime * smoothness
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isDraggable) return;

        isDragging = true;

        // Вычисляем смещение между позицией панели и курсором
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );

        offset = rectTransform.anchoredPosition - localPointerPosition;

        // Визуальные эффекты при захвате
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.8f;
        }

        // Поднимаем панель на передний план
        transform.SetAsLastSibling();

        TriggerDragStart();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable || !isDragging) return;

        // Конвертируем позицию мыши в локальные координаты
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );

        // Вычисляем новую позицию с учётом смещения
        Vector2 newPosition = localPointerPosition + offset;

        // Применяем ограничения
        newPosition = ApplyConstraints(newPosition);

        // Устанавливаем позицию
        if (smoothness <= 0)
        {
            rectTransform.anchoredPosition = newPosition;
        }
        else
        {
            targetPosition = newPosition;
        }

        TriggerPositionChanged(newPosition);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // Восстанавливаем визуальные эффекты
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        TriggerDragEnd();
    }

    private Vector2 ApplyConstraints(Vector2 position)
    {
        if (!constrainToScreen && boundaryRect == null)
            return position;

        RectTransform parentRect = rectTransform.parent as RectTransform;
        RectTransform constraintRect = boundaryRect != null ? boundaryRect : parentRect;

        if (constraintRect == null)
            return position;

        // Получаем размеры панели и ограничивающей области
        Vector2 panelSize = rectTransform.sizeDelta;
        Vector2 constraintSize = constraintRect.sizeDelta;

        // Вычисляем границы с учётом анкеров
        Vector2 minPosition = -constraintSize * 0.5f + panelSize * 0.5f;
        Vector2 maxPosition = constraintSize * 0.5f - panelSize * 0.5f;

        // Ограничиваем позицию
        position.x = Mathf.Clamp(position.x, minPosition.x, maxPosition.x);
        position.y = Mathf.Clamp(position.y, minPosition.y, maxPosition.y);

        return position;
    }

    // Публичные методы для управления
    public void SetDraggable(bool draggable)
    {
        isDraggable = draggable;
    }

    public void SetConstrainToScreen(bool constrain)
    {
        constrainToScreen = constrain;
    }

    public void SetBoundaryRect(RectTransform boundary)
    {
        boundaryRect = boundary;
    }

    public void ResetPosition()
    {
        rectTransform.anchoredPosition = Vector2.zero;
        targetPosition = Vector2.zero;
    }

    // Свойства для чтения
    public bool IsDragging => isDragging;
    public Vector2 CurrentPosition => rectTransform.anchoredPosition;

    // События для интеграции с другими системами
    public System.Action OnDragStart;
    public System.Action OnDragEnd;
    public System.Action<Vector2> OnPositionChanged;

    // Вызываем события в соответствующих методах
    private void TriggerDragStart()
    {
        OnDragStart?.Invoke();
    }

    private void TriggerDragEnd()
    {
        OnDragEnd?.Invoke();
    }

    private void TriggerPositionChanged(Vector2 newPosition)
    {
        OnPositionChanged?.Invoke(newPosition);
    }
}