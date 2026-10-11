using Electron2D;

internal static partial class PhysicsRegisteredBackendTests
{
    private sealed partial class Body : PhysicsBackendCollider
    {
        internal override void InvalidateCollisionExceptions() { }
    }
}
