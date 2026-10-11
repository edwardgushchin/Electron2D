namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal void InvalidateCollisionExceptions() => Attached.InvalidateCollisionExceptions();
    internal PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current) => Attached.ReadResolvedFields(current);

    internal void PrepareContactPolicy()
    {
        _implementation?.PrepareContactPolicy();
    }
}
