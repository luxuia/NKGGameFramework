using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Gameplay
{

    public sealed class BuffEffectRegistry
    {
        private readonly Dictionary<string, IBuffEffect> _effects = new(StringComparer.Ordinal);

        public BuffEffectRegistry()
        {
            Register(BuffEffectKeys.None, NullBuffEffect.Instance);
            Register(BuffEffectKeys.AttributeModifier, new AttributeModifierBuffEffect());
        }

        public static BuffEffectRegistry CreateDefault()
        {
            return new BuffEffectRegistry();
        }

        public BuffEffectRegistry Register(string key, IBuffEffect effect)
        {
            NkgThrow.IfNullOrWhiteSpace(key);
            NkgThrow.IfNull(effect);

            _effects[key] = effect;
            return this;
        }

        public bool TryResolve(string key, out IBuffEffect effect)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                key = BuffEffectKeys.None;
            }

            return _effects.TryGetValue(key, out effect!);
        }

        public IBuffEffect Resolve(string key)
        {
            if (TryResolve(key, out var effect))
            {
                return effect;
            }

            throw new KeyNotFoundException($"Buff effect '{key}' is not registered.");
        }

        private sealed class NullBuffEffect : BuffEffect
        {
            public static readonly NullBuffEffect Instance = new();
        }
    }
}
