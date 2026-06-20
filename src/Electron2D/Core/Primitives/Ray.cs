using System.Numerics;

namespace Electron2D;

/// <summary>
/// Representation of rays.
/// A ray is an infinite line starting at origin and going in direction.
/// </summary>
public struct Ray
{
    /// <summary>The origin point of the ray.</summary>
    public Vector2 Origin;

    /// <summary>Direction of the ray.</summary>
    public Vector2 Direction;

    /// <summary>Creates a ray starting at origin along direction.</summary>
    public Ray(Vector2 origin, Vector2 direction)
    {
        this.Origin = origin;
        this.Direction = direction;
    }

    /// <summary>Returns a point at distance units along the ray.</summary>
    public readonly Vector2 GetPoint(float distance) => Origin + Direction * distance;

    /// <summary>Returns a nicely formatted string for the ray.</summary>
    public readonly override string ToString()
        => $"Origin: ({Origin.X:0.###}, {Origin.Y:0.###}), Dir: ({Direction.X:0.###}, {Direction.Y:0.###})";
}