using NKGGameFramework.Ecs;
using NKGGameFramework.Gameplay;

namespace NKGGameFramework.Tests.Gameplay;

public sealed class AttributeSystemTests
{
    private static readonly GameplayAttribute Attack = GameplayAttribute.From("Attack");

    [Fact]
    public void Base_value_is_returned_without_modifiers()
    {
        var set = new AttributeSetComponent();
        set.AddAttribute(Attack, 100);

        Assert.Equal(100, set.GetValue(Attack));
    }

    [Fact]
    public void Add_modifier_increases_value()
    {
        var set = new AttributeSetComponent();
        set.AddAttribute(Attack, 100);
        set.ApplyModifier(new AttributeModifier { Attribute = Attack, Operation = AttributeModifierOperation.Add, Magnitude = 25 });

        Assert.Equal(125, set.GetValue(Attack));
    }

    [Fact]
    public void Multiply_modifier_scales_value()
    {
        var set = new AttributeSetComponent();
        set.AddAttribute(Attack, 100);
        set.ApplyModifier(new AttributeModifier { Attribute = Attack, Operation = AttributeModifierOperation.Multiply, Magnitude = 1.5 });

        Assert.Equal(150, set.GetValue(Attack));
    }

    [Fact]
    public void Override_modifier_replaces_value_and_ignores_base()
    {
        var set = new AttributeSetComponent();
        set.AddAttribute(Attack, 100);
        set.ApplyModifier(new AttributeModifier { Attribute = Attack, Operation = AttributeModifierOperation.Add, Magnitude = 25 });
        set.ApplyModifier(new AttributeModifier { Attribute = Attack, Operation = AttributeModifierOperation.Override, Magnitude = 10 });

        Assert.Equal(10, set.GetValue(Attack));
    }

    [Fact]
    public void Value_is_clamped_to_min_and_max()
    {
        var set = new AttributeSetComponent();
        set.AddAttribute(Attack, 100, minValue: 0, maxValue: 200);
        set.ApplyModifier(new AttributeModifier { Attribute = Attack, Operation = AttributeModifierOperation.Add, Magnitude = 500 });

        Assert.Equal(200, set.GetValue(Attack));
    }

    [Fact]
    public void Removing_modifiers_by_source_restores_base()
    {
        var set = new AttributeSetComponent();
        set.AddAttribute(Attack, 100);

        var source = new object();
        set.ApplyModifier(new AttributeModifier { Attribute = Attack, Operation = AttributeModifierOperation.Add, Magnitude = 30, Source = source });
        set.ApplyModifier(new AttributeModifier { Attribute = Attack, Operation = AttributeModifierOperation.Add, Magnitude = 40, Source = source });

        Assert.Equal(170, set.GetValue(Attack));

        Assert.True(set.RemoveModifiers(source));
        Assert.Equal(100, set.GetValue(Attack));
    }

    [Fact]
    public void Attribute_registry_validates_registered_names()
    {
        var registry = new GameplayAttributeRegistry();
        registry.Register("Attack");

        Assert.True(registry.Contains("Attack"));
        Assert.True(registry.TryGet("Attack", out var attribute));
        Assert.Equal("Attack", attribute.Name);
        Assert.False(registry.Contains("Defense"));
    }

    [Fact]
    public void Buff_attribute_modifier_is_applied_and_removed_with_buff_lifecycle()
    {
        using var scene = new Scene("battle");
        scene.Systems.Add(new BuffUpdateSystem());

        var target = scene.CreateEntity();
        target.Add(new AttributeSetComponent());
        target.Get<AttributeSetComponent>().AddAttribute(Attack, 100);

        var buff = new BuffDefinition
        {
            Id = "war_cry",
            TargetKind = BuffTargetKind.Self,
            EffectKey = BuffEffectKeys.AttributeModifier,
            Duration = TimeSpan.FromSeconds(1),
            AttributeModifiers =
            [
                new AttributeModifierSpec
                {
                    Attribute = Attack,
                    Operation = AttributeModifierOperation.Add,
                    Magnitude = 25,
                },
            ],
        };

        BuffManager.Apply(target, target, buff);

        // Waiting: not yet applied.
        Assert.Equal(100, target.Get<AttributeSetComponent>().GetValue(Attack));

        scene.Update(0, 0);

        // Applied on the first tick.
        Assert.Equal(125, target.Get<AttributeSetComponent>().GetValue(Attack));

        scene.Update(1, 1);

        // Expired and removed.
        Assert.Equal(100, target.Get<AttributeSetComponent>().GetValue(Attack));
    }
}
