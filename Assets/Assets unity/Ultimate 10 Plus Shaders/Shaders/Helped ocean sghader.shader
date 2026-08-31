Shader "Hidden/OceanInteraction"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PreviousTex ("Previous", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Fade
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _PreviousTex;
            float _FadeSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_PreviousTex, i.uv);
                col *= _FadeSpeed; // Fade over time
                return col;
            }
            ENDCG
        }

        // Pass 1: Add Interaction
        Pass
        {
            Blend One One // Additive blending
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float2 _InteractionPos;
            float _InteractionRadius;
            float _InteractionStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 diff = i.uv - _InteractionPos;
                float dist = length(diff);
                
                // Create ripple effect
                float ripple = 0;
                if (dist < _InteractionRadius)
                {
                    float normalizedDist = dist / _InteractionRadius;
                    ripple = (1.0 - normalizedDist) * _InteractionStrength;
                    ripple *= smoothstep(1.0, 0.0, normalizedDist);
                }
                
                // Calculate direction for normal map
                float2 direction = normalize(diff);
                
                return fixed4(direction.x * ripple * 0.5 + 0.5, 
                             direction.y * ripple * 0.5 + 0.5, 
                             ripple, 
                             1.0);
            }
            ENDCG
        }
    }
}