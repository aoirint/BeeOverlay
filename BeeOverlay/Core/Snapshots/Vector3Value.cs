#nullable enable

namespace BeeOverlay.Core.Snapshots;

/// <summary>
/// Framework-free three-dimensional value crossing the Core boundary.
/// </summary>
internal readonly struct Vector3Value
{
    public float X { get; }

    public float Y { get; }

    public float Z { get; }

    public Vector3Value(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float DistanceTo(Vector3Value other)
    {
        var x = X - other.X;
        var y = Y - other.Y;
        var z = Z - other.Z;
        return (float)System.Math.Sqrt(x * x + y * y + z * z);
    }
}
