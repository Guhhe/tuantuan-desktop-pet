using TuantuanDesktopPet.Core;

namespace TuantuanDesktopPet.Core.Tests;

public sealed class PetControllerTests
{
    [Fact]
    public void StartupGreetingReturnsToIdle()
    {
        var controller = new PetController(new SequenceRandomSource(6_000));

        controller.StartStartupGreeting();
        Assert.Equal(PetState.Waving, controller.State);

        controller.Tick(701, 500, 500);
        Assert.Equal(PetState.Idle, controller.State);
        Assert.Equal(new SpriteFrame(0, 0), controller.CurrentFrame);
    }

    [Fact]
    public void DoubleClickJumpOverridesWave()
    {
        var controller = new PetController(new SequenceRandomSource(6_000));

        controller.TriggerWave();
        controller.TriggerJump();

        Assert.Equal(PetState.Jumping, controller.State);
        Assert.Equal(4, controller.CurrentFrame.Row);
    }

    [Fact]
    public void DragHasPriorityUntilReleased()
    {
        var controller = new PetController(new SequenceRandomSource(6_000));

        controller.TriggerWave();
        controller.BeginDrag();
        controller.TriggerJump();
        var duringDrag = controller.Tick(2_000, 500, 500);

        Assert.Equal(PetState.Dragging, duringDrag.State);
        Assert.Equal(0, duringDrag.MoveXDips);

        controller.EndDrag();
        Assert.Equal(PetState.Idle, controller.State);
    }

    [Fact]
    public void DragUsesCurrentHorizontalDirectionAndCanReverse()
    {
        var controller = new PetController(new SequenceRandomSource(6_000));

        controller.BeginDrag();
        controller.UpdateDragDirection(8);
        Assert.Equal(new SpriteFrame(1, 0), controller.CurrentFrame);

        controller.Tick(121, 500, 500);
        Assert.Equal(new SpriteFrame(1, 1), controller.CurrentFrame);

        controller.UpdateDragDirection(-4);
        Assert.Equal(new SpriteFrame(2, 0), controller.CurrentFrame);
        Assert.Equal(PetState.Dragging, controller.State);
    }

    [Fact]
    public void WalkingCanBeDisabledWithoutDisablingIdleActions()
    {
        var controller = new PetController(new SequenceRandomSource(5_000, 0, 6_000));
        controller.SetWalkingEnabled(false);

        var tick = controller.Tick(5_001, 500, 500);

        Assert.Equal(PetState.IdleVariant, tick.State);
        Assert.Equal(6, tick.Frame.Row);
        Assert.Equal(0, tick.MoveXDips);
        Assert.False(controller.WalkingEnabled);
    }

    [Fact]
    public void StationaryIdleVariantsRotateThroughUnusedRows()
    {
        var controller = new PetController(
            new SequenceRandomSource(3_000, 50, 3_000, 50));
        controller.SetWalkingEnabled(false);

        controller.Tick(3_001, 500, 500);
        Assert.Equal(PetState.IdleVariant, controller.State);
        Assert.Equal(6, controller.CurrentFrame.Row);

        controller.Tick(2_000, 500, 500);
        Assert.Equal(PetState.Idle, controller.State);

        controller.Tick(3_001, 500, 500);
        Assert.Equal(PetState.IdleVariant, controller.State);
        Assert.Equal(7, controller.CurrentFrame.Row);
    }

    [Fact]
    public void ClickReactionUsesAdditionalExistingAnimationRows()
    {
        var controller = new PetController(new SequenceRandomSource(6_000, 1));

        controller.TriggerClickReaction();

        Assert.Equal(PetState.Reacting, controller.State);
        Assert.Equal(6, controller.CurrentFrame.Row);
    }

    [Fact]
    public void FullscreenHiddenBlocksInteractions()
    {
        var controller = new PetController(new SequenceRandomSource(6_000));

        controller.SetFullscreenHidden(true);
        controller.TriggerWave();

        Assert.Equal(PetState.HiddenForFullscreen, controller.State);
        controller.SetFullscreenHidden(false);
        Assert.Equal(PetState.Idle, controller.State);
    }

    [Fact]
    public void PausedStateBlocksAnimationAndInteractions()
    {
        var controller = new PetController(new SequenceRandomSource(6_000));

        controller.SetPaused(true);
        controller.TriggerWave();
        controller.TriggerJump();
        var tick = controller.Tick(20_000, 500, 500);

        Assert.Equal(PetState.Paused, tick.State);
        Assert.Equal(new SpriteFrame(0, 0), tick.Frame);
        Assert.Equal(0, tick.MoveXDips);
    }

    [Fact]
    public void AutonomousWalkNeverMovesPastAvailableSpace()
    {
        // Initial next-decision, behavior roll, preferred direction, walk distance.
        var controller = new PetController(new SequenceRandomSource(5_000, 0, 1, 420, 6_000));

        controller.Tick(5_001, 40, 5);
        var tick = controller.Tick(1_000, 40, 5);

        Assert.InRange(Math.Abs(tick.MoveXDips), 0, 40);
    }

    private sealed class SequenceRandomSource(params int[] values) : IRandomSource
    {
        private readonly Queue<int> _values = new(values);

        public int Next(int minInclusive, int maxExclusive)
        {
            if (_values.Count == 0)
            {
                return minInclusive;
            }

            return Math.Clamp(_values.Dequeue(), minInclusive, maxExclusive - 1);
        }
    }
}
