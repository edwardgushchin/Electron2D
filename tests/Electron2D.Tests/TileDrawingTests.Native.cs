using Electron2D;

internal static partial class TileDrawingTests
{
    private sealed class RuntimeLayer : TileMapLayer
    {
        internal bool OverrideDrawing;
        internal Material? RuntimeMaterial;
        protected override bool UseTileDataRuntimeUpdate(Vector2i coords) => OverrideDrawing;
        protected override void TileDataRuntimeUpdate(Vector2i coords, TileData data) { if (coords == default) { data.ZIndex = 5; data.Material = RuntimeMaterial; } }
    }
    internal static void RunNative()
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS; Engine.MaxFPS = 60;
        try { foreach (var renderer in new[] { "gpu", "compatibility" }) foreach (var physics in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) Native(renderer, physics); foreach (var renderer in new[] { "gpu", "compatibility" }) foreach (var fixture in new[] { "TextureGlsl", "TextureHlsl" }) NativeShader(renderer, fixture); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); Engine.MaxFPS = fps; }
    }
    private static CanvasItem[] DrawingItems(TileMapLayer layer) => layer.ServerState!.Children.Select(w => w.TryGetTarget(out var item) ? item : null).Where(i => i?.ServerState?.TileOwner == layer).Cast<CanvasItem>().ToArray();
    private static void Native(string renderer, PhysicsServer.Backend physics)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal);
        var red = library.Atlas.GetTileData(default, 0); red.Modulate = new(.6f, 0, 0); red.ZIndex = 1;
        var alt = library.Atlas.CreateAlternativeTile(default); var blue = library.Atlas.GetTileData(default, alt); blue.Modulate = new(0, 0, .8f); blue.TextureOrigin = new(16, 0); blue.AddCollisionPolygon(0); blue.SetCollisionPolygonPoints(0, 0, red.GetCollisionPolygonPoints(0, 0)); blue.SetNavigationPolygon(0, red.GetNavigationPolygon(0));
        using var add = new CanvasItemMaterial { BlendMode = BlendMode.Add }; using var inherited = new CanvasItemMaterial { BlendMode = BlendMode.Add };
        using var world = new World(physics); var window = new Window { Title = "Tile drawing validation", Size = new(400, 360), World = world };
        var layer = new RuntimeLayer { Name = "Order", TileSet = library.Set, Position = new(64, 48), PhysicsQuadrantSize = 1, TextureFilter = TextureFilter.Nearest }; window.AddChild(layer); layer.SetCell(default, 0, Vector2i.Zero); layer.SetCell(new(1, 0), 0, Vector2i.Zero, alt);
        using var uvSet = new TileSet(); using var uvAtlas = new TileSetAtlasSource { UseTexturePadding = false };
        using var uvImage = Image.CreateEmpty(32, 16, false, Image.Format.Rgba8); uvImage.Fill(Colors.Blue); uvImage.FillRect(new(0, 0, 16, 16), Colors.Red); using var uvTexture = ImageTexture.CreateFromImage(uvImage); uvAtlas.Texture = uvTexture; uvAtlas.CreateTile(default); uvSet.AddSource(uvAtlas, 0);
        var uv = new TileMapLayer { Name = "UV", TileSet = uvSet, Position = new(210, 48), Scale = new(8, 8), TextureFilter = TextureFilter.Linear }; uv.SetCell(default, 0, Vector2i.Zero); window.AddChild(uv);
        var grids = new List<(TilePatternTests.Library Library, Vector2 Pixel)>();
        foreach (var shape in Enum.GetValues<TileShape>()) foreach (var layout in Enum.GetValues<TileLayout>()) foreach (var axis in Enum.GetValues<TileOffsetAxis>())
                {
                    var index = grids.Count; var gridLibrary = new TilePatternTests.Library(shape, layout, axis); var point = new Vector2(24 + 48 * (index % 8), 150 + 34 * (index / 8));
                    var grid = new TileMapLayer { Name = "Grid" + index, TileSet = gridLibrary.Set, RenderingQuadrantSize = 3, Position = point - gridLibrary.Set.MapToLocal(new(-1, -1)), TextureFilter = TextureFilter.Nearest }; grid.SetCell(new(-1, -1), 0, Vector2i.Zero); window.AddChild(grid); grids.Add((gridLibrary, point));
                }
        var frames = 0; long bytes = 0; RID[] identities = []; var finished = false;
        try
        {
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePostDraw += () =>
                {
                    frames++;
                    if (frames == 4)
                    {
                        Pixel(window, 72, 56, new(.6f, 0, 0)); Check(layer.GetChildCount(true) == 0, "Rendering groups do not change authored scene hierarchy");
                        foreach (var item in DrawingItems(layer)) { Reject<InvalidOperationException>(() => RenderingServer.FreeRID(item.GetCanvasItem())); Reject<InvalidOperationException>(item.Dispose); }
                        foreach (var c in layer.GetUsedCells()) { using var query = new PhysicsPointQueryParameters { Position = layer.GlobalTransform * layer.MapToLocal(c) }; Check(world.DirectSpaceState.IntersectPoint(query).Any(h => h.ColliderObject == layer), "Drawing order retains actual selected-world collision"); }
                        NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(layer.GetNavigationMap()).Count(r => NavigationServer.RegionGetOwnerID(r) == layer.InstanceID) == 2, "Drawing policies retain navigation");
                        using var image = window.GetTexture().GetImage()!; Check(image.GetPixel(337, 112).B > .1f, "Unclipped filtered UV samples neighboring atlas pixels"); uvSet.UVClipping = true; layer.RenderingQuadrantSize = 1;
                        foreach (var grid in grids) Pixel(window, (int)grid.Pixel.X, (int)grid.Pixel.Y, new(.25f, .7f, .4f));
                    }
                    if (frames == 5) { Check(DrawingItems(layer).Any(i => i.ServerState!.Transform!.Value.Origin.X == 24), "Changed quadrant size groups by floored cell coordinates"); Pixel(window, 337, 112, Colors.Red); layer.SelfModulate = new(.5f, 1, 1); }
                    if (frames == 6) { Pixel(window, 72, 56, new(.3f, 0, 0)); identities = DrawingItems(layer).Select(i => i.GetCanvasItem()).ToArray(); }
                    if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
                    if (frames == 40)
                    {
                        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Tile drawing frames allocated {bytes} B"); Check(identities.SequenceEqual(DrawingItems(layer).Select(i => i.GetCanvasItem())), "Forced redraw retains prepared drawing identities");
                        using var image = window.GetTexture().GetImage()!; System.IO.Directory.CreateDirectory("bin/tile-render-validation"); image.SavePNG($"bin/tile-render-validation/order-{renderer}-{physics}.png"); red.ZIndex = -1;
                    }
                    if (frames == 41) { Pixel(window, 72, 56, new(0, 0, .8f)); red.ZIndex = 0; RenderingServer.CanvasItemSetSortChildrenByY(layer.GetCanvasItem(), true); }
                    if (frames == 42) { Pixel(window, 72, 56, new(0, 0, .8f)); layer.YSortEnabled = true; layer.XDrawOrderReversed = true; }
                    if (frames == 43) { Pixel(window, 72, 56, new(.3f, 0, 0)); blue.YSortOrigin = 20; layer.YSortOrigin = -5; }
                    if (frames == 44) { Pixel(window, 72, 56, new(0, 0, .8f)); blue.Material = add; }
                    if (frames == 45) { Pixel(window, 72, 56, new(.3f, 0, .8f)); blue.Material = null; layer.Material = inherited; }
                    if (frames == 46) { Pixel(window, 72, 56, new(.3f, 0, .8f)); inherited.BlendMode = BlendMode.Mix; }
                    if (frames == 47)
                    {
                        Pixel(window, 72, 56, new(0, 0, .8f));
                        layer.YSortEnabled = false; layer.RenderingQuadrantSize = 3; var oldItems = DrawingItems(layer).Select(i => i.GetCanvasItem()).ToArray(); layer.SetCell(new(int.MinValue, 0), 0, Vector2i.Zero);
                        Reject<OverflowException>(layer.PrepareCanvas); Check(oldItems.SequenceEqual(DrawingItems(layer).Select(i => i.GetCanvasItem())), "Invalid anchor preserves all prepared drawing identities"); layer.EraseCell(new(int.MinValue, 0)); layer.YSortEnabled = true; layer.OverrideDrawing = true; layer.RuntimeMaterial = add; layer.NotifyRuntimeTileDataUpdate();
                    }
                    if (frames == 48) { Pixel(window, 72, 56, new(.3f, 0, .8f)); Check(red.Material is null && red.ZIndex == 0, "Runtime material/Z edits preserve authored values"); layer.Clear(); }
                    if (frames == 49) { Pixel(window, 72, 56, Colors.Black); Check(layer.ServerState!.Children.Count == 0, "Cleared batches release weak parent links"); finished = true; window.Tree!.Quit(); }
                    if (frames >= 6 && frames < 40) layer.QueueRedraw();
                };
            };
            Check(Engine.Run(window) == 0 && finished, "Native drawing workflow completed"); Console.WriteLine($"Native tile drawing: {renderer}/{physics}, material inheritance/live blend, Z/Y/X order, UV clipping, owned groups and 24 forced-redraw owner frames={bytes} B.");
        }
        finally { window.Dispose(); foreach (var grid in grids) grid.Library.Dispose(); }
    }
    private static void NativeShader(string renderer, string fixture)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); using var input = typeof(TileDrawingTests).Assembly.GetManifestResourceStream("TestShaders." + fixture + ".spv")!; using var memory = new MemoryStream(); input.CopyTo(memory);
        using var shader = Shader.CreateFromSPIRV(memory.ToArray()); using var material = new ShaderMaterial { Shader = shader };
        using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal); var data = library.Atlas.GetTileData(default, 0); data.Material = material; data.Modulate = Colors.White;
        shader.SetDefaultTextureParameter("colorMap", library.Atlas.Texture); material.SetShaderParameter("detailMap", library.Atlas.Texture); material.SetShaderParameter("tint", Colors.Green);
        var window = new Window { Size = new(64, 64) }; var layer = new TileMapLayer { TileSet = library.Set, TextureFilter = TextureFilter.Nearest }; layer.SetCell(default, 0, Vector2i.Zero); window.AddChild(layer); var frames = 0;
        try
        {
            if (renderer == "compatibility") Reject<NotSupportedException>(() => Engine.Run(window));
            else { window.Ready += _ => RenderingServer.FramePostDraw += () => { frames++; Pixel(window, 8, 8, frames == 1 ? Colors.Green : Colors.Blue); if (frames == 1) material.SetShaderParameter("tint", Colors.Blue); else window.Tree!.Quit(); }; Check(Engine.Run(window) == 0 && frames == 2, "Live typed tile shader parameters reach GPU pixels"); }
            Console.WriteLine($"Tile shader {fixture}/{renderer}: " + (renderer == "gpu" ? "typed pixels/live parameters passed." : "explicit capability rejection passed."));
        }
        finally { window.Dispose(); }
    }
    private static void Pixel(Window window, int x, int y, Color expected)
    { using var image = window.GetTexture().GetImage()!; var actual = image.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .035f && Math.Abs(actual.G - expected.G) < .035f && Math.Abs(actual.B - expected.B) < .035f, $"Pixel {x},{y}: expected {expected}, got {actual}"); }
}
