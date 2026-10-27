using System.Runtime.CompilerServices;
using Electron2D;
using SDL3;
using static PhysicsDebugTests;

internal static class GPUDeviceLifetimeTests
{
    internal static void Run()
    {
        var rendering = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        try
        {
            using var context = new GPUPhysicsDevice();
            using var local = RenderingServer.CreateLocalRenderingDevice();
            var buffer = local.StorageBufferCreate(4, new byte[] { 1, 2, 3, 4 });
            for (var cycle = 0; cycle < 3; cycle++)
            {
                using var window = new Window { Size = new(320, 240) };
                uint id = 0;
                var frames = 0;
                window.Ready += _ =>
                {
                    var native = SDL.GetWindows(out var count);
                    Check(count == 1 && native is { Length: 1 }, "One actual SDL window is active.");
                    id = SDL.GetWindowID(native![0]);
                    var global = RenderingServer.GetRenderingDevice()!;
                    Reject<ArgumentException>(() => global.BufferGetData(buffer));
                    RenderingServer.FramePostDraw += AfterFrame;
                };
                Check(Engine.Run(window) == 0 && frames == 2, "Two native frames finish.");
                Check(id != 0 && SDL.GetWindowFromID(id) == 0, "Compute retention must not keep a closed SDL window alive.");
                Check(local.BufferGetData(buffer).SequenceEqual(new byte[] { 1, 2, 3, 4 }), "Local resource survives renderer recreation.");

                void AfterFrame()
                {
                    if (++frames != 2) return;
                    RenderingServer.FramePostDraw -= AfterFrame;
                    window.Tree!.Quit();
                }
            }
            var abandoned = AbandonLease();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            Check(!abandoned.IsAlive, "An abandoned SafeHandle lease is collectible while other consumers remain active.");
            using var next = GPUDeviceFactory.Acquire(false).Handle;
            Check(next.DangerousGetHandle() == context.Device, "A later acquisition retains the canonical device.");
            Task.Run(() =>
            {
                using var worker = GPUDeviceFactory.Acquire(false).Handle;
                Check(worker.DangerousGetHandle() == context.Device, "Different logical owners retain the same physical device.");
            }).GetAwaiter().GetResult();
            local.FreeRID(buffer);
            Console.WriteLine("Device leases preserve RID isolation, finalizer fallback, cross-owner retention and native window retirement.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, rendering);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AbandonLease() => new(GPUDeviceFactory.Acquire(false).Handle);
}
