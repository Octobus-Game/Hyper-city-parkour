using UnityEngine;
using UnityEditor;

public class EditorThemeChanger : EditorWindow
{
    private Color backgroundColor = new Color(0.15f, 0.15f, 0.2f, 1f);
    private Color accentColor    = new Color(0.2f, 0.6f, 1f, 1f);
    private Color textColor      = new Color(0.9f, 0.9f, 0.9f, 1f);
    private Color toolbarColor   = new Color(0.1f, 0.1f, 0.15f, 1f);

    private static GUISkin customSkin;

    [MenuItem("Tools/Editor Theme Changer")]
    public static void ShowWindow()
    {
        GetWindow<EditorThemeChanger>("Theme Changer");
    }

    private void OnGUI()
    {
        GUILayout.Label("🎨 Editor Theme Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        accentColor     = EditorGUILayout.ColorField("Accent Color",     accentColor);
        textColor       = EditorGUILayout.ColorField("Text Color",       textColor);
        toolbarColor    = EditorGUILayout.ColorField("Toolbar Color",    toolbarColor);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Apply Theme", GUILayout.Height(35)))
            ApplyTheme();

        if (GUILayout.Button("Reset to Default", GUILayout.Height(30)))
            ResetTheme();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Изменения применяются к кастомным окнам через GUISkin.\n" +
            "Для глобального изменения используйте Unity Preferences → Colors.",
            MessageType.Info);
    }

    private void ApplyTheme()
    {
        // Сохраняем цвета в EditorPrefs
        SaveColorPref("BG",      backgroundColor);
        SaveColorPref("Accent",  accentColor);
        SaveColorPref("Text",    textColor);
        SaveColorPref("Toolbar", toolbarColor);

        // Применяем к текущему skin
        ApplySkinColors();

        Debug.Log("✅ Тема применена!");
        Repaint();
    }

    private void ApplySkinColors()
    {
        GUISkin skin = GUI.skin;
        if (skin == null) return;

        // Window
        skin.window.normal.background    = MakeTexture(2, 2, backgroundColor);
        skin.window.onNormal.background  = MakeTexture(2, 2, backgroundColor);

        // Box
        skin.box.normal.background       = MakeTexture(2, 2, toolbarColor);

        // Button
        skin.button.normal.background    = MakeTexture(2, 2, accentColor);
        skin.button.normal.textColor     = textColor;
        skin.button.hover.background     = MakeTexture(2, 2, accentColor * 1.2f);

        // Label
        skin.label.normal.textColor      = textColor;

        // TextField
        skin.textField.normal.background = MakeTexture(2, 2, toolbarColor * 1.3f);
        skin.textField.normal.textColor  = textColor;
    }

    private void ResetTheme()
    {
        backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        accentColor     = new Color(0.33f, 0.33f, 0.33f, 1f);
        textColor       = new Color(0.80f, 0.80f, 0.80f, 1f);
        toolbarColor    = new Color(0.18f, 0.18f, 0.18f, 1f);

        ApplySkinColors();
        Debug.Log("🔄 Тема сброшена по умолчанию");
        Repaint();
    }

    // Создаёт однотонную текстуру для фонов
    private Texture2D MakeTexture(int width, int height, Color col)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = col;

        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // Хелперы для EditorPrefs
    private void SaveColorPref(string key, Color c)
    {
        EditorPrefs.SetFloat(key + "_R", c.r);
        EditorPrefs.SetFloat(key + "_G", c.g);
        EditorPrefs.SetFloat(key + "_B", c.b);
        EditorPrefs.SetFloat(key + "_A", c.a);
    }

    private Color LoadColorPref(string key, Color fallback)
    {
        if (!EditorPrefs.HasKey(key + "_R")) return fallback;
        return new Color(
            EditorPrefs.GetFloat(key + "_R"),
            EditorPrefs.GetFloat(key + "_G"),
            EditorPrefs.GetFloat(key + "_B"),
            EditorPrefs.GetFloat(key + "_A")
        );
    }

    // Загружаем сохранённые цвета при открытии окна
    private void OnEnable()
    {
        backgroundColor = LoadColorPref("BG",      backgroundColor);
        accentColor     = LoadColorPref("Accent",  accentColor);
        textColor       = LoadColorPref("Text",    textColor);
        toolbarColor    = LoadColorPref("Toolbar", toolbarColor);
    }
}