namespace Electron2D;

internal sealed partial class GPUPhysicsWorldBackend
{
    internal override bool CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second,
        Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)
    {
        EnsureAccess();
        return PhysicsShapeCollision.Evaluate(PhysicsServer.Service.GetShapeGeometry(first), firstPose, firstMotion,
            PhysicsServer.Service.GetShapeGeometry(second), secondPose, secondMotion, contacts, out count);
    }
    internal override bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion,
        Span<Vector2> contacts, out int count)
    {
        EnsureAccess();
        var pair = PhysicsServer.Service.GetBodyShapePair(body, index);
        return CollideShapes(pair.Shape, pair.Pose, Vector2.Zero, shape, pose, motion, contacts, out count);
    }
}
