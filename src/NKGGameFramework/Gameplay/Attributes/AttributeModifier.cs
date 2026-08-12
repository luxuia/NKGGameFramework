namespace NKGGameFramework.Gameplay;

public enum AttributeModifierOperation
{
    Add,
    Multiply,
    Override,
}

// A single modifier applied to an attribute. `Source` tags the grantor (e.g. a
// BuffInstance); removing all modifiers for a source undoes its contribution.
public sealed class AttributeModifier
{
    public required GameplayAttribute Attribute { get; init; }

    public AttributeModifierOperation Operation { get; init; } = AttributeModifierOperation.Add;

    public double Magnitude { get; init; }

    public object? Source { get; init; }

    public override string ToString() => $"{Operation} {Magnitude} on {Attribute}";
}

// Declares a modifier that a buff grants, with optional per-level magnitude.
public sealed class AttributeModifierSpec
{
    public required GameplayAttribute Attribute { get; init; }

    public AttributeModifierOperation Operation { get; init; } = AttributeModifierOperation.Add;

    public Dictionary<int, double> ValuesByLevel { get; init; } = [];

    public double Magnitude { get; init; }

    public double GetMagnitude(int level)
    {
        if (ValuesByLevel.Count == 0)
        {
            return Magnitude;
        }

        if (ValuesByLevel.TryGetValue(level, out var exact))
        {
            return exact;
        }

        var bestLevel = int.MinValue;
        var bestValue = Magnitude;
        foreach (var (configuredLevel, value) in ValuesByLevel)
        {
            if (configuredLevel <= level && configuredLevel > bestLevel)
            {
                bestLevel = configuredLevel;
                bestValue = value;
            }
        }

        return bestLevel == int.MinValue ? Magnitude : bestValue;
    }
}
