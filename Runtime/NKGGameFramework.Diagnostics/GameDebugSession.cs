using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NKGGameFramework.Core;
using NKGGameFramework.Diagnostics;
using NKGGameFramework.Ecs;

namespace NKGGameFramework.Diagnostics
{

    public sealed class GameDebugSession
    {
        private readonly object _gate = new();
        private readonly List<RuntimeContext> _runtimeContexts = new();
        private readonly List<World> _worlds = new();

        public GameDebugSession Register(RuntimeContext runtimeContext)
        {
            NkgThrow.IfNull(runtimeContext);

            lock (_gate)
            {
                if (!_runtimeContexts.Contains(runtimeContext))
                {
                    _runtimeContexts.Add(runtimeContext);
                }
            }

            return this;
        }

        public GameDebugSession Register(World world)
        {
            NkgThrow.IfNull(world);

            lock (_gate)
            {
                if (!_worlds.Contains(world))
                {
                    _worlds.Add(world);
                }
            }

            return this;
        }

        public bool Unregister(RuntimeContext runtimeContext)
        {
            NkgThrow.IfNull(runtimeContext);

            lock (_gate)
            {
                return _runtimeContexts.Remove(runtimeContext);
            }
        }

        public bool Unregister(World world)
        {
            NkgThrow.IfNull(world);

            lock (_gate)
            {
                return _worlds.Remove(world);
            }
        }

        public IReadOnlyList<RuntimeContext> GetRuntimeContexts()
        {
            lock (_gate)
            {
                return _runtimeContexts.Count > 0
                    ? _runtimeContexts.ToArray()
                    : GameDebugRuntimeRegistry.GetRuntimeContexts();
            }
        }

        public IReadOnlyList<World> GetWorlds()
        {
            lock (_gate)
            {
                return _worlds.Count > 0
                    ? _worlds.ToArray()
                    : GameDebugRuntimeRegistry.GetWorlds();
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _runtimeContexts.Clear();
                _worlds.Clear();
            }
        }
    }
}
