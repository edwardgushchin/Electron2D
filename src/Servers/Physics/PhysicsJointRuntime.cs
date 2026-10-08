using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2WheelJoints;

namespace Electron2D;

internal sealed partial class PhysicsJointRuntime(RID rid, Joint? scene = null, PhysicsServer.JointType? declaredType = null)
{
    internal RID RID { get; } = rid;
    internal WeakReference<Joint>? Scene { get; } = scene is null ? null : new(scene);
    internal PhysicsServer.JointType? DeclaredType { get; } = declaredType;
    internal PhysicsServer.JointType Type { get; private set; } = PhysicsServer.JointType.Empty;
    internal PhysicsSpace? Space { get; private set; }
    internal RID BodyA { get; private set; }
    internal RID BodyB { get; private set; }
    internal B2JointId BackendID { get; private set; }
    internal B2BodyId BodyAID { get; private set; }
    internal B2BodyId BodyBID { get; private set; }
    private B2JointDef _definition;
    private float _lowerTranslation;
    private float _upperTranslation;
    internal bool DisableCollision { get; private set; } = true;
    internal bool PinLimitEnabled { get; private set; }
    internal float PinLimitLower { get; private set; }
    internal float PinLimitUpper { get; private set; }
    internal bool PinMotorEnabled { get; private set; }
    internal float PinMotorVelocity { get; private set; }
    internal float PinMotorMaxTorque { get; private set; } = 10;
    internal float SpringRestLength { get; private set; }
    internal float SpringStiffness { get; private set; } = 20;
    internal float SpringDamping { get; private set; } = 1;
    internal bool SpringAutomaticRest { get; private set; } = scene is not null;
    internal float SpringAutomaticLength { get; set; } = 50;
    internal float EffectiveSpringRestLength => SpringAutomaticRest ? MathF.Abs(SpringAutomaticLength) : SpringRestLength;

    internal void EnsureAccess(bool releasing = false)
    {
        if (releasing)
        {
            Space?.EnsureReleaseAccess();
            if (BodyA.IsValid()) BodySpace(BodyA)?.EnsureReleaseAccess();
            if (BodyB.IsValid()) BodySpace(BodyB)?.EnsureReleaseAccess();
            return;
        }
        Space?.EnsureQueryAccess();
        if (BodyA.IsValid()) BodySpace(BodyA)?.EnsureQueryAccess();
        if (BodyB.IsValid()) BodySpace(BodyB)?.EnsureQueryAccess();
    }

    internal void Require(PhysicsServer.JointType type)
    {
        if (Type != type && !(Type == PhysicsServer.JointType.Empty && DeclaredType == type))
            throw new ArgumentException("The joint does not have the requested concrete role.", nameof(type));
    }

    internal void ConfigurePin(Vector2 anchor, RID first, RID second, bool preserve = false) =>
        Configure(PhysicsServer.JointType.Pin, anchor, anchor, 0, 0, 0, first, second, preserve);

    internal void ConfigureGroove(Vector2 start, Vector2 end, Vector2 anchor, RID first, RID second, bool preserve = false)
    {
        var delta = end - start;
        var length = Length(delta);
        ValidateExtent(length);
        Configure(PhysicsServer.JointType.Groove, start, anchor, length == 0 ? MathF.PI / 2 : delta.Angle(),
            0, length, first, second, preserve);
    }

    internal void ConfigureSceneGroove(Transform transform, float length, float offset, RID first, RID second) =>
        Configure(PhysicsServer.JointType.Groove, transform.Origin, transform * new Vector2(0, offset),
            transform.Rotation + MathF.PI / 2, MathF.Min(0, length), MathF.Max(0, length), first, second, true);

    internal void ConfigureSpring(Vector2 anchorA, Vector2 anchorB, RID first, RID second, bool preserve = false) =>
        Configure(PhysicsServer.JointType.DampedSpring, anchorA, anchorB, 0, 0, 0, first, second, preserve);

