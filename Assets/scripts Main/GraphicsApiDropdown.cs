using System;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class GraphicsApiDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    private struct ApiOption
    {
        public readonly string Label;
        public readonly string LaunchArg;
        public readonly GraphicsDeviceType DeviceType;

        public ApiOption(string label, string launchArg, GraphicsDeviceType deviceType)
        {
            Label = label;
            LaunchArg = launchArg;
            DeviceType = deviceType;
        }
    }

    private readonly List<ApiOption> _options = new List<ApiOption>();

    private void Awake()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        BuildOptionsForCurrentPlatform();
        FillDropdown();
    }

    private void BuildOptionsForCurrentPlatform()
    {
        _options.Clear();

#if UNITY_STANDALONE_WIN
        _options.Add(new ApiOption("DirectX 11", "-force-d3d11", GraphicsDeviceType.Direct3D11));
        _options.Add(new ApiOption("DirectX 12", "-force-d3d12", GraphicsDeviceType.Direct3D12));
        _options.Add(new ApiOption("Vulkan", "-force-vulkan", GraphicsDeviceType.Vulkan));
#elif UNITY_STANDALONE_LINUX
        _options.Add(new ApiOption("Vulkan", "-force-vulkan", GraphicsDeviceType.Vulkan));
        _options.Add(new ApiOption("OpenGL Core", "-force-glcore", GraphicsDeviceType.OpenGLCore));
#elif UNITY_STANDALONE_OSX
        // Apple убрала поддержку OpenGL, поэтому на macOS реально доступен только Metal.
        _options.Add(new ApiOption("Metal", "-force-metal", GraphicsDeviceType.Metal));
#endif
    }

    private void FillDropdown()
    {
        if (dropdown == null)
        {
            Debug.LogError("GraphicsApiDropdown: не назначен TMP_Dropdown.");
            return;
        }

        dropdown.ClearOptions();

        var labels = new List<string>();
        foreach (var option in _options)
            labels.Add(option.Label);

        dropdown.AddOptions(labels);

        int currentIndex = _options.FindIndex(o => o.DeviceType == SystemInfo.graphicsDeviceType);
        dropdown.SetValueWithoutNotify(currentIndex >= 0 ? currentIndex : 0);

        dropdown.onValueChanged.RemoveListener(OnApiSelected);
        dropdown.onValueChanged.AddListener(OnApiSelected);
    }

    private void OnApiSelected(int index)
    {
        if (index < 0 || index >= _options.Count)
            return;

        ApiOption chosen = _options[index];

        if (chosen.DeviceType == SystemInfo.graphicsDeviceType)
            return; // этот API уже активен, перезапуск не нужен

        RestartWithGraphicsApi(chosen.LaunchArg);
    }

    private void RestartWithGraphicsApi(string launchArgument)
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;

            var startInfo = new ProcessStartInfo(exePath, launchArgument)
            {
                UseShellExecute = true
            };

            Process.Start(startInfo);
            Application.Quit();
        }
        catch (Exception e)
        {
            Debug.LogError($"Не удалось перезапустить приложение для смены Graphics API: {e.Message}");
        }
#else
        Debug.LogWarning("Switch Graphics API  (Windows/macOS/Linux).");
#endif
    }
}
