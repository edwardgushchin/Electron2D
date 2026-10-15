using System.Diagnostics;
using System.Text.Json;

namespace Electron2D.Examples.PhysicsNetwork;

internal static class MemoryGate
{
    internal static string Role = "";
    internal static PhysicsCheckpoint[] Points = [];
    private static readonly byte[] WeakTarget = new byte[131072];
    private static readonly Calibration Calibration = new() { Weak = new(WeakTarget) };
    internal static void Pause(string phase)
    {
        var folder = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_PHYSICS_MEMORY");
        if (string.IsNullOrEmpty(folder)) return;
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        var name = System.IO.Path.Combine(folder, $"{Environment.ProcessId}-{phase}");
        File.WriteAllText(name + ".tmp", JsonSerializer.Serialize(new { PID = Environment.ProcessId, Role, Phase = phase, ProcessMemory = ProcessMemory.Capture() }));
        File.Move(name + ".tmp", name + ".ready.json");
        var started = Stopwatch.GetTimestamp();
        while (!File.Exists(name + ".go"))
        {
            if (Stopwatch.GetElapsedTime(started).TotalSeconds > 30) throw new TimeoutException("The memory probe did not release its own child.");
            Thread.Sleep(10);
        }
        GC.KeepAlive(Calibration);
    }
}

internal sealed partial class NetworkSession
{
    partial void FillBackendReport(SessionReport report)
    {
        MemoryGate.Pause("completed");
        MemoryGate.Points = new PhysicsCheckpoint[4];
        for (var i = 0; i < MemoryGate.Points.Length; i++) MemoryGate.Points[i] = PhysicsServer.SpaceCreateCheckpoint(Simulation.World.Space);
        MemoryGate.Points[0].Restore();
        MemoryGate.Pause("checkpointed");
        Dispose();
        MemoryGate.Pause("disposed-held");
        MemoryGate.Points = [];
    }
}
