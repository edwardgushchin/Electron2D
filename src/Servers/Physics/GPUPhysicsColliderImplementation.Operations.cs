namespace Electron2D;

internal sealed partial class GPUPhysicsColliderImplementation
{
    // Resident shape rebuilding publishes contact policy with the device geometry.
    internal override void PrepareContactPolicy() { }
    private static PhysicsServer.BodyMode MotionMode(PhysicsServer.BodyMode mode) => mode switch
    {
        PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic or PhysicsServer.BodyMode.Rigid => mode,
        PhysicsServer.BodyMode.RigidLinear => PhysicsServer.BodyMode.Rigid,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
    internal override bool HasMotionMode(PhysicsServer.BodyMode mode) => MotionMode(GPU.GetMode(GPUHandle)) == MotionMode(mode);
    internal override void SetMotionMode(PhysicsServer.BodyMode mode)
    {
        GPU.SetMode(GPUHandle, mode); _gpuSurfaceLinear = default; _gpuSurfaceAngular = 0; _gpuParametersPrepared = false; Space.InvalidateGPUStates();
    }
    internal override void Detach()
    {
        if (GPUHandle.Generation == 0) { GPU.RetireCollider(Owner, this); return; }
        List<Exception>? errors = null;
        foreach (var shape in GPUShapes)
            try { shape.Query?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        try { if (!Space.HasBackendFailure) GPU.Remove(GPUHandle); } catch (Exception error) { (errors ??= []).Add(error); }
        try { Space.UnregisterGPUCollider(Owner); }
        catch (Exception error) { (errors ??= []).Add(error); }
        finally { GPUShapes.Clear(); GPUHandle = default; GPUStateValid = false; GPU.RetireCollider(Owner, this); }
        if (errors is not null) throw new AggregateException("GPU collider cleanup failed.", errors);
    }
    internal override void SetContactReporting(bool enabled) => GPU.SetContactReporting(GPUHandle, enabled);
    internal override void SetCollisionPriority(float value) => GPU.SetCollisionPriority(GPUHandle, value);
    internal override void UpdateFilter(uint layer, uint mask, bool wakeBody)
    {
        Space.EnsureQueryAccess();
        if (wakeBody) { WakeTouching(); SetAwake(true); }
        GPUMask = mask;
        foreach (var shape in GPUShapes) GPU.SetShapeFilter(shape.Handle, layer, mask, GPUSensor);
        Space.InvalidateGPUStates();
    }
    internal override void RebuildShapes(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask,
        bool sensor, float density, float friction, float bounce)
    {
        foreach (ref readonly var slot in slots)
        {
            if (!slot.Enabled) continue;
            if (!slot.Transform.IsFinite() || !slot.Transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(slot.Transform.Skew))
                throw new InvalidOperationException("Physics shapes require unit scale and zero skew.");
            if (!slot.Shape.IsDisposed) PhysicsShapeBackend.RequireBuiltIn(slot.Shape);
        }
        GPUSensor = sensor; GPUMask = mask; ClearGPUShapes();
        foreach (ref readonly var slot in slots)
            if (slot.Enabled && !slot.Shape.IsDisposed)
                AddGPUShape(slot.Shape, slot.Transform, slot.Index, sensor, layer, mask, friction, bounce, sensor ? null : slot.OneWay);
    }
    internal override (Vector2 Position, float Rotation) GetPose()
    {
        var pose = CurrentPose; return (new(pose.X, pose.Y), MathF.Atan2(pose.W, pose.Z));
    }
    internal override Transform GetTransform()
    {
        if (!Space.DecodeGPUTransforms)
        {
            var basis = CurrentPose;
            return new(new(basis.Z, basis.W), new(-basis.W, basis.Z), new(basis.X, basis.Y));
        }
        var pose = GetPose(); return new(pose.Rotation, Vector2.One, 0, pose.Position);
    }
    internal override Transform SampleJointLocalFrame(Vector2 point, float angle)
    {
        var current = GetTransform();
        var pointLocal = current.AffineInverse() * point;
        PhysicsJointRuntime.ValidateExtent(pointLocal.Length());
        return new Transform(angle - current.Rotation, Vector2.One, 0, pointLocal);
    }
    internal override (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion()
    {
        ref readonly var snapshot = ref GPUState;
        return (new Vector2(snapshot.Velocity.X, snapshot.Velocity.Y) - _gpuSurfaceLinear, snapshot.Velocity.Z - _gpuSurfaceAngular, !IsAwake);
    }
    internal override Vector2 LinearVelocity => new(GPUState.Velocity.X, GPUState.Velocity.Y);
    internal override float AngularVelocity => GPUState.Velocity.Z;
    internal override bool IsAwake => !HasMotionMode(PhysicsServer.BodyMode.Static) &&
        (_gpuActivityValid && _gpuActivityEpoch == Space.GPUStateEpoch ? _gpuActive : !GPUState.Sleeping);
    internal override void SavePose() => _gpuSavedPose = GetPose();
    internal override void RestorePose() => SetPose(_gpuSavedPose.Position, _gpuSavedPose.Rotation);
    internal override void SetPose(Vector2 position, float rotation)
    {
        GPU.SetPose(GPUHandle, position, rotation); Space.InvalidateGPUStates();
        if (GPUSensor) AcceptGPUPose(new(position.X, position.Y, MathF.Cos(rotation), MathF.Sin(rotation)));
    }
    internal override void SetTargetPose(Transform pose, double delta) => GPU.SetKinematicTarget(GPUHandle, pose.Origin, pose.Rotation);
    internal override void SetLinearVelocity(Vector2 velocity) { GPU.SetSolverLinearVelocity(GPUHandle, velocity); Space.InvalidateGPUStates(); }
    internal override void SetAngularVelocity(float velocity) { GPU.SetSolverAngularVelocity(GPUHandle, velocity); Space.InvalidateGPUStates(); }
    internal override void ClearVelocity()
    {
        if (HasMotionMode(PhysicsServer.BodyMode.Kinematic)) GPU.ClearKinematicVelocity(GPUHandle);
        else GPU.SetSolverVelocity(GPUHandle, default, 0);
        Space.InvalidateGPUStates();
    }
    internal override void SetAwake(bool awake)
    {
        if (!awake)
        {
            if (GPU.GetMode(GPUHandle) >= PhysicsServer.BodyMode.Rigid)
                GPUPhysicsWorldBackend.SynchronizeIntegrationInputs(this, PhysicsServer.Service.BodyRuntime(Owner.RID));
            Space.FlushGPUWakes();
        }
        GPU.SetSleeping(GPUHandle, !awake);
        if (awake) Space.InvalidateGPUStates(); else GPUStateValid = false;
    }
    internal override void SetCanSleep(bool canSleep) { GPU.SetCanSleep(GPUHandle, canSleep); Space.InvalidateGPUStates(); }
    internal override void SetRotationLocked(bool locked)
    {
        GPU.SetIntegrationPolicy(GPUHandle, GPU.GetIntegrationPolicy(GPUHandle) with { LockRotation = locked }); _gpuParametersPrepared = false; Space.InvalidateGPUStates();
    }
    internal override void SetGravityScale(float scale)
    {
        GPU.SetIntegrationPolicy(GPUHandle, GPU.GetIntegrationPolicy(GPUHandle) with { GravityScale = scale }); _gpuParametersPrepared = false; Space.InvalidateGPUStates();
    }
    internal override void ApplyCentralForce(Vector2 force, bool wake) { if (wake) SetAwake(true); GPU.ApplyForce(GPUHandle, force); }
    internal override void ApplyTorque(float torque, bool wake) { if (wake) SetAwake(true); GPU.ApplyForce(GPUHandle, default, torque); }
    internal override bool RotationLocked => GPU.GetRotationLocked(GPUHandle);
    internal override Vector2 CenterOfMass => CenterOfMassLocal.Rotated(GPUState.Rotation);
    internal override Vector2 CenterOfMassLocal => GPU.GetMassProperties(GPUHandle).Center;
    internal override float InverseMass => IsDynamic ? 1 / GPU.GetMassProperties(GPUHandle).Mass : 0;
    internal override float InverseInertia => RotationLocked ? 0 : IsDynamic && GPU.GetMassProperties(GPUHandle).Inertia is > 0 and var inertia ? 1 / inertia : 0;
    internal override PhysicsMass.Properties ApplyMassProfile(float mass, float inertia, Vector2? center)
    {
        GPU.SetMassProfile(GPUHandle, new(mass, inertia, center)); Space.InvalidateGPUStates(); return GPU.GetMassProperties(GPUHandle);
    }
    internal override void WakeTouching() { GPU.WakeConnected(GPUHandle); Space.InvalidateGPUStates(); }
    internal override Vector2 GetWorldPointVelocity(Vector2 point) => GetPointVelocity(point - GetPose().Position);
    internal override Vector2 GetPointVelocity(Vector2 offset)
    {
        var arm = offset - CenterOfMass;
        var value = LinearVelocity + new Vector2(-arm.Y, arm.X) * AngularVelocity;
        PhysicsBodyRuntime.Finite(value); return value;
    }
    internal override void ApplyImpulse(Vector2 impulse, float moment) { GPU.ApplyImpulse(GPUHandle, impulse, moment); Space.InvalidateGPUStates(); }
    internal override void SetSurfaceVelocity(Vector2 linear, float angular)
    {
        if (_gpuSurfaceLinear == linear && _gpuSurfaceAngular == angular) return;
        GPU.SetSurfaceVelocity(GPUHandle, linear, angular);
        _gpuSurfaceLinear = linear; _gpuSurfaceAngular = angular; Space.InvalidateGPUStates();
    }
    internal override void CaptureViewContacts(PhysicsDirectBodyState view, int limit) => Space.CaptureGPUViewContacts(Owner, view, limit);
}
