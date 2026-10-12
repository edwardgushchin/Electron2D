using Electron2D;

internal static class TileAnimationTests
{
    internal static void Run() { Resources(); Padding(); Replay(); Console.WriteLine("Tile atlas animation passed: reservations, fit/move/removal, duration/speed/mode, typed graphs, padding/cache lifetime and retained timed replay."); }
    private static void Resources()
    {
        using var image = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image); using var atlas = new TileSetAtlasSource { Texture = texture }; atlas.CreateTile(default);
        Check(atlas.GetTileAnimationColumns(default) == 0 && atlas.GetTileAnimationFramesCount(default) == 1 && atlas.GetTileAnimationSpeed(default) == 1 && atlas.UseTexturePadding, "Animation and padding defaults");
        atlas.SetTileAnimationFramesCount(default, 4); atlas.SetTileAnimationColumns(default, 2); Check(atlas.GetTileTextureRegion(default, 3) == new Rect2i(16, 16, 16, 16) && atlas.GetTileAtCoords(new(1, 1)) == Vector2i.Zero, "Wrapped frame regions and reservation lookup");
        Reject<ArgumentException>(() => atlas.CreateTile(new(1, 1))); Check(!atlas.HasRoomForTile(new(1, 0), Vector2i.One, 0, default, 1), "All frame reservations exclude other tiles");
        atlas.CreateTile(new(2, 2)); Reject<ArgumentException>(() => atlas.MoveTileInAtlas(default, new Vector2i(1, 1))); Check(atlas.HasTile(default), "Rejected movement preserves tile");
        Check(!atlas.HasRoomForTile(default, Vector2i.One, -1, default, 1) && !atlas.HasRoomForTile(default, Vector2i.Zero, 0, default, 1) && !atlas.HasRoomForTile(new(int.MaxValue, 0), Vector2i.One, 0, default, int.MaxValue), "Invalid/overflow layouts fail before allocation");
        atlas.SetTileAnimationFrameDuration(default, 0, .25f); atlas.SetTileAnimationFrameDuration(default, 1, .75f); atlas.SetTileAnimationSpeed(default, 2); atlas.SetTileAnimationMode(default, TileSetAtlasSource.TileAnimationMode.RandomStartTimes);
        Reject<ArgumentOutOfRangeException>(() => atlas.SetTileAnimationSpeed(default, float.NaN)); Reject<ArgumentOutOfRangeException>(() => atlas.SetTileAnimationFrameDuration(default, 0, 0)); Reject<ArgumentOutOfRangeException>(() => atlas.GetTileTextureRegion(default, 4)); Reject<ArgumentOutOfRangeException>(() => atlas.SetTileAnimationMode(default, TileSetAtlasSource.TileAnimationMode.Max));
        Check(atlas.GetTileAnimationTotalDuration(default) == 3 && atlas.GetTileAnimationSpeed(default) == 2, "Unscaled sum is independent of speed");
        atlas.SetTileAnimationFramesCount(default, 2); atlas.SetTileAnimationFramesCount(default, 4); Check(atlas.GetTileAnimationFrameDuration(default, 2) == 1, "Resize retains surviving durations and initializes new ones");
        atlas.SetTileAnimationSeparation(default, new(1, 0)); Check(atlas.GetTileTextureRegion(default, 1).Position.X == 32 && atlas.GetTileAtCoords(new(1, 0)) == new Vector2i(-1, -1), "Frame gaps are grid units and remain unreserved");
        using var smaller = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8); using var smallerTexture = ImageTexture.CreateFromImage(smaller);
        Check(atlas.GetTilesToBeRemovedOnChange(smallerTexture, default, default, new(16, 16)).Length == 2 && atlas.GetTilesToBeRemovedOnChange(null, default, default, new(16, 16)).Length == 2, "Proposed texture changes include every animation frame and null grid");
        using var copy = (TileSetAtlasSource)atlas.Duplicate(); Check(copy.GetTileAnimationMode(default) == TileSetAtlasSource.TileAnimationMode.RandomStartTimes && copy.GetTileAnimationFrameDuration(default, 0) == .25f && copy.GetRuntimeTexture() != atlas.GetRuntimeTexture(), "Duplication preserves authored values and owns a separate runtime cache");
        atlas.Texture = smallerTexture; Check(atlas.HasTilesOutsideTexture(), "Texture shrink retains authored out-of-bounds tiles");
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "animated-atlas-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try { ResourceSaver.Save(atlas, file); using var loaded = ResourceLoader.Load<TileSetAtlasSource>(file, ResourceLoader.CacheMode.Ignore); Check(loaded!.HasTilesOutsideTexture() && loaded.GetTileAnimationFramesCount(default) == 4 && loaded.GetTileAnimationSeparation(default) == new Vector2i(1, 0), "File graphs preserve authored animation after texture shrink"); }
        finally { File.Delete(file); }
        atlas.ClearTilesOutsideTexture(); Check(atlas.GetTilesCount() == 0, "Clear removes all invalid frame reservations");
        atlas.CreateTile(default); atlas.SetTileAnimationSeparation(default, new(int.MaxValue - 1, int.MaxValue - 1)); Check(atlas.GetTileTextureRegion(default).Position == Vector2i.Zero, "Single-frame separation does not overflow an unused stride");
        atlas.Texture = null; Check(atlas.GetRuntimeTexture() is null, "Missing source has no fabricated runtime image");
    }
    private static void Padding()
    {
        using var image = Image.CreateEmpty(32, 16, true, Image.Format.Rgba8); image.Fill(Colors.Red); image.FillRect(new(16, 0, 16, 16), Colors.Green); using var texture = ImageTexture.CreateFromImage(image); using var atlas = new TileSetAtlasSource { Texture = texture }; atlas.CreateTile(default); atlas.SetTileAnimationFramesCount(default, 2);
        var first = atlas.GetRuntimeTexture()!; using (var padded = first.GetImage()!) { Check(padded.Width == 36 && padded.Height == 18 && padded.GetPixel(0, 0) == Colors.Red && padded.GetPixel(17, 17) == Colors.Red && padded.GetPixel(18, 0) == Colors.Green && !padded.HasMIPMaps, "Each frame extrudes edges/corners, ordinary stride and base image format"); }
        Check(ReferenceEquals(first, atlas.GetRuntimeTexture()), "Unchanged runtime cache identity");
        for (var i = 0; i < 20; i++) atlas.GetRuntimeTexture(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) atlas.GetRuntimeTexture(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "1024 unchanged runtime-cache reads allocate zero bytes");
        image.Fill(Colors.Blue); texture.SetImage(image); var changed = atlas.GetRuntimeTexture()!; Check(first.IsDisposed && changed != first, "Pixel edits replace and release the owned cache");
        atlas.SetTileAnimationFramesCount(default, 1); changed = atlas.GetRuntimeTexture()!; atlas.Separation = new(32767, 0); Reject<ArgumentException>(() => atlas.GetRuntimeTexture()); Check(!changed.IsDisposed, "Failed preparation retains preceding cache lifetime"); atlas.Separation = default; atlas.GetRuntimeTexture(); Check(changed.IsDisposed, "Successful retry replaces retained cache");
        atlas.UseTexturePadding = false; Check(atlas.GetRuntimeTexture() == texture && atlas.GetRuntimeTileTextureRegion(default, 0) == atlas.GetTileTextureRegion(default), "Unpadded path returns original texture and coordinates");
        using var spaced = Image.CreateEmpty(18, 2, false, Image.Format.Rgbaf); spaced.Fill(new(2.5f, .25f, .5f)); using var spacedTexture = ImageTexture.CreateFromImage(spaced); using var other = new TileSetAtlasSource { Texture = spacedTexture, TextureRegionSize = new(2, 2), Separation = new(6, 0) }; other.CreateTile(default, new Vector2i(2, 1)); other.CreateTile(new(2, 0));
        Check(other.GetRuntimeTileTextureRegion(new(2, 0), 0).Position.X == 17, "Large spacing expands padded stride to prevent multi-cell overlap"); using var precise = other.GetRuntimeTexture()!.GetImage()!; Check(precise.GetPixel(0, 0).R == 2.5f && precise.GetPixel(11, 0).R == 2.5f && precise.GetPixel(16, 0).R == 2.5f && precise.PixelFormat == Image.Format.Rgbaf, "Byte-preserving high-precision padding and separated boundaries");
        var owned = other.GetRuntimeTexture()!; other.Dispose(); Check(owned.IsDisposed && !spacedTexture.IsDisposed, "Source owns padding and borrows original texture");
    }
    private static void Replay()
    {
        using var image = Image.CreateEmpty(32, 16, false, Image.Format.Rgba8); image.Fill(Colors.Red); image.FillRect(new(16, 0, 16, 16), Colors.Green); using var texture = ImageTexture.CreateFromImage(image); using var atlas = new TileSetAtlasSource { Texture = texture }; using var set = new TileSet(); set.AddSource(atlas, 0); atlas.CreateTile(default); atlas.SetTileAnimationFramesCount(default, 2); atlas.SetTileAnimationFrameDuration(default, 0, .25f); atlas.SetTileAnimationFrameDuration(default, 1, .75f); atlas.SetTileAnimationSpeed(default, 2);
        using var layer = new TileMapLayer { TileSet = set }; layer.SetCell(default, 0, Vector2i.Zero); layer.SetCell(new(1, 0), 0, Vector2i.Zero); layer.PrepareCanvas(); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); var expectedVertices = 0;
        Color Sample(double time, int cell) { vertices.Clear(); batches.Clear(); layer.AppendCanvas(vertices, batches, Transform.Identity, time); if (expectedVertices == 0) expectedVertices = vertices.Count; Check(vertices.Count == expectedVertices && vertices.Count > 0, "Frame selection preserves one geometry contribution per cell."); var index = cell * (vertices.Count / 2); var uv = vertices[index].UV; using var pixels = batches[0].Texture!.GetImage()!; return pixels.GetPixel((int)(uv.X * pixels.Width), (int)(uv.Y * pixels.Height)); }
        Check(Sample(0, 0) == Colors.Red && Sample(.124, 1) == Colors.Red && Sample(.125, 0) == Colors.Green && Sample(.499, 1) == Colors.Green && Sample(.5, 0) == Colors.Red && Sample(-.01, 0) == Colors.Green, "Unequal durations, speed, boundaries, wrap and negative time");
        atlas.SetTileAnimationMode(default, TileSetAtlasSource.TileAnimationMode.RandomStartTimes); layer.InvalidateCanvas(); layer.PrepareCanvas(); var differs = false; for (var i = 0; i < 100; i++) if (Sample(i / 200d, 0) != Sample(i / 200d, 1)) differs = true; Check(differs, "Random starts desynchronize cells"); var stable = Sample(.12, 0); layer.InvalidateCanvas(); layer.PrepareCanvas(); Check(Sample(.12, 0) == stable, "Random phase survives command re-recording");
        for (var i = 0; i < 30; i++) { vertices.Clear(); batches.Clear(); layer.AppendCanvas(vertices, batches, Transform.Identity, i / 60d); }
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) { vertices.Clear(); batches.Clear(); layer.AppendCanvas(vertices, batches, Transform.Identity, i / 60d); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Retained animated replay allocates zero managed bytes");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
