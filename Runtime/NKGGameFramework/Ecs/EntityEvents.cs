using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Ecs
{

    public readonly struct EntityCreated : IEquatable<EntityCreated>
    {
        public readonly EntityRef Entity;

        public EntityCreated(EntityRef Entity)
        {
            this.Entity = Entity;
        }

        public bool Equals(EntityCreated other) => EqualityComparer<EntityRef>.Default.Equals(Entity, other.Entity);

        public override int GetHashCode() => HashCode.Combine(Entity);

        public override bool Equals(object? obj) => obj is EntityCreated other && Equals(other);

        public static bool operator ==(EntityCreated left, EntityCreated right) => left.Equals(right);
        public static bool operator !=(EntityCreated left, EntityCreated right) => !left.Equals(right);

        public void Deconstruct(out EntityRef entity)
        {
            entity = Entity;
        }
    }

    public readonly struct EntityDestroyed : IEquatable<EntityDestroyed>
    {
        public readonly EntityRef Entity;

        public EntityDestroyed(EntityRef Entity)
        {
            this.Entity = Entity;
        }

        public bool Equals(EntityDestroyed other) => EqualityComparer<EntityRef>.Default.Equals(Entity, other.Entity);

        public override int GetHashCode() => HashCode.Combine(Entity);

        public override bool Equals(object? obj) => obj is EntityDestroyed other && Equals(other);

        public static bool operator ==(EntityDestroyed left, EntityDestroyed right) => left.Equals(right);
        public static bool operator !=(EntityDestroyed left, EntityDestroyed right) => !left.Equals(right);

        public void Deconstruct(out EntityRef entity)
        {
            entity = Entity;
        }
    }

    public readonly struct ComponentAdded<TComponent> where TComponent : struct, IComponent
    {
        public readonly EntityRef Entity;

        public ComponentAdded(EntityRef Entity)
        {
            this.Entity = Entity;
        }

        public bool Equals(ComponentAdded<TComponent> other) => EqualityComparer<EntityRef>.Default.Equals(Entity, other.Entity);

        public override int GetHashCode() => HashCode.Combine(Entity);

        public override bool Equals(object? obj) => obj is ComponentAdded<TComponent> other && Equals(other);

        public void Deconstruct(out EntityRef entity)
        {
            entity = Entity;
        }
    }

    public readonly struct ComponentUpdated<TComponent> where TComponent : struct, IComponent
    {
        public readonly EntityRef Entity;

        public ComponentUpdated(EntityRef Entity)
        {
            this.Entity = Entity;
        }

        public bool Equals(ComponentUpdated<TComponent> other) => EqualityComparer<EntityRef>.Default.Equals(Entity, other.Entity);

        public override int GetHashCode() => HashCode.Combine(Entity);

        public override bool Equals(object? obj) => obj is ComponentUpdated<TComponent> other && Equals(other);

        public void Deconstruct(out EntityRef entity)
        {
            entity = Entity;
        }
    }

    public readonly struct ComponentRemoved<TComponent> where TComponent : struct, IComponent
    {
        public readonly EntityRef Entity;

        public ComponentRemoved(EntityRef Entity)
        {
            this.Entity = Entity;
        }

        public bool Equals(ComponentRemoved<TComponent> other) => EqualityComparer<EntityRef>.Default.Equals(Entity, other.Entity);

        public override int GetHashCode() => HashCode.Combine(Entity);

        public override bool Equals(object? obj) => obj is ComponentRemoved<TComponent> other && Equals(other);

        public void Deconstruct(out EntityRef entity)
        {
            entity = Entity;
        }
    }

}
