using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private RenderHandle? _debugContactPipeline, _debugContactPoints, _debugContactStatus, _debugContactDownload;
    private int _debugContactCapacity, _debugContactLimit, _debugContactTransferCapacity, _debugContactPreviousCount, _debugContactSourceCount;
    private bool _debugContactReady;
    internal long DebugContactCaptureCount { get; private set; }
    internal long DebugContactReadCount { get; private set; }

    internal void PrepareDebugContacts(int limit)
    {
        EnsureAccess(allowFailed: limit == 0);
        if (limit < 0 || limit > (int.MaxValue - 8) / 8) throw new ArgumentOutOfRangeException(nameof(limit));
        _debugContactPreviousCount = 0; _debugContactReady = false;
        if (limit == 0) { _debugContactLimit = 0; return; }
        _debugContactPipeline ??= _context.CreatePipeline("PhysicsResidentDebugContacts.comp.spv");
        Grow(ref _debugContactPoints, ref _debugContactCapacity, limit, sizeof(Vector2), false);
        _debugContactStatus ??= Buffer(8);
        GrowTransfer(ref _debugContactDownload, ref _debugContactTransferCapacity, checked(8 + limit * sizeof(Vector2)), SDL.GPUTransferBufferUsage.Download);
        _debugContactLimit = limit;
    }

    /// <summary>Copies bounded boundary-point samples recorded by the last solver batch, before pose advancement; never performs collision detection.</summary>
    internal int ReadDebugContacts(Span<Vector2> destination)
    {
        EnsureAccess();
        if (destination.Length > _debugContactLimit) throw new ArgumentException("Prepare the diagnostic capacity before reading.", nameof(destination));
        if (destination.IsEmpty || !_debugContactReady || _debugContactSourceCount == 0 || ActiveSimulationBodyCount == 0) { _debugContactPreviousCount = 0; return 0; }
        var upper = (int)Math.Min(destination.Length, (long)_debugContactSourceCount * 2);
        var prefix = Math.Min(upper, Math.Max(2, _debugContactPreviousCount));
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire contact diagnostics");
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact diagnostic results");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _debugContactStatus!.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _debugContactDownload!.DangerousGetHandle() });
            DownloadDebugContacts(copy, 0, prefix);
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            ReadbackBytes += 8 + prefix * sizeof(Vector2); DebugContactReadCount++;
            var mapped = SDL.MapGPUTransferBuffer(Device, _debugContactDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map contact diagnostic results");
            int count;
            try
            {
                if (*(uint*)mapped != 0 || ((uint*)mapped)[1] > (long)_debugContactSourceCount * 2) throw new InvalidOperationException("GPU contact diagnostics returned invalid data.");
                count = (int)Math.Min((uint)destination.Length, ((uint*)mapped)[1]);
                new ReadOnlySpan<Vector2>((byte*)mapped + 8, Math.Min(count, prefix)).CopyTo(destination);
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _debugContactDownload.DangerousGetHandle()); }
            if (count > prefix)
            {
                command = SDL.AcquireGPUCommandBuffer(Device);
                if (command == 0) throw GPUPhysicsDevice.Failure("acquire remaining contact diagnostics");
                copy = SDL.BeginGPUCopyPass(command);
                if (copy == 0) throw GPUPhysicsDevice.Failure("begin remaining contact diagnostics");
                DownloadDebugContacts(copy, prefix, count - prefix); SDL.EndGPUCopyPass(copy); Finish(ref command);
                ReadbackBytes += (count - prefix) * sizeof(Vector2);
                mapped = SDL.MapGPUTransferBuffer(Device, _debugContactDownload.DangerousGetHandle(), false);
                if (mapped == 0) throw GPUPhysicsDevice.Failure("map remaining contact diagnostics");
                try { new ReadOnlySpan<Vector2>((byte*)mapped + 8 + prefix * sizeof(Vector2), count - prefix).CopyTo(destination[prefix..]); }
                finally { SDL.UnmapGPUTransferBuffer(Device, _debugContactDownload.DangerousGetHandle()); }
            }
            _debugContactPreviousCount = count; _failed = false; return count;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }
    private void RecordDebugContacts(nint command)
    {
        if (_debugContactLimit == 0) return;
        _debugContactSourceCount = ContactPointCount; _debugContactReady = true;
        if (ContactPointCount == 0) return;
        DebugContactPass(command, 0); DebugContactPass(command, ContactPointCount); DebugContactCaptureCount++;
    }
    private void DebugContactPass(nint command, int count)
    {
        var output = stackalloc SDL.GPUStorageBufferReadWriteBinding[2] { new() { Buffer = _debugContactPoints!.DangerousGetHandle() }, new() { Buffer = _debugContactStatus!.DangerousGetHandle() } };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)output, 2);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin contact diagnostics");
        SDL.BindGPUComputePipeline(compute, _debugContactPipeline!.DangerousGetHandle());
        var input = stackalloc nint[3] { _contactsGPU!.DangerousGetHandle(), _shapesGPU!.DangerousGetHandle(), _bodies!.DangerousGetHandle() };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)input, 3);
        var settings = stackalloc uint[4] { (uint)count, (uint)_shapeHighWater, (uint)_highWater, (uint)_debugContactLimit };
        SDL.PushGPUComputeUniformData(command, 0, (nint)settings, 16);
        SDL.DispatchGPUCompute(compute, Math.Max(1, ((uint)count + 63) / 64), 1, 1); SDL.EndGPUComputePass(compute);
        UniformBytes += 16;
    }
    private void DownloadDebugContacts(nint copy, int start, int count) => SDL.DownloadFromGPUBuffer(copy,
        new() { Buffer = _debugContactPoints!.DangerousGetHandle(), Offset = (uint)(start * sizeof(Vector2)), Size = (uint)(count * sizeof(Vector2)) },
        new() { TransferBuffer = _debugContactDownload!.DangerousGetHandle(), Offset = (uint)(8 + start * sizeof(Vector2)) });
    private void DisposeDebugContacts()
    { _debugContactPipeline?.Dispose(); _debugContactPoints?.Dispose(); _debugContactStatus?.Dispose(); _debugContactDownload?.Dispose(); }
}
