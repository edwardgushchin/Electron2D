using Electron2D;

public static partial class BackendChecks
{
    private sealed class NativeTransformWriter : Entity
    {
        internal Action? Write;
        protected override void OnPhysicsProcess(double delta) => Write!();
    }
    public static void RunNativeGroupTransformWrites(PhysicsServer.Backend backend, bool threaded, string renderer)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var oldInterpolation = ProjectSettings.Get(ProjectSettings.PhysicsInterpolation); var oldTicks = Engine.PhysicsTicksPerSecond; var oldFPS = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, false);
        Engine.PhysicsTicksPerSecond = 30; Engine.MaxFPS = 60;
        try
        {
            using var world = new World(backend); using var shape = new CircleShape { Radius = 2 };
            using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
            using var window = new Window { Size = new(128, 80), World = world };
            var writer = new NativeTransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
            var sprite = new Sprite { Texture = texture }; var body = new RigidBody { GravityScale = 0, CanSleep = false };
            body.AddChild(new CollisionShape { Shape = shape }); writer.AddChild(sprite); writer.AddChild(body); window.AddChild(writer);
            var owner = Environment.CurrentManagedThreadId; var phase = 0; var seen = 0; var frames = 0; var worker = 0;
            writer.Write = () => { worker = Environment.CurrentManagedThreadId; var point = new Vector2((++phase & 1) == 0 ? 30 : 80, 40); sprite.Position = body.Position = point; };
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "The requested native renderer is active without fallback");
                RenderingServer.FramePostDraw += Render;
            };
            Check(Engine.Run(window) == 0 && seen == 3 && worker != owner && window.IsDisposed, "Native own-group transform publication presents both actual states and cleans the host");
            Console.WriteLine($"Native worker transforms {world.PhysicsBackendName}/{renderer}/{(threaded ? "separate" : "owner")}: both rendered positions and matching public physics queries passed.");
            void Render()
            {
                if (++frames > 180) throw new InvalidOperationException("Worker transform native fixture did not present both states");
                if (phase == 0) return;
                var point = sprite.GlobalPosition; using var pixels = window.GetTexture().GetImage()!;
                var scaleX = pixels.Width / (float)window.Size.X; var scaleY = pixels.Height / (float)window.Size.Y;
                var pixel = pixels.GetPixel((int)(point.X * scaleX), (int)(point.Y * scaleY)); Check(pixel.R > .9f && pixel.G > .9f && pixel.B > .9f, "Rendered sprite follows the worker-authored logical position");
                var other = point.X == 30 ? 80 : 30; pixel = pixels.GetPixel((int)(other * scaleX), (int)(40 * scaleY));
                Check(pixel.R < .1f && pixel.G < .1f && pixel.B < .1f, "The old retained rendering transform is retired");
                query.Position = point; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == body.GetRID(), "Rendered and physical worker-authored poses agree after the join");
                var bit = point.X == 30 ? 1 : 2;
                if ((seen & bit) == 0 && Environment.GetEnvironmentVariable("ELECTRON2D_TRANSFORM_WRITES_CAPTURE") is { } path)
                    pixels.SavePNG(System.IO.Path.Combine(path, bit + ".png"));
                seen |= bit; if (seen == 3) window.Tree!.Quit();
            }
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback);
            ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, oldInterpolation); Engine.PhysicsTicksPerSecond = oldTicks; Engine.MaxFPS = oldFPS;
        }
    }
}
