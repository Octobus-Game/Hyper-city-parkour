using UnityEngine;

public class ClickToActivateOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Outline outlineScript;
    [SerializeField] private bool toggleOutline = true; // Включить/выключить переключение
    [SerializeField] private bool activateOnStart = false; // Активировать при старте

    [Header("Input Settings")]
    [SerializeField] private bool useMouseClick = true; // Использовать клик мыши
    [SerializeField] private bool useTouchInput = false; // Использовать касание (для мобильных)

    private Camera mainCamera;

    void Start()
    {
        // Получаем основную камеру
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();

        // Если Outline не указан в инспекторе, пытаемся найти его на этом объекте
        if (outlineScript == null)
            outlineScript = GetComponent<Outline>();

        // Устанавливаем начальное состояние
        if (outlineScript != null)
            outlineScript.enabled = activateOnStart;
    }

    void Update()
    {
        // Обработка клика мыши
        if (useMouseClick && Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }

        // Обработка касания для мобильных устройств
        if (useTouchInput && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                HandleTouch(touch.position);
            }
        }
    }

    private void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                ActivateOutline();
            }
        }
    }

    private void HandleTouch(Vector2 touchPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(touchPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                ActivateOutline();
            }
        }
    }

    private void ActivateOutline()
    {
        if (outlineScript == null)
        {
            Debug.LogWarning("Outline script не найден на объекте " + gameObject.name);
            return;
        }

        if (toggleOutline)
        {
            // Переключаем состояние Outline
            outlineScript.enabled = !outlineScript.enabled;
        }
        else
        {
            // Просто активируем Outline
            outlineScript.enabled = true;
        }

        Debug.Log("Outline " + (outlineScript.enabled ? "активирован" : "деактивирован") + " на объекте " + gameObject.name);
    }

    // Публичные методы для вызова из других скриптов
    public void EnableOutline()
    {
        if (outlineScript != null)
            outlineScript.enabled = true;
    }

    public void DisableOutline()
    {
        if (outlineScript != null)
            outlineScript.enabled = false;
    }

    public void ToggleOutline()
    {
        if (outlineScript != null)
            outlineScript.enabled = !outlineScript.enabled;
    }
}