namespace Electron2D;

public abstract partial class PhysicsBody
{
    private PhysicsServer.BodyMode? _serverMode;
    private Vector2 _serverLinear;
    private float _serverAngular;
    private bool _serverSleeping;
    private Transform _serverTarget = Transform.Identity;
    private bool _hasServerTarget;
    private bool _firstServerTarget = true;
    private bool _serverModeDirty, _preparingServerMode;

    internal bool HasServerMode => _serverMode is not null;
    internal PhysicsServer.BodyMode EffectiveBodyMode => PhysicsMadeStatic ? PhysicsServer.BodyMode.Static : _serverMode ?? RequestedBodyMode;
    internal Vector2 ServerLinearVelocity { get { PreparePendingServerMode(); return HasBackend && EffectiveBodyMode != PhysicsServer.BodyMode.Static ? Backend.LinearVelocity : _serverLinear; } }
    internal float ServerAngularVelocity { get { PreparePendingServerMode(); return HasBackend && EffectiveBodyMode != PhysicsServer.BodyMode.Static ? Backend.AngularVelocity : _serverAngular; } }
    internal bool ServerSleeping { get { PreparePendingServerMode(); return HasBackend ? !Backend.IsAwake : _serverSleeping; } }

    internal void SetServerMode(PhysicsServer.BodyMode mode)
    {
        if (_serverMode == mode) return;
        if (HasBackend) PrepareBackend();
        var linear = Runtime.GetLinearVelocity(); var angular = Runtime.GetAngularVelocity();
        _serverMode = mode; _hasServerTarget = false; _firstServerTarget = true; _serverSleeping = false;
        _serverLinear = mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic ? default : linear;
        _serverAngular = mode == PhysicsServer.BodyMode.Rigid ? angular : 0;
        Runtime.GPUParametersDirty = true;
        PublishBodyMode();
    }

    internal void PublishBodyMode()
    {
        if (!HasBackend) return;
        _serverModeDirty = true;
        PreparePendingServerMode();
    }

    internal void PreparePendingServerMode(bool writing = false)
    {
        if (writing || _serverModeDirty)
        {
            Space?.EnsureImplementationWriteAccess();
        }
        Space?.EnsureBackendAccess();
        Backend.PreparePendingMotion();
        if (!_serverModeDirty || Space is null) return;
        if (_preparingServerMode) throw new InvalidOperationException("Body mode preparation cannot reenter its own transition.");
        _preparingServerMode = true;
        try
        {
            if (PhysicsMadeStatic) OnMadeStatic();
            Backend.SetMotionMode(EffectiveBodyMode);
            if (HasServerMode)
            {
                Backend.SetRotationLocked(EffectiveBodyMode == PhysicsServer.BodyMode.RigidLinear);
                if (EffectiveBodyMode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear) Backend.SetAwake(true);
                ApplyServerMotion();
            }
            else { OnBodyTypeChanged(); Runtime.RestoreSceneState(); }
            MarkShapesDirty(); PrepareBackend();
            _serverModeDirty = false;
        }
        catch { Space.MarkColliderMetadataDirty(); throw; }
        finally { _preparingServerMode = false; }
    }

    internal void RestoreSceneMode()
    {
        PreparePendingServerMode();
        PreparePendingPose();
        if (this is RigidBody rigid) rigid.CaptureRoleMotion();
        if (!HasServerMode) return;
        _serverMode = null; _serverModeDirty = false; _hasServerTarget = false; Runtime.GPUParametersDirty = true; MarkShapesDirty();
    }

    internal PhysicsBodyConfiguration ServerConfiguration(PhysicsBodyConfiguration configuration) => !HasServerMode ? configuration :
        configuration with
        {
            Mode = EffectiveBodyMode,
            LinearVelocity = _serverLinear,
            AngularVelocity = _serverAngular,
            Sleeping = _serverSleeping,
            LockRotation = EffectiveBodyMode == PhysicsServer.BodyMode.RigidLinear
        };

    internal void SetServerLinearVelocity(Vector2 value)
    {
        PreparePendingServerMode(writing: true);
        _serverLinear = value;
        var dynamic = EffectiveBodyMode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
        if (dynamic) _serverSleeping = false;
        if (HasBackend) Backend.PublishMotion(linear: dynamic ? value : Vector2.Zero,
            surface: dynamic ? null : (value, _serverAngular), awake: dynamic ? true : null);
    }
    internal void SetServerAngularVelocity(float value)
    {
        PreparePendingServerMode(writing: true);
        _serverAngular = value;
        var dynamic = EffectiveBodyMode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
        if (dynamic) _serverSleeping = false;
        if (HasBackend) Backend.PublishMotion(angular: EffectiveBodyMode == PhysicsServer.BodyMode.Rigid ? value : 0,
            surface: dynamic ? null : (_serverLinear, value), awake: dynamic ? true : null);
    }
    internal void SetServerSleeping(bool value)
    {
        PreparePendingServerMode(writing: true);
        _serverSleeping = value;
        if (value) { _serverLinear = default; _serverAngular = 0; }
        if (HasBackend) Backend.PublishMotion(awake: !value);
    }
    internal void SetServerCanSleep(bool value)
    {
        PreparePendingServerMode(writing: true);
        var wake = !value && EffectiveBodyMode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
        if (wake) _serverSleeping = false;
        if (HasBackend) Backend.PublishMotion(canSleep: value, awake: wake ? true : null);
    }
    private void ApplyServerMotion()
    {
        var dynamic = EffectiveBodyMode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
        Backend.SetLinearVelocity(dynamic ? _serverLinear : default);
        Backend.SetAngularVelocity(EffectiveBodyMode == PhysicsServer.BodyMode.Rigid ? _serverAngular : 0);
        Backend.SetSurfaceVelocity(dynamic ? default : _serverLinear, dynamic ? 0 : _serverAngular);
    }

    internal void PrepareServerMotion(double delta)
    {
        if (EffectiveBodyMode == PhysicsServer.BodyMode.Kinematic && _hasServerTarget) Backend.SetTargetPose(_serverTarget, delta);
        else if (EffectiveBodyMode == PhysicsServer.BodyMode.Kinematic) Backend.ClearVelocity();
    }
    internal void SetServerTransform(Transform value)
    {
        GlobalTransform = value;
        try { ApplyServerTransform(value); }
        catch { MarkPoseDirty(); throw; }
    }
    private void ApplyServerTransform(Transform value)
    {
        if (EffectiveBodyMode == PhysicsServer.BodyMode.Kinematic)
        {
            _serverTarget = value; _hasServerTarget = true;
            if (_firstServerTarget && HasBackend) { Backend.SetPose(value); _firstServerTarget = false; }
        }
        else if (HasBackend) Backend.SetPose(value);
    }
    internal void CompleteServerMotion()
    {
        if (!HasServerMode) return;
        if (EffectiveBodyMode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear)
        {
            _serverLinear = Backend.LinearVelocity; _serverAngular = Backend.AngularVelocity; _serverSleeping = !Backend.IsAwake;
        }
        _hasServerTarget = false;
    }

    internal void CaptureServerMotion()
    {
        if (!HasServerMode || !HasBackend || _serverModeDirty || EffectiveBodyMode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic) return;
        var motion = Backend.GetRetainedSolverMotion();
        _serverLinear = motion.LinearVelocity; _serverAngular = motion.AngularVelocity; _serverSleeping = motion.Sleeping;
    }
}
