using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

// Backend instrumentation belongs to the tests; the actual example consumes only public engine APIs.
internal sealed partial class NetworkSession
{
    private int _numericalSteps;
    private bool _numericalThreadVerified = true;
    private bool _backendProbePrepared;
    partial void BeforeStepForTesting(bool warm, SimulationPhase phase)
    {
        if (_backendProbePrepared) return;
        _backendProbePrepared = true;
        var space = PhysicsServer.Service.GetSceneSpace(Simulation.World.Space);
        if (Simulation.World.PhysicsBackend != PhysicsServer.Backend.CPU) return;
        var owner = Environment.CurrentManagedThreadId;
        var separate = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DRunOnSeparateThread);
        // CCD owns pose finalization; observe the native collision interval without replacing its algorithms.
        Box2D.NET.B2Worlds.b2GetWorldFromId(space.WorldID).beginIslandChanges += _ =>
        {
            _numericalSteps++;
            _numericalThreadVerified &= (Environment.CurrentManagedThreadId != owner) == separate;
        };
    }
    partial void FillBackendReport(SessionReport report)
    {
        report.BackendInstrumented = true;
        report.NumericalSteps = _numericalSteps; report.NumericalThreadVerified = _numericalThreadVerified && _numericalSteps > 0;
        var gpu = PhysicsServer.Service.GetSceneSpace(Simulation.World.Space).GPUStore;
        if (gpu is null) return;
        report.GPUDriver = gpu.Driver; report.GPUDeviceName = gpu.DeviceName;
        report.GPUUploadBytes = gpu.UploadBytes; report.GPUReadbackBytes = gpu.ReadbackBytes;
        report.GPUSubmissions = gpu.SubmissionCount; report.GPUWaitMS = gpu.WaitMS;
    }
}
