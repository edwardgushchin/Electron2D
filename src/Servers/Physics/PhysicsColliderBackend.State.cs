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
        var pose = b2GetBodyTransformQuick(_world!, _body!);
        return (ToScene(pose.p), b2Rot_GetAngle(pose.q));
    }

    internal (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion()
    {
        var state = b2GetBodyState(_world!, _body!);
        return (ToScene(state?.linearVelocity ?? default), state?.angularVelocity ?? 0, state is null);
    }

    internal Vector2 LinearVelocity => ToScene(b2Body_GetLinearVelocity(BodyID));
    internal float AngularVelocity => b2Body_GetAngularVelocity(BodyID);
    internal bool IsAwake => b2Body_IsAwake(BodyID);

    internal void SetPose(Vector2 position, float rotation) => b2Body_SetTransform(BodyID, PhysicsShapeBackend.ToBackend(position), b2MakeRot(rotation));
    internal void SetPose(Transform pose) => SetPose(pose.Origin, pose.Rotation);
    internal void SetTargetPose(Transform pose, double delta) =>
        b2Body_SetTargetTransform(BodyID, new(PhysicsShapeBackend.ToBackend(pose.Origin), b2MakeRot(pose.Rotation)), (float)delta, wake: true);

    internal void SavePose() => _savedPose = b2Body_GetTransform(BodyID);
    internal void RestorePose() => b2Body_SetTransform(BodyID, _savedPose.p, _savedPose.q);

    internal void SetLinearVelocity(Vector2 velocity) => b2Body_SetLinearVelocity(BodyID, PhysicsShapeBackend.ToBackend(velocity));
    internal void SetAngularVelocity(float velocity) => b2Body_SetAngularVelocity(BodyID, velocity);
    internal void ClearVelocity() { b2Body_SetLinearVelocity(BodyID, default); b2Body_SetAngularVelocity(BodyID, 0); }
    internal void SetAwake(bool awake) => b2Body_SetAwake(BodyID, awake);
    internal void SetCanSleep(bool canSleep) => b2Body_EnableSleep(BodyID, canSleep);
    internal void SetRotationLocked(bool locked) => b2Body_SetMotionLocks(BodyID, new(false, false, locked));
    internal void SetGravityScale(float scale) => b2Body_SetGravityScale(BodyID, scale);
    internal void ApplyCentralForce(Vector2 force, bool wake) => b2Body_ApplyForceToCenter(BodyID, PhysicsShapeBackend.ToBackend(force), wake);
    internal void ApplyTorque(float torque, bool wake) => b2Body_ApplyTorque(BodyID, torque * PhysicsMass.InertiaScale, wake);
}
