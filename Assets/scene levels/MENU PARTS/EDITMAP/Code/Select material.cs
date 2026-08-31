using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

public class ImageSelectorUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button[] selectImageButtons;

    [Header("Materials to Replace")]
    [SerializeField] private Material[] materialsToReplace;

    [Header("Settings")]
    [SerializeField] private bool replaceMainTexture = true;
    [SerializeField] private string texturePropertyName = "_MainTex";
    [SerializeField] private bool keepOriginalMaterials = true;
    [SerializeField] private FilterMode textureFilterMode = FilterMode.Bilinear;
    [SerializeField] private TextureWrapMode textureWrapMode = TextureWrapMode.Clamp;

    [Header("Optional: Objects with Renderers")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private int[] materialIndices = { 0, 1 }; // Индексы материалов для замены

    private Material[] originalMaterials;
    private System.Collections.Generic.Dictionary<Renderer, Material[]> originalRendererMaterials;

#if UNITY_EDITOR
    void Start()
    {
        SetupButton();
        SaveOriginalMaterials();
    }
#endif

#if UNITY_STANDALONE_WIN
    [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileName
    {
        public int lStructSize = 0;
        public System.IntPtr hwndOwner = System.IntPtr.Zero;
        public System.IntPtr hInstance = System.IntPtr.Zero;
        public string lpstrFilter = null;
        public string lpstrCustomFilter = null;
        public int nMaxCustFilter = 0;
        public int nFilterIndex = 1;
        public string lpstrFile = null;
        public int nMaxFile = 0;
        public string lpstrFileTitle = null;
        public int nMaxFileTitle = 0;
        public string lpstrInitialDir = null;
        public string lpstrTitle = null;
        public int Flags = 0;
        public short nFileOffset = 0;
        public short nFileExtension = 0;
        public string lpstrDefExt = null;
        public System.IntPtr lCustData = System.IntPtr.Zero;
        public System.IntPtr lpfnHook = System.IntPtr.Zero;
        public string lpTemplateName = null;
        public System.IntPtr lpEditInfo = System.IntPtr.Zero;
        public string lpstrPrompt = null;
        public System.IntPtr lpVerbatim = System.IntPtr.Zero;
    }
#endif

    void Start01()
    {
        SetupButton();
        SaveOriginalMaterials();
    }

    private void SetupButton()
    {
        // Если массив кнопок пуст, пытаемся найти кнопку на текущем объекте
        if (selectImageButtons == null || selectImageButtons.Length == 0)
        {
            Button currentButton = GetComponent<Button>();
            if (currentButton != null)
            {
                selectImageButtons = new Button[] { currentButton };
            }
        }

        // Добавляем слушателей ко всем кнопкам
        if (selectImageButtons != null && selectImageButtons.Length > 0)
        {
            foreach (Button button in selectImageButtons)
            {
                if (button != null)
                {
                    button.onClick.AddListener(OpenFileDialog);
                }
            }
        }
        else
        {
            Debug.LogError("Кнопки не найдены! Прикрепите скрипт к объекту с Button или укажите кнопки в массиве Select Image Buttons.");
        }
    }

    private void SaveOriginalMaterials()
    {
        // Сохраняем оригинальные материалы из массива materialsToReplace
        if (materialsToReplace != null && materialsToReplace.Length > 0)
        {
            originalMaterials = new Material[materialsToReplace.Length];
            for (int i = 0; i < materialsToReplace.Length; i++)
            {
                if (materialsToReplace[i] != null)
                {
                    originalMaterials[i] = new Material(materialsToReplace[i]);
                }
            }
        }

        // Сохраняем оригинальные материалы из рендеров
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            originalRendererMaterials = new System.Collections.Generic.Dictionary<Renderer, Material[]>();
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer != null)
                {
                    Material[] rendererMats = new Material[renderer.materials.Length];
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        rendererMats[i] = new Material(renderer.materials[i]);
                    }
                    originalRendererMaterials[renderer] = rendererMats;
                }
            }
        }
    }

    public void OpenFileDialog()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        OpenFileName ofn = new OpenFileName();
        ofn.lStructSize = Marshal.SizeOf(ofn);
        ofn.lpstrFilter = "Image Files\0*.png;*.jpg;*.jpeg\0PNG Files\0*.png\0JPG Files\0*.jpg;*.jpeg\0All Files\0*.*\0";
        ofn.lpstrFile = new string(new char[256]);
        ofn.nMaxFile = ofn.lpstrFile.Length;
        ofn.lpstrFileTitle = new string(new char[64]);
        ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
        ofn.lpstrTitle = "Выберите изображение";
        ofn.Flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
        
        if (GetOpenFileName(ofn))
        {
            string selectedPath = ofn.lpstrFile;
            LoadImageFromPath(selectedPath);
        }
