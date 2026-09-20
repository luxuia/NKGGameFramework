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

    [ComponentGraph(Group = "Gameplay/Skills", Order = 10)]
    public struct SkillBookComponent : IComponent
    {
        [OdinSerialize]
        private Dictionary<string, SkillSlot>? _skills;

        public IReadOnlyDictionary<string, SkillSlot> Skills => MutableSkills;

        internal Dictionary<string, SkillSlot> MutableSkills => _skills ??= new Dictionary<string, SkillSlot>(StringComparer.Ordinal);

        public bool TryGet(string skillId, out SkillSlot slot)
        {
            NkgThrow.IfNullOrWhiteSpace(skillId);
            return MutableSkills.TryGetValue(skillId, out slot!);
        }
    }
}
