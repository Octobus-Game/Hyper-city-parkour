using UnityEngine;

public class TextureReplacer : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Объект, на котором нужно заменить текстуру")]
    public Renderer targetRenderer;

    [Header("Texture Settings")]
    [Tooltip("Новая текстура для замены")]
    public Texture2D newTexture;

    [Tooltip("Индекс материала (если у объекта несколько материалов)")]
    public int materialIndex = 0;

    [Tooltip("Имя свойства текстуры в шейдере (обычно _MainTex)")]
    public string texturePropertyName = "_MainTex";

    [Header("Options")]
    [Tooltip("Заменить текстуру при старте")]
    public bool replaceOnStart = true;

    [Tooltip("Создать копию материала (чтобы не изменять оригинал)")]
    public bool createMaterialInstance = true;

    private Material materialInstance;

    void Start()
    {
        if (replaceOnStart)
        {
            ReplaceTexture();
        }
    }

    /// <summary>
    /// Заменяет текстуру на целевом объекте
    /// </summary>
    public void ReplaceTexture()
    {
        // Проверка наличия всех необходимых компонентов
        if (targetRenderer == null)
        {
            Debug.LogError("Target Renderer не указан!");
            return;
        }

        if (newTexture == null)
        {
            Debug.LogError("New Texture не указана!");
            return;
        }

        // Проверка индекса материала
        if (materialIndex < 0 || materialIndex >= targetRenderer.sharedMaterials.Length)
        {
            Debug.LogError($"Material Index {materialIndex} вне диапазона! У объекта {targetRenderer.sharedMaterials.Length} материалов.");
            return;
        }

        // Получаем материал
        Material targetMaterial;

        if (createMaterialInstance)
        {
            // Создаём копию материала
            Material[] materials = targetRenderer.materials;
            targetMaterial = materials[materialIndex];
            materialInstance = targetMaterial;
        }
        else
        {
            // Используем shared material (изменит все объекты с этим материалом)
            targetMaterial = targetRenderer.sharedMaterials[materialIndex];
        }

        // Заменяем текстуру
        if (targetMaterial.HasProperty(texturePropertyName))
        {
            targetMaterial.SetTexture(texturePropertyName, newTexture);
            Debug.Log($"Текстура успешно заменена на объекте {targetRenderer.gameObject.name}");
        }
        else
        {
            Debug.LogError($"Свойство {texturePropertyName} не найдено в материале {targetMaterial.name}");
        }
    }

    /// <summary>
    /// Восстанавливает оригинальную текстуру
    /// </summary>
    public void RestoreOriginalTexture()
    {
        if (targetRenderer != null && materialInstance != null && createMaterialInstance)
        {
            Material[] materials = targetRenderer.materials;

            // Восстанавливаем оригинальный материал
            materials[materialIndex] = targetRenderer.sharedMaterials[materialIndex];
            targetRenderer.materials = materials;

            // Уничтожаем копию материала
            if (Application.isPlaying)
            {
                Destroy(materialInstance);
            }
            else
            {
                DestroyImmediate(materialInstance);
            }

            materialInstance = null;
            Debug.Log("Оригинальная текстура восстановлена");
        }
    }

    /// <summary>
    /// Заменяет текстуру на новую (можно вызвать из другого скрипта)
    /// </summary>
    public void ReplaceWithTexture(Texture2D texture)
    {
        newTexture = texture;
        ReplaceTexture();
    }

    void OnDestroy()
    {
        // Очищаем созданный материал при уничтожении объекта
        if (materialInstance != null && createMaterialInstance)
        {
            if (Application.isPlaying)
            {
                Destroy(materialInstance);
            }
            else
            {
                DestroyImmediate(materialInstance);
            }
        }
    }

    // Для визуализации в редакторе
    void OnDrawGizmosSelected()
    {
        if (targetRenderer != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetRenderer.transform.position);
        }
    }
}