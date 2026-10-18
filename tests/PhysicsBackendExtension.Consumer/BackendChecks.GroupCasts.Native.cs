using Electron2D;

public static partial class BackendChecks
{
    public static void RunNativeGroupCasts(PhysicsServer.Backend backend, bool threaded, string renderer)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var oldTicks = Engine.PhysicsTicksPerSecond; var oldFPS = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.PhysicsTicksPerSecond = 30; Engine.MaxFPS = 60;
        try
        {
            using var world = new World(backend); using var bodyShape = new CircleShape { Radius = 3 }; using var queryShape = new CircleShape { Radius = 1 };
            using var window = new Window { Size = new(80, 72), World = world }; var writer = new NativeTransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
            var ray = new RayCast { Position = new(20, 20), TargetPosition = new(30, 0), InternalPhysicsProcessEnabled = false };
            var cast = new ShapeCast { Position = new(20, 50), TargetPosition = new(30, 0), Shape = queryShape, InternalPhysicsProcessEnabled = false };
            writer.AddChild(ray); writer.AddChild(cast); window.AddChild(writer);
            var targets = new StaticBody[2];
            for (var i = 0; i < 2; i++) { targets[i] = new() { Position = new(50, 20 + 30 * i) }; targets[i].AddChild(new CollisionShape { Shape = bodyShape }); window.AddChild(targets[i]); }
            var owner = Environment.CurrentManagedThreadId; var phase = 0; var worker = 0; var seen = 0; var frames = 0;
            writer.Write = () =>
            {
                worker = Environment.CurrentManagedThreadId;
                if (ray.IsColliding()) Check(ray.GetColliderRID() == targets[0].GetRID(), "Native worker observes the held ray identity");
                if (cast.GetCollisionCount() != 0) Check(cast.GetColliderRID(0) == targets[1].GetRID(), "Native worker observes held cast identity");
                phase++; var hit = (phase & 1) != 0; ray.TargetPosition = cast.TargetPosition = hit ? new(30, 0) : default;
                ray.Enabled = cast.Enabled = hit; ray.InternalPhysicsProcessEnabled = cast.InternalPhysicsProcessEnabled = false;
            };
            window.Ready += _ =>
            {
                window.Tree!.DebugCollisionsHint = true; RenderingServer.SetDefaultClearColor(Colors.Black);
                ray.InternalPhysicsProcessEnabled = cast.InternalPhysicsProcessEnabled = false;
                Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "The exact requested renderer is active");
                ray.ForceRaycastUpdate(); cast.ForceShapecastUpdate();
                RenderingServer.FramePreDraw += () => { ray.ForceRaycastUpdate(); cast.ForceShapecastUpdate(); };
                RenderingServer.FramePostDraw += () =>
                {
                    if (++frames > 180) throw new InvalidOperationException("No native grouped cast states"); if (phase == 0) return;
                    var hit = (phase & 1) != 0;
                    Check(ray.IsColliding() == hit && (cast.GetCollisionCount() != 0) == hit, "Owner queries consume worker metadata before presentation");
                    using var pixels = window.GetTexture().GetImage()!; Pixel(new(30, 20)); Pixel(new(30, 50));
                    var bit = hit ? 1 : 2;
                    if ((seen & bit) == 0 && Environment.GetEnvironmentVariable("ELECTRON2D_GROUP_CASTS_CAPTURE") is { } path) pixels.SavePNG(System.IO.Path.Combine(path, bit + ".png"));
                    seen |= bit; if (seen == 3) window.Tree!.Quit();
                    void Pixel(Vector2 at)
                    {
                        var color = pixels.GetPixel((int)(at.X * pixels.Width / window.Size.X), (int)(at.Y * pixels.Height / window.Size.Y));
                        Check(hit ? color.R > .7f && color.G < .2f && color.B < .2f : color.R < .05f && color.G < .05f && color.B < .05f, $"Grouped cast diagnostic pixel {at}: {color}, hit {hit}");
                    }
                };
            };
            Check(Engine.Run(window) == 0 && seen == 3 && worker != owner && window.IsDisposed, "Native grouped cast presentation and lifetime complete");
            Console.WriteLine($"Native group casts {world.PhysicsBackendName}/{renderer}/{(threaded ? "separate" : "owner")}: worker cache reads/options, owner sampling and hit/miss diagnostic pixels passed.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback); Engine.PhysicsTicksPerSecond = oldTicks; Engine.MaxFPS = oldFPS;
        }
    }
}
