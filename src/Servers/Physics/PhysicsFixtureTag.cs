using Box2D.NET;

namespace Electron2D;

internal readonly record struct SeparationRayData(B2Vec2 From, B2Vec2 To, bool SlideOnSlope);
internal sealed record PhysicsFixtureTag(RID ColliderRID, int ShapeIndex, OneWayContactData? OneWay,
    SeparationRayData? SeparationRay = null)
{
    internal sealed record CompoundContour(WeakReference<ConvexPolygonShape> Source, Transform LocalPose)
    {
        internal ReadOnlySpan<Vector2> Points => Source.TryGetTarget(out var shape) && !shape.IsDisposed
            ? shape.GetGeometry().Points : throw new ObjectDisposedException(nameof(ConvexPolygonShape));
    }
    internal CompoundContour? Compound { get; init; }
    internal int CompoundPiece { get; init; }
    internal WeakReference<CollisionObject>? SceneOwner { get; init; }
    internal CollisionObject? SceneObject => SceneOwner is { } weak && weak.TryGetTarget(out var node) && !node.IsDisposed ? node : null;
}
