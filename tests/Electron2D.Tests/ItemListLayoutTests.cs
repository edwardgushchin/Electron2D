using Electron2D;

internal static class ItemListLayoutTests
{
    internal static void Run()
    {
        using var list = new ItemList { Size = new(40, 100), AutoWidth = true };
        const string text = "A long inventory item label";
        list.AddItem(text); list.ForceUpdateListSize();
        var font = list.GetThemeFont("font") ?? ThemeDB.FallbackFont!;
        var natural = font.GetStringSize(text, fontSize: list.GetThemeFontSize("font_size")).X;
        var expected = natural + list.GetThemeConstant("h_separation") + list.GetThemeStyleBox("panel")!.GetMinimumSize().X;
        Check(list.GetMinimumSize().X >= expected - 1, $"Automatic width uses natural text width: {list.GetMinimumSize().X}, expected {expected}.");
        list.AutoHeight = true; list.ForceUpdateListSize();
        Check(list.GetMinimumSize().Y >= list.GetItemRect(0, false).Size.Y, "Automatic height includes the measured row.");
        Grid(); Text(); Scroll(); Recovery(); FileState();
        Console.WriteLine("ItemList natural measurement, 960 icon grids, text/scroll/RTL/recovery/file-state checks passed.");
    }

    private static void Grid()
    {
        using var a = new TestTexture(12, 8); using var b = new TestTexture(6, 16);
        var cases = 0;
        foreach (var columns in new[] { 0, 1, 3, 8 }) foreach (var same in new[] { false, true })
                foreach (var fixedColumn in new[] { 0, 20 }) foreach (var allocation in new[] { Vector2i.Zero, new Vector2i(10, 6), new Vector2i(10, 0) })
                        foreach (var scale in new[] { -1f, 0f, .5f, 1f, 2f }) foreach (var rtl in new[] { false, true }) foreach (var top in new[] { false, true })
                                {
                                    using var list = new ItemList { Size = new(2000, 2000), MaxColumns = columns, SameColumnWidth = same, FixedColumnWidth = fixedColumn, FixedIconSize = allocation, IconScale = scale, IconDisplayMode = top ? ItemList.IconMode.Top : ItemList.IconMode.Left, LayoutDirection = rtl ? LayoutDirection.RTL : LayoutDirection.LTR };
                                    for (var i = 0; i < 5; i++) { list.AddIconItem((i & 1) == 0 ? a : b); list.SetItemIconTransposed(i, i == 2); }
                                    list.ForceUpdateListSize();
                                    var sizes = Enumerable.Range(0, 5).Select(i =>
                                    {
                                        var v = (i & 1) == 0 ? new Vector2(12, 8) : new Vector2(6, 16); if (i == 2) v = new(v.Y, v.X);
                                        if (allocation.X > 0 && allocation.Y > 0) v = allocation;
                                        v *= scale; v.X = fixedColumn > 0 ? fixedColumn : v.X;
                                        return v + new Vector2(list.GetThemeConstant("h_separation"), list.GetThemeConstant("v_separation"));
                                    }).ToArray();
                                    if (same) { var widest = Math.Max(list.GetThemeConstant("h_separation"), sizes.Max(v => v.X)); for (var i = 0; i < sizes.Length; i++) sizes[i].X = widest; }
                                    var count = columns == 0 ? 5 : columns; var offset = list.GetThemeStyleBox("panel")!.GetOffset(); var y = 0f;
                                    for (var start = 0; start < 5; start += count)
                                    {
                                        var row = sizes.Skip(start).Take(count).ToArray(); var height = Math.Max(0, row.Max(v => v.Y)); var x = 0f;
                                        for (var j = 0; j < row.Length; j++) { var rect = list.GetItemRect(start + j, false); Check(rect.Position == offset + new Vector2(x, y) && rect.Size == new Vector2(row[j].X, height), $"Icon grid case {cases}, item{start + j}: {rect}; expected{offset + new Vector2(x, y)}, {row[j].X}x{height}."); x += row[j].X; }
                                        y += height;
                                    }
                                    if (columns is 0 or 8) Check(list.GetItemRect(4) == list.GetItemRect(4, false), "An incomplete or unlimited final row has no artificial last-column expansion.");
                                    cases++;
                                }
        Check(cases == 960, "The complete icon grid matrix ran.");
    }

