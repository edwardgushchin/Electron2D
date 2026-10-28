using System.Runtime.InteropServices;
using Electron2D;
using static MovieWriterTests;

internal static class GPUTimestampTests
{
    internal static void Run(string profile)
    {
        var limit = ProjectSettings.Get(ProjectSettings.MaxTimestampQueryElements);
        try
        {
            Reject<ArgumentException>(() => ProjectSettings.Set(ProjectSettings.MaxTimestampQueryElements, 0));
            ProjectSettings.Set(ProjectSettings.MaxTimestampQueryElements, 3);
            if (profile == "renderer") Renderer(); else { Local(); Draw(); Retirement(); }
        }
        finally { ProjectSettings.Set(ProjectSettings.MaxTimestampQueryElements, limit); }
    }
    private static void Local()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        ProjectSettings.Set(ProjectSettings.MaxTimestampQueryElements, 5);
        Check(device.GetCapturedTimestampsCount() == 0 && device.GetCapturedTimestampsFrame() == 0, "Fresh device has no completed timestamps.");
        Reject<ArgumentNullException>(() => device.CaptureTimestamp(null!));
        foreach (var index in new[] { -1, 0, int.MaxValue })
        {
            Reject<ArgumentOutOfRangeException>(() => device.GetCapturedTimestampName(index));
            Reject<ArgumentOutOfRangeException>(() => device.GetCapturedTimestampCPUTime(index));
            Reject<ArgumentOutOfRangeException>(() => device.GetCapturedTimestampGPUTime(index));
        }
        Reject<InvalidOperationException>(() => Task.Run(() => device.CaptureTimestamp("worker")).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => device.GetCapturedTimestampsCount()).GetAwaiter().GetResult());
        using var source = new RDShaderSPIRV();
        using (var stream = typeof(GPUTimestampTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalCompute.comp.spv")!)
        { using var bytes = new MemoryStream(); stream.CopyTo(bytes); source.BytecodeCompute = bytes.ToArray(); }
        var shader = device.ShaderCreateFromSPIRV(source); var pipeline = device.ComputePipelineCreate(shader);
        float[] initial = [1, 2, 3, 4], settings = [2, 4, 3, 0], result = new float[4];
        var buffer = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(initial.AsSpan()));
        var uniform = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(settings.AsSpan()));
        using var output = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.StorageBuffer }; output.AddID(buffer);
        using var input = new RDUniform { Binding = 4, UniformType = RenderingDevice.UniformType.UniformBuffer }; input.AddID(uniform);
        var outputs = device.UniformSetCreate([output], shader, 3); var inputs = device.UniformSetCreate([input], shader, 2);
        var earliest = Time.GetTicksUSEC(); device.CaptureTimestamp("before 🌌");
        var list = device.ComputeListBegin(); device.CaptureTimestamp("");
        device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, outputs, 3); device.ComputeListBindUniformSet(list, inputs, 2);
        device.ComputeListDispatch(list, 1, 1, 1);
        Reject<InvalidOperationException>(() => device.CaptureTimestamp("used compute list"));
        device.ComputeListEnd(); device.CaptureTimestamp("after");
        Reject<InvalidOperationException>(() => device.CaptureTimestamp("capacity remains three"));
        Check(device.GetCapturedTimestampsCount() == 0, "Recorded work is unpublished.");
        device.Submit(); Check(device.GetCapturedTimestampsCount() == 0, "Submission alone does not publish unfinished GPU results.");
        Reject<InvalidOperationException>(device.Submit); device.Sync();
        Check(device.GetCapturedTimestampsCount() == 3 && device.GetCapturedTimestampsFrame() == 1, "Completed submission publishes one consistent marker frame.");
        Check(device.GetCapturedTimestampName(0) == "before 🌌" && device.GetCapturedTimestampName(1) == "" && device.GetCapturedTimestampName(2) == "after", "Unicode and empty marker names preserve order.");
        Check(device.GetCapturedTimestampCPUTime(0) >= earliest && device.GetCapturedTimestampCPUTime(2) <= Time.GetTicksUSEC(), "CPU markers use the existing microsecond clock.");
        Check(device.GetCapturedTimestampGPUTime(2) > device.GetCapturedTimestampGPUTime(0), "Real compute work advances native GPU timestamps.");
        for (var i = 1; i < 3; i++) Check(device.GetCapturedTimestampCPUTime(i) >= device.GetCapturedTimestampCPUTime(i - 1) && device.GetCapturedTimestampGPUTime(i) >= device.GetCapturedTimestampGPUTime(i - 1), "Both marker timelines preserve command order.");
        device.CaptureTimestamp("next frame");
        Check(device.GetCapturedTimestampsCount() == 3 && device.GetCapturedTimestampName(0) == "before 🌌", "Recording the next frame preserves the complete previous result bank.");
        device.Submit(); device.Sync();
        Check(device.GetCapturedTimestampsCount() == 1 && device.GetCapturedTimestampName(0) == "next frame", "The next completed bank atomically replaces the old one.");
        device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan()));
        Check(result.SequenceEqual(new float[] { 5, 7, 9, 11 }), "The timed compute dispatch produces actual output.");
        Check(device.GetCapturedTimestampsCount() == 0 && device.GetCapturedTimestampsFrame() == 3, "An empty completed transfer frame replaces the previous capture.");
        void Round()
        {
            device.CaptureTimestamp("repeat"); device.CaptureTimestamp("repeat"); device.Submit(); device.Sync();
            Check(device.GetCapturedTimestampsCount() == 2 && device.GetCapturedTimestampName(1) == "repeat", "Repeated names and query-pool reuse execute.");
        }
        for (var i = 0; i < 32; i++) Round();
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Round(); var active = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { _ = device.GetCapturedTimestampsCount(); _ = device.GetCapturedTimestampsFrame(); _ = device.GetCapturedTimestampName(0); _ = device.GetCapturedTimestampCPUTime(0); _ = device.GetCapturedTimestampGPUTime(1); }
        var idle = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(active == 0 && idle == 0, "Prepared timestamp capture/submission/query intervals allocate no managed owner bytes.");
        Console.WriteLine($"GPU timestamps local: real compute output, guards, frame publication, 64 active/64 idle intervals {active}/{idle} B passed.");
    }
    private static void Draw()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var module = RenderingDeviceGraphicsTests.Load("DeviceProcedural"); var shader = device.ShaderCreateFromSPIRV(module);
        using var format = new RDTextureFormat { Width = 16, Height = 16, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.ColorAttachment | RenderingDevice.TextureUsageBits.CanCopyFrom };
        using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(target, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Blue]);
        device.CaptureTimestamp("first marker inside empty draw list");
        device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3);
        Reject<InvalidOperationException>(() => device.CaptureTimestamp("used draw list"));
        device.DrawListEnd(); device.CaptureTimestamp("draw completed"); device.Submit(); device.Sync();
        Check(device.GetCapturedTimestampsCount() == 2 && device.GetCapturedTimestampGPUTime(1) >= device.GetCapturedTimestampGPUTime(0), "First-use query reset and native render-pass markers execute.");
        var pixels = device.TextureGetData(image, 0); var at = (8 * 16 + 8) * 4;
        Check(pixels[at] == 255 && pixels[at + 1] == 0 && pixels[at + 2] == 0, "The timed draw produces red pixels.");
        Console.WriteLine("GPU timestamps graphics: first capture inside native render pass and actual red pixels passed.");
    }
    private static void Retirement()
    {
        var recorded = RenderingServer.CreateLocalRenderingDevice(); recorded.CaptureTimestamp("cancel at disposal"); recorded.Dispose();
        Reject<ObjectDisposedException>(() => recorded.CaptureTimestamp("disposed"));
        Reject<ObjectDisposedException>(() => recorded.GetCapturedTimestampsCount());
        var submitted = RenderingServer.CreateLocalRenderingDevice(); submitted.CaptureTimestamp("wait at disposal"); submitted.Submit(); submitted.Dispose();
        using var fresh = RenderingServer.CreateLocalRenderingDevice(); Check(fresh.GetCapturedTimestampsCount() == 0, "Native pools and completed state do not leak into a fresh device.");
    }
    private static void Renderer()
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        var root = new Window { Size = new(320, 240) }; root.AddChild(new ColorRect { Color = Colors.Red, Size = new(320, 240) });
        RenderingDevice? device = null; var frames = 0;
        void Before() { device!.CaptureTimestamp("renderer start"); device.CaptureTimestamp("renderer end"); }
        void After()
        {
            frames++; Check(device!.GetCapturedTimestampsCount() == 2 && device.GetCapturedTimestampGPUTime(1) >= device.GetCapturedTimestampGPUTime(0), "Renderer completion publishes native markers before post-draw callbacks.");
            using var image = RenderingServer.Service!.Readback(); Check(image.GetPixel(8, 8) == Colors.Red, "Native timestamp work preserves window rendering.");
            if (frames == 2) { RenderingServer.FramePreDraw -= Before; RenderingServer.FramePostDraw -= After; root.Tree!.Quit(); }
        }
        root.Ready += _ => { device = RenderingServer.GetRenderingDevice(); RenderingServer.FramePreDraw += Before; RenderingServer.FramePostDraw += After; };
        try { Check(Engine.Run(root) == 0 && frames == 2 && device!.IsDisposed, "The borrowed device retires with the renderer."); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
        Console.WriteLine("GPU timestamps renderer: two native marker frames, real window pixels and borrowed-device lifetime passed.");
    }
}
