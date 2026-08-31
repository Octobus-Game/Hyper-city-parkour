Shader "Ultimate 10+ Shaders/Ocean"
{
    Properties
    {
        _Color ("Color", Color) = (0.0,0.25,0.35,0.0)
        _Normal1 ("Normal Map (1)", 2D) = "white" {}
        _NormalStrength1 ("Normal Strength (1)", Range(0, 2)) = 0.17
        _FlowDirection1("Flow Direction (1)", float) = (0.05, 0, 0, 1)
        _Normal2 ("Normal Map (2)", 2D) = "white" {}
        _NormalStrength2 ("Normal Strength (2)", Range(0, 2)) = 0.8
        _FlowDirection2("Flow Direction (2)", float) = (0, 0.05, 0, 1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.6
        _Metallic ("Metallic", Range(0,1)) = 0.2
        
        // Interaction properties
        _InteractionTex ("Interaction Texture", 2D) = "black" {}
        _InteractionStrength ("Interaction Strength", Range(0, 2)) = 1.0
        _InteractionFade ("Interaction Fade Speed", Range(0, 1)) = 0.95
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 150
        Cull [_Cull]
        Lighting Off
        ZWrite On
        
        CGINCLUDE
        #define _GLOSSYENV 1
        #define UNITY_SETUP_BRDF_INPUT SpecularSetup
        #define UNITY_SETUP_BRDF_INPUT MetallicSetup
        ENDCG
        
        CGPROGRAM
        #pragma surface surf Standard alpha
        #ifndef SHADER_API_D3D11
            #pragma target 3.0
        #else
            #pragma target 4.0
        #endif
        
        fixed4 _Color;
        sampler2D _Normal1;
        half _NormalStrength1;
        half2 _FlowDirection1;
        sampler2D _Normal2;
        half _NormalStrength2;
        half2 _FlowDirection2;
        half _Glossiness;
        half _Metallic;
        
        // Interaction variables
        sampler2D _InteractionTex;
        half _InteractionStrength;
        half _InteractionFade;
        
        struct Input
        {
            float2 uv_Normal1;
            float2 uv_Normal2;
            float3 worldPos;
        };
        
        fixed4 pixel;
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Animate normal maps
            IN.uv_Normal1 += _Time.y * _FlowDirection1;
            IN.uv_Normal2 += _Time.y * _FlowDirection2;
            
            // Sample normal maps
            pixel = (tex2D(_Normal1, IN.uv_Normal1) * _NormalStrength1 + 
                     tex2D(_Normal2, IN.uv_Normal2) * _NormalStrength2);
            
            // Sample interaction texture (using world position XZ)
            float2 interactionUV = IN.worldPos.xz * 0.1; // Scale for texture mapping
            fixed4 interaction = tex2D(_InteractionTex, interactionUV);
            
            // Apply interaction to normal map
            float3 baseNormal = UnpackNormal(pixel);
            float3 interactionNormal = float3(interaction.r * 2 - 1, interaction.g * 2 - 1, 0) * _InteractionStrength;
            
            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Normal = normalize(baseNormal + interactionNormal);
        }
        ENDCG
    }
    FallBack "Diffuse"
}