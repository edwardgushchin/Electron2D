using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private readonly Dictionary<nint, (uint Bytes, bool Transfer)> _bufferAllocations = [];
    private long _deviceBufferBytes, _transferBufferBytes, _peakBufferBytes, _bufferAllocationCount, _bufferReleaseCount;
    private int _deviceBufferCount, _transferBufferCount;

    // Requested SDL buffer sizes exclude driver heaps, pipelines and allocator overhead.
    internal (long DeviceBytes, long TransferBytes, long PeakBytes, int DeviceBuffers, int TransferBuffers, long Allocations, long Releases) BufferMemory
    {
        get
        {
            lock (_bufferAllocations)
                return (_deviceBufferBytes, _transferBufferBytes, _peakBufferBytes,
                    _deviceBufferCount, _transferBufferCount, _bufferAllocationCount, _bufferReleaseCount);
        }
    }

    private nint RegisterBuffer(nint handle, uint bytes, bool transfer)
    {
        if (handle == 0) throw GPUPhysicsDevice.Failure("create resident buffer");
        try
        {
            lock (_bufferAllocations)
            {
                _bufferAllocations.Add(handle, (bytes, transfer));
                if (transfer) { _transferBufferBytes += bytes; _transferBufferCount++; }
                else { _deviceBufferBytes += bytes; _deviceBufferCount++; }
                _bufferAllocationCount++;
                _peakBufferBytes = Math.Max(_peakBufferBytes, _deviceBufferBytes + _transferBufferBytes);
            }
        }
        catch
        {
            if (transfer) SDL.ReleaseGPUTransferBuffer(Device, handle);
            else SDL.ReleaseGPUBuffer(Device, handle);
            throw;
        }
        return handle;
    }

    private void ReleaseBuffer(nint handle)
    {
        lock (_bufferAllocations)
        {
            var allocation = _bufferAllocations[handle];
            if (allocation.Transfer)
            {
                SDL.ReleaseGPUTransferBuffer(Device, handle);
                _transferBufferBytes -= allocation.Bytes; _transferBufferCount--;
            }
            else
            {
                SDL.ReleaseGPUBuffer(Device, handle);
                _deviceBufferBytes -= allocation.Bytes; _deviceBufferCount--;
            }
            _bufferAllocations.Remove(handle); _bufferReleaseCount++;
        }
    }

    private nint CreateTransfer(uint size, SDL.GPUTransferBufferUsage usage) => RegisterBuffer(
        SDL.CreateGPUTransferBuffer(Device, new SDL.GPUTransferBufferCreateInfo { Size = size, Usage = usage }), size, true);
}
