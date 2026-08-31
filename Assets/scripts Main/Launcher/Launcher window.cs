using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class WindowsDisplayManager : MonoBehaviour
{
    [System.Serializable]
    public class MonitorInfo
    {
        public string name;
        public int width;
        public int height;
        public int x;
        public int y;
        public bool isPrimary;
    }

    // Windows API для работы с окнами и мониторами
    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll")]
    static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll")]
    static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    // Константы
    const uint SWP_SHOWWINDOW = 0x0040;
    const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 0x20;
        private const int CCHFORMNAME = 0x20;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    private List<MonitorInfo> availableMonitors = new List<MonitorInfo>();
    private static List<MONITORINFOEX> monitorInfoList = new List<MONITORINFOEX>();

    public List<MonitorInfo> GetAvailableMonitors()
    {
        availableMonitors.Clear();
        monitorInfoList.Clear();

        // Перечисляем все мониторы
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);

        // Конвертируем в удобный формат
        for (int i = 0; i < monitorInfoList.Count; i++)
        {
            var monitor = monitorInfoList[i];
            var info = new MonitorInfo
            {
                name = $"Монитор {i + 1} ({monitor.szDevice})",
                width = monitor.rcMonitor.right - monitor.rcMonitor.left,
                height = monitor.rcMonitor.bottom - monitor.rcMonitor.top,
                x = monitor.rcMonitor.left,
                y = monitor.rcMonitor.top,
                isPrimary = (monitor.dwFlags & 1) != 0
            };
            availableMonitors.Add(info);
        }

        return availableMonitors;
    }

    static bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
    {
        MONITORINFOEX mi = new MONITORINFOEX();
        mi.cbSize = Marshal.SizeOf(mi);

        if (GetMonitorInfo(hMonitor, ref mi))
        {
            monitorInfoList.Add(mi);
        }

        return true;
    }

    public void MoveWindowToMonitor(int monitorIndex)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try
        {
            IntPtr window = GetActiveWindow();
            if (window != IntPtr.Zero && monitorIndex < availableMonitors.Count)
            {
                var monitor = availableMonitors[monitorIndex];
                
                // Вычисляем позицию для центрирования окна на мониторе
                int windowWidth = Screen.width;
                int windowHeight = Screen.height;
                int x = monitor.x + (monitor.width - windowWidth) / 2;
                int y = monitor.y + (monitor.height - windowHeight) / 2;

                SetWindowPos(window, IntPtr.Zero, x, y, windowWidth, windowHeight, SWP_SHOWWINDOW);
                Debug.Log($"Окно перемещено на монитор {monitorIndex + 1}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка перемещения окна: {e.Message}");
        }
#endif
    }

    public string GetDetailedMonitorInfo(int monitorIndex)
    {
        if (monitorIndex >= 0 && monitorIndex < availableMonitors.Count)
        {
            var monitor = availableMonitors[monitorIndex];
            return $"{monitor.name}\n" +
                   $"Разрешение: {monitor.width} x {monitor.height}\n" +
                   $"Позиция: ({monitor.x}, {monitor.y})\n" +
                   $"Основной: {(monitor.isPrimary ? "Да" : "Нет")}";
        }
        return "Монитор не найден";
    }

    void Start()
    {
        // Получаем информацию о мониторах при старте
        GetAvailableMonitors();

        // Выводим информацию о найденных мониторах
        for (int i = 0; i < availableMonitors.Count; i++)
        {
            Debug.Log($"Найден {GetDetailedMonitorInfo(i)}");
        }
    }
}