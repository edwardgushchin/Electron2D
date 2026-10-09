using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    /// <summary>One receiver's completed-tick contact, in scene/world units; identities retain the observed generations.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ContactReport
    {
        internal uint Shape, ShapeGeneration, ColliderShape, ColliderShapeGeneration;
        internal uint ColliderBody, ColliderGeneration;
        internal float Depth;
        internal uint Padding;
        internal Float4 Positions, NormalImpulse, Velocities;
        internal readonly Vector2 Position => new(Positions.X, Positions.Y);
        internal readonly Vector2 ColliderPosition => new(Positions.Z, Positions.W);
        internal readonly Vector2 Normal => new(NormalImpulse.X, NormalImpulse.Y);
        internal readonly Vector2 Impulse => new(NormalImpulse.Z, NormalImpulse.W);
        internal readonly Vector2 Velocity => new(Velocities.X, Velocities.Y);
        internal readonly Vector2 ColliderVelocity => new(Velocities.Z, Velocities.W);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ReportRequest { internal uint Body, Generation, Limit, Offset; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ReportSettings { internal uint Stage, Count, Records, Table, Bodies, Initialize, Points, Capacity; }
    private RenderHandle? _reportPipeline, _reportRecords, _reportTable, _reportPointHeads, _reportPointLinks, _reportBodyHeads,
        _reportResults, _reportRequests, _reportCounts, _reportSummary, _reportUpload, _reportDownload;
    private int _reportRecordCapacity, _reportTableCapacity, _reportPointHeadCapacity, _reportPointLinkCapacity, _reportBodyHeadCapacity,
        _reportResultCapacity, _reportRequestCapacity, _reportCountCapacity, _reportUploadCapacity, _reportDownloadCapacity, _reportCount, _publishedReportBodies;
    private bool _captureReports, _reportOpen, _reportInitialize, _reportReady;
    internal bool CaptureContactReports
    {
        get { EnsureAccess(); return _captureReports; }
        set { EnsureAccess(); if (_captureReports == value) return; _captureReports = value; _reportReady = false; _reportCount = 0; }
    }
    internal long ReportPublicationCount { get; private set; }
    internal long ReportReadCount { get; private set; }
    internal long ReportDeviceCapacityBytes => (long)_reportRecordCapacity * 128 + ((long)_reportTableCapacity + _reportPointHeadCapacity) * 4 +
        (long)_reportPointLinkCapacity * 8 + (long)_reportBodyHeadCapacity * 4 + (long)_reportResultCapacity * sizeof(ContactReport) +
        (long)_reportRequestCapacity * sizeof(ReportRequest) + (long)_reportCountCapacity * 4 + (_reportSummary is null ? 0 : 8);
    internal long ReportTransferCapacityBytes => (long)_reportUploadCapacity + _reportDownloadCapacity;

    private void BeginContactReports()
    { if (!_captureReports) return; _reportCount = 0; _reportInitialize = true; _reportReady = false; _reportOpen = true; }

    private void PrepareContactReports()
    {
        if (!_captureReports || !_reportOpen) return;
        _reportPipeline ??= _context.CreatePipeline("PhysicsResidentReports.comp.spv");
        // Capacity recovery happens before submission: never replay the solver to grow a publication buffer.
        Grow(ref _reportRecords, ref _reportRecordCapacity, Math.Max(1, checked(_reportCount + ContactPointCount)), 128, true);
        Grow(ref _reportTable, ref _reportTableCapacity, checked(2 * _reportRecordCapacity), 4, false);
        Grow(ref _reportPointHeads, ref _reportPointHeadCapacity, _reportTableCapacity, 4, false);
        Grow(ref _reportPointLinks, ref _reportPointLinkCapacity, Math.Max(1, ContactPointCount), 8, false);
        Grow(ref _reportBodyHeads, ref _reportBodyHeadCapacity, _highWater, 4, false);
        Grow(ref _reportResults, ref _reportResultCapacity, 1, sizeof(ContactReport), false);
        Grow(ref _reportRequests, ref _reportRequestCapacity, 1, sizeof(ReportRequest), false);
        Grow(ref _reportCounts, ref _reportCountCapacity, 1, 4, false);
        _reportSummary ??= Buffer(8);
        GrowTransfer(ref _reportUpload, ref _reportUploadCapacity, 8, SDL.GPUTransferBufferUsage.Upload);
        GrowTransfer(ref _reportDownload, ref _reportDownloadCapacity, 8, SDL.GPUTransferBufferUsage.Download);
    }
    private void ResetReportStatus(nint command)
    {
        var mapped = SDL.MapGPUTransferBuffer(Device, _reportUpload!.DangerousGetHandle(), false);
        if (mapped == 0) throw GPUPhysicsDevice.Failure("map contact report status");
        ((uint*)mapped)[0] = 0; ((uint*)mapped)[1] = (uint)_reportCount;
        SDL.UnmapGPUTransferBuffer(Device, _reportUpload.DangerousGetHandle());
        var copy = SDL.BeginGPUCopyPass(command);
        if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact report reset");
        SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _reportUpload.DangerousGetHandle() }, new() { Buffer = _reportSummary!.DangerousGetHandle(), Size = 8 }, false);
        SDL.EndGPUCopyPass(copy); UploadBytes += 8;
    }
    private void RecordContactReports(nint command)
    {
        if (!_captureReports || !_reportOpen) return;
        ResetReportStatus(command);
        ReportPass(command, 0, Math.Max(_reportTableCapacity, _reportInitialize ? _highWater : 0));
        ReportPass(command, 1, _reportCount);
        ReportPass(command, 2, ContactPointCount);
        ReportPass(command, 3, ContactPointCount);
        DownloadReportStatus(command);
    }
    private void DownloadReportStatus(nint command)
    {
        var copy = SDL.BeginGPUCopyPass(command);
        if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact report status");
        SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _reportSummary!.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _reportDownload!.DangerousGetHandle() });
        SDL.EndGPUCopyPass(copy); ReadbackBytes += 8;
    }
    private void AcceptContactReports()
    {
        if (!_captureReports || !_reportOpen) return;
        var mapped = SDL.MapGPUTransferBuffer(Device, _reportDownload!.DangerousGetHandle(), false);
        if (mapped == 0) throw GPUPhysicsDevice.Failure("map completed contact reports");
        try
        {
            if (*(uint*)mapped != 0 || ((uint*)mapped)[1] > _reportRecordCapacity)
                throw new InvalidOperationException("GPU contact publication returned invalid data.");
            _reportCount = (int)((uint*)mapped)[1];
        }
        finally { SDL.UnmapGPUTransferBuffer(Device, _reportDownload.DangerousGetHandle()); }
        _reportInitialize = false;
    }
    private void EndContactReports(bool idle = false)
    {
        if (!_captureReports) return;
        if (_reportCount > 0)
        {
            var command = SDL.AcquireGPUCommandBuffer(Device);
            if (command == 0) throw GPUPhysicsDevice.Failure("acquire final contact publication");
            try
            {
                ResetReportStatus(command); ReportPass(command, idle ? 6u : 4u, idle ? _reportCount : _highWater);
                DownloadReportStatus(command); _failed = true; Finish(ref command);
                _reportOpen = true; AcceptContactReports(); _failed = false;
            }
            finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
        }
        _reportOpen = false; _reportReady = true; _publishedReportBodies = _highWater; ReportPublicationCount++;
    }

    /// <summary>Reads capped snapshots for requested bodies; each result segment has its requested limit, with counts identifying valid entries.</summary>
    internal void ReadContactReports(ReadOnlySpan<BodyHandle> bodies, ReadOnlySpan<int> limits, Span<int> counts, Span<ContactReport> results)
    {
        EnsureAccess();
        if (limits.Length != bodies.Length || counts.Length < bodies.Length) throw new ArgumentException("Contact request/count lengths differ.");
        var total = 0;
        for (var i = 0; i < bodies.Length; i++)
        {
            Validate(bodies[i]);
            if (limits[i] < 0 || limits[i] > PhysicsBodyRuntime.MaxContactLimit) throw new ArgumentOutOfRangeException(nameof(limits));
            total = checked(total + limits[i]);
        }
        if (results.Length < total) throw new ArgumentException("The contact report destination is too small.", nameof(results));
        if (!_captureReports || !_reportReady || _reportCount == 0 || total == 0) { counts[..bodies.Length].Clear(); return; }
        Grow(ref _reportRequests, ref _reportRequestCapacity, bodies.Length, sizeof(ReportRequest), false);
        Grow(ref _reportCounts, ref _reportCountCapacity, bodies.Length, 4, false);
        Grow(ref _reportResults, ref _reportResultCapacity, total, sizeof(ContactReport), false);
        var requestBytes = checked(bodies.Length * sizeof(ReportRequest)); var countBytes = checked(bodies.Length * 4); var resultBytes = checked(total * sizeof(ContactReport));
        GrowTransfer(ref _reportUpload, ref _reportUploadCapacity, checked(8 + requestBytes), SDL.GPUTransferBufferUsage.Upload);
        GrowTransfer(ref _reportDownload, ref _reportDownloadCapacity, checked(8 + countBytes + resultBytes), SDL.GPUTransferBufferUsage.Download);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire selected contact reports");
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _reportUpload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map contact report requests");
            *(ulong*)mapped = 0; var requests = (ReportRequest*)((byte*)mapped + 8); uint offset = 0;
            for (var i = 0; i < bodies.Length; i++)
            { requests[i] = new() { Body = (uint)bodies[i].Index, Generation = bodies[i].Generation, Limit = (uint)limits[i], Offset = offset }; offset += (uint)limits[i]; }
            SDL.UnmapGPUTransferBuffer(Device, _reportUpload.DangerousGetHandle());
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact report requests");
            SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _reportUpload.DangerousGetHandle() }, new() { Buffer = _reportSummary!.DangerousGetHandle(), Size = 8 }, false);
            SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _reportUpload.DangerousGetHandle(), Offset = 8 }, new() { Buffer = _reportRequests!.DangerousGetHandle(), Size = (uint)requestBytes }, false);
            SDL.EndGPUCopyPass(copy); ReportPass(command, 5, bodies.Length);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin selected contact download");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _reportSummary.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _reportDownload!.DangerousGetHandle() });
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _reportCounts!.DangerousGetHandle(), Size = (uint)countBytes }, new() { TransferBuffer = _reportDownload.DangerousGetHandle(), Offset = 8 });
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _reportResults!.DangerousGetHandle(), Size = (uint)resultBytes }, new() { TransferBuffer = _reportDownload.DangerousGetHandle(), Offset = (uint)(8 + countBytes) });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            UploadBytes += 8 + requestBytes; ReadbackBytes += 8 + countBytes + resultBytes; ReportReadCount++;
            mapped = SDL.MapGPUTransferBuffer(Device, _reportDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map selected contact results");
            try
            {
                if (*(uint*)mapped != 0) throw new InvalidOperationException("GPU contact report selection returned invalid data.");
                fixed (int* target = counts) System.Buffer.MemoryCopy((byte*)mapped + 8, target, countBytes, countBytes);
                fixed (ContactReport* target = results) System.Buffer.MemoryCopy((byte*)mapped + 8 + countBytes, target, resultBytes, resultBytes);
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _reportDownload.DangerousGetHandle()); }
            _failed = false;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }
    private void ReportPass(nint command, uint stage, int count)
    {
        if (count == 0) return;
        var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[8];
        outputs[0] = new() { Buffer = _reportRecords!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _reportTable!.DangerousGetHandle() };
        outputs[2] = new() { Buffer = _reportPointHeads!.DangerousGetHandle() }; outputs[3] = new() { Buffer = _reportPointLinks!.DangerousGetHandle() };
        outputs[4] = new() { Buffer = _reportBodyHeads!.DangerousGetHandle() }; outputs[5] = new() { Buffer = _reportResults!.DangerousGetHandle() };
        outputs[6] = new() { Buffer = _reportCounts!.DangerousGetHandle() }; outputs[7] = new() { Buffer = _reportSummary!.DangerousGetHandle() };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 8);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin contact report pass");
        SDL.BindGPUComputePipeline(compute, _reportPipeline!.DangerousGetHandle());
        var inputs = stackalloc nint[6] { _contactsGPU!.DangerousGetHandle(), _shapesGPU!.DangerousGetHandle(), _bodies!.DangerousGetHandle(),
            _centers!.DangerousGetHandle(), _constraintImpulsesGPU!.DangerousGetHandle(), _reportRequests!.DangerousGetHandle() };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 6);
        var settings = new ReportSettings
        {
            Stage = stage,
            Count = (uint)count,
            Records = (uint)_reportCount,
            Table = (uint)_reportTableCapacity,
            Bodies = (uint)(stage == 5 ? _publishedReportBodies : _highWater),
            Initialize = _reportInitialize ? 1u : 0u,
            Points = (uint)ContactPointCount,
            Capacity = (uint)_reportRecordCapacity
        };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(ReportSettings));
        SDL.DispatchGPUCompute(compute, ((uint)count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute); UniformBytes += sizeof(ReportSettings);
    }
    private void DisposeReports()
    { _reportPipeline?.Dispose(); _reportRecords?.Dispose(); _reportTable?.Dispose(); _reportPointHeads?.Dispose(); _reportPointLinks?.Dispose(); _reportBodyHeads?.Dispose(); _reportResults?.Dispose(); _reportRequests?.Dispose(); _reportCounts?.Dispose(); _reportSummary?.Dispose(); _reportUpload?.Dispose(); _reportDownload?.Dispose(); }
}
