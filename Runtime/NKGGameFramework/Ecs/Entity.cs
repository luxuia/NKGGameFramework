using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Ecs
{

    public readonly struct EntityId : IEquatable<EntityId>
    {
        public readonly int Value;

        public EntityId(int Value)
        {
            this.Value = Value;
        }

        public bool Equals(EntityId other) => EqualityComparer<int>.Default.Equals(Value, other.Value);

        public override int GetHashCode() => HashCode.Combine(Value);

        public override bool Equals(object? obj) => obj is EntityId other && Equals(other);

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);

        public void Deconstruct(out int value)
        {
            value = Value;
        }


        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public readonly struct Entity : IEquatable<Entity>
    {
        private readonly Scene? _scene;

        internal Entity(Scene scene, int id, int version)
        {
            _scene = scene;
            Id = new EntityId(id);
            Version = version;
        }

        public EntityId Id { get; }

        public int Version { get; }

        public bool IsAlive => _scene?.IsAlive(Id.Value, Version) == true;

        public Entity Add<TComponent>(TComponent component)
            where TComponent : struct, IComponent
        {
            Scene.SetComponent(this, component);
            return this;
        }

        public bool Remove<TComponent>()
            where TComponent : struct, IComponent
        {
            return Scene.RemoveComponent<TComponent>(this);
        }

        public bool Has<TComponent>()
            where TComponent : struct, IComponent
        {
            return Scene.HasComponent<TComponent>(this);
        }

        public ref TComponent Get<TComponent>()
            where TComponent : struct, IComponent
        {
            return ref Scene.GetComponent<TComponent>(this);
        }

        public void Destroy()
        {
            Scene.Destroy(this);
        }

        public EntityRef ToRef()
        {
            EnsureAlive();
            return new EntityRef(Scene, Id.Value, Version);
        }

        public bool Equals(Entity other)
        {
            return ReferenceEquals(_scene, other._scene) && Id.Equals(other.Id) && Version == other.Version;
        }

        public override bool Equals(object? obj)
        {
            return obj is Entity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_scene, Id, Version);
        }

        public static bool operator ==(Entity left, Entity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity left, Entity right)
        {
            return !left.Equals(right);
        }

        private Scene Scene => _scene ?? throw new ObjectDisposedException(nameof(Entity), "Entity is not attached to a scene.");

        private void EnsureAlive()
        {
            if (!IsAlive)
            {
                throw new ObjectDisposedException(nameof(Entity), $"Entity {Id.Value} is not alive.");
            }
        }
    }
}
