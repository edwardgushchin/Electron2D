namespace Electron2D;

internal sealed partial class GPUPhysicsWorldBackend
{
    private bool _exceptionsDirty = true;
    private HashSet<(GPUPhysicsBodyStore.BodyHandle From, GPUPhysicsBodyStore.BodyHandle To)> _exceptions = [], _nextExceptions = [];
    private readonly List<(RID From, RID To)> _exceptionPairs = [];
    internal override void InvalidateCollisionExceptions(PhysicsColliderBackend collider)
    {
        if (!ReferenceEquals(collider.Space, Space)) throw new InvalidOperationException("Collision policy requires this world's collider attachment.");
        _exceptionsDirty = true;
    }
    private void ObserveCollisionException(RID from, RID to)
    {
        if (!Space.TryGetGPUCollider(from, out var first) || !Space.TryGetGPUCollider(to, out var second)) return;
        var pair = (GPUStore.NativeCollider(first).GPUHandle, GPUStore.NativeCollider(second).GPUHandle);
        if (_nextExceptions.Add(pair) && !_exceptions.Contains(pair)) GPUStore.SetCollisionException(pair.Item1, pair.Item2, true);
    }
    internal override void SynchronizeCollisionExceptions()
    {
        EnsureAccess();
        if (!_exceptionsDirty) return;
        PhysicsServer.Service.CopyCollisionExceptions(_exceptionPairs);
        _nextExceptions.Clear();
        foreach (var pair in _exceptionPairs) ObserveCollisionException(pair.From, pair.To);
        foreach (var pair in _exceptions)
            if (!_nextExceptions.Contains(pair) && Space.TryGetGPUCollider(pair.From.Index, out var first) &&
                GPUStore.NativeCollider(first).GPUHandle == pair.From && Space.TryGetGPUCollider(pair.To.Index, out var second) &&
                GPUStore.NativeCollider(second).GPUHandle == pair.To)
                GPUStore.SetCollisionException(pair.From, pair.To, false);
        (_exceptions, _nextExceptions) = (_nextExceptions, _exceptions); _exceptionsDirty = false;
    }
}
