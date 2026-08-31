Shader "Custom/VortexDistortion"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1, 1, 1, 0.3)
        _MainTex ("Texture (optional)", 2D) = "white" {}
        _TwistAmount ("Twist Amount", Range(0, 20)) = 5.0
        _Radius ("Vortex Radius", Range(0.01, 2)) = 0.5
        _RotationSpeed ("Rotation Speed", Range(-10, 10)) = 1.0
        _Falloff ("Falloff Power", Range(0.1, 5)) = 2.0
        _CenterX ("Center Offset X", Range(-0.5, 0.5)) = 0
        _CenterY ("Center Offset Y", Range(-0.5, 0.5)) = 0
    }

    SubShader
    {
        // Важно: рендерим ПОСЛЕ непрозрачных объектов, чтобы GrabPass захватил уже готовую картинку
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True"
        }

        // Захватываем содержимое экрана за объектом
        GrabPass 
        { 
            "_GrabTexture" 
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _Color;
            float _TwistAmount;
            float _Radius;
            float _RotationSpeed;
            float _Falloff;
            float _CenterX;
            float _CenterY;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 grabPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Локальный центр вихря в UV координатах объекта (0..1)
                float2 center = float2(0.5 + _CenterX, 0.5 + _CenterY);
                float2 uv = i.uv - center;

                float dist = length(uv);
                float angle = atan2(uv.y, uv.x);

                // Сила закручивания уменьшается от центра к краям (falloff)
                float strength = saturate(1.0 - dist / _Radius);
                strength = pow(strength, _Falloff);

                // Добавляем вращение во времени + твист по силе
                float twist = strength * _TwistAmount + _Time.y * _RotationSpeed;
                angle += twist;

                // Пересчитываем обратно в декартовы координаты
                float2 distortedUV = center + float2(cos(angle), sin(angle)) * dist;

                // Переводим смещение UV в смещение экранных координат GrabPass
                float2 uvOffset = (distortedUV - i.uv);

                float4 grabPos = i.grabPos;
                grabPos.xy += uvOffset * grabPos.w * 0.5; // масштаб под перспективное деление

                fixed4 background = tex2Dproj(_GrabTexture, grabPos);

                // Опционально подмешиваем текстуру объекта и тонирование
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = background * tex * _Color;
                col.a = _Color.a;

                return col;
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
