Shader "Custom/HeatHaze"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _ScrollSpeed ("Scroll Speed", Range(0, 2)) = 0.5
        _HeatIntensity ("Heat Intensity", Range(0, 2)) = 1.0
        _Temperature ("Temperature", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        GrabPass { "_GrabTexture" }
        
        Pass
        {
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
                float4 grabPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            sampler2D _NoiseTex;
            sampler2D _GrabTexture;
            float4 _MainTex_ST;
            float _DistortionStrength;
            float _ScrollSpeed;
            float _HeatIntensity;
            float _Temperature;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Создаем движущийся шум для искажения
                float2 noiseUV1 = i.uv + _Time.y * _ScrollSpeed * float2(0.1, 0.2);
                float2 noiseUV2 = i.uv + _Time.y * _ScrollSpeed * float2(-0.15, 0.1);
                
                float noise1 = tex2D(_NoiseTex, noiseUV1).r;
                float noise2 = tex2D(_NoiseTex, noiseUV2).g;
                
                // Комбинируем шумы для более сложного паттерна
                float2 distortion = (noise1 + noise2 - 1.0) * _DistortionStrength * _HeatIntensity * _Temperature;
                
                // Добавляем вертикальное искажение (горячий воздух поднимается)
                distortion.y += sin(_Time.y * 2.0 + i.uv.x * 10.0) * _DistortionStrength * 0.5 * _Temperature;
                
                // Применяем искажение к grab texture
                float2 grabUV = i.grabPos.xy / i.grabPos.w + distortion;
                fixed4 grabColor = tex2D(_GrabTexture, grabUV);
                
                // Добавляем легкий оранжевый оттенок для эффекта жары
                float3 heatTint = lerp(float3(1, 1, 1), float3(1.2, 0.9, 0.7), _Temperature * 0.3);
                grabColor.rgb *= heatTint;
                
                return grabColor;
            }
            ENDCG
        }
    }
}