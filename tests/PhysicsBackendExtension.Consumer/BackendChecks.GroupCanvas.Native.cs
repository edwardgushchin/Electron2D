using Electron2D;

public static partial class BackendChecks
{
    public static void RunNativeGroupCanvas(PhysicsServer.Backend backend, bool threaded, string renderer)
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
            using var window = new Window { Size = new(128, 80), World = world }; var layer = new CanvasLayer { Transform = new Transform(0, new(3, 0)) };
            var writer = new NativeTransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread }; var probe = new CanvasNoticeProbe();
            var body = new RigidBody { GravityScale = 0, CanSleep = false, CollisionMask = 0 }; body.AddChild(new CollisionShape { Shape = shape });
            var area = new Area { CollisionLayer = 2 }; area.AddChild(new CollisionShape { Shape = shape });
            var sprite = new Sprite { Texture = texture, Scale = new(.5f, .5f) }; var sensor = new Sprite { Texture = texture, Scale = new(.5f, .5f), Modulate = Colors.Blue };
            var tiles = new TileMapLayer { TileSet = set, Modulate = Colors.Red }; tiles.SetCell(default, 3, Vector2i.Zero);
            writer.AddChild(body); writer.AddChild(area); writer.AddChild(sprite); writer.AddChild(sensor); writer.AddChild(tiles); writer.AddChild(probe); layer.AddChild(writer); window.AddChild(layer);
            var owner = Environment.CurrentManagedThreadId; var phase = 0; var worker = 0; var seen = 0; var frames = 0; var tileRID = default(RID);
            writer.Write = () =>
            {
                phase++; worker = Environment.CurrentManagedThreadId; var point = new Vector2((phase & 1) == 0 ? 30 : 80, 25);
                body.Position = sprite.Position = point; area.Position = sensor.Position = point + new Vector2(0, 30);
                tiles.GetGlobalTransform(); tiles.Position = point + new Vector2(17, -8); tiles.ForceUpdateTransform();
                Check(ReferenceEquals(body.GetCanvasLayerNode(), layer) && ReferenceEquals(tiles.GetCanvasLayerNode(), layer), "Worker membership reads preserve the actual layer");
                probe.Observe = true; var before = probe.Notices;
                writer.PropagateNotification(CanvasItem.NotificationExitCanvas); writer.PropagateNotification(Node.NotificationDisabled); writer.PropagateNotification(Node.NotificationEnabled); writer.PropagateNotification(CanvasItem.NotificationEnterCanvas);
                if ((phase & 1) != 0) writer.PropagateNotification(CanvasItem.NotificationExitCanvas);
                Check(probe.Notices == before + 4 + (phase & 1) && probe.LastThread == worker, "Native user notifications are immediate on the worker"); probe.Observe = false;
            };
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            window.Ready += _ =>
            {
                tiles.UpdateInternals(); query.Position = new(8, 8); query.CollisionMask = 4; query.CanvasInstanceID = layer.InstanceID;
                Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1, "Initial generated tile body exists"); tileRID = hits[0].ColliderRID;
                RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "The requested renderer is active without fallback");
                RenderingServer.FramePostDraw += () =>
                {
                    if (++frames > 180) throw new InvalidOperationException("No native canvas identity states"); if (phase == 0) return;
                    var point = new Vector2((phase & 1) == 0 ? 30 : 80, 25); var canvas = (phase & 1) == 0 ? layer.InstanceID : 0;
                    using var pixels = window.GetTexture().GetImage()!; Pixel(point + new Vector2(3, 0), Colors.White); Pixel(point + new Vector2(28, 0), Colors.Red); Pixel(point + new Vector2(3, 30), Colors.Blue); Pixel(new(point.X == 30 ? 83 : 33, 25), Colors.Black);
                    Match(body, body.GetRID(), body.GlobalPosition, false, 1); Match(area, area.GetRID(), area.GlobalPosition, true, 2); Match(tiles, tileRID, tiles.GlobalTransform * new Vector2(8, 8), false, 4);
                    Check(PhysicsServer.BodyGetCanvasInstanceID(body.GetRID()) == canvas && PhysicsServer.AreaGetCanvasInstanceID(area.GetRID()) == canvas && PhysicsServer.BodyGetCanvasInstanceID(tileRID) == canvas, "Rendered body/area/tile queries use the published canvas identity");
                    var bit = canvas == 0 ? 1 : 2;
                    if ((seen & bit) == 0 && Environment.GetEnvironmentVariable("ELECTRON2D_GROUP_CANVAS_CAPTURE") is { } path) pixels.SavePNG(System.IO.Path.Combine(path, bit + ".png"));
                    seen |= bit; if (seen == 3) window.Tree!.Quit();
                    void Pixel(Vector2 at, Color expected) { var color = pixels.GetPixel((int)(at.X * pixels.Width / window.Size.X), (int)(at.Y * pixels.Height / window.Size.Y)); Check(MathF.Abs(color.R - expected.R) < .1f && MathF.Abs(color.G - expected.G) < .1f && MathF.Abs(color.B - expected.B) < .1f, $"Native canvas pixel at {at}: {color}, expected {expected}"); }
                    void Match(Node node, RID target, Vector2 at, bool isArea, uint mask)
                    {
                        query.Position = at; query.CollisionMask = mask; query.CollideWithAreas = isArea; query.CollideWithBodies = !isArea; query.CanvasInstanceID = canvas;
                        Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderObject == node && hits[0].ColliderRID == target, "Native matching-canvas query preserves identity");
                        query.CanvasInstanceID = canvas == 0 ? layer.InstanceID : 0; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 0, "Native opposite-canvas query excludes the same rendered object");
                    }
                };
            };
            Check(Engine.Run(window) == 0 && seen == 3 && worker != owner && window.IsDisposed, "Native publication presents both states and retires the window");
            Console.WriteLine($"Native group canvas {world.PhysicsBackendName}/{renderer}/{(threaded ? "separate" : "owner")}: immediate notifications, layer pixels, filtered body/area/tile queries and lifetime passed.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback);
            ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, oldInterpolation); Engine.PhysicsTicksPerSecond = oldTicks; Engine.MaxFPS = oldFPS;
        }
    }
}