    private void Configure(PhysicsServer.JointType type, Vector2 anchorA, Vector2 anchorB, float angle,
        float lower, float upper, RID first, RID second, bool preserve)
    {
        if (!anchorA.IsFinite() || !anchorB.IsFinite()) throw new ArgumentOutOfRangeException(nameof(anchorA));
        if (first == second || !first.IsValid()) throw new ArgumentException("Joint endpoints must be distinct live bodies.", nameof(first));
        if (!second.IsValid() && type != PhysicsServer.JointType.Pin)
            throw new ArgumentException("This joint requires two live physics bodies.", nameof(second));
        if (DeclaredType is { } declared && declared != type)
            throw new InvalidOperationException("A scene joint cannot change its concrete node role through the server.");
        var a = Snapshot(first);
        var b = second.IsValid() ? Snapshot(second) : (Space: (PhysicsSpace?)null, ID: default(B2BodyId), Pose: Transform.Identity);
        if (a.Space is not null && b.Space is not null && !ReferenceEquals(a.Space, b.Space))
            throw new ArgumentException("Joint bodies must belong to the same physics space.", nameof(second));
        var nextSpace = a.Space ?? b.Space;
        if (Scene is { } weak && weak.TryGetTarget(out var owner) && owner.AttachmentSpace is { } sceneSpace &&
            nextSpace is not null && !ReferenceEquals(sceneSpace, nextSpace))
            throw new InvalidOperationException("A scene joint belongs to its scene physics world.");
        ValidatePose(a.Pose); ValidatePose(b.Pose);
        var definition = b2DefaultJointDef();
        definition.localFrameA.p = LocalPoint(a, anchorA);
        definition.localFrameB.p = LocalPoint(b, anchorB);
        definition.localFrameA.q = b2InvMulRot(Rotation(a), b2MakeRot(angle));
        definition.localFrameB.q = b2InvMulRot(Rotation(b), b2MakeRot(angle));
        definition.collideConnected = !DisableCollision;
        var rest = Length(anchorB - anchorA);
        if (type is PhysicsServer.JointType.DampedSpring or PhysicsServer.JointType.Groove) ValidateExtent(rest);
        DetachSpace();
        if (!preserve)
        {
            PinLimitEnabled = PinMotorEnabled = false;
            PinLimitLower = PinLimitUpper = PinMotorVelocity = 0;
            PinMotorMaxTorque = 10;
            SpringRestLength = rest; SpringAutomaticRest = false;
            SpringStiffness = 20; SpringDamping = 1.5f;
        }
        Type = type; BodyA = first; BodyB = second; _definition = definition;
        _lowerTranslation = lower * PhysicsSpace.MetersPerUnit;
        _upperTranslation = upper * PhysicsSpace.MetersPerUnit;
        RefreshSpace();
    }

    internal void RefreshSpace()
    {
        if (Type == PhysicsServer.JointType.Empty) return;
        (PhysicsSpace? Space, B2BodyId ID, Transform Pose) a, b;
        try
        {
            a = Snapshot(BodyA);
            b = BodyB.IsValid() ? Snapshot(BodyB) : (null, default, Transform.Identity);
        }
        catch (ArgumentException) { Clear(); return; }
        var next = a.Space ?? b.Space;
        if (Scene is { } weak && weak.TryGetTarget(out var owner) && owner.AttachmentSpace is { } sceneSpace &&
            next is not null && !ReferenceEquals(sceneSpace, next)) next = null;
        if (!ReferenceEquals(Space, next))
        {
            DetachSpace();
            Space = next;
            next?.AddJointRuntime(this);
        }
        if (BackendID.index1 != 0 || next is null || a.Space is null || BodyB.IsValid() && !ReferenceEquals(a.Space, b.Space)) return;
        BodyAID = a.ID;
        BodyBID = BodyB.IsValid() ? b.ID : next.GetJointWorldBody();
        var definition = _definition;
        definition.bodyIdA = BodyAID; definition.bodyIdB = BodyBID;
        definition.collideConnected = !DisableCollision;
        switch (Type)
        {
            case PhysicsServer.JointType.Pin:
                var pin = b2DefaultRevoluteJointDef();
                pin.@base = definition;
                pin.enableLimit = PinLimitEnabled;
                pin.lowerAngle = PinLimitEnabled ? PinLimitLower : 0;
                pin.upperAngle = PinLimitEnabled ? PinLimitUpper : 0;
                pin.enableMotor = PinMotorEnabled; pin.motorSpeed = PinMotorVelocity; pin.maxMotorTorque = PinMotorMaxTorque;
                BackendID = b2CreateRevoluteJoint(next.WorldID, pin);
                break;
            case PhysicsServer.JointType.Groove:
                var groove = b2DefaultWheelJointDef();
                groove.@base = definition; groove.enableSpring = false; groove.enableLimit = true;
                groove.lowerTranslation = _lowerTranslation; groove.upperTranslation = _upperTranslation;
                BackendID = b2CreateWheelJoint(next.WorldID, groove);
                break;
            case PhysicsServer.JointType.DampedSpring:
                var spring = b2DefaultFilterJointDef(); spring.@base = definition;
                BackendID = b2CreateFilterJoint(next.WorldID, spring);
                break;
        }
        if (BackendID.index1 == 0) throw new InvalidOperationException("The physics backend did not create the joint.");
        if (DisableCollision && BodyB.IsValid()) PhysicsServer.Service.JointCollisionContribution(BodyA, BodyB, add: true);
    }

