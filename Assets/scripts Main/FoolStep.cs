using UnityEngine;
using System.Collections.Generic;

public class FootprintManager : MonoBehaviour
{
    [Header("Footprint Settings")]
    public Material targetMaterial; // Материал с шейдером HashTiling
    public int textureResolution = 1024;
    public float fadeSpeed = 0.1f; // Скорость затухания следов
    public float footprintSize = 0.5f;
    public Color footprintColor = Color.black;

    [Header("Performance")]
    public int maxFootprints = 100;

    private RenderTexture footprintTexture;
    private RenderTexture tempTexture;
    private Material drawMaterial;
    private Queue<FootprintData> footprintQueue = new Queue<FootprintData>();

    private struct FootprintData
    {
        public Vector2 position;
        public float rotation;
        public float time;
    }

    void Start()
    {
        InitializeTextures();
        CreateDrawMaterial();

        if (targetMaterial != null)
        {
            targetMaterial.SetTexture("_FootprintTex", footprintTexture);
        }
    }

    void InitializeTextures()
    {
        footprintTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGBFloat);
        footprintTexture.filterMode = FilterMode.Bilinear;
        footprintTexture.wrapMode = TextureWrapMode.Repeat;

        tempTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGBFloat);
        tempTexture.filterMode = FilterMode.Bilinear;
        tempTexture.wrapMode = TextureWrapMode.Repeat;

        // Очищаем текстуру
        RenderTexture.active = footprintTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;
    }

    void CreateDrawMaterial()
    {
        Shader drawShader = Shader.Find("Hidden/FootprintDraw");
        if (drawShader == null)
        {
            Debug.LogError("Shader 'Hidden/FootprintDraw' not found!");
            return;
        }
        drawMaterial = new Material(drawShader);
    }

    public void AddFootprint(Vector3 worldPosition, Vector3 normal)
    {
        // Конвертируем мировые координаты в UV координаты
        Vector2 uv = WorldToUV(worldPosition);

        // Вычисляем вращение на основе нормали или направления движения
        float rotation = Mathf.Atan2(normal.x, normal.z);

        FootprintData footprint = new FootprintData
        {
            position = uv,
            rotation = rotation,
            time = Time.time
        };

        footprintQueue.Enqueue(footprint);

        if (footprintQueue.Count > maxFootprints)
        {
            footprintQueue.Dequeue();
        }

        DrawFootprint(uv, rotation);
    }

    Vector2 WorldToUV(Vector3 worldPos)
    {
        // Преобразуем мировые координаты в UV (0-1)
        // Это зависит от размера и позиции вашей поверхности
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            Vector2 uv = new Vector2(
                Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldPos.x),
                Mathf.InverseLerp(bounds.min.z, bounds.max.z, worldPos.z)
            );
            return uv;
        }

        return new Vector2(worldPos.x % 1f, worldPos.z % 1f);
    }

    void DrawFootprint(Vector2 uv, float rotation)
    {
        if (drawMaterial == null) return;

        // Копируем текущее состояние
        Graphics.Blit(footprintTexture, tempTexture);

        // Настраиваем параметры для рисования
        drawMaterial.SetVector("_FootprintPos", uv);
        drawMaterial.SetFloat("_FootprintSize", footprintSize);
        drawMaterial.SetFloat("_FootprintRotation", rotation);
        drawMaterial.SetColor("_FootprintColor", footprintColor);

        // Рисуем след
        Graphics.Blit(tempTexture, footprintTexture, drawMaterial);
    }

    void Update()
    {
        // Постепенное затухание следов
        if (fadeSpeed > 0 && drawMaterial != null)
        {
            drawMaterial.SetFloat("_FadeAmount", fadeSpeed * Time.deltaTime);
            Graphics.Blit(footprintTexture, tempTexture);
            Graphics.Blit(tempTexture, footprintTexture, drawMaterial, 1); // Pass 1 для затухания
        }
    }

    void OnDestroy()
    {
        if (footprintTexture != null)
            footprintTexture.Release();
        if (tempTexture != null)
            tempTexture.Release();
        if (drawMaterial != null)
            Destroy(drawMaterial);
    }
}