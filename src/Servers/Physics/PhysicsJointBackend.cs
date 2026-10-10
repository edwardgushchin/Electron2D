using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2WheelJoints;

namespace Electron2D;

/// <summary>Owns the selected physics joint attachment and adapts shared engine settings to solver operations.</summary>
internal sealed partial class PhysicsJointBackend
{
    private PhysicsSpace? _space;
    private B2BodyId BodyAID, BodyBID;
    private B2Transform _localFrameA, _localFrameB;
    internal B2JointId ID { get; private set; }
    internal bool IsAttached => ID.index1 != 0 || _gpuJoint.Generation != 0;

    internal static Transform SampleLocalFrame(PhysicsColliderBackend? body, Transform pose, Vector2 point, float angle)
    {
        if (body?.Space?.GPUStore is not null)
        {
            var current = body.GetTransform();
            var pointLocal = current.AffineInverse() * point;
            PhysicsJointRuntime.ValidateExtent(pointLocal.Length());
            return new Transform(angle - current.Rotation, Vector2.One, 0, pointLocal);
        }
        var attached = body?.Space is not null;
        var local = attached ? b2Body_GetLocalPoint(body!.BodyID, PhysicsShapeBackend.ToBackend(point)) :
            PhysicsShapeBackend.ToBackend(pose.AffineInverse() * point);
        if (!float.IsFinite(local.X) || !float.IsFinite(local.Y) ||
            Math.Sqrt((double)local.X * local.X + (double)local.Y * local.Y) > B2_HUGE)
            throw new ArgumentOutOfRangeException(nameof(point), "The local joint anchor exceeds the backend extent.");
        var rotation = attached ? b2Body_GetRotation(body!.BodyID) : b2MakeRot(pose.Rotation);
        var relative = b2InvMulRot(rotation, b2MakeRot(angle));
        // Keep the sampled basis instead of decoding and rebuilding its angle on reattachment.
        return new(new(relative.c, relative.s), new(-relative.s, relative.c),
            new(local.X * PhysicsSpace.UnitsPerMeter, local.Y * PhysicsSpace.UnitsPerMeter));
    }

    internal void Attach(PhysicsSpace space, PhysicsColliderBackend first, PhysicsColliderBackend? second, PhysicsJointRuntime settings)
    {
        if (IsAttached) throw new InvalidOperationException("A joint already has a backend attachment.");
        if (space.GPUStore is not null) { AttachGPU(space, first, second, settings); return; }
        var definition = b2DefaultJointDef();
        _localFrameA = ToBackend(settings.FrameA); _localFrameB = ToBackend(settings.FrameB);
        BodyAID = first.BodyID; BodyBID = second?.BodyID ?? space.GetJointWorldBody();
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
        _space = space;
        ApplySolverPolicy(settings);
    }

    internal void ApplySolverPolicy(PhysicsJointRuntime settings)
    {
        if (_space?.GPUStore is not null) { SetGPUJoint(GPUDefinition(settings)); return; }
        var world = B2Worlds.b2GetWorld(ID.world0);
        var joint = b2GetJointSim(world, b2GetJointFullId(world, ID));
        var bias = settings.Bias == 0 ? _space!.ConstraintDefaultBias : settings.Bias;
        var linearBias = MathF.Min(settings.MaxBias, 200) * PhysicsSpace.MetersPerUnit;
        var angularBias = MathF.Min(settings.MaxBias, 200);
        var linearForce = settings.MaxForce == float.MaxValue ? float.MaxValue : settings.MaxForce * PhysicsSpace.MetersPerUnit;
        var angularForce = settings.MaxForce == float.MaxValue ? float.MaxValue : settings.MaxForce * (PhysicsSpace.MetersPerUnit * PhysicsSpace.MetersPerUnit);
        var softness = settings.Type == PhysicsServer.JointType.Pin ? settings.PinSoftness : 0;
        if (joint.correctionBias == bias && joint.maxLinearBias == linearBias && joint.maxAngularBias == angularBias &&
            joint.maxLinearForce == linearForce && joint.maxAngularForce == angularForce && joint.linearSoftness == softness) return;
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

    internal void ApplyPortableFrames(Transform first, Transform second)
    {
        if (_space?.GPUStore is not null)
        {
            SetGPUJoint(_gpuDefinition with { FrameA = first, FrameB = second });
            return;
        }
        _localFrameA = ToBackend(first); _localFrameB = ToBackend(second);
        b2Joint_SetLocalFrameA(ID, _localFrameA); b2Joint_SetLocalFrameB(ID, _localFrameB);
    }

    internal void Detach()
    {
        if (_gpuJoint.Generation != 0)
        {
            if (_space?.HasBackendFailure != true) _space!.GPUStore!.RemoveJoint(_gpuJoint);
            _gpuJoint = default; _gpuDefinition = default;
        }
        else if (IsAttached && _space?.HasBackendFailure != true) b2DestroyJoint(ID, wakeAttached: true);
        ID = default; BodyAID = BodyBID = default;
        _localFrameA = _localFrameB = default;
        _pointA = _pointB = _pendingImpulse = default;
        _space = null;
    }

    internal void SetCollideConnected(bool value) { if (_space?.GPUStore is not null) SetGPUJoint(_gpuDefinition with { DisableCollision = !value }); else b2Joint_SetCollideConnected(ID, value); }
    internal void SetPinLimits(bool enabled, float lower, float upper)
    {
        if (_space?.GPUStore is not null) { SetGPUJoint(_gpuDefinition with { LimitEnabled = enabled, LowerAngle = enabled ? lower : 0, UpperAngle = enabled ? upper : 0 }); return; }
        if (enabled) b2RevoluteJoint_SetLimits(ID, lower, upper);
        b2RevoluteJoint_EnableLimit(ID, enabled);
    }
    internal void SetPinMotorEnabled(bool value) { if (_space?.GPUStore is not null) SetGPUJoint(_gpuDefinition with { MotorEnabled = value }); else b2RevoluteJoint_EnableMotor(ID, value); }
    internal void SetPinMotorVelocity(float value) { if (_space?.GPUStore is not null) SetGPUJoint(_gpuDefinition with { MotorVelocity = value }); else b2RevoluteJoint_SetMotorSpeed(ID, value); }
    internal void SetPinMotorMaxTorque(float value) { if (_space?.GPUStore is not null) SetGPUJoint(_gpuDefinition with { MotorMaxTorque = value }); else b2RevoluteJoint_SetMaxMotorTorque(ID, value); }
    internal void SetGrooveLimits(float lower, float upper)
    {
        if (_space?.GPUStore is not null) SetGPUJoint(_gpuDefinition with { LowerTranslation = lower, UpperTranslation = upper });
        else b2WheelJoint_SetLimits(ID, lower * PhysicsSpace.MetersPerUnit, upper * PhysicsSpace.MetersPerUnit);
    }

    private static B2Transform ToBackend(Transform frame) =>
        new(PhysicsShapeBackend.ToBackend(frame.Origin), new B2Rot(frame.X.X, frame.X.Y));
}
