/*
        ░█████╗░░█████╗░███████╗░█████╗░███╗░░██╗  ░██████╗██╗░░██╗░█████╗░██████╗░███████╗██████╗░
        ██╔══██╗██╔══██╗██╔════╝██╔══██╗████╗░██║  ██╔════╝██║░░██║██╔══██╗██╔══██╗██╔════╝██╔══██╗
        ██║░░██║██║░░╚═╝█████╗░░███████║██╔██╗██║  ╚█████╗░███████║███████║██║░░██║█████╗░░██████╔╝
        ██║░░██║██║░░██╗██╔══╝░░██╔══██║██║╚████║  ░╚═══██╗██╔══██║██╔══██║██║░░██║██╔══╝░░██╔══██╗
        ╚█████╗░╚█████╔╝███████╗██║░░██║██║░╚███║  ██████╔╝██║░░██║██║░░██║██████╔╝███████╗██║░░██║
        ░╚════╝░░╚════╝░╚══════╝╚═╝░░╚═╝╚═╝░░╚══╝  ╚═════╝░╚═╝░░╚═╝╚═╝░░╚═╝╚═════╝░╚══════╝╚═╝░░╚═╝

                █▀▀▄ █──█ 　 ▀▀█▀▀ █──█ █▀▀ 　 ░█▀▀▄ █▀▀ ▀█─█▀ █▀▀ █── █▀▀█ █▀▀█ █▀▀ █▀▀█ 
                █▀▀▄ █▄▄█ 　 ─░█── █▀▀█ █▀▀ 　 ░█─░█ █▀▀ ─█▄█─ █▀▀ █── █──█ █──█ █▀▀ █▄▄▀ 
                ▀▀▀─ ▄▄▄█ 　 ─░█── ▀──▀ ▀▀▀ 　 ░█▄▄▀ ▀▀▀ ──▀── ▀▀▀ ▀▀▀ ▀▀▀▀ █▀▀▀ ▀▀▀ ▀─▀▀
____________________________________________________________________________________________________________________________________________

        ▄▀█ █▀ █▀ █▀▀ ▀█▀ ▀   █░█ █░░ ▀█▀ █ █▀▄▀█ ▄▀█ ▀█▀ █▀▀   ▄█ █▀█ ▄█▄   █▀ █░█ ▄▀█ █▀▄ █▀▀ █▀█ █▀
        █▀█ ▄█ ▄█ ██▄ ░█░ ▄   █▄█ █▄▄ ░█░ █ █░▀░█ █▀█ ░█░ ██▄   ░█ █▄█ ░▀░   ▄█ █▀█ █▀█ █▄▀ ██▄ █▀▄ ▄█
____________________________________________________________________________________________________________________________________________
License:
    The license is ATTRIBUTION 3.0

    More license info here:
        https://creativecommons.org/licenses/by/3.0/
____________________________________________________________________________________________________________________________________________
Enhanced Version with new features:
    - Foam generation on wave peaks
    - Distance-based opacity for depth perception
    - Fresnel reflection effect
    - Wave height displacement
    - Caustics simulation
    - Wind direction control
    - Day/night color variation
    - Edge foam for shoreline effect
____________________________________________________________________________________________________________________________________________
*/

