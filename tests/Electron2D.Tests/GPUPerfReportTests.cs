using System.Reflection;
using Electron2D;
using SDL3;
using static TextureArrayTests;

internal static class GPUPerfReportTests
{
    internal static void Run(string driver)
    {
        VerifyLocal();
        VerifyRenderer(driver);
        Console.WriteLine($"Rendering-device performance report passed on {driver}.");
    }

    private static void VerifyLocal()
    {
        var device = RenderingServer.CreateLocalRenderingDevice();
        using (device)
        {
            Expect(device, 0, 0, 0);
            var source = device.StorageBufferCreate(16, [1, 2, 3, 4]);
            var destination = device.StorageBufferCreate(16);
            var uniform = device.UniformBufferCreate(16, [1, 2, 3, 4]);
            device.BufferUpdate(uniform, 4, 4, [5, 6, 7, 8]);
            device.BufferClear(uniform, 8, 4);
            device.BufferCopy(uniform, uniform, 4, 12, 4);
            device.BufferUpdate(source, 4, 4, [5, 6, 7, 8]);
            device.BufferClear(source, 8, 4);
            device.BufferCopy(uniform, destination, 12, 0, 4);
            device.BufferCopy(source, destination, 0, 4, 4);
            device.BufferUpdate(source, 16, 0, []);
            Reject<ArgumentOutOfRangeException>(() => device.BufferUpdate(source, 15, 4, [0, 0, 0, 0]));
            Check(device.BufferGetData(destination, 0, 8).AsSpan().SequenceEqual((ReadOnlySpan<byte>)[5, 6, 7, 8, 1, 2, 3, 4]),
                "Internal flush delivers real GPU buffer data.");
            Expect(device, 0, 0, 0);
            device.Submit();
            Expect(device, 5, 4, 60);
            device.Sync();

            device.BufferUpdate(source, 4, 4, [8, 7, 6, 5]);
            device.Submit();
            Expect(device, 1, 0, 4);
            Reject<InvalidOperationException>(device.Submit);
            device.Sync();
            Expect(device, 1, 0, 4);

            device.BufferUpdate(source, 8, 4, [9, 9, 9, 9]);
            RejectNativeSubmission(device);
            Expect(device, 1, 0, 4);
            device.BufferUpdate(source, 0, 8, [1, 2, 3, 4, 5, 6, 7, 8]);
            device.Submit();
            device.Sync();
            Expect(device, 1, 0, 8);
            Check(device.BufferGetData(source, 8, 4).AsSpan().SequenceEqual((ReadOnlySpan<byte>)[0, 0, 0, 0]),
                "A rejected native submission leaves the destination unchanged.");
            device.Submit();
            device.Sync();
            Expect(device, 0, 0, 0);

            for (var i = 0; i < 32; i++) UploadAndSubmit(device, source);
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) UploadAndSubmit(device, source);
            Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared reporting does not allocate on submission.");
            Expect(device, 1, 0, 4);
            Reject<InvalidOperationException>(() => Task.Run(device.GetPerfReport).GetAwaiter().GetResult());
        }
        Reject<ObjectDisposedException>(() => device.GetPerfReport());
    }

    private static void VerifyRenderer(string driver)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(64, 64) };
        var complete = false;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingDriverName() == driver, "The requested backend is active.");
            var device = RenderingServer.GetRenderingDevice()!;
            var buffer = device.StorageBufferCreate(16);
            var uniform = device.UniformBufferCreate(16, [1, 2, 3, 4]);
            RenderingServer.ForceSync();
            Expect(device, 0, 0, 0);
            void UploadBeforeDraw() => device.BufferUpdate(buffer, 0, 4, [5, 6, 7, 8]);
            void ReadAfterDraw() => Expect(device, 2, 1, 24);
            RenderingServer.FramePreDraw += UploadBeforeDraw;
            RenderingServer.FramePostDraw += ReadAfterDraw;
            try { RenderingServer.ForceDraw(swapBuffers: false); }
            finally
            {
                RenderingServer.FramePreDraw -= UploadBeforeDraw;
                RenderingServer.FramePostDraw -= ReadAfterDraw;
            }
            Expect(device, 2, 1, 24);

            void InterruptAfterFlush()
            {
                device.BufferUpdate(buffer, 4, 4, [8, 7, 6, 5]);
                Check(device.BufferGetData(buffer, 4, 4).AsSpan().SequenceEqual((ReadOnlySpan<byte>)[8, 7, 6, 5]),
                    "An interrupted frame can have accepted buffer uploads.");
                throw new ApplicationException("Interrupt after an internal flush.");
            }
            RenderingServer.FramePreDraw += InterruptAfterFlush;
            try { Reject<ApplicationException>(() => RenderingServer.ForceDraw(swapBuffers: false)); }
            finally { RenderingServer.FramePreDraw -= InterruptAfterFlush; }
            Expect(device, 2, 1, 24);
            RenderingServer.ForceDraw();
            Expect(device, 1, 0, 4);
            RenderingServer.ForceDraw();
            Expect(device, 0, 0, 0);
            device.FreeRID(buffer);
            device.FreeRID(uniform);
            complete = true;
            window.Tree!.Quit();
        };
        Check(Engine.Run(window) == 0 && complete, "The renderer report completed a native frame.");
    }

    private static void UploadAndSubmit(RenderingDevice device, RID buffer)
    {
        device.BufferUpdate(buffer, 0, 4, [1, 2, 3, 4]);
        device.Submit();
        device.Sync();
    }

    private static void RejectNativeSubmission(RenderingDevice device)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var field = typeof(SDL).GetField("SubmitGPUCommandBufferAndAcquireFenceNativeFunction", flags)!;
        var original = field.GetValue(null);
        var method = typeof(GPUPerfReportTests).GetMethod(nameof(CancelSubmission), flags)!;
        field.SetValue(null, method.CreateDelegate(field.FieldType));
        try { Reject<InvalidOperationException>(device.Submit); }
        finally { field.SetValue(null, original); }
    }

    private static nint CancelSubmission(nint command)
    {
        if (!SDL.CancelGPUCommandBuffer(command)) throw new ApplicationException("The rejected command buffer could not be cancelled.");
        return 0;
    }

    private static void Expect(RenderingDevice device, int gpu, int direct, int bytes)
    {
        var expected = FormattableString.Invariant($" gpu:{gpu} direct:{direct} bytes:{bytes} lazily alloc:0");
        var report = device.GetPerfReport();
        Check(report == expected, $"Expected '{expected}', got '{report}'.");
    }
}
