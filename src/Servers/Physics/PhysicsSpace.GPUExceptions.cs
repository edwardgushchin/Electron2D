namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private bool _gpuExceptionsDirty = true;
    private HashSet<(GPUPhysicsBodyStore.BodyHandle From, GPUPhysicsBodyStore.BodyHandle To)> _gpuExceptions = [], _gpuNextExceptions = [];
    internal void InvalidateGPUExceptions() => _gpuExceptionsDirty = true;
    internal void ObserveGPUException(RID from, RID to)
    {
        if (!_gpuRIDColliders.TryGetValue((ulong)from.GetID(), out var first) ||
            !_gpuRIDColliders.TryGetValue((ulong)to.GetID(), out var second)) return;
        var pair = (first.GPUHandle, second.GPUHandle);
        if (_gpuNextExceptions.Add(pair) && !_gpuExceptions.Contains(pair)) GPUStore!.SetCollisionException(pair.Item1, pair.Item2, true);
    }
    private void SyncGPUExceptions()
    {
        if (GPUStore is null || !_gpuExceptionsDirty) return;
        _gpuNextExceptions.Clear(); PhysicsServer.Service.ObserveGPUExceptions(this);
        foreach (var pair in _gpuExceptions)
            if (!_gpuNextExceptions.Contains(pair) &&
                _gpuColliders.TryGetValue(pair.From.Index, out var first) && first.GPUHandle == pair.From &&
                _gpuColliders.TryGetValue(pair.To.Index, out var second) && second.GPUHandle == pair.To)
                GPUStore.SetCollisionException(pair.From, pair.To, false);
        (_gpuExceptions, _gpuNextExceptions) = (_gpuNextExceptions, _gpuExceptions); _gpuExceptionsDirty = false;
    }
}
