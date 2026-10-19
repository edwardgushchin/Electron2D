using System.Diagnostics;
using Electron2D;
using static PhysicsDebugTests;
using SDL = SDL3.SDL;

internal static class PhysicsVelocityEditNativeTests
{
    internal static void Run()
    {
        var previousMethod = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var previousFPS = Engine.MaxFPS; var previousTicks = Engine.PhysicsTicksPerSecond;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); Engine.MaxFPS = 0; Engine.PhysicsTicksPerSecond = 60;
            Verify(PhysicsServer.Backend.CPU); Verify(PhysicsServer.Backend.GPU);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previousMethod); Engine.MaxFPS = previousFPS; Engine.PhysicsTicksPerSecond = previousTicks; }
    }
    private static void Verify(PhysicsServer.Backend backend)
    {
        const int count = 256;
        using var shape = new CircleShape { Radius = 5 }; using var world = new World(backend);
        var window = new Window { Title = "Physics component edit throughput", Size = new(640, 520), World = world };
        var ids = new RID[count]; var poses = new Transform[count];
        for (var i = 0; i < count; i++)
        {
            var position = new Vector2(20 + i % 16 * 39, 12 + i / 16 * 31);
            var fixedBody = new StaticBody { Name = "Fixed" + i, Position = position };
            fixedBody.AddChild(new CollisionShape { Shape = shape }); fixedBody.AddChild(new Disk(new(.6f, .7f, .7f))); window.AddChild(fixedBody);
            var body = new RigidBody { Name = "Moving" + i, GravityScale = 0, CanSleep = false, Position = position + new Vector2(0, 8) };
            body.AddChild(new CollisionShape { Shape = shape }); body.AddChild(new Disk(new(.9f, .5f, .7f))); window.AddChild(body);
            ids[i] = body.GetRID(); poses[i] = body.Transform;
        }
        var times = new double[16384]; var frames = 0; long began = 0, previous = 0, allocated = 0;
        ulong? warmStartTick = null;
        ulong startTick = 0, measuredTicks = 0; double elapsed = 0, waits = 0; long up = 0, down = 0, submits = 0;
        var finished = false; var vsync = DisplayServer.VSyncMode.Enabled;
        window.Ready += _ =>
        {
            DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled); vsync = DisplayServer.WindowGetVSyncMode();
            RenderingServer.SetDefaultClearColor(new(.12f, .09f, .14f));
            var tree = window.Tree!; var gpu = PhysicsServer.Service.GetSceneSpace(world.Space).GPUStore;
            Check(window.PhysicsObjectPicking, "The native allocation interval retains physics pointer picking");
            var windows = SDL.GetWindows(out var windowCount); Check(windowCount == 1, "One native allocation-test window");
            var mouse = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = SDL.GetWindowID(windows![0]), X = 20, Y = 12 } };
            Check(SDL.PushEvent(ref mouse), "Queue native motion to retain passive picking during measurement");
            tree.PhysicsFrameStarted += _ =>
            {
                for (var i = 0; i < ids.Length; i++)
                { PhysicsServer.BodySetTransform(ids[i], poses[i]); PhysicsServer.BodySetLinearVelocity(ids[i], Vector2.Zero); PhysicsServer.BodySetAngularVelocity(ids[i], 0); }
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (warmStartTick is null)
                {
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false); GC.WaitForPendingFinalizers();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false); GC.WaitForPendingFinalizers();
                    warmStartTick = tree.PhysicsFrameCount;
                }
                if (tree.PhysicsFrameCount - warmStartTick.Value < 96) return;
                var now = Stopwatch.GetTimestamp();
                if (began == 0)
                {
                    Check(window.Picking?.MouseKnown == true, "Passive picking is warm before the allocation interval");
                    began = previous = now; startTick = tree.PhysicsFrameCount; allocated = GC.GetAllocatedBytesForCurrentThread();
                    up = gpu?.UploadBytes ?? 0; down = gpu?.ReadbackBytes ?? 0; waits = gpu?.WaitMS ?? 0; submits = gpu?.SubmissionCount ?? 0;
                    return;
                }
                Check(frames < times.Length, "Frame sample capacity exceeded");
                times[frames++] = Stopwatch.GetElapsedTime(previous, now).TotalMilliseconds; previous = now;
                elapsed = Stopwatch.GetElapsedTime(began, now).TotalSeconds;
                if (elapsed < 4) return;
                allocated = GC.GetAllocatedBytesForCurrentThread() - allocated; measuredTicks = tree.PhysicsFrameCount - startTick;
                up = (gpu?.UploadBytes ?? 0) - up; down = (gpu?.ReadbackBytes ?? 0) - down; waits = (gpu?.WaitMS ?? 0) - waits; submits = (gpu?.SubmissionCount ?? 0) - submits;
                using var pixels = window.GetTexture().GetImage()!;
                Check(pixels.GetPixel(20, 12).G > .5f, "The real window renders the fixed bodies");
                pixels.SavePNG($"/tmp/e2d-velocity-window-{backend}.png");
                finished = true; tree.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && finished && measuredTicks > 0, "Window physics/render sequence completed");
        Check(allocated == 0, $"Warm window loop allocated {allocated} managed bytes across {measuredTicks} physics ticks ({backend})");
        Array.Sort(times, 0, frames);
        Console.WriteLine($"Component edits real window {backend}: 640x520 GPU renderer, vsync={vsync}, MaxFPS=0, 512 circle bodies, 256 pose/linear/angular edits per physics tick, requested 60 Hz; after 96 physics warmups, {elapsed:F3} s, {frames} frames, {measuredTicks} physics ticks; FPS={frames / elapsed:F2}, actual ticks/s={measuredTicks / elapsed:F2}, frame p50/p95/p99={times[frames / 2]:F4}/{times[(int)(frames * .95)]:F4}/{times[(int)(frames * .99)]:F4} ms; owner managed={allocated} B, GPU up/down={up}/{down} B, submissions={submits}, wait={waits:F3} ms.");
    }
    private sealed class Disk(Color color) : Entity
    { protected override void OnDraw() => DrawCircle(Vector2.Zero, 5, color); }
}
