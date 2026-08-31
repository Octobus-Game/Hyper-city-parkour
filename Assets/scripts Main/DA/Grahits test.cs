#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class ColorSpaceSwitcher : MonoBehaviour
{
    [Header("UI Settings")]
    public bool showGUI = true;
    public Rect buttonRect = new Rect(10, 10, 200, 30);

    void Start()
    {
        Debug.Log($"Current Color Space: {QualitySettings.activeColorSpace}");
    }

    void OnGUI()
    {
        if (!showGUI) return;

        // Показать текущий Color Space
        GUI.Label(new Rect(10, 50, 200, 20), $"Current: {QualitySettings.activeColorSpace}");

        // Предупреждение о том, что работает только в Editor
        GUI.Label(new Rect(10, 70, 300, 20), "Works only in Editor mode!");

        // Кнопка для переключения на Linear
        if (GUI.Button(buttonRect, "Switch to Linear"))
        {
            SwitchToLinear();
        }

        // Кнопка для переключения на Gamma
        if (GUI.Button(new Rect(10, 90, 200, 30), "Switch to Gamma"))
        {
            SwitchToGamma();
        }

        // Кнопка для переключения между режимами
        if (GUI.Button(new Rect(10, 130, 200, 30), "Toggle Color Space"))
        {
            ToggleColorSpace();
        }
    }

    /// <summary>
    /// Переключить на Linear Color Space (только в Editor)
    /// </summary>
    public void SwitchToLinear()
    {
        if (PlayerSettings.colorSpace != ColorSpace.Linear)
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("Switched to Linear Color Space");

            // Принудительно обновить настройки
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.Log("Already in Linear Color Space");
        }
    }

    /// <summary>
    /// Переключить на Gamma Color Space (только в Editor)
    /// </summary>
    public void SwitchToGamma()
    {
        if (PlayerSettings.colorSpace != ColorSpace.Gamma)
        {
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            Debug.Log("Switched to Gamma Color Space");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.Log("Already in Gamma Color Space");
        }
    }

    /// <summary>
    /// Переключить между Linear и Gamma
    /// </summary>
    public void ToggleColorSpace()
    {
        if (PlayerSettings.colorSpace == ColorSpace.Linear)
        {
            SwitchToGamma();
        }
        else
        {
            SwitchToLinear();
        }
    }

    /// <summary>
    /// Получить текущий Color Space из настроек проекта
    /// </summary>
    public ColorSpace GetProjectColorSpace()
    {
        return PlayerSettings.colorSpace;
    }
}

// Editor Window версия для более удобного использования
public class ColorSpaceWindow : EditorWindow
{
    [MenuItem("Tools/Color Space Switcher")]
    static void ShowWindow()
    {
        GetWindow<ColorSpaceWindow>("Color Space");
    }

    void OnGUI()
    {
        GUILayout.Label("Color Space Switcher", EditorStyles.boldLabel);

        GUILayout.Space(10);

        GUILayout.Label($"Current Color Space: {PlayerSettings.colorSpace}");
        GUILayout.Label($"Active Color Space: {QualitySettings.activeColorSpace}");

        GUILayout.Space(10);

        if (GUILayout.Button("Switch to Linear"))
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("Color Space changed to Linear");
        }

        if (GUILayout.Button("Switch to Gamma"))
        {
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            Debug.Log("Color Space changed to Gamma");
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Toggle Color Space"))
        {
            PlayerSettings.colorSpace = (PlayerSettings.colorSpace == ColorSpace.Linear)
                ? ColorSpace.Gamma
                : ColorSpace.Linear;

            Debug.Log($"Color Space toggled to: {PlayerSettings.colorSpace}");
        }
    }
}

#else
using UnityEngine;

public class ColorSpaceSwitcher : MonoBehaviour
{
    void Start()
    {
        Debug.LogWarning("Color Space switching is only available in Unity Editor!");
        Debug.Log($"Current Color Space: {QualitySettings.activeColorSpace}");
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 40), "Color Space switching works only in Editor mode!");
        GUI.Label(new Rect(10, 50, 300, 20), $"Current: {QualitySettings.activeColorSpace}");
    }
}
#endif