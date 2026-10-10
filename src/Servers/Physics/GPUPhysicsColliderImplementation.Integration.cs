namespace Electron2D;

internal sealed partial class GPUPhysicsColliderImplementation
{
    internal override void PrepareDefaultForces(PhysicsBodyRuntime runtime, PhysicsBody? scene)
    {
        var rigid = scene as RigidBody;
        if (!(rigid?.CustomIntegrator ?? runtime.OmitForces))
        {
            if (runtime.PendingForce != Vector2.Zero) ApplyCentralForce(runtime.PendingForce, false);
            if (runtime.PendingTorque != 0 && !runtime.RotationLocked) ApplyTorque(runtime.PendingTorque, false);
        }
        runtime.PendingForce = default; runtime.PendingTorque = 0;
    }
}
