using System.Reflection;
using Electron2D;

internal static class RenderMemoryLifetimeTests
{
    internal static void Run()
    {
        RenderMemoryContract.RunLocal();
        using var device = RenderingServer.CreateLocalRenderingDevice();
        for (var i = 0; i < 128; i++) _ = device.GetMemoryUsage(RenderingDevice.MemoryType.Total);
        var gate = typeof(GPUDeviceFactory).GetField("Gate", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        using var held = new ManualResetEventSlim();
        var complete = 0; var timedOut = false;
        var controller = new Thread(() =>
        {
            lock (gate)
            {
                held.Set();
                var start = System.Diagnostics.Stopwatch.GetTimestamp();
                while (Volatile.Read(ref complete) == 0)
                {
                    if (System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalSeconds > 5) { timedOut = true; break; }
                    Thread.Yield();
                }
            }
        });
        controller.Start(); held.Wait();
        try
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var memory = device.GetMemoryUsage(RenderingDevice.MemoryType.Total);
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            if (memory <= 0 || bytes != 0) throw new InvalidOperationException($"Memory read while the creation gate is held: {memory} bytes, {bytes} managed allocation.");
        }
        finally { Volatile.Write(ref complete, 1); controller.Join(); }
        if (timedOut) throw new InvalidOperationException("A memory diagnostic waited for the unrelated device creation gate.");
        Console.WriteLine("Native memory diagnostics bypass the device creation gate with zero owner allocation.");
    }
}
