using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private sealed class Readback
    {
        internal RenderHandle? Transfer;
        internal uint Capacity;
        internal bool Download;
        internal Memory<byte> Destination;
        internal byte[]? Data;
        internal Action<byte[]>? ArrayCallback;
        internal Action? Callback;
    }
    private readonly record struct ReadbackCompletion(Action<byte[]>? ArrayCallback, byte[]? Data, Action? Callback);
    private readonly Stack<Readback> _idleReadbacks = [];
    private readonly Queue<Readback> _recordedReadbacks = [], _submittedReadbacks = [];
    private readonly Queue<ReadbackCompletion> _completedReadbacks = [];
    private bool _deliveringReadbacks, _accessingReadbackMemory;

    /// <summary>Queues a buffer snapshot and delivers an independent array when local work completes.</summary>
    /// <param name="buffer">A live storage or uniform buffer on this device.</param>
    /// <param name="callback">Receives the request-time bytes on the owner thread.</param>
    /// <param name="offsetBytes">First byte to read.</param>
    /// <param name="sizeBytes">Bytes to read, or zero for the remaining buffer.</param>
    /// <remarks>End the compute list, queue reads, Submit, then Sync. The request does not wait for its
    /// own result or call the callback inline. Recording may wait for a previous submission. Sync,
    /// synchronous storage reads and FreeRID deliver completed callbacks in request order. FreeRID
    /// delivers after removing the resource. Device disposal cancels undelivered callbacks. Callback
    /// failures are collected after other completed callbacks run and thrown as AggregateException.</remarks>
    /// <exception cref="ArgumentNullException">The callback is null.</exception>
    /// <exception cref="ArgumentException">The identity is not a live buffer of this device.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The range exceeds the buffer.</exception>
    /// <exception cref="InvalidOperationException">The owner thread or compute-list state is invalid, or a native operation fails.</exception>
    public void BufferGetDataAsync(RID buffer, Action<byte[]> callback, uint offsetBytes = 0, uint sizeBytes = 0)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(callback); var entry = Get(buffer); Range(entry, offsetBytes, sizeBytes);
        var data = new byte[sizeBytes == 0 ? entry.Size - offsetBytes : sizeBytes];
        QueueReadback(entry, data, offsetBytes, callback, data, null);
    }

    /// <summary>Queues a buffer snapshot into caller-owned storage without allocating a result array.</summary>
    /// <param name="buffer">A live storage or uniform buffer on this device.</param>
    /// <param name="destination">Determines the byte count; keep it valid and do not access it until completion.</param>
    /// <param name="callback">Runs on the owner thread after destination is filled.</param>
    /// <param name="offsetBytes">First byte to read.</param>
    /// <remarks>Uses the same submission, delivery and cancellation contract as the array overload.
    /// Transfer buffers and queue entries are reused after completion; prepare the largest range and
    /// concurrent request count before allocation-sensitive work. Completed destinations are independent
    /// of later source-buffer writes. Disposal cancels pending requests without invoking callbacks.</remarks>
    /// <exception cref="ArgumentNullException">The callback is null.</exception>
    /// <exception cref="ArgumentException">The identity is not a live buffer of this device.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The range exceeds the buffer.</exception>
    /// <exception cref="InvalidOperationException">The owner thread or compute-list state is invalid, or a native operation fails.</exception>
    public void BufferGetDataAsync(RID buffer, Memory<byte> destination, Action callback, uint offsetBytes = 0)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(callback); var entry = Get(buffer); Range(entry, offsetBytes, (uint)destination.Length);
        QueueReadback(entry, destination, offsetBytes, null, null, callback);
    }

    private void QueueReadback(Entry entry, Memory<byte> destination, uint offset, Action<byte[]>? arrayCallback, byte[]? data, Action? callback)
    {
        var download = entry.Kind == Kind.Storage && !destination.IsEmpty;
        var command = download ? Commands() : 0;
        var request = _idleReadbacks.TryPop(out var idle) ? idle : new Readback();
        try
        {
            request.Destination = destination; request.ArrayCallback = arrayCallback; request.Data = data; request.Callback = callback; request.Download = download;
            if (download)
            {
                if (request.Transfer is null || request.Capacity < destination.Length)
                {
                    var transfer = Transfer((uint)destination.Length, SDL.GPUTransferBufferUsage.Download);
                    request.Transfer?.Dispose(); request.Transfer = transfer; request.Capacity = (uint)destination.Length;
                }
                var pass = SDL.BeginGPUCopyPass(command); if (pass == 0) throw GPUPhysicsDevice.Failure("begin asynchronous buffer read");
                try { SDL.DownloadFromGPUBuffer(pass, new() { Buffer = entry.Handle!.DangerousGetHandle(), Offset = offset, Size = (uint)destination.Length }, new() { TransferBuffer = request.Transfer.DangerousGetHandle() }); }
                finally { SDL.EndGPUCopyPass(pass); }
            }
            else if (entry.Kind == Kind.Uniform) entry.Data.AsSpan((int)offset, destination.Length).CopyTo(ReadbackDestination(destination));
            _recordedReadbacks.Enqueue(request);
        }
        catch { RecycleReadback(request); throw; }
    }
    private Span<byte> ReadbackDestination(Memory<byte> memory)
    {
        _accessingReadbackMemory = true;
        try { return memory.Span; }
        finally { _accessingReadbackMemory = false; }
    }
    private void RecycleReadback(Readback request)
    {
        request.Destination = default; request.ArrayCallback = null; request.Data = null; request.Callback = null; request.Download = false;
        _idleReadbacks.Push(request);
    }
    private void CompleteSubmission()
    {
        if (_fence != 0)
        {
            var fence = _fence;
            if (!SDL.WaitForGPUFences(Device, true, (nint)(&fence), 1)) throw GPUPhysicsDevice.Failure("wait for compute commands");
            SDL.ReleaseGPUFence(Device, _fence); _fence = 0;
        }
        while (_submittedReadbacks.TryPeek(out var request))
        {
            if (request.Download)
            {
                var destination = ReadbackDestination(request.Destination);
                var pointer = SDL.MapGPUTransferBuffer(Device, request.Transfer!.DangerousGetHandle(), false);
                if (pointer == 0) throw GPUPhysicsDevice.Failure("map asynchronous buffer read");
                try { new ReadOnlySpan<byte>((void*)pointer, request.Destination.Length).CopyTo(destination); }
                finally { SDL.UnmapGPUTransferBuffer(Device, request.Transfer.DangerousGetHandle()); }
            }
            _completedReadbacks.Enqueue(new(request.ArrayCallback, request.Data, request.Callback));
            _submittedReadbacks.Dequeue(); RecycleReadback(request);
        }
    }
    private void DeliverReadbacks()
    {
        if (_deliveringReadbacks) return;
        _deliveringReadbacks = true; List<Exception>? errors = null;
        try
        {
            while (_completedReadbacks.TryDequeue(out var completion))
            {
                try { if (completion.ArrayCallback is { } callback) callback(completion.Data!); else completion.Callback!(); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        finally { _deliveringReadbacks = false; }
        if (errors is not null) throw new AggregateException("Asynchronous buffer callbacks failed.", errors);
    }
    private void CancelReadbacks()
    {
        foreach (var request in _idleReadbacks) request.Transfer?.Dispose();
        foreach (var request in _recordedReadbacks) request.Transfer?.Dispose();
        foreach (var request in _submittedReadbacks) request.Transfer?.Dispose();
        _idleReadbacks.Clear(); _recordedReadbacks.Clear(); _submittedReadbacks.Clear(); _completedReadbacks.Clear();
    }
}
