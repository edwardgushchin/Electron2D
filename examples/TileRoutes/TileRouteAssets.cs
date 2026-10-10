namespace Electron2D.Examples.TileRoutes;

internal static class TileRouteAssets
{
    internal static void Write(string directory)
    {
        Directory.CreateDirectory(directory);
        using var polygon = new NavigationPolygon { AgentRadius = 0 }; Vector2[] points = [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]; polygon.SetVertices(points); polygon.AddPolygon([0, 1, 2, 3]); polygon.AddOutline(points);
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.Black); using var texture = ImageTexture.CreateFromImage(image);
        using var set = new TileSet(); using var source = new TileSetAtlasSource { Texture = texture }; set.AddNavigationLayer(); set.AddSource(source, 0); source.CreateTile(Vector2i.Zero); source.GetTileData(Vector2i.Zero, 0).SetNavigationPolygon(0, polygon);
        using var layer = new TileMapLayer { Name = "WalkableTiles", ZIndex = -1, TileSet = set, NavigationVisibilityMode = TileMapLayer.DebugVisibilityMode.ForceShow };
        for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) if (x is 0 or 3 || y is 0 or 3) layer.SetCell(new(x, y), 0, Vector2i.Zero);
        using var scene = new PackedScene(); scene.Pack(layer); ResourceSaver.Save(scene, System.IO.Path.Combine(directory, "tiles.e2dscene"));
    }
}
