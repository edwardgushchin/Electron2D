namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal void PrepareContactPolicy()
    {
        _implementation?.PrepareContactPolicy();
    }
}
