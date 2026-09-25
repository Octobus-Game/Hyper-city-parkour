// Фильтр смягчения карты глубины для DynamicOcclusionDepthBuffer.
// Делает мягкий край окклюзии: взвешенное размытие (Гаусс) по глубине
// с "защитой от призраков" - значения глубины, сильно отличающиеся от центра,
// не смешиваются, а обрабатываются как отдельный слой.
Shader "Hidden/VLB/DepthSoftenFilter"
{
    Properties
    {
        _MainTex ("Depth Source", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            // x = радиус размытия в текселях, y = порог "разрыва" глубины, z = режим (0 = blur, 1 = min/эрозия)
            float4 _VLB_SoftenParams;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.texcoord;
                return o;
            }

            // Depth-текстура камеры (RenderTextureFormat.Depth) при выборке в шейдере отдаёт глубину в .r (0..1).
            // Если на вашей платформе/версии Unity результат выглядит инвертированным (Reversed-Z, DX11/Metal/Vulkan),
            // переключите USE_REVERSED_Z ниже - фильтр работает одинаково, меняется только направление "ближе/дальше".
            //#define USE_REVERSED_Z
            inline float SampleDepth(float2 uv)
            {
                float d = tex2D(_MainTex, uv).r;
            #if defined(USE_REVERSED_Z)
                d = 1.0 - d;
            #endif
                return d;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float radius = _VLB_SoftenParams.x;
                float edge   = _VLB_SoftenParams.y;
                float2 texel = _MainTex_TexelSize.xy * radius;

                // Гауссовы веса 5x5 (сепарабельно приближённые)
                const float w[3] = { 0.375, 0.25, 0.0625 };

                float center = SampleDepth(i.uv);

                float sum = 0;
                float wsum = 0;

                // Сначала находим минимальную глубину в окрестности (ближайший перекрывающий объект)
                float minD = center;
                [unroll] for (int y = -2; y <= 2; y++)
                {
                    [unroll] for (int x = -2; x <= 2; x++)
                    {
                        float d = SampleDepth(i.uv + float2(x, y) * texel * 0.5);
                        minD = min(minD, d);
                    }
                }

                // Размытие с отбрасыванием отсчётов, слишком далёких от ближайшего слоя
                [unroll] for (int y2 = -2; y2 <= 2; y2++)
                {
                    [unroll] for (int x2 = -2; x2 <= 2; x2++)
                    {
                        float d = SampleDepth(i.uv + float2(x2, y2) * texel);
                        float wg = w[abs(x2)] * w[abs(y2)];

                        // если отсчёт дальше минимального слоя больше чем на edge - считаем его
                        // "фоном" и подменяем ближайшим слоем, чтобы не было призраков
                        float use = (d - minD) > edge ? minD : d;
                        sum  += use * wg;
                        wsum += wg;
                    }
                }

                float result = sum / wsum;
                return fixed4(result, result, result, 1);
            }
            ENDCG
        }
    }
}
