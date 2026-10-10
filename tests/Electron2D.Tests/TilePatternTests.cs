using System.Text.Json;
using Electron2D;

internal static partial class TilePatternTests
{
    internal static void Run() { Metadata(); Reference(); Failures(); Consumers(); Console.WriteLine("Tile patterns passed: 144 captures/576 placements, storage, failures, CPU/GPU consumers."); }
    private static void Metadata()
    {
        using var pattern = new TileMapPattern(); Check(pattern.IsEmpty() && pattern.GetSize() == default, "Pattern defaults");
        Check(!pattern.HasCell(default) && pattern.GetCellSourceID(default) == -1 && pattern.GetCellAtlasCoords(default) == new Vector2i(-1, -1) && pattern.GetCellAlternativeTile(default) == -1, "Missing cell sentinels");
        var notifications = 0; pattern.Changed += _ => notifications++;
        pattern.SetCell(new(2, 3)); pattern.SetCell(new(2, 3)); Check(notifications == 2 && !pattern.IsEmpty() && pattern.HasCell(new(2, 3)) && pattern.GetSize() == new Vector2i(3, 4), "Explicit empty cells and repeated notification");
        pattern.SetSize(new(10, 9)); pattern.RemoveCell(new(2, 3), false); Check(pattern.IsEmpty() && pattern.GetSize() == new Vector2i(10, 9), "Retained margins");
        pattern.SetCell(new(1, 2), 4, new(3, 5), TileSetAtlasSource.TransformTranspose | 2); pattern.SetSize(new(5, 6)); pattern.SetSize(new(2, 3));
        Reject<ArgumentOutOfRangeException>(() => pattern.SetSize(new(1, 3))); Reject<ArgumentOutOfRangeException>(() => pattern.SetSize(new(-1, 2)));
        using var copy = (TileMapPattern)pattern.Duplicate(); copy.RemoveCell(new(1, 2), true); Check(copy.IsEmpty() && copy.GetSize() == default && !pattern.IsEmpty(), "Independent copied cells");
        var cells = pattern.GetUsedCells(); cells[0] = default; Check(pattern.HasCell(new(1, 2)), "Copied coordinate exports");
        using var set = new TileSet(); using var other = new TileSet(); Check(set.AddPattern(pattern) == 0 && other.AddPattern(pattern) == 0, "Shared borrowed patterns");
        using var second = new TileMapPattern(); second.SetCell(default); Check(set.AddPattern(second, 0) == 0 && set.GetPattern(1) == pattern, "Ordered insertion");
        using var outside = new TileMapPattern(); outside.SetCell(default);
        Reject<ArgumentException>(() => set.AddPattern(pattern)); Reject<ArgumentOutOfRangeException>(() => set.GetPattern()); Reject<ArgumentOutOfRangeException>(() => set.AddPattern(outside, 5));
        using var shallow = (TileSet)set.Duplicate(); using var deep = (TileSet)set.Duplicate(true);
        Check(shallow.GetPattern(1) == pattern && deep.GetPattern(1) != pattern && deep.GetPattern(1).GetSize() == pattern.GetSize(), "Resource duplication modes");
        set.RemovePattern(0); Check(!second.IsDisposed && set.GetPattern(0) == pattern, "Removal does not dispose borrowed pattern");
        pattern.RemoveCell(new(1, 2), false); Check(set.GetPattern(0).IsEmpty(), "Stored patterns may become empty after insertion");
        second.Dispose(); Check(shallow.GetPatternsCount() == 1, "Disposed borrowed pattern removed from all observing lists");
        pattern.Dispose(); Check(set.GetPatternsCount() == 0 && other.GetPatternsCount() == 0 && shallow.GetPatternsCount() == 0, "Borrowed pattern lifetime cleanup");
        Reject<ObjectDisposedException>(() => pattern.GetSize());
    }
    private static void Reference()
    {
        using var fixture = JsonDocument.Parse(typeof(TilePatternTests).Assembly.GetManifestResourceStream("TestTiles.pattern-reference.json")!);
        var cases = fixture.RootElement.GetProperty("cases"); Check(cases.GetArrayLength() == 144, "Grid family oracle");
        foreach (var c in cases.EnumerateArray())
        {
            using var set = new TileSet { TileShape = (TileShape)c.GetProperty("shape").GetInt32(), TileLayout = (TileLayout)c.GetProperty("layout").GetInt32(), TileOffsetAxis = (TileOffsetAxis)c.GetProperty("axis").GetInt32() };
            using var layer = new TileMapLayer { TileSet = set }; var selected = c.GetProperty("selection").EnumerateArray().Select(Vector).ToArray();
            for (var i = 0; i < selected.Length - 1; i++) layer.SetCell(selected[i], i + 1, new(i, 2), i * 4096);
            using var pattern = layer.GetPattern(selected); Check(pattern.GetSize() == Vector(c.GetProperty("size")), "Captured extent");
            var rows = c.GetProperty("cells").EnumerateArray().ToArray(); Check(pattern.GetUsedCells().Length == rows.Length, "Captured selected count");
            foreach (var row in rows) { var coords = Vector(row); Check(pattern.GetCellSourceID(coords) == row[2].GetInt32() && pattern.GetCellAtlasCoords(coords) == new Vector2i(row[3].GetInt32(), row[4].GetInt32()) && pattern.GetCellAlternativeTile(coords) == row[5].GetInt32(), "Captured identities"); }
            foreach (var placement in c.GetProperty("placements").EnumerateArray())
            {
                var anchor = Vector(placement.GetProperty("anchor"));
                foreach (var row in placement.GetProperty("mapped").EnumerateArray()) Check(layer.MapPattern(anchor, Vector(row), pattern) == new Vector2i(row[2].GetInt32(), row[3].GetInt32()), "Parity mapping");
                layer.Clear(); layer.SetCell(anchor, 20, new(1, 1)); layer.SetPattern(anchor, pattern);
                var placed = placement.GetProperty("placed").EnumerateArray().ToArray(); Check(layer.GetUsedCells().Length == placed.Length, "Placed count including erasure");
                foreach (var row in placed) { var coords = Vector(row); Check(layer.GetCellSourceID(coords) == row[2].GetInt32() && layer.GetCellAtlasCoords(coords) == new Vector2i(row[3].GetInt32(), row[4].GetInt32()) && layer.GetCellAlternativeTile(coords) == row[5].GetInt32(), "Placed identities"); }
            }
        }
    }
    private static void Failures()
    {
        using var set = new TileSet(); using var layer = new TileMapLayer { TileSet = set }; using var pattern = new TileMapPattern();
        pattern.SetCell(default, 0, Vector2i.Zero, 0); pattern.SetCell(new(1, 0), 0, Vector2i.Zero, 0);
        Reject<OverflowException>(() => layer.SetPattern(new(int.MaxValue, 0), pattern)); Check(layer.GetUsedCells().Length == 0, "Overflow preflight before mutation");
        Reject<ArgumentOutOfRangeException>(() => pattern.SetCell(new(-1, 0))); Reject<OverflowException>(() => pattern.SetCell(new(int.MaxValue, 0)));
        Reject<ArgumentOutOfRangeException>(() => pattern.SetCell(default, 65535)); Reject<ArgumentException>(() => pattern.RemoveCell(new(8, 8), true));
        Reject<ArgumentException>(() => layer.MapPattern(default, new(7, 7), pattern)); Reject<OverflowException>(() => layer.GetPattern([new(int.MinValue, 0), new(int.MaxValue, 0)]));
        using var missing = new TileMapLayer(); Reject<InvalidOperationException>(() => missing.GetPattern([])); Reject<InvalidOperationException>(() => missing.SetPattern(default, pattern));
        using var empty = new TileMapPattern(); Reject<ArgumentException>(() => set.AddPattern(empty));
        var published = 0; set.Changed += _ => published++; Action<ElectronObject> schemaFail = _ => throw new InvalidOperationException("Expected pattern list listener failure."); set.PropertyListChanged += schemaFail;
        Reject<InvalidOperationException>(() => set.AddPattern(pattern)); set.PropertyListChanged -= schemaFail; Check(set.GetPatternsCount() == 1 && published == 1, "List commit and Changed survive schema listener failure");
        pattern.SetCell(new(2, 0), 0, new(-1, 0), 0); Reject<ArgumentOutOfRangeException>(() => layer.SetPattern(default, pattern)); Check(layer.GetUsedCells().Length == 0, "All stored identities preflight"); pattern.RemoveCell(new(2, 0), true);
        Action fail = () => throw new InvalidOperationException("Expected pattern callback failure."); layer.Changed += fail; Reject<InvalidOperationException>(() => layer.SetPattern(default, pattern)); layer.Changed -= fail;
        Check(layer.GetUsedCells().Length == 2, "Complete committed edit before listener failure");
        var events = 0; layer.Changed += () => events++; layer.SetPattern(default, pattern); Check(events == 0, "Unchanged placement is quiet");
        var sum = 0; for (var i = 0; i < 32; i++) sum += layer.MapPattern(default, default, pattern).X;
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) sum += pattern.GetCellSourceID(default) + pattern.GetCellAlternativeTile(default) + pattern.GetSize().X + (pattern.HasCell(default) ? 1 : 0) + layer.MapPattern(default, default, pattern).X;
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes && sum > 0, "Warmed scalar and mapping zero allocation");
        var descriptor = (PropertyDescriptor<TileMapPattern, int[]>)pattern.GetPropertyList().Single(p => p.Name == "Cells");
        Reject<ArgumentException>(() => descriptor.SetValue(pattern, [1]));
        Reject<ArgumentException>(() => descriptor.SetValue(pattern, [0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0]));
        Reject<ArgumentOutOfRangeException>(() => descriptor.SetValue(pattern, [-1, 0, 0, 0, 0, 0])); Check(pattern.GetUsedCells().Length == 2, "Malformed stored edits preserve pattern");
    }
    private static Vector2i Vector(JsonElement row) => new(row[0].GetInt32(), row[1].GetInt32());
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
