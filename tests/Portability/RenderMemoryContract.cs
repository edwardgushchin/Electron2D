using Electron2D;
using Info = Electron2D.RenderingServer.RenderingInfo;
using Kind = Electron2D.RenderingDevice.MemoryType;
using Monitor = Electron2D.Performance.Monitor;

internal static class RenderMemoryContract
{
    private readonly record struct Memory(long Textures, long Buffers, long Total);
    private static Memory Read()
    {
        var textures = RenderingServer.GetRenderingInfo(Info.TextureMemUsed);
        var buffers = RenderingServer.GetRenderingInfo(Info.BufferMemUsed);
        var total = RenderingServer.GetRenderingInfo(Info.VideoMemUsed);
        Check(textures >= 0 && buffers >= 0 && total >= 0, "Nonnegative native byte counts.");
        Check(Performance.GetMonitor(Monitor.RenderTextureMemUsed) == textures &&
              Performance.GetMonitor(Monitor.RenderBufferMemUsed) == buffers &&
              Performance.GetMonitor(Monitor.RenderVideoMemUsed) == total, "Paused Performance and server sources agree.");
        return new(textures, buffers, total);
    }

    internal static void RunManaged()
    {
        Check((int)Info.TextureMemUsed == 3 && (int)Info.BufferMemUsed == 4 && (int)Info.VideoMemUsed == 5 &&
              (int)Monitor.RenderVideoMemUsed == 14 && (int)Monitor.RenderTextureMemUsed == 15 &&
              (int)Monitor.RenderBufferMemUsed == 16 && (int)Kind.Textures == 0 && (int)Kind.Buffers == 1 && (int)Kind.Total == 2,
              "Pinned diagnostic identities.");
        Check(!RenderingServer.IsAvailable && Read() == default, "Cold reads do not create native services.");
        for (var i = 0; i < 128; i++) _ = Read();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 256; i++) _ = Read();
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0, $"Prepared cold reads allocate {ownerBytes}/{allBytes} owner/all-thread bytes.");
        Task.Run(() => Check(Read() == default, "Cold worker reads.")).GetAwaiter().GetResult();
    }

    internal static void RunLocal()
    {
        RunManaged();
        using var first = RenderingServer.CreateLocalRenderingDevice();
        var initial = first.GetMemoryUsage(Kind.Total);
        Check(initial > 0 && Read() == default, "A local physical device does not publish a rendering service.");
        Reject<ArgumentOutOfRangeException>(() => first.GetMemoryUsage((Kind)3));
        Task.Run(() => Reject<InvalidOperationException>(() => first.GetMemoryUsage(Kind.Total))).GetAwaiter().GetResult();
        var before = first.GetMemoryUsage(Kind.Buffers);
        var buffer = first.StorageBufferCreate(1024 * 1024);
        first.Submit(); first.Sync();
        Check(first.GetMemoryUsage(Kind.Buffers) >= before + 1024 * 1024, "Actual storage/transfer regions contribute.");
        using var format = new RDTextureFormat
        {
            Width = 17,
            Height = 19,
            Format = RenderingDevice.DataFormat.R8G8B8A8UNORM,
            UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanUpdate
        };
        using var view = new RDTextureView();
        var textures = first.GetMemoryUsage(Kind.Textures);
        var texture = first.TextureCreate(format, view);
        first.Submit(); first.Sync();
        var added = first.GetMemoryUsage(Kind.Textures) - textures;
        Check(added >= 17 * 19 * 4, "Native texture allocation includes actual driver layout.");
        using var second = RenderingServer.CreateLocalRenderingDevice();
        foreach (var kind in new[] { Kind.Textures, Kind.Buffers, Kind.Total })
            Check(first.GetMemoryUsage(kind) == second.GetMemoryUsage(kind), "Independent namespaces observe one shared physical allocator.");
        for (var i = 0; i < 128; i++) _ = first.GetMemoryUsage(Kind.Total);
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 256; i++) { _ = first.GetMemoryUsage(Kind.Textures); _ = first.GetMemoryUsage(Kind.Buffers); _ = first.GetMemoryUsage(Kind.Total); }
        Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "Prepared local native reads allocate 0/0 bytes.");
        first.FreeRID(texture); first.FreeRID(buffer); first.Dispose();
        Reject<ObjectDisposedException>(() => first.GetMemoryUsage(Kind.Total));
        Check(second.GetMemoryUsage(Kind.Total) >= second.GetMemoryUsage(Kind.Textures) + second.GetMemoryUsage(Kind.Buffers), "The surviving native lease remains valid.");
        Console.WriteLine($"Render memory: local native ownership, aligned texture delta={added}, shared leases, disposal and 256 zero-allocation read batches passed.");
    }

    internal static void Run(string renderer, PhysicsServer.Backend backend)
    {
        RunManaged();
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            for (var session = 0; session < 2; session++)
            {
                using var world = new World(backend);
                using var window = new Window { Size = new(96, 64), World = world, Title = "Native render memory", CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
                using var image = Image.CreateEmpty(17, 19, false, Image.Format.Rgba8); image.Fill(Colors.Cyan);
                using var source = ImageTexture.CreateFromImage(image);
                var paint = new Paint { Texture = source }; window.AddChild(paint);
                using var shape = new CircleShape { Radius = 2 };
                var body = new RigidBody { Position = new(50, 30), GravityScale = 0, CanSleep = false };
                body.AddChild(new CollisionShape { Shape = shape }); window.AddChild(body);
                var completed = false; var stop = 0; Exception? failure = null;
                using var started = new ManualResetEventSlim();
                var reader = new Thread(() =>
                {
                    try
                    {
                        _ = Performance.GetMonitor(Monitor.RenderVideoMemUsed); started.Set();
                        while (Volatile.Read(ref stop) == 0)
                        {
                            if (Performance.GetMonitor(Monitor.RenderVideoMemUsed) < 0) throw new InvalidOperationException("Invalid concurrent memory snapshot.");
                            Thread.SpinWait(128);
                        }
                    }
                    catch (Exception error) { failure = error; started.Set(); }
                });
                void Exercise(Window current, ulong frame)
                {
                    if (paint.PhysicsSteps == 0) return;
                    Check(world.PhysicsBackend == backend && Performance.GetMonitor(Monitor.PhysicsActiveObjects) >= 1,
                        "The selected physical backend actually stepped an awake body.");
                    RenderingServer.ForceDraw(false);
                    Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "Requested renderer executes.");
                    var memory = Read();
                    Check(memory.Textures > 0 && memory.Total >= memory.Textures + memory.Buffers, "Real allocations are published before diagnostics.");
                    Check(renderer == "gpu" ? memory.Buffers > 0 && memory.Total > memory.Textures + memory.Buffers : memory.Buffers == 0 && memory.Total == memory.Textures,
                        "Native allocator reservation versus compatibility texture ownership.");
                    using (var pixels = window.GetTexture().GetImage()!)
                        Check(pixels.GetPixel(12, 12).G > .9f && pixels.GetPixel(12, 12).B > .9f, "Real cyan texture output.");
                    if (renderer == "gpu")
                    {
                        var device = RenderingServer.GetRenderingDevice()!;
                        memory = Read();
                        var alias = RenderingServer.TextureGetRDTexture(source.GetRID());
                        var exported = Read();
                        Check(exported.Textures >= memory.Textures && device.GetMemoryUsage(Kind.Textures) == exported.Textures,
                            "Native borrowed-image support storage is included.");
                        Check(RenderingServer.TextureGetRDTexture(source.GetRID()) == alias && Read() == exported, "Cached exports reuse source and support storage.");
                        device.FreeRID(alias); RenderingServer.ForceDraw(false);
                    }
                    for (var i = 0; i < 32; i++) { paint.QueueRedraw(); RenderingServer.ForceDraw(false); _ = Read(); }
                    var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                    for (var i = 0; i < 64; i++) { paint.QueueRedraw(); RenderingServer.ForceDraw(false); _ = Read(); }
                    Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "64 complete active render/read cycles allocate 0/0 managed bytes.");
                    Task.Run(() =>
                    {
                        var expected = Read(); for (var i = 0; i < 128; i++) _ = Read();
                        var start = GC.GetAllocatedBytesForCurrentThread();
                        for (var i = 0; i < 256; i++) if (Read() != expected) throw new InvalidOperationException("Paused worker snapshot changed.");
                        Check(GC.GetAllocatedBytesForCurrentThread() == start, "Prepared worker native reads allocate nothing.");
                    }).GetAwaiter().GetResult();
                    reader.Start(); started.Wait();
                    Console.WriteLine($"Render memory: renderer={renderer}, driver={RenderingServer.GetCurrentRenderingDriverName()}, physics={backend}, textures={memory.Textures}, buffers={memory.Buffers}, total={memory.Total}.");
                    completed = true; current.Tree!.Quit();
                }
                try { Check(Engine.Run(Exercise, window) == 0 && completed, "Native session completes."); }
                finally { Volatile.Write(ref stop, 1); if (reader.IsAlive) reader.Join(); }
                Check(failure is null && Read() == default, "Concurrent reads survive teardown; completed closure publishes zero.");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }

    private sealed class Paint : Entity
    {
        internal ImageTexture? Texture;
        internal int PhysicsSteps;
        protected override void OnPhysicsProcess(double delta) => PhysicsSteps++;
        protected override void OnDraw() { if (Texture is not null) DrawTexture(Texture, new(4, 4)); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action operation) where T : Exception
    {
        try { operation(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
