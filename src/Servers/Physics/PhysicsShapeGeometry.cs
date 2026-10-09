namespace Electron2D;

/// <summary>Borrowed local geometry in scene units, independent of solver handles and storage.</summary>
internal readonly ref struct PhysicsShapeGeometry
{
    internal enum ShapeKind { Circle, Capsule, Segment, Rectangle, ConvexPolygon, ConcavePolygon, SeparationRay, WorldBoundary }

    internal required ShapeKind Kind { get; init; }
    // Endpoints for segments/capsules/rays, minimum and maximum corners for a rectangle.
    internal Vector2 A { get; init; }
    internal Vector2 B { get; init; }
    internal float Radius { get; init; }
    internal ReadOnlySpan<Vector2> Points { get; init; }
    internal bool SlideOnSlope { get; init; }
}
