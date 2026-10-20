using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using SDL3;
using Image = Electron2D.Image;
using static TextureArrayTests;

internal static class LowProcessorRenderingTests
{
    internal static void RunManaged() => LowProcessorContract.Run();

    internal static void RunNative(string backend)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var mode = ProjectSettings.Get(ProjectSettings.LowProcessorMode); var sleep = ProjectSettings.Get(ProjectSettings.LowProcessorModeSleepUsec);
        var fps = Engine.MaxFPS; var oldMode = OS.LowProcessorUsageMode; var oldSleep = OS.LowProcessorUsageModeSleepUsec;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            ProjectSettings.Set(ProjectSettings.LowProcessorMode, true); ProjectSettings.Set(ProjectSettings.LowProcessorModeSleepUsec, 4000);
            Engine.MaxFPS = 0;
            var window = new Window { Size = new(64, 48), Title = "Electron2D rendering changes", CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
            var box = new Box { Position = new(8, 8) }; var driver = new Driver(window, box);
            window.AddChild(new Camera { AnchorMode = AnchorMode.FixedTopLeft });
            window.AddChild(box); window.AddChild(driver);
            window.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested native backend.");
                Check(OS.LowProcessorUsageMode && OS.LowProcessorUsageModeSleepUsec == 4000, "Project policy is applied before readiness.");
                RenderingServer.SetDefaultClearColor(Colors.Black);
            };
            Check(Engine.Run(window) == 0 && driver.Completed && window.IsDisposed, "Owned idle/active run and cleanup.");
            Check(!RenderingServer.IsAvailable && !DisplayServer.IsAvailable, "Native services retired.");
            Console.WriteLine(JsonSerializer.Serialize(new { scenario = "low-processor-rendering", backend, driver.IdleFrames, driver.IdleBytes, driver.ActiveFrames, driver.ActiveBytes, driver.Elapsed }));
            ProjectSettings.Set(ProjectSettings.LowProcessorModeSleepUsec, int.MaxValue);
            var captured = new Window { Size = new(64, 48), Title = "Electron2D static capture" };
            captured.Ready += _ => RenderingServer.SetDefaultClearColor(Colors.Red);
            using var writer = new MovieWriterTests.Writer { Window = captured, FPS = 59, Limit = 8 };
            MovieWriter.AddWriter(writer); var drawn = Engine.GetFramesDrawn();
            Check(Engine.Run(captured, "low-processor.movieprobe", 59) == 17 && writer.Frames == 8 && writer.Ends == 1 && Engine.GetFramesDrawn() - drawn == 8,
                "Static movie capture renders all eight actual RGB frames and bypasses the enormous configured wait.");
            Console.WriteLine(JsonSerializer.Serialize(new { scenario = "low-processor-capture", backend, writer.Frames, writer.Ends, drawn = Engine.GetFramesDrawn() - drawn }));
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback);
            ProjectSettings.Set(ProjectSettings.LowProcessorMode, mode); ProjectSettings.Set(ProjectSettings.LowProcessorModeSleepUsec, sleep);
            Engine.MaxFPS = fps; OS.LowProcessorUsageMode = oldMode; OS.LowProcessorUsageModeSleepUsec = oldSleep;
        }
    }

    private sealed class Driver(Window window, Box box) : Node
    {
        private int ticks; private ulong idleDrawn, physics; private long bytes; private readonly Stopwatch watch = new();
        private Shader? shader; private ShaderMaterial? material;
        internal bool Completed; internal int IdleFrames; internal long IdleBytes; internal ulong ActiveFrames; internal long ActiveBytes; internal double Elapsed;
        protected override void OnReady() => ProcessEnabled = true;
        protected override void OnProcess(double delta)
        {
            ticks++;
            if (ticks == 1) { Contract(); watch.Start(); }
            if (ticks == 64) { Settle(); idleDrawn = Engine.GetFramesDrawn(); physics = Engine.PhysicsFrames; bytes = GC.GetAllocatedBytesForCurrentThread(); }
            if (ticks > 64 && ticks <= 192)
            {
                Check(Engine.GetFramesDrawn() == idleDrawn && !RenderingServer.HasChanged(), "Idle owned iterations do not draw."); IdleFrames++;
            }
            if (ticks == 192)
            {
                IdleBytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
                Check(IdleBytes == 0 && Engine.PhysicsFrames > physics, "Prepared idle loop allocates zero owner bytes while physics continues.");
                box.Modulate = Colors.Green;
            }
            if (ticks == 200)
            {
                Check(Engine.GetFramesDrawn() > idleDrawn, "Scene mutation resumes automatic drawing."); Pixel(Colors.Green); Settle();
                box.Sliced = true; box.QueueRedraw(); idleDrawn = Engine.GetFramesDrawn();
            }
            if (ticks == 216)
            {
                ActiveFrames = Engine.GetFramesDrawn() - idleDrawn; Check(ActiveFrames >= 15, "Retained animation intervals continue automatic drawing.");
                box.Sliced = false; box.QueueRedraw();
            }
            if (ticks == 224)
            {
                Settle();
                var windows = SDL.GetWindows(out _); var exposed = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowExposed, WindowID = SDL.GetWindowID(windows![0]) } };
                Check(SDL.PushEvent(ref exposed), "Native expose accepted."); idleDrawn = Engine.GetFramesDrawn();
            }
            if (ticks == 232)
            {
                Check(Engine.GetFramesDrawn() > idleDrawn, "Expose repaints unchanged scene output.");
                OS.LowProcessorUsageMode = false; idleDrawn = Engine.GetFramesDrawn();
            }
            if (ticks == 240)
            {
                Check(Engine.GetFramesDrawn() >= idleDrawn + 8, "Disabling optimization restores ordinary automatic drawing.");
                OS.LowProcessorUsageMode = true; window.Tree!.PhysicsInterpolation = true;
                box.Moving = true; box.PhysicsProcessEnabled = true; idleDrawn = Engine.GetFramesDrawn();
            }
            if (ticks == 272)
            {
                Check(Engine.GetFramesDrawn() > idleDrawn + 16, "Interpolation keeps drawing between physical pose writes.");
                box.Moving = false; window.Tree!.PhysicsInterpolation = false; box.ResetPhysicsInterpolation();
                if (RenderingServer.GetCurrentRenderingMethod() == "gpu")
                {
                    using var input = typeof(LowProcessorRenderingTests).Assembly.GetManifestResourceStream("TestShaders.TimeOnly.spv")!;
                    using var data = new MemoryStream(); input.CopyTo(data); shader = Shader.CreateFromSPIRV(data.ToArray());
                    material = new ShaderMaterial { Shader = shader }; box.Material = material; idleDrawn = Engine.GetFramesDrawn();
                }
            }
            if (ticks == 288)
            {
                if (shader is not null)
                {
                    Check(Engine.GetFramesDrawn() >= idleDrawn + 16 && RenderingServer.HasChanged(), "TIME keeps ordinary automatic frames active.");
                    box.Material = null; material!.Dispose(); shader.Dispose();
                }
            }
            if (ticks == 296)
            {
                Settle(); Elapsed = watch.Elapsed.TotalSeconds;
                Check(Elapsed > .5, "Configured wait throttles an otherwise unlimited owned loop.");
                OS.LowProcessorUsageModeSleepUsec = int.MaxValue;
                var windows = SDL.GetWindows(out _); var close = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = SDL.GetWindowID(windows![0]) } };
                Check(SDL.PushEvent(ref close), "Close request queued before a long wait."); Completed = true;
            }
            if (ticks > 306) throw new InvalidOperationException("Rendering change test did not complete.");
        }

        private void Contract()
        {
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.HasChanged())).GetAwaiter().GetResult();
            Check(RenderingServer.HasChanged(), "New native session requires output."); Settle();
            Check(!RenderingServer.HasChanged() && !RenderingServer.HasChanged(), "Observation does not consume or create changes.");
            box.Position += Vector2.One; Check(RenderingServer.HasChanged(), "Transform invalidates without rerecording."); Settle();
            RenderingServer.CanvasItemSetModulate(box.GetCanvasItem(), Colors.Blue); Check(RenderingServer.HasChanged(), "Retained item state invalidates."); Settle(); Pixel(Colors.Blue);
            box.Modulate = Colors.White; Settle();
            using var resource = new Gradient(); resource.SetBlockSignals(true);
            Task.Run(resource.EmitChanged).GetAwaiter().GetResult(); Check(RenderingServer.HasChanged(), "Off-owner blocked resource publication still invalidates."); Settle();
            RenderingServer.RequestFrameDrawnCallback(() => box.Position += Vector2.One);
            RenderingServer.ForceDraw(); Check(RenderingServer.HasChanged(), "Completion mutation survives frame commitment."); Settle();
            RenderingServer.ForceDraw(false); Check(RenderingServer.HasChanged(), "Nonpresented drawing retains presentation work."); Settle();
            box.Fault = true; box.QueueRedraw(); Reject<InvalidOperationException>(() => RenderingServer.ForceDraw());
            Check(RenderingServer.HasChanged(), "Failed recording retains pending work."); box.Fault = false; Settle();
            RenderingServer.RenderLoopEnabled = false; box.QueueRedraw(); RenderingServer.ForceDraw(); RenderingServer.RenderLoopEnabled = true; Settle();
            using var pixels = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); pixels.Fill(Colors.Red);
            using var red = ImageTexture.CreateFromImage(pixels); pixels.Fill(Colors.Green);
            using var green = ImageTexture.CreateFromImage(pixels);
            using var animation = new AnimatedTexture { Frames = 2, Pause = true, OneShot = true };
            animation.SetFrameTexture(0, red); animation.SetFrameTexture(1, green);
            animation.SetFrameDuration(0, .02f); animation.SetFrameDuration(1, .02f);
            using var sprite = new Sprite { Texture = animation, Centered = false, Position = new(40, 8) };
            window.AddChild(sprite); Settle();
            animation.Pause = false; RenderingServer.ForceDraw(); Thread.Sleep(25);
            Check(animation.CurrentFrame == 1 || RenderingServer.HasChanged(), "Due animation is either already presented or requests a draw.");
            RenderingServer.ForceDraw(); Check(animation.CurrentFrame == 1, "Animation advances through the ordinary render clock producer.");
            animation.Pause = true; Settle();
            pixels.Fill(Colors.Blue); green.Update(pixels); Check(RenderingServer.HasChanged(), "Borrowed texture content invalidates output."); Settle();
            sprite.QueueFree();
            using var sub = new SubViewport { Size = new(16, 16), RenderTargetUpdateMode = ViewportUpdateMode.Always };
            window.AddChild(sub); RenderingServer.ForceDraw();
            Check(RenderingServer.HasChanged(), "Explicit Always viewport remains a continuous consumer.");
            sub.RenderTargetUpdateMode = ViewportUpdateMode.Disabled; Settle();
            for (var i = 0; i < 64; i++) { box.Modulate = (i & 1) == 0 ? Colors.Red : Colors.White; RenderingServer.ForceDraw(); }
            var activeStart = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { box.Modulate = (i & 1) == 0 ? Colors.Red : Colors.White; RenderingServer.ForceDraw(); }
            ActiveBytes = GC.GetAllocatedBytesForCurrentThread() - activeStart;
            Check(ActiveBytes == 0, "Prepared active mutation, revision and native rendering allocate zero owner bytes."); Settle();

        }
        private static void Settle()
        {
            for (var i = 0; i < 8 && RenderingServer.HasChanged(); i++) RenderingServer.ForceDraw();
            Check(!RenderingServer.HasChanged(), "Static retained output settles after successful presentation.");
        }
        private void Pixel(Color expected)
        {
            using var image = window.GetTexture().GetImage() ?? throw new InvalidOperationException("Missing actual pixels.");
            var actual = image.GetPixel(16, 16);
            Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02, "Actual native output follows mutation.");
        }
    }
    private sealed class Box : Entity
    {
        internal bool Fault, Sliced, Moving;
        protected override void OnPhysicsProcess(double delta) { if (Moving) Position += new Vector2(.05f, 0); }
        protected override void OnDraw()
        {
            if (Fault) throw new InvalidOperationException("Expected recording failure.");
            if (Sliced) DrawAnimationSlice(.08, 0, .04);
            DrawRect(new(0, 0, 24, 24), Colors.White);
        }
    }
}
