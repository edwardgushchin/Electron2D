using static Box2D.NET.B2Shapes;

namespace Electron2D;

internal sealed partial class CPUPhysicsColliderImplementation
{
    // CPU queries and pre-solve policy read the shared identity and priority directly.
    internal override void RefreshIdentity() { }
    internal override void SetCollisionPriority(float value) { }
    private long _shapePolicyEpoch = -1;
    internal override void PrepareContactPolicy()
    {
        if (Space is null || _shapePolicyEpoch == Shape.SolverPolicyEpoch) return;
        var changed = false;
        foreach (var id in _shapes)
        {
            var shape = b2GetShape(_world!, id);
            if (shape.userData.GetRef<PhysicsFixtureTag>()?.Source is not { } weak || !weak.TryGetTarget(out var resource) || resource.IsDisposed) continue;
            var value = resource.CustomSolverBias;
            if (shape.customSolverBias == value) continue;
            shape.customSolverBias = value; changed = true;
        }
        if (changed) { SetAwake(true); WakeTouching(); }
        _shapePolicyEpoch = Shape.SolverPolicyEpoch;
    }
}
