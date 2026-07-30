namespace TuantuanDesktopPet.Core;

public sealed record AnimationClip(
    string Name,
    int Row,
    IReadOnlyList<int> Columns,
    IReadOnlyList<int> DurationsMs,
    bool Loop)
{
    public int FrameCount => Columns.Count;

    public void Validate()
    {
        if (Row < 0)
        {
            throw new InvalidOperationException($"{Name}: row must be non-negative.");
        }

        if (Columns.Count == 0 || Columns.Count != DurationsMs.Count)
        {
            throw new InvalidOperationException($"{Name}: columns and durations must be non-empty and equal in length.");
        }

        if (Columns.Any(column => column is < 0 or > 7))
        {
            throw new InvalidOperationException($"{Name}: columns must be in the 8-column atlas.");
        }

        if (DurationsMs.Any(duration => duration <= 0))
        {
            throw new InvalidOperationException($"{Name}: frame durations must be positive.");
        }
    }
}
