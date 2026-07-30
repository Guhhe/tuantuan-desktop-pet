namespace TuantuanDesktopPet.Core;

public readonly record struct LookFrame(int DirectionIndex, double Degrees, int Row, int Column);

public static class DirectionMapper
{
    public const int DirectionCount = 16;
    public const double StepDegrees = 22.5;

    public static LookFrame? Map(double deltaX, double deltaY, double deadzone)
    {
        if (Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY)) <= deadzone)
        {
            return null;
        }

        // Screen Y grows downward. atan2(dx, -dy) makes 0 degrees point upward
        // and increases clockwise, matching the v2 pet atlas contract.
        var degrees = Math.Atan2(deltaX, -deltaY) * (180.0 / Math.PI);
        if (degrees < 0)
        {
            degrees += 360.0;
        }

        var index = (int)Math.Round(degrees / StepDegrees, MidpointRounding.AwayFromZero) % DirectionCount;
        return new LookFrame(
            index,
            index * StepDegrees,
            9 + (index / 8),
            index % 8);
    }
}
