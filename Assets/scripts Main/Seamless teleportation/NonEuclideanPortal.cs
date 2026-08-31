using UnityEngine;

/// <summary>
/// Компонент портала. Прикрепляется к объекту с MeshRenderer (плоскость портала).
/// Рендерит вид с другой стороны через RenderTexture.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class NonEuclideanPortal : MonoBehaviour
{
    [Header("Связанный портал")]
    public NonEuclideanPortal linkedPortal;

    [Header("Настройки рендера")]
    public int renderTextureWidth  = 1280;
    public int renderTextureHeight = 720;
    [Range(1, 8)]
    public int recursionDepth = 2; // Глубина рекурсивного рендера

    [Header("Ссылки")]
    public Camera mainCamera;      // Основная камера игрока

    // ── Приватные поля ──────────────────────────────────────────────
    private Camera        _portalCamera;
    private RenderTexture _renderTexture;
    private MeshRenderer  _meshRenderer;
    private Material      _portalMaterial;

    // Порог пересечения плоскости портала (метры)
    private const float CrossThreshold = 0.05f;

    // ── Unity Lifecycle ──────────────────────────────────────────────
    void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();

        // Создаём камеру портала как дочерний объект
        var camGO = new GameObject($"PortalCamera_{name}");
        camGO.transform.SetParent(transform);
        _portalCamera = camGO.AddComponent<Camera>();
        _portalCamera.enabled = false; // Рендерим вручную
        _portalCamera.fieldOfView = mainCamera != null ? mainCamera.fieldOfView : 60f;
        _portalCamera.nearClipPlane = 0.01f;

        CreateRenderTexture();
    }

    void OnValidate()
    {
        if (_renderTexture != null) CreateRenderTexture();
    }

    void OnDestroy()
    {
        if (_renderTexture != null) _renderTexture.Release();
        if (_portalMaterial  != null) Destroy(_portalMaterial);
    }

    // ── Публичные методы ─────────────────────────────────────────────

    /// <summary>
    /// Вызывается из PortalManager перед основным рендером кадра.
    /// </summary>
    public void RenderPortalView(int depth = 0)
    {
        if (!linkedPortal || !mainCamera) return;
        if (depth >= recursionDepth) return;

        // Рекурсивный рендер связанного портала
        if (depth + 1 < recursionDepth)
            linkedPortal.RenderPortalView(depth + 1);

        // Позиционируем камеру портала относительно связанного портала
        PositionPortalCamera();

        // Косоугольная проекция: обрезаем сцену по плоскости портала
        _portalCamera.projectionMatrix = CalculateObliqueProjection();

        _portalCamera.Render();
    }

    /// <summary>
    /// Проверяет, пересёк ли объект плоскость портала в этом кадре.
    /// Возвращает true, если нужна телепортация.
    /// </summary>
    public bool ShouldTeleport(Transform traveller, Vector3 previousPos)
    {
        // Нормаль портала в мировых координатах
        Vector3 normal     = transform.forward;
        Vector3 portalPos  = transform.position;

        float previousSide = Vector3.Dot(previousPos        - portalPos, normal);
        float currentSide  = Vector3.Dot(traveller.position - portalPos, normal);

        // Пересечение плоскости (смена знака) И объект достаточно близко
        bool crossedPlane    = Mathf.Sign(previousSide) != Mathf.Sign(currentSide);
        bool withinThreshold = Mathf.Abs(previousSide) < CrossThreshold * 10f;

        return crossedPlane && withinThreshold;
    }

    /// <summary>
    /// Возвращает трансформацию телепортации:
    /// мировая позиция и поворот на выходе из связанного портала.
    /// </summary>
    public (Vector3 newPos, Quaternion newRot) GetTeleportTransform(Transform traveller)
    {
        // Матрица перехода: из локального пространства этого портала
        // в мировое пространство связанного портала (с разворотом на 180°)
        Matrix4x4 m = linkedPortal.transform.localToWorldMatrix
                    * Matrix4x4.Rotate(Quaternion.Euler(0, 180f, 0))
                    * transform.worldToLocalMatrix;

        Vector3    newPos = m.MultiplyPoint(traveller.position);
        Quaternion newRot = m.rotation * traveller.rotation;

        return (newPos, newRot);
    }

    // ── Приватные методы ─────────────────────────────────────────────

    private void CreateRenderTexture()
    {
        if (_renderTexture != null) _renderTexture.Release();

        _renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24)
        {
            name = $"PortalRT_{name}"
        };
        _renderTexture.Create();

        if (_portalCamera) _portalCamera.targetTexture = _renderTexture;

        // Применяем текстуру к материалу (создаём копию, чтобы не менять шейдер глобально)
        if (_meshRenderer)
        {
            _portalMaterial = new Material(_meshRenderer.sharedMaterial);
            _portalMaterial.mainTexture = _renderTexture;
            _meshRenderer.material = _portalMaterial;
        }
    }

    /// <summary>
    /// Зеркально отображает позицию/поворот основной камеры
    /// через пару порталов, чтобы камера портала смотрела в нужную сторону.
    /// </summary>
    private void PositionPortalCamera()
    {
        // Преобразование: из мира → локальное пространство ЭТОГО портала →
        // мировое пространство СВЯЗАННОГО портала (с поворотом 180° вокруг Y)
        Matrix4x4 m = linkedPortal.transform.localToWorldMatrix
                    * Matrix4x4.Rotate(Quaternion.Euler(0, 180f, 0))
                    * transform.worldToLocalMatrix;

        _portalCamera.transform.position = m.MultiplyPoint(mainCamera.transform.position);
        _portalCamera.transform.rotation = m.rotation * mainCamera.transform.rotation;
    }

    /// <summary>
    /// Вычисляет матрицу косоугольной проекции (Oblique Projection),
    /// чтобы near clip plane совпадал с поверхностью связанного портала.
    /// Это исключает рендер объектов "позади" плоскости портала.
    /// </summary>
    private Matrix4x4 CalculateObliqueProjection()
    {
        // Плоскость отсечения в пространстве камеры портала
        Vector3 normal   = linkedPortal.transform.forward;
        Vector3 position = linkedPortal.transform.position;

        // Убеждаемся, что нормаль смотрит в сторону камеры
        if (Vector3.Dot(normal, position - _portalCamera.transform.position) > 0)
            normal = -normal;

        // Переводим в Camera Space
        Vector4 clipPlane = CameraSpacePlane(_portalCamera, position, normal);

        // Стандартная матрица проекции Unity
        Matrix4x4 proj = mainCamera.projectionMatrix;
        MakeProjectionOblique(ref proj, clipPlane);
        return proj;
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 worldPos, Vector3 worldNormal)
    {
        Matrix4x4 viewMatrix = cam.worldToCameraMatrix;
        Vector3 cPos = viewMatrix.MultiplyPoint(worldPos);
        Vector3 cNorm = viewMatrix.MultiplyVector(worldNormal).normalized;

        return new Vector4(cNorm.x, cNorm.y, cNorm.z, -Vector3.Dot(cPos, cNorm));
    }

    /// <summary>
    /// Модифицирует матрицу проекции так, чтобы near plane совпадал с плоскостью portala.
    /// Алгоритм Эрика Ленджела (GPU Gems).
    /// </summary>
    private void MakeProjectionOblique(ref Matrix4x4 proj, Vector4 clipPlane)
    {
        Vector4 q = proj.inverse * new Vector4(
            Mathf.Sign(clipPlane.x),
            Mathf.Sign(clipPlane.y),
            1f,
            1f
        );

        Vector4 c = clipPlane * (2f / Vector4.Dot(clipPlane, q));

        proj[2] = c.x - proj[3];
        proj[6] = c.y - proj[7];
        proj[10] = c.z - proj[11];
        proj[14] = c.w - proj[15];
    }
}
