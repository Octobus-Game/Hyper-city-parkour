Shader "Custom/MirrorWithTextures"
{
    Properties
    {
        _MainTex ("Основная текстура", 2D) = "white" {}
        _NormalMap ("Карта нормалей", 2D) = "bump" {}
        _HeightMap ("Карта высот", 2D) = "gray" {}
        _HeightScale ("Сила высот", Range(0, 0.1)) = 0.02
        _MirrorStrength ("Сила зеркала", Range(0, 1)) = 0.5
        _Smoothness ("Гладкость", Range(0, 1)) = 0.9
        _Metallic ("Металличность", Range(0, 1)) = 0.8
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _HeightMap;
        sampler2D _CameraReflectionTexture;
        
        float _HeightScale;
        float _MirrorStrength;
        float _Smoothness;
        float _Metallic;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float4 screenPos;
            float3 viewDir;
            float3 worldRefl;
            INTERNAL_DATA
        };
        
        void vert(inout appdata_full v)
        {
            // Параллакс маппинг на основе карты высот
            float height = tex2Dlod(_HeightMap, float4(v.texcoord.xy, 0, 0)).r;
            v.vertex.xyz += v.normal * (height - 0.5) * _HeightScale;
        }
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Параллакс маппинг для текстур
            float2 parallaxOffset = ParallaxOffset(
                tex2D(_HeightMap, IN.uv_MainTex).r,
                _HeightScale,
                IN.viewDir
            );
            float2 uv = IN.uv_MainTex + parallaxOffset;
            
            // Основная текстура
            fixed4 mainTex = tex2D(_MainTex, uv);
            
            // Нормали
            fixed3 normalTex = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            
            // Зеркальное отражение
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            
            // Искажение UV на основе нормалей для более реалистичного отражения
            screenUV += normalTex.xy * 0.1;
            
            // Получаем отражение окружения через reflection probe
            float3 worldRefl = WorldReflectionVector(IN, normalTex);
            fixed4 reflectionColor = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, worldRefl);
            reflectionColor.rgb = DecodeHDR(reflectionColor, unity_SpecCube0_HDR);
            
            // Смешиваем текстуру с отражением
            fixed4 finalColor = lerp(mainTex, fixed4(reflectionColor.rgb, 1), _MirrorStrength);
            
            o.Albedo = finalColor.rgb;
            o.Normal = normalTex;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = mainTex.a;
        }
        ENDCG
    }
    
    FallBack "Standard"
}

