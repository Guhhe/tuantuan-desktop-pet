using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using TuantuanDesktopPet.Core;

namespace TuantuanDesktopPet;

internal sealed class SpriteAtlas : IDisposable
{
    internal const string ExpectedSha256 =
        "c0767ed4b89b19b8b256ffe0fd6b6463e2fe5c3b1c08a40240d96a1e12ee953c";

    private readonly SKBitmap _bitmap;
    private readonly Dictionary<SpriteFrame, FrameData> _cache = [];

    private SpriteAtlas(PetPackageData package, bool isBuiltIn)
    {
        PetPackageContract.ValidateManifest(package.Manifest, allowBuiltInId: isBuiltIn);
        if (isBuiltIn)
        {
            var actualHash = Convert.ToHexStringLower(SHA256.HashData(package.SpritesheetBytes));
            if (!string.Equals(actualHash, ExpectedSha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException("内嵌的团团素材校验失败，程序文件可能已损坏。");
            }
        }

        using var decoded = SKBitmap.Decode(package.SpritesheetBytes);
        if (decoded is null ||
            decoded.Width != AnimationCatalog.AtlasWidth ||
            decoded.Height != AnimationCatalog.AtlasHeight)
        {
            throw new InvalidDataException(
                $"宠物图集尺寸无效，应为 {AnimationCatalog.AtlasWidth}×{AnimationCatalog.AtlasHeight}。");
        }

        _bitmap = decoded.Copy(SKColorType.Bgra8888)
            ?? throw new InvalidDataException("无法转换宠物图集的像素格式。");

        if (_bitmap.AlphaType == SKAlphaType.Opaque)
        {
            _bitmap.Dispose();
            throw new InvalidDataException("宠物图集不包含透明通道。");
        }

        // The embedded Tuantuan atlas predates the current strict rule for transparent
        // reserved cells. Preserve backward compatibility without changing its bytes;
        // every newly imported pet must pass the complete current v2 cell contract.
        if (!isBuiltIn)
        {
            ValidateCells();
        }
    }

    internal static SpriteAtlas Load(PetPackageData package, bool isBuiltIn) =>
        new(package, isBuiltIn);

    internal FrameData GetFrame(SpriteFrame frame)
    {
        if (frame.Row is < 0 or >= AnimationCatalog.Rows ||
            frame.Column is < 0 or >= AnimationCatalog.Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(frame));
        }

        if (_cache.TryGetValue(frame, out var cached))
        {
            return cached;
        }

        var stride = AnimationCatalog.CellWidth * 4;
        var pixels = new byte[stride * AnimationCatalog.CellHeight];
        var alpha = new byte[AnimationCatalog.CellWidth * AnimationCatalog.CellHeight];
        var atlasPixels = _bitmap.GetPixels();
        var sourceXBytes = frame.Column * AnimationCatalog.CellWidth * 4;
        var sourceY = frame.Row * AnimationCatalog.CellHeight;

        for (var y = 0; y < AnimationCatalog.CellHeight; y++)
        {
            var source = nint.Add(
                atlasPixels,
                ((sourceY + y) * _bitmap.RowBytes) + sourceXBytes);
            var destinationOffset = y * stride;
            Marshal.Copy(source, pixels, destinationOffset, stride);

            for (var x = 0; x < AnimationCatalog.CellWidth; x++)
            {
                alpha[(y * AnimationCatalog.CellWidth) + x] = pixels[destinationOffset + (x * 4) + 3];
            }
        }

        var image = BitmapSource.Create(
            AnimationCatalog.CellWidth,
            AnimationCatalog.CellHeight,
            96,
            96,
            PixelFormats.Pbgra32,
            null,
            pixels,
            stride);
        image.Freeze();

        cached = new FrameData(image, alpha);
        _cache.Add(frame, cached);
        return cached;
    }

    public void Dispose()
    {
        _cache.Clear();
        _bitmap.Dispose();
    }

    private void ValidateCells()
    {
        for (var row = 0; row < AnimationCatalog.Rows; row++)
        {
            for (var column = 0; column < AnimationCatalog.Columns; column++)
            {
                var shouldContainPixels = column < PetPackageContract.UsedColumnsByRow[row];
                var containsPixels = CellContainsVisiblePixel(row, column);
                if (shouldContainPixels && !containsPixels)
                {
                    _bitmap.Dispose();
                    throw new InvalidDataException($"宠物图集第 {row + 1} 行第 {column + 1} 格为空。");
                }
                if (!shouldContainPixels && containsPixels)
                {
                    _bitmap.Dispose();
                    throw new InvalidDataException(
                        $"宠物图集第 {row + 1} 行第 {column + 1} 格应保持完全透明。");
                }
            }
        }
    }

    private bool CellContainsVisiblePixel(int row, int column)
    {
        var startX = column * AnimationCatalog.CellWidth;
        var startY = row * AnimationCatalog.CellHeight;
        for (var y = startY; y < startY + AnimationCatalog.CellHeight; y++)
        {
            for (var x = startX; x < startX + AnimationCatalog.CellWidth; x++)
            {
                if (_bitmap.GetPixel(x, y).Alpha != 0)
                {
                    return true;
                }
            }
        }
        return false;
    }
}

internal sealed record FrameData(BitmapSource Image, byte[] AlphaMask);
