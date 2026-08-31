Shader "Custom/BlackHole"
{
    Properties
    {
        [Header(Skybox 6 Sided)]
        _FrontTex("Front (+Z)", 2D) = "white" {}
        _BackTex("Back (-Z)", 2D) = "white" {}
        _LeftTex("Left (+X)", 2D) = "white" {}
        _RightTex("Right (-X)", 2D) = "white" {}
        _UpTex("Up (+Y)", 2D) = "white" {}
        _DownTex("Down (-Y)", 2D) = "white" {}
        _SkyboxExposure("Skybox Exposure", Range(0, 2)) = 1.0

        [Header(Alternative Cubemap)]
        _MainTex("Skybox Cubemap (Optional)", Cube) = "white" {}
        [Toggle] _UseCubemap("Use Cubemap Instead of 6-Sided", Float) = 0
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
            LOD 200

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float3 worldPos : TEXCOORD0;
                };

                sampler2D _FrontTex;
                sampler2D _BackTex;
                sampler2D _LeftTex;
                sampler2D _RightTex;
                sampler2D _UpTex;
                sampler2D _DownTex;
                float _SkyboxExposure;

                samplerCUBE _MainTex;
                float _UseCubemap;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    return o;
                }

                float4 sampleSkybox6Sided(float3 dir)
                {
                    dir = normalize(dir);
                    float3 absDir = abs(dir);
                    float2 uv;
                    float4 color;

                    if (absDir.x >= absDir.y && absDir.x >= absDir.z)
                    {
                        if (dir.x > 0.0)
                        {
                            uv = float2(-dir.z / absDir.x, dir.y / absDir.x) * 0.5 + 0.5;
                            color = tex2D(_RightTex, uv);
                        }
                        else
                        {
                            uv = float2(dir.z / absDir.x, dir.y / absDir.x) * 0.5 + 0.5;
                            color = tex2D(_LeftTex, uv);
                        }
                    }
                    else if (absDir.y >= absDir.x && absDir.y >= absDir.z)
                    {
                        if (dir.y > 0.0)
                        {
                            uv = float2(dir.x / absDir.y, -dir.z / absDir.y) * 0.5 + 0.5;
                            color = tex2D(_UpTex, uv);
                        }
                        else
                        {
                            uv = float2(dir.x / absDir.y, dir.z / absDir.y) * 0.5 + 0.5;
                            color = tex2D(_DownTex, uv);
                        }
                    }
                    else
                    {
                        if (dir.z > 0.0)
                        {
                            uv = float2(dir.x / absDir.z, dir.y / absDir.z) * 0.5 + 0.5;
                            color = tex2D(_FrontTex, uv);
                        }
                        else
                        {
                            uv = float2(-dir.x / absDir.z, dir.y / absDir.z) * 0.5 + 0.5;
                            color = tex2D(_BackTex, uv);
                        }
                    }

                    return color * _SkyboxExposure;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float3 rayDir = normalize(i.worldPos - _WorldSpaceCameraPos);

                    if (_UseCubemap > 0.5)
                        return texCUBE(_MainTex, rayDir) * _SkyboxExposure;
                    else
                        return sampleSkybox6Sided(rayDir);
                }
                ENDCG
            }
        }
            FallBack "Diffuse"
}