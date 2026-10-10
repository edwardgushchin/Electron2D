using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

internal sealed partial class CPUPhysicsColliderImplementation
{
    private B2World? _world;
    private B2Body? _body;
    private B2Transform _savedPose;

    private static Vector2 ToScene(B2Vec2 value) => new(value.X * PhysicsSpace.UnitsPerMeter, value.Y * PhysicsSpace.UnitsPerMeter);

    internal override (Vector2 Position, float Rotation) GetPose()
    {
        var pose = b2GetBodyTransformQuick(_world!, _body!);
        return (ToScene(pose.p), b2Rot_GetAngle(pose.q));
    }

    /// <summary>Reads the current unit-scale pose using the CPU angle convention.</summary>
    internal override Transform GetTransform()
    {
        // The CPU adapter retains its published angle convention.
        var pose = GetPose(); return new(pose.Rotation, Vector2.One, 0, pose.Position);
    }

    internal override Transform SampleJointLocalFrame(Vector2 point, float angle) =>
        CPUPhysicsJointImplementation.SampleLocalFrame(Owner, default, point, angle);

    internal override (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion()
    {
        var state = b2GetBodyState(_world!, _body!);
        return (ToScene(state?.linearVelocity ?? default), state?.angularVelocity ?? 0, !IsAwake);
    }

    internal override Vector2 LinearVelocity => ToScene(b2Body_GetLinearVelocity(BodyID));
    internal override float AngularVelocity => b2Body_GetAngularVelocity(BodyID);
    internal override bool IsAwake => _contactReporting && HasMotionMode(PhysicsServer.BodyMode.Kinematic) || b2Body_IsAwake(BodyID);

    internal override void SetPose(Vector2 position, float rotation)
    {
        b2Body_SetTransform(BodyID, PhysicsShapeBackend.ToBackend(position), b2MakeRot(rotation));
    }
    internal override void SetTargetPose(Transform pose, double delta)
    {
        b2Body_SetTargetTransform(BodyID, new(PhysicsShapeBackend.ToBackend(pose.Origin), b2MakeRot(pose.Rotation)), (float)delta, wake: true);
    }
    internal override void SavePose() { _savedPose = b2Body_GetTransform(BodyID); }
    internal override void RestorePose() { b2Body_SetTransform(BodyID, _savedPose.p, _savedPose.q); }
    internal override void SetLinearVelocity(Vector2 velocity)
    {
        b2Body_SetLinearVelocity(BodyID, PhysicsShapeBackend.ToBackend(velocity));
    }
    internal override void SetAngularVelocity(float velocity)
    {
        b2Body_SetAngularVelocity(BodyID, velocity);
    }
    internal override void ClearVelocity()
    {
        b2Body_SetLinearVelocity(BodyID, default); b2Body_SetAngularVelocity(BodyID, 0);
    }
    internal override void SetAwake(bool awake)
    {
        b2Body_SetAwake(BodyID, awake);
    }
    internal override void SetCanSleep(bool canSleep)
    {
        b2Body_EnableSleep(BodyID, canSleep);
        if (canSleep && _body!.type == B2BodyType.b2_dynamicBody) PrepareSleepCapacity();
    }
    internal override void SetRotationLocked(bool locked)
    {
        b2Body_SetMotionLocks(BodyID, new(false, false, locked));
    }
    internal override void SetGravityScale(float scale)
    {
        b2Body_SetGravityScale(BodyID, scale);
    }
    internal override void ApplyCentralForce(Vector2 force, bool wake)
    {
        b2Body_ApplyForceToCenter(BodyID, PhysicsShapeBackend.ToBackend(force), wake);
    }
    internal override void ApplyTorque(float torque, bool wake)
    {
        b2Body_ApplyTorque(BodyID, torque * PhysicsMass.InertiaScale, wake);
    }
}
