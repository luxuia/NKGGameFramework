using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NKGGameFramework.Ecs;

namespace NKGGameFramework.Gameplay
{

    public sealed class BehaviorBlackboardPoolComponent : ISceneComponent, IDisposable
    {
        private readonly BehaviorBlackboardValuePool _valuePool = new();

        internal BehaviorBlackboardValuePool ValuePool => _valuePool;

        public void Dispose()
        {
            _valuePool.Clear();
        }
    }
}
