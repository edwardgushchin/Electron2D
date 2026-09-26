using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed class PhysicsBodyRuntime(RID rid)
{
    internal RID RID { get; } = rid;
    internal Vector2 ConstantForce;
    internal float ConstantTorque;
    internal bool OmitForces;
    internal int MaxContacts;
    internal Vector2 Gravity;
    internal float LinearDamp;
    internal float AngularDamp;
    internal Action<PhysicsDirectBodyState>? ForceCallback;
    internal Action<PhysicsDirectBodyState>? SyncCallback;
    internal PhysicsDirectBodyState? View;
    internal bool ActiveBeforeStep;
    internal bool FieldsInitialized;

    internal (PhysicsBody? Scene, PhysicsServerCollider? Server) Owners => PhysicsServer.Instance.ResolveBodyOwners(RID);
    internal PhysicsSpace? Space { get { var owner = Owners; return owner.Scene?.Space ?? owner.Server?.Space; } }
    internal B2BodyId BodyID { get { var owner = Owners; return owner.Scene?.BackendID ?? owner.Server!.BackendID; } }
    internal bool Omitted { get => Owners.Scene is RigidBody rigid ? rigid.CustomIntegrator : OmitForces; }
    internal int ContactLimit => Owners.Scene is RigidBody rigid ? rigid.MaxContactsReported : MaxContacts;

    internal PhysicsDirectBodyState GetView(PhysicsSpace space, B2BodyId id)
    {
        if (View is null || View.IsDisposed || !View.Matches(space, id)) View = new(this, space, id);
        return View;
    }

    internal void EnsureMutable() => Space?.EnsureQueryAccess();

    internal void ApplyBeforeStep()
    {
        var id = BodyID;
        ActiveBeforeStep = b2Body_GetType(id) != B2BodyType.b2_staticBody && b2Body_IsAwake(id);
        if (Omitted)
        {
            b2Body_SetGravityScale(id, 0);
            var sim = Simulation(id);
            sim.force = default; sim.torque = 0;
            return;
        }
        if (Owners.Scene is RigidBody rigid) { rigid.ApplyConstantForces(); return; }
        b2Body_SetGravityScale(id, 1);
        if (ConstantForce != Vector2.Zero) b2Body_ApplyForceToCenter(id, Shape.ToBackend(ConstantForce), false);
        if (ConstantTorque != 0) b2Body_ApplyTorque(id, ConstantTorque * 0.0001f, false);
    }

    internal static B2BodySim Simulation(B2BodyId id)
    {
        var world = b2GetWorldFromId(b2Body_GetWorld(id));
        return b2GetBodySim(world, b2GetBodyFullId(world, id));
    }
}
