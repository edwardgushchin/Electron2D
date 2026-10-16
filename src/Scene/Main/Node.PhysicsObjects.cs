namespace Electron2D;

public partial class Node
{
    // External object bindings are borrowed; this reverse list does not make a body own its target.
    private List<PhysicsColliderBackend>? _physicsObjectBindings;
    internal void AddPhysicsObjectBinding(PhysicsColliderBackend binding) => (_physicsObjectBindings ??= []).Add(binding);
    internal void RemovePhysicsObjectBinding(PhysicsColliderBackend binding) => _physicsObjectBindings?.Remove(binding);

    internal void EnsurePhysicsObjectAccess()
    {
        Tree?.EnsureExtensionHierarchyChange();
        if (this is ICollisionGeometry geometry)
            foreach (var shape in geometry.OwnerShapes) shape.EnsureServerBindingAccess();
        if (!PhysicsColliderBackend.HasAnyAttachments && !CollisionObject.HasSpaceGeometryBorrows && !PhysicsServerShape.HasDataUses) return;
        if (this is CollisionObject collider)
        {
            collider.EnsureGeometryAuthoringAccess();
            if (collider.Backend.Space is { } space) { space.EnsureReleaseAccess(); space.EnsureExtensionHierarchyChange(); }
        }
        if (_physicsObjectBindings is not null)
            foreach (var binding in _physicsObjectBindings)
            { binding.Space?.EnsureReleaseAccess(); binding.Space?.EnsureExtensionHierarchyChange(); }
        foreach (var child in _children) child.EnsurePhysicsObjectAccess();
    }

    private void EnsurePhysicsAuthoringAccess()
    {
        if (!PhysicsColliderBackend.HasAnyAttachments && !CollisionObject.HasSpaceGeometryBorrows && !PhysicsServerShape.HasDataUses) return;
        for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent) Check(ancestor);
        Descendants(this);
        static void Check(Node node)
        {
            if (node is not CollisionObject collider) return;
            collider.EnsureGeometryAuthoringAccess();
            collider.Backend.Space?.EnsureImplementationWriteAccess();
        }
        static void Descendants(Node node)
        { Check(node); foreach (var child in node._children) Descendants(child); }
    }

    private void NotifyPhysicsObjectBindings(bool entering)
    {
        if (_physicsObjectBindings is not { Count: > 0 }) return;
        // Tree membership is structural. Snapshot before callbacks can rebind or release other bodies.
        List<Exception>? errors = null;
        foreach (var binding in _physicsObjectBindings.ToArray())
            if (binding.ObjectIdentity.ID == InstanceID)
                try { binding.Space?.ObjectTreeChanged(this, entering); }
                catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Physics object membership callbacks failed.", errors);
    }
}
