namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    private sealed class MotionPublication(Vector2? linear, float? angular, (Vector2 Linear, float Angular)? surface, bool? awake, bool? canSleep)
    {
        internal readonly Vector2? Linear = linear;
        internal readonly float? Angular = angular;
        internal readonly (Vector2 Linear, float Angular)? Surface = surface;
        internal readonly bool? Awake = awake, CanSleep = canSleep;
    }
    private MotionPublication? _pendingMotion;
    private bool _publishingMotion;
    internal void DiscardPendingMotion() => _pendingMotion = null;

    internal void PublishMotion(Vector2? linear = null, float? angular = null, (Vector2 Linear, float Angular)? surface = null,
        bool? awake = null, bool? canSleep = null)
    {
        PreparePendingMotion();
        if (_publishingMotion) throw new InvalidOperationException("Motion publication cannot reenter its own write.");
        _publishingMotion = true;
        try { ApplyMotion(linear, angular, surface, awake, canSleep); }
        catch { _pendingMotion = new(linear, angular, surface, awake, canSleep); Space?.MarkColliderMetadataDirty(); throw; }
        finally { _publishingMotion = false; }
    }
    internal void PreparePendingMotion()
    {
        if (_pendingMotion is not { } pending) return;
        if (_publishingMotion) throw new InvalidOperationException("Motion publication cannot reenter its own write.");
        _publishingMotion = true;
        try { ApplyMotion(pending.Linear, pending.Angular, pending.Surface, pending.Awake, pending.CanSleep); _pendingMotion = null; }
        finally { _publishingMotion = false; }
    }
    private void ApplyMotion(Vector2? linear, float? angular, (Vector2 Linear, float Angular)? surface, bool? awake, bool? canSleep)
    {
        if (canSleep is { } permission) Attached.SetCanSleep(permission);
        if (linear is { } velocity) Attached.SetLinearVelocity(velocity);
        if (angular is { } rotation) Attached.SetAngularVelocity(rotation);
        if (surface is { } virtualVelocity) Attached.SetSurfaceVelocity(virtualVelocity.Linear, virtualVelocity.Angular);
        if (awake is { } active) Attached.SetAwake(active);
    }
    internal (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetRetainedSolverMotion()
    {
        var motion = Attached.GetSolverMotion();
        if (_pendingMotion is not { } pending) return motion;
        return (pending.Awake == false ? default : pending.Linear ?? motion.LinearVelocity,
            pending.Awake == false ? 0 : pending.Angular ?? motion.AngularVelocity,
            pending.Awake is { } awake ? !awake : pending.CanSleep == false ? false : motion.Sleeping);
    }

    internal (Vector2 Position, float Rotation) GetPose() => Attached.GetPose();
    internal Transform GetTransform() => Attached.GetTransform();
    internal Transform SampleJointLocalFrame(Vector2 point, float angle) => Attached.SampleJointLocalFrame(point, angle);
    internal (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion() => Attached.GetSolverMotion();
    internal Vector2 LinearVelocity => Attached.LinearVelocity;
    internal float AngularVelocity => Attached.AngularVelocity;
    internal bool IsAwake => Attached.IsAwake;
    internal void SetPose(Vector2 position, float rotation) => Attached.SetPose(position, rotation);
    internal void SetPose(Transform pose) => SetPose(pose.Origin, pose.Rotation);
    internal void SetTargetPose(Transform pose, double delta) => Attached.SetTargetPose(pose, delta);
    internal void SavePose() => Attached.SavePose();
    internal void RestorePose() => Attached.RestorePose();
    internal void SetLinearVelocity(Vector2 velocity) => Attached.SetLinearVelocity(velocity);
    internal void SetAngularVelocity(float velocity) => Attached.SetAngularVelocity(velocity);
    internal void ClearVelocity() => Attached.ClearVelocity();
    internal void SetAwake(bool awake) => Attached.SetAwake(awake);
    internal void SetCanSleep(bool canSleep) => Attached.SetCanSleep(canSleep);
    internal void SetRotationLocked(bool locked) => Attached.SetRotationLocked(locked);
    private float? _pendingGravityScale;
    private bool _publishingGravityScale;
    internal void SetGravityScale(float scale)
    {
        if (_publishingGravityScale) throw new InvalidOperationException("Gravity publication cannot reenter its own write.");
        _publishingGravityScale = true;
        try { Attached.SetGravityScale(scale); _pendingGravityScale = null; }
        catch { _pendingGravityScale = scale; Space?.MarkColliderMetadataDirty(); throw; }
        finally { _publishingGravityScale = false; }
    }
    internal void PreparePendingGravityScale()
    {
        if (_publishingGravityScale) throw new InvalidOperationException("Gravity publication cannot reenter its own write.");
        if (_pendingGravityScale is not { } scale) return;
        Space?.EnsureImplementationWriteAccess(); Space?.EnsureBackendAccess();
        SetGravityScale(scale);
    }
    internal void ApplyCentralForce(Vector2 force, bool wake) => Attached.ApplyCentralForce(force, wake);
    internal void ApplyTorque(float torque, bool wake) => Attached.ApplyTorque(torque, wake);
}
