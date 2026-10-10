namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal bool TryGetBodyPointMotion(RID rid, Vector2 point, out Vector2 velocity, out uint layer)
    {
        EnsureQueryAccess();
        if (PhysicsServer.Service.TryResolveBodyOwners(rid, out var owners))
        {
            var backend = owners.Scene?.Backend ?? owners.Server!.Backend;
            if (ReferenceEquals(backend.Space, this))
            {
                velocity = backend.GetWorldPointVelocity(point);
                layer = owners.Scene?.EffectiveCollisionLayer ?? owners.Server!.CollisionLayer;
                return true;
            }
        }
        velocity = default; layer = 0; return false;
    }

    internal MotionResultData TestBodyMotion(RID ownerRID, Transform from, Vector2 motion, float margin,
        bool recoveryAsCollision, RID[] excludedBodies, ulong[] excludedObjects, bool collideSeparationRay = false)
    {
        PrepareForQuery();
        return _backend.TestBodyMotion(ownerRID, from, motion, margin, recoveryAsCollision,
            excludedBodies, excludedObjects, collideSeparationRay);
    }
}
