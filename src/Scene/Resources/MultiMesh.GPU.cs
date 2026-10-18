namespace Electron2D;

public sealed partial class MultiMesh
{
    private MultiMeshGPUState? _gpu;
    internal bool UseIndirect;
    internal ReadOnlySpan<float> CurrentData => _buffer;
    internal ReadOnlySpan<float> PreviousData => _previous;
    internal bool InterpolationEnabled => _interpolationEnabled;
    internal MultiMeshGPUState? GPUState => _gpu is { Alive: true } ? _gpu : null;
    internal bool Owns(MultiMeshGPUState state) => !IsDisposed && ReferenceEquals(_gpu, state);
    internal MultiMeshGPUState? PrepareGPU(RenderingDevice device, bool geometry = true)
    {
        lock (Gate)
        {
            ThrowIfDisposed(); if (_count == 0) return null;
            if ((long)_buffer.Length * 4 > int.MaxValue || (long)_count * 64 > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(InstanceCount), "Instance storage exceeds the device-buffer byte budget.");
            if (_gpu is null || !_gpu.Alive || !ReferenceEquals(_gpu.Device, device)) _gpu = new(this, device);
            _gpu.Prepare(geometry); return _gpu;
        }
    }
}
