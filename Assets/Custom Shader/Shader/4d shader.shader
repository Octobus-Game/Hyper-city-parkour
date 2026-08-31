Shader "Custom/HypercubeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _EdgeColor ("Edge Color", Color) = (0.8, 1.0, 1.0, 1.0)
        _Time4D ("4D Time", Range(0, 6.28)) = 0
        _RotationSpeed ("Rotation Speed", Range(0, 2)) = 1
        _EdgeWidth ("Edge Width", Range(0.001, 0.05)) = 0.01
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _InnerAlpha ("Inner Alpha", Range(0, 1)) = 0.3
        _ProjectionDistance ("Projection Distance", Range(1, 10)) = 3
        [KeywordEnum(Hypercube, Hypersphere, Hypercylinder, Hypercone)] _Shape4D ("4D Shape", Float) = 0
        _ShapeScale ("Shape Scale", Range(0.1, 3.0)) = 1.0
        _NoiseAmplitude ("Noise Amplitude", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD2;
                float3 normal : TEXCOORD3;
                float4 originalPos : TEXCOORD4;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _EdgeColor;
            float _Time4D;
            float _RotationSpeed;
            float _EdgeWidth;
            float _Brightness;
            float _InnerAlpha;
            float _ProjectionDistance;
            float _Shape4D;
            float _ShapeScale;
            float _NoiseAmplitude;
            
            // 4D rotation matrices
            float4x4 rotation4D_XW(float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float4x4(
                    c, 0, 0, -s,
                    0, 1, 0, 0,
                    0, 0, 1, 0,
                    s, 0, 0, c
                );
            }
            
            float4x4 rotation4D_YW(float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float4x4(
                    1, 0, 0, 0,
                    0, c, 0, -s,
                    0, 0, 1, 0,
                    0, s, 0, c
                );
            }
            
            float4x4 rotation4D_ZW(float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float4x4(
                    1, 0, 0, 0,
                    0, 1, 0, 0,
                    0, 0, c, -s,
                    0, 0, s, c
                );
            }
            
            // Project 4D point to 3D
            float3 project4Dto3D(float4 point4D)
            {
                float w = point4D.w + _ProjectionDistance;
                return point4D.xyz / max(w, 0.1);
            }
            
            // Generate 4D shape vertices
            float4 get4DShapeVertex(float3 localPos)
            {
                float4 pos4D;
                float time = _Time.y * _RotationSpeed + _Time4D;
                
                // Add noise for organic feel
                float3 noise = float3(
                    sin(localPos.x * 10 + time) * _NoiseAmplitude,
                    sin(localPos.y * 10 + time * 1.1) * _NoiseAmplitude,
                    sin(localPos.z * 10 + time * 0.9) * _NoiseAmplitude
                );
                localPos += noise;
                
                if (_Shape4D < 0.5) // Hypercube
                {
                    pos4D = float4(localPos * _ShapeScale, sin(localPos.x + localPos.y + localPos.z + time));
                }
                else if (_Shape4D < 1.5) // Hypersphere (4D sphere)
                {
                    // Convert to spherical coordinates and add 4th dimension
                    float r = length(localPos) * _ShapeScale;
                    float3 spherePos = normalize(localPos) * r;
                    float w = sin(r * 2 + time) * cos(r + time * 0.7);
                    pos4D = float4(spherePos, w);
                }
                else if (_Shape4D < 2.5) // Hypercylinder (4D cylinder)
                {
                    // Cylindrical coordinates with 4D extension
                    float2 xz = localPos.xz;
                    float radius = length(xz);
                    float angle = atan2(xz.y, xz.x);
                    
                    float newRadius = radius * _ShapeScale;
                    float2 newXZ = float2(cos(angle), sin(angle)) * newRadius;
                    
                    float w = sin(angle * 3 + localPos.y * 2 + time) * cos(radius * 4 + time * 0.8);
                    pos4D = float4(newXZ.x, localPos.y * _ShapeScale, newXZ.y, w);
                }
                else // Hypercone (4D cone)
                {
                    // Conical coordinates with 4D extension
                    float height = localPos.y + 1; // Shift to make positive
                    float coneRadius = height * 0.5 * _ShapeScale;
                    
                    float2 xz = normalize(localPos.xz) * coneRadius * length(localPos.xz);
                    
                    float w = sin(height * 3 + time) * cos(length(localPos.xz) * 5 + time * 0.6) * height;
                    pos4D = float4(xz.x, (localPos.y + 1) * _ShapeScale, xz.y, w);
                }
                
                // Apply 4D rotations
                pos4D = mul(rotation4D_XW(time * 0.7), pos4D);
                pos4D = mul(rotation4D_YW(time * 0.5), pos4D);
                pos4D = mul(rotation4D_ZW(time * 0.3), pos4D);
                
                return pos4D;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // Store original position
                o.originalPos = v.vertex;
                
                // Get 4D shape position
                float4 pos4D = get4DShapeVertex(v.vertex.xyz);
                
                // Project to 3D
                float3 pos3D = project4Dto3D(pos4D);
                
                // Transform to world space
                float4 worldPos = mul(unity_ObjectToWorld, float4(pos3D, 1.0));
                o.worldPos = worldPos.xyz;
                
                // Transform to clip space
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                
                // UV and normal
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Base texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Calculate distance from edges for wireframe effect
                float3 localPos = i.originalPos.xyz;
                float edgeDist;
                
                if (_Shape4D < 0.5) // Hypercube
                {
                    edgeDist = min(min(abs(localPos.x), abs(localPos.y)), abs(localPos.z));
                    edgeDist = min(edgeDist, min(abs(1 - abs(localPos.x)), min(abs(1 - abs(localPos.y)), abs(1 - abs(localPos.z)))));
                }
                else if (_Shape4D < 1.5) // Hypersphere
                {
                    float distFromCenter = length(localPos);
                    edgeDist = abs(distFromCenter - 0.5); // Distance from sphere surface
                }
                else if (_Shape4D < 2.5) // Hypercylinder
                {
                    float radialDist = length(localPos.xz);
                    float heightDist = min(abs(localPos.y + 1), abs(localPos.y - 1));
                    edgeDist = min(abs(radialDist - 0.5), heightDist * 0.5);
                }
                else // Hypercone
                {
                    float height = localPos.y + 1;
                    float expectedRadius = height * 0.5;
                    float actualRadius = length(localPos.xz);
                    float radialDist = abs(actualRadius - expectedRadius * 0.5);
                    float baseDist = abs(localPos.y + 1);
                    edgeDist = min(radialDist, baseDist * 0.3);
                }
                
                // Edge highlighting
                float edgeFactor = 1.0 - smoothstep(0, _EdgeWidth, edgeDist);
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edgeFactor);
                
                // 4D effect: modulate based on 4D position
                float4 pos4D = get4DShapeVertex(localPos);
                float wFactor = (pos4D.w + 1.0) * 0.5; // Normalize W coordinate
                
                // Apply 4D-based effects
                col.rgb *= 1.0 + wFactor * 0.5;
                col.a = lerp(_InnerAlpha, 1.0, edgeFactor);
                col.a *= (0.7 + wFactor * 0.3);
                
                // Brightness
                col.rgb *= _Brightness;
                
                // Add pulsing effect based on 4D rotation
                float pulse = sin(_Time.y * 2 + pos4D.w) * 0.1 + 0.9;
                col.rgb *= pulse;
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
    
    // Fallback shader
    FallBack "Transparent/Diffuse"
}