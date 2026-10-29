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
        => ReadFrozenContactReports(version, [(offset, results.Length)], results);

    /// <summary>Reads immutable rows in request order through one packed transfer and completed submission.</summary>
    internal void ReadFrozenContactReports(long version, ReadOnlySpan<(int Offset, int Count)> rows, Span<ContactReport> results)
    {
        EnsureAccess();
        if (version != ContactResultVersion) throw new InvalidOperationException("The selected contact results were replaced.");
        var total = 0;
        foreach (var row in rows)
        {
            if ((uint)row.Offset > (uint)_frozenReportSlots || (uint)row.Count > (uint)(_frozenReportSlots - row.Offset))
                throw new ArgumentOutOfRangeException(nameof(rows));
            if (row.Count > results.Length - total) throw new ArgumentException("The destination cannot hold the selected rows.", nameof(results));
            total += row.Count;
        }
        if (total != results.Length) throw new ArgumentException("The destination must match the selected rows.", nameof(results));
        if (MemoryMarshal.AsBytes(rows).Overlaps(MemoryMarshal.AsBytes(results)))
            throw new ArgumentException("Contact row requests cannot overlap their destination.", nameof(rows));
        if (results.IsEmpty) return;
        var bytes = checked(results.Length * sizeof(ContactReport));
        GrowTransfer(ref _reportDownload, ref _reportDownloadCapacity, bytes, SDL.GPUTransferBufferUsage.Download);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire frozen contact results");
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin frozen contact download");
            uint written = 0;
            foreach (var row in rows)
            {
                if (row.Count == 0) continue;
                var size = checked((uint)(row.Count * sizeof(ContactReport)));
                SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _reportResults!.DangerousGetHandle(), Offset = checked((uint)(row.Offset * sizeof(ContactReport))), Size = size },
                    new() { TransferBuffer = _reportDownload!.DangerousGetHandle(), Offset = written });
                written += size;
            }
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            var mapped = SDL.MapGPUTransferBuffer(Device, _reportDownload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map frozen contact results");
            try { fixed (ContactReport* target = results) System.Buffer.MemoryCopy((void*)mapped, target, bytes, bytes); }
            finally { SDL.UnmapGPUTransferBuffer(Device, _reportDownload.DangerousGetHandle()); }
            ReadbackBytes += bytes; ReportResultReadbackBytes += bytes; ReportReadCount++;
            _failed = false;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }
}
