using UnityEngine;
using System.Collections;

namespace VLB
{
    [ExecuteInEditMode]
    [HelpURL(Consts.Help.UrlDynamicOcclusionDepthBuffer)]
    public class DynamicOcclusionDepthBuffer : DynamicOcclusionAbstractBase
    {
        public new const string ClassName = "DynamicOcclusionDepthBuffer";

        /// <summary>
        /// The beam can only be occluded by objects located on the layers matching this mask.
        /// It's very important to set it as restrictive as possible (checking only the layers which are necessary)
        /// to perform a more efficient process in order to increase the performance.
        /// It should NOT include the layer on which the beams are generated.
        /// </summary>
        public LayerMask layerMask = Consts.DynOcclusion.LayerMaskDefault;

        /// <summary>
        /// Whether or not the virtual camera will use occlusion culling during rendering from the beam's POV.
        /// </summary>
        public bool useOcclusionCulling = Consts.DynOcclusion.DepthBufferOcclusionCullingDefault;

        /// <summary>
        /// Controls how large the depth texture captured by the virtual camera is.
        /// The lower the resolution, the better the performance, but the less accurate the rendering.
        /// </summary>
        public int depthMapResolution = Consts.DynOcclusion.DepthBufferDepthMapResolutionDefault;

        /// <summary>
        /// Fade out the beam before the occlusion surface in order to soften the transition.
        /// </summary>
        public float fadeDistanceToSurface = Consts.DynOcclusion.FadeDistanceToSurfaceDefault;

        // ----------------------------------------------------------------------------------
        //  SOFT EDGES  (новое)
        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Включить смягчение краёв окклюзии. Карта глубины дополнительно фильтруется,
        /// поэтому границы тени/разреза луча становятся плавными вместо резких.
        /// </summary>
        public bool softEdges = true;

        public enum SoftEdgeMode
        {
            /// <summary>
            /// Безопасный режим: исходная depth-текстура остаётся как есть (формат и декодирование в шейдере VLB не меняются),
            /// краю добавляется билинейная выборка и автоматически увеличенная fadeDistanceToSurface.
            /// Работает с любым шейдером VLB без правок.
            /// </summary>
            Safe,

            /// <summary>
            /// Полноценное размытие карты глубины отдельным проходом. Даёт самый мягкий край,
            /// но шейдер VLB должен корректно читать RFloat-текстуру (см. комментарий в OnModifyMaterialCallback).
            /// </summary>
            Filtered
        }

        /// <summary>Способ смягчения краёв. Начните с Safe; если края всё ещё резкие - попробуйте Filtered.</summary>
        public SoftEdgeMode softEdgeMode = SoftEdgeMode.Safe;

        /// <summary>
        /// В режиме Safe: во сколько раз усиливается fadeDistanceToSurface относительно заданного значения.
        /// </summary>
        [Range(1f, 10f)]
        public float safeModeFadeBoost = 3f;

        /// <summary>
        /// Радиус размытия краёв в текселях карты глубины. 0 = без размытия.
        /// Чем больше значение, тем мягче край (но тем сильнее "растекается" тень).
        /// </summary>
        [Range(0f, 8f)]
        public float edgeSoftness = 2.0f;

        /// <summary>
        /// Сколько раз повторить проход фильтра. Больше проходов = более гладкий край,
        /// но дороже по производительности. Обычно хватает 1-2.
        /// </summary>
        [Range(1, 4)]
        public int softenPasses = 2;

        /// <summary>
        /// Порог разницы глубин (0..1), после которого соседние отсчёты считаются разными объектами
        /// и не смешиваются. Защищает от артефактов-"призраков" между близким и дальним слоем.
        /// </summary>
        [Range(0.001f, 0.5f)]
        public float depthDiscontinuityThreshold = 0.05f;

        const string kSoftenShaderName = "Hidden/VLB/DepthSoftenFilter";
        static readonly int kSoftenParamsID = Shader.PropertyToID("_VLB_SoftenParams");

        protected override string GetShaderKeyword() { return "VLB_OCCLUSION_DEPTH_TEXTURE"; }
        protected override MaterialManager.DynamicOcclusion GetDynamicOcclusionMode() { return MaterialManager.DynamicOcclusion.DepthTexture; }

