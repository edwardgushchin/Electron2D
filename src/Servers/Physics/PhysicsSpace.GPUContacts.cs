using System.Runtime.CompilerServices;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private GPUPhysicsBodyStore.ContactIdentity[] _gpuReportIdentities = [];
    private int[] _gpuReportSourceOffsets = [], _gpuReportNativeOffsets = [];
    private GPUPhysicsBodyStore.ContactReport[] _gpuContactScratch = [];
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
        foreach (var body in _bodies)
            if (body.Runtime.ContactLimit > 0 && _gpuReportRanges[GPUCollider(body.Backend).GPUHandle.Index].Keys && RequiresGPUContactDetails(body.Runtime)) { MaterializeGPUContactReports(); return; }
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
            if (_gpuReportRanges[_gpuReportBodies[i].Index].Keys)
                count = Math.Max(count, checked(_gpuReportSourceOffsets[i] + _gpuReportCounts[i]));
            capacity = checked(capacity + _gpuReportLimits[i]);
        }
        if (_gpuReports.Length < capacity) Array.Resize(ref _gpuReports, Math.Max(8, checked(capacity * 2)));
        var reports = ReadGPUContactRow(_gpuContactResultVersion, 0, count);
        for (var i = 0; i < _gpuReportBodyCount; i++)
        {
            var index = _gpuReportBodies[i].Index; var range = _gpuReportRanges[index];
            if (!range.Keys) continue;
            var offset = checked(_gpuReportFullSlots + range.Offset);
            reports.Slice(_gpuReportSourceOffsets[i], range.Count).CopyTo(_gpuReports.AsSpan(offset));
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