    internal void BodyLeaving(RID body)
    {
        if (body == BodyA || body == BodyB) DetachNative();
    }

    internal void Clear()
    {
        DetachSpace(); Type = PhysicsServer.JointType.Empty; BodyA = BodyB = default;
    }

    internal void DetachSpace()
    {
        DetachNative();
        var old = Space; Space = null;
        old?.RemoveJointRuntime(this);
    }

    private void DetachNative()
    {
        if (BackendID.index1 != 0)
        {
            if (DisableCollision && BodyB.IsValid()) PhysicsServer.Service.JointCollisionContribution(BodyA, BodyB, add: false);
            if (Space?.HasBackendFailure != true) b2DestroyJoint(BackendID, wakeAttached: true);
        }
        BackendID = default; BodyAID = BodyBID = default;
    }

    internal void SetDisableCollision(bool value)
    {
        if (DisableCollision == value) return;
        if (BackendID.index1 != 0 && DisableCollision && BodyB.IsValid())
            PhysicsServer.Service.JointCollisionContribution(BodyA, BodyB, add: false);
        DisableCollision = value;
        if (BackendID.index1 != 0) b2Joint_SetCollideConnected(BackendID, !value);
        if (BackendID.index1 != 0 && value && BodyB.IsValid())
            PhysicsServer.Service.JointCollisionContribution(BodyA, BodyB, add: true);
        if (BodyA.IsValid()) MarkBodyDirty(BodyA);
        if (BodyB.IsValid()) MarkBodyDirty(BodyB);
    }

    internal void SetPinLimitEnabled(bool value)
    {
        if (value) ValidateLimits(PinLimitLower, PinLimitUpper);
        PinLimitEnabled = value; ApplyPinLimits();
    }
    internal void SetPinLimitLower(float value)
    {
        Finite(value); if (PinLimitEnabled) ValidateLimits(value, PinLimitUpper);
        PinLimitLower = value; ApplyPinLimits();
    }
    internal void SetPinLimitUpper(float value)
    {
        Finite(value); if (PinLimitEnabled) ValidateLimits(PinLimitLower, value);
        PinLimitUpper = value; ApplyPinLimits();
    }
    private void ApplyPinLimits()
    {
        if (BackendID.index1 == 0 || Type != PhysicsServer.JointType.Pin) return;
        if (PinLimitEnabled) b2RevoluteJoint_SetLimits(BackendID, PinLimitLower, PinLimitUpper);
        b2RevoluteJoint_EnableLimit(BackendID, PinLimitEnabled);
    }
    internal void SetPinMotorEnabled(bool value)
    {
        PinMotorEnabled = value;
        if (BackendID.index1 != 0 && Type == PhysicsServer.JointType.Pin) b2RevoluteJoint_EnableMotor(BackendID, value);
    }
    internal void SetPinMotorVelocity(float value)
    {
        Finite(value); PinMotorVelocity = value;
        if (BackendID.index1 != 0 && Type == PhysicsServer.JointType.Pin) b2RevoluteJoint_SetMotorSpeed(BackendID, value);
    }
    internal void SetPinMotorMaxTorque(float value)
    {
        Coefficient(value); PinMotorMaxTorque = value;
        if (BackendID.index1 != 0 && Type == PhysicsServer.JointType.Pin) b2RevoluteJoint_SetMaxMotorTorque(BackendID, value);
    }
    internal void SetGrooveLength(float value)
    {
        ValidateExtent(value);
        _lowerTranslation = MathF.Min(0, value) * PhysicsSpace.MetersPerUnit;
        _upperTranslation = MathF.Max(0, value) * PhysicsSpace.MetersPerUnit;
        if (BackendID.index1 != 0 && Type == PhysicsServer.JointType.Groove)
            b2WheelJoint_SetLimits(BackendID, _lowerTranslation, _upperTranslation);
    }
    internal void SetSpringRestLength(float value, bool automatic = false)
    {
        ValidateExtent(value); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        SpringRestLength = value; SpringAutomaticRest = automatic && value == 0;
    }
    internal void SetSpringStiffness(float value) { Coefficient(value); SpringStiffness = value; }
    internal void SetSpringDamping(float value) { Coefficient(value); SpringDamping = value; }

