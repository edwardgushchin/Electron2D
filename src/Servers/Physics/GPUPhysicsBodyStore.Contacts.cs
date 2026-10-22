using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    /// <summary>A device contact point: shape generations/features, A-to-B normal, signed separation and body-local anchors.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ContactPoint
    {
        internal uint ShapeA, ShapeB, GenerationA, GenerationB;
        internal uint FeatureA, FeatureB, PieceA, PieceB;
        internal Float4 Normal, Anchors;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ContactSettings
    {
        internal uint Pairs, Capacity, Shapes, Bodies;
        internal Float4 Tolerances;
        internal uint Stage, HistoryCount, HistoryCapacity, NextCapacity;
        internal uint NextTableCapacity, Padding1, Padding2, Padding3;
    }

    private RenderHandle? _contactPipeline, _contactsGPU, _contactSummary;
    private int _contactCapacity;
    private long _contactPairVersion = -1;
    private float _contactMargin, _contactSensorMargin;
    internal int ContactPointCount { get; private set; }
    internal long ContactCapacityRetries { get; private set; }
    internal long ContactSubmissionCount { get; private set; }
    // Diagnostic control for the same pair/contact kernels with separate completion waits.
    internal bool SeparateContactSubmissions;

    /// <summary>Finds resident contacts, sharing fresh pair discovery and contact completion while retaining bounded capacity recovery.</summary>
    internal int FindContacts(float margin = 0, float sensorMargin = 0)
    {
        EnsureAccess();
        if (!float.IsFinite(margin) || margin < 0) throw new ArgumentOutOfRangeException(nameof(margin));
        if (!float.IsFinite(sensorMargin) || sensorMargin < 0) throw new ArgumentOutOfRangeException(nameof(sensorMargin));
        var pairMargin = MathF.Max(margin, sensorMargin);
        if (ShapeCount >= 2 && !SeparateContactSubmissions)
        {
            Step(0, default); FlushJoints();
            if (!PairsCurrent(pairMargin)) return FindContactBatch(margin, sensorMargin, pairMargin);
        }
        var pairs = FindPairs(pairMargin);
        if (_contactPairVersion == BroadPhaseSubmissionCount && _contactMargin == margin && _contactSensorMargin == sensorMargin) return ContactPointCount;
        _contactMargin = margin; _contactSensorMargin = sensorMargin;
        if (pairs == 0) { _oneWayHistoryCount = 0; ContactPointCount = 0; _contactPairVersion = BroadPhaseSubmissionCount; return 0; }
        PrepareContacts();
        var retry = false;
        while (true)
        {
            var (count, oneWayCount) = DispatchContacts(margin);
            if (count > _contactCapacity || oneWayCount > _oneWayNextCapacity)
            {
                if (retry) { _failed = true; throw new InvalidOperationException("The immutable GPU contact batch changed during capacity recovery."); }
                Grow(ref _contactsGPU, ref _contactCapacity, count, sizeof(ContactPoint), false);
                Grow(ref _oneWayNextGPU, ref _oneWayNextCapacity, oneWayCount, 48, false);
                Grow(ref _oneWayNextTableGPU, ref _oneWayNextTableCapacity, checked(2 * _oneWayNextCapacity), 4, false);
                ContactCapacityRetries++; retry = true; continue;
            }
            PublishOneWay(oneWayCount);
            ContactPointCount = count; _contactPairVersion = BroadPhaseSubmissionCount; return count;
        }
    }

    private void PrepareContacts()
    {
        _contactPipeline ??= _context.CreatePipeline("PhysicsResidentContacts.comp.spv");
        _contactSummary ??= Buffer(8);
        Grow(ref _contactsGPU, ref _contactCapacity, 1, sizeof(ContactPoint), false);
        EnsureOneWay();
    }

    private int FindContactBatch(float margin, float sensorMargin, float pairMargin)
    {
        _pairBodyVersion = -1; _pairMargin = pairMargin; _pairSweepDelta = 0; _pairSweepCorrections = false;
        _contactMargin = margin; _contactSensorMargin = sensorMargin;
        PrepareGeometry(); EnsureSpatial(); PrepareContacts();
        var retry = false; var pairRetried = false; var contactRetried = false;
        while (true)
        {
            var counts = DispatchSpatial(retry, contacts: true);
            if (counts.Pairs > _pairCapacity)
            {
                if (pairRetried) { _failed = true; throw new InvalidOperationException("The immutable GPU pair batch changed during capacity recovery."); }
                Grow(ref _pairsGPU, ref _pairCapacity, counts.Pairs, sizeof(PairData), false);
                PairCapacityRetries++; pairRetried = retry = true; continue;
            }
            if (counts.Contacts > _contactCapacity || counts.OneWay > _oneWayNextCapacity)
            {
                if (contactRetried) { _failed = true; throw new InvalidOperationException("The immutable GPU contact batch changed during capacity recovery."); }
                Grow(ref _contactsGPU, ref _contactCapacity, counts.Contacts, sizeof(ContactPoint), false);
                Grow(ref _oneWayNextGPU, ref _oneWayNextCapacity, counts.OneWay, 48, false);
                Grow(ref _oneWayNextTableGPU, ref _oneWayNextTableCapacity, checked(2 * _oneWayNextCapacity), 4, false);
                ContactCapacityRetries++; contactRetried = retry = true; continue;
            }
            PairCount = counts.Pairs; CommitGeometry();
            _pairBodyVersion = _boundsBodyVersion = _bodyVersion; _pairShapeVersion = _boundsShapeVersion = _shapeVersion;
            PublishOneWay(counts.OneWay);
            ContactPointCount = counts.Contacts; _contactPairVersion = BroadPhaseSubmissionCount;
            return ContactPointCount;
        }
    }

    internal int ReadContacts(Span<ContactPoint> destination, float margin = 0)
    {
        var count = FindContacts(margin);
        if (destination.Length < count) throw new ArgumentException("The contact destination is too small.", nameof(destination));
        if (count == 0) return 0;
        var bytes = checked((uint)(count * sizeof(ContactPoint)));
        DownloadSpatial(_contactsGPU!, 0, bytes);
        var mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload!.DangerousGetHandle(), false);
        if (mapped == 0) { _failed = true; throw GPUPhysicsDevice.Failure("map resident contacts"); }
        try { fixed (ContactPoint* target = destination) System.Buffer.MemoryCopy((void*)mapped, target, bytes, bytes); }
        finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
        return count;
    }

    private (int Contacts, int OneWay) DispatchContacts(float margin)
    {
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire resident contacts");
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _spatialUpload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map contact status reset");
            *(ulong*)mapped = 0; SDL.UnmapGPUTransferBuffer(Device, _spatialUpload.DangerousGetHandle());
            var uniformBytes = RecordContacts(command, margin, residentPairs: false);
            DownloadContactStatus(command, 0); _failed = true; Finish(ref command);
            UploadBytes += _oneWayShapeCount > 0 ? 16 : 8; ReadbackBytes += _oneWayShapeCount > 0 ? 16 : 8; UniformBytes += uniformBytes; ContactSubmissionCount++;
            mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map contact summary");
            (int Contacts, int OneWay) counts;
            try { counts = AcceptContactStatus(mapped, 0); }
            finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
            _failed = false; return counts;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }

    private int RecordContacts(nint command, float margin, bool residentPairs)
    {
        var copy = SDL.BeginGPUCopyPass(command);
        if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact status reset");
        UploadSpatial(copy, _contactSummary!, 0, 8);
        if (_oneWayShapeCount > 0) UploadSpatial(copy, _oneWaySummary!, 0, 8);
        SDL.EndGPUCopyPass(copy);
        var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[5];
        outputs[0] = new() { Buffer = _contactsGPU!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _contactSummary!.DangerousGetHandle() };
        outputs[2] = new() { Buffer = _oneWayNextGPU!.DangerousGetHandle() };
        outputs[3] = new() { Buffer = _oneWayNextTableGPU!.DangerousGetHandle() };
        outputs[4] = new() { Buffer = _oneWaySummary!.DangerousGetHandle() };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 5);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident contact compute");
        SDL.BindGPUComputePipeline(compute, _contactPipeline!.DangerousGetHandle());
        var inputs = stackalloc nint[8] { _bodies!.DangerousGetHandle(), _verticesGPU!.DangerousGetHandle(), _geometryGPU!.DangerousGetHandle(), _shapesGPU!.DangerousGetHandle(), _pairsGPU!.DangerousGetHandle(), _oneWayHistoryGPU!.DangerousGetHandle(), _oneWayHistoryTableGPU!.DangerousGetHandle(), _spatialSummary!.DangerousGetHandle() };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 8);
        var settings = new ContactSettings
        {
            Pairs = (uint)(residentPairs ? _pairCapacity : PairCount),
            Capacity = (uint)_contactCapacity,
            Shapes = (uint)_shapeHighWater,
            Bodies = (uint)_highWater,
            Tolerances = new(margin, 0.5f, 0.00001f, _contactSensorMargin),
            HistoryCount = (uint)_oneWayHistoryCount,
            HistoryCapacity = (uint)OneWayHistoryTableSize,
            NextCapacity = (uint)_oneWayNextCapacity,
            NextTableCapacity = (uint)_oneWayNextTableCapacity,
            Padding1 = residentPairs ? 1u : 0u
        };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(ContactSettings));
        SDL.DispatchGPUCompute(compute, (settings.Pairs + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
        if (_oneWayShapeCount > 0)
            for (uint stage = 1; stage <= 2; stage++)
            {
                compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 5);
                if (compute == 0) throw GPUPhysicsDevice.Failure("begin one-way history publication");
                SDL.BindGPUComputePipeline(compute, _contactPipeline.DangerousGetHandle());
                SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 8);
                settings.Stage = stage;
                SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(ContactSettings));
                SDL.DispatchGPUCompute(compute, (uint)((stage == 1 ? _oneWayNextTableCapacity : _oneWayNextCapacity) + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
            }
        return sizeof(ContactSettings) * (_oneWayShapeCount > 0 ? 3 : 1);
    }

    private void DownloadContactStatus(nint command, uint offset)
    {
        var copy = SDL.BeginGPUCopyPass(command);
        if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact summary");
        SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _contactSummary!.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _spatialDownload!.DangerousGetHandle(), Offset = offset });
        if (_oneWayShapeCount > 0) SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _oneWaySummary!.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _spatialDownload.DangerousGetHandle(), Offset = offset + 8 });
        SDL.EndGPUCopyPass(copy);
    }

    private (int Contacts, int OneWay) AcceptContactStatus(nint mapped, uint offset)
    {
        var values = (uint*)((byte*)mapped + offset);
        if (values[0] != 0) throw new InvalidOperationException("GPU resident contacts returned invalid geometry or contact state.");
        var count = values[1]; var oneWayCount = _oneWayShapeCount > 0 ? values[2] : 0;
        if (count > int.MaxValue || oneWayCount > int.MaxValue) throw new InvalidOperationException("GPU contact count exceeds addressable storage.");
        return ((int)count, (int)oneWayCount);
    }

    private void DisposeContacts() { _contactPipeline?.Dispose(); _contactsGPU?.Dispose(); _contactSummary?.Dispose(); DisposeOneWay(); }
}
