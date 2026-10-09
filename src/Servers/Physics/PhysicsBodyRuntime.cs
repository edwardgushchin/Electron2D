namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime(RID rid, WeakReference<CollisionObject>? sceneOwner, PhysicsServerCollider? serverOwner)
{
    internal RID RID { get; } = rid;
    internal float Mass = 1;
    internal float Inertia;
    internal Vector2? CustomCenter;
    private PhysicsMass.Geometry? _massGeometry;
    internal PhysicsMass.Properties MassProperties = new(1, 0, default);
    internal Vector2 ConstantForce;
    internal float ConstantTorque;
    internal bool OmitForces;
    internal const int MaxContactLimit = 4095;
    internal int MaxContacts;
    internal Vector2 Gravity;
    internal float LinearDamp;
    internal float AngularDamp;
    internal Action<PhysicsDirectBodyState>? ForceCallback;
    internal Action<PhysicsDirectBodyState>? SyncCallback;
    internal PhysicsDirectBodyState? View { get; private set; }
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
    internal PhysicsColliderBackend Backend { get { var owner = Owners; return owner.Scene?.Backend ?? owner.Server!.Backend; } }
    internal bool Omitted { get => Owners.Scene is RigidBody rigid ? rigid.CustomIntegrator : OmitForces; }
    internal int ContactLimit => Owners.Scene is RigidBody rigid ? rigid.MaxContactsReported : MaxContacts;

    internal void EnsureMutable() => Space?.EnsureQueryAccess();

    internal void ApplyMassProfile()
    {
        var owners = Owners;
        var mass = owners.Scene is RigidBody rigid ? rigid.Mass : Mass;
        var inertia = owners.Scene is RigidBody rigidInertia ? rigidInertia.Inertia : Inertia;
        var center = owners.Scene is RigidBody rigidCenter ? rigidCenter.CustomMassCenter : CustomCenter;
        SetAttachedMassProfile(mass, inertia, center);
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
            SetAttachedMassProfile(mass, inertia, center);
        }
        Mass = mass; Inertia = inertia; CustomCenter = center;
    }

    internal void SetAttachedMassProfile(float mass, float inertia, Vector2? center) =>
        MassProperties = Backend.ApplyMassProfile(mass, inertia, center);

    internal void ApplyBeforeStep(PhysicsBody? scene)
    {
        var backend = Backend;
        ActiveBeforeStep = !backend.HasMotionMode(PhysicsServer.BodyMode.Static) && backend.IsAwake;
        if (backend.HasMotionMode(PhysicsServer.BodyMode.Static) || backend.IsDynamic && !ActiveBeforeStep) return;
        var rigid = scene as RigidBody;
        if (Space?.GPUStore is not null)
        {
            if (!(rigid?.CustomIntegrator ?? OmitForces))
            {
                if (PendingForce != Vector2.Zero) backend.ApplyCentralForce(PendingForce, false);
                if (PendingTorque != 0 && !RotationLocked) backend.ApplyTorque(PendingTorque, false);
            }
            PendingForce = default; PendingTorque = 0; return;
        }
        if (rigid?.CustomIntegrator ?? OmitForces)
        {
            backend.SetGravityScale(0); backend.ClearTransientForces();
            PendingForce = default; PendingTorque = 0;
            return;
        }
        if (PendingForce != Vector2.Zero) backend.ApplyCentralForce(PendingForce, false);
        if (PendingTorque != 0 && !RotationLocked) backend.ApplyTorque(PendingTorque, false);
        PendingForce = default; PendingTorque = 0;
        if (rigid is not null) { rigid.ApplyConstantForces(); return; }
        backend.SetGravityScale(BodyGravityScale);
        if (ConstantForce != Vector2.Zero) backend.ApplyCentralForce(ConstantForce, false);
        if (ConstantTorque != 0) backend.ApplyTorque(ConstantTorque, false);
    }
}
