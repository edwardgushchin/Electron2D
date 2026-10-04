namespace Electron2D;

public sealed partial class PhysicsServer
{
    private readonly Dictionary<RID, PhysicsJointRuntime> _jointRuntimes = [];
    private readonly List<RID> _staleJoints = [];
    private int _jointRegistrationsSinceSweep;

    /// <summary>Specifies the configured role of a physics joint resource.</summary>
    public enum JointType
    {
        /// <summary>Two anchors sharing a point with free relative rotation.</summary>
        Pin = 0,
        /// <summary>An anchor sliding along a finite groove with free rotation.</summary>
        Groove = 1,
        /// <summary>Hooke force and axial drag between two anchors.</summary>
        DampedSpring = 2,
        /// <summary>An allocated or cleared joint with no body connection.</summary>
        Empty = 3
    }

    internal RID JointCreateCore()
    {
        ThrowIfDisposed();
        var rid = RID.Allocate();
        lock (_registryGate) _jointRuntimes.Add(rid, new(rid));
        return rid;
    }

    internal PhysicsJointRuntime RegisterSceneJoint(Joint scene, JointType type)
    {
        var rid = RID.Allocate();
        var runtime = new PhysicsJointRuntime(rid, scene, type);
        lock (_registryGate)
        {
            _jointRuntimes.Add(rid, runtime);
            if (++_jointRegistrationsSinceSweep >= 256)
            {
                _jointRegistrationsSinceSweep = 0; _staleJoints.Clear();
                foreach (var pair in _jointRuntimes)
                    if (pair.Value.Scene is { } weak && (!weak.TryGetTarget(out var node) || node.IsDisposed) && pair.Value.Space is null)
                        _staleJoints.Add(pair.Key);
                foreach (var stale in _staleJoints) _jointRuntimes.Remove(stale);
            }
        }
        return runtime;
    }

    internal void UnregisterSceneJoint(RID rid)
    {
        lock (_registryGate)
            if (_jointRuntimes.Remove(rid, out var runtime)) runtime.Clear();
    }

    private PhysicsJointRuntime GetJoint(RID rid, JointType? type = null)
    {
        ThrowIfDisposed();
        PhysicsJointRuntime runtime;
        lock (_registryGate)
        {
            if (!_jointRuntimes.TryGetValue(rid, out runtime!))
                throw new ArgumentException("The RID does not identify a live physics joint.", nameof(rid));
            if (runtime.Scene is { } weak)
            {
                if (!weak.TryGetTarget(out var scene) || scene.IsDisposed)
                    throw new ArgumentException("The scene joint owner is no longer alive.", nameof(rid));
                scene.Tree?.EnsureOwnerThread();
            }
        }
        runtime.EnsureAccess();
        if (type is { } role) runtime.Require(role);
        return runtime;
    }

    internal void JointClearCore(RID joint)
    {
        var runtime = GetJoint(joint);
        runtime.Clear(); MarkSceneOverride(runtime);
    }

    internal JointType JointGetTypeCore(RID joint) => GetJoint(joint).Type;

    internal void JointDisableCollisionsBetweenBodiesCore(RID joint, bool disable) => GetJoint(joint).SetDisableCollision(disable);

    internal bool JointIsDisabledCollisionsBetweenBodiesCore(RID joint) => GetJoint(joint).DisableCollision;

    internal void JointMakePinCore(RID joint, Vector2 anchor, RID bodyA, RID bodyB = default)
    {
        var runtime = GetJoint(joint);
        PrepareJointBody(bodyA); if (bodyB.IsValid()) PrepareJointBody(bodyB);
        runtime.ConfigurePin(anchor, bodyA, bodyB); MarkSceneOverride(runtime);
    }

    internal void JointMakeGrooveCore(RID joint, Vector2 groove1A, Vector2 groove2A, Vector2 anchorB,
    RID bodyA = default, RID bodyB = default)
    {
        var runtime = GetJoint(joint);
        PrepareJointBody(bodyA); PrepareJointBody(bodyB);
        runtime.ConfigureGroove(groove1A, groove2A, anchorB, bodyA, bodyB); MarkSceneOverride(runtime);
    }

    internal void JointMakeDampedSpringCore(RID joint, Vector2 anchorA, Vector2 anchorB, RID bodyA, RID bodyB = default)
    {
        var runtime = GetJoint(joint);
        PrepareJointBody(bodyA); PrepareJointBody(bodyB);
        runtime.ConfigureSpring(anchorA, anchorB, bodyA, bodyB); MarkSceneOverride(runtime);
    }

