using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Body : PhysicsBackendCollider
    {
        protected override void InvalidateCollisionExceptions() { }
    }
}
