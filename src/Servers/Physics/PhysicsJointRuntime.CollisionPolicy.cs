namespace Electron2D;

internal sealed partial class PhysicsJointRuntime
{
    private bool _collisionContribution, _collisionPolicyDirty, _preparingCollisionPolicy;

    private void SetCollisionPolicyDirty(bool value)
    {
        if (_collisionPolicyDirty == value) return;
        _collisionPolicyDirty = value;
        Space!.ChangePendingJointCollisionPolicies(value ? 1 : -1);
    }

    private void UpdateCollisionContribution(bool value)
    {
        if (_collisionContribution == value) return;
        // The common edge is committed before selected invalidation can throw.
        _collisionContribution = value;
        PhysicsServer.Service.JointCollisionContribution(BodyA, BodyB, value);
    }

    internal void PrepareCollisionPolicy()
    {
        if (!_collisionPolicyDirty) return;
        if (_preparingCollisionPolicy) throw new InvalidOperationException("Joint collision policy preparation cannot reenter its own publication.");
        EnsureWriteAccess();
        _preparingCollisionPolicy = true;
        try { Backend.SetCollideConnected(!DisableCollision); SetCollisionPolicyDirty(false); }
        finally { _preparingCollisionPolicy = false; }
    }
}
