using Electron2D;

internal static partial class LightingTests
{
    private static void Files()
    {
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White); using var pixels = ImageTexture.CreateFromImage(image);
        using var canvas = new CanvasTexture { DiffuseTexture = pixels, NormalTexture = pixels, SpecularShininess = .25f };
        using var contour = new OccluderPolygon { Closed = false, Polygon = [new(-8, 0), new(8, 0)] };
        using var source = new TileSetAtlasSource { Texture = canvas }; source.CreateTile(Vector2i.Zero); using var set = new TileSet(); set.AddOcclusionLayer(); set.SetOcclusionLayerLightMask(0, uint.MaxValue); set.SetOcclusionLayerSDFCollision(0, true); set.AddSource(source, 0); var data = source.GetTileData(default, 0); data.SetOccluderPolygonsCount(0, 2); data.SetOccluderPolygon(0, 0, contour); data.SetOccluderPolygon(0, 1, contour);
        using var root = new Entity(); var light = new PointLight { Texture = canvas, Height = 8 }; var occluder = new LightOccluder { Occluder = contour }; var tile = new TileMapLayer { TileSet = set }; tile.SetCell(new(1, 1), 0, default);
        foreach (var child in new Node[] { light, occluder, tile }) { root.AddChild(child); child.Owner = root; }
        using var scene = new PackedScene(); scene.Pack(root); var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "canvas-lighting-" + Guid.NewGuid() + ".e2dscene");
        try
        {
            ResourceSaver.Save(scene, path); RunFile(path);
            var start = new System.Diagnostics.ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(LightingTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_LIGHTING"); start.Environment.Remove("ELECTRON2D_TEST_LIGHTING_NATIVE"); start.Environment["ELECTRON2D_TEST_LIGHTING_FILE"] = path;
            using var child = System.Diagnostics.Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh lighting archive."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh lighting graph passed"), errors.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
    }
    internal static void RunFile(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var root = (Entity)scene.Instantiate(); var light = (PointLight)root.GetChild(0); var occluder = (LightOccluder)root.GetChild(1); var layer = (TileMapLayer)root.GetChild(2); var set = layer.TileSet!; var source = (TileSetAtlasSource)set.GetSource(0); var data = source.GetTileData(default, 0);
        Check(light.Height == 8 && light.Texture == source.Texture && light.Texture is CanvasTexture canvas && canvas.DiffuseTexture == canvas.NormalTexture && canvas.SpecularShininess == .25f, "Portable shared multichannel light resource");
        Check(set.GetOcclusionLayerLightMask(0) == uint.MaxValue && set.GetOcclusionLayerSDFCollision(0) && data.GetOccluderPolygon(0, 0) == occluder.Occluder && data.GetOccluderPolygon(0, 1) == occluder.Occluder && occluder.Occluder!.Closed == false, "Portable shared contours and layer policy");
        var flipped = data.GetOccluderPolygon(0, 0, true)!; Check(!flipped.Closed && flipped.Polygon.Length == 2 && data.GetOccluderPolygon(0, 0, true) == flipped, "Cached transformed topology");
        Console.WriteLine("Fresh lighting graph passed.");
    }
}