    private static (PhysicsSpace? Space, B2BodyId ID, Transform Pose) Snapshot(RID body)
    {
        var owners = PhysicsServer.Service.ResolveBodyOwners(body);
        return owners.Scene is { } scene ? (scene.Space, scene.BackendID, scene.GlobalTransform) :
            (owners.Server!.Space, owners.Server.BackendID, owners.Server.GetTransform());
    }
    private static PhysicsSpace? BodySpace(RID body)
    {
        try
        {
            var owners = PhysicsServer.Service.ResolveBodyOwners(body);
            return owners.Scene?.Space ?? owners.Server?.Space;
        }
        catch (ArgumentException) { return null; }
    }
    private static B2Vec2 LocalPoint((PhysicsSpace? Space, B2BodyId ID, Transform Pose) body, Vector2 point)
    {
        var local = body.Space is null ? PhysicsShapeBackend.ToBackend(body.Pose.AffineInverse() * point) :
            b2Body_GetLocalPoint(body.ID, PhysicsShapeBackend.ToBackend(point));
        if (!float.IsFinite(local.X) || !float.IsFinite(local.Y) ||
            Math.Sqrt((double)local.X * local.X + (double)local.Y * local.Y) > B2_HUGE)
            throw new ArgumentOutOfRangeException(nameof(point), "The local joint anchor exceeds the backend extent.");
        return local;
    }
    private static B2Rot Rotation((PhysicsSpace? Space, B2BodyId ID, Transform Pose) body) =>
        body.Space is null ? b2MakeRot(body.Pose.Rotation) : b2Body_GetRotation(body.ID);
    private static void MarkBodyDirty(RID rid)
    {
        try
        {
            var owners = PhysicsServer.Service.ResolveBodyOwners(rid);
            if (owners.Scene is { } scene) scene.MarkShapesDirty(); else owners.Server!.MarkShapesDirty();
        }
        catch (ArgumentException) { }
    }
    private static void ValidatePose(Transform pose)
    {
        if (!pose.IsFinite() || !pose.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(pose.Skew))
            throw new ArgumentException("Joint bodies require finite unit-scale, zero-skew poses.");
    }
    private static float Length(Vector2 value) => (float)Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y);
    internal static void ValidateExtent(float value)
    {
        if (!float.IsFinite(value) || MathF.Abs(value) > B2_HUGE * PhysicsSpace.UnitsPerMeter)
            throw new ArgumentOutOfRangeException(nameof(value));
    }
    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void Coefficient(float value) { Finite(value); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void ValidateLimits(float lower, float upper)
    {
        if (lower < -0.99f * MathF.PI || upper > 0.99f * MathF.PI || lower > upper)
            throw new ArgumentOutOfRangeException(nameof(lower), "Enabled pin limits must be ordered within ±0.99π radians.");
    }
}
