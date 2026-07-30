namespace TuantuanDesktopPet.Core;

public sealed class PetController
{
    public const double WalkingSpeedDipsPerSecond = 55.0;

    private readonly IRandomSource _random;
    private AnimationClip _clip = AnimationCatalog.Idle;
    private int _frameIndex;
    private double _frameElapsedMs;
    private double _idleElapsedMs;
    private double _nextDecisionMs;
    private double _walkRemainingDips;
    private int _dragDirection;
    private int _idleVariantIndex;

    public PetController(IRandomSource? random = null)
    {
        AnimationCatalog.Validate();
        _random = random ?? new SystemRandomSource();
        State = PetState.Idle;
        ScheduleNextDecision();
    }

    public PetState State { get; private set; }

    public bool WalkingEnabled { get; private set; } = true;

    public SpriteFrame CurrentFrame => new(_clip.Row, _clip.Columns[_frameIndex]);

    public void StartStartupGreeting() => StartOneShot(PetState.Waving, AnimationCatalog.Waving);

    public void TriggerWave()
    {
        if (CanAcceptInteraction())
        {
            StartOneShot(PetState.Waving, AnimationCatalog.Waving);
        }
    }

    public void TriggerJump()
    {
        if (CanAcceptInteraction())
        {
            StartOneShot(PetState.Jumping, AnimationCatalog.Jumping);
        }
    }

    public void TriggerClickReaction()
    {
        if (!CanAcceptInteraction())
        {
            return;
        }

        var clip = _random.Next(0, 4) switch
        {
            0 => AnimationCatalog.Waving,
            1 => AnimationCatalog.PawPlay,
            2 => AnimationCatalog.LookingAround,
            _ => AnimationCatalog.Curious
        };
        StartOneShot(PetState.Reacting, clip);
    }

    public void SetWalkingEnabled(bool enabled)
    {
        WalkingEnabled = enabled;
        if (!enabled && State is PetState.WalkingLeft or PetState.WalkingRight)
        {
            EnterIdle();
        }
    }

    public void BeginDrag()
    {
        if (State is PetState.Paused or PetState.HiddenForFullscreen)
        {
            return;
        }

        State = PetState.Dragging;
        _dragDirection = 0;
        SetClip(AnimationCatalog.Idle);
    }

    public void UpdateDragDirection(double deltaXDips)
    {
        if (State != PetState.Dragging || Math.Abs(deltaXDips) < 0.25)
        {
            return;
        }

        var direction = deltaXDips < 0 ? -1 : 1;
        if (_dragDirection == direction)
        {
            return;
        }

        _dragDirection = direction;
        SetClip(direction < 0 ? AnimationCatalog.WalkingLeft : AnimationCatalog.WalkingRight);
    }

    public void EndDrag()
    {
        if (State == PetState.Dragging)
        {
            _dragDirection = 0;
            EnterIdle();
        }
    }

    public void SetPaused(bool paused)
    {
        if (paused)
        {
            State = PetState.Paused;
            SetClip(AnimationCatalog.Idle);
        }
        else if (State == PetState.Paused)
        {
            EnterIdle();
        }
    }

    public void SetFullscreenHidden(bool hidden)
    {
        if (hidden)
        {
            State = PetState.HiddenForFullscreen;
            SetClip(AnimationCatalog.Idle);
        }
        else if (State == PetState.HiddenForFullscreen)
        {
            EnterIdle();
        }
    }

