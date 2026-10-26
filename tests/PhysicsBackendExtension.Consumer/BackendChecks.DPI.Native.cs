using Electron2D;

public static partial class BackendChecks
{
    private sealed class DPIWindow : Window
    {
        internal Action? Changed;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationWmDpiChange) Changed?.Invoke(); }
    }
    private sealed class DPIBody : RigidBody
    {
        internal Action? Changed;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationWmDpiChange) Changed?.Invoke(); }
    }

    public static void RunNativeDPI(PhysicsServer.Backend backend, bool threaded, string renderer)
    {
        var folder = Environment.GetEnvironmentVariable("ELECTRON2D_DPI_CONTROL_DIR") ?? throw new InvalidOperationException("A fresh compositor-control directory is required.");
        var oldThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded); ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        using var world = new World(backend); using var shape = new CircleShape { Radius = 3 };
        using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        var root = new DPIWindow { World = world, Size = new(320, 240), Title = "Root DPI notification" };
        var child = new DPIWindow { World = world, Size = new(128, 96), ForceNative = true, Visible = false, Title = "Child DPI notification" };
        var body = new DPIBody { Position = new(16, 16), GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        body.AddChild(new CollisionShape { Shape = shape }); body.AddChild(new Sprite { Texture = texture, Centered = false, Modulate = Colors.Red }); root.AddChild(body);
        var childBody = new DPIBody { Position = new(100, 60), GravityScale = 0, Freeze = true };
        childBody.AddChild(new CollisionShape { Shape = shape }); childBody.AddChild(new Sprite { Texture = texture, Centered = false, Modulate = Colors.Green }); child.AddChild(childBody); root.AddChild(child);
        using var rootTarget = root.GetTexture(); using var childTarget = child.GetTexture(); using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2];
        var rootCalls = new List<string>(); var childCalls = new List<string>(); var scales = new List<float>();
        var owner = Environment.CurrentManagedThreadId; var frame = 0; var phase = 0; var observedFrame = 0; var armed = false; var rootSignals = 0; var childSignals = 0;
        Vector2 rootPose = default, childPose = default; RID bodyRID = default, childRID = default;
        root.Changed = () => Notify(root, body, rootCalls, true); body.Changed = () => Notify(root, body, rootCalls, false);
        child.Changed = () => Notify(child, childBody, childCalls, true); childBody.Changed = () => Notify(child, childBody, childCalls, false);
        root.DpiChanged += () => Signal(rootCalls, true); child.DpiChanged += () => Signal(childCalls, false);
        root.Ready += _ =>
        {
            child.Show(); bodyRID = body.GetRID(); childRID = childBody.GetRID(); RenderingServer.SetDefaultClearColor(Colors.Black);
            root.Tree!.ProcessFrameStarted += _ => frame++; RenderingServer.FramePostDraw += Capture;
        };
        try { Check(Engine.Run(root) == 0, "Native DPI host exits successfully"); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThread); }
        void Notify(DPIWindow window, DPIBody physical, List<string> calls, bool isWindow)
        {
            if (!armed) return;
            Check(Environment.CurrentManagedThreadId == owner && physical.GetRID() == (ReferenceEquals(window, root) ? bodyRID : childRID), "DPI delivery retains owner and physical identity");
            if (isWindow)
            {
                Check(calls.Count == 0, "Each native window starts its own notification scope");
                if (ReferenceEquals(window, root)) rootPose = physical.Position; else childPose = physical.Position;
                var size = window.Size;
                Check(window.GetVisibleRect().Size == new Vector2(size.X, size.Y), "DPI notification observes the committed viewport pixel dimensions");
                calls.Add("window");
            }
            else
            {
                Check(calls.SequenceEqual(["window"]) && physical.Position == (ReferenceEquals(window, root) ? rootPose : childPose), "DPI propagation is parent-first and does not move physical bodies");
                calls.Add("body");
            }
        }
        void Signal(List<string> calls, bool main)
        {
            if (!armed) return;
            Check(calls.SequenceEqual(["window", "body"]), "DPI signal follows only its own containing physical subtree"); calls.Clear();
            var scale = DisplayServer.ScreenGetScale();
            Check(MathF.Abs(scale - (phase == 1 ? 1.25f : 1f)) < .0001f, "DPI observers see the compositor's committed window scale");
            if (main) { rootSignals++; scales.Add(scale); } else childSignals++;
            observedFrame = frame; Console.WriteLine($"Native DPI signal {(main ? "root" : "child")}: scale {scale}, frame {frame}, counts {rootSignals}/{childSignals}.");
        }
        void Capture()
        {
            if (frame < 16) return;
            if (phase == 0)
            {
                Check(MathF.Abs(DisplayServer.ScreenGetScale() - 1) < .0001f, "Controlled compositor starts at scale one");
                armed = true; phase = 1; File.WriteAllText(System.IO.Path.Combine(folder, "ready-0"), "ready"); return;
            }
            Check(rootSignals <= phase && childSignals <= phase, "A native scale transition emits once per containing window");
            if (rootSignals != phase || childSignals != phase || frame < observedFrame + 8) return;
            Verify(rootTarget, root, body, Colors.Red); Verify(childTarget, child, childBody, Colors.Green);
            point.Position = body.Position; var count = world.DirectSpaceState.IntersectPoint(point, hits);
            Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null && body.Position.X > 16 && hits.Take(count).Any(hit => hit.ColliderRID == bodyRID), "DPI changes preserve selected physical motion and public query identity");
            Console.WriteLine($"Native DPI phase {phase}: scale {scales[^1]}, root {root.Size}, child {child.Size}, signals {rootSignals}/{childSignals}, physical identity and pixels passed.");
            if (phase == 1) { phase = 2; File.WriteAllText(System.IO.Path.Combine(folder, "ready-1"), "ready"); }
            else { Check(scales.SequenceEqual([1.25f, 1f]), "The native scale round trip is complete"); RenderingServer.FramePostDraw -= Capture; root.Tree!.Quit(); }
        }
        static void Verify(ViewportTexture target, Window window, DPIBody physical, Color expected)
        {
            using var pixels = target.GetImage()!; var actual = pixels.GetPixel((int)physical.Position.X + 3, (int)physical.Position.Y + 3);
            Check(pixels.Size == window.Size && MathF.Abs(actual.R - expected.R) < .02f && MathF.Abs(actual.G - expected.G) < .02f && MathF.Abs(actual.B - expected.B) < .02f, "Actual rendered physical pixels follow native DPI dimensions");
        }
    }
}
