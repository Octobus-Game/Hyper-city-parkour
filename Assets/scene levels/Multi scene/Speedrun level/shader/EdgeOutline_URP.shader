// ============================================================
//  EdgeOutline_URP.shader
//  Метод: Inverted Hull для Universal Render Pipeline (URP)
//
//  Требования:
//   - Unity 2021+ с URP пакетом
//   - В настройках URP Renderer добавьте
//     "Renderer Feature" → "Render Objects" если нужен доп. контроль
//
//  Использование:
//   1. Создайте материал с этим шейдером
//   2. Назначьте объекту
//   3. Для lit-прохода используйте отдельный URP/Lit материал
//      или объедините оба прохода в один объект через дополнительный
//      материал в Mesh Renderer (Materials → Size = 2)
// ============================================================
Shader "Custom/EdgeOutline_URP"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color)  = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.05)) = 0.005

        // Base surface (используется во втором pass)
        _BaseColor    ("Base Color",    Color)  = (1, 1, 1, 1)
        _BaseMap      ("Base Texture",  2D)     = "white" {}
        _Smoothness   ("Smoothness",    Range(0, 1)) = 0.5
        _Metallic     ("Metallic",      Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"             = "Opaque"
            "RenderPipeline"         = "UniversalPipeline"
            "Queue"                  = "Geometry"
            "UniversalMaterialType"  = "Lit"
        }
        LOD 300

        // ─── PASS 1 — Outline ────────────────────────────────────────────
        Pass
        {
            Name "Outline"

            Cull  Front
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   OutlineVert
            #pragma fragment OutlineFrag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Позиция в clip-space
                VertexPositionInputs posInputs =
                    GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs =
                    GetVertexNormalInputs(IN.normalOS);

                float3 normalWS  = normInputs.normalWS;
                float4 clipPos   = posInputs.positionCS;

                // Смещение нормали в clip-space с коррекцией aspect ratio
                float3 clipNormal = mul((float3x3)UNITY_MATRIX_VP, normalWS);
                float2 offset     = normalize(clipNormal.xy)
                                    * _OutlineWidth * clipPos.w;
                offset.x /= _ScreenParams.x / _ScreenParams.y;

                clipPos.xy += offset;
                OUT.positionHCS = clipPos;
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ─── PASS 2 — Lit surface (URP ForwardLit) ───────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull  Back
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   LitVert
            #pragma fragment LitFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings LitVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs =
                    GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs =
                    GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = normInputs.normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 LitFrag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap,
                                     sampler_BaseMap, IN.uv) * _BaseColor;

                InputData inputData = (InputData)0;
                inputData.positionWS     = IN.positionWS;
                inputData.normalWS       = normalize(IN.normalWS);
                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord    =
                    TransformWorldToShadowCoord(IN.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo       = texColor.rgb;
                surfaceData.alpha        = texColor.a;
                surfaceData.metallic     = _Metallic;
                surfaceData.smoothness   = _Smoothness;
                surfaceData.normalTS     = half3(0, 0, 1);
                surfaceData.occlusion    = 1;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb   = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // Shadow caster (нужен для корректного самозатенения)
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Universal Render Pipeline/Lit"
}
