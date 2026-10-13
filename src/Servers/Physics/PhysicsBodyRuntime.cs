namespace Electron2D;

/// <summary>Shares scene/server body state and tracks authored integration edits for the current attachment.</summary>
internal sealed partial class PhysicsBodyRuntime(RID rid, WeakReference<CollisionObject>? sceneOwner, PhysicsServerCollider? serverOwner)
{
    internal RID RID { get; } = rid;
    internal float Mass = 1;
    internal float Inertia;
    internal Vector2? CustomCenter;
    private PhysicsMass.Geometry? _massGeometry;
    internal PhysicsMass.Properties MassProperties = new(1, 0, default);
    internal bool GPUParametersDirty = true;
    private Vector2 _constantForce;
    private float _constantTorque;
    private bool _omitForces;
    internal Vector2 ConstantForce { get => _constantForce; set { GPUParametersDirty |= _constantForce != value; _constantForce = value; } }
    internal float ConstantTorque { get => _constantTorque; set { GPUParametersDirty |= _constantTorque != value; _constantTorque = value; } }
    internal bool OmitForces { get => _omitForces; set { GPUParametersDirty |= _omitForces != value; _omitForces = value; } }
    internal const int MaxContactLimit = 4095;
    internal int MaxContacts;
    internal float ContactsReportedDepthThreshold;
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

    internal void EnsureAccess(bool writing = false)
    {
        var owners = Owners;
        owners.Scene?.Tree?.EnsureOwnerThread();
        if (writing) owners.Scene?.EnsurePhysicsParticipationChange();
        var space = Space;
        space?.EnsureQueryAccess();
        if (writing) space?.EnsureImplementationWriteAccess();
    }

    internal void EnsureAuthoringReadAccess()
    {
        var space = Space;
        if (space?.IsNumericalInputThread != true) Owners.Scene?.Tree?.EnsureOwnerThread();
        space?.EnsureImplementationInputReadAccess();
    }

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
        EnsureAccess(writing: true);
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

    internal PhysicsBodyIntegrationConfiguration CaptureIntegrationConfiguration()
    {
        var rigid = Owners.Scene as RigidBody;
        return new(rigid?.GravityScale ?? BodyGravityScale, rigid?.LinearDamp ?? BodyLinearDamp,
            rigid?.AngularDamp ?? BodyAngularDamp, rigid?.LinearDampMode ?? BodyLinearDampMode,
            rigid?.AngularDampMode ?? BodyAngularDampMode, rigid is null || rigid.HasServerMode ? Backend.RotationLocked : !rigid.Freeze && rigid.LockRotation,
            rigid?.CustomIntegrator ?? OmitForces, rigid?.ConstantForce ?? ConstantForce, rigid?.ConstantTorque ?? ConstantTorque, ContinuousMode);
    }

    internal void ApplyBeforeStep(PhysicsBody? scene)
    {
        var backend = Backend;
        backend.SetContactReporting(ContactLimit > 0);
        ActiveBeforeStep = !backend.HasMotionMode(PhysicsServer.BodyMode.Static) && backend.IsAwake;
        if (backend.HasMotionMode(PhysicsServer.BodyMode.Static) || backend.IsDynamic && !ActiveBeforeStep) return;
        var rigid = scene as RigidBody;
        var configuration = new PhysicsBodyForceConfiguration(PendingForce, PendingTorque,
            rigid?.ConstantForce ?? ConstantForce, rigid?.ConstantTorque ?? ConstantTorque,
            rigid?.GravityScale ?? BodyGravityScale, rigid?.CustomIntegrator ?? OmitForces, backend.RotationLocked,
            rigid is null || (rigid.HasServerMode || !rigid.Freeze) && !rigid.PhysicsMadeStatic && backend.IsAwake, rigid is null);
        backend.PrepareDefaultForces(configuration);
        PendingForce = default; PendingTorque = 0;
    }
}
