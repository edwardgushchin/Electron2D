using Electron2D;
using Electron2D.Examples.SceneTiles;

internal static partial class TileSceneTests
{
    internal static void RunNative()
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS; Engine.MaxFPS = 60;
        try { foreach (var renderer in new[] { "gpu", "compatibility" }) foreach (var physics in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) Native(renderer, physics); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); Engine.MaxFPS = fps; }
    }
    private static void Native(string renderer, PhysicsServer.Backend physics)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); var directory = Environment.GetEnvironmentVariable("ELECTRON2D_SCENE_TILES_ASSETS") ?? "bin/scene-tile-assets";
        using var packed = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(directory, "scenes.e2dscene"), ResourceLoader.CacheMode.Ignore);
        using var world = new World(physics); var window = new Window { Title = "Scene tiles validation", Size = new(288, 128), World = world }; var layer = (TileMapLayer)packed.Instantiate(); window.AddChild(layer);
        var frames = 0; var finished = false; long bytes = 0; Node[] placed = [];
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames == 4)
                {
                    placed = layer.GetChildren(); Check(placed.Length == 16 && placed.All(n => n.Owner is null), "Fresh native scene children");
                    using var image = window.GetTexture().GetImage()!;
                    for (var i = 0; i < 8; i++)
                    {
                        var point = layer.GlobalTransform * layer.MapToLocal(new(i, 1)); var hit = Query(world, point); Check(hit is StaticBody body && body.Parent is Entity owner && owner.Position == layer.MapToLocal(new(i, 1)) && !layer.HasBodyRID(body.GetRID()), "Scene collider projects actual child ownership");
                        var actor = placed.OfType<Entity>().Single(n => n.Position == layer.MapToLocal(new(i, 0))); var marker = actor.GlobalTransform * new Vector2(2, -2); var color = image.GetPixel((int)marker.X, (int)marker.Y); Check(color.R > .9f && color.G > .9f && color.B > .9f, "All eight actual scene transform flag pixels");
                    }
                    NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(world.NavigationMap).Length == 8, "Placed navigation regions execute independently of atlas layers");
                    layer.CollisionEnabled = false; layer.NavigationEnabled = false;
                }
                if (frames == 5) { Check(placed.SequenceEqual(layer.GetChildren()) && Query(world, new(32, 64)) is StaticBody, "Atlas-only toggles retain active scene bodies and identity"); }
                if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 40)
                {
                    bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Scene tile owner intervals allocated {bytes} B"); Check(placed.SequenceEqual(layer.GetChildren()), "Forced redraw retains scene identities");
                    using var image = window.GetTexture().GetImage()!; System.IO.Directory.CreateDirectory("bin/scene-tiles-validation"); image.SavePNG($"bin/scene-tiles-validation/{renderer}-{physics}.png"); layer.Hide();
                }
                if (frames == 41) { using var image = window.GetTexture().GetImage()!; Check(image.GetPixel(32, 32).G < .01f && Query(world, new(32, 64)) is StaticBody && placed.SequenceEqual(layer.GetChildren()), "Hidden placed scene keeps simulation and identity"); layer.Show(); layer.Enabled = false; }
                if (frames == 42) { Check(layer.GetChildCount() == 0 && Query(world, new(32, 64)) is null, "Disable removes placed scene physics"); NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(world.NavigationMap).Length == 0, "Disable removes placed navigation"); layer.Enabled = true; }
                if (frames == 43)
                {
                    Check(layer.GetChildCount() == 16 && Query(world, new(32, 64)) is StaticBody, "Reenable restores placed scenes");
                    var previous = Query(world, new(32, 64)); layer.SetCell(new(0, 1), 0, Vector2i.Zero, 2 | TileSetAtlasSource.TransformFlipH);
                    Reject<AggregateException>(layer.UpdateInternals); Check(layer.GetChildCount() == 16 && ReferenceEquals(Query(world, new(32, 64)), previous), "Reflected body rejection cleans candidate and preserves prior collider");
                    NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(world.NavigationMap).Length == 8, "Reflected body failure retains prior navigation and releases candidate"); layer.SetCell(new(0, 1), 0, Vector2i.Zero, 2);
                }
                if (frames == 44) { Check(layer.GetChildCount() == 16, "Repaired body placement retries"); layer.Clear(); }
                if (frames == 45) { Check(layer.GetChildCount() == 0 && Query(world, new(32, 64)) is null, "Clear retires all placed scenes"); finished = true; window.Tree!.Quit(); }
                if (frames >= 5 && frames < 40) layer.QueueRedraw();
            };
        };
        Check(Engine.Run(window) == 0 && finished, "Native scene tile workflow completed"); Console.WriteLine($"Scene tiles {renderer}/{physics}: fresh graph, eight flag pixels, selected-world bodies/navigation, lifetime and 24 forced redraw owner frames={bytes} B passed.");
    }
    private static ElectronObject? Query(World world, Vector2 point)
    { using var query = new PhysicsPointQueryParameters { Position = point }; var hits = world.DirectSpaceState.IntersectPoint(query); return hits.Length == 0 ? null : hits.Single().ColliderObject; }
}
