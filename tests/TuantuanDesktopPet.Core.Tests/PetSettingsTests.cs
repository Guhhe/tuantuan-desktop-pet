using TuantuanDesktopPet.Core;

namespace TuantuanDesktopPet.Core.Tests;

public sealed class PetSettingsTests
{
    [Theory]
    [InlineData(0.2, 0.5)]
    [InlineData(0.73, 0.75)]
    [InlineData(1.18, 1.2)]
    [InlineData(2.4, 2.0)]
    public void NormalizeClampsAndSnapsScaleToFivePercentSteps(double input, double expected)
    {
        var settings = new PetSettings { Scale = input };

        settings.Normalize();

        Assert.Equal(expected, settings.Scale);
    }

    [Fact]
    public void NormalizeMigratesOldSettingsAndRestoresMissingPet()
    {
        var settings = new PetSettings { Version = 1, SelectedPetId = " " };

        settings.Normalize();

        Assert.Equal(3, settings.Version);
        Assert.Equal(PetPackageContract.BuiltInPetId, settings.SelectedPetId);
    }

    [Fact]
    public void DefaultsMatchFirstRunExperience()
    {
        var settings = new PetSettings();

        Assert.Equal(0.75, settings.Scale);
        Assert.True(settings.MouseFollowEnabled);
        Assert.True(settings.WalkingEnabled);
        Assert.Equal(PetPackageContract.BuiltInPetId, settings.SelectedPetId);
    }
}
