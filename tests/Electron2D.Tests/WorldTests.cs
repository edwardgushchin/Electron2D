using Electron2D;

internal static class WorldTests
{
    internal static void Run()
    {
        using var explicitWorld = new World(); using var copy = (World)explicitWorld.Duplicate();
        Check(explicitWorld.Canvas.IsValid() && explicitWorld.Canvas == copy.Canvas && explicitWorld.Space == copy.Space, "Duplication borrows complete live identities without copying solver state.");
        using var a = new SubViewport { Name = "A", Size = new(32, 32) }; using var b = new SubViewport { Name = "B", Size = new(32, 32) };
        Check(a.World!.Canvas != b.World!.Canvas && a.FindWorld() == a.World, "Detached viewports have independent default worlds.");
        using var shape = new CircleShape { Radius = 3 }; var first = new StaticBody { Name = "First", Position = new(8, 8) }; first.AddChild(new CollisionShape { Shape = shape }); a.AddChild(first);
        using var secondShape = new CircleShape { Radius = 3 };
        var second = new GuardBody { Name = "Second", Position = new(16, 8), GravityScale = 0, LinearVelocity = new(30, 0) }; var secondCollision = new CollisionShape { Shape = secondShape }; second.AddChild(secondCollision); b.AddChild(second);
        var sensor = new Area { Name = "Sensor", Position = new(8, 8) }; sensor.AddChild(new CollisionShape { Shape = shape }); b.AddChild(sensor);
        var root = new Node(); root.AddChild(a); root.AddChild(b); using var tree = new SceneTree(root);
        using var point = new PhysicsPointQueryParameters { Position = new(8, 8), CollideWithBodies = true, CollideWithAreas = false };
        var rid = second.GetRID(); var source = first.GetWorld()!; var original = second.GetWorld()!;
        Check(source.Space != original.Space && source.DirectSpaceState.IntersectPoint(point).Any(r => r.ColliderRID == first.GetRID()) && !original.DirectSpaceState.IntersectPoint(point).Any(r => r.ColliderRID == first.GetRID()), "Independent viewports isolate physics queries.");
        b.World = a.World; Check(second.WorldChanges == 1, "World-change notification follows committed binding."); Check(ReferenceEquals(second.GetWorld(), first.GetWorld()) && second.GetRID() == rid && sensor.Space == first.Space, "World sharing moves bodies/areas while preserving collision identities.");
        second.Integration = () => Reject<InvalidOperationException>(() => b.World = explicitWorld);
        var before = second.Position; tree.PhysicsFrame(1d / 60); second.Integration = null; Check(Math.Abs(second.Position.X - before.X - .5f) < .02f, "Shared world steps exactly once per tree tick.");
        var joint = new PinJoint { Name = "Link", NodeA = "../Second", NodeB = "../../A/First" }; b.AddChild(joint); tree.PhysicsFrame(1d / 60); var jointRID = joint.GetRID(); Check(PhysicsServer.JointGetType(jointRID) == PhysicsServer.JointType.Pin, "Joint connects inside the shared physics world.");
        sensor.Position = second.Position; tree.PhysicsFrame(1d / 60);
        Action<Entity> failedExit = body => { if (ReferenceEquals(body, second)) throw new InvalidOperationException("exit observer"); }; sensor.BodyExited += failedExit;
        Reject<AggregateException>(() => b.World = explicitWorld); sensor.BodyExited -= failedExit;
        Check(second.Space == explicitWorld.Runtime.Space && sensor.Space == explicitWorld.Runtime.Space, "Committed world movement completes despite an exit observer failure.");
        tree.PhysicsFrame(1d / 60); Check(joint.GetRID() == jointRID && PhysicsServer.JointGetType(jointRID) == PhysicsServer.JointType.Empty, "Cross-world joint disconnects without changing identity.");
        b.World = explicitWorld; Check(second.GetWorld()!.Space == explicitWorld.Space && second.GetRID() == rid && first.GetWorld()!.Space == source.Space, "Explicit world replacement preserves unrelated world and RID.");
        b.World = null; Check(b.World!.Canvas != explicitWorld.Canvas && b.World.Space != source.Space, "Null creates a fresh independent default world.");
        sensor.Position = second.Position; tree.PhysicsFrame(1d / 60);
        Action<Entity> brokenGeometry = body => { if (ReferenceEquals(body, second)) second.Scale = new(2, 1); }; sensor.BodyExited += brokenGeometry;
        Reject<AggregateException>(() => b.World = explicitWorld); sensor.BodyExited -= brokenGeometry; Check(second.Space is null, "Invalid attach geometry reports committed membership failure.");
        second.Scale = Vector2.One; tree.PhysicsFrame(1d / 60); Check(second.Space == explicitWorld.Runtime.Space && second.GetRID() == rid, "A corrected failed transfer recovers on the next physics tick.");
        source.Dispose(); var reopened = first.GetWorld()!; Check(reopened.Space == first.Space!.RID && reopened.Canvas == a.FindWorld()!.Canvas && !ReferenceEquals(reopened, source), "Disposing a bound wrapper preserves runtime identities.");
        Reject<InvalidOperationException>(() => Task.Run(() => b.World = explicitWorld).GetAwaiter().GetResult());
        using var other = new SubViewport(); using var otherTree = new SceneTree(other); Reject<InvalidOperationException>(() => other.World = a.World);
        using var disposed = new World(); disposed.Dispose(); var current = b.World; Reject<ObjectDisposedException>(() => b.World = disposed); Check(ReferenceEquals(current, b.World), "Invalid assignment preserves previous world.");
        for (var i = 0; i < 64; i++) { _ = first.GetWorld()!.Canvas; _ = b.FindWorld(); }
        var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 2000; i++) { _ = first.GetWorld()!.Canvas; _ = b.FindWorld(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "2000 prepared world/canvas lookups allocate zero managed bytes.");
        var canvas = reopened.Canvas; var space = reopened.Space; using var borrowedDuplicate = (World)reopened.Duplicate(); tree.Dispose(); Check(RenderingCanvasRegistry.ResolveOrNull(canvas) is null, "Scene teardown expires default world canvas, including duplicate wrappers."); Reject<ArgumentException>(() => PhysicsServer.SpaceGetDirectState(space));
        Check(RenderingCanvasRegistry.ResolveOrNull(explicitWorld.Canvas) is not null && copy.Space == explicitWorld.Space, "Caller-owned world survives tree teardown.");
        var retryRoot = new SubViewport(); var retryBody = new StaticBody(); retryBody.AddChild(new CollisionShape { Shape = shape }); retryRoot.AddChild(retryBody); Action<Node> badReady = _ => throw new InvalidOperationException("ready observer"); retryBody.Ready += badReady; Reject<AggregateException>(() => new SceneTree(retryRoot)); retryBody.Ready -= badReady; using (var retry = new SceneTree(retryRoot)) Check(retryBody.GetWorld()!.Space.IsValid(), "Failed activation releases its world driver and permits retry.");
        Console.WriteLine("World identity/sharing/replacement, physics isolation/once-per-tick and teardown passed.");
    }
    private sealed class GuardBody : RigidBody { internal Action? Integration; internal int WorldChanges; protected override void IntegrateForces(PhysicsDirectBodyState state) => Integration?.Invoke(); protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationWorldChanged) { Check(Space?.RID == GetWorld()!.Space, "Notification observes final physics membership."); WorldChanges++; } } }
    private sealed class Paint(Color color, Rect2 rect) : Entity { internal int WorldChanges; protected override void OnDraw() => DrawRect(rect, color); protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationWorldChanged) WorldChanges++; } }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        var window = new Window { Size = new(80, 40) }; var sub = new SubViewport { Name = "Shared", Size = new(64, 32), RenderTargetUpdateMode = ViewportUpdateMode.Always }; window.AddChild(sub);
        var red = new Paint(Colors.Red, new(4, 4, 8, 8)); var blue = new Paint(Colors.Blue, new(20, 4, 8, 8)); window.AddChild(red); sub.AddChild(blue);
        var driver = ""; var phase = -4; RID original = default, shared = default; World? defaultWorld = null;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested native backend."); driver = RenderingServer.GetCurrentRenderingDriverName(); Check(driver != "software", "Native host exercises a hardware renderer."); RenderingServer.SetDefaultClearColor(Colors.Black); original = sub.World!.Canvas; defaultWorld = window.World; shared = window.World!.Canvas;
            Reject<InvalidOperationException>(() => RenderingServer.FreeRID(shared));
            RenderingServer.FramePostDraw += () =>
            {
                using var rootImage = window.GetTexture().GetImage()!; using var subImage = sub.GetTexture().GetImage()!;
                if (phase < 0) { phase++; return; }
                switch (phase)
                {
                    case 0: Pixel(rootImage, 8, 8, Colors.Red); Pixel(rootImage, 24, 8, Colors.Black); Pixel(subImage, 8, 8, Colors.Black); Pixel(subImage, 24, 8, Colors.Blue); sub.World = window.World; break;
                    case 1: Pixel(rootImage, 24, 8, Colors.Blue); Pixel(subImage, 8, 8, Colors.Red); Pixel(subImage, 24, 8, Colors.Blue); RenderingServer.ViewportSetCanvasTransform(sub.GetViewportRID(), shared, new(0, new(4, 0))); break;
                    case 2: Pixel(rootImage, 8, 8, Colors.Red); Pixel(subImage, 12, 8, Colors.Red); Pixel(subImage, 28, 8, Colors.Blue); sub.CanvasTransform = Transform.Identity; Check(sub.DefaultCanvasRuntime.Attachments[sub.GetViewportRID()].Transform is null, "Source canvas-transform setter republishes selected world view."); sub.World = null; break;
                    case 3: Pixel(rootImage, 24, 8, Colors.Black); Pixel(subImage, 8, 8, Colors.Black); Pixel(subImage, 24, 8, Colors.Blue); Check(sub.World!.Canvas != original && sub.World.Canvas != shared, "Replacement changes only selected canvas."); sub.World = window.World; RenderingServer.ViewportSetCanvasTransform(sub.GetViewportRID(), shared, Transform.Identity); break;
                    default: Check(blue.WorldChanges == 3 && red.WorldChanges == 0, "World-change delivery stays within the changed viewport."); Pixel(subImage, 8, 8, Colors.Red); Pixel(rootImage, 24, 8, Colors.Blue); if (Environment.GetEnvironmentVariable("ELECTRON2D_WORLD_SNAPSHOT") is { } path) rootImage.SavePNG(path); window.Tree!.Quit(); break;
                }
                phase++;
            };
        };
        Engine.Run(window); Check(window.IsDisposed && RenderingServer.Service is null && RenderingCanvasRegistry.ResolveOrNull(shared) is null, "Native teardown releases shared default world and canvas ownership."); Reject<ObjectDisposedException>(() => defaultWorld!.Canvas.ToString());
        Console.WriteLine($"World shared/independent native canvas pixels/replacement/cleanup passed: {backend}/{driver}; {phase} phases.");
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var p = image.GetPixel(x, y); Check(Math.Abs(p.R - expected.R) < .01f && Math.Abs(p.G - expected.G) < .01f && Math.Abs(p.B - expected.B) < .01f, $"Pixel {x},{y}: expected {expected}, got {p}."); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
