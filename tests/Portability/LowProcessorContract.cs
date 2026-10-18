using System.Diagnostics;
using Electron2D;

internal static class LowProcessorContract
{
    internal static void Run()
    {
        Check(!OS.LowProcessorUsageMode && OS.LowProcessorUsageModeSleepUsec == 6900, "Initial process policy.");
        Check(!ProjectSettings.Get(ProjectSettings.LowProcessorMode) && ProjectSettings.Get(ProjectSettings.LowProcessorModeSleepUsec) == 6900, "Project defaults.");
        Reject<InvalidOperationException>(() => RenderingServer.HasChanged());
        Reject<ArgumentOutOfRangeException>(() => OS.LowProcessorUsageModeSleepUsec = -1);
        var mode = OS.LowProcessorUsageMode; var sleep = OS.LowProcessorUsageModeSleepUsec;
        try
        {
            Task.Run(() => { OS.LowProcessorUsageMode = true; OS.LowProcessorUsageModeSleepUsec = int.MaxValue; }).GetAwaiter().GetResult();
            Check(OS.LowProcessorUsageMode && OS.LowProcessorUsageModeSleepUsec == int.MaxValue, "Atomic off-owner policy access.");
            var properties = Engine.GetSingleton<OS>("OS").GetPropertyList();
            Check(properties.Any(p => p.Name == nameof(OS.LowProcessorUsageMode)) && properties.Any(p => p.Name == nameof(OS.LowProcessorUsageModeSleepUsec)), "Retained service descriptors.");
            using var root = new Node(); using var tree = new SceneTree(root);
            Engine.Start(tree); var watch = Stopwatch.StartNew();
            try { for (var i = 0; i < 64; i++) Engine.AdvanceFrame(1d / 60); }
            finally { Engine.Stop(); }
            Check(watch.Elapsed.TotalSeconds < 5, "Manual simulation does not apply the enormous owned-loop wait.");
        }
        finally { OS.LowProcessorUsageMode = mode; OS.LowProcessorUsageModeSleepUsec = sleep; }
        Console.WriteLine("Low-processor settings, descriptors, unavailable renderer and manual scheduler passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
