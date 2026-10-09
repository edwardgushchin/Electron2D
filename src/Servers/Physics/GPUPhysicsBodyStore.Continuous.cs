using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private int _ccdBodyCount;
    private RenderHandle? _ccdPipeline;
    private const float CCDTolerance = 0.001f;
    [StructLayout(LayoutKind.Sequential)]
    private struct CCDSettings
    {
        internal uint Pairs, Shapes, Bodies, Corrections;
        internal Float4 Tolerances;
        internal uint HistoryCount, HistoryCapacity;
        internal float EpisodeMargin;
        internal uint Padding;
    }
    internal long CCDQueryCount { get; private set; }
    internal long CCDIntervalCount { get; private set; }
    internal double CCDWaitMS { get; private set; }
    internal CCDMode GetCCDMode(BodyHandle body) { Validate(body); return _slots[body.Index].CCDMode; }
    internal void SetCCDMode(BodyHandle body, CCDMode mode)
    {
        Validate(body);
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        ref var slot = ref _slots[body.Index];
        if (slot.CCDMode == mode) return;
        if (slot.Mode >= PhysicsServer.BodyMode.Rigid) _ccdBodyCount += (mode != CCDMode.Disabled ? 1 : 0) - (slot.CCDMode != CCDMode.Disabled ? 1 : 0);
        slot.CCDMode = mode;
        ref var command = ref Edit(body.Index); command.Mask |= 2048;
        command.Padding = (command.Padding & ~768u) | ((uint)mode << 8);
        Wake(body.Index, true);
    }
    private void AdvanceContinuous(float delta, Vector2 gravity, int iterations, float margin, float penetration, float correction, float speed, float bounce, bool endTick)
    {
        var remaining = delta;
        for (var interval = 0; interval < 128; interval++)
        {
            var fraction = FindCCDFraction(remaining);
            if (fraction == 1)
            {
                Submit(remaining, default, default, default, 4, _hasPositionCorrections, delta, endTick: endTick);
                _hasPositionCorrections = false; return;
            }
            var travel = remaining * fraction;
            if (travel > 0) Submit(travel, default, default, default, 4, _hasPositionCorrections, 0);
            remaining -= travel; CCDIntervalCount++;
            SolveConstraintsCore(MathF.Max(remaining, delta * 0.000001f), iterations, MathF.Max(margin, 4 * CCDTolerance),
                penetration, correction, speed, bounce, gravity, false, 4 * CCDTolerance);
            if (remaining == 0)
            {
                Submit(0, default, default, default, 4, false, delta, endTick: endTick); _hasPositionCorrections = false; return;
            }
        }
        throw new InvalidOperationException("GPU continuous collision did not converge within its interval budget.");
    }
    private float FindCCDFraction(float delta)
    {
        var pairs = FindPairs(CCDTolerance, delta, _hasPositionCorrections);
        if (pairs == 0) return 1;
        EnsureOneWay();
        _ccdPipeline ??= _context.CreatePipeline("PhysicsResidentCCD.comp.spv");
        var wait = WaitMS;
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire resident continuous collision");
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _spatialUpload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map continuous collision reset");
            ((uint*)mapped)[0] = 0; ((float*)mapped)[1] = 1;
            SDL.UnmapGPUTransferBuffer(Device, _spatialUpload.DangerousGetHandle());
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin continuous collision reset");
            UploadSpatial(copy, _spatialSummary!, 0, 8); SDL.EndGPUCopyPass(copy);
            var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[2];
            outputs[0] = new() { Buffer = _spatialSummary!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _centers!.DangerousGetHandle() };
            var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 2);
            if (compute == 0) throw GPUPhysicsDevice.Failure("begin continuous collision sweep");
            SDL.BindGPUComputePipeline(compute, _ccdPipeline.DangerousGetHandle());
            var inputs = stackalloc nint[8] { _bodies!.DangerousGetHandle(), _verticesGPU!.DangerousGetHandle(), _geometryGPU!.DangerousGetHandle(),
                _shapesGPU!.DangerousGetHandle(), _pairsGPU!.DangerousGetHandle(), _positionCorrectionsGPU!.DangerousGetHandle(), _oneWayHistoryGPU!.DangerousGetHandle(), _oneWayHistoryTableGPU!.DangerousGetHandle() };
            SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 8);
            var settings = new CCDSettings
            {
                Pairs = (uint)pairs,
                Shapes = (uint)_shapeHighWater,
                Bodies = (uint)_highWater,
                Corrections = _hasPositionCorrections ? 1u : 0u,
                HistoryCount = (uint)_oneWayHistoryCount,
                HistoryCapacity = (uint)OneWayHistoryTableSize,
                EpisodeMargin = _contactMargin,
                Tolerances = new(CCDTolerance, 0.5f, 0.00001f, delta)
            };
            SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(CCDSettings));
            SDL.DispatchGPUCompute(compute, ((uint)pairs + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin continuous collision summary");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _spatialSummary.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _spatialDownload!.DangerousGetHandle() });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            UploadBytes += 8; ReadbackBytes += 8; UniformBytes += sizeof(CCDSettings); CCDQueryCount++; CCDWaitMS += WaitMS - wait;
            mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map continuous collision summary");
            float result;
            try
            {
                result = ((float*)mapped)[1];
                if (*(uint*)mapped != 0 || !float.IsFinite(result) || result < 0 || result > 1)
                    throw new InvalidOperationException($"GPU continuous collision returned an invalid or unconverged sweep (status {*(uint*)mapped}, fraction {result}).");
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
            _failed = false; return result;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }
}
