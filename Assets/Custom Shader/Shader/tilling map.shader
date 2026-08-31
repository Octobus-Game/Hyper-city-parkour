Shader "Custom/HashTiling"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _HeightMap ("Height Map", 2D) = "gray" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _HeightScale ("Height Scale", Range(0, 0.1)) = 0.02
        _HeightMinSamples ("Min Parallax Samples", Range(4, 16)) = 4
        _HeightMaxSamples ("Max Parallax Samples", Range(8, 64)) = 16
        _Tiling ("Tiling", Float) = 1.0
        _Rotation ("Enable Rotation", Range(0, 1)) = 0
        _Color ("Base Color", Color) = (1,1,1,1)
        _ColorVariation ("Color Variation", Range(0, 1)) = 0
        _Brightness ("Brightness", Range(0, 2)) = 1
        _Contrast ("Contrast", Range(0, 2)) = 1
        _Saturation ("Saturation", Range(0, 2)) = 1
        _Hue ("Hue Shift", Range(-180, 180)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _HeightMap;
        float _NormalStrength;
        float _HeightScale;
        float _HeightMinSamples;
        float _HeightMaxSamples;
        float _Tiling;
        float _Rotation;
        fixed4 _Color;
        float _ColorVariation;
        float _Brightness;
        float _Contrast;
        float _Saturation;
        float _Hue;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDirTS; // Направление взгляда в tangent space
        };

        // ─── Вершинный шейдер: вычисляем viewDir в tangent space ──────────────
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.uv_MainTex = v.texcoord.xy;

            // Строим матрицу TBN (object space → tangent space)
            float3 T = normalize(v.tangent.xyz);
            float3 N = normalize(v.normal);
            float3 B = cross(N, T) * v.tangent.w;

            // Переводим позицию камеры в object space и считаем viewDir
            float3 objCamPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1)).xyz;
            float3 viewDir   = v.vertex.xyz - objCamPos;

            // Проецируем в tangent space
            o.viewDirTS = float3(dot(viewDir, T), dot(viewDir, B), dot(viewDir, N));
        }

        // ─── Hash / rotate / normal-rotate (без изменений) ────────────────────
        float2 hash22(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
            p3 += dot(p3, p3.yzx + 19.19);
            return frac((p3.xx + p3.yz) * p3.zy);
        }

        float2 rotate(float2 p, float angle)
        {
            float c = cos(angle);
            float s = sin(angle);
            return float2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        float3 rotateNormal(float3 normal, float angle)
        {
            float c = cos(angle);
            float s = sin(angle);
            return float3(
                normal.x * c - normal.y * s,
                normal.x * s + normal.y * c,
                normal.z
            );
        }

        // ─── RGB ↔ HSV ────────────────────────────────────────────────────────
        float3 rgb2hsv(float3 c)
        {
            float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
            float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
            float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10;
            return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
        }

        float3 hsv2rgb(float3 c)
        {
            float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
            float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
            return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
        }

        // ─── Коррекция цвета ──────────────────────────────────────────────────
        fixed3 AdjustColor(fixed3 color, float2 hash)
        {
            color *= _Color.rgb;
            color *= _Brightness;
            color  = (color - 0.5) * _Contrast + 0.5;

            float3 hsv = rgb2hsv(color);
            hsv.y *= _Saturation;
            hsv.x  = frac(hsv.x + _Hue / 360.0);

            if (_ColorVariation > 0)
            {
                hsv.x = frac(hsv.x + (hash.x - 0.5) * _ColorVariation * 0.3);
                hsv.z = saturate(hsv.z + (hash.y - 0.5) * _ColorVariation * 0.2);
            }

            return hsv2rgb(hsv);
        }

        // ─── Вспомогательная функция: Hash UV для тайла ───────────────────────
        // Возвращает финальный UV и угол поворота для данного тайла.
        float2 GetTileUV(float2 uv, float tiling, out float angle, out float2 tileHash)
        {
            float2 scaledUV = uv * tiling;
            float2 tileID   = floor(scaledUV);
            float2 localUV  = frac(scaledUV);

            tileHash = hash22(tileID);

            angle = 0;
            if (_Rotation > 0.5)
            {
                angle   = tileHash.x * 6.28318;
                localUV = rotate(localUV - 0.5, angle) + 0.5;
            }

            return frac(localUV + tileHash);
        }

        // ─── Parallax Occlusion Mapping для одного тайла ──────────────────────
        // Принимает уже посчитанный tileUV и viewDirTS, возвращает смещённый UV.
        float2 ParallaxTileUV(float2 tileUV, float3 viewDirTS)
        {
            // Число шагов зависит от угла взгляда (больше угол — больше шагов)
            float numSamples = lerp(_HeightMaxSamples, _HeightMinSamples,
                                    abs(normalize(viewDirTS).z));

            float layerDepth  = 1.0 / numSamples;
            float2 uvDelta    = (viewDirTS.xy / viewDirTS.z) * _HeightScale / numSamples;

            float  curDepth   = 0.0;
            float2 curUV      = tileUV;
            float  curHeight  = tex2D(_HeightMap, curUV).r;

            // Шагаем по слоям пока высота карты не станет меньше текущей глубины
            [loop]
            for (int i = 0; i < (int)numSamples; i++)
            {
                if (curHeight <= curDepth) break;
                curDepth  += layerDepth;
                curUV     -= uvDelta;       // движемся против viewDir
                curHeight  = tex2D(_HeightMap, curUV).r;
            }

            // Интерполяция между предыдущим и текущим слоем (Parallax Occlusion)
            float2 prevUV      = curUV + uvDelta;
            float  prevHeight  = tex2D(_HeightMap, prevUV).r;
            float  prevDepth   = curDepth - layerDepth;

            float  afterDelta  = curHeight  - curDepth;
            float  beforeDelta = prevHeight - prevDepth;
            float  weight      = afterDelta / (afterDelta - beforeDelta);

            return lerp(curUV, prevUV, weight);
        }

        // ─── Albedo с Hash Tiling + Parallax ─────────────────────────────────
        fixed4 SampleHashTiling(sampler2D tex, float2 uv, float tiling,
                                float3 viewDirTS, out float angle)
        {
            float2 tileHash;
            float2 tileUV = GetTileUV(uv, tiling, angle, tileHash);

            // Parallax смещение в пространстве тайла
            tileUV = ParallaxTileUV(tileUV, viewDirTS);

            fixed4 texColor  = tex2D(tex, tileUV);
            texColor.rgb     = AdjustColor(texColor.rgb, tileHash);
            return texColor;
        }

        // ─── Normal Map с Hash Tiling + Parallax ─────────────────────────────
        float3 SampleNormalHashTiling(sampler2D normalTex, float2 uv, float tiling,
                                      float3 viewDirTS, float angle)
        {
            float2 tileHash;
            float  dummyAngle;
            float2 tileUV = GetTileUV(uv, tiling, dummyAngle, tileHash);

            // Тот же Parallax сдвиг, что и для Albedo
            tileUV = ParallaxTileUV(tileUV, viewDirTS);

            float3 normal  = UnpackNormal(tex2D(normalTex, tileUV));
            normal.xy     *= _NormalStrength;

            if (_Rotation > 0.5)
                normal = rotateNormal(normal, angle);

            return normalize(normal);
        }

        // ─── Surf ─────────────────────────────────────────────────────────────
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float angle;
            fixed4 c = SampleHashTiling(_MainTex, IN.uv_MainTex, _Tiling,
                                        IN.viewDirTS, angle);
            float3 normal = SampleNormalHashTiling(_NormalMap, IN.uv_MainTex, _Tiling,
                                                   IN.viewDirTS, angle);
            o.Albedo = c.rgb;
            o.Normal = normal;
            o.Alpha  = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}