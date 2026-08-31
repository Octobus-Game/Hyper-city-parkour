Shader "Skybox/Dual Cubemap Blend"
{
    Properties
    {
        [Header(Cubemaps)]
        _CubemapA ("Cubemap A (Primary)",   Cube) = "grey" {}
        _CubemapB ("Cubemap B (Secondary)", Cube) = "grey" {}

        [Header(Rotation)]
        [Space(4)]
        _RotationA ("Rotation A (deg)", Range(0, 360)) = 0.0
        _RotationB ("Rotation B (deg)", Range(0, 360)) = 0.0

        [Header(Blend)]
        [Space(4)]
        _Blend ("Blend  A to B", Range(0, 1)) = 0.0

        // ── Blend mode selector ──────────────────────────────────────────
        // 0  Linear
        // 1  Smoothstep
        // 2  Smootherstep  (Ken Perlin)
        // 3  Horizon Wipe  (bottom → top based on view dir Y)
        // 4  Radial Wipe   (angular sweep around +Y axis)
        // 5  Dissolve Noise (value-noise hash)
        // 6  Additive
        // 7  Multiply
        // 8  Screen
        // 9  Hard Light
        [Space(4)]
        [IntRange] _BlendMode ("Blend Mode (0-9)", Range(0, 9)) = 0

        // Extra parameters used by specific modes
        [Space(4)]
        _WipeEdge   ("Wipe Edge Softness",   Range(0.001, 0.5)) = 0.05
        _NoiseScale ("Dissolve Noise Scale", Range(1, 64))      = 8.0

        [Header(Exposure)]
        [Space(4)]
        _Exposure ("Exposure", Range(0, 8)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Background"
            "RenderType"      = "Background"
            "PreviewType"     = "Skybox"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #include "UnityCG.cginc"

            samplerCUBE _CubemapA;
            samplerCUBE _CubemapB;

            half  _Blend;
            float _RotationA;
            float _RotationB;
            half  _Exposure;
            int   _BlendMode;
            float _WipeEdge;
            float _NoiseScale;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 dirA     : TEXCOORD0;
                float3 dirB     : TEXCOORD1;
                float3 worldDir : TEXCOORD2; // raw direction for wipe modes
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ────────────────────────────────────────────────────────────
            // Helpers
            // ────────────────────────────────────────────────────────────

            float3 RotateAroundY(float3 dir, float angleDeg)
            {
                float rad = radians(angleDeg);
                float s = sin(rad), c = cos(rad);
                float3x3 rot = float3x3(c, 0, s,  0, 1, 0,  -s, 0, c);
                return mul(rot, dir);
            }

            // Smooth hermite (classic smoothstep)
            float Smoothstep(float t)
            {
                t = saturate(t);
                return t * t * (3.0 - 2.0 * t);
            }

            // Smoother hermite (Ken Perlin, C2 continuity)
            float Smootherstep(float t)
            {
                t = saturate(t);
                return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
            }

            // Cheap value-noise hash (single float → [0,1])
            float Hash(float3 p)
            {
                p  = frac(p * float3(127.1, 311.7, 74.7));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            // 3-D value noise, tri-linear interpolation
            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(lerp(Hash(i + float3(0,0,0)), Hash(i + float3(1,0,0)), u.x),
                         lerp(Hash(i + float3(0,1,0)), Hash(i + float3(1,1,0)), u.x), u.y),
                    lerp(lerp(Hash(i + float3(0,0,1)), Hash(i + float3(1,0,1)), u.x),
                         lerp(Hash(i + float3(0,1,1)), Hash(i + float3(1,1,1)), u.x), u.y),
                    u.z);
            }

            // Screen blend (like Photoshop Screen layer)
            half3 BlendScreen(half3 a, half3 b)
            {
                return 1.0 - (1.0 - a) * (1.0 - b);
            }

            // Hard Light blend
            half3 BlendHardLight(half3 a, half3 b)
            {
                // b < 0.5  →  Multiply;  b >= 0.5  →  Screen
                half3 mul2  = 2.0 * a * b;
                half3 scrn  = BlendScreen(a, b * 2.0 - 1.0);
                return lerp(mul2, scrn, step(0.5, b));
            }

            // ────────────────────────────────────────────────────────────
            // Vertex
            // ────────────────────────────────────────────────────────────

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);

                float3 worldDir = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
                o.worldDir = worldDir;
                o.dirA     = RotateAroundY(worldDir, _RotationA);
                o.dirB     = RotateAroundY(worldDir, _RotationB);
                return o;
            }

            // ────────────────────────────────────────────────────────────
            // Fragment
            // ────────────────────────────────────────────────────────────

            half4 frag(v2f i) : SV_Target
            {
                half4 colA = texCUBE(_CubemapA, i.dirA);
                half4 colB = texCUBE(_CubemapB, i.dirB);

                colA.rgb = DecodeHDR(colA, unity_SpecCube0_HDR);
                colB.rgb = DecodeHDR(colB, unity_SpecCube1_HDR);

                float t = saturate(_Blend);           // raw [0,1]
                float w = 0.0;                        // final weight for B
                half3 col = (half3)0;

                float3 d = normalize(i.worldDir);

                // ── 0 · Linear ───────────────────────────────────────────
                if (_BlendMode == 0)
                {
                    w   = t;
                    col = lerp(colA.rgb, colB.rgb, w);
                }

                // ── 1 · Smoothstep ───────────────────────────────────────
                else if (_BlendMode == 1)
                {
                    w   = Smoothstep(t);
                    col = lerp(colA.rgb, colB.rgb, w);
                }

                // ── 2 · Smootherstep ─────────────────────────────────────
                else if (_BlendMode == 2)
                {
                    w   = Smootherstep(t);
                    col = lerp(colA.rgb, colB.rgb, w);
                }

                // ── 3 · Horizon Wipe (bottom → top) ──────────────────────
                // d.y goes from -1 (nadir) to +1 (zenith).
                // We map it to [0,1] and compare with _Blend.
                else if (_BlendMode == 3)
                {
                    float horizon = d.y * 0.5 + 0.5;           // [0,1]
                    float edge    = _WipeEdge;
                    w   = smoothstep(t - edge, t + edge, horizon);
                    col = lerp(colA.rgb, colB.rgb, w);
                }

                // ── 4 · Radial Wipe (angular sweep around Y) ─────────────
                // atan2 on XZ plane → [-π, π] → remap to [0,1]
                else if (_BlendMode == 4)
                {
                    float angle = atan2(d.x, d.z);              // [-π, π]
                    float norm  = angle * (1.0 / (2.0 * UNITY_PI)) + 0.5; // [0,1]
                    float edge  = _WipeEdge;
                    w   = smoothstep(t - edge, t + edge, norm);
                    col = lerp(colA.rgb, colB.rgb, w);
                }

                // ── 5 · Dissolve (value-noise) ────────────────────────────
                // Pixel flips from A to B based on a noise threshold.
                else if (_BlendMode == 5)
                {
                    float noise = ValueNoise(d * _NoiseScale);
                    float edge  = _WipeEdge;
                    w   = smoothstep(t - edge, t + edge, noise);
                    col = lerp(colA.rgb, colB.rgb, w);
                }

                // ── 6 · Additive ──────────────────────────────────────────
                // A fades out while B fades in and both are added together.
                else if (_BlendMode == 6)
                {
                    col = colA.rgb * (1.0 - t) + colB.rgb * t;
                    // Allow overbrightness — do NOT saturate before exposure
                }

                // ── 7 · Multiply ──────────────────────────────────────────
                // Multiplied result blended against linear mix.
                else if (_BlendMode == 7)
                {
                    half3 mulCol = colA.rgb * colB.rgb;
                    col = lerp(lerp(colA.rgb, colB.rgb, t), mulCol, t);
                }

                // ── 8 · Screen ────────────────────────────────────────────
                // Linear mix → Screen blend cross-fade.
                else if (_BlendMode == 8)
                {
                    half3 screenCol = BlendScreen(colA.rgb, colB.rgb);
                    col = lerp(lerp(colA.rgb, colB.rgb, t), screenCol, t);
                }

                // ── 9 · Hard Light ────────────────────────────────────────
                // Linear mix → Hard Light blend cross-fade.
                else if (_BlendMode == 9)
                {
                    half3 hlCol = BlendHardLight(colA.rgb, colB.rgb);
                    col = lerp(lerp(colA.rgb, colB.rgb, t), hlCol, t);
                }

                col *= _Exposure;
                return half4(col, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
