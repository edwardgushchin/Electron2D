namespace Electron2D;

internal sealed partial class CPUPhysicsColliderImplementation
{
    internal override void PrepareDefaultForces(PhysicsBodyRuntime runtime, PhysicsBody? scene)
    {
        var rigid = scene as RigidBody;
        if (rigid?.CustomIntegrator ?? runtime.OmitForces)
        {
            SetGravityScale(0); ClearTransientForces();
            runtime.PendingForce = default; runtime.PendingTorque = 0;
            return;
        }
        if (runtime.PendingForce != Vector2.Zero) ApplyCentralForce(runtime.PendingForce, false);
        if (runtime.PendingTorque != 0 && !runtime.RotationLocked) ApplyTorque(runtime.PendingTorque, false);
        runtime.PendingForce = default; runtime.PendingTorque = 0;
        if (rigid is not null) { rigid.ApplyConstantForces(); return; }
        SetGravityScale(runtime.BodyGravityScale);
        if (runtime.ConstantForce != Vector2.Zero) ApplyCentralForce(runtime.ConstantForce, false);
        if (runtime.ConstantTorque != 0) ApplyTorque(runtime.ConstantTorque, false);
    }
    internal void ApplyResolvedFields(PhysicsBodyRuntime runtime, Vector2 gravity, float linearDamp, float angularDamp, Vector2 defaultGravity, double delta)
    {
        var rigid = runtime.Owners.Scene as RigidBody;
        var gravityScale = rigid?.GravityScale ?? runtime.BodyGravityScale;
        var ownLinear = rigid?.LinearDamp ?? runtime.BodyLinearDamp;
        var ownAngular = rigid?.AngularDamp ?? runtime.BodyAngularDamp;
        var linearMode = rigid?.LinearDampMode ?? runtime.BodyLinearDampMode;
        var angularMode = rigid?.AngularDampMode ?? runtime.BodyAngularDampMode;
        var scaledGravity = gravity * gravityScale;
        var resolvedLinear = linearMode == RigidBody.DampMode.Replace ? ownLinear : linearDamp + ownLinear;
        var resolvedAngular = angularMode == RigidBody.DampMode.Replace ? ownAngular : angularDamp + ownAngular;
        var linearFactor = MathF.Max(0, 1 - (float)delta * resolvedLinear);
        var angularFactor = MathF.Max(0, 1 - (float)delta * resolvedAngular);
        var acceleration = scaledGravity - defaultGravity * gravityScale;
        if (!scaledGravity.IsFinite() || !acceleration.IsFinite() || !float.IsFinite(resolvedLinear) ||
            !float.IsFinite(resolvedAngular) || !float.IsFinite(linearFactor) || !float.IsFinite(angularFactor))
            throw new InvalidOperationException("The resolved body field exceeds the finite simulation range.");
        var changed = runtime.FieldsInitialized && (runtime.Gravity != scaledGravity || runtime.LinearDamp != resolvedLinear || runtime.AngularDamp != resolvedAngular);
        ApplyFieldMotion(changed, runtime.Omitted, linearFactor, angularFactor, acceleration);
        runtime.Gravity = scaledGravity; runtime.LinearDamp = resolvedLinear; runtime.AngularDamp = resolvedAngular; runtime.FieldsInitialized = true;
    }
}
