using System.IO;
using System.Text.Json;
using TuantuanDesktopPet.Core;

namespace TuantuanDesktopPet;

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _directory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TuantuanDesktopPet");
    private readonly string _settingsPath;

    internal SettingsStore()
    {
        _settingsPath = Path.Combine(_directory, "settings.json");
    }

    internal bool IsFirstRun { get; private set; }

    internal PetSettings Load()
    {
        Directory.CreateDirectory(_directory);
        if (!File.Exists(_settingsPath))
        {
            IsFirstRun = true;
            return new PetSettings();
        }

        try
        {
            var settings = JsonSerializer.Deserialize<PetSettings>(
                File.ReadAllText(_settingsPath),
                JsonOptions) ?? throw new JsonException("设置内容为空。");
            settings.Normalize();
            return settings;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            var backupName = $"settings.corrupt.{DateTime.Now:yyyyMMdd-HHmmss}.json";
            File.Move(_settingsPath, Path.Combine(_directory, backupName), false);
            IsFirstRun = true;
            return new PetSettings();
        }
    }

    internal void Save(PetSettings settings)
    {
        settings.Normalize();
        Directory.CreateDirectory(_directory);
        var temporaryPath = Path.Combine(_directory, "settings.json.tmp");
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, _settingsPath, true);
    }
}
