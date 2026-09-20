using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Gameplay
{

    public interface IBuffEffect
    {
        void OnApply(BuffEffectContext context);

        void OnRefresh(BuffEffectContext context);

        void OnUpdate(BuffEffectContext context);

        void OnRemove(BuffEffectContext context);
    }

    public abstract class BuffEffect : IBuffEffect
    {
        public virtual void OnApply(BuffEffectContext context)
        {
        }

        public virtual void OnRefresh(BuffEffectContext context)
        {
        }

        public virtual void OnUpdate(BuffEffectContext context)
        {
        }

        public virtual void OnRemove(BuffEffectContext context)
        {
        }
    }

    public sealed class DelegateBuffEffect : BuffEffect
    {
        private readonly Action<BuffEffectContext>? _onApply;
        private readonly Action<BuffEffectContext>? _onRefresh;
        private readonly Action<BuffEffectContext>? _onUpdate;
        private readonly Action<BuffEffectContext>? _onRemove;

        public DelegateBuffEffect(
            Action<BuffEffectContext>? onApply = null,
            Action<BuffEffectContext>? onRefresh = null,
            Action<BuffEffectContext>? onUpdate = null,
            Action<BuffEffectContext>? onRemove = null)
        {
            _onApply = onApply;
            _onRefresh = onRefresh;
            _onUpdate = onUpdate;
            _onRemove = onRemove;
        }

        public override void OnApply(BuffEffectContext context)
        {
            _onApply?.Invoke(context);
        }

        public override void OnRefresh(BuffEffectContext context)
        {
            _onRefresh?.Invoke(context);
        }

        public override void OnUpdate(BuffEffectContext context)
        {
            _onUpdate?.Invoke(context);
        }

        public override void OnRemove(BuffEffectContext context)
        {
            _onRemove?.Invoke(context);
        }
    }
}
