using System.Text.Json;
using Electron2D;

internal static partial class TileProxyTests
{
    internal static void Run() { Reference(); Metadata(); Consumers(); Console.WriteLine("Tile proxies passed: all levels, validity/precedence/single-hop mapping, storage, failures and CPU/GPU consumers."); }
    private static void Reference()
    {
        using var fixture = JsonDocument.Parse(typeof(TileProxyTests).Assembly.GetManifestResourceStream("TestTiles.proxy-reference.json")!); var root = fixture.RootElement;
        using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal); var set = library.Set; var atlas = library.Atlas;
        var probes = root.GetProperty("probes").EnumerateArray().Select(row => row.EnumerateArray().Select(x => x.GetInt32()).ToArray()).ToArray(); var stages = root.GetProperty("stages"); Check(stages.GetArrayLength() == 19, "Complete proxy fixture phases");
        foreach (var stage in stages.EnumerateArray())
        {
            var action = stage.GetProperty("action"); var a = action[1].EnumerateArray().Select(x => x.GetInt32()).ToArray();
            switch (action[0].GetString())
            {
                case "source": set.SetSourceLevelTileProxy(a[0], a[1]); break;
                case "coords": set.SetCoordsLevelTileProxy(a[0], new(a[1], a[2]), a[3], new(a[4], a[5])); break;
                case "alternative": set.SetAlternativeLevelTileProxy(a[0], new(a[1], a[2]), a[3], a[4], new(a[5], a[6]), a[7]); break;
                case "remove_source": set.RemoveSourceLevelTileProxy(a[0]); break;
                case "remove_coords": set.RemoveCoordsLevelTileProxy(a[0], new(a[1], a[2])); break;
                case "remove_alternative": set.RemoveAlternativeLevelTileProxy(a[0], new(a[1], a[2]), a[3]); break;
                case "cleanup": set.CleanupInvalidTileProxies(); break;
                case "clear": set.ClearTileProxies(); break;
                case "add_alt": atlas.CreateAlternativeTile(default, 1); break;
                case "rename": set.SetSourceID(0, 5); break;
            }
            var index = 0;
            foreach (var row in stage.GetProperty("mapped").EnumerateArray())
            { var probe = probes[index++]; var expected = (row[0].GetInt32(), new Vector2i(row[1].GetInt32(), row[2].GetInt32()), row[3].GetInt32()); Check(set.MapTileProxy(probe[0], new(probe[1], probe[2]), probe[3]) == expected, "Pinned single-hop mapping"); }
            foreach (var row in stage.GetProperty("sources").EnumerateArray())
            { var id = row[0].GetInt32(); Check(set.HasSourceLevelTileProxy(id) == row[1].GetBoolean(), "Source membership"); if (row[1].GetBoolean()) Check(set.GetSourceLevelTileProxy(id) == row[2].GetInt32(), "Source destination"); }
            foreach (var row in stage.GetProperty("coords").EnumerateArray())
            { var id = row[0].GetInt32(); var coords = new Vector2i(row[1].GetInt32(), row[2].GetInt32()); Check(set.HasCoordsLevelTileProxy(id, coords) == row[3].GetBoolean(), "Coordinate membership"); if (row[3].GetBoolean()) Check(set.GetCoordsLevelTileProxy(id, coords) == (row[4].GetInt32(), new Vector2i(row[5].GetInt32(), row[6].GetInt32())), "Coordinate destination"); }
            foreach (var row in stage.GetProperty("alternatives").EnumerateArray())
            { var id = row[0].GetInt32(); var coords = new Vector2i(row[1].GetInt32(), row[2].GetInt32()); var alt = row[3].GetInt32(); Check(set.HasAlternativeLevelTileProxy(id, coords, alt) == row[4].GetBoolean(), "Alternative membership"); if (row[4].GetBoolean()) Check(set.GetAlternativeLevelTileProxy(id, coords, alt) == (row[5].GetInt32(), new Vector2i(row[6].GetInt32(), row[7].GetInt32()), row[8].GetInt32()), "Alternative destination"); }
        }
    }
    private static void Metadata()
    {
        using var set = new TileSet(); var changes = 0; set.Changed += _ => changes++;
        set.SetSourceLevelTileProxy(100, 0); set.SetSourceLevelTileProxy(100, 0); Check(changes == 2, "Repeated assignments notify");
        set.SetCoordsLevelTileProxy(101, new(1, 2), 0, default); set.SetAlternativeLevelTileProxy(102, new(3, 4), -1, 0, default, -1);
        var exported = set.GetCoordsLevelTileProxy(101, new(1, 2)); exported.SourceID = 99; exported.AtlasCoords = new(9, 9); Check(exported.SourceID == 99 && set.GetCoordsLevelTileProxy(101, new(1, 2)) == (0, Vector2i.Zero), "Returned tuple edits do not mutate tables");
        Reject<ArgumentOutOfRangeException>(() => set.SetSourceLevelTileProxy(-1, 5)); Reject<ArgumentOutOfRangeException>(() => set.SetSourceLevelTileProxy(5, -1));
        Reject<ArgumentOutOfRangeException>(() => set.SetCoordsLevelTileProxy(1, new(-1, 0), 0, default)); Reject<ArgumentOutOfRangeException>(() => set.SetAlternativeLevelTileProxy(1, default, -2, 0, default, 0));
        Reject<KeyNotFoundException>(() => set.GetSourceLevelTileProxy(9)); Reject<KeyNotFoundException>(() => set.GetCoordsLevelTileProxy(9, default)); Reject<KeyNotFoundException>(() => set.GetAlternativeLevelTileProxy(9, default, 0));
        Reject<KeyNotFoundException>(() => set.RemoveSourceLevelTileProxy(9)); Reject<KeyNotFoundException>(() => set.RemoveCoordsLevelTileProxy(9, default)); Reject<KeyNotFoundException>(() => set.RemoveAlternativeLevelTileProxy(9, default, 0));
        using var copy = (TileSet)set.Duplicate(); using var deep = (TileSet)set.Duplicate(true); using var replacement = new TileSet(); replacement.CopyFromResource(set);
        copy.ClearTileProxies(); Check(set.HasSourceLevelTileProxy(100) && deep.GetCoordsLevelTileProxy(101, new(1, 2)) == (0, Vector2i.Zero) && replacement.HasAlternativeLevelTileProxy(102, new(3, 4), -1), "Independent copied tables");
        foreach (var name in new[] { "SourceProxies", "CoordsProxies", "AlternativeProxies" })
        {
            var descriptor = (PropertyDescriptor<TileSet, int[]>)set.GetPropertyList().Single(p => p.Name == name); var before = descriptor.GetValue(set);
            Reject<ArgumentException>(() => descriptor.SetValue(set, [1])); Reject<ArgumentException>(() => descriptor.SetValue(set, before.Concat(before).ToArray())); Check(descriptor.GetValue(set).SequenceEqual(before), "Malformed/duplicate stored tables reject before replacement");
        }
        using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal); set.AddSource(library.Atlas, 0);
        var total = 0; for (var i = 0; i < 32; i++) total += ReadProxies(set);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) total += ReadProxies(set);
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes && total > 0, "Typed proxy queries allocate zero warmed owner bytes");
        set.SetSourceLevelTileProxy(0, 99); set.SetCoordsLevelTileProxy(0, default, 99, default); set.SetAlternativeLevelTileProxy(0, default, 0, 99, default, 0);
        Action<Resource> fail = _ => throw new InvalidOperationException("Expected cleanup listener failure."); set.Changed += fail; Reject<InvalidOperationException>(() => set.CleanupInvalidTileProxies()); set.Changed -= fail;
        Check(!set.HasSourceLevelTileProxy(0) && !set.HasCoordsLevelTileProxy(0, default) && !set.HasAlternativeLevelTileProxy(0, default, 0) && set.HasSourceLevelTileProxy(100), "All obsolete keys removed before callback failure, unresolved keys retained");
        var beforeClear = changes; set.ClearTileProxies(); set.ClearTileProxies(); Check(changes == beforeClear + 2, "Empty clear still notifies");
        set.Dispose(); Reject<ObjectDisposedException>(() => set.MapTileProxy(1, default, 0));
    }
    private static int ReadProxies(TileSet set) => set.GetSourceLevelTileProxy(100) + set.GetCoordsLevelTileProxy(101, new(1, 2)).SourceID
        + set.GetAlternativeLevelTileProxy(102, new(3, 4), -1).SourceID + (set.HasSourceLevelTileProxy(100) ? 1 : 0)
        + (set.HasCoordsLevelTileProxy(101, new(1, 2)) ? 1 : 0) + (set.HasAlternativeLevelTileProxy(102, new(3, 4), -1) ? 1 : 0)
        + set.MapTileProxy(100, default, 0).SourceID + set.MapTileProxy(101, new(1, 2), 0).SourceID + set.MapTileProxy(102, new(3, 4), -1).SourceID
        + set.MapTileProxy(0, default, TileSetAtlasSource.TransformTranspose).SourceID + set.MapTileProxy(300, default, 0).SourceID;
    private static void Paint(TileMapLayer layer, Vector2i coord, int source, Vector2i atlas, int alternative)
    { var mapped = layer.TileSet!.MapTileProxy(source, atlas, alternative); layer.SetCell(coord, mapped.SourceID, mapped.AtlasCoords, mapped.AlternativeTile); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
