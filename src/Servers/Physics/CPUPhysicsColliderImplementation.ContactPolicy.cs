using Box2D.NET;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

internal sealed partial class CPUPhysicsColliderImplementation
{
    // CPU queries and pre-solve policy read the shared identity and priority directly.
    internal override void RefreshIdentity() { }
    internal override void SetCollisionPriority(float value) { }
    internal override PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current) => current;
    internal override void InvalidateCollisionExceptions() => Space.BackendImplementation.InvalidateCollisionExceptions(Owner);
    internal void RefreshCollisionExceptions()
    {
        var world = _world!;
        var excepted = PhysicsServer.Service.HasBodyCollisionExceptions(Owner.RID);
        foreach (var id in _shapes)
        {
            var shape = b2GetShape(world, id);
            shape.enablePreSolveEvents = shape.sensorIndex < 0 &&
                (excepted || shape.userData.GetRef<PhysicsFixtureTag>()?.OneWay is not null);
        }
        // Existing contacts cache this flag independently from their shapes.
        for (var key = _body!.headContactKey; key >= 0;)
        {
            var contact = world.contacts.data[key >> 1];
            var enabled = world.shapes.data[contact.shapeIdA].enablePreSolveEvents || world.shapes.data[contact.shapeIdB].enablePreSolveEvents;
            var sim = B2Contacts.b2GetContactSim(world, contact);
            if (enabled) sim.simFlags |= (uint)B2ContactSimFlags.b2_simEnablePreSolveEvents;
            else sim.simFlags &= ~(uint)B2ContactSimFlags.b2_simEnablePreSolveEvents;
            key = contact.edges[key & 1].nextKey;
        }
    }
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
