namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private bool _testingMotion;
    private RID[] _motionExcludedBodies = [];
    private ulong[] _motionExcludedObjects = [];
    internal bool IsBodyExcludedFromMotionTest(RID body)
    {
        EnsureImplementationInputReadAccess();
        return _testingMotion && _motionExcludedBodies.AsSpan().Contains(body);
    }
    internal bool IsObjectExcludedFromMotionTest(ulong objectID)
    {
        EnsureImplementationInputReadAccess();
        return _testingMotion && _motionExcludedObjects.AsSpan().Contains(objectID);
    }
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
        EnsureQueryAccess();
        if (_testingMotion) throw new InvalidOperationException("A selected motion query cannot recursively test its own space.");
        PrepareForQuery();
        BeginExtensionCallback(); _testingMotion = true;
        _motionExcludedBodies = excludedBodies; _motionExcludedObjects = excludedObjects;
        try
        {
            var data = _backend.TestBodyMotion(ownerRID, from, motion, margin, recoveryAsCollision,
                excludedBodies, excludedObjects, collideSeparationRay);
            return PhysicsServer.Service.ValidateMotionResult(this, ownerRID, data, excludedBodies, excludedObjects);
        }
        finally
        {
            _motionExcludedBodies = []; _motionExcludedObjects = [];
            _testingMotion = false; EndExtensionCallback();
        }
    }
}
