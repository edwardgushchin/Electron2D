using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2WheelJoints;

namespace Electron2D;

/// <summary>Owns one CPU constraint's native handles, exact local frames and transient spring state.</summary>
internal sealed partial class CPUPhysicsJointImplementation(PhysicsSpace space, CPUPhysicsWorldBackend worldOwner, PhysicsJointBackend owner) : PhysicsJointImplementation(space)
{
    private B2BodyId BodyAID, BodyBID;
    private B2Transform _localFrameA, _localFrameB;
    internal B2JointId ID { get; private set; }
    internal override bool IsAttached => ID.index1 != 0;

    internal static Transform SampleLocalFrame(B2BodyId body, Vector2 point, float angle)
    {
        var local = b2Body_GetLocalPoint(body, PhysicsShapeBackend.ToBackend(point));
        return LocalFrame(local, b2Body_GetRotation(body), angle);
    }
    internal static Transform SampleDetachedLocalFrame(Transform pose, Vector2 point, float angle) =>
        LocalFrame(PhysicsShapeBackend.ToBackend(pose.AffineInverse() * point), PhysicsShapeBackend.RotationToBackend(pose.Rotation), angle);
    private static Transform LocalFrame(B2Vec2 local, B2Rot rotation, float angle)
    {
        if (!float.IsFinite(local.X) || !float.IsFinite(local.Y) ||
            Math.Sqrt((double)local.X * local.X + (double)local.Y * local.Y) > B2_HUGE)
            throw new ArgumentOutOfRangeException("point", "The local joint anchor exceeds the backend extent.");
        var relative = b2InvMulRot(rotation, PhysicsShapeBackend.RotationToBackend(angle));
        // Keep the sampled basis instead of decoding and rebuilding its angle on reattachment.
        return new(new(relative.c, relative.s), new(-relative.s, relative.c),
            new(local.X * PhysicsSpace.UnitsPerMeter, local.Y * PhysicsSpace.UnitsPerMeter));
    }

    internal override void Attach(PhysicsColliderBackend first, PhysicsColliderBackend? second, in PhysicsJointConfiguration settings)
    {
        if (IsAttached) throw new InvalidOperationException("A joint already has a backend attachment.");
        var space = Space;
        var definition = b2DefaultJointDef();
        _localFrameA = ToBackend(settings.FrameA); _localFrameB = ToBackend(settings.FrameB);
        BodyAID = worldOwner.NativeCollider(first).BodyID; BodyBID = second is null ? worldOwner.GetJointWorldBody() : worldOwner.NativeCollider(second).BodyID;
        definition.localFrameA = _localFrameA; definition.localFrameB = _localFrameB;
        definition.bodyIdA = BodyAID; definition.bodyIdB = BodyBID;
        definition.collideConnected = !settings.DisableCollision;
        switch (settings.Type)
        {
            case PhysicsServer.JointType.Pin:
                var pin = b2DefaultRevoluteJointDef();
                pin.@base = definition;
                pin.enableLimit = settings.PinLimitEnabled;
                pin.lowerAngle = settings.PinLimitEnabled ? settings.PinLimitLower : 0;
                pin.upperAngle = settings.PinLimitEnabled ? settings.PinLimitUpper : 0;
                pin.enableMotor = settings.PinMotorEnabled; pin.motorSpeed = settings.PinMotorVelocity; pin.maxMotorTorque = settings.PinMotorMaxTorque;
                ID = b2CreateRevoluteJoint(space.WorldID, pin);
                break;
            case PhysicsServer.JointType.Groove:
                var groove = b2DefaultWheelJointDef();
                groove.@base = definition; groove.enableSpring = false; groove.enableLimit = true;
                groove.lowerTranslation = settings.LowerTranslation * PhysicsSpace.MetersPerUnit;
                groove.upperTranslation = settings.UpperTranslation * PhysicsSpace.MetersPerUnit;
                ID = b2CreateWheelJoint(space.WorldID, groove);
                break;
            case PhysicsServer.JointType.DampedSpring:
                var spring = b2DefaultFilterJointDef(); spring.@base = definition;
                ID = b2CreateFilterJoint(space.WorldID, spring);
                break;
        }
        if (!IsAttached) throw new InvalidOperationException("The physics backend did not create the joint.");
        ApplySolverPolicy(settings);
    }

