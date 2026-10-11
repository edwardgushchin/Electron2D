namespace Electron2D;

internal sealed partial class GPUPhysicsColliderImplementation
{
    internal override void PrepareDefaultForces(in PhysicsBodyForceConfiguration configuration)
    {
        if (!configuration.OmitForces)
        {
            if (configuration.PendingForce != Vector2.Zero) ApplyCentralForce(configuration.PendingForce, false);
            if (configuration.PendingTorque != 0 && !configuration.RotationLocked) ApplyTorque(configuration.PendingTorque, false);
        }
    }
}
