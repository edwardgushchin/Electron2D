using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

// Backend instrumentation belongs to the tests; the actual example consumes only public engine APIs.
internal sealed partial class NetworkSession
{
    partial void FillBackendReport(SessionReport report)
    {
        report.BackendInstrumented = true;
        var gpu = PhysicsServer.Service.GetSceneSpace(Simulation.World.Space).GPUStore;
        if (gpu is null) return;
        report.GPUUploadBytes = gpu.UploadBytes; report.GPUReadbackBytes = gpu.ReadbackBytes;
        report.GPUSubmissions = gpu.SubmissionCount; report.GPUWaitMS = gpu.WaitMS;
    }
}
