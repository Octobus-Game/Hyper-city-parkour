Shader "Custom/GeometrySphereDots"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ShapeSize ("Shape Size", Range(0.01, 1.0)) = 0.1
        _ShapeResolution ("Shape Resolution", Range(3, 20)) = 8
        _Spacing ("Spacing", Range(0.1, 5.0)) = 1.0
        [KeywordEnum(Sphere, Cube, Triangle, Cylinder)] _ShapeType ("Shape Type", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0
            #pragma multi_compile _SHAPETYPE_SPHERE _SHAPETYPE_CUBE _SHAPETYPE_TRIANGLE _SHAPETYPE_CYLINDER
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 pos : POSITION;
                float3 normal : NORMAL;
                float4 worldPos : TEXCOORD0;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
                float4 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            float4 _Color;
            float _ShapeSize;
            int _ShapeResolution;
            float _Spacing;

            v2g vert (appdata v)
            {
                v2g o;
                o.pos = v.vertex;
                o.normal = v.normal;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            // Функция для создания куба
            void CreateCube(float3 center, inout TriangleStream<g2f> triStream)
            {
                float s = _ShapeSize;
                float3 vertices[8] = {
                    float3(-s, -s, -s), float3(s, -s, -s), float3(s, s, -s), float3(-s, s, -s),
                    float3(-s, -s, s), float3(s, -s, s), float3(s, s, s), float3(-s, s, s)
                };
                
                int indices[36] = {
                    0,2,1, 0,3,2, 1,2,6, 6,5,1, 4,5,6, 6,7,4,
                    2,3,6, 6,3,7, 0,7,3, 0,4,7, 0,1,5, 0,5,4
                };
                
                for(int i = 0; i < 12; i++)
                {
                    for(int j = 0; j < 3; j++)
                    {
                        g2f o;
                        float3 vertex = vertices[indices[i*3 + j]];
                        o.pos = UnityObjectToClipPos(float4(center + vertex, 1));
                        
                        // Вычисляем нормаль грани
                        float3 v0 = vertices[indices[i*3]];
                        float3 v1 = vertices[indices[i*3+1]];
                        float3 v2 = vertices[indices[i*3+2]];
                        o.normal = normalize(cross(v1-v0, v2-v0));
                        
                        o.worldPos = mul(unity_ObjectToWorld, float4(center + vertex, 1));
                        o.uv = float2(0, 0);
                        triStream.Append(o);
                    }
                    triStream.RestartStrip();
                }
            }

            // Функция для создания треугольной призмы
            void CreateTriangle(float3 center, inout TriangleStream<g2f> triStream)
            {
                float s = _ShapeSize;
                float h = s * 1.732; // Высота равностороннего треугольника
                
                float3 vertices[6] = {
                    // Нижний треугольник
                    float3(-s, -s, 0), float3(s, -s, 0), float3(0, -s, h),
                    // Верхний треугольник
                    float3(-s, s, 0), float3(s, s, 0), float3(0, s, h)
                };
                
                int indices[24] = {
                    // Боковые грани
                    0,1,4, 4,3,0, 1,2,5, 5,4,1, 2,0,3, 3,5,2,
                    // Верх и низ
                    0,2,1, 3,4,5
                };
                
                for(int i = 0; i < 8; i++)
                {
                    for(int j = 0; j < 3; j++)
                    {
                        g2f o;
                        float3 vertex = vertices[indices[i*3 + j]];
                        o.pos = UnityObjectToClipPos(float4(center + vertex, 1));
                        
                        float3 v0 = vertices[indices[i*3]];
                        float3 v1 = vertices[indices[i*3+1]];
                        float3 v2 = vertices[indices[i*3+2]];
                        o.normal = normalize(cross(v1-v0, v2-v0));
                        
                        o.worldPos = mul(unity_ObjectToWorld, float4(center + vertex, 1));
                        o.uv = float2(0, 0);
                        triStream.Append(o);
                    }
                    triStream.RestartStrip();
                }
            }

            // Функция для создания цилиндра
            void CreateCylinder(float3 center, inout TriangleStream<g2f> triStream)
            {
                int segments = _ShapeResolution;
                float s = _ShapeSize;
                
                // Создаем простой цилиндр из сегментов
                for(int i = 0; i < segments; i++)
                {
                    float angle1 = 2.0 * 3.14159 * i / segments;
                    float angle2 = 2.0 * 3.14159 * (i + 1) / segments;
                    
                    float3 p1 = float3(cos(angle1) * s, -s, sin(angle1) * s);
                    float3 p2 = float3(cos(angle2) * s, -s, sin(angle2) * s);
                    float3 p3 = float3(cos(angle1) * s, s, sin(angle1) * s);
                    float3 p4 = float3(cos(angle2) * s, s, sin(angle2) * s);
                    
                    // Боковая поверхность (2 треугольника на сегмент)
                    g2f o;
                    
                    // Первый треугольник
                    o.pos = UnityObjectToClipPos(float4(center + p1, 1));
                    o.normal = normalize(float3(p1.x, 0, p1.z));
                    o.worldPos = mul(unity_ObjectToWorld, float4(center + p1, 1));
                    o.uv = float2(0, 0);
                    triStream.Append(o);
                    
                    o.pos = UnityObjectToClipPos(float4(center + p2, 1));
                    o.normal = normalize(float3(p2.x, 0, p2.z));
                    o.worldPos = mul(unity_ObjectToWorld, float4(center + p2, 1));
                    triStream.Append(o);
                    
                    o.pos = UnityObjectToClipPos(float4(center + p3, 1));
                    o.normal = normalize(float3(p3.x, 0, p3.z));
                    o.worldPos = mul(unity_ObjectToWorld, float4(center + p3, 1));
                    triStream.Append(o);
                    
                    triStream.RestartStrip();
                    
                    // Второй треугольник
                    o.pos = UnityObjectToClipPos(float4(center + p2, 1));
                    o.normal = normalize(float3(p2.x, 0, p2.z));
                    o.worldPos = mul(unity_ObjectToWorld, float4(center + p2, 1));
                    triStream.Append(o);
                    
                    o.pos = UnityObjectToClipPos(float4(center + p4, 1));
                    o.normal = normalize(float3(p4.x, 0, p4.z));
                    o.worldPos = mul(unity_ObjectToWorld, float4(center + p4, 1));
                    triStream.Append(o);
                    
                    o.pos = UnityObjectToClipPos(float4(center + p3, 1));
                    o.normal = normalize(float3(p3.x, 0, p3.z));
                    o.worldPos = mul(unity_ObjectToWorld, float4(center + p3, 1));
                    triStream.Append(o);
                    
                    triStream.RestartStrip();
                }
            }

            // Функция для создания сферы
            void CreateSphere(float3 center, inout TriangleStream<g2f> triStream)
            {
                float s = _ShapeSize;
                float3 vertices[6] = {
                    float3(0, s, 0), float3(0, -s, 0),
                    float3(s, 0, 0), float3(-s, 0, 0),
                    float3(0, 0, s), float3(0, 0, -s)
                };
                
                int indices[24] = {
                    0,2,4, 0,4,3, 0,3,5, 0,5,2,
                    1,4,2, 1,3,4, 1,5,3, 1,2,5
                };
                
                for(int i = 0; i < 8; i++)
                {
                    for(int j = 0; j < 3; j++)
                    {
                        g2f o;
                        float3 vertex = vertices[indices[i*3 + j]];
                        vertex = normalize(vertex) * s;
                        
                        o.pos = UnityObjectToClipPos(float4(center + vertex, 1));
                        o.normal = normalize(vertex);
                        o.worldPos = mul(unity_ObjectToWorld, float4(center + vertex, 1));
                        o.uv = float2(0, 0);
                        
                        triStream.Append(o);
                    }
                    triStream.RestartStrip();
                }
            }

            [maxvertexcount(60)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                // Для каждой вершины треугольника создаем форму
                for(int v = 0; v < 3; v++)
                {
                    float3 center = input[v].pos.xyz;
                    
                    #ifdef _SHAPETYPE_SPHERE
                        CreateSphere(center, triStream);
                    #endif
                    
                    #ifdef _SHAPETYPE_CUBE
                        CreateCube(center, triStream);
                    #endif
                    
                    #ifdef _SHAPETYPE_TRIANGLE
                        CreateTriangle(center, triStream);
                    #endif
                    
                    #ifdef _SHAPETYPE_CYLINDER
                        CreateCylinder(center, triStream);
                    #endif
                }
            }

            fixed4 frag (g2f i) : SV_Target
            {
                // Простое освещение
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(dot(i.normal, lightDir), 0.0);
                
                fixed4 col = _Color * (NdotL * 0.8 + 0.2);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}