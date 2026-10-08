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
    }

    private RenderHandle? _contactPipeline, _contactsGPU;
    private int _contactCapacity;
    private long _contactPairVersion = -1;
    internal int ContactPointCount { get; private set; }
    internal long ContactCapacityRetries { get; private set; }
    internal long ContactSubmissionCount { get; private set; }

    internal int FindContacts(float margin = 0)
    {
        var pairs = FindPairs(margin);
        if (_contactPairVersion == BroadPhaseSubmissionCount) return ContactPointCount;
        if (pairs == 0) { ContactPointCount = 0; _contactPairVersion = BroadPhaseSubmissionCount; return 0; }
        _contactPipeline ??= _context.CreatePipeline("PhysicsResidentContacts.comp.spv");
        Grow(ref _contactsGPU, ref _contactCapacity, 1, sizeof(ContactPoint), false);
        var retry = false;
        while (true)
        {
            var count = DispatchContacts(margin);
            if (count > _contactCapacity)
            {
                if (retry) { _failed = true; throw new InvalidOperationException("The immutable GPU contact batch changed during capacity recovery."); }
                Grow(ref _contactsGPU, ref _contactCapacity, count, sizeof(ContactPoint), false);
                ContactCapacityRetries++; retry = true; continue;
            }
            ContactPointCount = count; _contactPairVersion = BroadPhaseSubmissionCount; return count;
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

    private int DispatchContacts(float margin)
    {
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire resident contacts");
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _spatialUpload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map contact status reset");
            *(ulong*)mapped = 0; SDL.UnmapGPUTransferBuffer(Device, _spatialUpload.DangerousGetHandle());
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact status reset");
            UploadSpatial(copy, _spatialSummary!, 0, 8); SDL.EndGPUCopyPass(copy);
            var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[2];
            outputs[0] = new() { Buffer = _contactsGPU!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _spatialSummary!.DangerousGetHandle() };
            var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 2);
            if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident contact compute");
            SDL.BindGPUComputePipeline(compute, _contactPipeline!.DangerousGetHandle());
            var inputs = stackalloc nint[5] { _bodies!.DangerousGetHandle(), _verticesGPU!.DangerousGetHandle(), _geometryGPU!.DangerousGetHandle(), _shapesGPU!.DangerousGetHandle(), _pairsGPU!.DangerousGetHandle() };
            SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 5);
            var settings = new ContactSettings { Pairs = (uint)PairCount, Capacity = (uint)_contactCapacity, Shapes = (uint)_shapeHighWater, Bodies = (uint)_highWater, Tolerances = new(margin, 0.5f, 0.00001f, 0) };
            SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(ContactSettings));
            SDL.DispatchGPUCompute(compute, ((uint)PairCount + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact summary");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _spatialSummary!.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _spatialDownload!.DangerousGetHandle() });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            UploadBytes += 8; ReadbackBytes += 8; UniformBytes += sizeof(ContactSettings); ContactSubmissionCount++;
            mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map contact summary");
            uint count;
            try
            {
                if (*(uint*)mapped != 0) throw new InvalidOperationException("GPU resident contacts returned invalid geometry or contact state.");
                count = ((uint*)mapped)[1];
                if (count > int.MaxValue) throw new InvalidOperationException("GPU contact count exceeds addressable storage.");
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
            _failed = false; return (int)count;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }

    private void DisposeContacts() { _contactPipeline?.Dispose(); _contactsGPU?.Dispose(); }
}
