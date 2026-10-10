namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal void SetSurfaceVelocity(Vector2 linear, float angular) => Attached.SetSurfaceVelocity(linear, angular);
}
