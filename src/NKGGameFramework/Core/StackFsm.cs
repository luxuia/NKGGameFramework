namespace NKGGameFramework.Core;

public sealed class StackFsm<TOwner>
    where TOwner : class
{
    private readonly LinkedList<StackFsmState<TOwner>> _states = [];

    public StackFsm(string name, TOwner owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(owner);

        Name = name;
        Owner = owner;
    }

    public string Name { get; }

    public TOwner Owner { get; }

    public StackFsmState<TOwner>? CurrentState => _states.First?.Value;

    public IReadOnlyList<StackFsmState<TOwner>> States => _states.ToArray();

    public bool IsRunning => _states.Count > 0;

    public bool Contains<TState>()
        where TState : StackFsmState<TOwner>
    {
        return Find<TState>() is not null;
    }

    public TState? Find<TState>()
        where TState : StackFsmState<TOwner>
    {
        foreach (var state in _states)
        {
            if (state is TState typed)
            {
                return typed;
            }
        }

        return null;
    }

    public bool Push(StackFsmState<TOwner> state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var existing = Find(state.GetType());
        if (existing is not null)
        {
            return Promote(existing);
        }

        if (!state.CanEnter(this))
        {
            return false;
        }

        var conflicts = new List<StackFsmState<TOwner>>();
        foreach (var activeState in _states)
        {
            if (!state.ConflictsWith(activeState))
            {
                continue;
            }

            if (activeState.Priority > state.Priority)
            {
                return false;
            }

            conflicts.Add(activeState);
        }

        foreach (var conflict in conflicts)
        {
            Remove(conflict, notifyRemoved: true);
        }

        var previousTop = CurrentState;
        InsertByPriority(state);
        state.Initialize(this);

        if (ReferenceEquals(CurrentState, state))
        {
            if (!ReferenceEquals(previousTop, state))
            {
                previousTop?.Exit(this, isShutdown: false);
            }

            state.Enter(this);
        }

        return ReferenceEquals(CurrentState, state);
    }

    public bool PopCurrent()
    {
        var current = CurrentState;
        return current is not null && Remove(current, notifyRemoved: true);
    }

    public bool Remove<TState>()
        where TState : StackFsmState<TOwner>
    {
        var state = Find<TState>();
        return state is not null && Remove(state, notifyRemoved: true);
    }

    public void Update(in GameFrameTime time)
    {
        CurrentState?.Update(this, in time);
    }

    public void Update(double deltaTime, double realDeltaTime)
    {
        var time = GameFrameTime.FromSeconds(deltaTime, realDeltaTime);
        Update(in time);
    }

    public void Shutdown()
    {
        while (CurrentState is { } current)
        {
            Remove(current, notifyRemoved: false, isShutdown: true);
        }
    }

    private StackFsmState<TOwner>? Find(Type stateType)
    {
        foreach (var state in _states)
        {
            if (state.GetType() == stateType)
            {
                return state;
            }
        }

        return null;
    }

    private bool Promote(StackFsmState<TOwner> state)
    {
        if (!_states.Contains(state))
        {
            throw new FrameworkException($"Stack FSM '{Name}' cannot promote a detached state.");
        }

        var previousTop = CurrentState;
        _states.Remove(state);
        InsertByPriority(state);

        if (ReferenceEquals(CurrentState, state) && !ReferenceEquals(previousTop, state))
        {
            previousTop?.Exit(this, isShutdown: false);
            state.Enter(this);
            return true;
        }

        return ReferenceEquals(CurrentState, state);
    }

    private bool Remove(StackFsmState<TOwner> state, bool notifyRemoved, bool isShutdown = false)
    {
        var wasTop = ReferenceEquals(CurrentState, state);
        if (!_states.Remove(state))
        {
            return false;
        }

        if (wasTop)
        {
            state.Exit(this, isShutdown);
        }

        if (notifyRemoved)
        {
            state.Removed(this);
        }

        if (wasTop && CurrentState is { } next)
        {
            next.Enter(this);
        }

        return true;
    }

    private void InsertByPriority(StackFsmState<TOwner> state)
    {
        var node = _states.First;
        while (node is not null)
        {
            if (state.Priority >= node.Value.Priority)
            {
                _states.AddBefore(node, state);
                return;
            }

            node = node.Next;
        }

        _states.AddLast(state);
    }
}

public abstract class StackFsmState<TOwner>
    where TOwner : class
{
    public virtual string Name => GetType().Name;

    public virtual int Priority => 0;

    protected TOwner Owner { get; private set; } = null!;

    internal bool CanEnter(StackFsm<TOwner> fsm)
    {
        return OnCanEnter(fsm);
    }

    internal bool ConflictsWith(StackFsmState<TOwner> other)
    {
        return OnConflictsWith(other);
    }

    protected virtual bool OnCanEnter(StackFsm<TOwner> fsm)
    {
        return true;
    }

    protected virtual bool OnConflictsWith(StackFsmState<TOwner> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.GetType() == GetType();
    }

    internal void Initialize(StackFsm<TOwner> fsm)
    {
        Owner = fsm.Owner;
        OnInitialize(fsm);
    }

    internal void Enter(StackFsm<TOwner> fsm)
    {
        OnEnter(fsm);
    }

    internal void Update(StackFsm<TOwner> fsm, in GameFrameTime time)
    {
        OnUpdate(fsm, in time);
    }

    internal void Exit(StackFsm<TOwner> fsm, bool isShutdown)
    {
        OnExit(fsm, isShutdown);
    }

    internal void Removed(StackFsm<TOwner> fsm)
    {
        OnRemoved(fsm);
    }

    protected virtual void OnInitialize(StackFsm<TOwner> fsm)
    {
    }

    protected virtual void OnEnter(StackFsm<TOwner> fsm)
    {
    }

    protected virtual void OnUpdate(StackFsm<TOwner> fsm, in GameFrameTime time)
    {
        OnUpdate(fsm, time.DeltaSeconds, time.RealDeltaSeconds);
    }

    protected virtual void OnUpdate(StackFsm<TOwner> fsm, double deltaTime, double realDeltaTime)
    {
    }

    protected virtual void OnExit(StackFsm<TOwner> fsm, bool isShutdown)
    {
    }

    protected virtual void OnRemoved(StackFsm<TOwner> fsm)
    {
    }
}
