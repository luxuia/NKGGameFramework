using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Ecs
{

    // Reusable component recipe. Compose once at startup, instantiate many times.
    // Children are spawned alongside the parent and linked via a Parent relationship.
    public sealed class EntityTemplate
    {
        private readonly List<Action<Entity>> _componentAppliers = new();
        private readonly List<EntityTemplate> _children = new();

        public EntityTemplate(string id)
        {
            NkgThrow.IfNullOrWhiteSpace(id);
            Id = id;
        }

        public string Id { get; }

        public IReadOnlyList<EntityTemplate> Children => _children;

        public EntityTemplate Add<TComponent>(TComponent component)
            where TComponent : struct, IComponent
        {
            _componentAppliers.Add(entity => entity.Add(component));
            return this;
        }

        public EntityTemplate AddChild(EntityTemplate child)
        {
            NkgThrow.IfNull(child);
            _children.Add(child);
            return this;
        }

        public Entity Instantiate(Scene scene)
        {
            NkgThrow.IfNull(scene);

            var entity = scene.CreateEntity();
            ApplyTo(entity);

            foreach (var child in _children)
            {
                var childEntity = child.Instantiate(scene);
                RelationshipUtility.SetParent(childEntity, entity);
            }

            return entity;
        }

        public void ApplyTo(Entity entity)
        {
            foreach (var apply in _componentAppliers)
            {
                apply(entity);
            }
        }
    }
}
