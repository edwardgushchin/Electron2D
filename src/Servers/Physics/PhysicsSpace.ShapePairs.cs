namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private readonly PhysicsShapePairBuffers _shapePairs = new();
    internal bool CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second,
        Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)
    {
        EnsureQueryAccess(); PhysicsShapePairBuffers.ValidateCapacity(contacts.Length); PhysicsShapeCollision.Validate(firstPose, firstMotion); PhysicsShapeCollision.Validate(secondPose, secondMotion);
        BeginExtensionCallback();
        try
        {
            _ = PhysicsServer.Service.GetShapeGeometry(first).GetGeometryData();
            _ = PhysicsServer.Service.GetShapeGeometry(second).GetGeometryData();
            var scratch = _shapePairs.Begin(contacts.Length);
            try
            {
                var hit = _backend.CollideShapes(first, firstPose, firstMotion, second, secondPose, secondMotion, scratch, out var pairCount);
                PhysicsShapePairBuffers.Commit(hit, pairCount, scratch, contacts); count = pairCount; return hit;
            }
            finally { _shapePairs.End(); }
        }
        finally { EndExtensionCallback(); }
    }
    internal bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion,
        Span<Vector2> contacts, out int count)
    {
        EnsureQueryAccess(); PhysicsShapePairBuffers.ValidateCapacity(contacts.Length); PhysicsShapeCollision.Validate(pose, motion);
        var owners = PhysicsServer.Service.ResolveBodyOwners(body);
        var backend = owners.Scene?.Backend ?? owners.Server!.Backend;
        if (!ReferenceEquals(backend.Space, this)) throw new ArgumentException("The body belongs to another physics space.", nameof(body));
        BeginExtensionCallback();
        try
        {
            var pair = PhysicsServer.Service.GetBodyShapePair(body, index);
            _ = PhysicsServer.Service.GetShapeGeometry(pair.Shape).GetGeometryData();
            _ = PhysicsServer.Service.GetShapeGeometry(shape).GetGeometryData();
            var scratch = _shapePairs.Begin(contacts.Length);
            try
            {
                var hit = _backend.BodyCollideShape(body, index, shape, pose, motion, scratch, out var pairCount);
                PhysicsShapePairBuffers.Commit(hit, pairCount, scratch, contacts); count = pairCount; return hit;
            }
            finally { _shapePairs.End(); }
        }
        finally { EndExtensionCallback(); }
    }
}
