using System.Globalization;

namespace NKGGameFramework.Mathematics;

// Engine-agnostic double-precision 2D vector. Uses double to match the
// framework's existing DeltaSeconds / sample Position-Velocity convention and
// to keep lockstep arithmetic independent of single-precision engine rounding.
public readonly record struct Vector2(double X, double Y)
{
    public static readonly Vector2 Zero = default;
    public static readonly Vector2 One = new(1, 1);
    public static readonly Vector2 UnitX = new(1, 0);
    public static readonly Vector2 UnitY = new(0, 1);

    public double SqrMagnitude => X * X + Y * Y;

    public double Magnitude => Math.Sqrt(SqrMagnitude);

    public Vector2 Normalized
    {
        get
        {
            var magnitude = Magnitude;
            return magnitude == 0 ? Zero : new Vector2(X / magnitude, Y / magnitude);
        }
    }

    public static Vector2 operator +(Vector2 left, Vector2 right) => new(left.X + right.X, left.Y + right.Y);

    public static Vector2 operator -(Vector2 left, Vector2 right) => new(left.X - right.X, left.Y - right.Y);

    public static Vector2 operator -(Vector2 value) => new(-value.X, -value.Y);

    public static Vector2 operator *(Vector2 value, double scalar) => new(value.X * scalar, value.Y * scalar);

    public static Vector2 operator *(double scalar, Vector2 value) => value * scalar;

    public static Vector2 operator /(Vector2 value, double scalar) => new(value.X / scalar, value.Y / scalar);

    public static double Dot(Vector2 left, Vector2 right) => left.X * right.X + left.Y * right.Y;

    public static double Cross(Vector2 left, Vector2 right) => left.X * right.Y - left.Y * right.X;

    public static double Distance(Vector2 left, Vector2 right) => (left - right).Magnitude;

    public static double DistanceSquared(Vector2 left, Vector2 right) => (left - right).SqrMagnitude;

    public static Vector2 Lerp(Vector2 start, Vector2 end, double t) => start + (end - start) * t;

    public Vector2 ClampMagnitude(double maxMagnitude)
    {
        if (maxMagnitude < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMagnitude), "Max magnitude cannot be negative.");
        }

        return SqrMagnitude <= maxMagnitude * maxMagnitude ? this : Normalized * maxMagnitude;
    }

    public Vector2 Clamp(Vector2 min, Vector2 max) => new(
        Math.Clamp(X, min.X, max.X),
        Math.Clamp(Y, min.Y, max.Y));

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X}, {Y})");
}
