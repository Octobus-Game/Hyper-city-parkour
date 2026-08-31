Shader "Custom/SphereDots"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ShapeSize ("Shape Size", Range(0.01, 0.5)) = 0.1
        _Spacing ("Spacing", Range(0.1, 2.0)) = 1.0
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        [KeywordEnum(Sphere, Cube, Triangle, Cylinder)] _ShapeType ("Shape Type", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        #pragma multi_compile _SHAPETYPE_SPHERE _SHAPETYPE_CUBE _SHAPETYPE_TRIANGLE _SHAPETYPE_CYLINDER

        sampler2D _MainTex;
        fixed4 _Color;
        float _ShapeSize;
        float _Spacing;
        half _Metallic;
        half _Smoothness;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 localPos;
        };

        void vert (inout appdata_full v, out Input o) 
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.localPos = v.vertex.xyz;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Создаем сетку точек на основе локальных координат
            float3 gridPos = IN.localPos * _Spacing;
            float3 cellCenter = floor(gridPos) / _Spacing;
            
            // Локальная позиция внутри ячейки сетки
            float3 localCellPos = IN.localPos - cellCenter;
            
            float shapeMask = 0.0;
            float3 shapeNormal = float3(0, 1, 0);
            
            #ifdef _SHAPETYPE_SPHERE
                // Сфера
                float distToCenter = length(localCellPos);
                shapeMask = 1.0 - smoothstep(_ShapeSize * 0.8, _ShapeSize, distToCenter);
                shapeNormal = normalize(localCellPos);
            #endif
            
            #ifdef _SHAPETYPE_CUBE
                // Куб
                float3 absPos = abs(localCellPos);
                float maxDist = max(max(absPos.x, absPos.y), absPos.z);
                shapeMask = 1.0 - smoothstep(_ShapeSize * 0.8, _ShapeSize, maxDist);
                
                // Нормаль куба
                if (absPos.x > absPos.y && absPos.x > absPos.z)
                    shapeNormal = float3(sign(localCellPos.x), 0, 0);
                else if (absPos.y > absPos.z)
                    shapeNormal = float3(0, sign(localCellPos.y), 0);
                else
                    shapeNormal = float3(0, 0, sign(localCellPos.z));
            #endif
            
            #ifdef _SHAPETYPE_TRIANGLE
                // Треугольная призма
                float2 pos2D = localCellPos.xz;
                
                // Треугольник в 2D (равносторонний)
                float triHeight = _ShapeSize * 1.732; // sqrt(3)
                float triWidth = _ShapeSize * 2.0;
                
                // Проверяем, находится ли точка внутри треугольника
                float y1 = triHeight * 0.5;
                float y2 = -triHeight * 0.5;
                float x1 = -triWidth * 0.5;
                float x2 = triWidth * 0.5;
                
                bool inTriangle = (pos2D.y < y1) && 
                                 (pos2D.y > y2) && 
                                 (pos2D.x > x1 + (pos2D.y - y2) * (triWidth / triHeight)) &&
                                 (pos2D.x < x2 - (pos2D.y - y2) * (triWidth / triHeight));
                
                // Высота призмы
                bool inHeight = abs(localCellPos.y) < _ShapeSize;
                
                shapeMask = (inTriangle && inHeight) ? 1.0 : 0.0;
                
                // Упрощенная нормаль для треугольника
                if (abs(localCellPos.y) > _ShapeSize * 0.8)
                    shapeNormal = float3(0, sign(localCellPos.y), 0);
                else
                    shapeNormal = normalize(float3(localCellPos.x, 0, localCellPos.z));
            #endif
            
            #ifdef _SHAPETYPE_CYLINDER
                // Цилиндр
                float2 radialPos = localCellPos.xz;
                float radialDist = length(radialPos);
                float heightDist = abs(localCellPos.y);
                
                bool inRadius = radialDist < _ShapeSize;
                bool inHeight = heightDist < _ShapeSize;
                
                shapeMask = (inRadius && inHeight) ? 1.0 : 0.0;
                
                // Нормаль цилиндра
                if (heightDist > _ShapeSize * 0.8)
                    shapeNormal = float3(0, sign(localCellPos.y), 0);
                else
                    shapeNormal = normalize(float3(radialPos.x, 0, radialPos.y));
            #endif
            
            // Если точка вне формы - делаем ее прозрачной
            if (shapeMask < 0.01)
                discard;
            
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb * shapeMask;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = c.a * shapeMask;
            o.Normal = shapeNormal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}