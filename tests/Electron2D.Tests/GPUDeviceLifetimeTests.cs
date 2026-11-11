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
            VerifyLocalReceiver();
            using var context = new GPUPhysicsDevice();
            using var local = RenderingServer.CreateLocalRenderingDevice();
            var buffer = local.StorageBufferCreate(4, new byte[] { 1, 2, 3, 4 });
            for (var cycle = 0; cycle < 3; cycle++)
            {
                using var window = new Window { Size = new(320, 240) };
                uint id = 0;
                var frames = 0;
                RenderingDevice? child = null;
                RID childBuffer = default;
                window.Ready += _ =>
                {
                    var native = SDL.GetWindows(out var count);
                    Check(count == 1 && native is { Length: 1 }, "One actual SDL window is active.");
                    id = SDL.GetWindowID(native![0]);
                    var global = RenderingServer.GetRenderingDevice()!;
                    Reject<ArgumentException>(() => global.BufferGetData(buffer));
                    child = global.CreateLocalDevice();
                    Check(child.GetDeviceName() == global.GetDeviceName(), "A local device uses the active renderer GPU.");
                    childBuffer = child.StorageBufferCreate(4, [5, 6, 7, 8]);
                    Reject<ArgumentException>(() => global.BufferGetData(childBuffer));
                    RenderingServer.FramePostDraw += AfterFrame;
                };
                try
                {
                    Check(Engine.Run(window) == 0 && frames == 2, "Two native frames finish.");
                    Check(id != 0 && SDL.GetWindowFromID(id) == 0, "Compute retention must not keep a closed SDL window alive.");
                    Check(child is not null && child.BufferGetData(childBuffer).SequenceEqual(new byte[] { 5, 6, 7, 8 }),
                        "A device created from the canvas remains usable after canvas retirement.");
                }
                finally { child?.Dispose(); }
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
            Console.WriteLine($"Device leases preserve RID isolation, independent submissions, finalizer fallback and native window retirement ({context.Driver}, {context.DeviceName}).");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, rendering);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback);
        }
    }

    private static void VerifyLocalReceiver()
    {
        using var receiver = RenderingServer.CreateLocalRenderingDevice();
        Task.Run(() => Reject<InvalidOperationException>(() => receiver.CreateLocalDevice())).GetAwaiter().GetResult();
        var original = receiver.StorageBufferCreate(4, [1, 2, 3, 4]);
        receiver.ComputeListBegin();
        using var local = receiver.CreateLocalDevice();
        receiver.ComputeListEnd();
        var delivered = false;
        receiver.BufferGetDataAsync(original, data =>
        {
            Check(data.AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 4 }), "The receiver keeps its own GPU data.");
            delivered = true;
        });

        var source = local.StorageBufferCreate(4, [9, 8, 7, 6]);
        var destination = local.StorageBufferCreate(4);
        local.BufferCopy(source, destination, 0, 0, 4);
        local.Submit();
        local.Sync();
        Check(!delivered, "The local submission leaves receiver work recorded.");
        Check(local.BufferGetData(destination).SequenceEqual(new byte[] { 9, 8, 7, 6 }), "Local GPU copy completes independently.");
        Reject<ArgumentException>(() => local.BufferGetData(original));
        Reject<ArgumentException>(() => receiver.BufferGetData(destination));
        receiver.Submit();
        receiver.Sync();
        Check(delivered, "The receiver publishes its own readback when submitted.");
        receiver.Dispose();
        Reject<ObjectDisposedException>(() => receiver.CreateLocalDevice());
        local.BufferClear(destination, 0, 4);
        Check(local.BufferGetData(destination).SequenceEqual(new byte[4]), "A local lease survives receiver disposal.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AbandonLease() => new(GPUDeviceFactory.Acquire(false).Handle);
}
