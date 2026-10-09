namespace Electron2D;

public partial class Node
{
    // External object bindings are borrowed; this reverse list does not make a body own its target.
    private List<PhysicsColliderBackend>? _physicsObjectBindings;
    internal void AddPhysicsObjectBinding(PhysicsColliderBackend binding) => (_physicsObjectBindings ??= []).Add(binding);
    internal void RemovePhysicsObjectBinding(PhysicsColliderBackend binding) => _physicsObjectBindings?.Remove(binding);

    internal void EnsurePhysicsObjectAccess()
    {
        if (_physicsObjectBindings is null) return;
        foreach (var binding in _physicsObjectBindings) binding.Space?.EnsureReleaseAccess();
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
