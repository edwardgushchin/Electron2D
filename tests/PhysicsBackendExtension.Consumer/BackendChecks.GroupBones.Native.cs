using Electron2D;

public static partial class BackendChecks
{
    private sealed class GroupBoneInk : Entity
    {
        protected override void OnDraw() => DrawCircle(Vector2.Zero, 3, Colors.Red);
    }
    public static void RunNativeGroupBones(PhysicsServer.Backend backend, bool threaded, string renderer)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var oldTicks = Engine.PhysicsTicksPerSecond; var oldFPS = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.PhysicsTicksPerSecond = 30; Engine.MaxFPS = 60;
        try
        {
            using var world = new World(backend); using var shape = new CircleShape { Radius = 2 }; using var window = new Window { Size = new(128, 64), World = world };
            var scene = new GroupBoneScene(window, shape, true, true, false); scene.A.Position = new(24, 24); scene.Parent.AddChild(new GroupBoneInk());
            scene.Parent.Reconfigure = scene.Child.Reconfigure = false;
            var owner = Environment.CurrentManagedThreadId; var seen = 0; var frames = 0; ulong tick = 0;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "The exact requested native renderer is active");
                RenderingServer.FramePostDraw += () =>
                {
                    if (++frames > 180) throw new InvalidOperationException("No native grouped bone states");
                    var current = PhysicsServer.SpaceGetTick(world.Space); if (current == tick) return; tick = current;
                    scene.Aligned(); Check(scene.Parent.GlobalPosition.DistanceTo(scene.A.GlobalPosition) < .001f && scene.Parent.IsSimulatingPhysics(), "Native group following reaches actual body presentation");
                    using var pixels = window.GetTexture().GetImage()!; var at = scene.Parent.GlobalPosition;
                    var color = pixels.GetPixel((int)(at.X * pixels.Width / window.Size.X), (int)(at.Y * pixels.Height / window.Size.Y));
                    Check(color.R > .9f && color.G < .1f && color.B < .1f, "Native pixels follow the worker-authored physical bone");
                    var bit = at.X < 40 ? 1 : 2;
                    if ((seen & bit) == 0 && Environment.GetEnvironmentVariable("ELECTRON2D_GROUP_BONES_CAPTURE") is { } path) pixels.SavePNG(System.IO.Path.Combine(path, bit + ".png"));
                    seen |= bit; if (seen == 3) window.Tree!.Quit(); else scene.A.Position = new(54, 24);
                };
            };
            Check(Engine.Run(window) == 0 && seen == 3 && scene.Parent.ThreadID != owner && window.IsDisposed && world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null,
                "Native grouped bones retain selected physics, presentation and lifetime");
            Console.WriteLine($"Native group bones {world.PhysicsBackendName}/{renderer}/{threaded}: real worker following and changed physical-body pixels passed.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback); Engine.PhysicsTicksPerSecond = oldTicks; Engine.MaxFPS = oldFPS;
        }
    }
}
