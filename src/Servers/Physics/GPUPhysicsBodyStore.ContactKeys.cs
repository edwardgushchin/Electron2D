using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ContactIdentity
    {
        internal uint Shape, ShapeGeneration, ColliderShape, ColliderShapeGeneration;
    }

    internal long ContactResultVersion { get; private set; }
    private int _frozenReportSlots;
    private void InvalidateContactResults() { _frozenReportSlots = 0; ContactResultVersion++; }

    /// <summary>Reads an already selected immutable row, without resampling body state or reselecting contacts.</summary>
    internal void ReadFrozenContactReports(long version, int offset, Span<ContactReport> results)
    {
        EnsureAccess();
        if (version != ContactResultVersion) throw new InvalidOperationException("The selected contact results were replaced.");
        if ((uint)offset > (uint)_frozenReportSlots || results.Length > _frozenReportSlots - offset)
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (results.IsEmpty) return;
        var bytes = checked(results.Length * sizeof(ContactReport));
        GrowTransfer(ref _reportDownload, ref _reportDownloadCapacity, bytes, SDL.GPUTransferBufferUsage.Download);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire frozen contact results");
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin frozen contact download");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _reportResults!.DangerousGetHandle(), Offset = checked((uint)(offset * sizeof(ContactReport))), Size = (uint)bytes },
                new() { TransferBuffer = _reportDownload!.DangerousGetHandle() });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            var mapped = SDL.MapGPUTransferBuffer(Device, _reportDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map frozen contact results");
            try { fixed (ContactReport* target = results) System.Buffer.MemoryCopy((void*)mapped, target, bytes, bytes); }
            finally { SDL.UnmapGPUTransferBuffer(Device, _reportDownload.DangerousGetHandle()); }
            ReadbackBytes += bytes; ReportResultReadbackBytes += bytes; ReportReadCount++;
            _failed = false;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }
}
