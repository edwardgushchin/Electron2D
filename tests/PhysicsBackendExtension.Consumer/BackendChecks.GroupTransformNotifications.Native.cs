using Electron2D;

public static partial class BackendChecks
{
    public static void RunNativeGroupTransformNotifications(PhysicsServer.Backend backend, bool threaded, string renderer)
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
            using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
            using var atlas = new TileSetAtlasSource { Texture = texture }; using var set = new TileSet(); set.AddPhysicsLayer(); set.SetPhysicsLayerCollisionLayer(0, 4); set.AddSource(atlas, 3);
            atlas.CreateTile(default); var data = atlas.GetTileData(default, 0); data.AddCollisionPolygon(0); data.SetCollisionPolygonPoints(0, 0, [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]);
            using var window = new Window { Size = new(128, 80), World = world };
            var writer = new NativeTransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
            var marker = new Entity { NotifyTransformChanges = true }; var sprite = new Sprite { Texture = texture };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, CollisionLayer = 1, CollisionMask = 0 }; body.AddChild(new CollisionShape { Shape = shape });
            var camera = new Camera { AnchorMode = AnchorMode.FixedTopLeft, LimitEnabled = false, ProcessCallback = ProcessPhase.Physics };
            var tiles = new TileMapLayer { TileSet = set, Modulate = Colors.Red }; tiles.SetCell(default, 3, Vector2i.Zero);
            writer.AddChild(marker); writer.AddChild(sprite); writer.AddChild(body); writer.AddChild(camera); writer.AddChild(tiles); window.AddChild(writer);
            var owner = Environment.CurrentManagedThreadId; var phase = 0; var forced = 0; var worker = 0; var seen = 0; var frames = 0;
            writer.Write = () => { worker = Environment.CurrentManagedThreadId; phase++; _ = marker.GetGlobalTransform(); marker.Position = new(phase, 0); marker.ForceUpdateTransform(); };
            marker.TransformChanged += sender =>
            {
                if (phase == 0) return;
                Check(Environment.CurrentManagedThreadId == worker, "The native game callback executes immediately in its worker group"); forced++;
                var point = new Vector2((phase & 1) == 0 ? 30 : 80, 40); _ = camera.GetGlobalTransform(); _ = tiles.GetGlobalTransform(); camera.Position = new((phase & 1) == 0 ? 0 : 10, 0); camera.ForceUpdateTransform();
                sprite.Position = body.Position = point + camera.Position; tiles.Position = point + camera.Position + new Vector2(25, 0); tiles.ForceUpdateTransform();
            };
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "The requested renderer is active without fallback");
                RenderingServer.FramePostDraw += () =>
                {
                    if (++frames > 180) throw new InvalidOperationException("Forced native notifications did not present both states");
                    if (phase == 0) return;
                    Check(forced == phase, "Owner publication never replays the worker game notification");
                    var point = new Vector2((phase & 1) == 0 ? 30 : 80, 40); using var pixels = window.GetTexture().GetImage()!;
                    var sx = pixels.Width / (float)window.Size.X; var sy = pixels.Height / (float)window.Size.Y;
                    var color = pixels.GetPixel((int)(point.X * sx), (int)(point.Y * sy)); Check(color.R > .9f && color.G > .9f && color.B > .9f, "Actual sprite pixels follow the forced worker callback and published camera");
                    color = pixels.GetPixel((int)((point.X == 30 ? 80 : 30) * sx), (int)(40 * sy)); Check(color.R < .1f && color.G < .1f && color.B < .1f, "The previous presented sprite position is cleared");
                    color = pixels.GetPixel((int)((point.X + 33) * sx), (int)(48 * sy)); Check(color.R > .9f && color.G < .1f && color.B < .1f, "The forced tile and camera transforms reach actual rendering");
                    query.Position = body.GlobalPosition; query.CollisionMask = 1; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == body.GetRID(), "Native body query matches the worker game pose");
                    query.Position = tiles.GlobalTransform * new Vector2(8, 8); query.CollisionMask = 4; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderObject == tiles, "Native generated tile body publication matches its rendered pose");
                    var bit = point.X == 30 ? 1 : 2;
                    if ((seen & bit) == 0 && Environment.GetEnvironmentVariable("ELECTRON2D_TRANSFORM_NOTIFICATIONS_CAPTURE") is { } path) pixels.SavePNG(System.IO.Path.Combine(path, bit + ".png"));
                    seen |= bit; if (seen == 3) window.Tree!.Quit();
                };
            };
            Check(Engine.Run(window) == 0 && seen == 3 && worker != owner && window.IsDisposed, "Native forced notifications publish both camera/tile/body states and retire the host");
            Console.WriteLine($"Native group notifications {world.PhysicsBackendName}/{renderer}/{(threaded ? "separate" : "owner")}: immediate worker events, camera/tile pixels, matching body/tile queries and lifetime passed.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback);
            ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, oldInterpolation); Engine.PhysicsTicksPerSecond = oldTicks; Engine.MaxFPS = oldFPS;
        }
    }
}
