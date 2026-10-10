namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal void CaptureViewContacts(PhysicsDirectBodyState view, int limit) => Attached.CaptureViewContacts(view, limit);
}
