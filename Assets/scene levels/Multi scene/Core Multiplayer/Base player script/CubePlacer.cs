using Mirror;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Повесьте на префаб игрока (Player с NetworkIdentity).
///
/// Логика:
///  1. Каждый кадр (только для локального игрока) кастуем луч из камеры.
///  2. Если луч попал в существующий куб (CubeAnchors) — ищем его
///     ближайшую СВОБОДНУЮ точку соединения и показываем там превью-куб.
///     Если свободных точек на этом кубе нет — превью прячем, ставить нельзя.
///  3. Если луч попал не в куб (например, в пол) — это место для самого
///     первого куба стройки, превью ставится прямо в точку попадания.
///  4. ЛКМ подтверждает размещение там, где сейчас стоит превью —
///     отправляется команда на сервер, который ещё раз проверяет место
///     (защита от двух игроков, кликнувших в одну точку одновременно)
///     и спавнит куб для всех клиентов.
///
/// Новое:
///  — Слайдеры R/G/B для выбора цвета куба.
///  — Toggle «Радуга» — автоматическая смена цвета по hue.
///  — Image-превью итогового цвета.
///  — Позиции привязываются к сетке (cubeSize).
///  — Цвет синхронизируется по сети через SyncVar.
///  — ПКМ удаляет куб (CubeAnchors), на который смотрит игрок.
///    Подсветка хайлайтом показывает, что будет удалено.
///    Удаление отправляется командой на сервер (NetworkServer.Destroy).
/// </summary>
public class CubePlacer : NetworkBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // Инспектор: префабы и настройки
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Префабы")]
    [SerializeField] private GameObject cubePrefab;
    [Tooltip("Визуальная копия куба без коллайдера и NetworkIdentity, с полупрозрачным материалом")]
    [SerializeField] private GameObject previewPrefab;

    [Header("Настройки")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask placementMask = ~0;
    [SerializeField] private float maxRayDistance = 100f;
    [SerializeField] private float cubeSize = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    // Инспектор: UI
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI — цвет куба")]
    [SerializeField] private Slider sliderR;
    [SerializeField] private Slider sliderG;
    [SerializeField] private Slider sliderB;
    [Tooltip("Toggle включает радужную анимацию и блокирует слайдеры")]
    [SerializeField] private Toggle rainbowToggle;
    [Tooltip("Image, которая показывает итоговый выбранный цвет")]
    [SerializeField] private Image colorPreviewImage;

    // ─────────────────────────────────────────────────────────────────────────
    // Приватные поля
    // ─────────────────────────────────────────────────────────────────────────

    private GameObject _previewInstance;
    private Renderer   _previewRenderer;

    private Vector3?   _validSpawnPosition;
    private Quaternion _validSpawnRotation;

    // Текущий цвет, выбранный локальным игроком
    private Color _selectedColor = Color.white;
    private float _rainbowHue;

    // SyncVar: сервер хранит и рассылает цвет всем клиентам
    [SyncVar(hook = nameof(OnSyncColorChanged))]
    private Color _syncedColor = Color.white;

    // ─────────────────────────────────────────────────────────────────────────
    // Поля для удаления (ПКМ)
    // ─────────────────────────────────────────────────────────────────────────

    // Куб, на который сейчас смотрит игрок (кандидат на удаление)
    private CubeAnchors _hoveredCube;
    // Рендереры хайлайта: запоминаем, чтобы снять подсветку при уходе курсора
    private Renderer[]  _hoveredRenderers;

    [Header("Настройки удаления")]
    [Tooltip("Цвет подсветки куба при наведении ПКМ (outline-эффект через emission)")]
    [SerializeField] private Color destroyHighlightColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Интенсивность emission-подсветки при наведении")]
    [SerializeField] private float destroyHighlightIntensity = 1.5f;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (!isLocalPlayer)
        {
            // Не локальный игрок — UI не нужен
            DisableUI();
            return;
        }

        // Превью-куб
        if (previewPrefab != null)
        {
            _previewInstance = Instantiate(previewPrefab);
            _previewInstance.SetActive(false);
            _previewRenderer = _previewInstance.GetComponentInChildren<Renderer>();
        }

        // Настраиваем слайдеры
        SetupSliders();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleRainbow();
        UpdateColorUI();
        UpdatePreview();
        UpdateDestroyHighlight();

        if (Input.GetMouseButtonDown(0) && _validSpawnPosition.HasValue)
        {
            CmdSpawnCube(_validSpawnPosition.Value, _validSpawnRotation, _selectedColor);
        }

        // ПКМ — удаляем куб, на который смотрим
        if (Input.GetMouseButtonDown(1) && _hoveredCube != null)
        {
            NetworkIdentity cubeNetId = _hoveredCube.GetComponent<NetworkIdentity>();
            if (cubeNetId != null)
                CmdDestroyCube(cubeNetId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI: инициализация и логика цвета
    // ─────────────────────────────────────────────────────────────────────────

    private void SetupSliders()
    {
        if (sliderR != null) { sliderR.minValue = 0f; sliderR.maxValue = 1f; sliderR.value = 1f; sliderR.onValueChanged.AddListener(_ => OnSliderChanged()); }
        if (sliderG != null) { sliderG.minValue = 0f; sliderG.maxValue = 1f; sliderG.value = 1f; sliderG.onValueChanged.AddListener(_ => OnSliderChanged()); }
        if (sliderB != null) { sliderB.minValue = 0f; sliderB.maxValue = 1f; sliderB.value = 1f; sliderB.onValueChanged.AddListener(_ => OnSliderChanged()); }

        if (rainbowToggle != null)
            rainbowToggle.onValueChanged.AddListener(OnRainbowToggleChanged);

        UpdateColorFromSliders();
    }

    private void OnSliderChanged()
    {
        if (rainbowToggle != null && rainbowToggle.isOn) return; // в режиме радуги слайдеры не управляют цветом
        UpdateColorFromSliders();
    }

    private void UpdateColorFromSliders()
    {
        float r = sliderR != null ? sliderR.value : 1f;
        float g = sliderG != null ? sliderG.value : 1f;
        float b = sliderB != null ? sliderB.value : 1f;
        _selectedColor = new Color(r, g, b);
    }

    private void OnRainbowToggleChanged(bool isOn)
    {
        // Блокируем/разблокируем слайдеры при переключении радуги
        if (sliderR != null) sliderR.interactable = !isOn;
        if (sliderG != null) sliderG.interactable = !isOn;
        if (sliderB != null) sliderB.interactable = !isOn;
    }

    private void HandleRainbow()
    {
        if (rainbowToggle == null || !rainbowToggle.isOn) return;

        _rainbowHue = (_rainbowHue + Time.deltaTime * 0.3f) % 1f;
        _selectedColor = Color.HSVToRGB(_rainbowHue, 1f, 1f);

        // Синхронизируем слайдеры визуально (без вызова callback'ов)
        SetSliderValueSilent(sliderR, _selectedColor.r);
        SetSliderValueSilent(sliderG, _selectedColor.g);
        SetSliderValueSilent(sliderB, _selectedColor.b);
    }

    /// <summary>Ставит значение слайдера без срабатывания onValueChanged.</summary>
    private void SetSliderValueSilent(Slider slider, float value)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.value = value;
        slider.onValueChanged.AddListener(_ => OnSliderChanged());
    }

    private void UpdateColorUI()
    {
        // Image-превью
        if (colorPreviewImage != null)
            colorPreviewImage.color = _selectedColor;

        // Превью-куб тоже окрашиваем
        if (_previewRenderer != null)
        {
            // Добавляем прозрачность (alpha = 0.5) для превью
            Color previewColor = _selectedColor;
            previewColor.a = 0.5f;
            _previewRenderer.material.color = previewColor;
        }
    }

    /// <summary>Скрывает UI-элементы у не-локальных игроков.</summary>
    private void DisableUI()
    {
        if (sliderR     != null) sliderR.gameObject.SetActive(false);
        if (sliderG     != null) sliderG.gameObject.SetActive(false);
        if (sliderB     != null) sliderB.gameObject.SetActive(false);
        if (rainbowToggle     != null) rainbowToggle.gameObject.SetActive(false);
        if (colorPreviewImage != null) colorPreviewImage.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Превью и размещение
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdatePreview()
    {
        _validSpawnPosition = null;

        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null)
        {
            SetPreviewActive(false);
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementMask))
        {
            SetPreviewActive(false);
            return;
        }

        CubeAnchors hitAnchors = hit.collider.GetComponentInParent<CubeAnchors>();

        if (hitAnchors != null)
        {
            Transform freePoint = hitAnchors.GetNearestFreePoint(hit.point);

            if (freePoint == null)
            {
                // Все точки этого куба заняты
                SetPreviewActive(false);
                return;
            }

            // Привязываем позицию точки соединения к сетке
            _validSpawnPosition = SnapToGrid(freePoint.position);
            _validSpawnRotation = hitAnchors.transform.rotation;
        }
        else
        {
            // Не куб (пол/стена) — первый куб стройки
            Vector3 rawPos = hit.point + hit.normal * (cubeSize * 0.5f);
            _validSpawnPosition = SnapToGrid(rawPos);
            _validSpawnRotation = Quaternion.identity;
        }

        SetPreviewActive(true);
        if (_previewInstance != null && _validSpawnPosition.HasValue)
        {
            _previewInstance.transform.SetPositionAndRotation(_validSpawnPosition.Value, _validSpawnRotation);
        }
    }

    /// <summary>
    /// Округляет позицию до ближайшего узла сетки с шагом cubeSize.
    /// Ось Y округляется отдельно, чтобы кубы не «плавали».
    /// </summary>
    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / cubeSize) * cubeSize,
            Mathf.Round(position.y / cubeSize) * cubeSize,
            Mathf.Round(position.z / cubeSize) * cubeSize
        );
    }

    private void SetPreviewActive(bool active)
    {
        if (_previewInstance != null)
            _previewInstance.SetActive(active);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Сеть
    // ─────────────────────────────────────────────────────────────────────────

    [Command]
    private void CmdSpawnCube(Vector3 position, Quaternion rotation, Color color)
    {
        // Повторная проверка: вдруг два игрока кликнули в одну точку одновременно
        Collider[] overlap = Physics.OverlapSphere(position, cubeSize * 0.4f);
        foreach (var col in overlap)
        {
            if (col.GetComponentInParent<CubeAnchors>() != null)
                return; // место уже занято
        }

        GameObject cube = Instantiate(cubePrefab, position, rotation);

        // Применяем цвет на сервере до Spawn, чтобы он попал к клиентам через SyncVar
        CubeColorSync colorSync = cube.GetComponent<CubeColorSync>();
        if (colorSync != null)
            colorSync.ServerSetColor(color);

        NetworkServer.Spawn(cube, connectionToClient);
    }

    /// <summary>
    /// Hook для SyncVar _syncedColor — вызывается на всех клиентах при изменении.
    /// Здесь можно обновить UI локального игрока, если нужно.
    /// </summary>
    private void OnSyncColorChanged(Color oldColor, Color newColor)
    {
        // Можно использовать для реакции на смену цвета, если понадобится
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Удаление (ПКМ)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Каждый кадр кастуем луч и ищем куб под курсором.
    /// Если нашли — подсвечиваем emission; если курсор ушёл — снимаем подсветку.
    /// </summary>
    private void UpdateDestroyHighlight()
    {
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        CubeAnchors newHovered = null;

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementMask))
            newHovered = hit.collider.GetComponentInParent<CubeAnchors>();

        // Куб под курсором не изменился — ничего не делаем
        if (newHovered == _hoveredCube) return;

        // Снимаем подсветку со старого куба
        SetDestroyHighlight(_hoveredRenderers, false);

        _hoveredCube = newHovered;

        if (_hoveredCube != null)
        {
            _hoveredRenderers = _hoveredCube.GetComponentsInChildren<Renderer>();
            SetDestroyHighlight(_hoveredRenderers, true);
        }
        else
        {
            _hoveredRenderers = null;
        }
    }

    /// <summary>Включает или выключает emission-подсветку на массиве рендереров.</summary>
    private void SetDestroyHighlight(Renderer[] renderers, bool enable)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            // Работаем с инстанс-материалом, чтобы не менять asset
            Material mat = r.material;
            if (enable)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", destroyHighlightColor * destroyHighlightIntensity);
            }
            else
            {
                mat.SetColor("_EmissionColor", Color.black);
                mat.DisableKeyword("_EMISSION");
            }
        }
    }

    /// <summary>
    /// Команда на сервер: проверяем, что объект существует и является кубом,
    /// затем уничтожаем его для всех клиентов.
    /// </summary>
    [Command]
    private void CmdDestroyCube(NetworkIdentity cubeIdentity)
    {
        if (cubeIdentity == null) return;

        // Убеждаемся, что это действительно наш куб, а не произвольный объект
        if (cubeIdentity.GetComponent<CubeAnchors>() == null) return;

        NetworkServer.Destroy(cubeIdentity.gameObject);
    }
}