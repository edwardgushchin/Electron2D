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

    /// <summary>Creates an empty caller-owned joint identity.</summary>
    /// <returns>A stable nonempty RID; FreeRID releases it.</returns>
    public RID JointCreate()
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

    /// <summary>Clears the body connection while retaining the RID and collision policy.</summary>
    /// <param name="joint">A live scene-owned or caller-owned joint RID.</param>
    /// <remarks>A scene joint remains cleared until scene path/geometry edits or reentry configure it again.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public void JointClear(RID joint)
    {
        var runtime = GetJoint(joint);
        runtime.Clear(); MarkSceneOverride(runtime);
    }

    /// <summary>Returns the current configured role, including Empty for an unconnected resource.</summary>
    /// <param name="joint">A live joint RID.</param>
    /// <returns>The configured role; identity does not change when the role is replaced.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public JointType JointGetType(RID joint) => GetJoint(joint).Type;

    /// <summary>Changes mutual contact suppression without changing the local anchors.</summary>
    /// <param name="joint">A live joint RID.</param>
    /// <param name="disable">Whether the connected pair omits body contacts.</param>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public void JointDisableCollisionsBetweenBodies(RID joint, bool disable) => GetJoint(joint).SetDisableCollision(disable);

    /// <summary>Returns the joint's stored mutual collision policy.</summary>
    /// <param name="joint">A live joint RID.</param>
    /// <returns>True when mutual body contacts are suppressed.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public bool JointIsDisabledCollisionsBetweenBodies(RID joint) => GetJoint(joint).DisableCollision;

    /// <summary>Configures a pin at a finite global anchor.</summary>
    /// <param name="joint">A live joint RID; a scene owner must have the PinJoint role.</param>
    /// <param name="anchor">Global scene-unit pivot.</param>
    /// <param name="bodyA">First live body RID.</param>
    /// <param name="bodyB">Second live body RID, or empty to attach body A to the fixed world.</param>
    /// <remarks>Body-local frames are sampled once. Detached bodies connect when they later share a space.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void JointMakePin(RID joint, Vector2 anchor, RID bodyA, RID bodyB = default)
    {
        var runtime = GetJoint(joint);
        PrepareJointBody(bodyA); if (bodyB.IsValid()) PrepareJointBody(bodyB);
        runtime.ConfigurePin(anchor, bodyA, bodyB); MarkSceneOverride(runtime);
    }

    /// <summary>Configures a finite body-A groove and a freely rotating body-B anchor.</summary>
    /// <param name="joint">A live joint RID; a scene owner must have the GrooveJoint role.</param>
    /// <param name="groove1A">First global groove endpoint in scene units.</param>
    /// <param name="groove2A">Second global endpoint; equal endpoints form a point constraint.</param>
    /// <param name="anchorB">Global point used to sample body B's local anchor.</param>
    /// <param name="bodyA">First live body RID; empty rejects.</param>
    /// <param name="bodyB">Second live body RID; empty rejects.</param>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void JointMakeGroove(RID joint, Vector2 groove1A, Vector2 groove2A, Vector2 anchorB,
        RID bodyA = default, RID bodyB = default)
    {
        var runtime = GetJoint(joint);
        PrepareJointBody(bodyA); PrepareJointBody(bodyB);
        runtime.ConfigureGroove(groove1A, groove2A, anchorB, bodyA, bodyB); MarkSceneOverride(runtime);
    }

    /// <summary>Configures spring force between two sampled body-local anchors.</summary>
    /// <param name="joint">A live joint RID; a scene owner must have the DampedSpringJoint role.</param>
    /// <param name="anchorA">First global anchor in scene units.</param>
    /// <param name="anchorB">Second global anchor in scene units.</param>
    /// <param name="bodyA">First live body RID.</param>
    /// <param name="bodyB">Second live body RID; empty rejects.</param>
    /// <remarks>Resets relaxed length to anchor separation, stiffness to 20 and damping to 1.5 kg/s.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void JointMakeDampedSpring(RID joint, Vector2 anchorA, Vector2 anchorB, RID bodyA, RID bodyB = default)
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
    /// <summary>Returns whether the relative angle is limited.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Whether the relative angle is limited.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public bool PinJointGetAngularLimitEnabled(RID joint) => GetJoint(joint, JointType.Pin).PinLimitEnabled;

    /// <summary>Changes whether the relative angle is limited.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Whether the relative angle is limited.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void PinJointSetAngularLimitEnabled(RID joint, bool value) => GetJoint(joint, JointType.Pin).SetPinLimitEnabled(value);

    /// <summary>Returns lower relative angle in radians.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Lower relative angle in radians.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public float PinJointGetAngularLimitLower(RID joint) => GetJoint(joint, JointType.Pin).PinLimitLower;

    /// <summary>Changes lower relative angle in radians.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Lower relative angle in radians.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void PinJointSetAngularLimitLower(RID joint, float value) => GetJoint(joint, JointType.Pin).SetPinLimitLower(value);

    /// <summary>Returns upper relative angle in radians.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Upper relative angle in radians.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public float PinJointGetAngularLimitUpper(RID joint) => GetJoint(joint, JointType.Pin).PinLimitUpper;

    /// <summary>Changes upper relative angle in radians.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Upper relative angle in radians.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void PinJointSetAngularLimitUpper(RID joint, float value) => GetJoint(joint, JointType.Pin).SetPinLimitUpper(value);

    /// <summary>Returns whether the angular motor runs.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Whether the angular motor runs.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public bool PinJointGetMotorEnabled(RID joint) => GetJoint(joint, JointType.Pin).PinMotorEnabled;

    /// <summary>Changes whether the angular motor runs.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Whether the angular motor runs.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public void PinJointSetMotorEnabled(RID joint, bool value) => GetJoint(joint, JointType.Pin).SetPinMotorEnabled(value);

    /// <summary>Returns desired relative radians per second.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Desired relative radians per second.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public float PinJointGetMotorTargetVelocity(RID joint) => GetJoint(joint, JointType.Pin).PinMotorVelocity;

    /// <summary>Changes desired relative radians per second.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Desired relative radians per second.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void PinJointSetMotorTargetVelocity(RID joint, float value) => GetJoint(joint, JointType.Pin).SetPinMotorVelocity(value);

    /// <summary>Returns finite nonnegative torque cap in newton-meters.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Finite nonnegative torque cap in newton-meters.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public float PinJointGetMotorMaxTorque(RID joint) => GetJoint(joint, JointType.Pin).PinMotorMaxTorque;

    /// <summary>Changes finite nonnegative torque cap in newton-meters.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Finite nonnegative torque cap in newton-meters.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void PinJointSetMotorMaxTorque(RID joint, float value) => GetJoint(joint, JointType.Pin).SetPinMotorMaxTorque(value);

    /// <summary>Returns nonnegative relaxed separation in scene units; server zero is literal.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <returns>Nonnegative relaxed separation in scene units; server zero is literal.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public float DampedSpringJointGetRestLength(RID joint) => GetJoint(joint, JointType.DampedSpring).EffectiveSpringRestLength;

    /// <summary>Changes nonnegative relaxed separation in scene units; server zero is literal.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <param name="value">Nonnegative relaxed separation in scene units; server zero is literal.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void DampedSpringJointSetRestLength(RID joint, float value) => GetJoint(joint, JointType.DampedSpring).SetSpringRestLength(value);

    /// <summary>Returns nonnegative Hooke coefficient in kilograms per second squared.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <returns>Nonnegative Hooke coefficient in kilograms per second squared.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public float DampedSpringJointGetStiffness(RID joint) => GetJoint(joint, JointType.DampedSpring).SpringStiffness;

    /// <summary>Changes nonnegative Hooke coefficient in kilograms per second squared.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <param name="value">Nonnegative Hooke coefficient in kilograms per second squared.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void DampedSpringJointSetStiffness(RID joint, float value) => GetJoint(joint, JointType.DampedSpring).SetSpringStiffness(value);

    /// <summary>Returns nonnegative axial drag coefficient in kilograms per second.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <returns>Nonnegative axial drag coefficient in kilograms per second.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public float DampedSpringJointGetDamping(RID joint) => GetJoint(joint, JointType.DampedSpring).SpringDamping;

    /// <summary>Changes nonnegative axial drag coefficient in kilograms per second.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <param name="value">Nonnegative axial drag coefficient in kilograms per second.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public void DampedSpringJointSetDamping(RID joint, float value) => GetJoint(joint, JointType.DampedSpring).SetSpringDamping(value);

}
