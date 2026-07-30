namespace TuantuanDesktopPet.Core;

public interface IRandomSource
{
    int Next(int minInclusive, int maxExclusive);
}

public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random = Random.Shared;

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
