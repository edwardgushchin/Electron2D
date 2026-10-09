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
        internal uint HistoryCapacity, PreviousPoints, ContactPoints, Flags;
    }
    private RenderHandle? _solverPipeline, _solverUpdatePipeline, _solverGatherPipeline, _constraintsGPU, _constraintImpulsesGPU, _contactHeadsGPU, _solverHistoryGPU, _solverHistoryTableGPU, _positionCorrectionsGPU;
    private int _constraintCapacity, _constraintImpulseCapacity, _contactHeadCapacity, _solverHistoryCapacity, _solverHistoryTableCapacity, _previousPointCount, _positionCorrectionCapacity;
    private bool _hasPositionCorrections;
    private float _previousSolveDelta;
    internal int WarmStartedPointCount { get; private set; }
    internal long SolverSubmissionCount { get; private set; }
    internal double SolverMS { get; private set; }
    internal double SolverWaitMS { get; private set; }
    // Diagnostic only: individual fences disturb batching and include submission overhead.
    internal bool ProfileSolverPasses;
    internal readonly double[] SolverPassMS = new double[7];

    /// <summary>Advances resident force/contact/pose stages and connected sleep, skipping device-confirmed unchanged inactive worlds.</summary>
    internal void Simulate(float delta, Vector2 gravity, int substeps = 4, int iterations = 16,
        float margin = 2, float allowedPenetration = 0.5f, float correctionFactor = 0.2f,
        float maxCorrectionSpeed = 200, float bounceThreshold = 100)
    {
        EnsureAccess();
        if (!float.IsFinite(delta) || delta < 0 || !gravity.IsFinite() || substeps < 1) throw new ArgumentOutOfRangeException(nameof(delta));
        var h = delta / substeps;
        ValidateSolver(delta == 0 ? 1 : h, iterations, margin, allowedPenetration, correctionFactor, maxCorrectionSpeed, bounceThreshold);
        RememberSleepSolver(iterations, margin, allowedPenetration, correctionFactor, maxCorrectionSpeed, bounceThreshold);
        PrepareMasses();
        if (_highWater == 0) return;
        if (delta == 0) { Step(0, default); return; }
        if (_sleepGravityKnown && _sleepGravity != gravity) _wakeAllSleep = true;
        _sleepGravity = gravity; _sleepGravityKnown = true;
        if (ActiveSimulationBodyCount == 0 && !_wakeAllSleep && _pendingCount == 0 &&
            _sleepBodyVersion == _bodyVersion && _sleepShapeVersion == _shapeVersion && _sleepGeometryEpoch == Shape.GeometryEpoch) return;
        try
        {
            for (var substep = 0; substep < substeps; substep++)
            {
                var dampingDelta = substep == 0 ? delta : 0;
                Submit(h, gravity, default, default, 3, dampingDelta: dampingDelta);
                SolveConstraintsCore(h, iterations, margin, allowedPenetration, correctionFactor, maxCorrectionSpeed, bounceThreshold, gravity, dampingDelta: dampingDelta);
                if (_ccdBodyCount > 0 && ShapeCount > 0) AdvanceContinuous(h, gravity, iterations, margin, allowedPenetration, correctionFactor, maxCorrectionSpeed, bounceThreshold);
                else Submit(h, default, default, default, 4, _hasPositionCorrections);
                _hasPositionCorrections = false;
            }
            _sleepBodyVersion = _bodyVersion; _sleepShapeVersion = _shapeVersion; _sleepGeometryEpoch = Shape.GeometryEpoch;
        }
        catch { _failed = true; throw; }
    }

    /// <summary>Solves current contacts, pins, grooves and axial springs on the device without advancing poses.</summary>
    internal void SolveConstraints(float delta, int iterations = 16, float margin = 2,
        float allowedPenetration = 0.5f, float correctionFactor = 0.2f, float maxCorrectionSpeed = 200, float bounceThreshold = 100) =>
        SolveConstraintsCore(delta, iterations, margin, allowedPenetration, correctionFactor, maxCorrectionSpeed, bounceThreshold, null);

    private void SolveConstraintsCore(float delta, int iterations, float margin, float allowedPenetration, float correctionFactor,
        float maxCorrectionSpeed, float bounceThreshold, Vector2? stepGravity, bool externalForces = true, float contactThreshold = 0, float dampingDelta = 0)
    {
        EnsureAccess();
        ValidateSolver(delta, iterations, margin, allowedPenetration, correctionFactor, maxCorrectionSpeed, bounceThreshold);
        if (externalForces) RememberSleepSolver(iterations, margin, allowedPenetration, correctionFactor, maxCorrectionSpeed, bounceThreshold);
        PrepareMasses();
        try
        {
            _hasPositionCorrections = false;
            Step(0, default); FlushJoints();
            if (FindContacts(margin) == 0)
            {
                _previousPointCount = 0; _previousSolveDelta = 0; WarmStartedPointCount = 0;
                if (_highWater == 0) return;
                EnsureSpatial(); Grow(ref _contactsGPU, ref _contactCapacity, 1, 64, false);
            }
            var constraintCount = checked(ContactPointCount + (JointCount > 0 ? JointRows * _jointHighWater : 0));
            EnsureSleep();
            _solverPipeline ??= _context.CreatePipeline("PhysicsResidentSolve.comp.spv");
            _solverUpdatePipeline ??= _context.CreatePipeline("PhysicsResidentUpdate.comp.spv");
            _solverGatherPipeline ??= _context.CreatePipeline("PhysicsResidentGather.comp.spv");
            Grow(ref _constraintsGPU, ref _constraintCapacity, Math.Max(1, constraintCount), 64, false);
            Grow(ref _constraintImpulsesGPU, ref _constraintImpulseCapacity, Math.Max(1, constraintCount), 32, false);
            Grow(ref _positionCorrectionsGPU, ref _positionCorrectionCapacity, _highWater, 16, false);
            Grow(ref _contactHeadsGPU, ref _contactHeadCapacity, _highWater, 8, false);
            Grow(ref _solverHistoryGPU, ref _solverHistoryCapacity, Math.Max(1, Math.Max(ContactPointCount, _previousPointCount)), 80, true);
            var previousTableCapacity = _solverHistoryTableCapacity;
            Grow(ref _solverHistoryTableGPU, ref _solverHistoryTableCapacity, checked(2 * Math.Max(1, Math.Max(ContactPointCount, _previousPointCount))), 4, false);
            DispatchSolver(new()
            {
                Bodies = (uint)_highWater,
                Points = (uint)constraintCount,
                Time = new(delta, 1 / delta, correctionFactor, maxCorrectionSpeed),
                Policy = new(allowedPenetration, bounceThreshold, _previousSolveDelta > 0 ? delta / _previousSolveDelta : 0, MathF.Max(contactThreshold, _ccdBodyCount > 0 ? 4 * CCDTolerance : 0)),
                HistoryCapacity = (uint)_solverHistoryTableCapacity,
                PreviousPoints = (uint)_previousPointCount,
                ContactPoints = (uint)ContactPointCount,
                Flags = externalForces ? 0u : 1u
            }, iterations, previousTableCapacity != _solverHistoryTableCapacity, stepGravity, dampingDelta);
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

    private void DispatchSolver(SolverUniforms settings, int iterations, bool rebuildHistory, Vector2? stepGravity, float dampingDelta)
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
            PrepareSleep(command, settings.Time.X, stepGravity, dampingDelta);
            if (rebuildHistory && _previousPointCount > 0)
            {
                SolverPass(ref command, settings, 5, _solverHistoryTableCapacity);
                SolverPass(ref command, settings, 6, _previousPointCount);
            }
            SolverPass(ref command, settings, 0, _highWater);
            if (_springJointCount > 0 && settings.Flags == 0)
            {
                JointPass(command, settings, 0);
                SolverPass(ref command, settings, 3, _highWater);
                SolverPass(ref command, settings, 0, _highWater);
            }
            SolverPass(ref command, settings, 1, ContactPointCount);
            if (JointCount > 0) JointPass(command, settings, 1);
            if (_limitedJointCount > 0) JointPass(command, settings, 3);
            if (_previousPointCount > 0 || JointCount > 0) { SolverPass(ref command, settings, 3, _highWater); }
            for (var iteration = 0; settings.Points > 0 && iteration < iterations; iteration++)
            {
                SolverPass(ref command, settings, 2, (int)settings.Points);
                if (_limitedJointCount > 0) JointPass(command, settings, 3);
                SolverPass(ref command, settings, 3, _highWater);
            }
            if (JointCount > 0) JointPass(command, settings, 2);
            SolverPass(ref command, settings, 4, ContactPointCount);
            SolverPass(ref command, settings, 5, _solverHistoryTableCapacity);
            SolverPass(ref command, settings, 6, ContactPointCount);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin solver result");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _spatialSummary!.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _spatialDownload!.DangerousGetHandle() });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            UploadBytes += 8; ReadbackBytes += 8; SolverSubmissionCount++;
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

    private void SolverPass(ref nint command, SolverUniforms settings, uint stage, int count)
    {
        if (count == 0) return;
        var start = ProfileSolverPasses ? Stopwatch.GetTimestamp() : 0;
        var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[8];
        var inputs = stackalloc nint[4];
        RenderHandle pipeline; uint inputCount, outputCount;
        if (stage == 2)
        {
            pipeline = _solverUpdatePipeline!; inputCount = 4; outputCount = 2;
            inputs[0] = _bodies!.DangerousGetHandle(); inputs[1] = _contactHeadsGPU!.DangerousGetHandle(); inputs[2] = _positionCorrectionsGPU!.DangerousGetHandle();
            inputs[3] = _constraintsGPU!.DangerousGetHandle();
            outputs[0] = new() { Buffer = _constraintImpulsesGPU!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _spatialSummary!.DangerousGetHandle() };
        }
        else if (stage == 3)
        {
            pipeline = _solverGatherPipeline!; inputCount = 3; outputCount = 3;
            inputs[0] = _constraintsGPU!.DangerousGetHandle(); inputs[1] = _contactHeadsGPU!.DangerousGetHandle(); inputs[2] = _constraintImpulsesGPU!.DangerousGetHandle();
            outputs[0] = new() { Buffer = _bodies!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _positionCorrectionsGPU!.DangerousGetHandle() }; outputs[2] = new() { Buffer = _spatialSummary!.DangerousGetHandle() };
        }
        else
        {
            pipeline = _solverPipeline!; inputCount = 4; outputCount = 8;
            outputs[0] = new() { Buffer = _bodies!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _constraintsGPU!.DangerousGetHandle() };
            outputs[2] = new() { Buffer = _contactHeadsGPU!.DangerousGetHandle() }; outputs[3] = new() { Buffer = _spatialSummary!.DangerousGetHandle() };
            outputs[4] = new() { Buffer = _solverHistoryGPU!.DangerousGetHandle() }; outputs[5] = new() { Buffer = _solverHistoryTableGPU!.DangerousGetHandle() };
            outputs[6] = new() { Buffer = _positionCorrectionsGPU!.DangerousGetHandle() };
            outputs[7] = new() { Buffer = _constraintImpulsesGPU!.DangerousGetHandle() };
            inputs[0] = _contactsGPU!.DangerousGetHandle(); inputs[1] = _shapesGPU!.DangerousGetHandle(); inputs[2] = _geometryGPU!.DangerousGetHandle(); inputs[3] = _centers!.DangerousGetHandle();
        }
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, outputCount);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident solver compute");
        SDL.BindGPUComputePipeline(compute, pipeline.DangerousGetHandle());
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, inputCount);
        settings.Stage = stage; settings.Count = (uint)count;
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(SolverUniforms));
        SDL.DispatchGPUCompute(compute, ((uint)count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
        UniformBytes += sizeof(SolverUniforms);
        if (ProfileSolverPasses)
        {
            Finish(ref command);
            SolverPassMS[stage] += Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            command = SDL.AcquireGPUCommandBuffer(Device);
            if (command == 0) throw GPUPhysicsDevice.Failure("acquire diagnostic solver pass");
        }
    }

    private void JointPass(nint command, SolverUniforms settings, uint stage)
    {
        _jointPreparePipeline ??= _context.CreatePipeline("PhysicsResidentJoints.comp.spv");
        var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[5];
        outputs[0] = new() { Buffer = _jointStatesGPU!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _constraintsGPU!.DangerousGetHandle() };
        outputs[2] = new() { Buffer = _constraintImpulsesGPU!.DangerousGetHandle() }; outputs[3] = new() { Buffer = _contactHeadsGPU!.DangerousGetHandle() };
        outputs[4] = new() { Buffer = _spatialSummary!.DangerousGetHandle() };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 5);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident joint constraints");
        SDL.BindGPUComputePipeline(compute, _jointPreparePipeline.DangerousGetHandle());
        var inputs = stackalloc nint[3] { _bodies!.DangerousGetHandle(), _jointsGPU!.DangerousGetHandle(), _centers!.DangerousGetHandle() };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 3);
        settings.Stage = stage; settings.Count = (uint)_jointHighWater;
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(SolverUniforms));
        SDL.DispatchGPUCompute(compute, (settings.Count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
        UniformBytes += sizeof(SolverUniforms);
    }

    private void DisposeSolver() { _solverPipeline?.Dispose(); _solverUpdatePipeline?.Dispose(); _solverGatherPipeline?.Dispose(); _constraintsGPU?.Dispose(); _constraintImpulsesGPU?.Dispose(); _contactHeadsGPU?.Dispose(); _solverHistoryGPU?.Dispose(); _solverHistoryTableGPU?.Dispose(); _positionCorrectionsGPU?.Dispose(); }
}
