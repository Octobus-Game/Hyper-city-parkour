// MirrorReflection.shader
// Настоящее зеркало для Built-in Render Pipeline.
// Отражение берётся из RenderTexture, которую рендерит камера-зеркало
// (см. MirrorReflectionCamera.cs). Шейдер сам находит текстуру по имени
// свойства "_ReflectionTex", которое скрипт задаёт через Renderer.material.

Shader "Custom/MirrorReflection"
{
    Properties
    {
        [Header(Reflection)]
        _ReflectionTex ("Reflection Texture (авто, задаётся скриптом)", 2D) = "black" {}
        _ReflectionTint ("Reflection Tint", Color) = (1,1,1,1)
        _ReflectionStrength ("Reflection Strength", Range(0,1)) = 1.0

        [Header(Surface Tint)]
        _BaseColor ("Base Tint (подложка под отражением)", Color) = (0.05,0.05,0.05,1)

        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power (яркость по краю)", Range(0.1, 8)) = 2.0
        _FresnelMin ("Fresnel Min (мин. отражение)", Range(0,1)) = 0.4
        _FresnelMax ("Fresnel Max (макс. отражение)", Range(0,1)) = 1.0

        [Header(Normal Map Optional)]
        _BumpMap ("Normal Map (опционально)", 2D) = "bump" {}
        _BumpStrength ("Normal Strength", Range(0,2)) = 0.0

        [Header(Distortion)]
        _DistortionStrength ("UV Distortion (лёгкое искажение)", Range(0,0.05)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _ReflectionTex;
        sampler2D _BumpMap;

        fixed4 _ReflectionTint;
        fixed4 _BaseColor;
        half _ReflectionStrength;
        half _FresnelPower;
        half _FresnelMin;
        half _FresnelMax;
        half _BumpStrength;
        half _DistortionStrength;

        struct Input
        {
            float2 uv_BumpMap;
            float4 screenPos;
            float3 viewDir;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v)
        {
            // ничего специального, но оставлено для расширения (волны/анимация вершин)
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Нормаль (можно оставить плоской, если _BumpStrength = 0)
            half3 normalTS = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            normalTS.xy *= _BumpStrength;
            o.Normal = normalize(normalTS);

            // Экранные координаты для сэмплинга отражения + лёгкое искажение по нормали
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            screenUV += normalTS.xy * _DistortionStrength;

            fixed4 reflection = tex2D(_ReflectionTex, screenUV) * _ReflectionTint;

            // Fresnel: больше отражения под острым углом обзора (как в реальных зеркалах/стекле)
            half fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), _FresnelPower);
            half fresnelFactor = lerp(_FresnelMin, _FresnelMax, fresnel);

            half finalReflectivity = saturate(_ReflectionStrength * fresnelFactor);

            fixed3 finalColor = lerp(_BaseColor.rgb, reflection.rgb, finalReflectivity);

            o.Albedo = finalColor;
            o.Metallic = 0.0;
            o.Smoothness = 0.0;
            o.Emission = finalColor * finalReflectivity; // чтобы отражение не "гасло" от освещения сцены
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
