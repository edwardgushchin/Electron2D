using Electron2D;

public static partial class BackendChecks
{
    public static void RunNativeGroupSkeleton(PhysicsServer.Backend backend, bool threaded, string renderer)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var oldTicks = Engine.PhysicsTicksPerSecond; var oldFPS = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.PhysicsTicksPerSecond = 30; Engine.MaxFPS = 60;
        try
        {
            using var world = new World(backend); using var window = new Window { Size = new(100, 100), World = world }; using var shape = new CircleShape { Radius = 1 };
            using var scene = new GroupSkeletonScene(window, shape, "two-bone", ProcessPhase.Idle, true, origin: new(20, 40)); using var observe = new GroupPoseModification();
            var owner = Environment.CurrentManagedThreadId; var worker = 0; observe.Apply = () => worker = Environment.CurrentManagedThreadId; scene.Stack.AddModification(observe);
            var skin = new Electron2D.Polygon { Position = scene.Rig.Position, Skeleton = "../Rig", Color = new(.1f, .7f, 1), Vertices = [new(0, -5), new(30, -5), new(60, -5), new(60, 5), new(30, 5), new(0, 5)] };
            skin.AddBone("A", [1, 1, 0, 0, 1, 1]); skin.AddBone("A/B", [0, 0, 1, 1, 0, 0]); window.AddChild(skin);
            var frames = 0; var seen = 0;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "The exact requested native renderer is active");
                RenderingServer.FramePostDraw += () =>
                {
                    if (++frames > 180) throw new InvalidOperationException("No native grouped skeletal states");
                    if (PhysicsServer.SpaceGetTick(world.Space) == 0 || scene.Tip.GlobalPosition.DistanceTo(scene.Target.GlobalPosition) > .002f) return;
                    var down = scene.Target.GlobalPosition.Y > scene.Rig.GlobalPosition.Y;
                    Check(worker != owner && scene.Body.GlobalTransform.IsEqualApprox(scene.A.GlobalTransform), "The worker solves the pose and its actual physical follower before drawing");
                    Check(PhysicsServer.BodyGetTransform(scene.Body.GetRID()).IsEqualApprox(scene.A.GlobalTransform), "Native physical state consumes the grouped bone pose");
                    using var pixels = window.GetTexture().GetImage()!; var live = pixels.GetPixel(48, down ? 68 : 12); var absent = pixels.GetPixel(48, down ? 12 : 68);
                    Check(live.B > .8f && live.G > .5f && absent.B < .1f, "Weighted skin pixels follow the real grouped solve");
                    var bit = down ? 1 : 2;
                    if ((seen & bit) == 0 && Environment.GetEnvironmentVariable("ELECTRON2D_GROUP_SKELETON_CAPTURE") is { } path) pixels.SavePNG(System.IO.Path.Combine(path, bit + ".png"));
                    seen |= bit; if (seen == 3) window.Tree!.Quit(); else scene.Target.GlobalPosition = scene.Rig.GlobalPosition + new Vector2(30, -30);
                };
            };
            Check(Engine.Run(window) == 0 && seen == 3 && window.IsDisposed && world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null,
                "Native grouped skin, selected physics and lifetime complete");
            Console.WriteLine($"Native group skeleton {world.PhysicsBackendName}/{renderer}/{threaded}: real worker IK, physical following and changed weighted skin pixels passed.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback); Engine.PhysicsTicksPerSecond = oldTicks; Engine.MaxFPS = oldFPS;
        }
    }
}
