using Electron2D;

public static partial class BackendChecks
{
    public static void RunNativeGroupInterpolation(PhysicsServer.Backend backend, bool threaded, string renderer)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var oldInterpolation = ProjectSettings.Get(ProjectSettings.PhysicsInterpolation); var oldTicks = Engine.PhysicsTicksPerSecond; var oldFPS = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
        Engine.PhysicsTicksPerSecond = 30; Engine.MaxFPS = 120;
        try
        {
            using var world = new World(backend); using var shape = new CircleShape { Radius = 2 };
            using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
            using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = [new(-3, -3), new(3, -3), new(3, 3), new(-3, 3)], Indices = [0, 1, 2, 0, 2, 3] });
            using var instances = new MultiMesh { Mesh = mesh, InstanceCount = 1 };
            using var window = new Window { Size = new(128, 80), World = world };
            var writer = new NativeTransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
            var sprite = new Sprite { Texture = texture }; var multi = new MultiMeshInstance { MultiMesh = instances, Modulate = Colors.Red };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, CollisionMask = 0 }; body.AddChild(new CollisionShape { Shape = shape });
            var camera = new Camera { AnchorMode = AnchorMode.FixedTopLeft, LimitEnabled = false, ProcessCallback = ProcessPhase.Physics };
            writer.AddChild(sprite); writer.AddChild(multi); writer.AddChild(body); writer.AddChild(camera); window.AddChild(writer);
            var owner = Environment.CurrentManagedThreadId; var worker = 0; var phase = 0; var seen = 0; var frames = 0;
            writer.Write = () =>
            {
                worker = Environment.CurrentManagedThreadId; phase++;
                writer.PhysicsInterpolationMode = PhysicsInterpolationMode.Off; writer.PhysicsInterpolationMode = PhysicsInterpolationMode.On;
                var point = new Vector2((phase & 1) == 0 ? 30 : 80, 25);
                sprite.Position = point + new Vector2(10, 0); sprite.ResetPhysicsInterpolation(); sprite.Position += new Vector2(10, 0);
                body.Position = sprite.Position;
                camera.GetGlobalTransform(); camera.Position = new(10, 0); camera.ForceUpdateTransform(); camera.ResetPhysicsInterpolation();
                camera.GetGlobalTransform(); camera.Position = new(20, 0); camera.ForceUpdateTransform();
                instances.SetInstanceTransform2D(0, new Transform(0, point + new Vector2(10, 30))); multi.ResetPhysicsInterpolation();
                instances.SetInstanceTransform2D(0, new Transform(0, point + new Vector2(20, 30)));
            };
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "Native interpolation uses the requested renderer without fallback");
                RenderingServer.FramePostDraw += () =>
                {
                    if (++frames > 240) throw new InvalidOperationException("No suitable group interpolation frames");
                    var fraction = Engine.PhysicsInterpolationFraction; if (phase < 3 || fraction is < .25 or > .65) return;
                    var point = new Vector2((phase & 1) == 0 ? 30 : 80, 25); using var pixels = window.GetTexture().GetImage()!;
                    CheckPixel(point, Colors.White); CheckPixel(point + new Vector2(0, 30), Colors.Red);
                    CheckPixel(new(point.X == 30 ? 80 : 30, 25), Colors.Black);
                    query.Position = body.GlobalPosition; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == body.GetRID() && body.Position == point + new Vector2(20, 0), "Visual history does not move the authoritative body query");
                    var bit = point.X == 30 ? 1 : 2;
                    if ((seen & bit) == 0 && Environment.GetEnvironmentVariable("ELECTRON2D_GROUP_INTERPOLATION_CAPTURE") is { } path) pixels.SavePNG(System.IO.Path.Combine(path, bit + ".png"));
                    seen |= bit; if (seen == 3) window.Tree!.Quit();
                    void CheckPixel(Vector2 at, Color expected)
                    {
                        var actual = pixels.GetPixel((int)(at.X * pixels.Width / window.Size.X), (int)(at.Y * pixels.Height / window.Size.Y));
                        Check(MathF.Abs(actual.R - expected.R) < .1f && MathF.Abs(actual.G - expected.G) < .1f && MathF.Abs(actual.B - expected.B) < .1f, $"Interpolated sprite/camera/MultiMesh pixel at {at}: {actual}, expected {expected}, fraction {fraction}");
                    }
                };
            };
            Check(Engine.Run(window) == 0 && seen == 3 && worker != owner && window.IsDisposed, "Native interpolation presents both explicit reset endpoints and retires the host");
            Console.WriteLine($"Native group interpolation {world.PhysicsBackendName}/{renderer}/{(threaded ? "separate" : "owner")}: intermediate-fraction sprite/camera/MultiMesh reset pixels, authoritative query and lifetime passed.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback);
            ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, oldInterpolation); Engine.PhysicsTicksPerSecond = oldTicks; Engine.MaxFPS = oldFPS;
        }
    }
}
