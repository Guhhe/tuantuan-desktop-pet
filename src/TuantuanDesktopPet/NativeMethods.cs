using System.Runtime.InteropServices;
using System.Text;

namespace TuantuanDesktopPet;

internal static class NativeMethods
{
    internal const int GwlExStyle = -20;
    internal const long WsExToolWindow = 0x00000080L;
    internal const long WsExNoActivate = 0x08000000L;
    internal const uint SwpNoSize = 0x0001;
    internal const uint SwpNoActivate = 0x0010;
    internal const uint SwpNoZOrder = 0x0004;
    internal const uint SwpMoveOnly = SwpNoSize | SwpNoActivate | SwpNoZOrder;
    internal const uint MonitorDefaultToNearest = 0x00000002;
    internal const uint GaRoot = 2;
    internal static readonly nint HwndBroadcast = new(0xffff);
    internal static readonly int WakeMessage =
        RegisterWindowMessage("TuantuanDesktopPet.WakeExistingInstance.v1");

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;

        internal readonly int Width => Right - Left;
        internal readonly int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct MonitorInfoEx
    {
        internal int CbSize;
        internal Rect Monitor;
        internal Rect Work;
        internal uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        internal string DeviceName;
    }

    internal delegate bool MonitorEnumProc(nint monitor, nint hdc, ref Rect rect, nint data);

    [DllImport("user32.dll")]
    internal static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    internal static extern bool GetWindowRect(nint hwnd, out Rect rect);

    [DllImport("user32.dll")]
    internal static extern bool SetWindowPos(
        nint hwnd,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromWindow(nint hwnd, uint flags);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromPoint(Point point, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern bool GetMonitorInfo(nint monitor, ref MonitorInfoEx info);

    [DllImport("user32.dll")]
    internal static extern bool EnumDisplayMonitors(
        nint hdc,
        nint clip,
        MonitorEnumProc callback,
        nint data);

    [DllImport("user32.dll")]
    internal static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern nint GetShellWindow();

    [DllImport("user32.dll")]
    internal static extern bool IsWindowVisible(nint hwnd);

    [DllImport("user32.dll")]
    internal static extern bool IsIconic(nint hwnd);

    [DllImport("user32.dll")]
    internal static extern nint GetAncestor(nint hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint hwnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr64(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr64(nint hwnd, int index, nint value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong32(nint hwnd, int index, int value);

    [DllImport("user32.dll")]
    private static extern int RegisterWindowMessage(string value);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hwnd, int message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hwnd, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hwnd);

    internal static long GetExtendedStyle(nint hwnd) =>
        nint.Size == 8 ? GetWindowLongPtr64(hwnd, GwlExStyle).ToInt64() : GetWindowLong32(hwnd, GwlExStyle);

    internal static void SetExtendedStyle(nint hwnd, long style)
    {
        if (nint.Size == 8)
        {
            _ = SetWindowLongPtr64(hwnd, GwlExStyle, new nint(style));
        }
        else
        {
            _ = SetWindowLong32(hwnd, GwlExStyle, (int)style);
        }
    }

    internal static string GetWindowClass(nint hwnd)
    {
        var value = new StringBuilder(128);
        _ = GetClassName(hwnd, value, value.Capacity);
        return value.ToString();
    }

    internal static void BroadcastWakeExistingInstance() =>
        _ = PostMessage(HwndBroadcast, WakeMessage, nint.Zero, nint.Zero);

    internal static void RestoreWindow(nint hwnd)
    {
        _ = ShowWindow(hwnd, 4);
        _ = SetForegroundWindow(hwnd);
    }
}
