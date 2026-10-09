namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal void ObjectTreeChanged(Node node, bool entering)
    {
        EnsureReleaseAccess();
        foreach (var rigid in _contactBodies) rigid.ObjectTreeChanged(node, entering, _contactEvents);
        foreach (var area in _areas) area.ObjectTreeChanged(node, entering, _overlapEvents);
        // Raw server monitor callbacks report physical pairs, independently of scene membership.
        DispatchEvents();
    }
}
