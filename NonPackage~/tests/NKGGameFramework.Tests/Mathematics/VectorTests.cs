using NKGGameFramework.Mathematics;

namespace NKGGameFramework.Tests.Mathematics;

public sealed class Vector2Tests
{
    [Fact]
    public void Add_subtract_and_scale_apply_componentwise()
    {
        var left = new Vector2(1, 2);
        var right = new Vector2(3, 4);

        Assert.Equal(new Vector2(4, 6), left + right);
        Assert.Equal(new Vector2(-2, -2), left - right);
        Assert.Equal(new Vector2(-1, -2), -left);
        Assert.Equal(new Vector2(3, 6), left * 3);
        Assert.Equal(new Vector2(3, 6), 3 * left);
        Assert.Equal(new Vector2(0.5, 1), left / 2);
    }

    [Fact]
    public void Dot_and_cross_return_scalars()
    {
        var left = new Vector2(1, 2);
        var right = new Vector2(3, 4);

        Assert.Equal(11, Vector2.Dot(left, right));
        Assert.Equal(-2, Vector2.Cross(left, right));
    }

    [Fact]
    public void Magnitude_and_normalized_are_consistent()
    {
        var vector = new Vector2(3, 4);

        Assert.Equal(5, vector.Magnitude, 6);
        Assert.Equal(25, vector.SqrMagnitude);

        var normalized = vector.Normalized;
        Assert.Equal(1, normalized.Magnitude, 6);
        Assert.Equal(new Vector2(0.6, 0.8), normalized);
    }

    [Fact]
    public void Normalized_zero_vector_returns_zero()
    {
        Assert.Equal(Vector2.Zero, Vector2.Zero.Normalized);
    }

    [Fact]
    public void Lerp_interpolates_between_endpoints()
    {
        Assert.Equal(new Vector2(1, 2), Vector2.Lerp(new Vector2(1, 2), new Vector2(5, 10), 0));
        Assert.Equal(new Vector2(5, 10), Vector2.Lerp(new Vector2(1, 2), new Vector2(5, 10), 1));
        Assert.Equal(new Vector2(3, 6), Vector2.Lerp(new Vector2(1, 2), new Vector2(5, 10), 0.5));
    }

    [Fact]
    public void Clamp_magnitude_caps_length_without_changing_direction()
    {
        var vector = new Vector2(3, 4);

        var clamped = vector.ClampMagnitude(2);

        Assert.Equal(2, clamped.Magnitude, 6);
        Assert.Equal(vector.Normalized, clamped.Normalized);
    }
}

public sealed class Vector3Tests
{
    [Fact]
    public void Cross_returns_perpendicular_vector()
    {
        var x = Vector3.UnitX;
        var y = Vector3.UnitY;

        Assert.Equal(Vector3.UnitZ, Vector3.Cross(x, y));
        Assert.Equal(0, Vector3.Dot(Vector3.Cross(x, y), x), 6);
        Assert.Equal(0, Vector3.Dot(Vector3.Cross(x, y), y), 6);
    }

    [Fact]
    public void Distance_matches_magnitude_of_difference()
    {
        var from = new Vector3(1, 1, 1);
        var to = new Vector3(4, 5, 1);

        Assert.Equal(5, Vector3.Distance(from, to), 6);
        Assert.Equal(25, Vector3.DistanceSquared(from, to));
    }
}
