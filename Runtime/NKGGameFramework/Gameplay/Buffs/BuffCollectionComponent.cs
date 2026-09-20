using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NKGGameFramework.Ecs;
using OdinSerializer;

namespace NKGGameFramework.Gameplay
{

    [ComponentGraph(Group = "Gameplay/Buffs", Order = 10)]
    public struct BuffCollectionComponent : IComponent
    {
        [OdinSerialize]
        private List<BuffInstance>? _buffs;

        public IReadOnlyList<BuffInstance> Buffs => MutableBuffs;

        public int Count => MutableBuffs.Count;

        internal List<BuffInstance> MutableBuffs => _buffs ??= new();

        public bool Has(string buffId)
        {
            return TryGet(buffId, out _);
        }

        public bool TryGet(string buffId, out BuffInstance buff)
        {
            NkgThrow.IfNullOrWhiteSpace(buffId);

            foreach (var candidate in MutableBuffs)
            {
                if (candidate.Definition.Id == buffId && candidate.State != BuffState.Finished)
                {
                    buff = candidate;
                    return true;
                }
            }

            buff = null!;
            return false;
        }
    }
}
