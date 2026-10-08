using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SolverUniforms
    {
        internal uint Stage, Count, Bodies, Points;
        internal Float4 Time, Policy;
        internal uint HistoryCapacity, PreviousPoints, Padding1, Padding2;
    }
    private RenderHandle? _solverPipeline, _constraintsGPU, _contactHeadsGPU, _solverHistoryGPU, _solverHistoryTableGPU, _positionCorrectionsGPU;
    private int _constraintCapacity, _contactHeadCapacity, _solverHistoryCapacity, _solverHistoryTableCapacity, _previousPointCount, _positionCorrectionCapacity;
    private bool _hasPositionCorrections;
    private float _previousSolveDelta;
    internal int WarmStartedPointCount { get; private set; }
    internal long SolverSubmissionCount { get; private set; }
    internal double SolverMS { get; private set; }
    internal double SolverWaitMS { get; private set; }

    /// <summary>Advances resident bodies with split force/contact/pose stages; no CPU world or contact publication.</summary>
    internal void Simulate(float delta, Vector2 gravity, int substeps = 4, int iterations = 16,
        float margin = 2, float allowedPenetration = 0.5f, float correctionFactor = 0.2f,
        float maxCorrectionSpeed = 200, float bounceThreshold = 100)
    {
        EnsureAccess();
        if (!float.IsFinite(delta) || delta < 0 || !gravity.IsFinite() || substeps < 1) throw new ArgumentOutOfRangeException(nameof(delta));
        var h = delta / substeps;
        ValidateSolver(delta == 0 ? 1 : h, iterations, margin, allowedPenetration, correctionFactor, maxCorrectionSpeed, bounceThreshold);
        if (_highWater == 0) return;
        if (delta == 0) { Step(0, default); return; }
        try
        {
            for (var substep = 0; substep < substeps; substep++)
            {
                Submit(h, gravity, default, default, 3);
                SolveContacts(h, iterations, margin, allowedPenetration, correctionFactor, maxCorrectionSpeed, bounceThreshold);
                Submit(h, default, default, default, 4, _hasPositionCorrections);
                _hasPositionCorrections = false;
            }
        }
        catch { _failed = true; throw; }
    }

    /// <summary>Solves current contact velocities on the device without advancing poses.</summary>
    internal void SolveContacts(float delta, int iterations = 16, float margin = 2,
        float allowedPenetration = 0.5f, float correctionFactor = 0.2f, float maxCorrectionSpeed = 200, float bounceThreshold = 100)
    {
        EnsureAccess();
        ValidateSolver(delta, iterations, margin, allowedPenetration, correctionFactor, maxCorrectionSpeed, bounceThreshold);
        try
        {
            _hasPositionCorrections = false;
            if (FindContacts(margin) == 0) { _previousPointCount = 0; _previousSolveDelta = 0; WarmStartedPointCount = 0; return; }
            _solverPipeline ??= _context.CreatePipeline("PhysicsResidentSolve.comp.spv");
            Grow(ref _constraintsGPU, ref _constraintCapacity, ContactPointCount, 112, false);
            Grow(ref _positionCorrectionsGPU, ref _positionCorrectionCapacity, _highWater, 16, false);
            Grow(ref _contactHeadsGPU, ref _contactHeadCapacity, _highWater, 8, false);
            Grow(ref _solverHistoryGPU, ref _solverHistoryCapacity, Math.Max(ContactPointCount, _previousPointCount), 80, true);
            var previousTableCapacity = _solverHistoryTableCapacity;
            Grow(ref _solverHistoryTableGPU, ref _solverHistoryTableCapacity, checked(2 * Math.Max(ContactPointCount, _previousPointCount)), 4, false);
            DispatchSolver(new()
            {
                Bodies = (uint)_highWater,
                Points = (uint)ContactPointCount,
                Time = new(delta, 1 / delta, correctionFactor, maxCorrectionSpeed),
                Policy = new(allowedPenetration, bounceThreshold, _previousSolveDelta > 0 ? delta / _previousSolveDelta : 0, 0),
                HistoryCapacity = (uint)_solverHistoryTableCapacity,
                PreviousPoints = (uint)_previousPointCount
            }, iterations, previousTableCapacity != _solverHistoryTableCapacity);
            _previousPointCount = ContactPointCount; _previousSolveDelta = delta; _hasPositionCorrections = true;
            _bodyVersion++;
        }
        catch { _failed = true; throw; }
    }

    private static void ValidateSolver(float delta, int iterations, float margin, float penetration, float correction, float speed, float bounce)
    {
        if (!float.IsFinite(delta) || delta <= 0 || !float.IsFinite(1 / delta) || iterations < 1 ||
            !float.IsFinite(margin) || margin < 0 || !float.IsFinite(penetration) || penetration < 0 ||
            !float.IsFinite(correction) || correction < 0 || correction > 1 || !float.IsFinite(speed) || speed < 0 ||
            !float.IsFinite(bounce) || bounce < 0) throw new ArgumentOutOfRangeException(nameof(delta));
    }

    private void DispatchSolver(SolverUniforms settings, int iterations, bool rebuildHistory)
    {
        var start = Stopwatch.GetTimestamp(); var wait = WaitMS;
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire resident solver");
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _spatialUpload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map solver reset");
            *(ulong*)mapped = 0; SDL.UnmapGPUTransferBuffer(Device, _spatialUpload.DangerousGetHandle());
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin solver reset");
            UploadSpatial(copy, _spatialSummary!, 0, 8); SDL.EndGPUCopyPass(copy);
            var passes = 0;
            if (rebuildHistory && _previousPointCount > 0)
            {
                SolverPass(command, settings, 5, _solverHistoryTableCapacity);
                SolverPass(command, settings, 6, _previousPointCount); passes += 2;
            }
            SolverPass(command, settings, 0, _highWater);
            SolverPass(command, settings, 1, ContactPointCount); passes += 2;
            if (_previousPointCount > 0) { SolverPass(command, settings, 3, _highWater); passes++; }
            for (var iteration = 0; iteration < iterations; iteration++)
            {
                SolverPass(command, settings, 2, ContactPointCount);
                SolverPass(command, settings, 3, _highWater);
            }
            SolverPass(command, settings, 4, ContactPointCount);
            SolverPass(command, settings, 5, _solverHistoryTableCapacity);
            SolverPass(command, settings, 6, ContactPointCount); passes += 3;
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin solver result");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _spatialSummary!.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _spatialDownload!.DangerousGetHandle() });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            UploadBytes += 8; ReadbackBytes += 8; UniformBytes += sizeof(SolverUniforms) * (passes + 2L * iterations); SolverSubmissionCount++;
            mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map solver result");
            try
            {
                if (*(uint*)mapped != 0) throw new InvalidOperationException("GPU resident solver returned invalid constraints or velocity.");
                WarmStartedPointCount = checked((int)((uint*)mapped)[1]);
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
            _failed = false;
            SolverMS += Stopwatch.GetElapsedTime(start).TotalMilliseconds; SolverWaitMS += WaitMS - wait;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }

    private void SolverPass(nint command, SolverUniforms settings, uint stage, int count)
    {
        var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[7];
        outputs[0] = new() { Buffer = _bodies!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _constraintsGPU!.DangerousGetHandle() };
        outputs[2] = new() { Buffer = _contactHeadsGPU!.DangerousGetHandle() }; outputs[3] = new() { Buffer = _spatialSummary!.DangerousGetHandle() };
        outputs[4] = new() { Buffer = _solverHistoryGPU!.DangerousGetHandle() }; outputs[5] = new() { Buffer = _solverHistoryTableGPU!.DangerousGetHandle() };
        outputs[6] = new() { Buffer = _positionCorrectionsGPU!.DangerousGetHandle() };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 7);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident solver compute");
        SDL.BindGPUComputePipeline(compute, _solverPipeline!.DangerousGetHandle());
        var inputs = stackalloc nint[3] { _contactsGPU!.DangerousGetHandle(), _shapesGPU!.DangerousGetHandle(), _geometryGPU!.DangerousGetHandle() };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 3);
        settings.Stage = stage; settings.Count = (uint)count;
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(SolverUniforms));
        SDL.DispatchGPUCompute(compute, ((uint)count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
    }

    private void DisposeSolver() { _solverPipeline?.Dispose(); _constraintsGPU?.Dispose(); _contactHeadsGPU?.Dispose(); _solverHistoryGPU?.Dispose(); _solverHistoryTableGPU?.Dispose(); _positionCorrectionsGPU?.Dispose(); }
}
