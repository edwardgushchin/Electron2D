namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    /// <summary>Authored scalar gravity and signed body damping; omission skips default forces, not constraints or impulses.</summary>
    internal readonly record struct IntegrationPolicy(float GravityScale = 1, float LinearDamp = 0, float AngularDamp = 0,
        bool LockRotation = false, bool OmitForceIntegration = false, RigidBody.DampMode LinearDampMode = RigidBody.DampMode.Combine, RigidBody.DampMode AngularDampMode = RigidBody.DampMode.Combine);
    private const uint ModeEdit = 4096, PolicyEdit = 8192, ClearAngular = 16384;
    private const uint CollisionPriorityEdit = 524288;

    internal float GetCollisionPriority(BodyHandle body) { Validate(body); return _slots[body.Index].Surface.W; }
    internal void SetCollisionPriority(BodyHandle body, float value)
    {
        Validate(body); PhysicsColliderBackend.ValidateCollisionPriority(value);
        ref var slot = ref _slots[body.Index]; if (slot.Surface.W == value) return;
        ref var command = ref Edit(body.Index); slot.Surface.W = value;
        command.Body.Surface.W = value; command.Mask |= CollisionPriorityEdit;
    }
    internal bool GetRotationLocked(BodyHandle body) { Validate(body); return RotationLocked(_slots[body.Index]); }
    internal PhysicsServer.BodyMode GetMode(BodyHandle body) { Validate(body); return _slots[body.Index].Mode; }
    internal IntegrationPolicy GetIntegrationPolicy(BodyHandle body) { Validate(body); return _slots[body.Index].Integration; }

    /// <summary>Changes live solver role without replacing identity, geometry, mass configuration or joints.</summary>
    internal void SetMode(BodyHandle body, PhysicsServer.BodyMode mode)
    {
        Validate(body);
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (mode != PhysicsServer.BodyMode.Static && _areaFields.ContainsKey(body.Index)) throw new InvalidOperationException("An Area field owner must remain static.");
        ref var slot = ref _slots[body.Index];
        if (slot.Mode == mode) return;
        if (slot.CCDMode != CCDMode.Disabled)
            _ccdBodyCount += (mode >= PhysicsServer.BodyMode.Rigid ? 1 : 0) - (slot.Mode >= PhysicsServer.BodyMode.Rigid ? 1 : 0);
        var previousMode = slot.Mode;
        _kinematicBodyCount += (mode == PhysicsServer.BodyMode.Kinematic ? 1 : 0) - (previousMode == PhysicsServer.BodyMode.Kinematic ? 1 : 0);
        slot.Mode = mode;
        ref var command = ref Edit(body.Index);
        command.Mask |= ModeEdit; command.Body.Mode = (uint)mode;
        if (mode < PhysicsServer.BodyMode.Rigid) SetVelocity(body, Vector2.Zero, 0);
        else
        {
            if (previousMode < PhysicsServer.BodyMode.Rigid)
            {
                var surface = slot.Surface; SetVelocity(body, new(surface.X, surface.Y), surface.Z);
                slot.Surface = new(0, 0, 0, surface.W); command.Body.Surface = slot.Surface; command.Mask |= SurfaceEdit;
            }
            if (RotationLocked(slot)) ClearAngularMotion(body.Index);
        }
        CancelKinematicTarget(body.Index);
        WriteIntegrationPolicy(body.Index);
        // A mode transition retires contact identities while retaining the authored shape handles.
        for (var shape = slot.FirstShape; shape >= 0; shape = _shapeSlots[shape].NextOnBody) MarkShape(shape, false);
        Wake(body.Index);
    }

    internal void SetIntegrationPolicy(BodyHandle body, in IntegrationPolicy policy)
    {
        Validate(body); ValidateIntegrationPolicy(policy);
        ref var slot = ref _slots[body.Index];
        if (slot.Integration == policy) return;
        var wasLocked = RotationLocked(slot);
        slot.Integration = policy;
        if (!wasLocked && RotationLocked(slot)) ClearAngularMotion(body.Index);
        WriteIntegrationPolicy(body.Index); Wake(body.Index, true);
    }

    private static void ValidateIntegrationPolicy(in IntegrationPolicy policy)
    {
        if (!float.IsFinite(policy.GravityScale) || !float.IsFinite(policy.LinearDamp) || !float.IsFinite(policy.AngularDamp) || !Enum.IsDefined(policy.LinearDampMode) || !Enum.IsDefined(policy.AngularDampMode))
            throw new ArgumentOutOfRangeException(nameof(policy));
    }
    private static bool RotationLocked(in Slot slot) => slot.Mode >= PhysicsServer.BodyMode.Rigid &&
        (slot.Mode == PhysicsServer.BodyMode.RigidLinear || slot.Integration.LockRotation);
    private void ClearAngularMotion(int index)
    {
        ref var command = ref Edit(index); command.Mask |= ClearAngular;
        command.Body.Velocity.Z = 0; command.Impulse.Z = 0;
    }
    private void WriteIntegrationPolicy(int index)
    {
        ref readonly var slot = ref _slots[index]; var policy = slot.Integration;
        ref var command = ref Edit(index); command.Mask |= PolicyEdit;
        command.Body.Force.W = policy.GravityScale;
        command.Body.Properties.Z = policy.LinearDamp; command.Body.Properties.W = policy.AngularDamp;
        command.Body.Locks = (command.Body.Locks & ~50180u) | (RotationLocked(slot) ? 4u : 0u) | (policy.OmitForceIntegration ? 1024u : 0u) | (policy.LinearDampMode == RigidBody.DampMode.Replace ? 16384u : 0u) | (policy.AngularDampMode == RigidBody.DampMode.Replace ? 32768u : 0u);
    }
}
