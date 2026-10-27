using System.Reflection;
using SDL3;
using Electron2D;

internal static class CanvasPipelineFaultTests
{
    private static int _failures;
    internal static void Run(string renderer)
    {
        if (renderer == "gpu")
        {
            var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
            try
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
                using var world = new World(PhysicsServer.Backend.CPU);
                using var window = new Window { Size = new(64, 48), World = world };
                Fail(() => Engine.Run(window));
                if (RenderingServer.IsAvailable || Performance.GetMonitor(Performance.Monitor.PipelineCompilationsCanvas) != 0)
                    throw new Exception("Failed startup exposed a renderer/pipeline count.");
            }
            finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
        }
        CanvasPipelineContract.Run(renderer, renderer == "gpu" ? Fail : null);
        Console.WriteLine($"Injected native pipeline creation failures: {_failures}.");
    }
    private static nint Failed(nint device, in SDL.GPUGraphicsPipelineCreateInfo info) { Interlocked.Increment(ref _failures); return 0; }
    private static void Fail(Action attempt)
    {
        var field = typeof(SDL).GetField("CreateGPUGraphicsPipelineNativeFunction", BindingFlags.NonPublic | BindingFlags.Static)!;
        var original = field.GetValue(null);
        var failed = typeof(CanvasPipelineFaultTests).GetMethod(nameof(Failed), BindingFlags.NonPublic | BindingFlags.Static)!;
        try
        {
            field.SetValue(null, failed.CreateDelegate(field.FieldType));
            var before = _failures;
            try { attempt(); }
            catch (InvalidOperationException) when (_failures > before) { return; }
            throw new Exception("Failed native pipeline creation was accepted.");
        }
        finally { field.SetValue(null, original); }
    }
}
