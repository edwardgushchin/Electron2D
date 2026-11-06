using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private const uint SolverCompletionOffset = 32;
    private nint _solverContinuation;
    private bool _solverCompletionPending;
    /// <summary>Counts completed command batches sharing solver and following spatial work.</summary>
    internal long SharedSolverCompletionCount { get; private set; }

    private nint AcquireSpatialCommands()
    {
        var command = _solverContinuation;
        _solverContinuation = 0;
        return command != 0 ? command : SDL.AcquireGPUCommandBuffer(Device);
    }

    private void AcceptSolverContinuation()
    {
        if (!_solverCompletionPending) return;
        UploadBytes += 16; ReadbackBytes += 16; SolverSubmissionCount++;
        var mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload!.DangerousGetHandle(), false);
        if (mapped == 0) throw GPUPhysicsDevice.Failure("map shared solver completion");
        try
        {
            var state = (uint*)((byte*)mapped + SolverCompletionOffset);
            if (state[0] != 0) throw new InvalidOperationException("GPU resident solver returned invalid constraints or velocity.");
            WarmStartedPointCount = checked((int)state[1]);
            if (state[2] != 0) throw new InvalidOperationException("GPU resident body work returned invalid state.");
            ActiveSimulationBodyCount = checked((int)state[3]);
        }
        finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
        AcceptContactReports();
        _solverCompletionPending = false;
    }

    private void FlushSolverContinuation()
    {
        if (_solverContinuation == 0) return;
        var command = AcquireSpatialCommands();
        try { _failed = true; Finish(ref command); _failed = false; }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }

    private void CancelSolverContinuation()
    {
        var command = _solverContinuation; _solverContinuation = 0;
        if (command != 0) SDL.CancelGPUCommandBuffer(command);
        _solverCompletionPending = false;
    }
}
