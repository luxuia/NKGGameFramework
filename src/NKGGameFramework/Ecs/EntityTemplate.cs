namespace NKGGameFramework.Ecs;

// Reusable component recipe. Compose once at startup, instantiate many times.
// Children are spawned alongside the parent and linked via a Parent relationship.
public sealed class EntityTemplate
{
    private readonly List<Action<Entity>> _componentAppliers = [];
    private readonly List<EntityTemplate> _children = [];

    public EntityTemplate(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
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
        ArgumentNullException.ThrowIfNull(child);
        _children.Add(child);
        return this;
    }

    public Entity Instantiate(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

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
