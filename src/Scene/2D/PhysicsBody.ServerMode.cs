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

    internal bool HasServerMode => _serverMode is not null;
    internal PhysicsServer.BodyMode EffectiveBodyMode => PhysicsMadeStatic ? PhysicsServer.BodyMode.Static : _serverMode ?? RequestedBodyMode;
    internal Vector2 ServerLinearVelocity => HasBackend && EffectiveBodyMode != PhysicsServer.BodyMode.Static ? Backend.LinearVelocity : _serverLinear;
    internal float ServerAngularVelocity => HasBackend && EffectiveBodyMode != PhysicsServer.BodyMode.Static ? Backend.AngularVelocity : _serverAngular;
    internal bool ServerSleeping => HasBackend ? !Backend.IsAwake : _serverSleeping;

    internal void SetServerMode(PhysicsServer.BodyMode mode)
    {
        if (_serverMode == mode) return;
        if (HasBackend) PrepareBackend();
        var linear = Runtime.GetLinearVelocity(); var angular = Runtime.GetAngularVelocity();
        _serverMode = mode; _hasServerTarget = false; _firstServerTarget = true; _serverSleeping = false;
        _serverLinear = mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic ? default : linear;
        _serverAngular = mode == PhysicsServer.BodyMode.Rigid ? angular : 0;
        Runtime.GPUParametersDirty = true;
        if (!HasBackend) return;
        Backend.SetMotionMode(EffectiveBodyMode);
        Backend.SetRotationLocked(EffectiveBodyMode == PhysicsServer.BodyMode.RigidLinear);
        ApplyServerMotion(); MarkShapesDirty(); PrepareBackend();
    }

    internal void RestoreSceneMode()
    {
        if (!HasServerMode) return;
        _serverMode = null; _hasServerTarget = false; Runtime.GPUParametersDirty = true; MarkShapesDirty();
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
        _serverLinear = value;
        if (HasBackend) ApplyServerMotion();
    }
    internal void SetServerAngularVelocity(float value)
    {
        _serverAngular = value;
        if (HasBackend) ApplyServerMotion();
    }
    internal void SetServerSleeping(bool value)
    {
        _serverSleeping = value;
        if (HasBackend) Backend.SetAwake(!value);
        if (value) { _serverLinear = default; _serverAngular = 0; }
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
        ApplyServerTransform(value);
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
        if (!HasServerMode || !HasBackend || EffectiveBodyMode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic) return;
        _serverLinear = ServerLinearVelocity; _serverAngular = ServerAngularVelocity; _serverSleeping = !Backend.IsAwake;
    }
}
