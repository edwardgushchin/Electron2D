namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    internal float? FrictionOverride;
    internal float? BounceOverride;
    internal float BodyGravityScale = 1;
    internal float BodyLinearDamp;
    internal float BodyAngularDamp;
    internal RigidBody.DampMode BodyLinearDampMode;
    internal RigidBody.DampMode BodyAngularDampMode;

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
