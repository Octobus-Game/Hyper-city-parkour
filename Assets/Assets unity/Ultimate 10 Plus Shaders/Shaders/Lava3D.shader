Shader "Ultimate 10+ Shaders/Lava3D"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _HeightMap ("Height Map (Black and White)", 2D) = "bump" {}
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0.0, 3.0)) = 1.0
        _FlowDirection ("Flow Direction", Vector) = (1, 0, 0, 0)
        _Speed ("Speed", float) = 0.25
        _Amplitude ("Amplitude", float) = 1.0

        // Режим смещения вершин
        // 0 = вдоль мировой оси Y (плоскость)
        // 1 = вдоль нормали меша (сфера, цилиндр и т.д.)
        [KeywordEnum(World_Y, Along_Normal)] _DisplaceMode ("Displace Mode", Float) = 0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

        // ── Intersection / Contact effect ──────────────────────────
        [Header(Intersection Effect)]
        [Toggle] _IntersectEnable ("Enable Intersection Effect", Float) = 1.0

        // Режим эффекта пересечения:
        // 0 = линия (заливка одним цветом)
        // 1 = текстура
        [KeywordEnum(Line, Texture)] _IntersectMode ("Intersection Mode", Float) = 0

        [HDR] _IntersectColor ("Intersection Color", Color) = (1, 0.4, 0, 1)
        _IntersectTex ("Intersection Texture", 2D) = "white" {}
        _IntersectTexScale ("Intersection Tex Scale", Float) = 5.0

        // Толщина зоны пересечения в мировых единицах
        _IntersectThreshold ("Intersection Threshold", Range(0.001, 2.0)) = 0.25

        // Резкость края линии (0 = мягкий градиент, 1 = жёсткий край)
        _IntersectSharpness ("Intersection Sharpness", Range(0.0, 1.0)) = 0.5

        // Прокрутка текстуры пересечения
        _IntersectTexSpeed ("Intersection Tex Scroll Speed", Float) = 0.5
    }

    SubShader
    {
        // Используем Transparent queue чтобы иметь доступ к _CameraDepthTexture.
        // При этом сам объект остаётся визуально непрозрачным (Alpha=1),
        // но intersection-эффект смешивается поверх через стандартный forward pass.
        Tags
        {
            "RenderType" = "Transparent"
            "Queue"       = "Transparent"
        }
        LOD 150
        Cull [_Cull]
        ZWrite Off          // Transparent-очередь не пишет в z-буфер по умолчанию

        CGPROGRAM
        // surface-шейдер с alpha-blend и доступом к экранным координатам
        #pragma surface surf Standard alpha:fade addshadow fullforwardshadows
        #pragma vertex vert

        #ifndef SHADER_API_D3D11
            #pragma target 3.0
        #else
            #pragma target 4.0
        #endif

        #include "UnityCG.cginc"

        // ── Основные параметры ────────────────────────────────────────
        fixed4    _Color;
        sampler2D _MainTex;
        sampler2D _HeightMap;
        sampler2D _NormalMap;
        half      _NormalStrength;
        half4     _FlowDirection;
        half      _Speed;
        half      _Amplitude;
        float     _DisplaceMode;

        // ── Intersection параметры ────────────────────────────────────
        float     _IntersectEnable;
        float     _IntersectMode;
        fixed4    _IntersectColor;
        sampler2D _IntersectTex;
        float4    _IntersectTex_ST;
        float     _IntersectTexScale;
        float     _IntersectThreshold;
        float     _IntersectSharpness;
        float     _IntersectTexSpeed;

        // Текстура глубины сцены (заполняется Unity автоматически
        // при включении Camera → DepthTextureMode.Depth)
        sampler2D_float _CameraDepthTexture;

        // ── Input ─────────────────────────────────────────────────────
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_HeightMap;
            float2 uv_NormalMap;

            // Экранные координаты фрагмента (нужны для выборки глубины)
            float4 screenPos;

            // Мировая позиция (для маппинга текстуры пересечения)
            float3 worldPos;
        };

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        // ─── Surface shader ───────────────────────────────────────────
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half2 flowOffset = _FlowDirection.xy * fmod(_Time.y, 1200.0) * _Speed;

            // Albedo
            fixed4 pixel = tex2D(_MainTex, IN.uv_MainTex + flowOffset) * _Color;
            o.Albedo = pixel.rgb;
            o.Alpha  = pixel.a;

            // Normal map
            half3 n = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap + flowOffset));
            n.xy *= _NormalStrength;
            o.Normal = normalize(n);

            // ── Intersection effect ───────────────────────────────────
            if (_IntersectEnable > 0.5)
            {
                // Линейная глубина сцены из depth buffer (0..far)
                float sceneDepth = LinearEyeDepth(
                    SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos))
                );

                // Линейная глубина текущего фрагмента
                float fragDepth = IN.screenPos.w; // screenPos.w = eye-space Z после vert

                // Разница глубин → расстояние до ближайшей поверхности сцены
                float depthDiff = sceneDepth - fragDepth;

                // Нормализуем в диапазон [0..1] в пределах threshold
                float t = saturate(depthDiff / _IntersectThreshold);

                // Применяем резкость: lerp между гладким и step-краем
                float sharpT = lerp(t, step(0.5, t), _IntersectSharpness);

                // intersectFactor: 1 в зоне пересечения, 0 — вдали
                float intersectFactor = 1.0 - sharpT;

                if (intersectFactor > 0.001)
                {
                    fixed4 intersectCol;

                    if (_IntersectMode < 0.5)
                    {
                        // Режим «линия» — сплошной HDR-цвет
                        intersectCol = _IntersectColor;
                    }
                    else
                    {
                        // Режим «текстура» — прокручиваем по мировым XZ
                        float2 texUV = IN.worldPos.xz * _IntersectTexScale
                                     + _Time.y * _IntersectTexSpeed * _FlowDirection.xy;
                        intersectCol = tex2D(_IntersectTex, TRANSFORM_TEX(texUV, _IntersectTex))
                                     * _IntersectColor;
                    }

                    // Смешиваем intersection-цвет поверх albedo
                    o.Albedo  = lerp(o.Albedo, intersectCol.rgb, intersectFactor * intersectCol.a);
                    o.Emission = intersectCol.rgb * intersectFactor * intersectCol.a;
                }
            }
        }

        // ─── Vertex struct ────────────────────────────────────────────
        struct appdata
        {
            float4 vertex    : POSITION;
            float4 tangent   : TANGENT;
            float3 normal    : NORMAL;
            float4 texcoord  : TEXCOORD0;
            float4 texcoord1 : TEXCOORD1;
            fixed4 color     : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // ─── Vertex shader ────────────────────────────────────────────
        void vert (inout appdata v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            half2 flowOffset = _FlowDirection.xy * fmod(_Time.y, 1200.0) * _Speed;

            fixed4 texPixel = tex2Dlod(
                _HeightMap,
                float4(v.texcoord1.xy + flowOffset, 0, 0)
            );
            half displacement = texPixel.r * _Amplitude;

            if (_DisplaceMode < 0.5)
            {
                v.vertex.y += displacement;
            }
            else
            {
                v.vertex.xyz += normalize(v.normal) * displacement;
            }

            // Вычисляем screenPos вручную чтобы он был корректен
            // после смещения вершины
            o.screenPos = ComputeScreenPos(UnityObjectToClipPos(v.vertex));

            // Мировая позиция
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
