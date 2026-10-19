using Box2D.NET;

namespace Electron2D;

internal readonly record struct SeparationRayData(B2Vec2 From, B2Vec2 To, bool SlideOnSlope);
internal sealed record PhysicsFixtureTag(RID ColliderRID, int ShapeIndex, OneWayContactData? OneWay,
    SeparationRayData? SeparationRay = null)
{
    public RID ColliderRID { get; set; } = ColliderRID;
    public int ShapeIndex { get; set; } = ShapeIndex;
    public OneWayContactData? OneWay { get; set; } = OneWay;
    public SeparationRayData? SeparationRay { get; set; } = SeparationRay;
    internal sealed record CompoundContour(WeakReference<Shape> Source, Transform LocalPose)
    {
        public Transform LocalPose { get; set; } = LocalPose;
        internal ReadOnlySpan<Vector2> Points => Source.TryGetTarget(out var shape) && !shape.IsDisposed
            ? shape.GetGeometry().Points : throw new ObjectDisposedException(nameof(Shape));
    }
    internal CompoundContour? Compound { get; set; }
    internal CompoundContour? CachedCompound;
    internal int CompoundPiece { get; set; }
    internal PhysicsShapeGeometry.ShapeKind Kind { get; set; }
    internal WeakReference<Shape>? Source { get; set; }
    internal WeakReference<CollisionObject>? SceneOwner { get; set; }
    internal PhysicsColliderBackend? Owner { get; set; }
    internal ObjectIdentity ObjectIdentity => Owner?.ObjectIdentity ?? default;
    internal CollisionObject? SceneObject => SceneOwner is { } weak && weak.TryGetTarget(out var node) && !node.IsDisposed ? node : null;
}