    private static void Text()
    {
        using var list = new ItemList { Size = new(300, 200), IconDisplayMode = ItemList.IconMode.Top, FixedColumnWidth = 40, MaxTextLines = 1 };
        list.AddItem("A"); var one = list.GetItemRect(0, false); list.MaxTextLines = 3;
        var reserved = list.GetItemRect(0, false); Check(reserved.Size.Y == one.Size.Y + 2 * list.GetThemeConstant("line_separation"), "Top-icon text reserves the configured line spacing even for a short label.");
        list.SetItemText(0, "one two three four five six seven"); var wrapped = list.GetItemRect(0, false);
        Check(wrapped.Size.Y > reserved.Size.Y && wrapped.Size.X == 40 + list.GetThemeConstant("h_separation"), "Fixed text width wraps into the permitted top-icon lines, with outer separation retained.");
        list.MaxTextLines = 1; Check(list.GetItemRect(0, false).Size.Y < wrapped.Size.Y, "The line cap limits vertical content.");
        list.FixedColumnWidth = 0; list.AutoWidth = true; list.ForceUpdateListSize();
        Check(list.GetMinimumSize().X > 100, "Unconstrained top captions measure their natural width.");
    }

    private static void Scroll()
    {
        foreach (var rtl in new[] { false, true })
        {
            using var icon = new TestTexture(30, 20);
            using var list = new ItemList { Size = new(60, 40), MaxColumns = 3, WraparoundItems = false, LayoutDirection = rtl ? LayoutDirection.RTL : LayoutDirection.LTR };
            for (var i = 0; i < 9; i++) list.AddIconItem(icon);
            using var viewport = new SubViewport { Size = new(400, 400) }; viewport.AddChild(list); using var tree = new SceneTree(viewport);
            list.ForceUpdateListSize(); var v = list.GetVScrollBar(); var h = list.GetHScrollBar(); var margins = list.GetThemeStyleBox("panel")!.GetMinimumSize();
            Check(v.Visible && h.Visible, "Two-axis overflow publishes both real scrollbars.");
            Near(v.Page, Math.Max(0, list.Size.Y - margins.Y - h.GetCombinedMinimumSize().Y), "Horizontal bar consumes vertical page");
            Near(h.Page, Math.Max(0, list.Size.X - margins.X - v.GetCombinedMinimumSize().X), "Vertical bar consumes horizontal page");
            list.Current = 8; list.CenterOnCurrent(); Check(v.Value > 0 && (rtl ? h.Value < 0 : h.Value > 0), "Centering reaches the current item on both signed horizontal profiles.");
            var observed = 0; v.ValueChanged += _ => { list.GetItemRect(0); observed++; }; h.ValueChanged += _ => { list.GetItemRect(0); observed++; };
            list.Size = new(400, 400); list.ForceUpdateListSize(); Check(!v.Visible && !h.Visible && v.Value == 0 && h.Value == 0, "Enlargement retires overflow and resets stale signed scroll values.");
            Check(observed > 0, "Scroll clamp observers can read the newly prepared geometry without recursive measurement.");
            list.Clear(); list.ForceUpdateListSize(); Check(!v.Visible && !h.Visible, "An empty model has no stale content extent.");
        }
    }

