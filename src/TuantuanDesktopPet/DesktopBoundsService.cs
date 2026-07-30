namespace TuantuanDesktopPet;

internal sealed class DesktopBoundsService
{
    internal MonitorArea GetForWindow(nint hwnd)
    {
        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MonitorDefaultToNearest);
        return Get(monitor);
    }

    internal MonitorArea GetForPoint(int x, int y)
    {
        var monitor = NativeMethods.MonitorFromPoint(
            new NativeMethods.Point { X = x, Y = y },
            NativeMethods.MonitorDefaultToNearest);
        return Get(monitor);
    }

    internal IReadOnlyList<MonitorArea> GetAll()
    {
        var result = new List<MonitorArea>();
        _ = NativeMethods.EnumDisplayMonitors(
            nint.Zero,
            nint.Zero,
            (nint monitor, nint _, ref NativeMethods.Rect _, nint _) =>
            {
                result.Add(Get(monitor));
                return true;
            },
            nint.Zero);
        return result;
    }

    internal PixelPosition Clamp(MonitorArea area, int left, int top, int width, int height)
    {
        var maxLeft = Math.Max(area.Work.Left, area.Work.Right - width);
        var maxTop = Math.Max(area.Work.Top, area.Work.Bottom - height);
        return new PixelPosition(
            Math.Clamp(left, area.Work.Left, maxLeft),
            Math.Clamp(top, area.Work.Top, maxTop));
    }

    internal MonitorArea FindSavedMonitor(string? deviceName, int x, int y)
    {
        var monitors = GetAll();
        var saved = monitors.FirstOrDefault(
            candidate => string.Equals(candidate.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
        return saved ?? GetForPoint(x, y);
    }

    private static MonitorArea Get(nint monitor)
    {
        var info = new NativeMethods.MonitorInfoEx
        {
            CbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfoEx>(),
            DeviceName = string.Empty
        };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            throw new InvalidOperationException("无法读取显示器工作区。");
        }

        return new MonitorArea(monitor, info.DeviceName, info.Monitor, info.Work, (info.Flags & 1) != 0);
    }
}

internal sealed record MonitorArea(
    nint Handle,
    string DeviceName,
    NativeMethods.Rect Bounds,
    NativeMethods.Rect Work,
    bool IsPrimary);

internal readonly record struct PixelPosition(int Left, int Top);
