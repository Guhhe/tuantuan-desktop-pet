using Microsoft.Win32;

namespace TuantuanDesktopPet;

internal sealed class AutoStartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "TuantuanDesktopPet";

    internal void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true);
        if (!enabled)
        {
            key.DeleteValue(ValueName, false);
            return;
        }

        var executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("无法确定团团桌宠程序路径。");
        key.SetValue(ValueName, $"\"{executable}\"", RegistryValueKind.String);
    }

    internal bool IsCurrentPathRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        var expected = $"\"{Environment.ProcessPath}\"";
        return string.Equals(key?.GetValue(ValueName) as string, expected, StringComparison.OrdinalIgnoreCase);
    }
}