Shader "Ultimate 10+ Shaders/Enhanced Ocean"
{
    Properties
    {
        [Header(Basic Ocean)]
        _Color ("Ocean Color (Day)", Color) = (0.0,0.25,0.35,0.8)
        _NightColor ("Ocean Color (Night)", Color) = (0.0,0.1,0.2,0.9)
        _DeepColor ("Deep Water Color", Color) = (0.0,0.1,0.2,1.0)
        
        [Header(Wave Settings)]
        _WaveHeight ("Wave Height", Range(0, 5)) = 0.5
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.0
        _WindDirection ("Wind Direction", Vector) = (1, 0, 1, 0)
        
        [Header(Normal Maps)]
        _Normal1 ("Normal Map (1)", 2D) = "bump" {}
        _NormalStrength1 ("Normal Strength (1)", Range(0, 2)) = 0.17
        _FlowDirection1("Flow Direction (1)", Vector) = (0.05, 0, 0, 1)

        _Normal2 ("Normal Map (2)", 2D) = "bump" {}
        _NormalStrength2 ("Normal Strength (2)", Range(0, 2)) = 0.8
        _FlowDirection2("Flow Direction (2)", Vector) = (0, 0.05, 0, 1)
        
        [Header(Foam)]
        _FoamTexture ("Foam Texture", 2D) = "white" {}
        _FoamColor ("Foam Color", Color) = (1,1,1,1)
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.8
        _FoamSoftness ("Foam Softness", Range(0, 1)) = 0.2
        _EdgeFoamWidth ("Edge Foam Width", Range(0, 5)) = 1.0
        
        [Header(Reflection and Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0, 10)) = 3.0
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.8
        
        [Header(Caustics)]
        _CausticsTexture ("Caustics Texture", 2D) = "black" {}
        _CausticsStrength ("Caustics Strength", Range(0, 2)) = 0.5
        _CausticsSpeed ("Caustics Speed", Range(0, 5)) = 1.5
        
        [Header(Depth and Distance)]
        _DepthFade ("Depth Fade Distance", Range(0, 100)) = 10.0
        _DistanceFade ("Distance Fade", Range(0, 1000)) = 200.0
        
        [Header(Material Properties)]
        _Glossiness ("Smoothness", Range(0,1)) = 0.85
        _Metallic ("Metallic", Range(0,1)) = 0.1
        
        [Header(Day Night Cycle)]
        _DayNightBlend ("Day/Night Blend", Range(0, 1)) = 0.0
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 300
        Cull [_Cull]
        ZWrite Off
        GrabPass { "_BackgroundTexture" }

        CGINCLUDE
        #define _GLOSSYENV 1
        #define UNITY_SETUP_BRDF_INPUT MetallicSetup
        ENDCG

        CGPROGRAM
        #pragma surface surf Standard alpha vertex:vert
        #pragma target 4.0
        #include "UnityCG.cginc"

        // Properties
        fixed4 _Color, _NightColor, _DeepColor, _FoamColor;
        half _WaveHeight, _WaveSpeed, _DayNightBlend;
        half4 _WindDirection;

        sampler2D _Normal1, _Normal2, _FoamTexture, _CausticsTexture;
        sampler2D _BackgroundTexture, _CameraDepthTexture;
        
        half _NormalStrength1, _NormalStrength2;
        half4 _FlowDirection1, _FlowDirection2;
        
        half _FresnelPower, _ReflectionStrength;
        half _FoamThreshold, _FoamSoftness, _EdgeFoamWidth;
        half _CausticsStrength, _CausticsSpeed;
        half _DepthFade, _DistanceFade;
        half _Glossiness, _Metallic;

        struct Input
        {
            float2 uv_Normal1;
            float2 uv_Normal2;
            float2 uv_FoamTexture;
            float2 uv_CausticsTexture;
            float4 screenPos;
            float3 worldPos;
            float3 viewDir;
            float eyeDepth;
        };

        // Vertex displacement for waves
        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            
            // Generate waves using multiple sine waves
            float time = _Time.y * _WaveSpeed;
            float wave1 = sin(worldPos.x * 0.1 + worldPos.z * 0.1 + time) * 0.5;
            float wave2 = sin(worldPos.x * 0.05 - worldPos.z * 0.15 + time * 0.8) * 0.3;
            float wave3 = sin(worldPos.x * 0.2 + worldPos.z * 0.05 + time * 1.2) * 0.2;
            
            float waveHeight = (wave1 + wave2 + wave3) * _WaveHeight;
            
            // Apply wind direction influence
            float windInfluence = dot(normalize(worldPos.xz), _WindDirection.xz) * 0.5 + 0.5;
            waveHeight *= windInfluence;
            
            v.vertex.y += waveHeight;
            
            COMPUTE_EYEDEPTH(o.eyeDepth);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Animated UV coordinates for water flow
            float2 uv1 = IN.uv_Normal1 + _Time.y * _FlowDirection1.xy * _WaveSpeed;
            float2 uv2 = IN.uv_Normal2 + _Time.y * _FlowDirection2.xy * _WaveSpeed;
            
            // Sample normal maps
            fixed3 normal1 = UnpackNormal(tex2D(_Normal1, uv1)) * _NormalStrength1;
            fixed3 normal2 = UnpackNormal(tex2D(_Normal2, uv2)) * _NormalStrength2;
            fixed3 combinedNormal = normalize(normal1 + normal2);
            
            // Fresnel effect
            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), combinedNormal)), _FresnelPower);
            
            // Day/night color blending
            fixed4 waterColor = lerp(_Color, _NightColor, _DayNightBlend);
            
            // Distance-based depth effect
            float distance = length(_WorldSpaceCameraPos - IN.worldPos);
            float depthFactor = saturate(distance / _DistanceFade);
            waterColor = lerp(waterColor, _DeepColor, depthFactor);
            
            // Screen space depth for edge foam
            float4 screenPos = IN.screenPos;
            float2 screenUV = screenPos.xy / screenPos.w;
            float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
            float surfaceDepth = IN.eyeDepth;
            float depthDifference = sceneDepth - surfaceDepth;
            
            // Edge foam calculation
            float edgeFoam = 1.0 - saturate(depthDifference / _EdgeFoamWidth);
            edgeFoam = pow(edgeFoam, 2.0);
            
            // Wave peak foam
            float waveHeight = sin(IN.worldPos.x * 0.1 + IN.worldPos.z * 0.1 + _Time.y * _WaveSpeed) * 0.5 + 0.5;
            float peakFoam = smoothstep(_FoamThreshold, _FoamThreshold + _FoamSoftness, waveHeight);
            
            // Combine foam effects
            float2 foamUV = IN.uv_FoamTexture + _Time.y * 0.1;
            fixed4 foamTexture = tex2D(_FoamTexture, foamUV);
            float totalFoam = saturate(edgeFoam + peakFoam);
            
            // Caustics effect
            float2 causticsUV = IN.uv_CausticsTexture + _Time.y * _CausticsSpeed * 0.1;
            fixed4 caustics = tex2D(_CausticsTexture, causticsUV) * _CausticsStrength;
            
            // Apply caustics to water color
            waterColor.rgb += caustics.rgb * (1.0 - totalFoam);
            
            // Final color mixing
            o.Albedo = lerp(waterColor.rgb, _FoamColor.rgb, totalFoam * foamTexture.r);
            
            // Transparency with fresnel
            o.Alpha = lerp(waterColor.a, 1.0, fresnel * _ReflectionStrength);
            o.Alpha = lerp(o.Alpha, _FoamColor.a, totalFoam);
            
            o.Normal = combinedNormal;
            o.Metallic = _Metallic * (1.0 - totalFoam);
            o.Smoothness = lerp(_Glossiness, 0.1, totalFoam);
        }
        ENDCG
    }
    
    Fallback "Legacy Shaders/Transparent/VertexLit"
}