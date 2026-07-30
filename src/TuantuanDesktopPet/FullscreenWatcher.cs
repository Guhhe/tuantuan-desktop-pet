namespace TuantuanDesktopPet;

internal sealed class FullscreenWatcher
{
    internal bool IsForeignFullscreenWindow(nint ownWindow)
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == nint.Zero ||
            foreground == ownWindow ||
            foreground == NativeMethods.GetShellWindow() ||
            !NativeMethods.IsWindowVisible(foreground) ||
            NativeMethods.IsIconic(foreground))
        {
            return false;
        }

        foreground = NativeMethods.GetAncestor(foreground, NativeMethods.GaRoot);
        var className = NativeMethods.GetWindowClass(foreground);
        if (className is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd")
        {
            return false;
        }

        if (!NativeMethods.GetWindowRect(foreground, out var window))
        {
            return false;
        }

        var monitor = NativeMethods.MonitorFromWindow(foreground, NativeMethods.MonitorDefaultToNearest);
        var info = new NativeMethods.MonitorInfoEx
        {
            CbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfoEx>(),
            DeviceName = string.Empty
        };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        const int tolerance = 2;
        return window.Left <= info.Monitor.Left + tolerance &&
               window.Top <= info.Monitor.Top + tolerance &&
               window.Right >= info.Monitor.Right - tolerance &&
               window.Bottom >= info.Monitor.Bottom - tolerance;
    }
}