    public PetTickResult Tick(double elapsedMs, double availableLeftDips, double availableRightDips)
    {
        if (elapsedMs <= 0)
        {
            return new PetTickResult(CurrentFrame, 0, State);
        }

        if (State is PetState.Paused or PetState.HiddenForFullscreen)
        {
            return new PetTickResult(CurrentFrame, 0, State);
        }

        if (State == PetState.Dragging)
        {
            AdvanceAnimation(elapsedMs);
            return new PetTickResult(CurrentFrame, 0, State);
        }

        var moveX = 0.0;
        if (State is PetState.WalkingLeft or PetState.WalkingRight)
        {
            var direction = State == PetState.WalkingLeft ? -1.0 : 1.0;
            var available = direction < 0 ? Math.Max(0, availableLeftDips) : Math.Max(0, availableRightDips);
            var requested = Math.Min(
                WalkingSpeedDipsPerSecond * (elapsedMs / 1000.0),
                _walkRemainingDips);
            var actual = Math.Min(requested, available);
            moveX = direction * actual;
            _walkRemainingDips -= actual;

            AdvanceAnimation(elapsedMs);
            if (_walkRemainingDips <= 0.001 || available <= requested + 0.001)
            {
                EnterIdle();
            }

            return new PetTickResult(CurrentFrame, moveX, State);
        }

        var completed = AdvanceAnimation(elapsedMs);
        if (completed &&
            State is (PetState.Waving or PetState.Jumping or PetState.Reacting or PetState.IdleVariant))
        {
            EnterIdle();
        }

        if (State == PetState.Idle)
        {
            _idleElapsedMs += elapsedMs;
            if (_idleElapsedMs >= _nextDecisionMs)
            {
                ChooseAutonomousAction(availableLeftDips, availableRightDips);
            }
        }

        return new PetTickResult(CurrentFrame, moveX, State);
    }

    private bool CanAcceptInteraction() =>
        State is not (PetState.Paused or PetState.HiddenForFullscreen or PetState.Dragging);

    private void ChooseAutonomousAction(double availableLeftDips, double availableRightDips)
    {
        var roll = _random.Next(0, 100);
        if (WalkingEnabled && roll < 25)
        {
            StartWalk(availableLeftDips, availableRightDips);
        }
        else if (roll < 92)
        {
            StartNextIdleVariant();
        }
        else
        {
            EnterIdle();
        }
    }

    private void StartNextIdleVariant()
    {
        var clip = AnimationCatalog.IdleVariants[
            _idleVariantIndex % AnimationCatalog.IdleVariants.Count];
        _idleVariantIndex++;
        StartOneShot(PetState.IdleVariant, clip);
    }

    private void StartWalk(double availableLeftDips, double availableRightDips)
    {
        if (!WalkingEnabled)
        {
            EnterIdle();
            return;
        }

        var preferLeft = _random.Next(0, 2) == 0;
        var leftPossible = availableLeftDips >= 16;
        var rightPossible = availableRightDips >= 16;

        if (!leftPossible && !rightPossible)
        {
            EnterIdle();
            return;
        }

        var walkLeft = preferLeft ? leftPossible : !rightPossible;
        if (!preferLeft && rightPossible)
        {
            walkLeft = false;
        }

        State = walkLeft ? PetState.WalkingLeft : PetState.WalkingRight;
        SetClip(walkLeft ? AnimationCatalog.WalkingLeft : AnimationCatalog.WalkingRight);
        _walkRemainingDips = _random.Next(160, 421);
        _idleElapsedMs = 0;
    }

    private void StartOneShot(PetState state, AnimationClip clip)
    {
        State = state;
        SetClip(clip);
        _idleElapsedMs = 0;
    }

    private void EnterIdle()
    {
        State = PetState.Idle;
        SetClip(AnimationCatalog.Idle);
        _idleElapsedMs = 0;
        _walkRemainingDips = 0;
        ScheduleNextDecision();
    }

    private void ScheduleNextDecision() => _nextDecisionMs = _random.Next(3_000, 6_001);

    private void SetClip(AnimationClip clip)
    {
        _clip = clip;
        _frameIndex = 0;
        _frameElapsedMs = 0;
    }

    private bool AdvanceAnimation(double elapsedMs)
    {
        _frameElapsedMs += elapsedMs;
        var completed = false;

        while (_frameElapsedMs >= _clip.DurationsMs[_frameIndex])
        {
            _frameElapsedMs -= _clip.DurationsMs[_frameIndex];
            if (_frameIndex + 1 < _clip.FrameCount)
            {
                _frameIndex++;
                continue;
            }

            if (_clip.Loop)
            {
                _frameIndex = 0;
                continue;
            }

            _frameElapsedMs = 0;
            completed = true;
            break;
        }

        return completed;
    }
}
