using System.Diagnostics;
using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed partial class NetworkSession
{
    private CostSampler? _costs;
    partial void SessionReadyForTesting()
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_NETWORK_COST_ROLE") is not ("server" or "cpu" or "gpu" or "late")) return;
        var gpu = PhysicsServer.Service.GetSceneSpace(Simulation.World.Space).GPUStore;
        _costs = new(() => new(gpu?.UploadBytes ?? 0, gpu?.ReadbackBytes ?? 0, gpu?.DeviceCopyBytes ?? 0,
            gpu?.SubmissionCount ?? 0, gpu?.UniformBytes ?? 0, gpu?.WaitMS ?? 0, Stopwatch.GetTimestamp()));
        Simulation.Costs = _costs;
    }
    partial void ProfileForTesting(int stage, bool begin) => _costs?.Mark(stage < 4 ? (int)Simulation.Phase * 4 + stage : stage + 8, begin);
    partial void FillCostReportForTesting()
    {
        if (_costs is null) return;
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_NETWORK_COST_DIR") ?? throw new InvalidOperationException("Missing cost output directory");
        Directory.CreateDirectory(directory);
        _costs.Write(System.IO.Path.Combine(directory, Environment.GetEnvironmentVariable("ELECTRON2D_NETWORK_COST_ROLE") + "-cost.json"));
    }
}
internal sealed partial class NetworkWorld
{
    internal CostSampler? Costs;
    partial void ProfileForTesting(int stage, bool begin) => Costs?.Mark((int)Phase * 4 + stage, begin);
}
internal static partial class ProcessCheck
{
    static partial void ConfigureChildForTesting(System.Diagnostics.ProcessStartInfo info, string role) => info.Environment["ELECTRON2D_NETWORK_COST_ROLE"] = role;
}
