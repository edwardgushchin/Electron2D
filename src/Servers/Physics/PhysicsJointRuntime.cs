namespace Electron2D;

/// <summary>Retains joint identity, sampled engine frames, settings and common scene/server lifetime.</summary>
internal sealed partial class PhysicsJointRuntime(RID rid, Joint? scene = null, PhysicsServer.JointType? declaredType = null)
{
    internal RID RID { get; } = rid;
    internal WeakReference<Joint>? Scene { get; } = scene is null ? null : new(scene);
    internal PhysicsServer.JointType? DeclaredType { get; } = declaredType;
    internal PhysicsServer.JointType Type { get; private set; } = PhysicsServer.JointType.Empty;
    internal PhysicsSpace? Space { get; private set; }
    internal RID BodyA { get; private set; }
    internal RID BodyB { get; private set; }
    internal const float MaxExtentSceneUnits = 10_000_000;
    internal PhysicsJointBackend Backend { get; } = new();
    internal bool HasBackend => Backend.IsAttached;
    internal Transform FrameA { get; private set; } = Transform.Identity;
    internal Transform FrameB { get; private set; } = Transform.Identity;
    internal float LowerTranslation { get; private set; }
    internal float UpperTranslation { get; private set; }
    internal bool DisableCollision { get; private set; } = true;
    internal float Bias { get; private set; }
    internal float MaxBias { get; private set; } = float.MaxValue;
    internal float MaxForce { get; private set; } = float.MaxValue;
    internal float PinSoftness { get; private set; }
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
        var b = second.IsValid() ? Snapshot(second) : (Space: (PhysicsSpace?)null, Backend: (PhysicsColliderBackend?)null, Pose: Transform.Identity);
        if (a.Space is not null && b.Space is not null && !ReferenceEquals(a.Space, b.Space))
            throw new ArgumentException("Joint bodies must belong to the same physics space.", nameof(second));
        var nextSpace = a.Space ?? b.Space;
        if (Scene is { } weak && weak.TryGetTarget(out var owner) && owner.AttachmentSpace is { } sceneSpace &&
            nextSpace is not null && !ReferenceEquals(sceneSpace, nextSpace))
            throw new InvalidOperationException("A scene joint belongs to its scene physics world.");
        ValidatePose(a.Pose); ValidatePose(b.Pose);
        var frameA = PhysicsJointBackend.SampleLocalFrame(a.Backend, a.Pose, anchorA, angle);
        var frameB = PhysicsJointBackend.SampleLocalFrame(b.Backend, b.Pose, anchorB, angle);
        var rest = Length(anchorB - anchorA);
        if (type is PhysicsServer.JointType.DampedSpring or PhysicsServer.JointType.Groove) ValidateExtent(rest);
        DetachSpace();
        if (!preserve)
        {
            PinLimitEnabled = PinMotorEnabled = false;
            PinLimitLower = PinLimitUpper = PinMotorVelocity = PinSoftness = 0;
            PinMotorMaxTorque = 10;
            SpringRestLength = rest; SpringAutomaticRest = false;
            SpringStiffness = 20; SpringDamping = 1.5f;
        }
        Type = type; BodyA = first; BodyB = second; FrameA = frameA; FrameB = frameB;
        LowerTranslation = lower; UpperTranslation = upper;
        RefreshSpace();
    }

    internal void RefreshSpace()
    {
        if (Type == PhysicsServer.JointType.Empty) return;
        (PhysicsSpace? Space, PhysicsColliderBackend? Backend, Transform Pose) a, b;
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
        if (HasBackend || next is null || a.Space is null || BodyB.IsValid() && !ReferenceEquals(a.Space, b.Space)) return;
        Backend.Attach(next, a.Backend!, BodyB.IsValid() ? b.Backend : null, this);
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
        if (HasBackend && DisableCollision && BodyB.IsValid())
            PhysicsServer.Service.JointCollisionContribution(BodyA, BodyB, add: false);
        Backend.Detach();
    }

    internal void SetDisableCollision(bool value)
    {
        if (DisableCollision == value) return;
        if (HasBackend && DisableCollision && BodyB.IsValid())
            PhysicsServer.Service.JointCollisionContribution(BodyA, BodyB, add: false);
        DisableCollision = value;
        if (HasBackend) Backend.SetCollideConnected(!value);
        if (HasBackend && value && BodyB.IsValid())
            PhysicsServer.Service.JointCollisionContribution(BodyA, BodyB, add: true);
        if (BodyA.IsValid()) MarkBodyDirty(BodyA);
        if (BodyB.IsValid()) MarkBodyDirty(BodyB);
    }

    internal void SetBias(float value) { ValidateBias(value); if (Bias == value) return; Bias = value; ApplySolverPolicy(); }
    internal void SetMaxBias(float value) { Coefficient(value); if (MaxBias == value) return; MaxBias = value; ApplySolverPolicy(); }
    internal void SetMaxForce(float value) { Coefficient(value); if (MaxForce == value) return; MaxForce = value; ApplySolverPolicy(); }
    internal void SetPinSoftness(float value) { Coefficient(value); if (PinSoftness == value) return; PinSoftness = value; ApplySolverPolicy(); }
    internal void ApplySolverPolicy() { if (HasBackend) Backend.ApplySolverPolicy(this); }
    internal static void ValidateBias(float value)
    { if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); }

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
        if (!HasBackend || Type != PhysicsServer.JointType.Pin) return;
        Backend.SetPinLimits(PinLimitEnabled, PinLimitLower, PinLimitUpper);
    }
    internal void SetPinMotorEnabled(bool value)
    {
        PinMotorEnabled = value;
        if (HasBackend && Type == PhysicsServer.JointType.Pin) Backend.SetPinMotorEnabled(value);
    }
    internal void SetPinMotorVelocity(float value)
    {
        Finite(value); PinMotorVelocity = value;
        if (HasBackend && Type == PhysicsServer.JointType.Pin) Backend.SetPinMotorVelocity(value);
    }
    internal void SetPinMotorMaxTorque(float value)
    {
        Coefficient(value); PinMotorMaxTorque = value;
        if (HasBackend && Type == PhysicsServer.JointType.Pin) Backend.SetPinMotorMaxTorque(value);
    }
    internal void SetGrooveLength(float value)
    {
        ValidateExtent(value);
        LowerTranslation = MathF.Min(0, value);
        UpperTranslation = MathF.Max(0, value);
        if (HasBackend && Type == PhysicsServer.JointType.Groove)
            Backend.SetGrooveLimits(LowerTranslation, UpperTranslation);
    }
    internal void SetSpringRestLength(float value, bool automatic = false)
    {
        ValidateExtent(value); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        SpringRestLength = value; SpringAutomaticRest = automatic && value == 0;
    }
    internal void SetSpringStiffness(float value) { Coefficient(value); SpringStiffness = value; }
    internal void SetSpringDamping(float value) { Coefficient(value); SpringDamping = value; }

    internal void PrepareSolverStep(float delta) => Backend.PrepareSolverStep(delta, this);
    internal void ValidateSolverStep(PhysicsSpace space) => Backend.ValidateSolverStep(space);
    internal void ApplySolverStep() => Backend.ApplySolverStep();

    private static (PhysicsSpace? Space, PhysicsColliderBackend? Backend, Transform Pose) Snapshot(RID body)
    {
        var owners = PhysicsServer.Service.ResolveBodyOwners(body);
        return owners.Scene is { } scene ? (scene.Space, scene.Backend, scene.GlobalTransform) :
            (owners.Server!.Space, owners.Server.Backend, owners.Server.GetTransform());
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
        if (!float.IsFinite(value) || MathF.Abs(value) > MaxExtentSceneUnits)
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
