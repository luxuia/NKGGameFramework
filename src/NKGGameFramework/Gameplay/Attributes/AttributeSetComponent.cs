using NKGGameFramework.Ecs;
using OdinSerializer;

namespace NKGGameFramework.Gameplay;

// Runtime state of a single attribute: base value (serialized ground truth)
// plus transient modifiers (re-applied by buffs/skills each load).
public sealed class GameplayAttributeInstance
{
    private readonly List<AttributeModifier> _modifiers = [];

    public GameplayAttributeInstance(
        GameplayAttribute attribute,
        double baseValue,
        double minValue = double.NegativeInfinity,
        double maxValue = double.PositiveInfinity)
    {
        if (!attribute.IsValid)
        {
            throw new ArgumentException("Attribute must be valid.", nameof(attribute));
        }

        if (maxValue < minValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValue), "Max value cannot be less than min value.");
        }

        Attribute = attribute;
        BaseValue = baseValue;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public GameplayAttribute Attribute { get; }

    public double BaseValue { get; set; }

    public double MinValue { get; set; }

    public double MaxValue { get; set; }

    public IReadOnlyList<AttributeModifier> Modifiers => _modifiers;

    public double CurrentValue => Evaluate();

    // Evaluation order: a later Override replaces the whole result and ignores
    // base and all other operations; otherwise base + sum(Add) then product
    // (Multiply); finally clamped to [MinValue, MaxValue].
    public double Evaluate()
    {
        // A later Override replaces the whole result and ignores base and all
        // other operations.
        for (var i = _modifiers.Count - 1; i >= 0; i--)
        {
            if (_modifiers[i].Operation == AttributeModifierOperation.Override)
            {
                return Math.Clamp(_modifiers[i].Magnitude, MinValue, MaxValue);
            }
        }

        var value = BaseValue;
        foreach (var modifier in _modifiers)
        {
            switch (modifier.Operation)
            {
                case AttributeModifierOperation.Add:
                    value += modifier.Magnitude;
                    break;
                case AttributeModifierOperation.Multiply:
                    value *= modifier.Magnitude;
                    break;
            }
        }

        return Math.Clamp(value, MinValue, MaxValue);
    }

    public void ApplyModifier(AttributeModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        _modifiers.Add(modifier);
    }

    public bool RemoveModifiers(object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var removed = false;
        for (var i = _modifiers.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_modifiers[i].Source, source))
            {
                _modifiers.RemoveAt(i);
                removed = true;
            }
        }

        return removed;
    }
}

// Per-entity attribute collection. Base values are the serialized ground truth;
// modifiers are runtime-transient and re-applied by buffs/skills.
[ComponentGraph(Group = "Gameplay/Attributes", Order = 10)]
public struct AttributeSetComponent : IComponent
{
    [OdinSerialize]
    private List<GameplayAttributeInstance>? _attributes;

    public AttributeSetComponent()
    {
        _attributes = [];
    }

    private List<GameplayAttributeInstance> MutableAttributes => _attributes ??= [];

    public IReadOnlyList<GameplayAttributeInstance> Attributes => MutableAttributes;

    public bool TryGet(GameplayAttribute attribute, out GameplayAttributeInstance instance)
    {
        foreach (var candidate in MutableAttributes)
        {
            if (candidate.Attribute == attribute)
            {
                instance = candidate;
                return true;
            }
        }

        instance = null!;
        return false;
    }

    public double GetValue(GameplayAttribute attribute, double fallback = 0)
        => TryGet(attribute, out var instance) ? instance.CurrentValue : fallback;

    public double GetBaseValue(GameplayAttribute attribute, double fallback = 0)
        => TryGet(attribute, out var instance) ? instance.BaseValue : fallback;

    public GameplayAttributeInstance AddAttribute(
        GameplayAttribute attribute,
        double baseValue,
        double minValue = double.NegativeInfinity,
        double maxValue = double.PositiveInfinity)
    {
        if (TryGet(attribute, out var existing))
        {
            existing.BaseValue = baseValue;
            existing.MinValue = minValue;
            existing.MaxValue = maxValue;
            return existing;
        }

        var instance = new GameplayAttributeInstance(attribute, baseValue, minValue, maxValue);
        MutableAttributes.Add(instance);
        return instance;
    }

    public bool SetBaseValue(GameplayAttribute attribute, double baseValue)
    {
        if (!TryGet(attribute, out var instance))
        {
            return false;
        }

        instance.BaseValue = baseValue;
        return true;
    }

    public AttributeModifier ApplyModifier(AttributeModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);

        if (!TryGet(modifier.Attribute, out var instance))
        {
            throw new KeyNotFoundException($"Attribute '{modifier.Attribute}' is not present; add it before applying modifiers.");
        }

        instance.ApplyModifier(modifier);
        return modifier;
    }

    public bool RemoveModifiers(object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var removed = false;
        foreach (var instance in MutableAttributes)
        {
            removed |= instance.RemoveModifiers(source);
        }

        return removed;
    }

    public bool RemoveModifiers(GameplayAttribute attribute, object source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return TryGet(attribute, out var instance) && instance.RemoveModifiers(source);
    }
}
