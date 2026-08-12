using NKGGameFramework.Ecs;

namespace NKGGameFramework.Ecs.Tests;

public sealed class EntityOrganizationTests
{
    [Fact]
    public void Template_instantiates_entity_with_components()
    {
        var scene = new Scene("template");
        var template = new EntityTemplate("soldier")
            .Add(new Position(1, 2))
            .Add(new Health(10));

        var entity = template.Instantiate(scene);

        Assert.Equal(1, entity.Get<Position>().X);
        Assert.Equal(2, entity.Get<Position>().Y);
        Assert.Equal(10, entity.Get<Health>().Value);
    }

    [Fact]
    public void Template_apply_to_existing_entity()
    {
        var scene = new Scene("template");
        var entity = scene.CreateEntity();
        var template = new EntityTemplate("tag")
            .Add(new Position(5, 6));

        template.ApplyTo(entity);

        Assert.Equal(5, entity.Get<Position>().X);
    }

    [Fact]
    public void Template_children_are_linked_to_parent()
    {
        var scene = new Scene("template");
        var childTemplate = new EntityTemplate("child").Add(new Health(1));
        var parentTemplate = new EntityTemplate("parent").AddChild(childTemplate);

        var parent = parentTemplate.Instantiate(scene);

        var children = new List<Entity>();
        RelationshipUtility.ForEachLinked(scene, RelationshipKinds.Parent, parent, children.Add);

        Assert.Single(children);
        Assert.Equal(1, children[0].Get<Health>().Value);
        Assert.True(RelationshipUtility.TryGetParent(children[0], out var resolvedParent));
        Assert.Equal(parent, resolvedParent);
    }

    [Fact]
    public void Owner_link_resolves_and_does_not_resolve_reused_entity()
    {
        var scene = new Scene("owner");
        var owner = scene.CreateEntity();
        var minion = scene.CreateEntity();

        RelationshipUtility.SetOwner(minion, owner);

        Assert.True(RelationshipUtility.TryGetOwner(minion, out var resolved));
        Assert.Equal(owner, resolved);

        owner.Destroy();
        scene.CreateEntity();

        Assert.False(RelationshipUtility.TryGetOwner(minion, out _));
    }

    [Fact]
    public void Unlink_removes_relationship()
    {
        var scene = new Scene("unlink");
        var owner = scene.CreateEntity();
        var minion = scene.CreateEntity();

        RelationshipUtility.SetOwner(minion, owner);
        Assert.True(RelationshipUtility.TryGetOwner(minion, out _));

        RelationshipUtility.Unlink(minion, RelationshipKinds.Owner);
        Assert.False(RelationshipUtility.TryGetOwner(minion, out _));
    }

    [Fact]
    public void Aspect_provides_named_ref_access()
    {
        var scene = new Scene("aspect");
        var entity = scene.CreateEntity()
            .Add(new Position(1, 2))
            .Add(new Velocity(3, 4));

        var aspect = scene.GetAspect<Position, Velocity>(entity);
        aspect.First.X = 10;
        aspect.Second.Y = 40;

        Assert.Equal(10, entity.Get<Position>().X);
        Assert.Equal(2, entity.Get<Position>().Y);
        Assert.Equal(40, entity.Get<Velocity>().Y);
    }

    [Fact]
    public void TryGetAspect_fails_when_component_missing()
    {
        var scene = new Scene("aspect");
        var entity = scene.CreateEntity().Add(new Position(0, 0));

        Assert.False(scene.TryGetAspect<Position, Velocity>(entity, out _));
        Assert.False(scene.TryGetAspect<Position, Health>(entity, out _));
    }

    [Fact]
    public void Three_component_query_returns_only_matching_entities()
    {
        var scene = new Scene("three");
        var match = scene.CreateEntity()
            .Add(new Position(0, 0))
            .Add(new Velocity(0, 0))
            .Add(new Health(10));
        scene.CreateEntity()
            .Add(new Position(0, 0))
            .Add(new Velocity(0, 0));
        scene.CreateEntity()
            .Add(new Position(0, 0))
            .Add(new Health(10));

        var seen = new List<EntityId>();
        scene.Query<Position, Velocity, Health>().ForEach((ref Position _, ref Velocity _, ref Health _, Entity entity) =>
        {
            seen.Add(entity.Id);
        });

        Assert.Single(seen);
        Assert.Equal(match.Id, seen[0]);
    }

    [Fact]
    public void Three_component_query_system_mutates_components()
    {
        var scene = new Scene("move");
        scene.Systems.Add(new MoveAndDecaySystem());

        var entity = scene.CreateEntity()
            .Add(new Position(0, 0))
            .Add(new Velocity(2, 3))
            .Add(new Health(10));

        scene.Update(1, 1);

        Assert.Equal(2, entity.Get<Position>().X);
        Assert.Equal(3, entity.Get<Position>().Y);
        Assert.Equal(9, entity.Get<Health>().Value);
    }

    private sealed class MoveAndDecaySystem : QuerySystem<Position, Velocity, Health>
    {
        protected override void OnUpdate(EntityQuery<Position, Velocity, Health> query, in SystemUpdateContext context)
        {
            var deltaTime = context.DeltaTime;
            query.ForEach((ref Position position, ref Velocity velocity, ref Health health, Entity _) =>
            {
                position.X += velocity.X * deltaTime;
                position.Y += velocity.Y * deltaTime;
                health.Value -= deltaTime;
            });
        }
    }

    private struct Position(double x, double y) : IComponent
    {
        public double X { get; set; } = x;

        public double Y { get; set; } = y;
    }

    private struct Velocity(double x, double y) : IComponent
    {
        public double X { get; set; } = x;

        public double Y { get; set; } = y;
    }

    private struct Health(double value) : IComponent
    {
        public double Value { get; set; } = value;
    }
}
