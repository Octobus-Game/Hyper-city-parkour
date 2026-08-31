using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// UI-логика выбора текстуры для спрея.
/// Вешается на объект игрока (рядом со SprayController) либо на UI Canvas —
/// главное, чтобы ссылка sprayTexture в SprayController указывала на этот компонент
/// локального игрока.
///
/// Требуется:
///  - Button "Задать свою текстуру"  -> setTextureButton
///  - Button "Reset текстуры"        -> resetTextureButton
///  - Image "итоговое изображение"   -> previewImage
///  - (опционально) TMP_Text для статуса -> statusLabel
/// </summary>
public class PlayerSprayTexture : MonoBehaviour
{
    [Header("Текстуры")]
    [Tooltip("Текстура по умолчанию. Должна иметь включённый Read/Write Enabled " +
             "в Import Settings, иначе EncodeToPNG выбросит исключение.")]
    public Texture2D defaultTexture;

    [Header("UI")]
    public Button setTextureButton;
    public Button resetTextureButton;
    public Image previewImage;

    [Header("TMP статус (необязательно)")]
    public TMP_Text statusLabel;

    private Texture2D _currentTexture;

    private void Start()
    {
        _currentTexture = defaultTexture;
        UpdatePreview(_currentTexture);
        SetStatus("Используется стандартная текстура");

        if (setTextureButton != null)
            setTextureButton.onClick.AddListener(OnSetTextureClicked);

        if (resetTextureButton != null)
            resetTextureButton.onClick.AddListener(OnResetTextureClicked);
    }

    private void OnSetTextureClicked()
    {
#if UNITY_EDITOR
        // Быстрый вариант для тестирования прямо в редакторе Unity.
        string path = EditorUtility.OpenFilePanel("Выбрать текстуру", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            LoadTextureFromFile(path);
        }
#else
        // В готовом билде у Unity нет встроенного диалога выбора файла.
        // Подключите рантайм-плагин, например "SimpleFileBrowser" или
        // "Standalone File Browser", и в его коллбэке вызовите
        // LoadTextureFromFile(выбранныйПуть).
        SetStatus("Диалог выбора файла не реализован для билда. " +
                   "Подключите runtime file picker.");
#endif
    }

    private void OnResetTextureClicked()
    {
        _currentTexture = defaultTexture;
        UpdatePreview(_currentTexture);
        SetStatus("Сброшено на стандартную текстуру");
    }

    private void LoadTextureFromFile(string path)
    {
        if (!File.Exists(path))
        {
            SetStatus("Файл не найден");
            return;
        }

        byte[] fileBytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (tex.LoadImage(fileBytes))
        {
            _currentTexture = tex;
            UpdatePreview(_currentTexture);
            SetStatus("Загружено: " + Path.GetFileName(path));
        }
        else
        {
            SetStatus("Не удалось загрузить изображение");
        }
    }

    private void UpdatePreview(Texture2D tex)
    {
        if (previewImage == null || tex == null) return;

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );
        previewImage.sprite = sprite;
    }

    private void SetStatus(string text)
    {
        if (statusLabel != null)
            statusLabel.text = text;
    }

    /// <summary>
    /// PNG-байты текущей выбранной текстуры.
    /// Используется SprayController'ом при спавне декаля на сервере.
    /// </summary>
    public byte[] GetCurrentTexturePng()
    {
        if (_currentTexture == null) return null;
        return _currentTexture.EncodeToPNG();
    }
}
