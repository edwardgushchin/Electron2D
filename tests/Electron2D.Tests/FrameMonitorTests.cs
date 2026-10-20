using System.Diagnostics;
using Electron2D;
using Monitor = Electron2D.Performance.Monitor;

internal static class FrameMonitorTests
{
    internal static void Run()
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_HOST_DELTA") == "1") { HostDeltaSmoothingTests.RunManaged(); return; }
        FrameMonitorContract.Run();
        var service = Performance.Service;
        service.ResetFrameTiming();
        service.ObserveFrame(.4, 100, 200, 300);
        service.ObserveFrame(.59, 90, 210, 290);
        Check(Performance.GetMonitor(Monitor.TimeProcess) == 0, "Window boundary.");
        service.ObserveFrame(.01, 95, 190, 310);
        Check(FrameMonitorContract.Values() is var values && values.Process == 100d / Stopwatch.Frequency &&
            values.Physics == 210d / Stopwatch.Frequency && values.Navigation == 310d / Stopwatch.Frequency,
            "Per-lane window peaks, not averages or accumulated catch-up costs.");
        service.ObserveFrame(1.25, 4, 2, 3);
        Check(Performance.GetMonitor(Monitor.TimeProcess) == 4d / Stopwatch.Frequency, "Completed window replaces previous peak.");
        service.ObserveFrame(.74, 1, 0, 0);
        Check(Performance.GetMonitor(Monitor.TimeProcess) == 4d / Stopwatch.Frequency, "Remainder retains partial window.");
        service.ObserveFrame(.01, 2, 0, 0);
        Check(Performance.GetMonitor(Monitor.TimeProcess) == 2d / Stopwatch.Frequency &&
            Performance.GetMonitor(Monitor.TimePhysicsProcess) == 0 &&
            Performance.GetMonitor(Monitor.TimeNavigationProcess) == 0, "Empty physics lanes publish zero.");
        service.ResetFrameTiming();
        Console.WriteLine("Exact frame-monitor publication windows and independent lane maxima passed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
