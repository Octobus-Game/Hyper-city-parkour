// MirrorReflectionCamera.cs
// Повесь этот скрипт на объект с зеркалом (тот же, где Renderer с материалом
// на шейдере "Custom/MirrorReflection"). Скрипт создаёт скрытую камеру,
// зеркально отражённую относительно плоскости зеркала, рендерит в RenderTexture
// и передаёт её в шейдер через свойство "_ReflectionTex".
//
// Built-in Render Pipeline. Требует камеру с тегом "MainCamera" в сцене
// (обычно Camera.main).

using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class MirrorReflectionCamera : MonoBehaviour
{
    [Header("Настройки качества")]
    [Tooltip("Разрешение текстуры отражения. Больше = чётче, но дороже.")]
    public int textureSize = 1024;

    [Tooltip("Множитель дальности отсечения камеры-зеркала относительно основной камеры")]
    [Range(0.1f, 3f)]
    public float clipPlaneOffset = 0.07f;

    [Header("Что отражать")]
    [Tooltip("Слои, которые должны попадать в отражение")]
    public LayerMask reflectLayers = ~0;

    [Tooltip("Отражать ли скайбокс")]
    public bool reflectSkybox = true;

    [Header("Производительность")]
    [Tooltip("Пересчитывать отражение каждый кадр (выключи для статичных сцен и ставь false + вызывай RenderReflection() вручную)")]
    public bool renderEveryFrame = true;

    private Camera _reflectionCamera;
    private RenderTexture _reflectionTexture;
    private Renderer _mirrorRenderer;
    private static bool _isRenderingReflection; // защита от рекурсии (зеркало в зеркале)

    private static readonly int ReflectionTexID = Shader.PropertyToID("_ReflectionTex");

    void OnEnable()
    {
        _mirrorRenderer = GetComponent<Renderer>();
        CreateReflectionTexture();
    }

    void OnDisable()
    {
        CleanUp();
    }

    void CreateReflectionTexture()
    {
        if (_reflectionTexture != null && _reflectionTexture.width == textureSize)
            return;

        if (_reflectionTexture != null)
        {
            DestroyImmediate(_reflectionTexture);
        }

        _reflectionTexture = new RenderTexture(textureSize, textureSize, 24)
        {
            name = "__MirrorReflectionTexture",
            hideFlags = HideFlags.DontSave,
            antiAliasing = 1
        };

        if (_mirrorRenderer != null && _mirrorRenderer.sharedMaterial != null)
        {
            _mirrorRenderer.sharedMaterial.SetTexture(ReflectionTexID, _reflectionTexture);
        }
    }

    void CleanUp()
    {
        if (_reflectionCamera != null)
        {
            if (Application.isEditor)
                DestroyImmediate(_reflectionCamera.gameObject);
            else
                Destroy(_reflectionCamera.gameObject);
            _reflectionCamera = null;
        }

        if (_reflectionTexture != null)
        {
            DestroyImmediate(_reflectionTexture);
            _reflectionTexture = null;
        }
    }

    void OnWillRenderObject()
    {
        if (!renderEveryFrame) return;
        RenderReflection();
    }

    public void RenderReflection()
    {
        if (_isRenderingReflection) return; // не даём зеркалу отражать само себя рекурсивно
        if (!gameObject.activeInHierarchy || !enabled) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        CreateReflectionTexture();
        EnsureReflectionCamera(mainCam);

        // Плоскость зеркала в мировых координатах: точка + нормаль (локальная ось Z объекта)
        Vector3 pos = transform.position;
        Vector3 normal = transform.forward;

        // Матрица отражения относительно плоскости зеркала
        Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(pos, normal);

        _isRenderingReflection = true;

        // Копируем параметры основной камеры
        _reflectionCamera.CopyFrom(mainCam);
        _reflectionCamera.cullingMask = reflectLayers;
        _reflectionCamera.clearFlags = reflectSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        _reflectionCamera.backgroundColor = Color.black;
        _reflectionCamera.targetTexture = _reflectionTexture;

        // Позиция и поворот камеры-зеркала = отражение основной камеры
        _reflectionCamera.worldToCameraMatrix = mainCam.worldToCameraMatrix * reflectionMatrix;

        // Наклонная (oblique) плоскость отсечения, чтобы зеркало не рендерило то,
        // что находится "за" его поверхностью
        Vector4 clipPlane = CameraSpacePlane(_reflectionCamera, pos, normal, clipPlaneOffset);
        _reflectionCamera.projectionMatrix = mainCam.CalculateObliqueMatrix(clipPlane);

        // Зеркальное отражение переворачивает winding order треугольников
        GL.invertCulling = true;
        _reflectionCamera.Render();
        GL.invertCulling = false;

        _isRenderingReflection = false;
    }

    void EnsureReflectionCamera(Camera source)
    {
        if (_reflectionCamera != null) return;

        GameObject camObj = new GameObject("__MirrorReflectionCamera")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        _reflectionCamera = camObj.AddComponent<Camera>();
        _reflectionCamera.enabled = false; // рендерим вручную через Render()
    }

    static Matrix4x4 CalculateReflectionMatrix(Vector3 pos, Vector3 normal)
    {
        normal.Normalize();
        float d = -Vector3.Dot(normal, pos);
        Vector4 plane = new Vector4(normal.x, normal.y, normal.z, d);

        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = 1f - 2f * plane.x * plane.x;
        m.m01 = -2f * plane.x * plane.y;
        m.m02 = -2f * plane.x * plane.z;
        m.m03 = -2f * plane.w * plane.x;

        m.m10 = -2f * plane.y * plane.x;
        m.m11 = 1f - 2f * plane.y * plane.y;
        m.m12 = -2f * plane.y * plane.z;
        m.m13 = -2f * plane.w * plane.y;

        m.m20 = -2f * plane.z * plane.x;
        m.m21 = -2f * plane.z * plane.y;
        m.m22 = 1f - 2f * plane.z * plane.z;
        m.m23 = -2f * plane.w * plane.z;

        m.m30 = 0f;
        m.m31 = 0f;
        m.m32 = 0f;
        m.m33 = 1f;
        return m;
    }

    static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float offset)
    {
        Vector3 offsetPos = pos + normal * offset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cPos = m.MultiplyPoint(offsetPos);
        Vector3 cNormal = m.MultiplyVector(normal).normalized;
        return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
    }
}
