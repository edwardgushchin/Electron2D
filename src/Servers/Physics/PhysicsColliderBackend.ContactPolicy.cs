using static Box2D.NET.B2Shapes;

namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    private long _shapePolicyEpoch = -1;
    internal void PrepareContactPolicy()
    {
        if (GPU is not null) return; // The resident store observes shared shape policy revisions.
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
