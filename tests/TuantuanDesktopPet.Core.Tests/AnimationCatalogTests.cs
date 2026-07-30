using TuantuanDesktopPet.Core;

namespace TuantuanDesktopPet.Core.Tests;

public sealed class AnimationCatalogTests
{
    [Fact]
    public void ContractDimensionsMatchV2Atlas()
    {
        Assert.Equal(1536, AnimationCatalog.AtlasWidth);
        Assert.Equal(2288, AnimationCatalog.AtlasHeight);
        Assert.Equal(8, AnimationCatalog.Columns);
        Assert.Equal(11, AnimationCatalog.Rows);
        Assert.Equal(192, AnimationCatalog.CellWidth);
        Assert.Equal(208, AnimationCatalog.CellHeight);
    }

    [Fact]
    public void DesktopClipsUseEveryExistingAnimationRow()
    {
        AnimationCatalog.Validate();
        Assert.Equal(
            [0, 1, 2, 3, 4, 5, 6, 7, 8],
            AnimationCatalog.DesktopClips.Select(clip => clip.Row).ToArray());
        Assert.Equal([6, 7, 8, 5, 3], AnimationCatalog.IdleVariants.Select(clip => clip.Row).ToArray());
    }

    [Fact]
    public void DurationsMatchCodexPetContract()
    {
        Assert.Equal([0, 1, 2, 3, 4, 5], AnimationCatalog.Idle.Columns);
        Assert.Equal(Enumerable.Range(0, 8), AnimationCatalog.WalkingRight.Columns);
        Assert.Equal(Enumerable.Range(0, 8), AnimationCatalog.WalkingLeft.Columns);
        Assert.Equal([0, 1, 2, 3], AnimationCatalog.Waving.Columns);
        Assert.Equal([0, 1, 2, 3, 4], AnimationCatalog.Jumping.Columns);
        Assert.Equal(Enumerable.Range(0, 8), AnimationCatalog.Sleepy.Columns);
        Assert.Equal([0, 1, 2, 3, 4, 5], AnimationCatalog.PawPlay.Columns);
        Assert.Equal([0, 1, 2, 3, 4, 5], AnimationCatalog.LookingAround.Columns);
        Assert.Equal([0, 1, 2, 3, 4, 5], AnimationCatalog.Curious.Columns);
        Assert.Equal([280, 110, 110, 140, 140, 320], AnimationCatalog.Idle.DurationsMs);
        Assert.Equal([120, 120, 120, 120, 120, 120, 120, 220], AnimationCatalog.WalkingRight.DurationsMs);
        Assert.Equal([120, 120, 120, 120, 120, 120, 120, 220], AnimationCatalog.WalkingLeft.DurationsMs);
        Assert.Equal([140, 140, 140, 280], AnimationCatalog.Waving.DurationsMs);
        Assert.Equal([140, 140, 140, 140, 280], AnimationCatalog.Jumping.DurationsMs);
    }
}
