using Electron2D;
using System.Diagnostics;

internal static class GUIPrimitivesTests
{
    internal static void Run()
    {
        Defaults(); GeometryAndThemes(); ContainerLayout(); PackingAndInput(); GuardsAndWarm();
        Console.WriteLine("GUI primitives: color/defaults, exact centered integer style rectangles, theme inheritance/live edits, containers/input, fresh stored scenes, owner/capture guards and 64 warm active/idle frames=0 managed bytes passed.");
    }
    private static void Defaults()
    {
        using var color = new ColorRect(); using var horizontal = new HSeparator(); using var vertical = new VSeparator();
        Check(color.Color == Colors.White && color.GetMinimumSize() == Vector2.Zero && color.MouseFilter == MouseFilter.Stop, "Color rectangle defaults.");
        Check(typeof(Separator).IsAbstract && horizontal.GetMinimumSize() == new Vector2(3, 4) && vertical.GetMinimumSize() == new Vector2(4, 3), "Abstract separator and built-in axis defaults.");
        Check(horizontal.GetThemeStyleBox("separator") is StyleBoxLine { Vertical: false } && vertical.GetThemeStyleBox("separator") is StyleBoxLine { Vertical: true }, "Actual owned theme styles.");
        var original = color.Color; Reject<ArgumentException>(() => color.Color = new(float.NaN, 0, 0)); Check(color.Color == original, "Invalid color write is atomic.");
        color.Color = new(4, -2, .5f, .25f); Check(color.Color.R == 4 && color.Color.G == -2, "Finite HDR/signed channels are authored without clamping.");
    }
    private sealed class ProbeStyle : StyleBox
    {
        internal Rect2 Last; internal int Draws;
        internal ProbeStyle(float x, float y) { ContentMarginLeft = x; ContentMarginTop = y; ContentMarginRight = ContentMarginBottom = 0; }
        protected override void OnDraw(CanvasItem item, Rect2 rect) { Last = rect; Draws++; }
    }
    private static void GeometryAndThemes()
    {
        using var style = new ProbeStyle(5.9f, 7.9f); using var horizontal = new HSeparator { Size = new(100.9f, 40.9f) }; using var vertical = new VSeparator { Size = new(40.9f, 100.9f) };
        horizontal.AddThemeStyleBoxOverride("separator", style); vertical.AddThemeStyleBoxOverride("separator", style);
        Record(horizontal); Check(style.Last == new Rect2(0, 16, 100, 7), "Horizontal dimensions and style minimum truncate before integer centering.");
        Record(vertical); Check(style.Last == new Rect2(17, 0, 5, 100), "Vertical dimensions and style minimum truncate before integer centering.");
        style.ContentMarginTop = 51; horizontal.Size = new(100, 20); Record(horizontal); Check(style.Last == new Rect2(0, -15, 100, 51), "Oversized style uses signed truncation toward zero.");
        horizontal.AddThemeConstantOverride("separation", -5); Check(horizontal.GetMinimumSize() == new Vector2(3, -5) && horizontal.GetCombinedMinimumSize() == new Vector2(3, 0), "Intrinsic theme values stay signed; combined minimum clamps to zero.");
        using var theme = new Theme(); theme.SetTypeVariation("Rule", "HSeparator"); theme.SetConstant("separation", "Rule", 9); theme.SetStyleBox("separator", "Rule", style);
        var parent = new Control { Theme = theme }; var inherited = new HSeparator { ThemeTypeVariation = "Rule", Size = new(100, 30) }; parent.AddChild(inherited); using var tree = new SceneTree(parent); tree.ProcessFrame(0);
        Check(inherited.GetMinimumSize() == new Vector2(3, 9), "Variation theme lookup follows the native chain.");
        Task.Run(() => theme.SetConstant("separation", "Rule", 13)).GetAwaiter().GetResult(); tree.ProcessFrame(0); Check(inherited.GetMinimumSize().Y == 13, "Worker theme changes reach owner layout.");
        style.ContentMarginTop = 5; Record(inherited); var before = style.Draws; Task.Run(() => style.ContentMarginTop = 11).GetAwaiter().GetResult(); tree.ProcessFrame(0); inherited.PrepareCanvas(); Check(style.Draws > before && style.Last.Size.Y == 11, "Borrowed style content refreshes retained drawing.");
        inherited.Dispose(); Check(!style.IsDisposed && !theme.IsDisposed, "Separators borrow theme/style ownership.");
    }
    private static void ContainerLayout()
    {
        var box = new VBoxContainer { Size = new(200, 100) };
        var first = new ColorRect { Name = "First", CustomMinimumSize = new(50, 20) };
        var rule = new HSeparator(); rule.AddThemeConstantOverride("separation", 8);
        var last = new ColorRect { Name = "Last", CustomMinimumSize = new(50, 20) };
        box.AddChild(first); box.AddChild(rule); box.AddChild(last); using var tree = new SceneTree(box); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(rule.Size.Y == 8 && last.Position.Y >= rule.Position.Y + rule.Size.Y, "Container consumes separator intrinsic spacing.");
        var previous = last.Position.Y; rule.AddThemeConstantOverride("separation", 16); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(rule.Size.Y == 16 && last.Position.Y > previous, "Live theme spacing rearranges container siblings.");
    }
    private static void PackingAndInput()
    {
        var root = new SubViewport { Size = new(200, 120) }; var color = new ColorRect { Name = "Fill", Size = new(150, 60), Color = Colors.Red };
        root.AddChild(color); color.Owner = root; var horizontal = new HSeparator { Name = "H", Position = new(0, 80), Size = new(150, 10) }; root.AddChild(horizontal); horizontal.Owner = root;
        var vertical = new VSeparator { Name = "V", Position = new(160, 0), Size = new(10, 100) }; root.AddChild(vertical); vertical.Owner = root;
        using var scene = new PackedScene(); scene.Pack(root); using var copy = (SubViewport)scene.Instantiate();
        Check(copy.GetNode("Fill") is ColorRect { Color: var red } && red == Colors.Red && copy.GetNode("H") is HSeparator && copy.GetNode("V") is VSeparator, "Concrete classes and stored color survive packing.");
        var probe = new UnhandledProbe(); root.AddChild(probe); using var tree = new SceneTree(root); tree.ProcessFrame(0); using var input = new InputEventMouseButton { Position = new(20, 20), ButtonIndex = MouseButton.Left, Pressed = true };
        root.PushInput(input, true); Check(probe.Received == 0, "Ordinary stopping background input prevents unhandled delivery."); color.MouseFilter = MouseFilter.Ignore; root.PushInput(input, true); Check(probe.Received == 1, "Decorative input policy permits unhandled delivery.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-primitives-" + Guid.NewGuid() + ".e2dscene");
        try
        {
            ResourceSaver.Save(scene, path, SaverFlags.Compress | SaverFlags.SaveBigEndian);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(GUIPrimitivesTests).Assembly.Location);
            start.Environment.Remove("ELECTRON2D_TEST_GUI_PRIMITIVES"); start.Environment["ELECTRON2D_TEST_GUI_PRIMITIVES_CHILD"] = path;
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh GUI primitives."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh GUI primitives passed"), "Fresh file: " + error.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var root = (SubViewport)scene.Instantiate(); scene.Dispose(); using var tree = new SceneTree(root); tree.ProcessFrame(0);
        var color = (ColorRect)root.GetNode("Fill"); var h = (HSeparator)root.GetNode("H"); var v = (VSeparator)root.GetNode("V");
        Check(color.Color == Colors.Red && h.GetMinimumSize() == new Vector2(3, 4) && v.GetMinimumSize() == new Vector2(4, 3), "Fresh static factories and theme chain."); Record(color); Record(h); Record(v); Console.WriteLine("Fresh GUI primitives passed");
    }
    private sealed class UnhandledProbe : Node
    {
        internal int Received;
        internal UnhandledProbe() => UnhandledInputEnabled = true;
        protected override void OnUnhandledInput(InputEvent input) => Received++;
    }
    private sealed class CaptureColor : ColorRect
    {
        internal bool Guarded;
        protected override Func<Node> CreateSceneInstanceFactory() => CreateCapture;
        private static Node CreateCapture() => new CaptureColor();
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
        {
            foreach (var property in base.GetPropertyDescriptors()) yield return property;
            yield return new PropertyDescriptor<CaptureColor, int>("Guard", c => c.ReadGuard(), (c, v) => { }, _ => 0, stored: true);
        }
        private int ReadGuard() { Reject<InvalidOperationException>(() => Color = Colors.Blue); Guarded = true; return 0; }
    }
    private static void GuardsAndWarm()
    {
        using var capture = new CaptureColor(); using var scene = new PackedScene(); scene.Pack(capture); Check(capture.Guarded && capture.Color == Colors.White, "Capture rejects authored fill mutation.");
        var root = new Control { Size = new(200, 120) }; var color = new ColorRect { Size = new(100, 60) }; var h = new HSeparator { Size = new(150, 20) }; var v = new VSeparator { Size = new(20, 100) }; root.AddChild(color); root.AddChild(h); root.AddChild(v); using var tree = new SceneTree(root); tree.ProcessFrame(0);
        Reject<InvalidOperationException>(() => Task.Run(() => color.Color = Colors.Blue).GetAwaiter().GetResult());
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        for (var i = 0; i < 64; i++) { color.Color = (i & 1) == 0 ? Colors.Red : Colors.Blue; tree.ProcessFrame(0); RecordTree(root, vertices, batches); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { color.Color = (i & 1) == 0 ? Colors.Red : Colors.Blue; tree.ProcessFrame(0); RecordTree(root, vertices, batches); }
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Warm active color/style recording makes zero managed allocations.");
        before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) tree.ProcessFrame(0); Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Idle primitives make zero managed allocations.");
        color.Dispose(); Reject<ObjectDisposedException>(() => _ = color.Color);
    }
    private static void Record(CanvasItem item) { item.InvalidateCanvas(); item.PrepareCanvas(); }
    private static void RecordTree(Control root, List<CanvasVertex> vertices, List<CanvasBatch> batches)
    { root.PrepareCanvas(); for (var i = 0; i < root.ChildCount; i++) ((CanvasItem)root.GetChild(i)).PrepareCanvas(); vertices.Clear(); batches.Clear(); root.AppendCanvas(vertices, batches, Transform.Identity); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
