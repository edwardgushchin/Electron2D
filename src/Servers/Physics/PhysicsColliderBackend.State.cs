namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal (Vector2 Position, float Rotation) GetPose() => Attached.GetPose();
    internal Transform GetTransform() => Attached.GetTransform();
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
    internal void SetGravityScale(float scale) => Attached.SetGravityScale(scale);
    internal void ApplyCentralForce(Vector2 force, bool wake) => Attached.ApplyCentralForce(force, wake);
    internal void ApplyTorque(float torque, bool wake) => Attached.ApplyTorque(torque, wake);
}
