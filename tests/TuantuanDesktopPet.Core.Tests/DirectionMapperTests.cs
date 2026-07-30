using TuantuanDesktopPet.Core;

namespace TuantuanDesktopPet.Core.Tests;

public sealed class DirectionMapperTests
{
    [Theory]
    [InlineData(0, -100, 0, 9, 0)]
    [InlineData(100, 0, 4, 9, 4)]
    [InlineData(0, 100, 8, 10, 0)]
    [InlineData(-100, 0, 12, 10, 4)]
    [InlineData(100, -100, 2, 9, 2)]
    [InlineData(100, 100, 6, 9, 6)]
    [InlineData(-100, 100, 10, 10, 2)]
    [InlineData(-100, -100, 14, 10, 6)]
    public void MapsClockwiseScreenDirections(
        double deltaX,
        double deltaY,
        int expectedIndex,
        int expectedRow,
        int expectedColumn)
    {
        var result = DirectionMapper.Map(deltaX, deltaY, 1);

        Assert.NotNull(result);
        Assert.Equal(expectedIndex, result.Value.DirectionIndex);
        Assert.Equal(expectedRow, result.Value.Row);
        Assert.Equal(expectedColumn, result.Value.Column);
    }

    [Fact]
    public void ReturnsNullInsideDeadzone()
    {
        Assert.Null(DirectionMapper.Map(12, 12, 24));
    }

    [Fact]
    public void EveryDirectionUsesRowsNineAndTenInClockwiseOrder()
    {
        for (var index = 0; index < 16; index++)
        {
            var radians = index * DirectionMapper.StepDegrees * Math.PI / 180.0;
            var deltaX = Math.Sin(radians) * 100;
            var deltaY = -Math.Cos(radians) * 100;

            var result = DirectionMapper.Map(deltaX, deltaY, 1);

            Assert.NotNull(result);
            Assert.Equal(index, result.Value.DirectionIndex);
            Assert.Equal(9 + (index / 8), result.Value.Row);
            Assert.Equal(index % 8, result.Value.Column);
        }
    }
}
