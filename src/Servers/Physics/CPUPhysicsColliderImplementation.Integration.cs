namespace Electron2D;

internal sealed partial class CPUPhysicsColliderImplementation
{
    internal override void PrepareDefaultForces(in PhysicsBodyForceConfiguration configuration)
    {
        if (configuration.OmitForces)
        {
            SetGravityScale(0); ClearTransientForces();
            return;
        }
        if (configuration.PendingForce != Vector2.Zero) ApplyCentralForce(configuration.PendingForce, false);
        if (configuration.PendingTorque != 0 && !configuration.RotationLocked) ApplyTorque(configuration.PendingTorque, false);
        if (configuration.RefreshGravityScale) SetGravityScale(configuration.GravityScale);
        if (!configuration.ApplyPersistentForces) return;
        if (configuration.ConstantForce != Vector2.Zero) ApplyCentralForce(configuration.ConstantForce, false);
        if (configuration.ConstantTorque != 0) ApplyTorque(configuration.ConstantTorque, false);
    }
    internal PhysicsBodyFields ApplyResolvedFields(in PhysicsBodyIntegrationConfiguration configuration, in PhysicsBodyFields previous,
        Vector2 gravity, float linearDamp, float angularDamp, Vector2 defaultGravity, double delta)
    {
        var gravityScale = configuration.GravityScale;
        var ownLinear = configuration.LinearDamp;
        var ownAngular = configuration.AngularDamp;
        var linearMode = configuration.LinearDampMode;
        var angularMode = configuration.AngularDampMode;
        var scaledGravity = gravity * gravityScale;
        var resolvedLinear = linearMode == RigidBody.DampMode.Replace ? ownLinear : linearDamp + ownLinear;
        var resolvedAngular = angularMode == RigidBody.DampMode.Replace ? ownAngular : angularDamp + ownAngular;
        var linearFactor = MathF.Max(0, 1 - (float)delta * resolvedLinear);
        var angularFactor = MathF.Max(0, 1 - (float)delta * resolvedAngular);
        var acceleration = scaledGravity - defaultGravity * gravityScale;
        if (!scaledGravity.IsFinite() || !acceleration.IsFinite() || !float.IsFinite(resolvedLinear) ||
            !float.IsFinite(resolvedAngular) || !float.IsFinite(linearFactor) || !float.IsFinite(angularFactor))
            throw new InvalidOperationException("The resolved body field exceeds the finite simulation range.");
        var changed = previous.Initialized && (previous.Gravity != scaledGravity || previous.LinearDamp != resolvedLinear || previous.AngularDamp != resolvedAngular);
        ApplyFieldMotion(changed, configuration.OmitForces, linearFactor, angularFactor, acceleration);
        return new(scaledGravity, resolvedLinear, resolvedAngular, true);
    }
}
