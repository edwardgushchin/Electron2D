using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private RenderHandle? _colorPipeline, _coloredSolvePipeline, _contactColors, _colorOwners, _colorStatus, _colorDownload, _colorBuckets, _colorOrder;
    private int _colorCapacity, _colorOwnerCapacity, _colorOrderCapacity;
    private readonly int[] _colorCounts = new int[32], _colorStarts = new int[32];
    private RenderHandle? _packSolverPipeline, _packedConstraints, _packedImpulses, _solverBodies;
    private int _packedConstraintCapacity, _packedImpulseCapacity, _solverBodyCapacity;
    // Diagnostic control: compare storage layouts with the same constraints and solver sweeps.
    internal bool PackColoredContacts = true;
    internal bool PackSolverBodyState = true;
    internal long PackedSolverStorageBytes => (long)_packedConstraintCapacity * 64 + (long)_packedImpulseCapacity * 32 + (long)_solverBodyCapacity * 48;
    internal int ContactColorCount { get; private set; }
    internal int ContactColorRounds { get; private set; }
    internal long ContactColorFallbacks { get; private set; }
    private int BuildContactColors(ref nint command)
    {
        _colorPipeline ??= _context.CreatePipeline("PhysicsResidentColors.comp.spv");
        _coloredSolvePipeline ??= _context.CreatePipeline("PhysicsResidentColoredSolve.comp.spv");
        Grow(ref _contactColors, ref _colorCapacity, ContactPointCount, 4, false);
        Grow(ref _colorOwners, ref _colorOwnerCapacity, _highWater, 8, false);
        Grow(ref _colorOrder, ref _colorOrderCapacity, ContactPointCount, 4, false);
        _colorBuckets ??= Buffer(384); _colorStatus ??= Buffer(16); _colorDownload ??= Transfer(144, SDL.GPUTransferBufferUsage.Download);
        var mapped = SDL.MapGPUTransferBuffer(Device, _spatialUpload!.DangerousGetHandle(), false);
        if (mapped == 0) throw GPUPhysicsDevice.Failure("map contact coloring reset");
        ((ulong*)mapped)[0] = ((ulong*)mapped)[1] = 0; SDL.UnmapGPUTransferBuffer(Device, _spatialUpload.DangerousGetHandle());
        var copy = SDL.BeginGPUCopyPass(command);
        if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact coloring reset");
        UploadSpatial(copy, _colorStatus, 0, 16); SDL.EndGPUCopyPass(copy);
        ColorPass(command, 0, _highWater, 0); ColorPass(command, 1, ContactPointCount, 0); ColorPass(command, 7, 32, 0);
        // shortcut: 32 colors/64 rounds use complete Jacobi on overflow; enlarge the schedule if high-degree workloads need colored convergence.
        var limit = (uint)Math.Clamp(ContactColorRounds + 4, 8, 64); var round = 0u;
        while (true)
        {
            for (; round < limit; round++)
            {
                ColorPass(command, 2, _highWater, round);
                ColorPass(command, 3, ContactPointCount, round);
                ColorPass(command, 4, ContactPointCount, round);
            }
            ColorPass(command, 5, _highWater, 0); ColorPass(command, 6, ContactPointCount, 0);
            ColorPass(command, 8, 1, 0); ColorPass(command, 9, ContactPointCount, 0);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin contact coloring result");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _colorStatus.DangerousGetHandle(), Size = 16 }, new() { TransferBuffer = _colorDownload.DangerousGetHandle() });
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _colorBuckets.DangerousGetHandle(), Size = 128 }, new() { TransferBuffer = _colorDownload.DangerousGetHandle(), Offset = 16 });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command); ReadbackBytes += 144;
            mapped = SDL.MapGPUTransferBuffer(Device, _colorDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map contact coloring result");
            bool incomplete, fallback;
            try
            {
                var state = (uint*)mapped;
                if (state[1] > 32 || state[3] != 0) throw new InvalidOperationException("GPU contact coloring returned an invalid or conflicting schedule.");
                incomplete = state[0] != 0 && (state[2] & 1) == 0 && round < 64;
                fallback = state[0] != 0 || (state[2] & 1) != 0;
                ContactColorCount = state[0] == 0 && (state[2] & 1) == 0 ? (int)state[1] : 0;
                ContactColorRounds = (int)(state[2] >> 8);
                var offset = 0;
                for (var c = 0; c < ContactColorCount; c++)
                {
                    _colorStarts[c] = offset; _colorCounts[c] = checked((int)state[4 + c]); offset = checked(offset + _colorCounts[c]);
                }
                if (offset > ContactPointCount) throw new InvalidOperationException("GPU contact coloring exceeded its point count.");
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _colorDownload.DangerousGetHandle()); }
            command = SDL.AcquireGPUCommandBuffer(Device);
            if (command == 0) throw GPUPhysicsDevice.Failure("acquire colored contact solve");
            if (!incomplete) { if (fallback) ContactColorFallbacks++; break; }
            limit = Math.Min(64, limit + 16);
        }
        UploadBytes += 16;
        _failed = false; return ContactColorCount;
    }
    private void ColorPass(nint command, uint stage, int count, uint round)
    {
        var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[5]
        {
            new() { Buffer = _contactColors!.DangerousGetHandle() }, new() { Buffer = _colorOwners!.DangerousGetHandle() }, new() { Buffer = _colorStatus!.DangerousGetHandle() },
            new() { Buffer = _colorBuckets!.DangerousGetHandle() }, new() { Buffer = _colorOrder!.DangerousGetHandle() }
        };
        var inputs = stackalloc nint[2] { _bodies!.DangerousGetHandle(), _constraintsGPU!.DangerousGetHandle() };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 5);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin contact coloring");
        SDL.BindGPUComputePipeline(compute, _colorPipeline!.DangerousGetHandle()); SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 2);
        var settings = stackalloc uint[8] { stage, (uint)count, (uint)_highWater, (uint)ContactPointCount, round, 0, 0, 0 };
        SDL.PushGPUComputeUniformData(command, 0, (nint)settings, 32); SDL.DispatchGPUCompute(compute, ((uint)count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute); UniformBytes += 32;
    }
    private void ColoredSolve(nint command, SolverUniforms settings, int color)
    {
        if (_colorCounts[color] == 0) return;
        var inputs = stackalloc nint[2] { (PackColoredContacts ? _packedConstraints! : _constraintsGPU!).DangerousGetHandle(), _colorOrder!.DangerousGetHandle() };
        var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[5]
        {
            new() { Buffer = _bodies!.DangerousGetHandle() }, new() { Buffer = _positionCorrectionsGPU!.DangerousGetHandle() },
            new() { Buffer = (PackColoredContacts ? _packedImpulses! : _constraintImpulsesGPU!).DangerousGetHandle() }, new() { Buffer = _spatialSummary!.DangerousGetHandle() },
            new() { Buffer = _solverBodies!.DangerousGetHandle() }
        };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 5);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin colored contact solve");
        SDL.BindGPUComputePipeline(compute, _coloredSolvePipeline!.DangerousGetHandle()); SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 2);
        settings.Stage = (uint)color; settings.Count = (uint)_colorCounts[color]; settings.PreviousPoints = (uint)_colorStarts[color];
        if (PackColoredContacts) settings.Flags |= 4;
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(SolverUniforms));
        SDL.DispatchGPUCompute(compute, (settings.Count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute); UniformBytes += sizeof(SolverUniforms);
    }
    private void PackSolverState(nint command, uint stage)
    {
        _packSolverPipeline ??= _context.CreatePipeline("PhysicsResidentPackSolver.comp.spv");
        Grow(ref _packedConstraints, ref _packedConstraintCapacity, ContactPointCount, 64, false);
        Grow(ref _packedImpulses, ref _packedImpulseCapacity, ContactPointCount, 32, false);
        var inputs = stackalloc nint[2] { _constraintsGPU!.DangerousGetHandle(), _colorOrder!.DangerousGetHandle() };
        var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[6]
        {
            new() { Buffer = _constraintImpulsesGPU!.DangerousGetHandle() },
            new() { Buffer = _packedConstraints!.DangerousGetHandle() }, new() { Buffer = _packedImpulses!.DangerousGetHandle() },
            new() { Buffer = _bodies!.DangerousGetHandle() }, new() { Buffer = _positionCorrectionsGPU!.DangerousGetHandle() }, new() { Buffer = _solverBodies!.DangerousGetHandle() }
        };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 6);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin packed solver state");
        SDL.BindGPUComputePipeline(compute, _packSolverPipeline.DangerousGetHandle()); SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 2);
        var count = (uint)(stage >= 2 ? _highWater : _colorStarts[ContactColorCount - 1] + _colorCounts[ContactColorCount - 1]);
        var settings = stackalloc uint[4] { stage, count, 0, 0 };
        SDL.PushGPUComputeUniformData(command, 0, (nint)settings, 16); SDL.DispatchGPUCompute(compute, (count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute); UniformBytes += 16;
    }
    private void DisposeColors()
    {
        _packSolverPipeline?.Dispose(); _packedConstraints?.Dispose(); _packedImpulses?.Dispose(); _solverBodies?.Dispose();
        _colorPipeline?.Dispose(); _coloredSolvePipeline?.Dispose(); _contactColors?.Dispose(); _colorOwners?.Dispose(); _colorStatus?.Dispose(); _colorDownload?.Dispose(); _colorBuckets?.Dispose(); _colorOrder?.Dispose();
    }
}
