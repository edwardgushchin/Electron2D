using Electron2D;
using Monitor = Electron2D.Performance.Monitor;
using Info = Electron2D.RenderingServer.RenderingInfo;

internal static class CanvasPipelineContract
{
    private static long Read()
    {
        var count = RenderingServer.GetRenderingInfo(Info.PipelineCompilationsCanvas);
        Check(Performance.GetMonitor(Monitor.PipelineCompilationsCanvas) == count, "Server and Performance agree while the producer is paused.");
        return count;
    }
    internal static void RunManaged()
    {
        Check((int)Info.PipelineCompilationsCanvas == 6 && (int)Monitor.PipelineCompilationsCanvas == 34, "Pinned selector identities.");
        Check(!RenderingServer.IsAvailable && Read() == 0, "Cold queries do not create a renderer.");
        Task.Run(() => Check(Read() == 0, "Cold worker reads need no owner/device.")).GetAwaiter().GetResult();
        foreach (var value in new[] { -1, 3, 4, 5, 7, int.MaxValue }) Reject<ArgumentOutOfRangeException>(() => RenderingServer.GetRenderingInfo((Info)value));
        foreach (var value in new[] { -1, 4, 5, 35, 36, 37, 38, int.MaxValue }) Reject<ArgumentOutOfRangeException>(() => Performance.GetMonitor((Monitor)value));
        for (var i = 0; i < 128; i++) _ = Read();
        var before = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 256; i++) _ = Read();
        Check(GC.GetAllocatedBytesForCurrentThread() == before && GC.GetTotalAllocatedBytes(true) == all, "Prepared cold reads allocate 0/0 bytes.");
        Console.WriteLine("Canvas pipelines: cold owner/worker reads, selector gaps and 256 zero-allocation batches passed.");
    }
    internal static void Run(string renderer, Action<Action>? failPipeline = null)
    {
        RunManaged();
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            long previousFinal = 0;
            for (var run = 0; run < 2; run++)
            {
                using var world = new World(PhysicsServer.Backend.CPU);
                using var add = new CanvasItemMaterial { BlendMode = BlendMode.Add };
                using var sub = new CanvasItemMaterial { BlendMode = BlendMode.Sub };
                using var mul = new CanvasItemMaterial { BlendMode = BlendMode.Mul };
                using var premult = new CanvasItemMaterial { BlendMode = BlendMode.PremultAlpha };
                using var shader = renderer == "gpu" ? Shader.CreateFromSPIRV(ShaderBytes()) : null;
                using var custom = shader is null ? null : new ShaderMaterial { Shader = shader };
                using var window = new Window { Size = new(96, 64), World = world, Title = "Canvas pipeline diagnostics" };
                var paint = new Paint(); window.AddChild(paint); var completed = false; var stop = 0; Exception? readerError = null;
                using var started = new ManualResetEventSlim();
                var reader = new Thread(() =>
                {
                    try
                    {
                        started.Set();
                        while (Volatile.Read(ref stop) == 0)
                        {
                            if (RenderingServer.GetRenderingInfo(Info.PipelineCompilationsCanvas) < 0 || Performance.GetMonitor(Monitor.PipelineCompilationsCanvas) < 0)
                                throw new InvalidOperationException("Invalid retired snapshot.");
                            Thread.SpinWait(64);
                        }
                    }
                    catch (Exception error) { readerError = error; }
                });
                void Exercise(Window current, ulong frame)
                {
                    Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "The requested renderer is actually active.");
                    Console.WriteLine($"Canvas pipeline backend={RenderingServer.GetCurrentRenderingMethod()}, driver={RenderingServer.GetCurrentRenderingDriverName()}.");
                    var initial = Read();
                    Check(renderer == "gpu" ? initial > 0 : initial == 0, "Initial owned pipeline versus compatibility capability.");
                    if (run != 0 && renderer == "gpu") Check(initial < previousFinal, "A new renderer resets the cumulative count.");
                    RenderingServer.ForceDraw(false); var baseline = Read();
                    using (var image = window.GetTexture().GetImage()!)
                    {
                        var pixel = image.GetPixel(12, 12); Check(pixel.R < .1f && pixel.G > .9f && pixel.B > .9f, "Real cyan canvas pixels.");
                    }
                    Change(add); var added = Read(); Expect(added, baseline, 1, "New blend pipeline.");
                    RenderingServer.ForceDraw(false); Check(Read() == added, "Cache hit does not compile.");
                    Change(sub); var subtracted = Read(); Expect(subtracted, added, 1, "Second native pipeline.");
                    paint.Visible = false;
                    for (var i = 0; i < 4; i++) RenderingServer.ForceDraw(false);
                    Check(Read() == subtracted, "Cache retirement never decrements the cumulative count.");
                    paint.Visible = true; RenderingServer.ForceDraw(false); var recreated = Read();
                    Check(recreated == subtracted, "Permanent built-in pipeline survives hidden frames.");
                    if (custom is not null)
                    {
                        Change(custom); Expect(Read(), recreated, 1, "Custom canvas pipeline.");
                        paint.Visible = false;
                        for (var i = 0; i < 4; i++) RenderingServer.ForceDraw(false);
                        Check(Read() == recreated + 1, "Retiring a custom pipeline retains the cumulative count.");
                        paint.Visible = true; RenderingServer.ForceDraw(false);
                        Expect(Read(), recreated + 1, 1, "A retired custom pipeline is recreated.");
                        recreated = Read();
                    }
                    RenderingServer.RequestFrameDrawnCallback(static () => throw new ApplicationException("Expected completion failure."));
                    paint.Material = renderer == "gpu" ? mul : null; paint.QueueRedraw(); Reject<ApplicationException>(() => RenderingServer.ForceDraw(false));
                    var afterObserverFailure = Read(); Expect(afterObserverFailure, recreated, 1, "A later frame failure retains successful creation.");
                    if (renderer == "gpu" && failPipeline is not null)
                    {
                        failPipeline(() => Change(premult));
                        Check(Read() == afterObserverFailure, "Failed native creation is not counted.");
                        RenderingServer.ForceDraw(false); Expect(Read(), afterObserverFailure, 1, "Retry counts the successfully owned pipeline.");
                    }
                    paint.Fail = true; paint.QueueRedraw(); var beforeFailure = Read();
                    Reject<ApplicationException>(() => RenderingServer.ForceDraw(false)); Check(Read() == beforeFailure, "Failed authoring does not create a pipeline.");
                    paint.Fail = false; paint.QueueRedraw();
                    for (var i = 0; i < 64; i++) { RenderingServer.ForceDraw(false); _ = Read(); }
                    var stable = Read(); var before = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                    for (var i = 0; i < 64; i++) { paint.QueueRedraw(); RenderingServer.ForceDraw(false); _ = Read(); }
                    var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - before; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
                    Check(ownerBytes == 0 && allBytes == 0 && Read() == stable, $"Prepared full render/read cycles allocate {ownerBytes}/{allBytes} bytes without recompilation.");
                    Task.Run(() =>
                    {
                        for (var i = 0; i < 128; i++) _ = Read();
                        var allocated = GC.GetAllocatedBytesForCurrentThread();
                        for (var i = 0; i < 256; i++) Check(Read() == stable, "Worker snapshot.");
                        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared worker reads allocate zero bytes.");
                    }).GetAwaiter().GetResult();
                    previousFinal = Read(); reader.Start(); started.Wait(); completed = true; window.Tree!.Quit();
                    Console.WriteLine($"Canvas pipelines {renderer} run={run}: initial={initial}, final={previousFinal}, 64 full cycles={ownerBytes}/{allBytes} bytes.");
                }
                try { Check(Engine.Run(Exercise, window) == 0 && completed, "Native session completes."); }
                finally { Volatile.Write(ref stop, 1); if (reader.IsAlive) reader.Join(); }
                Check(readerError is null && Read() == 0, "Concurrent teardown reads never touch retired native state; completed closure is zero.");

                void Change(Material material) { paint.Material = renderer == "gpu" ? material : null; paint.QueueRedraw(); RenderingServer.ForceDraw(false); }
                void Expect(long actual, long before, long delta, string message) => Check(actual == before + (renderer == "gpu" ? delta : 0), $"{message} {actual}/{before}+{delta}");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private static byte[] ShaderBytes()
    {
        using var stream = typeof(CanvasPipelineContract).Assembly.GetManifestResourceStream("TestShaders.TimeOnly.spv")!;
        var bytes = new byte[stream.Length]; stream.ReadExactly(bytes); return bytes;
    }
    private sealed class Paint : Entity
    {
        internal bool Fail;
        protected override void OnDraw()
        {
            if (Fail) throw new ApplicationException("Expected authoring failure.");
            DrawRect(new(4, 4, 32, 24), Colors.Cyan);
        }
    }
    private static void Reject<T>(Action operation) where T : Exception
    {
        try { operation(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
