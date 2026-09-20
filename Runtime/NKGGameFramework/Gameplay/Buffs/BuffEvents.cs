using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NKGGameFramework.Ecs;

namespace NKGGameFramework.Gameplay
{

    public readonly struct BuffApplicationResult : IEquatable<BuffApplicationResult>
    {
        public readonly bool IsNewInstance;
        public readonly bool IsRefresh;
        public readonly BuffInstance Instance;

        public BuffApplicationResult(bool IsNewInstance, bool IsRefresh, BuffInstance Instance)
        {
            this.IsNewInstance = IsNewInstance;
            this.IsRefresh = IsRefresh;
            this.Instance = Instance;
        }

        public bool Equals(BuffApplicationResult other) => EqualityComparer<bool>.Default.Equals(IsNewInstance, other.IsNewInstance) && EqualityComparer<bool>.Default.Equals(IsRefresh, other.IsRefresh) && EqualityComparer<BuffInstance>.Default.Equals(Instance, other.Instance);

        public override int GetHashCode() => HashCode.Combine(IsNewInstance, IsRefresh, Instance);

        public override bool Equals(object? obj) => obj is BuffApplicationResult other && Equals(other);

        public static bool operator ==(BuffApplicationResult left, BuffApplicationResult right) => left.Equals(right);
        public static bool operator !=(BuffApplicationResult left, BuffApplicationResult right) => !left.Equals(right);

        public void Deconstruct(out bool isNewInstance, out bool isRefresh, out BuffInstance instance)
        {
            isNewInstance = IsNewInstance; isRefresh = IsRefresh; instance = Instance;
        }
    }

    public readonly struct BuffApplied : IEquatable<BuffApplied>
    {
        public readonly EntityRef Target;
        public readonly EntityRef Source;
        public readonly string BuffId;
        public readonly int Level;
        public readonly int Stacks;

        public BuffApplied(EntityRef Target, EntityRef Source, string BuffId, int Level, int Stacks)
        {
            this.Target = Target;
            this.Source = Source;
            this.BuffId = BuffId;
            this.Level = Level;
            this.Stacks = Stacks;
        }

        public bool Equals(BuffApplied other) => EqualityComparer<EntityRef>.Default.Equals(Target, other.Target) && EqualityComparer<EntityRef>.Default.Equals(Source, other.Source) && EqualityComparer<string>.Default.Equals(BuffId, other.BuffId) && EqualityComparer<int>.Default.Equals(Level, other.Level) && EqualityComparer<int>.Default.Equals(Stacks, other.Stacks);

        public override int GetHashCode() => HashCode.Combine(Target, Source, BuffId, Level, Stacks);

        public override bool Equals(object? obj) => obj is BuffApplied other && Equals(other);

        public static bool operator ==(BuffApplied left, BuffApplied right) => left.Equals(right);
        public static bool operator !=(BuffApplied left, BuffApplied right) => !left.Equals(right);

        public void Deconstruct(out EntityRef target, out EntityRef source, out string buffId, out int level, out int stacks)
        {
            target = Target; source = Source; buffId = BuffId; level = Level; stacks = Stacks;
        }
    }

    public readonly struct BuffRefreshed : IEquatable<BuffRefreshed>
    {
        public readonly EntityRef Target;
        public readonly EntityRef Source;
        public readonly string BuffId;
        public readonly int Level;
        public readonly int Stacks;

        public BuffRefreshed(EntityRef Target, EntityRef Source, string BuffId, int Level, int Stacks)
        {
            this.Target = Target;
            this.Source = Source;
            this.BuffId = BuffId;
            this.Level = Level;
            this.Stacks = Stacks;
        }

        public bool Equals(BuffRefreshed other) => EqualityComparer<EntityRef>.Default.Equals(Target, other.Target) && EqualityComparer<EntityRef>.Default.Equals(Source, other.Source) && EqualityComparer<string>.Default.Equals(BuffId, other.BuffId) && EqualityComparer<int>.Default.Equals(Level, other.Level) && EqualityComparer<int>.Default.Equals(Stacks, other.Stacks);

        public override int GetHashCode() => HashCode.Combine(Target, Source, BuffId, Level, Stacks);

        public override bool Equals(object? obj) => obj is BuffRefreshed other && Equals(other);

        public static bool operator ==(BuffRefreshed left, BuffRefreshed right) => left.Equals(right);
        public static bool operator !=(BuffRefreshed left, BuffRefreshed right) => !left.Equals(right);

        public void Deconstruct(out EntityRef target, out EntityRef source, out string buffId, out int level, out int stacks)
        {
            target = Target; source = Source; buffId = BuffId; level = Level; stacks = Stacks;
        }
    }

    public readonly struct BuffRemoved : IEquatable<BuffRemoved>
    {
        public readonly EntityRef Target;
        public readonly EntityRef Source;
        public readonly string BuffId;
        public readonly int Level;
        public readonly int Stacks;

        public BuffRemoved(EntityRef Target, EntityRef Source, string BuffId, int Level, int Stacks)
        {
            this.Target = Target;
            this.Source = Source;
            this.BuffId = BuffId;
            this.Level = Level;
            this.Stacks = Stacks;
        }

        public bool Equals(BuffRemoved other) => EqualityComparer<EntityRef>.Default.Equals(Target, other.Target) && EqualityComparer<EntityRef>.Default.Equals(Source, other.Source) && EqualityComparer<string>.Default.Equals(BuffId, other.BuffId) && EqualityComparer<int>.Default.Equals(Level, other.Level) && EqualityComparer<int>.Default.Equals(Stacks, other.Stacks);

        public override int GetHashCode() => HashCode.Combine(Target, Source, BuffId, Level, Stacks);

        public override bool Equals(object? obj) => obj is BuffRemoved other && Equals(other);

        public static bool operator ==(BuffRemoved left, BuffRemoved right) => left.Equals(right);
        public static bool operator !=(BuffRemoved left, BuffRemoved right) => !left.Equals(right);

        public void Deconstruct(out EntityRef target, out EntityRef source, out string buffId, out int level, out int stacks)
        {
            target = Target; source = Source; buffId = BuffId; level = Level; stacks = Stacks;
        }
    }
}
