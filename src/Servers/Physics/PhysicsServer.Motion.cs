namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal bool BodyTestMotionCore(RID body, PhysicsTestMotionParameters parameters,
    PhysicsTestMotionResult? result = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parameters);
        if (result?.IsDisposed == true) throw new ObjectDisposedException(nameof(result));
        var data = TestMotionData(body, parameters.From, parameters.Motion, parameters.Margin,
            parameters.RecoveryAsCollision, parameters.ExcludedBodies, parameters.ExcludedObjects,
            parameters.CollideSeparationRay);
        result?.Set(data);
        return data.Collided;
    }

    internal MotionResultData TestMotionData(RID body, Transform from, Vector2 motion,
        float margin, bool recoveryAsCollision, RID[] excludedBodies, ulong[] excludedObjects,
        bool collideSeparationRay = false)
    {
        ThrowIfDisposed();
        var owners = ResolveBodyOwners(body);
        var space = owners.Scene?.Space ?? owners.Server?.Space;
        if (space is null) throw new InvalidOperationException("A body motion test requires a registered physics space.");
        return space.TestBodyMotion(body, from, motion, margin, recoveryAsCollision,
            excludedBodies, excludedObjects, collideSeparationRay);
    }
}