    internal override void ApplySolverPolicy(in PhysicsJointConfiguration settings)
    {
        var world = B2Worlds.b2GetWorld(ID.world0);
        var joint = b2GetJointSim(world, b2GetJointFullId(world, ID));
        var bias = settings.Bias == 0 ? Space.ConstraintDefaultBias : settings.Bias;
        var linearBias = MathF.Min(settings.MaxBias, 200) * PhysicsSpace.MetersPerUnit;
        var angularBias = MathF.Min(settings.MaxBias, 200);
        var linearForce = settings.MaxForce == float.MaxValue ? float.MaxValue : settings.MaxForce * PhysicsSpace.MetersPerUnit;
        var angularForce = settings.MaxForce == float.MaxValue ? float.MaxValue : settings.MaxForce * (PhysicsSpace.MetersPerUnit * PhysicsSpace.MetersPerUnit);
        var softness = settings.Type == PhysicsServer.JointType.Pin ? settings.PinSoftness : 0;
        if (joint.correctionBias == bias && joint.maxLinearBias == linearBias && joint.maxAngularBias == angularBias &&
            joint.maxLinearForce == linearForce && joint.maxAngularForce == angularForce && joint.linearSoftness == softness)
        {
            if (settings.Type == PhysicsServer.JointType.DampedSpring) b2Joint_WakeBodies(ID);
            return;
        }
        b2Joint_WakeBodies(ID);
        joint = b2GetJointSim(world, b2GetJointFullId(world, ID));
        joint.correctionBias = bias; joint.maxLinearBias = linearBias; joint.maxAngularBias = angularBias;
        joint.maxLinearForce = linearForce; joint.maxAngularForce = angularForce; joint.linearSoftness = softness;
        if (settings.Type == PhysicsServer.JointType.Pin)
        {
            ref var pin = ref joint.uj.revoluteJoint;
            pin.linearImpulse = default; pin.springImpulse = pin.motorImpulse = pin.lowerImpulse = pin.upperImpulse = 0;
        }
        else if (settings.Type == PhysicsServer.JointType.Groove)
        {
            ref var groove = ref joint.uj.wheelJoint;
            groove.perpImpulse = groove.springImpulse = groove.motorImpulse = groove.lowerImpulse = groove.upperImpulse = 0;
        }
    }

    internal override void ApplyPortableFrames(Transform first, Transform second)
    {
        _localFrameA = ToBackend(first); _localFrameB = ToBackend(second);
        b2Joint_SetLocalFrameA(ID, _localFrameA); b2Joint_SetLocalFrameB(ID, _localFrameB);
    }

    internal override void Detach()
    {
        try { if (IsAttached && !Space.HasBackendFailure) b2DestroyJoint(ID, wakeAttached: true); }
        finally
        {
            ID = default; BodyAID = BodyBID = default; _localFrameA = _localFrameB = default;
            _pointA = _pointB = _pendingImpulse = default;
            worldOwner.RetireJoint(owner, this);
        }
    }

    internal override void SetCollideConnected(bool value) { b2Joint_SetCollideConnected(ID, value); }
    internal override void SetPinLimits(bool enabled, float lower, float upper)
    {
        if (enabled) b2RevoluteJoint_SetLimits(ID, lower, upper);
        b2RevoluteJoint_EnableLimit(ID, enabled);
    }
    internal override void SetPinMotorEnabled(bool value) { b2RevoluteJoint_EnableMotor(ID, value); }
    internal override void SetPinMotorVelocity(float value) { b2RevoluteJoint_SetMotorSpeed(ID, value); }
    internal override void SetPinMotorMaxTorque(float value) { b2RevoluteJoint_SetMaxMotorTorque(ID, value); }
    internal override void SetGrooveLimits(float lower, float upper)
    {
        b2WheelJoint_SetLimits(ID, lower * PhysicsSpace.MetersPerUnit, upper * PhysicsSpace.MetersPerUnit);
    }

    private static B2Transform ToBackend(Transform frame) =>
        new(PhysicsShapeBackend.ToBackend(frame.Origin), new B2Rot(frame.X.X, frame.X.Y));
}
