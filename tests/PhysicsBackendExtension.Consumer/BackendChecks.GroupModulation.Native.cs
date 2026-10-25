using Electron2D;

public static partial class BackendChecks
{
    public static void RunNativeGroupModulation(PhysicsServer.Backend backend, bool threaded, string renderer)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldPool = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        using var world = new World(backend); using var alternate = new World(backend); using var shape = new CircleShape { Radius = 2 };
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { World = world, Size = new(96, 64), Title = "Grouped canvas modulation" };
        var a = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var b = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var first = new CanvasModulate(); var second = new CanvasModulate(); var layer = new CanvasLayer(); var layerTint = new CanvasModulate();
        var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        body.AddChild(new CollisionShape { Shape = shape }); a.AddChild(body); a.AddChild(first); b.AddChild(second);
        a.AddChild(layer); layer.AddChild(layerTint); layer.AddChild(new Sprite { Texture = texture, Centered = false, Position = new(36, 4) });
        window.AddChild(new Sprite { Texture = texture, Centered = false, Position = new(4, 4) }); window.AddChild(a); window.AddChild(b);
        using var together = new Barrier(2); var sawConcurrent = false;
        var frame = 0; var phase = 0; var warnings = 0; var measured = 0; long ownerStart = 0, allStart = 0, ownerBytes = 0, allBytes = 0;
        RID originalBody = default; ulong originalID = 0;
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1]; using var target = window.GetTexture();
        a.Write = () =>
        {
            if (frame < 2 && !together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Native modulation groups did not overlap");
            first.Color = phase < 2 ? Colors.Red : phase == 2 ? Colors.Green : (frame & 1) == 0 ? Colors.Red : Colors.Green;
            layerTint.Color = Colors.Blue;
            first.PropagateNotification(CanvasItem.NotificationWorldChanged); first.PropagateNotification(CanvasItem.NotificationVisibilityChanged);
            first.UpdateConfigurationWarnings();
        };
        b.Write = () => { if (frame < 2 && !together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Native modulation groups did not overlap"); second.Color = phase == 0 ? Colors.Red : Colors.Blue; second.PropagateNotification(CanvasItem.NotificationWorldChanged); second.UpdateConfigurationWarnings(); };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); window.Tree!.EditedSceneRoot = window;
            originalBody = body.GetRID(); originalID = body.InstanceID;
            window.Tree.NodeConfigurationWarningChanged += (_, _) => Interlocked.Increment(ref warnings);
            window.Tree.ProcessFrameStarted += _ =>
            {
                frame++;
                if (frame is >= 64 and < 128) { ownerStart = GC.GetAllocatedBytesForCurrentThread(); allStart = GC.GetTotalAllocatedBytes(true); }
            };
            RenderingServer.FramePostDraw += Capture;
        };
        try { Engine.Run(window); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldPool); }
        void Capture()
        {
            sawConcurrent |= a.ThreadID != b.ThreadID;
            if (frame is >= 64 and < 128) { ownerBytes += GC.GetAllocatedBytesForCurrentThread() - ownerStart; allBytes += GC.GetTotalAllocatedBytes(true) - allStart; measured++; }
            if (frame is >= 4 and <= 7)
            {
                using var pixels = target.GetImage()!;
                var expected = frame switch { 4 => Colors.Red, 5 => Colors.Blue, 6 => Colors.Green, _ => (frame & 1) == 0 ? Colors.Red : Colors.Green };
                Pixel(pixels, 8, expected); Pixel(pixels, 40, Colors.Blue);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder)
                { Directory.CreateDirectory(folder); pixels.SavePNG(System.IO.Path.Combine(folder, $"group-modulation-{frame}.png")); }
            }
            if (frame == 4) { first.Hide(); phase = 1; }
            else if (frame == 5) { second.Hide(); first.Show(); phase = 2; }
            else if (frame == 6) { window.World = alternate; phase = 3; }
            if (frame == 128)
            {
                Check(measured == 64 && ownerBytes == 0 && allBytes == 0, $"Native grouped modulation frames allocated {ownerBytes}/{allBytes} B over {measured} frames");
                point.Position = body.Position;
                Check(alternate.PhysicsBackend == backend && alternate.PhysicsFallbackReason is null && alternate.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == originalBody && body.InstanceID == originalID,
                    "Real native modulation preserves selected physics and identity after world replacement");
                Check(warnings > 0 && sawConcurrent && first.GetCanvas() == alternate.Canvas && layer.GetCanvas() != alternate.Canvas, $"Native host executes actual group warnings and isolated canvas identities: warnings={warnings}, concurrent={sawConcurrent}, canvas={first.GetCanvas()}, expected={alternate.Canvas}, layer={layer.GetCanvas()}");
                Console.WriteLine($"Native group modulation {backend}/{(threaded ? "separate" : "owner")}/{renderer}: four pixel phases, promotion/world replacement and 64 process/render frames at {ownerBytes}/{allBytes} B passed.");
                RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
        }
        static void Pixel(Image pixels, int x, Color expected)
        {
            var actual = pixels.GetPixel(x, 8);
            Check(MathF.Abs(actual.R - expected.R) < .012f && MathF.Abs(actual.G - expected.G) < .012f && MathF.Abs(actual.B - expected.B) < .012f, $"Grouped tint pixel {x}: expected {expected}, actual {actual}");
        }
    }
}
