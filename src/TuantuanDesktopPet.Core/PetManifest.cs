using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TuantuanDesktopPet.Core;

public sealed class PetManifest
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SpriteVersionNumber { get; set; }
    public string SpritesheetPath { get; set; } = string.Empty;
}

public static partial class PetPackageContract
{
    public const string BuiltInPetId = "jindou";
    public const string ManifestFileName = "pet.json";
    public const string SpritesheetFileName = "spritesheet.webp";
    public const int MaximumManifestBytes = 64 * 1024;
    public const int MaximumSpritesheetBytes = 128 * 1024 * 1024;

    // The number of populated cells in each row of a complete Hatch Pet v2 atlas.
    public static IReadOnlyList<int> UsedColumnsByRow { get; } =
        [6, 8, 8, 4, 5, 8, 6, 6, 6, 8, 8];

    public static void ValidateManifest(PetManifest manifest, bool allowBuiltInId = false)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        manifest.Id = manifest.Id.Trim();
        manifest.DisplayName = manifest.DisplayName.Trim();
        manifest.Description = manifest.Description.Trim();
        manifest.SpritesheetPath = manifest.SpritesheetPath.Trim();

        if (!PetIdPattern().IsMatch(manifest.Id))
        {
            throw new InvalidDataException(
                "宠物 id 必须为 1–64 位小写字母、数字、点、下划线或连字符，并以字母或数字开头。");
        }
        if (!allowBuiltInId &&
            string.Equals(manifest.Id, BuiltInPetId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("宠物 id“jindou”由内置团团保留，请换一个 id。");
        }
        if (manifest.DisplayName.Length is < 1 or > 64)
        {
            throw new InvalidDataException("宠物 displayName 必须为 1–64 个字符。");
        }
        if (manifest.Description.Length > 500)
        {
            throw new InvalidDataException("宠物 description 不能超过 500 个字符。");
        }
        if (manifest.SpriteVersionNumber != 2)
        {
            throw new InvalidDataException("仅支持 spriteVersionNumber 为 2 的 Hatch Pet 图集。");
        }
        if (!string.Equals(
                manifest.SpritesheetPath,
                SpritesheetFileName,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("spritesheetPath 必须为根目录下的 spritesheet.webp。");
        }
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex PetIdPattern();
}
