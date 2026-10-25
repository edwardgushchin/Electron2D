using Electron2D;

public static partial class BackendChecks
{
    private sealed class PositionWindow : Window
    {
        internal Action? Moved;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationWmPositionChanged) Moved?.Invoke(); }
    }
    private sealed class PositionBody : RigidBody
    {
        internal Action? Moved;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationWmPositionChanged) Moved?.Invoke(); }
    }

    public static void RunNativeWindowPosition(PhysicsServer.Backend backend, bool threaded, string renderer, bool unavailable)
    {
        var oldThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        using var world = new World(backend); using var shape = new CircleShape { Radius = 3 };
        using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); image.Fill(Colors.Red); using var texture = ImageTexture.CreateFromImage(image);
        var root = new PositionWindow { World = world, Size = new(200, 150), Title = "Native position notifications" };
        var child = new PositionWindow { World = world, Size = new(100, 80), Visible = false, ForceNative = true, Title = "Independent position notifications" };
        var body = new PositionBody { Position = new(16, 16), GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        body.AddChild(new CollisionShape { Shape = shape }); body.AddChild(new Sprite { Texture = texture, Centered = false }); root.AddChild(body);
        var childBody = new PositionBody { Position = new(12, 12), GravityScale = 0, Freeze = true }; childBody.AddChild(new CollisionShape { Shape = shape }); child.AddChild(childBody); root.AddChild(child);
        var calls = new List<string>(); var frame = 0; var phase = 0; long deadline = 0; var owner = Environment.CurrentManagedThreadId;
        Vector2 during = default; RID bodyRID = default; RID childRID = default; Vector2i target = default; PositionWindow? pending = null;
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2]; using var targetTexture = root.GetTexture();
        root.Moved = () => Observe("root", body); body.Moved = () => Observe("body", body);
        child.Moved = () => Observe("child", childBody); childBody.Moved = () => Observe("child-body", childBody);
        root.Ready += _ =>
        {
            child.Show(); bodyRID = body.GetRID(); childRID = childBody.GetRID(); RenderingServer.SetDefaultClearColor(Colors.Black);
            root.Tree!.ProcessFrameStarted += _ => frame++;
            RenderingServer.FramePostDraw += Capture;
        };
        try { Check(Engine.Run(root) == 0, "Native position host exits successfully"); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThread); }

        void Observe(string name, PositionBody physical)
        {
            Check(Environment.CurrentManagedThreadId == owner, "Native position notifications run on the scene owner");
            if (name is "root" or "child") during = physical.Position;
            else Check(physical.Position == during, "Window propagation preserves the physical pose");
            Check(physical.GetRID() == (ReferenceEquals(physical, body) ? bodyRID : childRID), "Native movement preserves the physical RID");
            calls.Add(name);
        }
        void Request(PositionWindow window, bool display)
        {
            calls.Clear(); pending = window; target = window.Position + new Vector2i(48, 32); var pose = body.Position; var childPose = childBody.Position;
            if (display) DisplayServer.WindowSetPosition(target, window.GetWindowID()); else window.Position = target;
            Check(calls.Count == 0 && body.Position == pose && childBody.Position == childPose, "A placement request neither fabricates notifications nor moves physical bodies");
            deadline = Environment.TickCount64 + 5000;
        }
        void Capture()
        {
            Check(phase is not (> 0 and < 5) || Environment.TickCount64 < deadline, $"Native position deadline: phase {phase}, position {pending?.Position}, target {target}, calls {string.Join(',', calls)}");
            if (frame < 16) return;
            if (phase == 0)
            {
                calls.Clear();
                if (unavailable)
                {
                    Reject<NotSupportedException>(() => _ = root.Position); Reject<NotSupportedException>(() => root.Position = new(200, 200));
                    Reject<NotSupportedException>(() => _ = child.Position); Reject<NotSupportedException>(() => DisplayServer.WindowSetPosition(new(220, 220), child.GetWindowID()));
                    phase = 5;
                }
                else { phase = 1; Request(root, false); }
                return;
            }
            if (phase < 5)
            {
                if (calls.Count == 0) return;
                var expected = phase <= 2 ? new[] { "root", "body" } : new[] { "child", "child-body" };
                Check(calls.SequenceEqual(expected) && pending!.Position == target && DisplayServer.WindowGetPosition(pending.GetWindowID()) == target, "Actual native movement commits the position and notifies only its containing subtree, parent-first");
                Console.WriteLine($"Native move {phase}: {pending!.GetWindowID()} -> {target}; {string.Join(',', calls)}");
                phase++;
                if (phase == 2) Request(root, true);
                else if (phase == 3) Request(child, false);
                else if (phase == 4) Request(child, true);
                else { calls.Clear(); root.Position = root.Position; DisplayServer.WindowSetPosition(child.Position, child.GetWindowID()); }
                return;
            }
            if (frame < 64) return;
            Check(calls.Count == 0, "Equal requests and unavailable global coordinates publish no position notifications");
            point.Position = body.Position;
            var count = world.DirectSpaceState.IntersectPoint(point, hits);
            Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null && body.Position.X > 16 && hits.Take(count).Any(hit => hit.ColliderRID == bodyRID), "Native movement preserves selected physical motion and public query identity");
            Check(root.GetScreenTransform() == root.GetFinalTransform(), "Viewport projection excludes desktop placement");
            using var pixels = targetTexture.GetImage()!; var pixel = pixels.GetPixel((int)body.Position.X + 3, (int)body.Position.Y + 3);
            Check(pixel.R > .98f && pixel.G < .02f && pixel.B < .02f, "The physical body retains actual rendered pixels after native movement");
            Console.WriteLine($"Native window position {backend}/{(threaded ? "separate" : "owner")}/{renderer}/{(unavailable ? "unavailable" : "X11")}: public root/child requests, containing notifications, physical identity/motion/query and pixels passed.");
            RenderingServer.FramePostDraw -= Capture; root.Tree!.Quit();
        }
    }
}
