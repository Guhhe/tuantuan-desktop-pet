using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TuantuanDesktopPet.Core;

namespace TuantuanDesktopPet;

internal sealed class PetCatalog
{
    private const string SpriteResource = "TuantuanDesktopPet.Assets.spritesheet.webp";
    private const string ManifestResource = "TuantuanDesktopPet.Assets.pet.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _petsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TuantuanDesktopPet",
        "pets");

    internal PetDescriptor BuiltIn { get; }

    internal PetCatalog()
    {
        var package = ReadEmbedded();
        PetPackageContract.ValidateManifest(package.Manifest, allowBuiltInId: true);
        BuiltIn = new PetDescriptor(
            package.Manifest.Id,
            package.Manifest.DisplayName,
            package.Manifest.Description,
            true,
            null);
    }

    internal IReadOnlyList<PetDescriptor> GetPets()
    {
        Directory.CreateDirectory(_petsDirectory);
        var pets = new List<PetDescriptor> { BuiltIn };
        foreach (var directory in Directory.EnumerateDirectories(_petsDirectory))
        {
            try
            {
                var manifestPath = Path.Combine(directory, PetPackageContract.ManifestFileName);
                var spritesheetPath = Path.Combine(directory, PetPackageContract.SpritesheetFileName);
                if (!File.Exists(manifestPath) || !File.Exists(spritesheetPath))
                {
                    continue;
                }

                var manifestBytes = ReadLimitedFile(manifestPath, PetPackageContract.MaximumManifestBytes);
                var manifest = ParseManifest(manifestBytes, allowBuiltInId: false);
                if (!string.Equals(Path.GetFileName(directory), manifest.Id, StringComparison.Ordinal))
                {
                    continue;
                }

                pets.Add(new PetDescriptor(
                    manifest.Id,
                    manifest.DisplayName,
                    manifest.Description,
                    false,
                    directory));
            }
            catch
            {
                // A damaged pet is ignored here and reported if the user explicitly tries to import it again.
            }
        }

        return pets
            .OrderByDescending(pet => pet.IsBuiltIn)
            .ThenBy(pet => pet.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    internal PetDescriptor? Find(string id) =>
        GetPets().FirstOrDefault(pet =>
            string.Equals(pet.Id, id, StringComparison.OrdinalIgnoreCase));

    internal PetPackageData Load(PetDescriptor descriptor)
    {
        if (descriptor.IsBuiltIn)
        {
            return ReadEmbedded();
        }

        if (descriptor.DirectoryPath is null)
        {
            throw new InvalidDataException("外部宠物目录无效。");
        }

        var manifestBytes = ReadLimitedFile(
            Path.Combine(descriptor.DirectoryPath, PetPackageContract.ManifestFileName),
            PetPackageContract.MaximumManifestBytes);
        var spriteBytes = ReadLimitedFile(
            Path.Combine(descriptor.DirectoryPath, PetPackageContract.SpritesheetFileName),
            PetPackageContract.MaximumSpritesheetBytes);
        var manifest = ParseManifest(manifestBytes, allowBuiltInId: false);
        return new PetPackageData(manifest, manifestBytes, spriteBytes);
    }

    internal PetPackageData ReadImport(string selectedPath)
    {
        var extension = Path.GetExtension(selectedPath);
        return extension.ToLowerInvariant() switch
        {
            ".ttpet" or ".zip" => ReadArchive(selectedPath),
            ".json" when string.Equals(
                Path.GetFileName(selectedPath),
                PetPackageContract.ManifestFileName,
                StringComparison.OrdinalIgnoreCase) =>
                ReadPair(Path.GetDirectoryName(selectedPath)!, selectedPath),
            ".webp" when string.Equals(
                Path.GetFileName(selectedPath),
                PetPackageContract.SpritesheetFileName,
                StringComparison.OrdinalIgnoreCase) =>
                ReadPair(
                    Path.GetDirectoryName(selectedPath)!,
                    Path.Combine(
                        Path.GetDirectoryName(selectedPath)!,
                        PetPackageContract.ManifestFileName)),
            ".json" or ".webp" => throw new InvalidDataException(
                "文件对必须命名为 pet.json 和 spritesheet.webp。"),
            _ => throw new InvalidDataException("请选择 .ttpet、.zip、pet.json 或 spritesheet.webp。")
        };
    }

    internal PetDescriptor Install(PetPackageData package, bool replace)
    {
        PetPackageContract.ValidateManifest(package.Manifest, allowBuiltInId: false);
        Directory.CreateDirectory(_petsDirectory);

        var finalDirectory = Path.Combine(_petsDirectory, package.Manifest.Id);
        if (Directory.Exists(finalDirectory) && !replace)
        {
            throw new IOException("同 id 的宠物已经存在。");
        }

        var staging = Path.Combine(_petsDirectory, $".import-{Guid.NewGuid():N}");
        var backup = Path.Combine(_petsDirectory, $".backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        try
        {
            // The original WebP bytes are copied verbatim. No decoded or derived image is written.
            File.WriteAllBytes(
                Path.Combine(staging, PetPackageContract.SpritesheetFileName),
                package.SpritesheetBytes);
            File.WriteAllText(
                Path.Combine(staging, PetPackageContract.ManifestFileName),
                JsonSerializer.Serialize(package.Manifest, JsonOptions),
                new UTF8Encoding(false));

            if (Directory.Exists(finalDirectory))
            {
                Directory.Move(finalDirectory, backup);
            }
            Directory.Move(staging, finalDirectory);
            if (Directory.Exists(backup))
            {
                try
                {
                    Directory.Delete(backup, true);
                }
                catch
                {
                    // The new pet is installed successfully; a stale backup is harmless
                    // and can be cleaned on a future maintenance pass.
                }
            }
        }
        catch
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, true);
            }
            if (Directory.Exists(backup) && !Directory.Exists(finalDirectory))
            {
                Directory.Move(backup, finalDirectory);
            }
            throw;
        }

        return new PetDescriptor(
            package.Manifest.Id,
            package.Manifest.DisplayName,
            package.Manifest.Description,
            false,
            finalDirectory);
    }

    private static PetPackageData ReadPair(string directory, string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("所选文件旁缺少 pet.json。", manifestPath);
        }

        var manifestBytes = ReadLimitedFile(manifestPath, PetPackageContract.MaximumManifestBytes);
        var manifest = ParseManifest(manifestBytes, allowBuiltInId: false);
        var spritePath = Path.Combine(directory, PetPackageContract.SpritesheetFileName);
        if (!File.Exists(spritePath))
        {
            throw new FileNotFoundException("所选文件旁缺少 spritesheet.webp。", spritePath);
        }

        return new PetPackageData(
            manifest,
            manifestBytes,
            ReadLimitedFile(spritePath, PetPackageContract.MaximumSpritesheetBytes));
    }

    private static PetPackageData ReadArchive(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        if (archive.Entries.Count != 2)
        {
            throw new InvalidDataException("宠物包必须只包含根目录下的 pet.json 和 spritesheet.webp。");
        }

        var manifestEntry = FindRootEntry(archive, PetPackageContract.ManifestFileName);
        var spriteEntry = FindRootEntry(archive, PetPackageContract.SpritesheetFileName);
        var manifestBytes = ReadLimitedEntry(manifestEntry, PetPackageContract.MaximumManifestBytes);
        var spriteBytes = ReadLimitedEntry(spriteEntry, PetPackageContract.MaximumSpritesheetBytes);
        var manifest = ParseManifest(manifestBytes, allowBuiltInId: false);
        return new PetPackageData(manifest, manifestBytes, spriteBytes);
    }

    private static ZipArchiveEntry FindRootEntry(ZipArchive archive, string name)
    {
        var matches = archive.Entries
            .Where(entry =>
                string.Equals(entry.FullName, name, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(entry.Name))
            .ToArray();
        return matches.Length == 1
            ? matches[0]
            : throw new InvalidDataException($"宠物包缺少唯一的根目录 {name}。");
    }

    private static PetPackageData ReadEmbedded()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var manifestBytes = ReadResource(assembly, ManifestResource);
        var spriteBytes = ReadResource(assembly, SpriteResource);
        var manifest = ParseManifest(manifestBytes, allowBuiltInId: true);
        return new PetPackageData(manifest, manifestBytes, spriteBytes);
    }

    private static PetManifest ParseManifest(byte[] bytes, bool allowBuiltInId)
    {
        var manifest = JsonSerializer.Deserialize<PetManifest>(bytes, JsonOptions)
            ?? throw new InvalidDataException("pet.json 内容为空。");
        PetPackageContract.ValidateManifest(manifest, allowBuiltInId);
        return manifest;
    }

    private static byte[] ReadResource(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException($"缺少内嵌资源：{name}");
        return ReadLimitedStream(stream, PetPackageContract.MaximumSpritesheetBytes);
    }

    private static byte[] ReadLimitedFile(string path, int maximumBytes)
    {
        var info = new FileInfo(path);
        if (info.Length > maximumBytes)
        {
            throw new InvalidDataException($"文件 {info.Name} 超过允许大小。");
        }
        return File.ReadAllBytes(path);
    }

    private static byte[] ReadLimitedEntry(ZipArchiveEntry entry, int maximumBytes)
    {
        if (entry.Length > maximumBytes)
        {
            throw new InvalidDataException($"文件 {entry.Name} 超过允许大小。");
        }
        using var stream = entry.Open();
        return ReadLimitedStream(stream, maximumBytes);
    }

    private static byte[] ReadLimitedStream(Stream stream, int maximumBytes)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        if (memory.Length > maximumBytes)
        {
            throw new InvalidDataException("宠物包中的文件超过允许大小。");
        }
        return memory.ToArray();
    }
}
