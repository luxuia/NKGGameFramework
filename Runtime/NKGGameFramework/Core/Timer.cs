using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace NKGGameFramework.Core
{

    public interface IGameClock
    {
        long Tick { get; }

        TimeSpan Elapsed { get; }
    }

    public sealed class ManualGameClock : IGameClock
    {
        public long Tick { get; private set; }

        public TimeSpan Elapsed { get; private set; }

        public GameFrameTime AdvanceFrame(TimeSpan delta, TimeSpan? realDelta = null)
        {
            Advance(delta);
            return new GameFrameTime(Tick, delta, realDelta ?? delta);
        }

        public void Advance(TimeSpan delta)
        {
            if (delta < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delta));
            }

            Tick++;
            Elapsed += delta;
        }
    }

    public enum TimerExceptionPolicy
    {
        Throw,
        Continue,
    }

    public enum TimerTimeMode
    {
        GameTime,
        RealTime,
    }

    public readonly struct TimerCallbackContext : IEquatable<TimerCallbackContext>
    {
        public readonly long TimerId;
        public readonly long Tick;
        public readonly TimeSpan Elapsed;
        public readonly TimeSpan RealElapsed;
        public readonly GameFrameTime Time;
        public readonly TimerTimeMode TimeMode;
        public readonly long FireCount;

        public TimerCallbackContext(long TimerId, long Tick, TimeSpan Elapsed, TimeSpan RealElapsed, GameFrameTime Time, TimerTimeMode TimeMode, long FireCount)
        {
            this.TimerId = TimerId;
            this.Tick = Tick;
            this.Elapsed = Elapsed;
            this.RealElapsed = RealElapsed;
            this.Time = Time;
            this.TimeMode = TimeMode;
            this.FireCount = FireCount;
        }

        public bool Equals(TimerCallbackContext other) => EqualityComparer<long>.Default.Equals(TimerId, other.TimerId) && EqualityComparer<long>.Default.Equals(Tick, other.Tick) && EqualityComparer<TimeSpan>.Default.Equals(Elapsed, other.Elapsed) && EqualityComparer<TimeSpan>.Default.Equals(RealElapsed, other.RealElapsed) && EqualityComparer<GameFrameTime>.Default.Equals(Time, other.Time) && EqualityComparer<TimerTimeMode>.Default.Equals(TimeMode, other.TimeMode) && EqualityComparer<long>.Default.Equals(FireCount, other.FireCount);

        public override int GetHashCode() => HashCode.Combine(TimerId, Tick, Elapsed, RealElapsed, Time, TimeMode, FireCount);

        public override bool Equals(object? obj) => obj is TimerCallbackContext other && Equals(other);

        public static bool operator ==(TimerCallbackContext left, TimerCallbackContext right) => left.Equals(right);
        public static bool operator !=(TimerCallbackContext left, TimerCallbackContext right) => !left.Equals(right);

        public void Deconstruct(out long timerId, out long tick, out TimeSpan elapsed, out TimeSpan realElapsed, out GameFrameTime time, out TimerTimeMode timeMode, out long fireCount)
        {
            timerId = TimerId; tick = Tick; elapsed = Elapsed; realElapsed = RealElapsed; time = Time; timeMode = TimeMode; fireCount = FireCount;
        }
    }

    public interface IGameTimer
    {
        int ScheduledTimerCount { get; }

        TimerExceptionPolicy ExceptionPolicy { get; set; }

        Action<Exception, TimerCallbackContext>? ExceptionHandler { get; set; }

        long Schedule(TimeSpan delay, Action callback, bool repeat = false, TimerTimeMode timeMode = TimerTimeMode.GameTime);

        long Schedule(TimeSpan delay, Action<TimerCallbackContext> callback, bool repeat = false, TimerTimeMode timeMode = TimerTimeMode.GameTime);

        long ScheduleRepeating(TimeSpan interval, Action callback, TimerTimeMode timeMode = TimerTimeMode.GameTime);

        long ScheduleRepeating(TimeSpan delay, TimeSpan interval, Action callback, TimerTimeMode timeMode = TimerTimeMode.GameTime);

        bool Cancel(long timerId);

        bool HasTimer(long timerId);

        UniTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);

        UniTask DelayAsync(TimeSpan delay, TimerTimeMode timeMode, CancellationToken cancellationToken = default);

        UniTask NextFrameAsync(CancellationToken cancellationToken = default);

        UniTask DelayFrameAsync(int frameCount, CancellationToken cancellationToken = default);

        void Clear();
    }

    public class GameTimer : IGameTimer
    {
        private readonly object _syncRoot = new();
        private readonly PriorityQueue<TimerEntry, TimerPriority> _gameTimers = new();
        private readonly PriorityQueue<TimerEntry, TimerPriority> _realTimers = new();
        private readonly PriorityQueue<TimerEntry, FrameTimerPriority> _frameTimers = new();
        private readonly Dictionary<long, TimerEntry> _activeTimers = new();
        private long _nextId;

        public int ScheduledTimerCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _activeTimers.Count;
                }
            }
        }

        public long Tick { get; private set; }

        public TimeSpan Elapsed { get; private set; }

        public TimeSpan RealElapsed { get; private set; }

        public GameFrameTime Time { get; private set; } = GameFrameTime.Zero;

        public TimerExceptionPolicy ExceptionPolicy { get; set; } = TimerExceptionPolicy.Throw;

        public Action<Exception, TimerCallbackContext>? ExceptionHandler { get; set; }

        public long Schedule(TimeSpan delay, Action callback, bool repeat = false, TimerTimeMode timeMode = TimerTimeMode.GameTime)
        {
            NkgThrow.IfNull(callback);

            return Schedule(
                delay,
                _ => callback(),
                repeat,
                timeMode);
        }

        public long Schedule(TimeSpan delay, Action<TimerCallbackContext> callback, bool repeat = false, TimerTimeMode timeMode = TimerTimeMode.GameTime)
        {
            NkgThrow.IfNull(callback);
            ThrowIfNegative(delay, nameof(delay));
            ThrowIfInvalid(timeMode, nameof(timeMode));

            return ScheduleCore(delay, delay, repeat, timeMode, callback, onCanceled: null);
        }

        public long ScheduleRepeating(TimeSpan interval, Action callback, TimerTimeMode timeMode = TimerTimeMode.GameTime)
        {
            return ScheduleRepeating(interval, interval, callback, timeMode);
        }

        public long ScheduleRepeating(TimeSpan delay, TimeSpan interval, Action callback, TimerTimeMode timeMode = TimerTimeMode.GameTime)
        {
            NkgThrow.IfNull(callback);
            ThrowIfNegative(delay, nameof(delay));
            ThrowIfNegative(interval, nameof(interval));
            ThrowIfInvalid(timeMode, nameof(timeMode));

            return ScheduleCore(
                delay,
                interval,
                repeat: true,
                timeMode,
                _ => callback(),
                onCanceled: null);
        }

        public bool Cancel(long timerId)
        {
            TimerEntry? timer;
            lock (_syncRoot)
            {
                if (!_activeTimers.Remove(timerId, out timer))
                {
                    return false;
                }

                timer.IsCanceled = true;
            }

            timer.Cancel();
            return true;
        }

        public bool HasTimer(long timerId)
        {
            lock (_syncRoot)
            {
                return _activeTimers.ContainsKey(timerId);
            }
        }

        public UniTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            return DelayAsync(delay, TimerTimeMode.GameTime, cancellationToken);
        }

        public UniTask DelayAsync(TimeSpan delay, TimerTimeMode timeMode, CancellationToken cancellationToken = default)
        {
            ThrowIfNegative(delay, nameof(delay));
            ThrowIfInvalid(timeMode, nameof(timeMode));

            if (cancellationToken.IsCancellationRequested)
            {
                return UniTask.FromCanceled(cancellationToken);
            }

            var state = new TimerDelayState(this, cancellationToken);
            var timerId = ScheduleCore(
                delay,
                delay,
                repeat: false,
                timeMode,
                _ => state.Complete(),
                state.Cancel);

            state.Initialize(timerId);
            return state.Task;
        }

        public UniTask NextFrameAsync(CancellationToken cancellationToken = default)
        {
            return DelayFrameAsync(1, cancellationToken);
        }

        public UniTask DelayFrameAsync(int frameCount, CancellationToken cancellationToken = default)
        {
            if (frameCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameCount));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return UniTask.FromCanceled(cancellationToken);
            }

            var state = new TimerDelayState(this, cancellationToken);
            var timerId = ScheduleFrameCore(
                frameCount,
                _ => state.Complete(),
                state.Cancel);

            state.Initialize(timerId);
            return state.Task;
        }

        internal void Advance(in GameFrameTime time)
        {
            List<TimerEntry> dueTimers = new();
            lock (_syncRoot)
            {
                Time = time;
                Tick = time.Frame;
                Elapsed += time.DeltaTime;
                RealElapsed += time.RealDeltaTime;

                CollectDueTimers(_gameTimers, Elapsed, dueTimers);
                CollectDueTimers(_realTimers, RealElapsed, dueTimers);
                CollectDueFrameTimers(dueTimers);
            }

            dueTimers.Sort(static (left, right) => left.Id.CompareTo(right.Id));

            foreach (var timer in dueTimers)
            {
                Invoke(timer);
            }
        }

        public void Clear()
        {
            TimerEntry[] timers;
            lock (_syncRoot)
            {
                timers = _activeTimers.Values.ToArray();
                foreach (var timer in timers)
                {
                    timer.IsCanceled = true;
                }

                _activeTimers.Clear();
                _gameTimers.Clear();
                _realTimers.Clear();
                _frameTimers.Clear();
            }

            foreach (var timer in timers)
            {
                timer.Cancel();
            }
        }

        private long ScheduleFrameCore(
            int frameCount,
            Action<TimerCallbackContext> callback,
            Action? onCanceled)
        {
            lock (_syncRoot)
            {
                var id = checked(++_nextId);
                var dueFrame = checked(Tick + frameCount);
                var timer = new TimerEntry(
                    id,
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    repeat: false,
                    TimerTimeMode.GameTime,
                    callback,
                    onCanceled,
                    TimerScheduleKind.Frame,
                    dueFrame);

                _activeTimers.Add(id, timer);
                Enqueue(timer);
                return id;
            }
        }

        private long ScheduleCore(
            TimeSpan delay,
            TimeSpan interval,
            bool repeat,
            TimerTimeMode timeMode,
            Action<TimerCallbackContext> callback,
            Action? onCanceled)
        {
            lock (_syncRoot)
            {
                var id = checked(++_nextId);
                var dueTime = GetElapsed(timeMode) + delay;
                var timer = new TimerEntry(id, dueTime, interval, repeat, timeMode, callback, onCanceled);

                _activeTimers.Add(id, timer);
                Enqueue(timer);
                return id;
            }
        }

        private void CollectDueTimers(
            PriorityQueue<TimerEntry, TimerPriority> queue,
            TimeSpan elapsed,
            List<TimerEntry> dueTimers)
        {
            while (queue.TryPeek(out var timer, out var priority) && priority.DueTime <= elapsed)
            {
                queue.Dequeue();
                if (timer.IsCanceled ||
                    !_activeTimers.TryGetValue(timer.Id, out var activeTimer) ||
                    !ReferenceEquals(activeTimer, timer))
                {
                    continue;
                }

                dueTimers.Add(timer);
            }
        }

        private void CollectDueFrameTimers(List<TimerEntry> dueTimers)
        {
            while (_frameTimers.TryPeek(out var timer, out var priority) && priority.DueFrame <= Tick)
            {
                _frameTimers.Dequeue();
                if (timer.IsCanceled ||
                    !_activeTimers.TryGetValue(timer.Id, out var activeTimer) ||
                    !ReferenceEquals(activeTimer, timer))
                {
                    continue;
                }

                dueTimers.Add(timer);
            }
        }

        private void Invoke(TimerEntry timer)
        {
            if (!TryBeginInvoke(timer, out var context))
            {
                return;
            }

            var completed = false;
            try
            {
                timer.Callback(context);
                completed = true;
            }
            catch (Exception ex) when (ExceptionPolicy == TimerExceptionPolicy.Continue)
            {
                ExceptionHandler?.Invoke(ex, context);
                completed = true;
            }
            finally
            {
                if (timer.Repeat)
                {
                    if (completed)
                    {
                        Reschedule(timer);
                    }
                    else
                    {
                        RemoveActive(timer);
                    }
                }
            }
        }

        private bool TryBeginInvoke(TimerEntry timer, out TimerCallbackContext context)
        {
            lock (_syncRoot)
            {
                if (timer.IsCanceled ||
                    !_activeTimers.TryGetValue(timer.Id, out var activeTimer) ||
                    !ReferenceEquals(activeTimer, timer))
                {
                    context = default;
                    return false;
                }

                timer.FireCount++;
                if (!timer.Repeat)
                {
                    _activeTimers.Remove(timer.Id);
                }

                context = new TimerCallbackContext(
                    timer.Id,
                    Tick,
                    Elapsed,
                    RealElapsed,
                    Time,
                    timer.TimeMode,
                    timer.FireCount);
                return true;
            }
        }

        private void Reschedule(TimerEntry timer)
        {
            lock (_syncRoot)
            {
                if (timer.IsCanceled ||
                    !_activeTimers.TryGetValue(timer.Id, out var activeTimer) ||
                    !ReferenceEquals(activeTimer, timer))
                {
                    return;
                }

                timer.DueTime = GetElapsed(timer.TimeMode) + timer.Interval;
                Enqueue(timer);
            }
        }

        private void RemoveActive(TimerEntry timer)
        {
            lock (_syncRoot)
            {
                if (_activeTimers.TryGetValue(timer.Id, out var activeTimer) &&
                    ReferenceEquals(activeTimer, timer))
                {
                    timer.IsCanceled = true;
                    _activeTimers.Remove(timer.Id);
                }
            }
        }

        private void Enqueue(TimerEntry timer)
        {
            if (timer.Kind == TimerScheduleKind.Frame)
            {
                _frameTimers.Enqueue(timer, new FrameTimerPriority(timer.DueFrame, timer.Id));
                return;
            }

            GetQueue(timer.TimeMode).Enqueue(timer, new TimerPriority(timer.DueTime, timer.Id));
        }

        private TimeSpan GetElapsed(TimerTimeMode timeMode)
        {
            return timeMode switch
            {
                TimerTimeMode.GameTime => Elapsed,
                TimerTimeMode.RealTime => RealElapsed,
                _ => throw new ArgumentOutOfRangeException(nameof(timeMode)),
            };
        }

        private PriorityQueue<TimerEntry, TimerPriority> GetQueue(TimerTimeMode timeMode)
        {
            return timeMode switch
            {
                TimerTimeMode.GameTime => _gameTimers,
                TimerTimeMode.RealTime => _realTimers,
                _ => throw new ArgumentOutOfRangeException(nameof(timeMode)),
            };
        }

        private static void ThrowIfNegative(TimeSpan value, string parameterName)
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void ThrowIfInvalid(TimerTimeMode timeMode, string parameterName)
        {
            if (!Enum.IsDefined(typeof(TimerTimeMode), timeMode))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private enum TimerScheduleKind
        {
            Time,
            Frame,
        }

        private sealed class TimerEntry
        {
            public TimerEntry(
                long id,
                TimeSpan dueTime,
                TimeSpan interval,
                bool repeat,
                TimerTimeMode timeMode,
                Action<TimerCallbackContext> callback,
                Action? onCanceled,
                TimerScheduleKind kind = TimerScheduleKind.Time,
                long dueFrame = 0)
            {
                Id = id;
                DueTime = dueTime;
                Interval = interval;
                Repeat = repeat;
                TimeMode = timeMode;
                Kind = kind;
                DueFrame = dueFrame;
                Callback = callback;
                _onCanceled = onCanceled;
            }

            private readonly Action? _onCanceled;

            public long Id { get; }

            public TimeSpan DueTime { get; set; }

            public TimeSpan Interval { get; }

            public bool Repeat { get; }

            public TimerTimeMode TimeMode { get; }

            public TimerScheduleKind Kind { get; }

            public long DueFrame { get; }

            public Action<TimerCallbackContext> Callback { get; }

            public long FireCount { get; set; }

            public bool IsCanceled { get; set; }

            public void Cancel()
            {
                _onCanceled?.Invoke();
            }
        }

        private sealed class TimerDelayState
        {
            private readonly GameTimer _timer;
            private readonly CancellationToken _cancellationToken;

            public TimerDelayState(GameTimer timer, CancellationToken cancellationToken)
            {
                _timer = timer;
                _cancellationToken = cancellationToken;
            }

            private readonly UniTaskCompletionSource<AsyncUnit> _completion = new();
            private CancellationTokenRegistration _cancellationRegistration;
            private int _isCompleted;
            private long _timerId;

            public UniTask Task => _completion.Task.AsUniTask();

            public void Initialize(long timerId)
            {
                _timerId = timerId;
                if (!_cancellationToken.CanBeCanceled)
                {
                    return;
                }

                _cancellationRegistration = _cancellationToken.Register(
                    static state => ((TimerDelayState)state!).CancelFromToken(),
                    this);

                if (Volatile.Read(ref _isCompleted) != 0)
                {
                    _cancellationRegistration.Dispose();
                }
            }

            public void Complete()
            {
                if (Interlocked.Exchange(ref _isCompleted, 1) != 0)
                {
                    return;
                }

                _cancellationRegistration.Dispose();
                _completion.TrySetResult(AsyncUnit.Default);
            }

            public void Cancel()
            {
                if (Interlocked.Exchange(ref _isCompleted, 1) != 0)
                {
                    return;
                }

                if (!_cancellationToken.IsCancellationRequested)
                {
                    _cancellationRegistration.Dispose();
                }

                if (_cancellationToken.IsCancellationRequested)
                {
                    _completion.TrySetCanceled(_cancellationToken);
                }
                else
                {
                    _completion.TrySetCanceled();
                }
            }

            private void CancelFromToken()
            {
                _timer.Cancel(_timerId);
            }
        }

        private readonly struct TimerPriority : IEquatable<TimerPriority>, IComparable<TimerPriority>
        {
            public readonly TimeSpan DueTime;
            public readonly long Id;

            public TimerPriority(TimeSpan DueTime, long Id)
            {
                this.DueTime = DueTime;
                this.Id = Id;
            }

            public bool Equals(TimerPriority other) => EqualityComparer<TimeSpan>.Default.Equals(DueTime, other.DueTime) && EqualityComparer<long>.Default.Equals(Id, other.Id);

            public override int GetHashCode() => HashCode.Combine(DueTime, Id);

            public override bool Equals(object? obj) => obj is TimerPriority other && Equals(other);

            public static bool operator ==(TimerPriority left, TimerPriority right) => left.Equals(right);
            public static bool operator !=(TimerPriority left, TimerPriority right) => !left.Equals(right);

            public void Deconstruct(out TimeSpan dueTime, out long id)
            {
                dueTime = DueTime; id = Id;
            }


            public int CompareTo(TimerPriority other)
            {
                var dueTimeComparison = DueTime.CompareTo(other.DueTime);
                return dueTimeComparison != 0
                    ? dueTimeComparison
                    : Id.CompareTo(other.Id);
            }
        }

        private readonly struct FrameTimerPriority : IEquatable<FrameTimerPriority>, IComparable<FrameTimerPriority>
        {
            public readonly long DueFrame;
            public readonly long Id;

            public FrameTimerPriority(long DueFrame, long Id)
            {
                this.DueFrame = DueFrame;
                this.Id = Id;
            }

            public bool Equals(FrameTimerPriority other) => EqualityComparer<long>.Default.Equals(DueFrame, other.DueFrame) && EqualityComparer<long>.Default.Equals(Id, other.Id);

            public override int GetHashCode() => HashCode.Combine(DueFrame, Id);

            public override bool Equals(object? obj) => obj is FrameTimerPriority other && Equals(other);

            public static bool operator ==(FrameTimerPriority left, FrameTimerPriority right) => left.Equals(right);
            public static bool operator !=(FrameTimerPriority left, FrameTimerPriority right) => !left.Equals(right);

            public void Deconstruct(out long dueFrame, out long id)
            {
                dueFrame = DueFrame; id = Id;
            }


            public int CompareTo(FrameTimerPriority other)
            {
                var dueFrameComparison = DueFrame.CompareTo(other.DueFrame);
                return dueFrameComparison != 0
                    ? dueFrameComparison
                    : Id.CompareTo(other.Id);
            }
        }
    }

    public sealed class TimerService : GameTimer, IUpdateModule
    {
        public void Update(in GameFrameTime time)
        {
            Advance(in time);
        }

        public void Update(double deltaTime, double realDeltaTime)
        {
            var time = GameFrameTime.Advance(Time, deltaTime, realDeltaTime);
            Update(in time);
        }
    }
}
