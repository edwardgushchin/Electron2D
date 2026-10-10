using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    private B2World? _world;
    private B2Body? _body;
    private B2Transform _savedPose;

    private static Vector2 ToScene(B2Vec2 value) => new(value.X * PhysicsSpace.UnitsPerMeter, value.Y * PhysicsSpace.UnitsPerMeter);

    internal (Vector2 Position, float Rotation) GetPose()
    {
        if (GPU is not null) return (GPUState.Position, GPUState.Rotation);
        var pose = b2GetBodyTransformQuick(_world!, _body!);
        return (ToScene(pose.p), b2Rot_GetAngle(pose.q));
    }

    /// <summary>Reads the current unit-scale pose, preserving the resident GPU basis and the CPU angle convention.</summary>
    internal Transform GetTransform()
    {
        if (GPU is not null && !Space!.DecodeGPUTransforms)
        {
            ref readonly var state = ref GPUState;
            return new(new(state.Pose.Z, state.Pose.W), new(-state.Pose.W, state.Pose.Z), state.Position);
        }
        // The CPU adapter retains its published angle convention.
        var pose = GetPose(); return new(pose.Rotation, Vector2.One, 0, pose.Position);
    }

    internal (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion()
    {
        if (GPU is not null)
        {
            ref readonly var snapshot = ref GPUState;
            return (new Vector2(snapshot.Velocity.X, snapshot.Velocity.Y) - _gpuSurfaceLinear,
                snapshot.Velocity.Z - _gpuSurfaceAngular, !IsAwake);
        }
        var state = b2GetBodyState(_world!, _body!);
        return (ToScene(state?.linearVelocity ?? default), state?.angularVelocity ?? 0, !IsAwake);
    }

    internal Vector2 LinearVelocity => GPU is not null ? new(GPUState.Velocity.X, GPUState.Velocity.Y) : ToScene(b2Body_GetLinearVelocity(BodyID));
    internal float AngularVelocity => GPU is not null ? GPUState.Velocity.Z : b2Body_GetAngularVelocity(BodyID);
    internal bool IsAwake => GPU is not null ? !HasMotionMode(PhysicsServer.BodyMode.Static) && !GPUState.Sleeping :
        _contactReporting && HasMotionMode(PhysicsServer.BodyMode.Kinematic) || b2Body_IsAwake(BodyID);

    internal void SetPose(Vector2 position, float rotation)
    {
        if (GPU is { } gpu) { gpu.SetPose(GPUHandle, position, rotation); Space!.InvalidateGPUStates(); }
        else b2Body_SetTransform(BodyID, PhysicsShapeBackend.ToBackend(position), b2MakeRot(rotation));
    }
    internal void SetPose(Transform pose) => SetPose(pose.Origin, pose.Rotation);
    internal void SetTargetPose(Transform pose, double delta)
    {
        if (GPU is { } gpu) gpu.SetKinematicTarget(GPUHandle, pose.Origin, pose.Rotation);
        else b2Body_SetTargetTransform(BodyID, new(PhysicsShapeBackend.ToBackend(pose.Origin), b2MakeRot(pose.Rotation)), (float)delta, wake: true);
    }
    internal void SavePose() { if (GPU is not null) _gpuSavedPose = GetPose(); else _savedPose = b2Body_GetTransform(BodyID); }
    internal void RestorePose() { if (GPU is not null) SetPose(_gpuSavedPose.Position, _gpuSavedPose.Rotation); else b2Body_SetTransform(BodyID, _savedPose.p, _savedPose.q); }
    internal void SetLinearVelocity(Vector2 velocity)
    {
        if (GPU is { } gpu) { gpu.SetSolverLinearVelocity(GPUHandle, velocity); Space!.InvalidateGPUStates(); }
        else b2Body_SetLinearVelocity(BodyID, PhysicsShapeBackend.ToBackend(velocity));
    }
    internal void SetAngularVelocity(float velocity)
    {
        if (GPU is { } gpu) { gpu.SetSolverAngularVelocity(GPUHandle, velocity); Space!.InvalidateGPUStates(); }
        else b2Body_SetAngularVelocity(BodyID, velocity);
    }
    internal void ClearVelocity()
    {
        if (GPU is { } gpu)
        {
            if (HasMotionMode(PhysicsServer.BodyMode.Kinematic)) gpu.ClearKinematicVelocity(GPUHandle);
            else gpu.SetSolverVelocity(GPUHandle, default, 0);
            Space!.InvalidateGPUStates();
        }
        else { b2Body_SetLinearVelocity(BodyID, default); b2Body_SetAngularVelocity(BodyID, 0); }
    }
    internal void SetAwake(bool awake)
    {
        if (GPU is { } gpu) { if (!awake) Space!.FlushGPUWakes(); gpu.SetSleeping(GPUHandle, !awake); if (awake) Space!.InvalidateGPUStates(); else GPUStateValid = false; }
        else b2Body_SetAwake(BodyID, awake);
    }
    internal void SetCanSleep(bool canSleep)
    {
        if (GPU is { } gpu) { gpu.SetCanSleep(GPUHandle, canSleep); Space!.InvalidateGPUStates(); }
        else b2Body_EnableSleep(BodyID, canSleep);
    }
    internal void SetRotationLocked(bool locked)
    {
        if (GPU is { } gpu) { gpu.SetIntegrationPolicy(GPUHandle, gpu.GetIntegrationPolicy(GPUHandle) with { LockRotation = locked }); _gpuParametersPrepared = false; Space!.InvalidateGPUStates(); }
        else b2Body_SetMotionLocks(BodyID, new(false, false, locked));
    }
    internal void SetGravityScale(float scale)
    {
        if (GPU is { } gpu) { gpu.SetIntegrationPolicy(GPUHandle, gpu.GetIntegrationPolicy(GPUHandle) with { GravityScale = scale }); _gpuParametersPrepared = false; Space!.InvalidateGPUStates(); }
        else b2Body_SetGravityScale(BodyID, scale);
    }
    internal void ApplyCentralForce(Vector2 force, bool wake)
    {
        if (GPU is { } gpu) { if (wake) SetAwake(true); gpu.ApplyForce(GPUHandle, force); }
        else b2Body_ApplyForceToCenter(BodyID, PhysicsShapeBackend.ToBackend(force), wake);
    }
    internal void ApplyTorque(float torque, bool wake)
    {
        if (GPU is { } gpu) { if (wake) SetAwake(true); gpu.ApplyForce(GPUHandle, default, torque); }
        else b2Body_ApplyTorque(BodyID, torque * PhysicsMass.InertiaScale, wake);
    }
}
