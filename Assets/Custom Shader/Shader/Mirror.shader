Shader "Custom/Mirror"
{
    Properties
    {
        _ReflectionTex ("Reflection", 2D) = "white" {}
        _ReflectionIntensity ("Reflection Intensity", Range(0, 1)) = 1.0
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.9
        _Metallic ("Metallic", Range(0,1)) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _ReflectionTex;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;
        half _ReflectionIntensity;
        
        struct Input
        {
            float4 screenPos;
            float3 worldRefl;
            INTERNAL_DATA
        };
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Получаем координаты экрана
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            screenUV = (screenUV + 1.0) * 0.5;
            
            // Отражаем Y координату для правильного отражения
            screenUV.y = 1.0 - screenUV.y;
            
            // Сэмплируем текстуру отражения
            fixed4 reflection = tex2D(_ReflectionTex, screenUV);
            
            // Применяем цвет и интенсивность отражения
            o.Albedo = reflection.rgb * _Color.rgb * _ReflectionIntensity;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}