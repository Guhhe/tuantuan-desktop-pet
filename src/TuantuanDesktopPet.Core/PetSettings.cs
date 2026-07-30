namespace TuantuanDesktopPet.Core;

public sealed class PetSettings
{
    public int Version { get; set; } = 3;
    public string SelectedPetId { get; set; } = PetPackageContract.BuiltInPetId;
    public bool Topmost { get; set; } = true;
    public double Scale { get; set; } = 0.75;
    public bool MouseFollowEnabled { get; set; } = true;
    public bool WalkingEnabled { get; set; } = true;
    public bool AutoStartEnabled { get; set; } = true;
    public bool AutoHideFullscreen { get; set; } = true;
    public bool Paused { get; set; }
    public int? LeftPx { get; set; }
    public int? TopPx { get; set; }
    public string? MonitorDeviceName { get; set; }

    public void Normalize()
    {
        Version = 3;
        SelectedPetId = string.IsNullOrWhiteSpace(SelectedPetId)
            ? PetPackageContract.BuiltInPetId
            : SelectedPetId.Trim();
        var finiteScale = double.IsFinite(Scale) ? Scale : 0.75;
        Scale = Math.Clamp(
            Math.Round(finiteScale * 20, MidpointRounding.AwayFromZero) / 20,
            0.5,
            2.0);
    }
}
