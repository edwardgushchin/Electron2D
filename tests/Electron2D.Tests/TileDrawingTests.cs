using System.Diagnostics;
using Electron2D;

internal static partial class TileDrawingTests
{
    internal static void Run()
    {
        using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal);
        var data = library.Atlas.GetTileData(default, 0); using var material = new CanvasItemMaterial { BlendMode = BlendMode.Add };
        Check(data.Material is null && data.ZIndex == 0 && data.YSortOrigin == 0 && !library.Set.UVClipping, "Tile drawing defaults");
        var changes = 0; data.Changed += () => changes++; data.Material = material; data.Material = material; data.ZIndex = 2; data.YSortOrigin = 17;
        Check(changes == 4, "Assignments retain tile data notification semantics");
        Reject<ArgumentOutOfRangeException>(() => data.ZIndex = int.MaxValue); Check(data.ZIndex == 2, "Invalid Z rejects before mutation");
        using var layer = new TileMapLayer { TileSet = library.Set, RenderingQuadrantSize = 3, YSortOrigin = -11, XDrawOrderReversed = true, YSortEnabled = true };
        using var defaults = new TileMapLayer(); Check(defaults.RenderingQuadrantSize == 16, "Rendering quadrant default"); Reject<ArgumentOutOfRangeException>(() => layer.RenderingQuadrantSize = 0);
        library.Set.UVClipping = true; layer.SetCell(new(-1, -1), 0, Vector2i.Zero);
        using var shallow = (TileSet)library.Set.Duplicate(); using var deep = (TileSet)library.Set.Duplicate(true);
        Check(((TileSetAtlasSource)shallow.GetSource(0)).GetTileData(default, 0).Material == material, "Shallow tile material is borrowed");
        var copied = ((TileSetAtlasSource)deep.GetSource(0)).GetTileData(default, 0); Check(copied.Material != material && copied.Material is CanvasItemMaterial { BlendMode: BlendMode.Add } && copied.ZIndex == 2 && copied.YSortOrigin == 17 && deep.UVClipping, "Deep drawing resource/values copied");
        var runtime = data.Copy(); try { runtime.ZIndex = -2; runtime.YSortOrigin = 0; runtime.Material = null; Check(data.Material == material && data.ZIndex == 2 && data.YSortOrigin == 17, "Runtime drawing edits retain authored data"); } finally { runtime.Release(); }
        SaveFresh(layer);
        using var disposed = new CanvasItemMaterial(); disposed.Dispose(); Reject<ObjectDisposedException>(() => data.Material = disposed); Check(data.Material == material, "Disposed assignment rejects before replacement");
        Console.WriteLine("Tile drawing metadata passed: defaults, guarded Z/size, repeated notifications, copied/runtime materials and fresh scene schema.");
    }
    private static void SaveFresh(TileMapLayer layer)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tile-draw-" + Guid.NewGuid() + ".e2dscene"); using var packed = new PackedScene(); packed.Pack(layer);
        try
        {
            ResourceSaver.Save(packed, path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(TileDrawingTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_TILE_DRAW"); start.Environment.Remove("ELECTRON2D_TEST_TILE_DRAW_NATIVE"); start.Environment["ELECTRON2D_TEST_TILE_DRAW_FILE"] = path;
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh tile drawing scene process."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh tile drawing schema passed"), errors.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
    }
    internal static void RunFile(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var layer = (TileMapLayer)packed.Instantiate();
        var data = ((TileSetAtlasSource)layer.TileSet!.GetSource(0)).GetTileData(default, 0);
        Check(layer.RenderingQuadrantSize == 3 && layer.YSortOrigin == -11 && layer.XDrawOrderReversed && layer.YSortEnabled && layer.TileSet.UVClipping && data.ZIndex == 2 && data.YSortOrigin == 17 && data.Material is CanvasItemMaterial { BlendMode: BlendMode.Add }, "Portable drawing policies and borrowed material graph");
        Console.WriteLine("Fresh tile drawing schema passed.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
