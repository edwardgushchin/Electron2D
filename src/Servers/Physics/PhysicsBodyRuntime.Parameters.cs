namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    internal float? FrictionOverride;
    internal float? BounceOverride;
    private float _bodyGravityScale = 1;
    internal float BodyGravityScale { get => _bodyGravityScale; set { GPUParametersDirty |= _bodyGravityScale != value; _bodyGravityScale = value; } }
    private float _bodyLinearDamp;
    internal float BodyLinearDamp { get => _bodyLinearDamp; set { GPUParametersDirty |= _bodyLinearDamp != value; _bodyLinearDamp = value; } }
    private float _bodyAngularDamp;
    internal float BodyAngularDamp { get => _bodyAngularDamp; set { GPUParametersDirty |= _bodyAngularDamp != value; _bodyAngularDamp = value; } }
    private RigidBody.DampMode _bodyLinearDampMode;
    internal RigidBody.DampMode BodyLinearDampMode { get => _bodyLinearDampMode; set { GPUParametersDirty |= _bodyLinearDampMode != value; _bodyLinearDampMode = value; } }
    private RigidBody.DampMode _bodyAngularDampMode;
    internal RigidBody.DampMode BodyAngularDampMode { get => _bodyAngularDampMode; set { GPUParametersDirty |= _bodyAngularDampMode != value; _bodyAngularDampMode = value; } }

    internal float GetFriction() => FrictionOverride ?? Owners.Scene?.MaterialOverride?.ComputedFriction ?? 1;
    internal float GetBounce() => BounceOverride ?? Owners.Scene?.MaterialOverride?.ComputedBounce ?? 0;
    internal void ResetMaterialOverrides() { FrictionOverride = null; BounceOverride = null; }

    internal void SetMaterialParameter(float value, bool friction)
    {
        Finite(value);
        if (friction) FrictionOverride = value; else BounceOverride = value;
        var owners = Owners;
        if (owners.Scene is { } scene) scene.MarkShapesDirty(); else owners.Server!.MarkShapesDirty();
    }

    internal void ApplyResolvedFields(Vector2 gravity, float linearDamp, float angularDamp, Vector2 defaultGravity, double delta)
    {
        var rigid = Owners.Scene as RigidBody;
        var gravityScale = rigid?.GravityScale ?? BodyGravityScale;
        var ownLinear = rigid?.LinearDamp ?? BodyLinearDamp;
        var ownAngular = rigid?.AngularDamp ?? BodyAngularDamp;
        var linearMode = rigid?.LinearDampMode ?? BodyLinearDampMode;
        var angularMode = rigid?.AngularDampMode ?? BodyAngularDampMode;
        var scaledGravity = gravity * gravityScale;
        var resolvedLinear = linearMode == RigidBody.DampMode.Replace ? ownLinear : linearDamp + ownLinear;
        var resolvedAngular = angularMode == RigidBody.DampMode.Replace ? ownAngular : angularDamp + ownAngular;
        var linearFactor = MathF.Max(0, 1 - (float)delta * resolvedLinear);
        var angularFactor = MathF.Max(0, 1 - (float)delta * resolvedAngular);
        var acceleration = scaledGravity - defaultGravity * gravityScale;
        if (!scaledGravity.IsFinite() || !acceleration.IsFinite() || !float.IsFinite(resolvedLinear) ||
            !float.IsFinite(resolvedAngular) || !float.IsFinite(linearFactor) || !float.IsFinite(angularFactor))
            throw new InvalidOperationException("The resolved body field exceeds the finite simulation range.");
        var changed = FieldsInitialized && (Gravity != scaledGravity || LinearDamp != resolvedLinear || AngularDamp != resolvedAngular);
        Backend.ApplyFieldMotion(changed, Omitted, linearFactor, angularFactor, acceleration);
        Gravity = scaledGravity; LinearDamp = resolvedLinear; AngularDamp = resolvedAngular; FieldsInitialized = true;
    }
}
