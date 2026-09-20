using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using NKGGameFramework.Diagnostics;

namespace NKGGameFramework.Core
{

    public interface IRuntimeContext : IGameLoop, IDisposable
    {
        IEventBus Events { get; }

        IGameTimer Timers { get; }

        GameFrameTime Time { get; }

        T RegisterModule<T>(T module)
            where T : Module;

        T GetModule<T>()
            where T : class;

        bool TryGetModule<T>(out T? module)
            where T : class;

        void Shutdown();
    }

    public sealed class RuntimeContext : IRuntimeContext
    {
        private readonly Dictionary<Type, Module> _modulesByConcreteType = new();
        private readonly List<Module> _modules = new();
        private readonly GameTimer _timers;
        private bool _disposed;
        private bool _updateListDirty;
        private List<IUpdateModule> _updateModules = new();

        public RuntimeContext(IEventBus? events = null, GameTimer? timers = null)
        {
            Events = events ?? new EventBus();
            _timers = timers ?? new GameTimer();
            Timers = _timers;
            GameDebugRuntimeRegistry.Register(this);
        }

        public IEventBus Events { get; }

        public IGameTimer Timers { get; }

        public GameFrameTime Time { get; private set; } = GameFrameTime.Zero;

        public IReadOnlyList<Module> Modules => _modules;

        public bool IsDisposed => _disposed;

        public T RegisterModule<T>(T module)
            where T : Module
        {
            NkgThrow.IfNull(module);
            ThrowIfDisposed();

            var type = module.GetType();
            if (_modulesByConcreteType.ContainsKey(type))
            {
                throw new InvalidOperationException($"Module '{type.Name}' is already registered.");
            }

            _modulesByConcreteType.Add(type, module);
            _modules.Add(module);
            _updateListDirty = true;

            module.Initialize(this);
            return module;
        }

        public T GetModule<T>()
            where T : class
        {
            return TryGetModule<T>(out var module)
                ? module!
                : throw new KeyNotFoundException($"Module '{typeof(T).Name}' is not registered.");
        }

        public bool TryGetModule<T>(out T? module)
            where T : class
        {
            ThrowIfDisposed();

            if (_modulesByConcreteType.TryGetValue(typeof(T), out var exactModule))
            {
                module = (T)(object)exactModule;
                return true;
            }

            var matches = _modules.OfType<T>().Take(2).ToArray();
            if (matches.Length == 1)
            {
                module = matches[0];
                return true;
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"More than one module can be assigned to '{typeof(T).Name}'. Use a concrete module type.");
            }

            module = null;
            return false;
        }

        public void Update(in GameFrameTime time)
        {
            ThrowIfDisposed();
            if (!GameDebugController.Shared.TryBeginRuntimeFrame())
            {
                return;
            }

            var logicStartedAt = Stopwatch.GetTimestamp();
            RebuildUpdateListIfNeeded();
            Time = time;
            _timers.Advance(in time);

            foreach (var module in _updateModules)
            {
                module.Update(in time);
            }

            Events.DispatchQueuedEvents();
            var logicElapsed = TimeSpan.FromTicks((Stopwatch.GetTimestamp() - logicStartedAt) * TimeSpan.TicksPerSecond / Stopwatch.Frequency);
            var currentTime = Time;
            GameDebugFramePublisher.Shared.Publish(nameof(RuntimeContext), in currentTime, logicElapsed);
        }

        public void Update(double deltaTime, double realDeltaTime)
        {
            var time = GameFrameTime.Advance(Time, deltaTime, realDeltaTime);
            Update(in time);
        }

        public void Shutdown()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var module in _modules.OrderBy(static module => module.Priority).ToArray())
            {
                module.Shutdown();
            }

            _modules.Clear();
            _modulesByConcreteType.Clear();
            _updateModules.Clear();
            _timers.Clear();
            Events.Clear();
            _disposed = true;
            GameDebugRuntimeRegistry.Unregister(this);
        }

        public void Dispose()
        {
            Shutdown();
        }

        private void RebuildUpdateListIfNeeded()
        {
            if (!_updateListDirty)
            {
                return;
            }

            _updateModules = _modules
                .OfType<IUpdateModule>()
                .OrderByDescending(static module => module is Module m ? m.Priority : 0)
                .ToList();
            _updateListDirty = false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RuntimeContext));
            }
        }
    }
}