    private static void Recovery()
    {
        using var icon = new TestTexture(20, 10); using var list = new ItemList { Size = new(200, 100) }; list.AddIconItem(icon);
        icon.Read = () => throw new InvalidOperationException("expected measure failure");
        Reject<InvalidOperationException>(list.ForceUpdateListSize); icon.Read = null;
        Check(list.GetItemRect(0, false).Size.X == 20 + list.GetThemeConstant("h_separation"), "Failed measurement remains dirty and retries successfully.");
        icon.Read = () => list.GetItemRect(0); list.FixedColumnWidth = 25; Reject<InvalidOperationException>(list.ForceUpdateListSize); icon.Read = null; list.ForceUpdateListSize();
        icon.Read = () => { icon.Read = null; list.FixedColumnWidth = 30; }; list.FixedColumnWidth = 26; Reject<InvalidOperationException>(list.ForceUpdateListSize);
        Check(list.GetItemRect(0, false).Size.X == 30 + list.GetThemeConstant("h_separation"), "Mutation from a virtual measurement cannot publish a stale prepared layout.");
        list.IconScale = float.MaxValue; Reject<InvalidOperationException>(list.ForceUpdateListSize); list.IconScale = 1; list.ForceUpdateListSize();
        icon.Dispose(); list.ForceUpdateListSize(); Check(list.GetItemIcon(0) is null, "Disposing a borrowed icon clears its model edge and reflows safely.");
        using var viewport = new SubViewport { Size = new(320, 240) }; var attached = new ItemList { Size = new(100, 100) }; viewport.AddChild(attached);
        using var source = new TestTexture(20, 10); attached.AddIconItem(source); using var tree = new SceneTree(viewport);
        var old = attached.GetItemRect(0, false).Size.X;
        Task.Run(() => source.Resize(28)).GetAwaiter().GetResult();
        Check(attached.GetItemRect(0, false).Size.X == old + 8, "A worker resource edit queues owner-side icon measurement.");
        Task.Run(source.Dispose).GetAwaiter().GetResult();
        Check(attached.GetItemIcon(0) is null && attached.GetItemRect(0, false).Size.X == attached.GetThemeConstant("h_separation"), "Worker disposal retires borrowed icon edges on the scene owner.");
        Task.Run(() => Reject<InvalidOperationException>(attached.ForceUpdateListSize)).GetAwaiter().GetResult();

    }

    private static void FileState()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-item-layout-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { Author(path); Read(path); } finally { File.Delete(path); }
    }
    internal static void Author(string path)
    {
        using var list = new ItemList { AutoWidth = true, AutoHeight = true, FixedColumnWidth = 40, FixedIconSize = new(12, 10), IconScale = .5f, MaxColumns = 3, MaxTextLines = 2, SameColumnWidth = true, HintMode = VerticalScrollHintMode.Both, TileScrollHint = true, WraparoundItems = false, TextOverrunBehavior = TextOverrunBehavior.TrimWordEllipsis, IconDisplayMode = ItemList.IconMode.Top };
        list.AddItem("Bundled inventory label"); using var packed = new PackedScene(); packed.Pack(list); ResourceSaver.Save(packed, path, SaverFlags.BundleResources);
        Console.WriteLine("Authored ItemList layout file.");
    }
    internal static void Read(string path)
    {
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var restored = (ItemList)loaded.Instantiate(); restored.ForceUpdateListSize();
        Check(restored.AutoWidth && restored.AutoHeight && restored.FixedColumnWidth == 40 && restored.FixedIconSize == new Vector2i(12, 10) && restored.IconScale == .5f && restored.MaxColumns == 3 && restored.MaxTextLines == 2 && restored.SameColumnWidth && restored.HintMode == VerticalScrollHintMode.Both && restored.TileScrollHint && !restored.WraparoundItems && restored.TextOverrunBehavior == TextOverrunBehavior.TrimWordEllipsis && restored.GetItemText(0) == "Bundled inventory label", "All stored layout policies survive a fresh typed file graph.");
        Console.WriteLine("Restored ItemList layout file and executable measured geometry.");
    }

    private sealed class TestTexture(int width, int height) : Texture
    {
        internal Action? Read;
        internal void Resize(int value) { width = value; EmitChanged(); }
        public override int GetWidth() { Read?.Invoke(); return width; }
        public override int GetHeight() => height;
    }
    private static void Near(double actual, double expected, string message) => Check(Math.Abs(actual - expected) < .01, $"{message}: {actual}, expected {expected}.");
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
