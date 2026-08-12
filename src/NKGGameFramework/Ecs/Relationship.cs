using OdinSerializer;

namespace NKGGameFramework.Ecs;

public static class RelationshipKinds
{
    public const string Owner = "Owner";
    public const string Parent = "Parent";
    public const string Target = "Target";
}

public sealed class Relationship
{
    public required string Kind { get; init; }

    public EntityRef Target { get; init; }
}

// Named, version-checked links from one entity to others (Owner, Parent,
// Target, and custom kinds). Links survive entity reuse because EntityRef
// carries the target's version.
[ComponentGraph(Group = "Ecs/Relationships", Order = 20)]
public struct RelationshipComponent : IComponent
{
    [OdinSerialize]
    private List<Relationship>? _links;

    public RelationshipComponent()
    {
        _links = [];
    }

    private List<Relationship> MutableLinks => _links ??= [];

    public IReadOnlyList<Relationship> Links => MutableLinks;

    public bool TryGet(string kind, out EntityRef target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        foreach (var link in MutableLinks)
        {
            if (string.Equals(link.Kind, kind, StringComparison.Ordinal))
            {
                target = link.Target;
                return true;
            }
        }

        target = default;
        return false;
    }

    public void Set(string kind, EntityRef target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        var links = MutableLinks;
        for (var i = 0; i < links.Count; i++)
        {
            if (string.Equals(links[i].Kind, kind, StringComparison.Ordinal))
            {
                links[i] = new Relationship { Kind = kind, Target = target };
                return;
            }
        }

        links.Add(new Relationship { Kind = kind, Target = target });
    }

    public bool Remove(string kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        var links = MutableLinks;
        for (var i = 0; i < links.Count; i++)
        {
            if (string.Equals(links[i].Kind, kind, StringComparison.Ordinal))
            {
                links.RemoveAt(i);
                return true;
            }
        }

        return false;
    }
}

public static class RelationshipUtility
{
    public static void Link(Entity entity, string kind, Entity target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        EnsureComponent(entity).Set(kind, target.ToRef());
    }

    public static void Unlink(Entity entity, string kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        if (entity.Has<RelationshipComponent>())
        {
            ref var component = ref entity.Get<RelationshipComponent>();
            component.Remove(kind);
        }
    }

    public static bool TryGetTarget(Entity entity, string kind, out Entity target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        if (entity.Has<RelationshipComponent>()
            && entity.Get<RelationshipComponent>().TryGet(kind, out var reference)
            && reference.TryGet(out target))
        {
            return true;
        }

        target = default;
        return false;
    }

    public static void SetOwner(Entity entity, Entity owner) => Link(entity, RelationshipKinds.Owner, owner);

    public static bool TryGetOwner(Entity entity, out Entity owner) => TryGetTarget(entity, RelationshipKinds.Owner, out owner);

    public static void SetParent(Entity entity, Entity parent) => Link(entity, RelationshipKinds.Parent, parent);

    public static bool TryGetParent(Entity entity, out Entity parent) => TryGetTarget(entity, RelationshipKinds.Parent, out parent);

    // Reverse lookup: every entity whose `kind` link points at `target`.
    public static void ForEachLinked(Scene scene, string kind, Entity target, Action<Entity> action)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        var targetRef = target.ToRef();
        scene.Query<RelationshipComponent>().ForEach((ref RelationshipComponent component, Entity entity) =>
        {
            if (component.TryGet(kind, out var reference)
                && reference.Id == targetRef.Id
                && reference.Version == targetRef.Version)
            {
                action(entity);
            }
        });
    }

    private static ref RelationshipComponent EnsureComponent(Entity entity)
    {
        if (!entity.Has<RelationshipComponent>())
        {
            entity.Add(new RelationshipComponent());
        }

        return ref entity.Get<RelationshipComponent>();
    }
}
