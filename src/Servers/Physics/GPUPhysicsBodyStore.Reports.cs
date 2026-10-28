using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    /// <summary>One receiver's completed-tick contact, in scene/world units; identities retain the observed generations.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct ContactReport
    {
        internal uint Shape, ShapeGeneration, ColliderShape, ColliderShapeGeneration;
        internal float Depth;
        // Collider ownership resolves through the retained shape generation; the scalar wire stride is 68 bytes.
        internal Float4 Positions, NormalImpulse, Velocities;
        internal readonly Vector2 Position => new(Positions.X, Positions.Y);
        internal readonly Vector2 ColliderPosition => new(Positions.Z, Positions.W);
        internal readonly Vector2 Normal => new(NormalImpulse.X, NormalImpulse.Y);
        internal readonly Vector2 Impulse => new(NormalImpulse.Z, NormalImpulse.W);
        internal readonly Vector2 Velocity => new(Velocities.X, Velocities.Y);
        internal readonly Vector2 ColliderVelocity => new(Velocities.Z, Velocities.W);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ReportRequest { internal uint Body, Generation, Limit; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ReportSettings { internal uint Stage, Count, Records, Table, Bodies, Initialize, Points, Capacity; }
    private RenderHandle? _reportPipeline, _reportRecords, _reportTable, _reportPointHeads, _reportPointLinks, _reportBodyHeads,
        _reportResults, _reportRequests, _reportCounts, _reportSummary, _reportUpload, _reportDownload;
    private int _reportRecordCapacity, _reportTableCapacity, _reportPointHeadCapacity, _reportPointLinkCapacity, _reportBodyHeadCapacity,
        _reportResultCapacity, _reportRequestCapacity, _reportCountCapacity, _reportUploadCapacity, _reportDownloadCapacity, _reportCount, _publishedReportBodies;
    private int[] _reportRequestEpochs = [], _reportRequestMultiplicities = [];
    private int _reportRequestEpoch;
    private bool _captureReports, _reportOpen, _reportInitialize, _reportReady;
    // Diagnostic control for identical report freezing with a separate completion.
    internal bool SeparateReportSubmissions;
    private bool ShareFinalContactReports => _captureReports && _reportOpen && !SeparateReportSubmissions;
    internal bool CaptureContactReports
    {
        get { EnsureAccess(); return _captureReports; }
        set { EnsureAccess(); if (_captureReports == value) return; _captureReports = value; _reportReady = false; _reportCount = 0; }
    }
    internal long ReportPublicationCount { get; private set; }
    internal long ReportReadCount { get; private set; }
    internal long ReportResultReadbackBytes { get; private set; }
    internal long ReportDeviceCapacityBytes => (long)_reportRecordCapacity * 128 + ((long)_reportTableCapacity + _reportPointHeadCapacity) * 4 +
        (long)_reportPointLinkCapacity * 8 + (long)_reportBodyHeadCapacity * 4 + (long)_reportResultCapacity * sizeof(ContactReport) +
        (long)_reportRequestCapacity * sizeof(ReportRequest) + (long)_reportCountCapacity * 8 + (_reportSummary is null ? 0 : 8);
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
        Grow(ref _reportCounts, ref _reportCountCapacity, 1, 8, false);
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
    private void RecordContactReports(nint command, bool download = true)
    {
        if (!_captureReports || !_reportOpen) return;
        ResetReportStatus(command);
        ReportPass(command, 0, Math.Max(_reportTableCapacity, _reportInitialize ? _highWater : 0));
        ReportPass(command, 1, _reportCount);
        ReportPass(command, 2, ContactPointCount);
        ReportPass(command, 3, ContactPointCount);
        if (download) DownloadReportStatus(command);
    }
    /// <summary>Freezes final point velocities after the last numerical stage, using its current device record count.</summary>
    private void RecordFinalContactReports(nint command, bool resetStatus)
    {
        if (resetStatus) ResetReportStatus(command);
        ReportPass(command, 4, _highWater); DownloadReportStatus(command);
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
        if (!_captureReports || !idle && !_reportOpen) return;
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
        CompleteContactReports();
    }

    private void CompleteContactReports()
    { _reportOpen = false; _reportReady = true; _publishedReportBodies = _highWater; ReportPublicationCount++; }

    /// <summary>Caller spans for one synchronous report read, optionally sharing a state-publication submission.</summary>
    internal readonly ref struct ContactRead
    {
        internal readonly ReadOnlySpan<BodyHandle> Bodies;
        internal readonly ReadOnlySpan<int> Limits;
        internal readonly Span<int> Counts;
        internal readonly Span<ContactReport> Results;
        internal readonly Span<ContactIdentity> Identities;
        internal readonly Span<int> SourceOffsets;
        internal readonly bool IdentityOnly;
        internal ContactRead(ReadOnlySpan<BodyHandle> bodies, ReadOnlySpan<int> limits, Span<int> counts, Span<ContactReport> results)
        { Bodies = bodies; Limits = limits; Counts = counts; Results = results; }
        internal ContactRead(ReadOnlySpan<BodyHandle> bodies, ReadOnlySpan<int> limits, Span<int> counts, Span<ContactIdentity> identities, Span<int> sourceOffsets)
        { Bodies = bodies; Limits = limits; Counts = counts; Identities = identities; SourceOffsets = sourceOffsets; IdentityOnly = true; }
        internal int WireStride => IdentityOnly ? sizeof(ContactIdentity) : sizeof(ContactReport);
    }

    /// <summary>Reads capped snapshots for requested bodies; each result segment has its requested limit, with counts identifying valid entries.</summary>
    internal void ReadContactReports(ReadOnlySpan<BodyHandle> bodies, ReadOnlySpan<int> limits, Span<int> counts, Span<ContactReport> results) =>
        ReadContactReports(new(bodies, limits, counts, results));

    internal void ReadContactReports(ContactRead read)
    {
        var resultCount = PrepareContactRead(read);
        if (resultCount == 0) { AcceptContactRead(read, 0); return; }
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire selected contact reports");
        try
        {
            RecordContactRead(command, read, resultCount); _failed = true; Finish(ref command);
            AcceptContactRead(read, resultCount); _failed = false;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }

    private int PrepareContactRead(ContactRead read)
    {
        EnsureAccess();
        var bodies = read.Bodies; var limits = read.Limits; var counts = read.Counts; var results = read.Results;
        if (limits.Length != bodies.Length || counts.Length < bodies.Length) throw new ArgumentException("Contact request/count lengths differ.");
        if (read.IdentityOnly && (read.SourceOffsets.Length < bodies.Length || read.SourceOffsets.Overlaps(limits) || read.SourceOffsets.Overlaps(counts)))
            throw new ArgumentException("Contact source offsets require a separate destination for every receiver.");
        var total = 0; var largestLimit = 0;
        for (var i = 0; i < bodies.Length; i++)
        {
            Validate(bodies[i]);
            if (limits[i] < 0 || limits[i] > PhysicsBodyRuntime.MaxContactLimit) throw new ArgumentOutOfRangeException(nameof(limits));
            total = checked(total + limits[i]);
            largestLimit = Math.Max(largestLimit, limits[i]);
        }
        if ((read.IdentityOnly ? read.Identities.Length : results.Length) < total) throw new ArgumentException("The contact report destination is too small.", nameof(results));
        if (!_captureReports || !_reportReady || _reportCount == 0 || total == 0) return 0;
        if (_reportRequestEpochs.Length < _highWater)
        {
            Array.Resize(ref _reportRequestEpochs, _slots.Length); Array.Resize(ref _reportRequestMultiplicities, _slots.Length);
        }
        if (_reportRequestEpoch == int.MaxValue) { Array.Clear(_reportRequestEpochs); _reportRequestEpoch = 0; }
        var epoch = ++_reportRequestEpoch; var multiplicity = 0;
        foreach (var body in bodies)
        {
            if (_reportRequestEpochs[body.Index] != epoch) { _reportRequestEpochs[body.Index] = epoch; _reportRequestMultiplicities[body.Index] = 0; }
            multiplicity = Math.Max(multiplicity, ++_reportRequestMultiplicities[body.Index]);
        }
        // A frame record belongs to at most two receivers; repeated requests multiply that bound.
        var resultCount = (int)Math.Min(total, 2L * _reportCount * multiplicity);
        Grow(ref _reportRequests, ref _reportRequestCapacity, bodies.Length, sizeof(ReportRequest), false);
        Grow(ref _reportCounts, ref _reportCountCapacity, bodies.Length, 8, false);
        if (resultCount > _reportResultCapacity) InvalidateContactResults();
        Grow(ref _reportResults, ref _reportResultCapacity, resultCount, sizeof(ContactReport), false);
        if (read.IdentityOnly)
        {
            // Selection is complete; its hash scratch can hold compact keys without adding a ninth writable binding.
            Grow(ref _reportTable, ref _reportTableCapacity, checked(resultCount * 4), 4, false);
            Grow(ref _reportPointHeads, ref _reportPointHeadCapacity, _reportTableCapacity, 4, false);
        }
        var requestBytes = checked(bodies.Length * sizeof(ReportRequest)); var countBytes = checked(bodies.Length * 8); var resultBytes = checked(resultCount * read.WireStride);
        GrowTransfer(ref _reportUpload, ref _reportUploadCapacity, checked(8 + requestBytes), SDL.GPUTransferBufferUsage.Upload);
        GrowTransfer(ref _reportDownload, ref _reportDownloadCapacity,
            Math.Max(checked(8 + countBytes + resultBytes), read.IdentityOnly ? checked(largestLimit * sizeof(ContactReport)) : 0), SDL.GPUTransferBufferUsage.Download);
        return resultCount;
    }

    private void RecordContactRead(nint command, ContactRead read, int resultCount)
    {
        if (resultCount == 0) return;
        var bodies = read.Bodies; var limits = read.Limits;
        var requestBytes = checked(bodies.Length * sizeof(ReportRequest)); var countBytes = checked(bodies.Length * 8); var resultBytes = checked(resultCount * read.WireStride);
        var mapped = SDL.MapGPUTransferBuffer(Device, _reportUpload!.DangerousGetHandle(), false);
        if (mapped == 0) throw GPUPhysicsDevice.Failure("map contact report requests");
        *(ulong*)mapped = 0; var requests = (ReportRequest*)((byte*)mapped + 8);
        for (var i = 0; i < bodies.Length; i++)
        { requests[i] = new() { Body = (uint)bodies[i].Index, Generation = bodies[i].Generation, Limit = (uint)limits[i] }; }
        SDL.UnmapGPUTransferBuffer(Device, _reportUpload.DangerousGetHandle());
        var copy = SDL.BeginGPUCopyPass(command);
        if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact report requests");
        SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _reportUpload.DangerousGetHandle() }, new() { Buffer = _reportSummary!.DangerousGetHandle(), Size = 8 }, false);
        SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _reportUpload.DangerousGetHandle(), Offset = 8 }, new() { Buffer = _reportRequests!.DangerousGetHandle(), Size = (uint)requestBytes }, false);
        SDL.EndGPUCopyPass(copy); ReportPass(command, 5, bodies.Length, resultCount);
        _frozenReportSlots = resultCount; ContactResultVersion++;
        if (read.IdentityOnly) ReportPass(command, 7, resultCount, resultCount);
        copy = SDL.BeginGPUCopyPass(command);
        if (copy == 0) throw GPUPhysicsDevice.Failure("begin selected contact download");
        SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _reportSummary.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _reportDownload!.DangerousGetHandle() });
        SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _reportCounts!.DangerousGetHandle(), Size = (uint)countBytes }, new() { TransferBuffer = _reportDownload.DangerousGetHandle(), Offset = 8 });
        SDL.DownloadFromGPUBuffer(copy, new() { Buffer = (read.IdentityOnly ? _reportTable : _reportResults)!.DangerousGetHandle(), Size = (uint)resultBytes }, new() { TransferBuffer = _reportDownload.DangerousGetHandle(), Offset = (uint)(8 + countBytes) });
        SDL.EndGPUCopyPass(copy);
    }

    private void AcceptContactRead(ContactRead read, int resultCount)
    {
        var bodies = read.Bodies; var limits = read.Limits; var counts = read.Counts; var results = read.Results;
        if (resultCount == 0) { if (read.IdentityOnly) read.SourceOffsets[..bodies.Length].Clear(); counts[..bodies.Length].Clear(); return; }
        var countBytes = checked(bodies.Length * 8);
        UploadBytes += 8 + bodies.Length * sizeof(ReportRequest); ReadbackBytes += 8 + countBytes + resultCount * read.WireStride; ReportReadCount++;
        ReportResultReadbackBytes += (long)resultCount * read.WireStride;
        var mapped = SDL.MapGPUTransferBuffer(Device, _reportDownload!.DangerousGetHandle(), false);
        if (mapped == 0) throw GPUPhysicsDevice.Failure("map selected contact results");
        try
        {
            var selected = ((uint*)mapped)[1]; var rows = (uint*)((byte*)mapped + 8); ulong sum = 0;
            if (*(uint*)mapped != 0 || selected > resultCount) throw new InvalidOperationException("GPU contact report selection returned invalid data.");
            for (var i = 0; i < bodies.Length; i++)
            {
                var count = rows[i * 2]; var first = rows[i * 2 + 1];
                if (count > limits[i] || first > selected || count > selected - first) throw new InvalidOperationException("GPU contact report selection returned invalid ranges.");
                sum += count;
            }
            if (sum != selected) throw new InvalidOperationException("GPU contact report selection returned inconsistent counts.");
            fixed (ContactReport* full = results)
            fixed (ContactIdentity* keys = read.Identities)
            {
                var target = read.IdentityOnly ? (byte*)keys : (byte*)full;
                var offset = 0; var payload = (byte*)mapped + 8 + countBytes;
                for (var i = 0; i < bodies.Length; i++)
                {
                    var count = rows[i * 2]; var first = rows[i * 2 + 1];
                    if (count != 0) System.Buffer.MemoryCopy(payload + (long)first * read.WireStride, target + (long)offset * read.WireStride,
                        (long)limits[i] * read.WireStride, (long)count * read.WireStride);
                    offset += limits[i];
                }
            }
            if (read.IdentityOnly) for (var i = 0; i < bodies.Length; i++) read.SourceOffsets[i] = (int)rows[i * 2 + 1];
            // Commit counts after scattering so callers may alias their limits and count spans.
            for (var i = 0; i < bodies.Length; i++) counts[i] = (int)rows[i * 2];
        }
        finally { SDL.UnmapGPUTransferBuffer(Device, _reportDownload.DangerousGetHandle()); }
    }
    private void ReportPass(nint command, uint stage, int count, int resultCount = 0)
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
            Capacity = (uint)(stage == 5 ? resultCount : _reportRecordCapacity)
        };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(ReportSettings));
        SDL.DispatchGPUCompute(compute, ((uint)count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute); UniformBytes += sizeof(ReportSettings);
    }
    private void DisposeReports()
    { _reportRequestEpochs = []; _reportRequestMultiplicities = []; _reportPipeline?.Dispose(); _reportRecords?.Dispose(); _reportTable?.Dispose(); _reportPointHeads?.Dispose(); _reportPointLinks?.Dispose(); _reportBodyHeads?.Dispose(); _reportResults?.Dispose(); _reportRequests?.Dispose(); _reportCounts?.Dispose(); _reportSummary?.Dispose(); _reportUpload?.Dispose(); _reportDownload?.Dispose(); }
}