        Camera m_DepthCamera = null;
        bool m_NeedToUpdateOcclusionNextFrame = false;

        // Текстуры для смягчения (ping-pong A <-> B). Итог всегда в A.
        RenderTexture m_SoftRT_A = null;
        RenderTexture m_SoftRT_B = null;
        Material m_SoftenMaterial = null;

        bool IsFilteredMode { get { return softEdges && softEdgeMode == SoftEdgeMode.Filtered && edgeSoftness > 0.001f; } }
        bool IsSoftenActive { get { return IsFilteredMode && m_SoftenMaterial != null; } }

        /// <summary>
        /// Текстура, которая реально отдаётся шейдеру луча:
        /// смягчённая, если фильтр включён, иначе оригинальная карта глубины камеры.
        /// </summary>
        RenderTexture OutputDepthTexture
        {
            get
            {
                if (IsSoftenActive && m_SoftRT_A != null)
                    return m_SoftRT_A;
                return m_DepthCamera != null ? m_DepthCamera.targetTexture : null;
            }
        }

        void ProcessOcclusionInternal()
        {
            UpdateDepthCameraPropertiesAccordingToBeam();
            m_DepthCamera.Render();

            if (IsFilteredMode)
                ApplySoftenFilter();
        }

        /// <summary>
        /// Читает глубину камеры и прогоняет её через фильтр смягчения.
        /// Результат ВСЕГДА оказывается в m_SoftRT_A (ссылка не меняется, поэтому шейдер луча
        /// продолжает читать актуальную текстуру без повторной установки свойства материала).
        /// </summary>
        void ApplySoftenFilter()
        {
            if (m_DepthCamera == null || m_DepthCamera.targetTexture == null) return;
            if (!EnsureSoftenResources()) return;

            m_SoftenMaterial.SetVector(kSoftenParamsID,
                new Vector4(edgeSoftness, depthDiscontinuityThreshold, 0f, 0f));

            // Проход 0: сырая глубина камеры -> A (шейдер сам читает depth-текстуру через _CameraDepthTexture-подобную выборку)
            Graphics.Blit(m_DepthCamera.targetTexture, m_SoftRT_A, m_SoftenMaterial, 0);

            // Дополнительные проходы: A -> B -> A. Чётное число Blit'ов гарантирует, что итог снова в A.
            for (int i = 1; i < softenPasses; i++)
            {
                Graphics.Blit(m_SoftRT_A, m_SoftRT_B, m_SoftenMaterial, 0);
                Graphics.Blit(m_SoftRT_B, m_SoftRT_A, m_SoftenMaterial, 0);
            }
        }

