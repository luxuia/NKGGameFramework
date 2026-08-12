using System.Globalization;

namespace NKGGameFramework.Mathematics;

// Engine-agnostic double-precision 3D vector.
public readonly record struct Vector3(double X, double Y, double Z)
{
    public static readonly Vector3 Zero = default;
    public static readonly Vector3 One = new(1, 1, 1);
    public static readonly Vector3 UnitX = new(1, 0, 0);
    public static readonly Vector3 UnitY = new(0, 1, 0);
    public static readonly Vector3 UnitZ = new(0, 0, 1);

    public double SqrMagnitude => X * X + Y * Y + Z * Z;

    public double Magnitude => Math.Sqrt(SqrMagnitude);

    public Vector3 Normalized
    {
        get
        {
            var magnitude = Magnitude;
            return magnitude == 0 ? Zero : new Vector3(X / magnitude, Y / magnitude, Z / magnitude);
        }
    }

    public static Vector3 operator +(Vector3 left, Vector3 right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Vector3 operator -(Vector3 left, Vector3 right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public static Vector3 operator -(Vector3 value) => new(-value.X, -value.Y, -value.Z);

    public static Vector3 operator *(Vector3 value, double scalar) => new(value.X * scalar, value.Y * scalar, value.Z * scalar);

    public static Vector3 operator *(double scalar, Vector3 value) => value * scalar;

    public static Vector3 operator /(Vector3 value, double scalar) => new(value.X / scalar, value.Y / scalar, value.Z / scalar);

    public static double Dot(Vector3 left, Vector3 right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    public static Vector3 Cross(Vector3 left, Vector3 right) => new(
        left.Y * right.Z - left.Z * right.Y,
        left.Z * right.X - left.X * right.Z,
        left.X * right.Y - left.Y * right.X);

    public static double Distance(Vector3 left, Vector3 right) => (left - right).Magnitude;

    public static double DistanceSquared(Vector3 left, Vector3 right) => (left - right).SqrMagnitude;

    public static Vector3 Lerp(Vector3 start, Vector3 end, double t) => start + (end - start) * t;

    public Vector3 ClampMagnitude(double maxMagnitude)
    {
        if (maxMagnitude < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMagnitude), "Max magnitude cannot be negative.");
        }

        return SqrMagnitude <= maxMagnitude * maxMagnitude ? this : Normalized * maxMagnitude;
    }

    public Vector3 Clamp(Vector3 min, Vector3 max) => new(
        Math.Clamp(X, min.X, max.X),
        Math.Clamp(Y, min.Y, max.Y),
        Math.Clamp(Z, min.Z, max.Z));

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X}, {Y}, {Z})");
}
