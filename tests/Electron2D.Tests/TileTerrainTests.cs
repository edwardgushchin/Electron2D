using System.Text.Json;
using Electron2D;

internal static partial class TileTerrainTests
{
    private sealed class Library : IDisposable
    {
        internal readonly TileSet Set;
        internal readonly TileSetAtlasSource Atlas;
        private readonly Image _image;
        private readonly ImageTexture _texture;
        internal Library(TileShape shape, TileLayout layout, TileOffsetAxis axis, TerrainMode mode, bool complete = true)
        {
            Set = new() { TileShape = shape, TileLayout = layout, TileOffsetAxis = axis }; Set.AddTerrainSet(); Set.SetTerrainSetMode(0, mode); Set.AddTerrain(0);
            _image = Image.CreateEmpty(3, 1, false, Image.Format.Rgba8); _texture = ImageTexture.CreateFromImage(_image); Atlas = new() { Texture = _texture, TextureRegionSize = Vector2i.One }; Set.AddSource(Atlas, 0); Atlas.CreateTile(default);
            var probe = Atlas.GetTileData(default, 0); probe.TerrainSet = 0; var bits = Enumerable.Range(0, 16).Select(i => (CellNeighbor)i).Where(probe.IsValidTerrainPeeringBit).ToArray();
            var count = 1 << bits.Length;
            if (!complete) { probe.Terrain = 0; foreach (var b in bits) probe.SetTerrainPeeringBit(b, 0); return; }
            for (var index = 0; index < count * 2; index++)
            {
                var coords = new Vector2i(index / 4000, 0); var alternative = index % 4000;
                if (alternative == 0) { if (index > 0) Atlas.CreateTile(coords); } else Atlas.CreateAlternativeTile(coords, alternative);
                var data = Atlas.GetTileData(coords, alternative); data.TerrainSet = 0; data.Terrain = index < count ? -1 : 0;
                for (var i = 0; i < bits.Length; i++) data.SetTerrainPeeringBit(bits[i], (index % count & 1 << i) != 0 ? 0 : -1);
            }
        }
        internal void PrepareDrawing()
        {
            _image.Resize(24, 8); _image.Fill(Colors.White); _texture.SetImage(_image); Atlas.TextureRegionSize = new(8, 8); Set.TileSize = new(24, 18);
        }
        public void Dispose() { Set.Dispose(); Atlas.Dispose(); _texture.Dispose(); _image.Dispose(); }
    }
    internal static void Run()
    {
        Resources(); ScalarAllocation(); CallbackFailure(); TransferFailure(); Reference(); Validation(); Weights(); Consumers();
        Console.WriteLine("Tile terrains passed: definitions/remapping, all modes and grid configurations, pinned connect/path behavior, typed failures and weighted alternatives.");
    }
    private static void Resources()
    {
        using var library = new Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal, TerrainMode.MatchCornersAndSides, false); var set = library.Set; var data = library.Atlas.GetTileData(default, 0);
        Check(set.GetTerrainSetsCount() == 1 && set.GetTerrainsCount(0) == 1 && set.GetTerrainName(0, 0) == "Terrain 0" && set.GetTerrainColor(0, 0) == Color.FromHSV(1f / 16, .5f, .5f), "Terrain defaults");
        set.SetTerrainName(0, 0, "Grass"); set.SetTerrainColor(0, 0, new(.1f, .7f, .3f, .4f)); Check(set.GetTerrainName(0, 0) == "Grass" && set.GetTerrainColor(0, 0).A == 1, "Names and opaque diagnostic colors");
        set.AddTerrain(0, 0); Check(data.Terrain == 1 && data.GetTerrainPeeringBit(CellNeighbor.RightSide) == 1 && set.GetTerrainName(0, 1) == "Grass", "Insertion preserves center and peering identity");
        set.AddTerrain(0); set.MoveTerrain(0, 0, 3); Check(data.Terrain == 0 && data.GetTerrainPeeringBit(CellNeighbor.RightSide) == 0 && set.GetTerrainName(0, 0) == "Grass", "Forward move preserves all assigned identities");
        set.MoveTerrain(0, 2, 0); Check(data.Terrain == 1 && data.GetTerrainPeeringBit(CellNeighbor.RightSide) == 1, "Backward move preserves identities");
        set.AddTerrainSet(0); Check(data.TerrainSet == 1 && data.Terrain == 1, "Terrain set insertion remaps alternatives"); set.MoveTerrainSet(0, 2); Check(data.TerrainSet == 0 && data.Terrain == 1, "Forward set move uses final insertion index");
        using var shallow = (TileSet)set.Duplicate(); var copied = ((TileSetAtlasSource)shallow.GetSource(0)).GetTileData(default, 0); Check(copied.TerrainSet == 0 && copied.Terrain == 1 && copied.GetTerrainPeeringBit(CellNeighbor.TopSide) == 1, "Source data duplication binds copied definitions");
        using var copy = new TileSet(); copy.CopyFromResource(set); Check(copy.GetTerrainName(0, 1) == "Grass" && ((TileSetAtlasSource)copy.GetSource(0)).GetTileData(default, 0).Terrain == 1, "CopyFromResource preserves definition/data graphs");
        var changes = 0; data.Changed += () => changes++; data.Probability = 1; data.Terrain = 1; data.TerrainSet = 0; Check(changes == 0, "Quiet equal setters");
        data.Probability = 2.5f; Check(changes == 1, "Weight changes notify");
        set.SetTerrainSetMode(0, TerrainMode.MatchCorners); Check(!data.IsValidTerrainPeeringBit(CellNeighbor.RightSide) && data.IsValidTerrainPeeringBit(CellNeighbor.BottomRightCorner), "Mode filters directions");
        Reject<ArgumentOutOfRangeException>(() => data.GetTerrainPeeringBit(CellNeighbor.RightSide)); set.SetTerrainSetMode(0, TerrainMode.MatchSides); Check(data.GetTerrainPeeringBit(CellNeighbor.RightSide) == 1, "Inactive authored peering values survive mode changes");
        set.TileShape = TileShape.Hexagon; Check(data.IsValidTerrainPeeringBit(CellNeighbor.BottomRightSide) && !data.IsValidTerrainPeeringBit(CellNeighbor.BottomSide), "Shape updates availability"); set.TileShape = TileShape.Square;
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-terrain-" + Guid.NewGuid() + ".e2dres");
        try
        {
            ResourceSaver.Save(set, path); using var restored = ResourceLoader.Load<TileSet>(path, ResourceLoader.CacheMode.Ignore); var loaded = ((TileSetAtlasSource)restored.GetSource(0)).GetTileData(default, 0);
            Check(restored.GetTerrainName(0, 1) == "Grass" && restored.GetTerrainSetsCount() == 2 && loaded.TerrainSet == 0 && loaded.Terrain == 1 && loaded.Probability == 2.5f && loaded.GetTerrainPeeringBit(CellNeighbor.RightSide) == 1, "Typed definition/data file roundtrip");
        }
        finally { System.IO.File.Delete(path); }
        set.RemoveTerrain(0, 1); Check(data.Terrain == -1 && data.GetTerrainPeeringBit(CellNeighbor.RightSide) == -1, "Removing assigned terrain clears center and peering");
        data.Terrain = 0; data.SetTerrainPeeringBit(CellNeighbor.RightSide, 0); set.ClearTerrains(0); Check(data.Terrain == -1 && data.GetTerrainPeeringBit(CellNeighbor.RightSide) == -1, "Clear preserves terrain set but clears assignments");
        set.AddTerrain(0); data.Terrain = 0; data.SetTerrainPeeringBit(CellNeighbor.RightSide, 0); set.RemoveTerrainSet(0); data.SetTerrainPeeringBit(CellNeighbor.RightSide, -1); Check(data.TerrainSet == -1 && data.Terrain == -1 && !data.IsValidTerrainPeeringBit(CellNeighbor.RightSide), "Removing set clears all assignments");
    }
    private static void ScalarAllocation()
    {
        using var library = new Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal, TerrainMode.MatchSides, false); var data = library.Atlas.GetTileData(default, 0);
        var sum = 0f; for (var i = 0; i < 32; i++) sum += data.Terrain + data.Probability + library.Set.GetTerrainsCount(0);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) sum += data.Terrain + data.Probability + data.GetTerrainPeeringBit(CellNeighbor.RightSide) + (data.IsValidTerrainPeeringBit(CellNeighbor.RightSide) ? 1 : 0) + library.Set.GetTerrainSetsCount() + library.Set.GetTerrainsCount(0);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes && sum > 0, "Warmed terrain scalar getters allocate zero managed bytes");
    }
    private static void CallbackFailure()
    {
        using var library = new Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal, TerrainMode.MatchSides, false);
        var set = library.Set; var atlas = library.Atlas; var first = atlas.GetTileData(default, 0); var id = atlas.CreateAlternativeTile(default); var second = atlas.GetTileData(default, id); second.TerrainSet = 0; second.Terrain = 0; second.SetTerrainPeeringBit(CellNeighbor.RightSide, 0);
        var published = 0; set.Changed += _ => published++;
        Action<ElectronObject> fail = _ => throw new InvalidOperationException("Expected terrain listener failure."); first.PropertyListChanged += fail;
        Reject<InvalidOperationException>(() => set.AddTerrain(0, 0));
        Check(first.Terrain == 1 && second.Terrain == 1 && first.GetTerrainPeeringBit(CellNeighbor.RightSide) == 1 && second.GetTerrainPeeringBit(CellNeighbor.RightSide) == 1 && published == 1, "Listener failure occurs after all identities remap and parent publication is attempted");
        first.PropertyListChanged -= fail;
        Action<ElectronObject> reenter = _ => set.AddTerrain(0); first.PropertyListChanged += reenter; Reject<InvalidOperationException>(() => set.MoveTerrain(0, 0, 2));
        Check(set.GetTerrainsCount(0) == 2 && first.Terrain == 0 && second.Terrain == 0, "Reentrant structural edits reject without partial remapping"); first.PropertyListChanged -= reenter;
        set.AddTerrain(0); Check(set.GetTerrainsCount(0) == 3, "Definition edit guard recovers after failure");
    }
    private static void TransferFailure()
    {
        using var library = new Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal, TerrainMode.MatchSides, false); using var target = new TileSet(); target.AddTerrainSet(); target.AddTerrain(0);
        var data = library.Atlas.GetTileData(default, 0); Action<ElectronObject> fail = _ => throw new InvalidOperationException("Expected binding listener failure."); data.PropertyListChanged += fail;
        Reject<InvalidOperationException>(() => target.AddSource(library.Atlas, 7));
        Check(!library.Set.HasSource(0) && target.GetSource(7) == library.Atlas, "Failed binding notification leaves source transferred coherently"); data.PropertyListChanged -= fail;
        var changed = 0; target.Changed += _ => changed++; data.Probability = 2; Check(changed == 1, "Failed binding notification retains source observation");
        library.Atlas.Dispose(); Check(!target.HasSource(7), "Failed binding notification retains disposal observation");
    }
    private static void Reference()
    {
        using var fixture = JsonDocument.Parse(typeof(TileTerrainTests).Assembly.GetManifestResourceStream("TestTiles.terrain-reference.json")!); var cases = fixture.RootElement.GetProperty("cases"); Check(cases.GetArrayLength() == 144, "Complete shape/layout/axis/mode family");
        foreach (var c in cases.EnumerateArray())
        {
            var shape = (TileShape)c.GetProperty("shape").GetInt32(); var layout = (TileLayout)c.GetProperty("layout").GetInt32(); var axis = (TileOffsetAxis)c.GetProperty("axis").GetInt32(); var mode = (TerrainMode)c.GetProperty("mode").GetInt32(); var identity = $"{shape}/{layout}/{axis}/{mode}";
            using var library = new Library(shape, layout, axis, mode); using var layer = new TileMapLayer { TileSet = library.Set };
            var probe = library.Atlas.GetTileData(default, 0); Check(Enumerable.Range(0, 16).Where(i => probe.IsValidTerrainPeeringBit((CellNeighbor)i)).SequenceEqual(c.GetProperty("bits").EnumerateArray().Select(i => i.GetInt32())), identity + " mode directions");
            foreach (var step in c.GetProperty("steps").EnumerateArray())
            {
                var input = step.GetProperty("input").EnumerateArray().Select(r => new Vector2i(r[0].GetInt32(), r[1].GetInt32())).ToArray(); var operation = step.GetProperty("operation").GetString(); var terrain = step.GetProperty("terrain").GetInt32(); var ignore = step.GetProperty("ignore_empty").GetBoolean();
                if (operation == "clear") layer.Clear(); else if (operation == "connect") layer.SetCellsTerrainConnect(input, 0, terrain, ignore); else layer.SetCellsTerrainPath(input, 0, terrain, ignore);
                var actual = layer.GetUsedCells().OrderBy(v => v.X).ThenBy(v => v.Y).ToArray(); var rows = step.GetProperty("cells");
                Check(actual.Length == rows.GetArrayLength(), $"{identity}/{operation}/{terrain}/{ignore} used cells {actual.Length} != {rows.GetArrayLength()}");
                for (var index = 0; index < actual.Length; index++)
                {
                    var row = rows[index]; var coord = actual[index]; Check(coord == new Vector2i(row[0].GetInt32(), row[1].GetInt32()), identity + " output coordinate");
                    var data = layer.GetCellTileData(coord)!; Check(data.Terrain == row[6].GetInt32(), identity + " terrain center");
                    Check(layer.GetCellSourceID(coord) == row[2].GetInt32() && layer.GetCellAtlasCoords(coord) == new Vector2i(row[3].GetInt32(), row[4].GetInt32()) && layer.GetCellAlternativeTile(coord) == row[5].GetInt32(), identity + " chosen unique pattern");
                    foreach (var b in c.GetProperty("bits").EnumerateArray()) { var bit = b.GetInt32(); Check(data.GetTerrainPeeringBit((CellNeighbor)bit) == row[7][bit].GetInt32(), identity + " peering bit " + bit); }
                }
            }
        }
    }
    private static void Validation()
    {
        using var library = new Library(TileShape.Hexagon, TileLayout.Stacked, TileOffsetAxis.Horizontal, TerrainMode.MatchCornersAndSides, false); using var layer = new TileMapLayer { TileSet = library.Set }; var data = library.Atlas.GetTileData(default, 0);
        Reject<ArgumentOutOfRangeException>(() => data.Probability = float.NaN); Reject<ArgumentOutOfRangeException>(() => data.Probability = -1); Reject<ArgumentOutOfRangeException>(() => data.TerrainSet = 1);
        var descriptor = (PropertyDescriptor<TileData, int>)data.GetPropertyList().Single(p => p.Name == nameof(TileData.TerrainSet)); Reject<ArgumentOutOfRangeException>(() => descriptor.SetValue(data, 1)); Reject<ArgumentOutOfRangeException>(() => data.Terrain = 1); Reject<ArgumentOutOfRangeException>(() => data.SetTerrainPeeringBit(CellNeighbor.BottomRightCorner, 1));
        Reject<ArgumentOutOfRangeException>(() => library.Set.SetTerrainSetMode(0, (TerrainMode)3)); Reject<ArgumentOutOfRangeException>(() => library.Set.SetTerrainColor(0, 0, new(float.NaN, 0, 0)));
        Reject<ArgumentOutOfRangeException>(() => data.SetTerrainPeeringBit(CellNeighbor.BottomSide, 0)); Check(!data.IsValidTerrainPeeringBit((CellNeighbor)16) && data.Probability == 1 && data.Terrain == 0, "Rejected edits preserve data");
        layer.SetCellsTerrainConnect([default], 0, 0); var cells = layer.TileMapData;
        Reject<ArgumentException>(() => layer.SetCellsTerrainPath([default, new(100, 100)], 0, 0)); Reject<ArgumentException>(() => layer.SetCellsTerrainPath([default, default], 0, 0));
        Reject<OverflowException>(() => layer.SetCellsTerrainConnect([new(int.MaxValue, int.MaxValue)], 0, 0)); Reject<ArgumentOutOfRangeException>(() => layer.SetCellsTerrainConnect([default], 0, 1));
        Reject<ArgumentNullException>(() => layer.SetCellsTerrainConnect(null!, 0, 0)); Check(layer.TileMapData.SequenceEqual(cells), "Rejected paint preserves authored cells");
        layer.SetCellsTerrainConnect([], 0, 0); Check(layer.TileMapData.SequenceEqual(cells), "Empty input is quiet");
        using var missing = new TileMapLayer(); Reject<InvalidOperationException>(() => missing.SetCellsTerrainConnect([default], 0, 0));
    }
    private static void Weights()
    {
        using var library = new Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal, TerrainMode.MatchSides, false); using var layer = new TileMapLayer { TileSet = library.Set }; var first = library.Atlas.GetTileData(default, 0);
        var alt = library.Atlas.CreateAlternativeTile(default); var second = library.Atlas.GetTileData(default, alt); second.TerrainSet = 0; second.Terrain = 0; foreach (var bit in new[] { CellNeighbor.RightSide, CellNeighbor.BottomSide, CellNeighbor.LeftSide, CellNeighbor.TopSide }) second.SetTerrainPeeringBit(bit, 0);
        layer.SetCell(default, 0, default(Vector2i)); first.Probability = 0; second.Probability = 1; for (var i = 0; i < 64; i++) { layer.SetCellsTerrainConnect([default], 0, 0); Check(layer.GetCellAlternativeTile(default) == alt, "Zero weight cannot displace a positive alternative"); }
        second.Probability = 0; layer.SetCellsTerrainConnect([default], 0, 0); Check(layer.GetCellAlternativeTile(default) == 0, "All-zero fallback uses first sorted candidate");
        first.Probability = 1; second.Probability = 3; var other = 0; for (var i = 0; i < 512; i++) { layer.SetCellsTerrainConnect([default], 0, 0); if (layer.GetCellAlternativeTile(default) == alt) other++; }
        Check(other > 280 && other < 460, "Weighted selection distribution");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
