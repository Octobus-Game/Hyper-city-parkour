// ============================================================
//  SobelEdgeDetection.shader
//  Постэффект: детектор граней через оператор Собеля
//  по буферу глубины + нормалей (Screen-Space Edge Detection)
//
//  Совместимость: Built-in RP (C#-скрипт ниже для применения)
//  Для URP используйте как Renderer Feature Blit Material
//
//  C# скрипт (добавьте на камеру):
//  ──────────────────────────────────────────────────────────
//  using UnityEngine;
//  [ExecuteInEditMode, ImageEffectAllowedInSceneView]
//  [RequireComponent(typeof(Camera))]
//  public class SobelEdgeEffect : MonoBehaviour
//  {
//      public Material edgeMaterial;
//      void OnRenderImage(RenderTexture src, RenderTexture dst)
//          => Graphics.Blit(src, dst, edgeMaterial);
//  }
//  ──────────────────────────────────────────────────────────
// ============================================================
Shader "Custom/SobelEdgeDetection"
{
    Properties
    {
        _MainTex      ("Screen Texture",    2D)    = "white" {}
        _EdgeColor    ("Edge Color",        Color) = (0, 0, 0, 1)
        _BgColor      ("Background Color",  Color) = (1, 1, 1, 0)
        _EdgeStrength ("Edge Strength",     Range(0, 10))   = 2.0
        _DepthThresh  ("Depth Threshold",   Range(0, 1))    = 0.01
        _NormalThresh ("Normal Threshold",  Range(0, 1))    = 0.4
        _BlendScene   ("Blend with Scene",  Range(0, 1))    = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthNormalsTexture; // Built-in: depth+normals

            float4 _MainTex_TexelSize;
            float4 _EdgeColor;
            float4 _BgColor;
            float  _EdgeStrength;
            float  _DepthThresh;
            float  _NormalThresh;
            float  _BlendScene;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                // 8 соседних UV для ядра Собеля
                float2 uv00 : TEXCOORD1;
                float2 uv01 : TEXCOORD2;
                float2 uv02 : TEXCOORD3;
                float2 uv10 : TEXCOORD4;
                float2 uv12 : TEXCOORD5;
                float2 uv20 : TEXCOORD6;
                float2 uv21 : TEXCOORD7;
                float2 uv22 : TEXCOORD8;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos  = UnityObjectToClipPos(v.vertex);
                o.uv   = v.uv;
                float2 d = _MainTex_TexelSize.xy;

                o.uv00 = v.uv + float2(-d.x, -d.y);
                o.uv01 = v.uv + float2(  0,  -d.y);
                o.uv02 = v.uv + float2( d.x, -d.y);
                o.uv10 = v.uv + float2(-d.x,   0);
                o.uv12 = v.uv + float2( d.x,   0);
                o.uv20 = v.uv + float2(-d.x,  d.y);
                o.uv21 = v.uv + float2(  0,   d.y);
                o.uv22 = v.uv + float2( d.x,  d.y);
                return o;
            }

            // Декодируем глубину и нормаль из _CameraDepthNormalsTexture
            void DecodeDepthNormal(float2 uv, out float depth, out float3 normal)
            {
                float4 cdn = tex2D(_CameraDepthNormalsTexture, uv);
                DecodeDepthNormal(cdn, depth, normal);
            }

            // Собель: возвращает силу края [0..1]
            float SobelDepth(float d00, float d01, float d02,
                             float d10,             float d12,
                             float d20, float d21, float d22)
            {
                float gx = -d00 - 2*d10 - d20 + d02 + 2*d12 + d22;
                float gy = -d00 - 2*d01 - d02 + d20 + 2*d21 + d22;
                return sqrt(gx*gx + gy*gy);
            }

            float SobelNormal(float3 n00, float3 n01, float3 n02,
                              float3 n10,              float3 n12,
                              float3 n20, float3 n21, float3 n22)
            {
                float3 gx = -n00 - 2*n10 - n20 + n02 + 2*n12 + n22;
                float3 gy = -n00 - 2*n01 - n02 + n20 + 2*n21 + n22;
                return length(gx) + length(gy);
            }

            float4 frag(v2f i) : SV_Target
            {
                // Считываем глубины и нормали для 3×3 ядра
                float  d00, d01, d02, d10, d12, d20, d21, d22;
                float3 n00, n01, n02, n10, n12, n20, n21, n22;

                DecodeDepthNormal(i.uv00, d00, n00);
                DecodeDepthNormal(i.uv01, d01, n01);
                DecodeDepthNormal(i.uv02, d02, n02);
                DecodeDepthNormal(i.uv10, d10, n10);
                DecodeDepthNormal(i.uv12, d12, n12);
                DecodeDepthNormal(i.uv20, d20, n20);
                DecodeDepthNormal(i.uv21, d21, n21);
                DecodeDepthNormal(i.uv22, d22, n22);

                float edgeD = SobelDepth(d00, d01, d02,
                                         d10,       d12,
                                         d20, d21, d22);

                float edgeN = SobelNormal(n00, n01, n02,
                                          n10,       n12,
                                          n20, n21, n22);

                // Объединяем depth и normal edges
                float edge = step(_DepthThresh,  edgeD * _EdgeStrength)
                           + step(_NormalThresh, edgeN * _EdgeStrength);
                edge = saturate(edge);

                // Исходный цвет сцены
                float4 scene = tex2D(_MainTex, i.uv);

                // Итоговый цвет: lerp между оригиналом и edge overlay
                float4 edgeColor = lerp(float4(0,0,0,0), _EdgeColor, edge);
                float4 result    = lerp(scene, _BgColor, 1 - _BlendScene);
                result           = lerp(result, _EdgeColor, edge);

                return result;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