        bool EnsureSoftenResources()
        {
            if (m_SoftenMaterial == null)
            {
                var shader = Shader.Find(kSoftenShaderName);
                if (shader == null)
                {
                    Debug.LogWarning("[VLB] Shader '" + kSoftenShaderName + "' не найден. " +
                                     "Добавьте DepthSoftenFilter.shader в проект (или в Always Included Shaders). Мягкие края отключены.");
                    softEdges = false;
                    return false;
                }
                m_SoftenMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            int res = depthMapResolution;
            if (!IsRTValid(m_SoftRT_A, res))   m_SoftRT_A   = CreateSoftRT(m_SoftRT_A, res, "VLB Soft Depth A");
            if (!IsRTValid(m_SoftRT_B, res))   m_SoftRT_B   = CreateSoftRT(m_SoftRT_B, res, "VLB Soft Depth B");

            return m_SoftRT_A && m_SoftRT_B;
        }

        static bool IsRTValid(RenderTexture rt, int res)
        {
            return rt != null && rt.IsCreated() && rt.width == res && rt.height == res;
        }

        static RenderTexture CreateSoftRT(RenderTexture old, int res, string name)
        {
            if (old != null) { old.Release(); DestroyImmediate(old); }

            // RFloat, если поддерживается, иначе RHalf / ARGB32 как запасной вариант
            var format = RenderTextureFormat.RFloat;
            if (!SystemInfo.SupportsRenderTextureFormat(format)) format = RenderTextureFormat.RHalf;
            if (!SystemInfo.SupportsRenderTextureFormat(format)) format = RenderTextureFormat.ARGBHalf;

            var rt = new RenderTexture(res, res, 0, format, RenderTextureReadWrite.Linear)
            {
                name = name,
                filterMode = FilterMode.Bilinear,   // билинейная фильтрация = дополнительное сглаживание при выборке
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            rt.Create();
            return rt;
        }

        void ReleaseSoftenResources()
        {
            ReleaseRT(ref m_SoftRT_A);
            ReleaseRT(ref m_SoftRT_B);

            if (m_SoftenMaterial)
            {
                DestroyImmediate(m_SoftenMaterial);
                m_SoftenMaterial = null;
            }
        }

        static void ReleaseRT(ref RenderTexture rt)
        {
            if (rt)
            {
                rt.Release();
                DestroyImmediate(rt);
                rt = null;
            }
        }

        protected override bool OnProcessOcclusion(ProcessOcclusionSource source)
        {
            Debug.Assert(m_Master && m_DepthCamera);

            if (source == ProcessOcclusionSource.RenderLoop && SRPHelper.IsUsingCustomRenderPipeline()) // Recursive rendering is not supported on SRP
                m_NeedToUpdateOcclusionNextFrame = true;
            else
                ProcessOcclusionInternal();

            return true;
        }

        void Update()
        {
            if (m_NeedToUpdateOcclusionNextFrame && m_Master && m_DepthCamera)
            {
                ProcessOcclusionInternal();
                m_NeedToUpdateOcclusionNextFrame = false;
            }
        }

        void UpdateDepthCameraPropertiesAccordingToBeam()
        {
            Debug.Assert(m_Master);
            Debug.Assert(m_DepthCamera);

            float apexDist = m_Master.coneApexOffsetZ;
            m_DepthCamera.transform.localPosition = m_Master.beamLocalForward * (-apexDist);
            m_DepthCamera.transform.localRotation = m_Master.beamInternalLocalRotation;

            var scale = m_Master.lossyScale;
            if (!Mathf.Approximately(scale.y * scale.z, 0))
            {
                const float kMinNearClipPlane = 0.1f; // should be the same than in shader
                m_DepthCamera.nearClipPlane = Mathf.Max(apexDist, kMinNearClipPlane) * scale.z;
                m_DepthCamera.farClipPlane = (m_Master.maxGeometryDistance + apexDist) * scale.z;
                m_DepthCamera.aspect = scale.x / scale.y;
                m_DepthCamera.fieldOfView = scale.y * m_Master.coneAngle / scale.z;
            }
        }

        public bool HasLayerMaskIssues()
        {
            if(Config.Instance.geometryOverrideLayer)
            {
                int layerBit = 1 << Config.Instance.geometryLayerID;
                return ((layerMask.value & layerBit) == layerBit);
            }
            return false;
        }

        protected override void OnValidateProperties()
        {
            base.OnValidateProperties();
            depthMapResolution = Mathf.Clamp(Mathf.NextPowerOfTwo(depthMapResolution), 8, 2048);
            fadeDistanceToSurface = Mathf.Max(fadeDistanceToSurface, 0f);
            edgeSoftness = Mathf.Max(edgeSoftness, 0f);
            softenPasses = Mathf.Clamp(softenPasses, 1, 4);
            safeModeFadeBoost = Mathf.Clamp(safeModeFadeBoost, 1f, 10f);
            depthDiscontinuityThreshold = Mathf.Clamp(depthDiscontinuityThreshold, 0.001f, 0.5f);
        }

        void InstantiateOrActivateDepthCamera()
        {
            if (m_DepthCamera != null)
            {
                m_DepthCamera.gameObject.SetActive(true); // active it in case it has been disabled by OnDisable()
            }
            else
            {
                m_DepthCamera = new GameObject("Depth Camera").AddComponent<Camera>();

                if (m_DepthCamera && m_Master)
                {
                    m_DepthCamera.enabled = false;
                    m_DepthCamera.cullingMask = layerMask;
                    m_DepthCamera.clearFlags = CameraClearFlags.Depth;
                    m_DepthCamera.depthTextureMode = DepthTextureMode.Depth;
                    m_DepthCamera.renderingPath = RenderingPath.VertexLit; // faster
                    m_DepthCamera.useOcclusionCulling = useOcclusionCulling;
                    m_DepthCamera.gameObject.hideFlags = Consts.Internal.ProceduralObjectsHideFlags;
                    m_DepthCamera.transform.SetParent(transform, false);

                    var rt = new RenderTexture(depthMapResolution, depthMapResolution, 16, RenderTextureFormat.Depth);
                    // Билинейная фильтрация при выборке шейдером даёт дополнительное сглаживание даже без фильтра
                    rt.filterMode = softEdges ? FilterMode.Bilinear : FilterMode.Point;
                    rt.wrapMode = TextureWrapMode.Clamp;
                    m_DepthCamera.targetTexture = rt;

                    UpdateDepthCameraPropertiesAccordingToBeam();

#if UNITY_EDITOR
                    UnityEditor.GameObjectUtility.SetStaticEditorFlags(m_DepthCamera.gameObject, m_Master.GetStaticEditorFlagsForSubObjects());
                    m_DepthCamera.gameObject.SetSameSceneVisibilityStatesThan(m_Master.gameObject);
#endif
                }
            }
        }

        protected override void OnEnablePostValidate()
        {
            InstantiateOrActivateDepthCamera();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_DepthCamera) m_DepthCamera.gameObject.SetActive(false);
        }

        protected override void Awake()
        {
            base.Awake();

#if UNITY_EDITOR
            MarkMaterialAsDirty();
#endif
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            DestroyDepthCamera();
            ReleaseSoftenResources();

#if UNITY_EDITOR
            MarkMaterialAsDirty();
#endif
        }

        void DestroyDepthCamera()
        {
            if (m_DepthCamera)
            {
                if (m_DepthCamera.targetTexture)
                {
                    m_DepthCamera.targetTexture.Release();
                    DestroyImmediate(m_DepthCamera.targetTexture);
                    m_DepthCamera.targetTexture = null;
                }

                DestroyImmediate(m_DepthCamera.gameObject); // Make sure to delete the GAO
                m_DepthCamera = null;
            }
        }

        protected override void OnModifyMaterialCallback(MaterialModifier.Interface owner)
        {
            Debug.Assert(owner != null);

            // ВАЖНО: в режиме Filtered шейдер получает RFloat-текстуру вместо Depth-текстуры.
            // Если в вашей версии шейдера VLB глубина декодируется через DECODE_EYEDEPTH/LinearEyeDepth,
            // для Filtered-режима может потребоваться поправить сэмплирование в шейдере
            // (читать значение из .r как линейную глубину 0..1). В режиме Safe ничего менять не нужно.
            var tex = OutputDepthTexture;
            owner.SetMaterialProp(ShaderProperties.DynamicOcclusionDepthTexture, tex);

            float fade = fadeDistanceToSurface;
            if (softEdges && softEdgeMode == SoftEdgeMode.Safe)
                fade = Mathf.Max(fade, 0.01f) * safeModeFadeBoost;   // растянутый переход = мягкий край без правок шейдера

            owner.SetMaterialProp(ShaderProperties.DynamicOcclusionDepthProps, fade);
        }

#if UNITY_EDITOR
        bool m_NeedToReinstantiateDepthCamera = false;

        void MarkMaterialAsDirty()
        {
            // when adding/removing this component in editor, we might need to switch from a GPU Instanced material to a custom one,
            // since this feature doesn't support GPU Instancing
            if (!Application.isPlaying)
                m_Master._EditorSetBeamGeomDirty();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            m_NeedToReinstantiateDepthCamera = true;
        }

        void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                if (m_NeedToReinstantiateDepthCamera)
                {
                    DestroyDepthCamera();
                    ReleaseSoftenResources();
                    InstantiateOrActivateDepthCamera();
                    m_NeedToReinstantiateDepthCamera = false;
                }

                if(m_Master && m_Master.enabled)
                    ProcessOcclusion(ProcessOcclusionSource.EditorUpdate);
            }
        }
#endif // UNITY_EDITOR
    }
}