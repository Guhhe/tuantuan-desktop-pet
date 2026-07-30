namespace TuantuanDesktopPet.Core;

public static class AnimationCatalog
{
    public const int AtlasWidth = 1536;
    public const int AtlasHeight = 2288;
    public const int Columns = 8;
    public const int Rows = 11;
    public const int CellWidth = 192;
    public const int CellHeight = 208;

    public static AnimationClip Idle { get; } = new(
        "idle",
        0,
        [0, 1, 2, 3, 4, 5],
        [280, 110, 110, 140, 140, 320],
        true);

    public static AnimationClip WalkingRight { get; } = new(
        "running-right",
        1,
        Enumerable.Range(0, 8).ToArray(),
        [120, 120, 120, 120, 120, 120, 120, 220],
        true);

    public static AnimationClip WalkingLeft { get; } = new(
        "running-left",
        2,
        Enumerable.Range(0, 8).ToArray(),
        [120, 120, 120, 120, 120, 120, 120, 220],
        true);

    public static AnimationClip Waving { get; } = new(
        "waving",
        3,
        [0, 1, 2, 3],
        [140, 140, 140, 280],
        false);

    public static AnimationClip Jumping { get; } = new(
        "jumping",
        4,
        [0, 1, 2, 3, 4],
        [140, 140, 140, 140, 280],
        false);

    public static AnimationClip Sleepy { get; } = new(
        "sleepy",
        5,
        Enumerable.Range(0, 8).ToArray(),
        [160, 160, 180, 180, 220, 180, 160, 320],
        false);

    public static AnimationClip PawPlay { get; } = new(
        "paw-play",
        6,
        [0, 1, 2, 3, 4, 5],
        [150, 150, 150, 150, 150, 280],
        false);

    public static AnimationClip LookingAround { get; } = new(
        "looking-around",
        7,
        [0, 1, 2, 3, 4, 5],
        [170, 170, 170, 170, 170, 300],
        false);

    public static AnimationClip Curious { get; } = new(
        "curious",
        8,
        [0, 1, 2, 3, 4, 5],
        [160, 160, 160, 160, 160, 300],
        false);

    public static IReadOnlyList<AnimationClip> IdleVariants { get; } =
        [PawPlay, LookingAround, Curious, Sleepy, Waving];

    public static IReadOnlyList<AnimationClip> DesktopClips { get; } =
        [
            Idle,
            WalkingRight,
            WalkingLeft,
            Waving,
            Jumping,
            Sleepy,
            PawPlay,
            LookingAround,
            Curious
        ];

    public static void Validate()
    {
        foreach (var clip in DesktopClips)
        {
            clip.Validate();
        }
    }
}
