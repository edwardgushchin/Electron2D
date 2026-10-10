namespace Electron2D.Examples.AnimatedTiles;

internal static class AnimatedTileAssets
{
    internal static void Write(string directory)
    {
        Directory.CreateDirectory(directory);
        using var image = Image.CreateEmpty(48, 16, false, Image.Format.Rgba8); image.Fill(Colors.Red); image.FillRect(new(16, 0, 16, 16), Colors.Green); image.FillRect(new(32, 0, 16, 16), Colors.Blue); using var texture = ImageTexture.CreateFromImage(image);
        using var synchronized = new TileSetAtlasSource { Texture = texture }; synchronized.CreateTile(Vector2i.Zero); synchronized.SetTileAnimationFramesCount(Vector2i.Zero, 2); synchronized.SetTileAnimationFrameDuration(Vector2i.Zero, 0, .05f); synchronized.SetTileAnimationFrameDuration(Vector2i.Zero, 1, .15f); synchronized.CreateTile(new(2, 0));
        using var random = (TileSetAtlasSource)synchronized.Duplicate(); random.SetTileAnimationMode(Vector2i.Zero, TileSetAtlasSource.TileAnimationMode.RandomStartTimes);
        using var set = new TileSet(); set.AddSource(synchronized, 0); set.AddSource(random, 1);
        using var layer = new TileMapLayer { Name = "AnimatedFloor", TileSet = set };
        for (var x = 0; x < 4; x++) { layer.SetCell(new(x, 0), 0, Vector2i.Zero); layer.SetCell(new(x, 1), 1, Vector2i.Zero); }
        layer.SetCell(new(4, 0), 0, new Vector2i(2, 0)); layer.SetCell(new(4, 1), 1, new Vector2i(2, 0));
        using var scene = new PackedScene(); scene.Pack(layer); ResourceSaver.Save(scene, System.IO.Path.Combine(directory, "animated-tiles.e2dscene"));
    }
}
