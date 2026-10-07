using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime(RID rid, WeakReference<CollisionObject>? sceneOwner, PhysicsServerCollider? serverOwner)
{
    internal RID RID { get; } = rid;
    internal float Mass = 1;
    internal float Inertia;
    internal Vector2? CustomCenter;
    internal readonly List<B2ShapeProxy> MassProxies = [];
    internal B2MassData MassData = new(1, default, 0);
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
    internal bool Released;

    internal (PhysicsBody? Scene, PhysicsServerCollider? Server) Owners
    {
        get
        {
            if (serverOwner is not null && !Released) return (null, serverOwner);
            if (sceneOwner is null) throw new ArgumentException("The RID does not identify a live physics body.", nameof(RID));
            if (sceneOwner.TryGetTarget(out var node) && node is PhysicsBody body && !body.IsDisposed) return (body, null);
            throw new ArgumentException("The RID does not identify a live physics body.", nameof(RID));
        }
    }
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

    internal void ApplyMassProfile()
    {
        var owners = Owners;
        var mass = owners.Scene is RigidBody rigid ? rigid.Mass : Mass;
        var inertia = owners.Scene is RigidBody rigidInertia ? rigidInertia.Inertia : Inertia;
        var center = owners.Scene is RigidBody rigidCenter ? rigidCenter.CustomMassCenter : CustomCenter;
        MassData = PhysicsMass.Apply(BodyID, owners.Scene?.BackendShapes ?? owners.Server!.BackendShapes, mass, inertia, center, MassProxies);
    }

    internal void SetMassProfile(float mass, float inertia, Vector2? center)
    {
        EnsureMutable();
        PhysicsMass.Validate(mass, inertia, center);
        var owners = Owners;
        if (owners.Scene is RigidBody rigid)
        {
            rigid.SetMassProfile(mass, inertia, center.HasValue ? RigidCenterOfMassMode.Custom : RigidCenterOfMassMode.Auto,
                center ?? Vector2.Zero);
            return;
        }
        if (Space is not null)
        {
            if (owners.Scene is { } scene) scene.PrepareBackend(); else owners.Server!.PrepareBackend();
            MassData = PhysicsMass.Apply(BodyID, owners.Scene?.BackendShapes ?? owners.Server!.BackendShapes, mass, inertia, center, MassProxies);
        }
        Mass = mass; Inertia = inertia; CustomCenter = center;
    }

    internal void ApplyBeforeStep(B2BodyId id, PhysicsBody? scene)
    {
        var type = b2Body_GetType(id);
        ActiveBeforeStep = type != B2BodyType.b2_staticBody && b2Body_IsAwake(id);
        if (type == B2BodyType.b2_staticBody || type == B2BodyType.b2_dynamicBody && !ActiveBeforeStep) return;
        var rigid = scene as RigidBody;
        if (rigid?.CustomIntegrator ?? OmitForces)
        {
            b2Body_SetGravityScale(id, 0);
            var sim = Simulation(id);
            sim.force = default; sim.torque = 0;
            PendingForce = default; PendingTorque = 0;
            return;
        }
        if (PendingForce != Vector2.Zero) b2Body_ApplyForceToCenter(id, Shape.ToBackend(PendingForce), false);
        if (PendingTorque != 0 && !RotationLocked) b2Body_ApplyTorque(id, PendingTorque * PhysicsMass.InertiaScale, false);
        PendingForce = default; PendingTorque = 0;
        if (rigid is not null) { rigid.ApplyConstantForces(); return; }
        b2Body_SetGravityScale(id, BodyGravityScale);
        if (ConstantForce != Vector2.Zero) b2Body_ApplyForceToCenter(id, Shape.ToBackend(ConstantForce), false);
        if (ConstantTorque != 0) b2Body_ApplyTorque(id, ConstantTorque * 0.0001f, false);
    }

    internal static B2BodySim Simulation(B2BodyId id)
    {
        var world = b2GetWorldFromId(b2Body_GetWorld(id));
        return b2GetBodySim(world, b2GetBodyFullId(world, id));
    }
}
