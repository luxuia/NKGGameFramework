using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NKGGameFramework.Ecs;

namespace NKGGameFramework.Gameplay
{

    public readonly struct SkillCastResult : IEquatable<SkillCastResult>
    {
        public readonly bool Succeeded;
        public readonly SkillCastFailureReason FailureReason;
        public readonly string? Message;

        public SkillCastResult(bool Succeeded, SkillCastFailureReason FailureReason = SkillCastFailureReason.None, string? Message = null)
        {
            this.Succeeded = Succeeded;
            this.FailureReason = FailureReason;
            this.Message = Message;
        }

        public bool Equals(SkillCastResult other) => EqualityComparer<bool>.Default.Equals(Succeeded, other.Succeeded) && EqualityComparer<SkillCastFailureReason>.Default.Equals(FailureReason, other.FailureReason) && EqualityComparer<string?>.Default.Equals(Message, other.Message);

        public override int GetHashCode() => HashCode.Combine(Succeeded, FailureReason, Message);

        public override bool Equals(object? obj) => obj is SkillCastResult other && Equals(other);

        public static bool operator ==(SkillCastResult left, SkillCastResult right) => left.Equals(right);
        public static bool operator !=(SkillCastResult left, SkillCastResult right) => !left.Equals(right);

        public void Deconstruct(out bool succeeded, out SkillCastFailureReason failureReason, out string? message)
        {
            succeeded = Succeeded; failureReason = FailureReason; message = Message;
        }
    }

    public readonly struct SkillCastSucceeded : IEquatable<SkillCastSucceeded>
    {
        public readonly EntityRef Caster;
        public readonly EntityRef Target;
        public readonly string SkillId;
        public readonly int Level;

        public SkillCastSucceeded(EntityRef Caster, EntityRef Target, string SkillId, int Level)
        {
            this.Caster = Caster;
            this.Target = Target;
            this.SkillId = SkillId;
            this.Level = Level;
        }

        public bool Equals(SkillCastSucceeded other) => EqualityComparer<EntityRef>.Default.Equals(Caster, other.Caster) && EqualityComparer<EntityRef>.Default.Equals(Target, other.Target) && EqualityComparer<string>.Default.Equals(SkillId, other.SkillId) && EqualityComparer<int>.Default.Equals(Level, other.Level);

        public override int GetHashCode() => HashCode.Combine(Caster, Target, SkillId, Level);

        public override bool Equals(object? obj) => obj is SkillCastSucceeded other && Equals(other);

        public static bool operator ==(SkillCastSucceeded left, SkillCastSucceeded right) => left.Equals(right);
        public static bool operator !=(SkillCastSucceeded left, SkillCastSucceeded right) => !left.Equals(right);

        public void Deconstruct(out EntityRef caster, out EntityRef target, out string skillId, out int level)
        {
            caster = Caster; target = Target; skillId = SkillId; level = Level;
        }
    }

    public readonly struct SkillCastFailed : IEquatable<SkillCastFailed>
    {
        public readonly EntityRef Caster;
        public readonly EntityRef Target;
        public readonly string SkillId;
        public readonly SkillCastFailureReason Reason;
        public readonly string? Message;

        public SkillCastFailed(EntityRef Caster, EntityRef Target, string SkillId, SkillCastFailureReason Reason, string? Message)
        {
            this.Caster = Caster;
            this.Target = Target;
            this.SkillId = SkillId;
            this.Reason = Reason;
            this.Message = Message;
        }

        public bool Equals(SkillCastFailed other) => EqualityComparer<EntityRef>.Default.Equals(Caster, other.Caster) && EqualityComparer<EntityRef>.Default.Equals(Target, other.Target) && EqualityComparer<string>.Default.Equals(SkillId, other.SkillId) && EqualityComparer<SkillCastFailureReason>.Default.Equals(Reason, other.Reason) && EqualityComparer<string?>.Default.Equals(Message, other.Message);

        public override int GetHashCode() => HashCode.Combine(Caster, Target, SkillId, Reason, Message);

        public override bool Equals(object? obj) => obj is SkillCastFailed other && Equals(other);

        public static bool operator ==(SkillCastFailed left, SkillCastFailed right) => left.Equals(right);
        public static bool operator !=(SkillCastFailed left, SkillCastFailed right) => !left.Equals(right);

        public void Deconstruct(out EntityRef caster, out EntityRef target, out string skillId, out SkillCastFailureReason reason, out string? message)
        {
            caster = Caster; target = Target; skillId = SkillId; reason = Reason; message = Message;
        }
    }
}
