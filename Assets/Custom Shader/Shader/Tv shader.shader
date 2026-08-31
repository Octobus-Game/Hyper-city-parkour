Shader "Custom/TVEffect"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _MainColor ("Main Texture Color", Color) = (1,1,1,1)
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseSpeed ("Noise Speed", Float) = 1.0
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.3
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.5
        _ScanlineCount ("Scanline Count", Float) = 800.0
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _Contrast ("Contrast", Range(0, 2)) = 1.0
        _Saturation ("Saturation", Range(0, 2)) = 1.0
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.3
        _StaticIntensity ("Static Intensity", Range(0, 1)) = 0.1
        _Alpha ("Alpha", Range(0, 1)) = 1.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            fixed4 _MainColor;
            
            float _NoiseSpeed;
            float _NoiseIntensity;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _Brightness;
            float _Contrast;
            float _Saturation;
            float _VignetteIntensity;
            float _StaticIntensity;
            float _Alpha;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            // Функция для создания случайного шума
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Функция для коррекции цвета
            float3 adjustColor(float3 color, float brightness, float contrast, float saturation)
            {
                // Яркость
                color *= brightness;
                
                // Контраст
                color = (color - 0.5) * contrast + 0.5;
                
                // Насыщенность
                float grey = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(float3(grey, grey, grey), color, saturation);
                
                return color;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Основная текстура с цветовым тинтом
                fixed4 mainCol = tex2D(_MainTex, i.uv) * _MainColor;
                
                // Движущаяся текстура помех (движется вверх)
                float2 noiseUV = i.uv;
                noiseUV.y += _Time.y * _NoiseSpeed;
                fixed4 noiseCol = tex2D(_NoiseTex, noiseUV * _NoiseTex_ST.xy + _NoiseTex_ST.zw);
                
                // Смешиваем основную текстуру с помехами
                float3 finalColor = lerp(mainCol.rgb, noiseCol.rgb, _NoiseIntensity);
                
                // Добавляем сканлайны (горизонтальные линии)
                float scanline = sin(i.uv.y * _ScanlineCount) * 0.5 + 0.5;
                scanline = pow(scanline, 3.0);
                finalColor *= lerp(1.0, scanline, _ScanlineIntensity);
                
                // Добавляем статические помехи
                float staticNoise = random(i.uv + _Time.y);
                finalColor = lerp(finalColor, float3(staticNoise, staticNoise, staticNoise), _StaticIntensity);
                
                // Виньетка (затемнение по краям)
                float2 center = i.uv - 0.5;
                float vignette = 1.0 - dot(center, center) * _VignetteIntensity;
                finalColor *= vignette;
                
                // Коррекция цвета
                finalColor = adjustColor(finalColor, _Brightness, _Contrast, _Saturation);
                
                // Ограничиваем значения цвета
                finalColor = saturate(finalColor);
                
                // Финальная альфа с учетом альфы основной текстуры и параметра _Alpha
                float finalAlpha = mainCol.a * _MainColor.a * _Alpha;
                
                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
    
    Fallback "Transparent/Diffuse"
}