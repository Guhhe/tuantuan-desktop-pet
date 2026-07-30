using TuantuanDesktopPet.Core;

namespace TuantuanDesktopPet.Core.Tests;

public sealed class PetPackageContractTests
{
    [Fact]
    public void ValidExternalManifestIsAccepted()
    {
        var manifest = CreateValidManifest();

        PetPackageContract.ValidateManifest(manifest);

        Assert.Equal("sample-pet", manifest.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../pet")]
    [InlineData("UPPER")]
    [InlineData("-leading")]
    [InlineData("white space")]
    public void UnsafeIdsAreRejected(string id)
    {
        var manifest = CreateValidManifest();
        manifest.Id = id;

        Assert.Throws<InvalidDataException>(() => PetPackageContract.ValidateManifest(manifest));
    }

    [Fact]
    public void BuiltInIdIsReservedForEmbeddedPet()
    {
        var manifest = CreateValidManifest();
        manifest.Id = PetPackageContract.BuiltInPetId;

        Assert.Throws<InvalidDataException>(() => PetPackageContract.ValidateManifest(manifest));
        PetPackageContract.ValidateManifest(manifest, allowBuiltInId: true);
    }

    [Theory]
    [InlineData(1, "spritesheet.webp")]
    [InlineData(2, "../spritesheet.webp")]
    [InlineData(3, "other.webp")]
    public void IncorrectVersionOrPathIsRejected(int version, string path)
    {
        var manifest = CreateValidManifest();
        manifest.SpriteVersionNumber = version;
        manifest.SpritesheetPath = path;

        Assert.Throws<InvalidDataException>(() => PetPackageContract.ValidateManifest(manifest));
    }

    [Fact]
    public void StandardRowsMatchCompleteHatchPetV2Contract()
    {
        Assert.Equal([6, 8, 8, 4, 5, 8, 6, 6, 6, 8, 8], PetPackageContract.UsedColumnsByRow);
    }

    private static PetManifest CreateValidManifest() => new()
    {
        Id = "sample-pet",
        DisplayName = "示例宠物",
        Description = "测试",
        SpriteVersionNumber = 2,
        SpritesheetPath = "spritesheet.webp"
    };
}
