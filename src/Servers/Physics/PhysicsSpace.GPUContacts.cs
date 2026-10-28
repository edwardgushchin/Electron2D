namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private GPUPhysicsBodyStore.ContactIdentity[] _gpuReportIdentities = [];
    private int[] _gpuReportSourceOffsets = [], _gpuReportNativeOffsets = [];
    private GPUPhysicsBodyStore.ContactReport[] _gpuContactScratch = [];
    private bool _gpuUseContactKeys, _gpuFullContactsRequested;
    private long _gpuContactResultVersion;

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
        var count = 0;
        for (var i = 0; i < _gpuReportBodyCount; i++)
            count = Math.Max(count, checked(_gpuReportSourceOffsets[i] + _gpuReportCounts[i]));
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
