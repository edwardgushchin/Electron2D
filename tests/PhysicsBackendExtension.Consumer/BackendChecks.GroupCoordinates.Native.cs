using Electron2D;

public static partial class BackendChecks
{
    public static void RunNativeGroupCoordinates(PhysicsServer.Backend backend, bool threaded, string renderer)
    {
        var oldPool = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        using var world = new World(backend); using var shape = new CircleShape { Radius = 2 };
        using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { World = world, Size = new(128, 96), ContentScaleSize = new(64, 48), ContentScaleMode = ContentScaleMode.Viewport, ContentScaleAspect = ContentScaleAspect.Keep, CanvasTransform = new(0, new(2, 3)), Title = "Grouped canvas coordinates" };
        var a = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var b = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var body = new RigidBody { Position = new(8, 8), GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        body.AddChild(new CollisionShape { Shape = shape }); var red = new Sprite { Texture = texture, Centered = false, Modulate = Colors.Red }; body.AddChild(red); a.AddChild(body);
        var layer = new CanvasLayer { Offset = new(32, 0) }; var blue = new Sprite { Texture = texture, Centered = false, Position = new(4, 4), Modulate = Colors.Blue }; layer.AddChild(blue); b.AddChild(layer); window.AddChild(a); window.AddChild(b);
        using var together = new Barrier(2); using var key = new InputEventKey(); using var target = window.GetTexture();
        var frame = 0; var pureScreen = false; var owner = Environment.CurrentManagedThreadId; var concurrent = false; var measured = 0;
        long ownerStart = 0, allStart = 0, ownerBytes = 0, allBytes = 0; RID bodyRID = default;
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        a.Write = Read; b.Write = Read;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); bodyRID = body.GetRID();
            window.Tree!.ProcessFrameStarted += _ => { frame++; if (frame is >= 64 and < 128) { ownerStart = GC.GetAllocatedBytesForCurrentThread(); allStart = GC.GetTotalAllocatedBytes(true); } };
            RenderingServer.FramePostDraw += Capture;
        };
        try { Check(Engine.Run(window) == 0, "Native grouped-coordinate host exits successfully"); }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldPool); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThread); }
        void Read()
        {
            if (frame < 2 && !together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Native coordinate groups did not overlap");
            var canvas = new Transform(0, pureScreen ? new Vector2(5, 3) : new Vector2(2, 3));
            var viewport = window.GetFinalTransform(); var sample = new Vector2(1, 2); var global = canvas * body.GetGlobalTransform();
            Check(window.GetVisibleRect().Size == (pureScreen ? new Vector2(80, 60) : new Vector2(64, 48)) && body.GetViewportRect() == window.GetVisibleRect(), "Native visible bounds retain owner-observed content dimensions");
            Check(body.GetCanvasTransform() == canvas && (body.GetGlobalTransformWithCanvas() * sample).DistanceTo(global * sample) < .002f && (body.GetViewportTransform() * sample).DistanceTo(viewport * canvas * sample) < .002f, "Native worker coordinates use actual canvas and viewport publication");
            Check(blue.GetCanvasTransform() == layer.GetFinalTransform() && body.MakeCanvasPositionLocal(global * sample).DistanceTo(sample) < .002f && ReferenceEquals(body.MakeInputLocal(key), key), "Native layer and inverse/event projections preserve the logical contract");
            Check(window.GetScreenTransform() == viewport, "Viewport screen projection excludes native desktop placement");
            if (pureScreen) Check((body.GetScreenTransform() * sample).DistanceTo(global * sample) < .002f, "An embedding root permits pure screen projection from active groups");
            else if (frame < 2 && Environment.CurrentManagedThreadId != owner) Reject<InvalidOperationException>(() => body.GetScreenTransform());
        }
        void Capture()
        {
            concurrent |= a.ThreadID != b.ThreadID;
            if (frame is >= 64 and < 128) { ownerBytes += GC.GetAllocatedBytesForCurrentThread() - ownerStart; allBytes += GC.GetTotalAllocatedBytes(true) - allStart; measured++; }
            if (frame is 4 or 5)
            {
                using var pixels = target.GetImage()!;
                Check(pixels.Size == (frame == 4 ? new Vector2i(64, 48) : new Vector2i(80, 60)), "Actual viewport pixels follow changed content dimensions");
                Pixel(pixels, frame == 4 ? 13 : 16, 14, Colors.Red); Pixel(pixels, 40, 8, Colors.Blue);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); pixels.SavePNG(System.IO.Path.Combine(folder, $"group-coordinates-{frame}.png")); }
            }
            if (frame == 4) { window.ContentScaleSize = new(80, 60); window.CanvasTransform = new(0, new(5, 3)); window.GUIEmbedSubwindows = true; pureScreen = true; }
            if (frame == 128)
            {
                point.Position = body.Position;
                Check(concurrent && measured == 64 && ownerBytes == 0 && allBytes == 0, $"Native group coordinates allocated {ownerBytes}/{allBytes} B over {measured} frames; concurrent={concurrent}");
                Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null && body.Position.X > 8 && world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == bodyRID, "Native coordinates preserve selected physical motion/query identity");
                Console.WriteLine($"Native group coordinates {backend}/{(threaded ? "separate" : "owner")}/{renderer}: changed content/canvas, native/pure screen boundaries, actual layer/body pixels and 64 process/render frames at {ownerBytes}/{allBytes} B passed.");
                RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
        }
        static void Pixel(Image pixels, int x, int y, Color expected)
        {
            var actual = pixels.GetPixel(x, y); Check(MathF.Abs(actual.R - expected.R) < .012f && MathF.Abs(actual.G - expected.G) < .012f && MathF.Abs(actual.B - expected.B) < .012f, $"Grouped coordinate pixel {x},{y}: expected {expected}, actual {actual}");
        }
    }
}
