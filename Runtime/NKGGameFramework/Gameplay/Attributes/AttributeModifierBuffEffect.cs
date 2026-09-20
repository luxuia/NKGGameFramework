using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Gameplay
{

    // Built-in buff effect that grants attribute modifiers while the buff is active
    // and removes them when the buff ends. Modifiers are keyed by the BuffInstance
    // so refresh/reapply never double-counts.
    public sealed class AttributeModifierBuffEffect : BuffEffect
    {
        public override void OnApply(BuffEffectContext context) => ApplyModifiers(context);

        public override void OnRefresh(BuffEffectContext context)
        {
            // Level/stacks may have changed: drop the old grant and re-apply.
            RemoveModifiers(context);
            ApplyModifiers(context);
        }

        public override void OnRemove(BuffEffectContext context) => RemoveModifiers(context);

        private static void ApplyModifiers(BuffEffectContext context)
        {
            var target = context.Target;
            if (!target.Has<AttributeSetComponent>())
            {
                return;
            }

            ref var set = ref target.Get<AttributeSetComponent>();
            foreach (var spec in context.Buff.Definition.AttributeModifiers)
            {
                set.ApplyModifier(new AttributeModifier
                {
                    Attribute = spec.Attribute,
                    Operation = spec.Operation,
                    Magnitude = spec.GetMagnitude(context.Buff.Level),
                    Source = context.Buff,
                });
            }
        }

        private static void RemoveModifiers(BuffEffectContext context)
        {
            var target = context.Target;
            if (!target.Has<AttributeSetComponent>())
            {
                return;
            }

            ref var set = ref target.Get<AttributeSetComponent>();
            set.RemoveModifiers(context.Buff);
        }
    }
}
