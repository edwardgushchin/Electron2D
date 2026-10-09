using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    /// <summary>Latest observable state since the previous publication, including removal/replacement generations. Not a replay checkpoint or an event history.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct BodyChange
    {
        internal uint Index, Generation, PreviousGeneration, Alive;
        internal Snapshot State;
    }

    private RenderHandle? _changesPipeline, _changesHistory, _changesResults, _changesStatus, _changesUpload, _changesDownload;
    private int _changesHistoryCapacity, _changesResultCapacity, _changesDownloadCapacity, _publishedSlots;
    internal long ChangePublicationCount { get; private set; }
    internal int BodySlotCount { get { EnsureAccess(); return _highWater; } }
    internal long ChangeDeviceCapacityBytes => (long)(_changesHistoryCapacity + _changesResultCapacity) * sizeof(BodyChange) + (_changesStatus is null ? 0 : 8);
    internal long ChangeTransferCapacityBytes => _changesDownloadCapacity + (_changesUpload is null ? 0 : 8);

    /// <summary>Consumes a single publisher's changes into a caller-retained span. Capacity must cover BodySlotCount before any work is submitted; only the returned prefix is written. Order is unspecified.</summary>
    /// <remarks>Compares on device, ignoring internal sleep clocks/solver flags. The first call includes all live bodies; later calls include changed poses, velocities, fields, exposed policy, births and deaths. Two waits download count then exactly that prefix; empty publications need one. No body-request list or CPU history is maintained.</remarks>
    internal int ReadChanges(Span<BodyChange> destination)
    {
        EnsureAccess();
        if (destination.Length < _highWater) throw new ArgumentException("The change destination must cover every body slot.", nameof(destination));
        if (_highWater == 0) return 0;
        Step(0, default);
        _changesPipeline ??= _context.CreatePipeline("PhysicsResidentChanges.comp.spv");
        Grow(ref _changesHistory, ref _changesHistoryCapacity, _highWater, sizeof(BodyChange), true);
        Grow(ref _changesResults, ref _changesResultCapacity, _highWater, sizeof(BodyChange), false);
        _changesStatus ??= Buffer(8);
        _changesUpload ??= Transfer(8, SDL.GPUTransferBufferUsage.Upload);
        GrowTransfer(ref _changesDownload, ref _changesDownloadCapacity, checked(8 + _highWater * sizeof(BodyChange)), SDL.GPUTransferBufferUsage.Download);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire body publication");
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _changesUpload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map body publication reset");
            *(ulong*)mapped = 0;
            SDL.UnmapGPUTransferBuffer(Device, _changesUpload.DangerousGetHandle());
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin body publication reset");
            SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _changesUpload.DangerousGetHandle() }, new() { Buffer = _changesStatus.DangerousGetHandle(), Size = 8 }, false);
            SDL.EndGPUCopyPass(copy);
            var bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[3];
            bindings[0] = new() { Buffer = _changesHistory!.DangerousGetHandle() };
            bindings[1] = new() { Buffer = _changesResults!.DangerousGetHandle() };
            bindings[2] = new() { Buffer = _changesStatus.DangerousGetHandle() };
            var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)bindings, 3);
            if (compute == 0) throw GPUPhysicsDevice.Failure("begin body publication");
            SDL.BindGPUComputePipeline(compute, _changesPipeline.DangerousGetHandle());
            var inputs = stackalloc nint[2] { _bodies!.DangerousGetHandle(), _resolvedFields!.DangerousGetHandle() };
            SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 2);
            var settings = stackalloc uint[4] { (uint)_highWater, (uint)_publishedSlots, 0, 0 };
            SDL.PushGPUComputeUniformData(command, 0, (nint)settings, 16);
            SDL.DispatchGPUCompute(compute, ((uint)_highWater + 63) / 64, 1, 1);
            SDL.EndGPUComputePass(compute);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin body publication count");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _changesStatus.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _changesDownload!.DangerousGetHandle() });
            SDL.EndGPUCopyPass(copy);
            // History has advanced once submitted. A later failure invalidates this store, never consumes changes silently.
            _failed = true; Finish(ref command);
            UploadBytes += 8; ReadbackBytes += 8; UniformBytes += 16;
            mapped = SDL.MapGPUTransferBuffer(Device, _changesDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map body publication count");
            int count;
            try
            {
                if (((uint*)mapped)[1] != 0 || *(uint*)mapped > _highWater) throw new InvalidOperationException("GPU body publication returned invalid data.");
                count = (int)*(uint*)mapped;
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _changesDownload.DangerousGetHandle()); }
            if (count > 0)
            {
                var bytes = checked((uint)(count * sizeof(BodyChange)));
                command = SDL.AcquireGPUCommandBuffer(Device);
                if (command == 0) throw GPUPhysicsDevice.Failure("acquire changed body readback");
                copy = SDL.BeginGPUCopyPass(command);
                if (copy == 0) throw GPUPhysicsDevice.Failure("begin changed body readback");
                SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _changesResults.DangerousGetHandle(), Size = bytes }, new() { TransferBuffer = _changesDownload.DangerousGetHandle(), Offset = 8 });
                SDL.EndGPUCopyPass(copy); Finish(ref command); ReadbackBytes += bytes;
                mapped = SDL.MapGPUTransferBuffer(Device, _changesDownload.DangerousGetHandle(), false);
                if (mapped == 0) throw GPUPhysicsDevice.Failure("map changed bodies");
                try { fixed (BodyChange* target = destination) System.Buffer.MemoryCopy((byte*)mapped + 8, target, bytes, bytes); }
                finally { SDL.UnmapGPUTransferBuffer(Device, _changesDownload.DangerousGetHandle()); }
            }
            _publishedSlots = _highWater; _failed = false; ChangePublicationCount++;
            return count;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }

    private void DisposeChanges()
    {
        _changesPipeline?.Dispose(); _changesHistory?.Dispose(); _changesResults?.Dispose();
        _changesStatus?.Dispose(); _changesUpload?.Dispose(); _changesDownload?.Dispose();
    }
}
