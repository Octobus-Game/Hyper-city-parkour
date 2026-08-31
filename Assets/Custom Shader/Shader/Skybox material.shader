Shader "Custom/SkyboxCubemap"
{
    Properties
    {
        _Cubemap ("Skybox Cubemap", Cube) = "" {}
        _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Tint ("Tint Color", Color) = (1, 1, 1, 1)
        _Rotation ("Rotation", Range(0, 360)) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Background" 
            "RenderType" = "Background" 
            "PreviewType" = "Skybox" 
        }
        
        Cull Off
        ZWrite Off
        
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
                float3 texcoord : TEXCOORD0;
            };
            
            samplerCUBE _Cubemap;
            half4 _Cubemap_HDR;
            half4 _Tint;
            half _Exposure;
            float _Rotation;
            
            float3 RotateAroundYInDegrees(float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                float3 rotated = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
                o.pos = UnityObjectToClipPos(float4(rotated, 1.0));
                o.texcoord = v.vertex.xyz;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                half4 tex = texCUBE(_Cubemap, i.texcoord);
                half3 c = DecodeHDR(tex, _Cubemap_HDR);
                c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
                c *= _Exposure;
                return half4(c, 1);
            }
            ENDCG
        }
    }
    
    Fallback Off
}