    private static void MarkSceneOverride(PhysicsJointRuntime runtime)
    {
        if (runtime.Scene is { } weak && weak.TryGetTarget(out var scene)) scene.MarkServerOverride();
    }

    private void PrepareJointBody(RID rid)
    {
        var owners = ResolveBodyOwners(rid);
        if (owners.Scene is { } scene) { scene.Space?.EnsureQueryAccess(); if (scene.HasBackend) scene.PrepareBackend(); }
        else { owners.Server!.Space?.EnsureQueryAccess(); owners.Server.PrepareBackend(); }
    }

    internal void NotifyJointBodySpaceChanged(RID body)
    {
        // ponytail: Membership changes scan joint resources; index by body if lifecycle profiling warrants it.
        lock (_registryGate)
            foreach (var runtime in _jointRuntimes.Values)
                if (runtime.BodyA == body || runtime.BodyB == body)
                {
                    if (runtime.Scene is { } weak && weak.TryGetTarget(out var node) && !node.HasServerOverride) continue;
                    runtime.RefreshSpace();
                }
    }

    internal void EnsureJointBodyMembershipChange(RID body)
    {
        lock (_registryGate)
            foreach (var runtime in _jointRuntimes.Values)
                if (runtime.BodyA == body || runtime.BodyB == body) runtime.EnsureAccess();
    }

    internal void ClearJointsForBody(RID body)
    {
        lock (_registryGate)
            foreach (var runtime in _jointRuntimes.Values)
                if (runtime.BodyA == body || runtime.BodyB == body) runtime.Clear();
    }
    internal bool PinJointGetAngularLimitEnabledCore(RID joint) => GetJoint(joint, JointType.Pin).PinLimitEnabled;

    internal void PinJointSetAngularLimitEnabledCore(RID joint, bool value) => GetJoint(joint, JointType.Pin).SetPinLimitEnabled(value);

    internal float PinJointGetAngularLimitLowerCore(RID joint) => GetJoint(joint, JointType.Pin).PinLimitLower;

    internal void PinJointSetAngularLimitLowerCore(RID joint, float value) => GetJoint(joint, JointType.Pin).SetPinLimitLower(value);

    internal float PinJointGetAngularLimitUpperCore(RID joint) => GetJoint(joint, JointType.Pin).PinLimitUpper;

    internal void PinJointSetAngularLimitUpperCore(RID joint, float value) => GetJoint(joint, JointType.Pin).SetPinLimitUpper(value);

    internal bool PinJointGetMotorEnabledCore(RID joint) => GetJoint(joint, JointType.Pin).PinMotorEnabled;

    internal void PinJointSetMotorEnabledCore(RID joint, bool value) => GetJoint(joint, JointType.Pin).SetPinMotorEnabled(value);

    internal float PinJointGetMotorTargetVelocityCore(RID joint) => GetJoint(joint, JointType.Pin).PinMotorVelocity;

    internal void PinJointSetMotorTargetVelocityCore(RID joint, float value) => GetJoint(joint, JointType.Pin).SetPinMotorVelocity(value);

    internal float PinJointGetMotorMaxTorqueCore(RID joint) => GetJoint(joint, JointType.Pin).PinMotorMaxTorque;

    internal void PinJointSetMotorMaxTorqueCore(RID joint, float value) => GetJoint(joint, JointType.Pin).SetPinMotorMaxTorque(value);

    internal float DampedSpringJointGetRestLengthCore(RID joint) => GetJoint(joint, JointType.DampedSpring).EffectiveSpringRestLength;

    internal void DampedSpringJointSetRestLengthCore(RID joint, float value) => GetJoint(joint, JointType.DampedSpring).SetSpringRestLength(value);

    internal float DampedSpringJointGetStiffnessCore(RID joint) => GetJoint(joint, JointType.DampedSpring).SpringStiffness;

    internal void DampedSpringJointSetStiffnessCore(RID joint, float value) => GetJoint(joint, JointType.DampedSpring).SetSpringStiffness(value);

    internal float DampedSpringJointGetDampingCore(RID joint) => GetJoint(joint, JointType.DampedSpring).SpringDamping;

    internal void DampedSpringJointSetDampingCore(RID joint, float value) => GetJoint(joint, JointType.DampedSpring).SetSpringDamping(value);

}
