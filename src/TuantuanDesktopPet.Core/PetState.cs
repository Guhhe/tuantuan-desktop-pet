namespace TuantuanDesktopPet.Core;

public enum PetState
{
    Idle,
    WalkingLeft,
    WalkingRight,
    Waving,
    Jumping,
    Reacting,
    IdleVariant,
    Dragging,
    Paused,
    HiddenForFullscreen
}

public readonly record struct SpriteFrame(int Row, int Column);

public readonly record struct PetTickResult(SpriteFrame Frame, double MoveXDips, PetState State);
