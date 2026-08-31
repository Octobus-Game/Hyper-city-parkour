// FresnelColorRamp.shader
// Replicates the Blender node graph:
//   Fresnel (IOR 0.9) → Color Ramp (dark orange → peach, pos 0.715) → Material Output (Surface)
// + Emission (FLAT / RAMP / FRESNEL_RIM)
// + Alpha transparency (OPAQUE / CUTOUT / TRANSPARENT / FRESNEL_ALPHA)

Shader "Custom/FresnelColorRamp"
{
    Properties
    {
        // --- Fresnel ---
        _IOR ("IOR (Index of Refraction)", Range(1.0, 3.0)) = 0.9

        // --- Color Ramp stops ---
        _ColorA ("Ramp Color A (edge,  pos 0.0)",   Color) = (0.18, 0.06, 0.02, 1)
        _ColorB ("Ramp Color B (mid,   pos 0.715)", Color) = (0.72, 0.32, 0.10, 1)
        _ColorC ("Ramp Color C (highlight, pos 1.0)", Color) = (0.85, 0.60, 0.42, 1)
        _RampBreak ("Ramp Break Point", Range(0.0, 1.0)) = 0.715

        // --- Fresnel controls ---
        _FresnelBias   ("Fresnel Bias",   Range(0.0, 1.0))  = 0.0
        _FresnelScale  ("Fresnel Scale",  Range(0.0, 5.0))  = 1.0
        _FresnelPower  ("Fresnel Power",  Range(0.1, 10.0)) = 5.0

        // --- Emission ---
        [HDR] _EmissionColor      ("Emission Color",     Color)         = (0, 0, 0, 1)
        _EmissionIntensity         ("Emission Intensity", Range(0, 10))  = 0.0
        [KeywordEnum(FLAT, RAMP, FRESNEL_RIM)]
        _EmissionMode              ("Emission Mode",      Float)         = 0
        _EmissionRimPower          ("Emission Rim Power", Range(0.1, 10)) = 3.0

        // --- Alpha ---
        // AlphaMode:  0 = OPAQUE, 1 = CUTOUT, 2 = TRANSPARENT, 3 = FRESNEL_ALPHA
        [KeywordEnum(OPAQUE, CUTOUT, TRANSPARENT, FRESNEL_ALPHA)]
        _AlphaMode ("Alpha Mode", Float) = 0

        // Master opacity multiplier (used by TRANSPARENT and FRESNEL_ALPHA)
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0

        // CUTOUT threshold
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // FRESNEL_ALPHA: invert makes centre opaque & edges transparent (glass-like)
        [Toggle] _FresnelAlphaInvert ("Fresnel Alpha Invert", Float) = 0

        // Blend state driven by AlphaMode (set via script or manually)
        [HideInInspector] _SrcBlend  ("__src",  Float) = 1
        [HideInInspector] _DstBlend  ("__dst",  Float) = 0
        [HideInInspector] _ZWrite    ("__zw",   Float) = 1
    }

    SubShader
    {
        // Queue and RenderType are overridden by the MaterialPropertyDrawer / script
        Tags { "RenderType"="TransparentCutout" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            // ---- Uniforms ----
            float  _IOR;
            float4 _ColorA, _ColorB, _ColorC;
            float  _RampBreak;
            float  _FresnelBias, _FresnelScale, _FresnelPower;
            float4 _EmissionColor;
            float  _EmissionIntensity, _EmissionMode, _EmissionRimPower;
            float  _AlphaMode, _Alpha, _Cutoff, _FresnelAlphaInvert;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
                SHADOW_COORDS(2)
            };

            // ---- Blender Fresnel (Schlick) ----
            float BlenderFresnel(float3 N, float3 V, float ior)
            {
                float F0    = pow((1.0 - ior) / (1.0 + ior), 2.0);
                float NdotV = saturate(dot(N, V));
                return saturate(F0 + (1.0 - F0) * pow(1.0 - NdotV, 5.0));
            }

            // ---- 2-stop Color Ramp ----
            float4 ColorRamp(float t, float bp,
                             float4 cA, float4 cB, float4 cC)
            {
                if (t < bp)
                {
                    float s = (bp > 0.0) ? t / bp : 0.0;
                    return lerp(cA, cB, s);
                }
                else
                {
                    float s = (bp < 1.0) ? (t - bp) / (1.0 - bp) : 1.0;
                    return lerp(cB, cC, s);
                }
            }

            // ---- Vertex ----
            v2f vert(appdata v)
            {
                v2f o;
                o.pos         = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                return o;
            }

            // ---- Fragment ----
            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

                // 1. Fresnel factor
                float ff = saturate(_FresnelBias +
                           _FresnelScale * pow(1.0 - saturate(dot(N, V)), _FresnelPower));

                // 2. Color Ramp
                float4 rampColor = ColorRamp(ff, _RampBreak,
                                             _ColorA, _ColorB, _ColorC);

                // 3. Diffuse lighting
                float3 L      = normalize(_WorldSpaceLightPos0.xyz);
                float  NdotL  = max(0.0, dot(N, L));
                float3 lit    = rampColor.rgb *
                                (_LightColor0.rgb * NdotL + unity_AmbientSky.rgb * 0.3);

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                // 4. Emission
                float3 emission = (float3)0;
                if (_EmissionMode < 0.5)
                    emission = _EmissionColor.rgb * _EmissionIntensity;
                else if (_EmissionMode < 1.5)
                    emission = _EmissionColor.rgb * rampColor.rgb * _EmissionIntensity;
                else
                {
                    float rim = pow(1.0 - saturate(dot(N, V)), _EmissionRimPower);
                    emission  = _EmissionColor.rgb * rim * _EmissionIntensity;
                }

                // 5. Alpha channel
                // Source alpha comes from the Color Ramp's own alpha track
                float rampAlpha = rampColor.a;

                float finalAlpha = 1.0;

                if (_AlphaMode < 0.5)
                {
                    // OPAQUE — ignore everything
                    finalAlpha = 1.0;
                }
                else if (_AlphaMode < 1.5)
                {
                    // CUTOUT — ramp.a * master alpha, then clip
                    finalAlpha = rampAlpha * _Alpha;
                    clip(finalAlpha - _Cutoff);
                    finalAlpha = 1.0; // after clip, write fully opaque
                }
                else if (_AlphaMode < 2.5)
                {
                    // TRANSPARENT — ramp.a * master alpha, standard blend
                    finalAlpha = rampAlpha * _Alpha;
                }
                else
                {
                    // FRESNEL_ALPHA — opacity driven by Fresnel factor
                    // _FresnelAlphaInvert = 0: edges opaque, centre transparent (rim glass)
                    // _FresnelAlphaInvert = 1: centre opaque, edges transparent (soft shell)
                    float fresnelA = (_FresnelAlphaInvert > 0.5) ? (1.0 - ff) : ff;
                    finalAlpha = saturate(fresnelA * _Alpha);
                }

                float3 finalRGB = lit * atten + emission;
                return float4(finalRGB, finalAlpha);
            }
            ENDCG
        }

        // Shadow caster — respects CUTOUT clip
        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual Cull Back

            CGPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #pragma target 3.0
            #include "UnityCG.cginc"

            float  _AlphaMode, _Alpha, _Cutoff, _FresnelAlphaInvert;
            float  _RampBreak, _FresnelBias, _FresnelScale, _FresnelPower;
            float4 _ColorA, _ColorB, _ColorC;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2fShadow
            {
                V2F_SHADOW_CASTER;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
            };

            float4 ColorRampAlpha(float t, float bp,
                                  float4 cA, float4 cB, float4 cC)
            {
                if (t < bp) { float s = bp > 0 ? t/bp : 0; return lerp(cA,cB,s); }
                else        { float s = bp < 1 ? (t-bp)/(1-bp) : 1; return lerp(cB,cC,s); }
            }

            v2fShadow vertShadow(appdata v)
            {
                v2fShadow o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 fragShadow(v2fShadow i) : SV_Target
            {
                // Clip transparent pixels for CUTOUT mode
                if (_AlphaMode > 0.5 && _AlphaMode < 1.5)
                {
                    float3 N  = normalize(i.worldNormal);
                    float3 V  = normalize(_WorldSpaceCameraPos - i.worldPos);
                    float  ff = saturate(_FresnelBias +
                                _FresnelScale * pow(1.0 - saturate(dot(N,V)), _FresnelPower));
                    float4 rc = ColorRampAlpha(ff, _RampBreak, _ColorA, _ColorB, _ColorC);
                    clip(rc.a * _Alpha - _Cutoff);
                }
                SHADOW_CASTER_FRAGMENT(i);
            }
            ENDCG
        }
    }

    // Automatically configure blend state when AlphaMode changes
    CustomEditor "FresnelColorRampGUI"
    FallBack "Transparent/Diffuse"
}
