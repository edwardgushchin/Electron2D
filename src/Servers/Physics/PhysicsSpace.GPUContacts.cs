using System.Runtime.CompilerServices;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private GPUPhysicsBodyStore.ContactIdentity[] _gpuReportIdentities = [];
    private int[] _gpuReportSourceOffsets = [], _gpuReportNativeOffsets = [];
    private GPUPhysicsBodyStore.ContactReport[] _gpuContactScratch = [];
    private bool _gpuUseContactKeys, _gpuFullContactsRequested;
    private long _gpuContactResultVersion;

    internal (long Keys, long Reports, long Scratch) GPUContactStagingBytes =>
        ((long)_gpuReportIdentities.Length * Unsafe.SizeOf<GPUPhysicsBodyStore.ContactIdentity>(),
            (long)_gpuReports.Length * Unsafe.SizeOf<GPUPhysicsBodyStore.ContactReport>(),
            (long)_gpuContactScratch.Length * Unsafe.SizeOf<GPUPhysicsBodyStore.ContactReport>());

    internal void RequestGPUContactDetails() { if (GPUStore is not null) _gpuFullContactsRequested = true; }
    private static bool RequiresGPUContactDetails(PhysicsBodyRuntime runtime) =>
        runtime.Owners.Scene is not RigidBody rigid || runtime.ContactsReportedDepthThreshold > 0 ||
        runtime.ForceCallback is not null || runtime.SyncCallback is not null ||
        runtime.View is PhysicsDirectBodyStateExtension || rigid.HasIntegrationCallback;

    internal void PrepareGPUContactPublication()
    {
        if (!_gpuUseContactKeys) return;
        foreach (var body in _bodies)
            if (body.Runtime.ContactLimit > 0 && RequiresGPUContactDetails(body.Runtime)) { MaterializeGPUContactReports(); return; }
    }
    internal ReadOnlySpan<GPUPhysicsBodyStore.ContactReport> ReadGPUContactRow(long version, int offset, int count)
    {
        RequestGPUContactDetails();
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
            count = Math.Max(count, checked(_gpuReportSourceOffsets[i] + _gpuReportCounts[i]));
            capacity = checked(capacity + _gpuReportLimits[i]);
        }
        if (_gpuReports.Length < capacity) Array.Resize(ref _gpuReports, Math.Max(8, checked(capacity * 2)));
        var reports = ReadGPUContactRow(_gpuContactResultVersion, 0, count);
        for (var i = 0; i < _gpuReportBodyCount; i++)
        {
            var range = _gpuReportRanges[_gpuReportBodies[i].Index];
            reports.Slice(_gpuReportSourceOffsets[i], range.Count).CopyTo(_gpuReports.AsSpan(range.Offset));
        }
        foreach (var body in _bodies) body.Runtime.View?.ApplyGPUContactDetails(_gpuContactResultVersion, reports);
        foreach (var body in _serverColliders) if (!body.IsArea) body.Runtime.View?.ApplyGPUContactDetails(_gpuContactResultVersion, reports);
        _gpuUseContactKeys = false;
    }
}
