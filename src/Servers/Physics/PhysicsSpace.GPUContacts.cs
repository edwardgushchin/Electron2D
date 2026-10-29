using System.Runtime.CompilerServices;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private GPUPhysicsBodyStore.ContactIdentity[] _gpuReportIdentities = [];
    private int[] _gpuReportSourceOffsets = [], _gpuReportNativeOffsets = [];
    private GPUPhysicsBodyStore.ContactReport[] _gpuContactScratch = [];
    private (int Offset, int Count)[] _gpuContactReads = [];
    private bool _gpuUseContactKeys;
    private byte[] _gpuReportFormats = [];
    private int _gpuReportFullSlots;
    private long _gpuContactResultVersion;

    internal (long Keys, long Reports, long Scratch) GPUContactStagingBytes =>
        ((long)_gpuReportIdentities.Length * Unsafe.SizeOf<GPUPhysicsBodyStore.ContactIdentity>(),
            (long)_gpuReports.Length * Unsafe.SizeOf<GPUPhysicsBodyStore.ContactReport>(),
            (long)_gpuContactScratch.Length * Unsafe.SizeOf<GPUPhysicsBodyStore.ContactReport>());

    private static bool RequiresGPUContactDetails(PhysicsBodyRuntime runtime) =>
        runtime.Owners.Scene is not RigidBody rigid || runtime.ContactsReportedDepthThreshold > 0 ||
        runtime.ForceCallback is not null || runtime.SyncCallback is not null ||
        runtime.View is PhysicsDirectBodyStateExtension || rigid.HasIntegrationCallback;

    internal void PrepareGPUContactPublication()
    {
        if (!_gpuUseContactKeys) return;
        var rows = 0; var count = 0;
        foreach (var body in _bodies)
        {
            if (body.Runtime.ContactLimit <= 0) continue;
            var index = GPUCollider(body.Backend).GPUHandle.Index; var range = _gpuReportRanges[index];
            if (!range.Keys || range.Count == 0 || !RequiresGPUContactDetails(body.Runtime)) continue;
            if (_gpuContactReads.Length == rows) Array.Resize(ref _gpuContactReads, Math.Max(8, checked((rows + 1) * 2)));
            _gpuContactReads[rows++] = (_gpuReportNativeOffsets[index], range.Count);
            count = checked(count + range.Count);
        }
        if (count == 0) return;
        var required = checked(_gpuReportFullSlots + count);
        if (_gpuReports.Length < required) Array.Resize(ref _gpuReports, Math.Max(8, checked(required * 2)));
        GPUStore!.ReadFrozenContactReports(_gpuContactResultVersion, _gpuContactReads.AsSpan(0, rows), _gpuReports.AsSpan(_gpuReportFullSlots, count));
        // Publish host ranges only after the complete selected batch has arrived.
        foreach (var body in _bodies)
        {
            if (body.Runtime.ContactLimit <= 0) continue;
            var index = GPUCollider(body.Backend).GPUHandle.Index; var range = _gpuReportRanges[index];
            if (!range.Keys || range.Count == 0 || !RequiresGPUContactDetails(body.Runtime)) continue;
            _gpuReportRanges[index] = (_gpuReportFullSlots, range.Count, false);
            _gpuReportFullSlots += range.Count;
        }
    }
    internal ReadOnlySpan<GPUPhysicsBodyStore.ContactReport> ReadGPUContactRow(long version, int offset, int count)
    {
        if (_gpuContactScratch.Length < count) Array.Resize(ref _gpuContactScratch, count);
        var reports = _gpuContactScratch.AsSpan(0, count);
        GPUStore!.ReadFrozenContactReports(version, offset, reports);
        return reports;
    }
    internal void MaterializeGPUContactReports()
    {
        if (!_gpuUseContactKeys) return;
        var count = 0; var capacity = 0;
        for (var i = 0; i < _gpuReportBodyCount; i++)
        {
            if (_gpuReportRanges[_gpuReportBodies[i].Index] is { Keys: true, Count: > 0 })
                count = Math.Max(count, checked(_gpuReportSourceOffsets[i] + _gpuReportCounts[i]));
            capacity = checked(capacity + _gpuReportLimits[i]);
        }
        if (_gpuReports.Length < capacity) Array.Resize(ref _gpuReports, Math.Max(8, checked(capacity * 2)));
        var reports = ReadGPUContactRow(_gpuContactResultVersion, 0, count);
        var next = _gpuReportFullSlots;
        for (var i = 0; i < _gpuReportBodyCount; i++)
        {
            var index = _gpuReportBodies[i].Index; var range = _gpuReportRanges[index];
            if (!range.Keys) continue;
            var offset = next; next = checked(next + _gpuReportLimits[i]);
            if (range.Count > 0) reports.Slice(_gpuReportSourceOffsets[i], range.Count).CopyTo(_gpuReports.AsSpan(offset));
            _gpuReportRanges[index] = (offset, range.Count, false);
        }
        foreach (var body in _bodies)
        {
            if (body.Runtime.ContactLimit > 0) body.Runtime.GPUContactDetailsRequested = true;
            body.Runtime.View?.ApplyGPUContactDetails(_gpuContactResultVersion, reports);
        }
        foreach (var body in _serverColliders) if (!body.IsArea)
            {
                if (body.Runtime.ContactLimit > 0) body.Runtime.GPUContactDetailsRequested = true;
                body.Runtime.View?.ApplyGPUContactDetails(_gpuContactResultVersion, reports);
            }
        _gpuReportFullSlots = capacity;
        _gpuUseContactKeys = false;
    }
}
