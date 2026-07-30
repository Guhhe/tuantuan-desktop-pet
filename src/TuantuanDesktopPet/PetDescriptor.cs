using TuantuanDesktopPet.Core;

namespace TuantuanDesktopPet;

internal sealed record PetDescriptor(
    string Id,
    string DisplayName,
    string Description,
    bool IsBuiltIn,
    string? DirectoryPath);

internal sealed record PetPackageData(
    PetManifest Manifest,
    byte[] ManifestBytes,
    byte[] SpritesheetBytes);
