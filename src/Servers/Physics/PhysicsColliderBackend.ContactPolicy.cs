namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal void PrepareContactPolicy()
    {
        if (_implementation is CPUPhysicsColliderImplementation cpu) cpu.PrepareContactPolicy();
    }
}
