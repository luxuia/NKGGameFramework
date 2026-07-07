using NKGGameFramework.Core;

namespace NKGGameFramework.Core.Tests;

public sealed class StackFsmTests
{
    [Fact]
    public void PushHigherPriorityStateTemporarilyOverridesLowerState()
    {
        var calls = new List<string>();
        var fsm = new StackFsm<TestOwner>("test", new TestOwner(calls));

        Assert.True(fsm.Push(new BaseState()));
        Assert.True(fsm.Push(new OverlayState()));
        Assert.IsType<OverlayState>(fsm.CurrentState);

        fsm.Update(0.1d, 0.1d);
        Assert.True(fsm.PopCurrent());

        Assert.IsType<BaseState>(fsm.CurrentState);
        Assert.Equal(
            [
                "enter:base",
                "leave:base",
                "enter:overlay",
                "update:overlay",
                "leave:overlay",
                "removed:overlay",
                "enter:base"
            ],
            calls);
    }

    [Fact]
    public void ConflictingLowerPriorityStateCannotInterruptHigherState()
    {
        var calls = new List<string>();
        var fsm = new StackFsm<TestOwner>("test", new TestOwner(calls));

        Assert.True(fsm.Push(new OverlayState()));
        Assert.False(fsm.Push(new BlockedBaseState()));

        Assert.IsType<OverlayState>(fsm.CurrentState);
        Assert.Equal(["enter:overlay"], calls);
    }

    [Fact]
    public void ConflictingHigherPriorityStateRemovesLowerState()
    {
        var calls = new List<string>();
        var fsm = new StackFsm<TestOwner>("test", new TestOwner(calls));

        Assert.True(fsm.Push(new BlockedBaseState()));
        Assert.True(fsm.Push(new OverlayState()));

        Assert.IsType<OverlayState>(fsm.CurrentState);
        Assert.Equal(
            [
                "enter:blocked",
                "leave:blocked",
                "removed:blocked",
                "enter:overlay"
            ],
            calls);
    }

    private sealed record TestOwner(List<string> Calls);

    private sealed class BaseState : StackFsmState<TestOwner>
    {
        public override int Priority => 0;

        protected override void OnEnter(StackFsm<TestOwner> fsm) => fsm.Owner.Calls.Add("enter:base");

        protected override void OnExit(StackFsm<TestOwner> fsm, bool isShutdown) => fsm.Owner.Calls.Add("leave:base");
    }

    private sealed class BlockedBaseState : StackFsmState<TestOwner>
    {
        public override int Priority => 0;

        protected override bool OnConflictsWith(StackFsmState<TestOwner> other)
        {
            return other is OverlayState;
        }

        protected override void OnEnter(StackFsm<TestOwner> fsm) => fsm.Owner.Calls.Add("enter:blocked");

        protected override void OnExit(StackFsm<TestOwner> fsm, bool isShutdown) => fsm.Owner.Calls.Add("leave:blocked");

        protected override void OnRemoved(StackFsm<TestOwner> fsm) => fsm.Owner.Calls.Add("removed:blocked");
    }

    private sealed class OverlayState : StackFsmState<TestOwner>
    {
        public override int Priority => 10;

        protected override bool OnConflictsWith(StackFsmState<TestOwner> other)
        {
            return other is BlockedBaseState;
        }

        protected override void OnEnter(StackFsm<TestOwner> fsm) => fsm.Owner.Calls.Add("enter:overlay");

        protected override void OnUpdate(StackFsm<TestOwner> fsm, double deltaTime, double realDeltaTime)
        {
            fsm.Owner.Calls.Add("update:overlay");
        }

        protected override void OnExit(StackFsm<TestOwner> fsm, bool isShutdown) => fsm.Owner.Calls.Add("leave:overlay");

        protected override void OnRemoved(StackFsm<TestOwner> fsm) => fsm.Owner.Calls.Add("removed:overlay");
    }
}
