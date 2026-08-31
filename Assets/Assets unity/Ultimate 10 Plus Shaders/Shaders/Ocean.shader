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

        [Header(Procedural Noise)]
        _NoiseScale ("Noise Scale", Range(0.1, 50)) = 8
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 0.5
        _NoiseStrength ("Noise Strength", Range(0, 2)) = 0.3
        _NoiseUVDistortion ("Noise UV Distortion", Range(0, 0.5)) = 0.05
        
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
        // Physically based Standard lighting model, and disabled shadows (the ocean doesn't have a shadow :D)
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

        half _NoiseScale;
        half _NoiseSpeed;
        half _NoiseStrength;
        half _NoiseUVDistortion;

        struct Input
        {
            float2 uv_Normal1;
            float2 uv_Normal2;
        };

        // ------------------------------------------------------------------
        // Procedural 2D value noise (no texture lookups needed)
        // ------------------------------------------------------------------

        // Pseudo-random hash based on a 2D input, returns value in [0,1]
        float noiseHash(float2 p)
        {
            return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
        }

        // Value noise with smooth (Hermite) interpolation between grid corners
        float valueNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);

            // Smoothstep-style interpolation curve (avoids grid artifacts)
            float2 u = f * f * (3.0 - 2.0 * f);

            float a = noiseHash(i + float2(0.0, 0.0));
            float b = noiseHash(i + float2(1.0, 0.0));
            float c = noiseHash(i + float2(0.0, 1.0));
            float d = noiseHash(i + float2(1.0, 1.0));

            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }

        // Fractal Brownian Motion - layers several octaves of valueNoise together
        // for a more natural, less repetitive result (classic for water/clouds)
        float fbmNoise(float2 p)
        {
            float value = 0.0;
            float amplitude = 0.5;

            [unroll]
            for (int i = 0; i < 4; i++)
            {
                value += amplitude * valueNoise(p);
                p *= 2.0;
                amplitude *= 0.5;
            }

            return value;
        }

        fixed4 pixel;
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Animated noise coordinate, independent of the normal map UVs
            float2 noiseUV = IN.uv_Normal1 * _NoiseScale + _Time.y * _NoiseSpeed;

            // Sample noise twice (offset) to build a fake "gradient" that we
            // can use to distort UVs, mimicking uneven water displacement
            float n = fbmNoise(noiseUV);
            float nx = fbmNoise(noiseUV + float2(0.37, 0.0));
            float ny = fbmNoise(noiseUV + float2(0.0, 0.37));
            float2 noiseGradient = float2(nx, ny) - n;

            IN.uv_Normal1 += _Time.y * _FlowDirection1 + noiseGradient * _NoiseUVDistortion;
            IN.uv_Normal2 += _Time.y * _FlowDirection2 + noiseGradient * _NoiseUVDistortion;

            pixel = (tex2D (_Normal1, IN.uv_Normal1) * _NormalStrength1 + tex2D(_Normal2, IN.uv_Normal2) * _NormalStrength2);

            // Build an extra procedural normal from the noise and blend it in,
            // giving small-scale ripples on top of the two normal maps
            fixed3 noiseNormal = fixed3(nx - n, ny - n, 1.0) * _NoiseStrength;
            pixel.rgb += noiseNormal;
            pixel.a = 1.0;

            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a;
            
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            o.Normal = UnpackNormal(pixel);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
