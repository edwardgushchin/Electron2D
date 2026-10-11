namespace Electron2D;

public sealed partial class PhysicsServer
{
    [ThreadStatic] private static PhysicsShapePairBuffers? s_detachedShapePairs;
    internal (RID Shape, Transform Pose) GetBodyShapePair(RID body, int index)
    {
        var owners = ShapeOwners(body, isArea: false);
        var shape = GetSlotShape(body, index, isArea: false);
        var local = GetSlotTransform(body, index, isArea: false);
        return (shape, (owners.Scene is { } scene ? scene.GlobalTransform : owners.Server!.GetTransform()) * local);
    }
    internal bool BodyCollideShapeCore(RID body, int index, RID shape, Transform pose, Vector2 motion,
        Span<Vector2> contacts, out int count)
    {
        ThrowIfDisposed(); PhysicsShapeCollision.Validate(pose, motion);
        PhysicsShapePairBuffers.ValidateCapacity(contacts.Length);
        var owners = ResolveBodyOwners(body);
        var space = owners.Scene?.Space ?? owners.Server?.Space;
        if (space is not null) return space.BodyCollideShape(body, index, shape, pose, motion, contacts, out count);
        var pair = GetBodyShapePair(body, index);
        var geometry = GetShapeGeometry(shape);
        var runtime = BodyRuntime(body);
        var frames = s_detachedShapePairs ??= new(); var scratch = frames.Begin(contacts.Length);
        runtime.BeginExtension();
        try
        {
            var hit = PhysicsShapeCollision.Evaluate(GetShapeGeometry(pair.Shape), pair.Pose, Vector2.Zero, geometry, pose, motion, scratch, out var pairCount);
            PhysicsShapePairBuffers.Commit(hit, pairCount, scratch, contacts); count = pairCount; return hit;
        }
        finally { runtime.EndExtension(); frames.End(); }
    }
}
