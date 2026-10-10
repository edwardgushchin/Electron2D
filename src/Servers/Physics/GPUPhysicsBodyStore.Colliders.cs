namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private readonly Dictionary<PhysicsColliderBackend, GPUPhysicsColliderImplementation> _colliders = [];
    internal GPUPhysicsColliderImplementation NativeCollider(PhysicsColliderBackend collider) =>
        _colliders.TryGetValue(collider, out var value) ? value : throw new InvalidOperationException("The collider has no attachment owned by this GPU store.");
    internal PhysicsColliderImplementation CreateCollider(PhysicsColliderBackend collider, PhysicsSpace space)
    {
        var implementation = new GPUPhysicsColliderImplementation(collider, space, this);
        _colliders.Add(collider, implementation);
        return implementation;
    }
    internal void RetireCollider(PhysicsColliderBackend collider, GPUPhysicsColliderImplementation implementation)
    {
        if (_colliders.TryGetValue(collider, out var current) && ReferenceEquals(current, implementation)) _colliders.Remove(collider);
    }
}