#elif UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Выберите изображение", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            LoadImageFromPath(path);
        }
#else
        Debug.LogWarning("Открытие файлового диалога поддерживается только в редакторе Unity и на Windows.");
#endif
    }

    private void LoadImageFromPath(string path)
    {
        if (File.Exists(path))
        {
            StartCoroutine(LoadImage(path));
        }
        else
        {
            Debug.LogError("Файл не найден: " + path);
        }
    }

    private IEnumerator LoadImage(string path)
    {
        byte[] imageData = File.ReadAllBytes(path);

        if (imageData != null && imageData.Length > 0)
        {
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageData))
            {
                texture.filterMode = textureFilterMode;
                texture.wrapMode = textureWrapMode;

                ApplyTextureToMaterials(texture);

                Debug.Log("Изображение успешно загружено и применено: " + Path.GetFileName(path));
            }
            else
            {
                Debug.LogError("Не удалось загрузить изображение: " + path);
                Destroy(texture);
            }
        }

        yield return null;
    }

    private void ApplyTextureToMaterials(Texture2D texture)
    {
        // Применяем к указанным материалам
        if (materialsToReplace != null && materialsToReplace.Length > 0)
        {
            foreach (Material mat in materialsToReplace)
            {
                if (mat != null)
                {
                    if (keepOriginalMaterials)
                    {
                        // Создаем копию материала
                        Material newMaterial = new Material(mat);
                        if (replaceMainTexture)
                        {
                            newMaterial.mainTexture = texture;
                        }
                        else
                        {
                            newMaterial.SetTexture(texturePropertyName, texture);
                        }
                    }
                    else
                    {
                        // Изменяем оригинальный материал
                        if (replaceMainTexture)
                        {
                            mat.mainTexture = texture;
                        }
                        else
                        {
                            mat.SetTexture(texturePropertyName, texture);
                        }
                    }
                }
            }
        }

        // Применяем к рендererам по индексам
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer != null)
                {
                    Material[] materials = renderer.materials;

                    foreach (int index in materialIndices)
                    {
                        if (index >= 0 && index < materials.Length)
                        {
                            if (keepOriginalMaterials)
                            {
                                materials[index] = new Material(materials[index]);
                            }

                            if (replaceMainTexture)
                            {
                                materials[index].mainTexture = texture;
                            }
                            else
                            {
                                materials[index].SetTexture(texturePropertyName, texture);
                            }
                        }
                    }

                    renderer.materials = materials;
                }
            }
        }
    }

    // Публичные методы для восстановления оригинальных материалов
    public void RestoreOriginalMaterials()
    {
        if (originalMaterials != null)
        {
            if (materialsToReplace != null && materialsToReplace.Length > 0)
            {
                for (int i = 0; i < materialsToReplace.Length && i < originalMaterials.Length; i++)
                {
                    if (materialsToReplace[i] != null && originalMaterials[i] != null)
                    {
                        materialsToReplace[i].CopyPropertiesFromMaterial(originalMaterials[i]);
                    }
                }
            }

            if (targetRenderers != null && targetRenderers.Length > 0)
            {
                // Восстанавливаем материалы для всех рендereров
                int materialIndex = 0;
                foreach (Renderer renderer in targetRenderers)
                {
                    if (renderer != null)
                    {
                        Material[] rendererMaterials = new Material[renderer.materials.Length];
                        for (int i = 0; i < rendererMaterials.Length; i++)
                        {
                            if (materialIndex < originalMaterials.Length)
                            {
                                rendererMaterials[i] = originalMaterials[materialIndex];
                                materialIndex++;
                            }
                        }
                        renderer.materials = rendererMaterials;
                    }
                }
            }
        }
    }

    // Метод для программного вызова
    public void SelectImage()
    {
        OpenFileDialog();
    }

    void OnDestroy()
    {
        // Очищаем слушатели для всех кнопок
        if (selectImageButtons != null && selectImageButtons.Length > 0)
        {
            foreach (Button button in selectImageButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(OpenFileDialog);
                }
            }
        }
    }
}