using System.Security.Cryptography;

namespace TuantuanDesktopPet.Core.Tests;

public sealed class AssetIntegrityTests
{
    private const string ExpectedHash = "c0767ed4b89b19b8b256ffe0fd6b6463e2fe5c3b1c08a40240d96a1e12ee953c";

    [Fact]
    public void SourceWebpIsByteForByteApprovedAsset()
    {
        var workspace = FindWorkspaceRoot();
        var asset = Path.Combine(workspace, "assets", "tuantuan", "spritesheet.webp");

        using var stream = File.OpenRead(asset);
        var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();

        Assert.Equal(ExpectedHash, actual);
    }

    [Fact]
    public void RepositoryContainsOnlyApprovedRuntimeImageAssets()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = workspace;
        var forbiddenExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".ico", ".webp"
        };

        var generatedImages = Directory
            .EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => forbiddenExtensions.Contains(Path.GetExtension(path)))
            .Where(path => !ApprovedImages(workspace).Contains(path))
            .ToArray();

        Assert.Empty(generatedImages);
        Assert.True(File.Exists(Path.Combine(
            projectRoot,
            "src",
            "TuantuanDesktopPet",
            "Assets",
            "TuantuanDesktopPet.ico")));
    }

    [Fact]
    public void LayeredWindowNeverUsesWholeWindowTransparentStyle()
    {
        var workspace = FindWorkspaceRoot();
        var appSource = Path.Combine(workspace, "src", "TuantuanDesktopPet");
        var sourceText = string.Join(
            "\n",
            Directory.EnumerateFiles(appSource, "*.cs").Select(File.ReadAllText));

        Assert.DoesNotContain("WsExTransparent", sourceText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WS_EX_TRANSPARENT", sourceText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EveryMoveOnlySetWindowPosCallPreservesWindowSize()
    {
        var workspace = FindWorkspaceRoot();
        var appSource = Path.Combine(workspace, "src", "TuantuanDesktopPet");
        var mainWindow = File.ReadAllText(Path.Combine(appSource, "MainWindow.xaml.cs"));
        var nativeMethods = File.ReadAllText(Path.Combine(appSource, "NativeMethods.cs"));

        Assert.Contains(
            "SwpMoveOnly = SwpNoSize | SwpNoActivate | SwpNoZOrder",
            nativeMethods,
            StringComparison.Ordinal);
        Assert.Equal(5, CountOccurrences(mainWindow, "NativeMethods.SwpMoveOnly"));
        Assert.DoesNotContain(
            "NativeMethods.SwpNoActivate | NativeMethods.SwpNoZOrder",
            mainWindow,
            StringComparison.Ordinal);
    }

    private static string FindWorkspaceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var asset = Path.Combine(directory.FullName, "assets", "tuantuan", "spritesheet.webp");
            if (File.Exists(asset))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the workspace root containing the approved Tuantuan asset.");
    }

    private static HashSet<string> ApprovedImages(string workspace) =>
        new(
            [
                Path.Combine(workspace, "assets", "tuantuan", "spritesheet.webp"),
                Path.Combine(
                    workspace,
                    "src",
                    "TuantuanDesktopPet",
                    "Assets",
                    "TuantuanDesktopPet.ico")
            ],
            StringComparer.OrdinalIgnoreCase);

    private static int CountOccurrences(string value, string pattern)
    {
        var count = 0;
        var start = 0;
        while ((start = value.IndexOf(pattern, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += pattern.Length;
        }

        return count;
    }
}
