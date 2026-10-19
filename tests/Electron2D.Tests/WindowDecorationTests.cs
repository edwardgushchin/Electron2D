using Path = System.IO.Path;
using Electron2D;

internal static partial class WindowDecorationTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException(typeof(T).Name + " expected."); }
    private static ImageTexture Icon(int width, int height, Color color)
    { using var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8); image.Fill(color); return ImageTexture.CreateFromImage(image); }
    private sealed class Skin : IDisposable
    {
        internal readonly Theme Theme = new();
        internal readonly ImageTexture Close = Icon(12, 10, Colors.Blue), Pressed = Icon(20, 16, Colors.Red);
        internal readonly StyleBoxFlat Focused = new() { BGColor = Colors.Green, ExpandMarginLeft = 5, ExpandMarginTop = 38, ExpandMarginRight = 5, ExpandMarginBottom = 5 };
        internal readonly StyleBoxFlat Unfocused = new() { BGColor = Colors.Yellow, ExpandMarginLeft = 5, ExpandMarginTop = 38, ExpandMarginRight = 5, ExpandMarginBottom = 5 };
        internal Skin()
        {
            Focused.ContentMarginLeft = 7; Focused.ContentMarginTop = 11; Focused.ContentMarginRight = 13; Focused.ContentMarginBottom = 17;
            Unfocused.ContentMarginLeft = 3; Unfocused.ContentMarginTop = 15; Unfocused.ContentMarginRight = 9; Unfocused.ContentMarginBottom = 8;
            Theme.SetStyleBox("embedded_border", "Window", Focused); Theme.SetStyleBox("embedded_unfocused_border", "Window", Unfocused);
            Theme.SetIcon("close", "Window", Close); Theme.SetIcon("close_pressed", "Window", Pressed);
            Theme.SetConstant("close_h_offset", "Window", 30); Theme.SetConstant("close_v_offset", "Window", 26); Theme.SetConstant("title_height", "Window", 44); Theme.SetConstant("resize_margin", "Window", 6); Theme.SetConstant("title_outline_size", "Window", 0);
            Theme.SetColor("title_color", "Window", Colors.White); Theme.SetColor("title_outline_modulate", "Window", Colors.Red); Theme.SetFont("title_font", "Window", null); Theme.SetFontSize("title_font_size", "Window", 16);
        }
        public void Dispose() { Theme.Dispose(); Close.Dispose(); Pressed.Dispose(); Focused.Dispose(); Unfocused.Dispose(); }
    }
    private static Vector2 ClosePoint(Window window) => (Vector2)window.Position + new Vector2(window.Size.X - window.GetThemeConstant("close_h_offset") + 6, -window.GetThemeConstant("close_v_offset") + 5);
    private static void Button(Viewport host, Vector2 point, bool pressed)
    { using var input = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0 }; host.PushInput(input, true); }
    private static void Motion(Viewport host, Vector2 point)
    { using var input = new InputEventMouseMotion { Position = point, ButtonMask = MouseButtonMask.Left }; host.PushInput(input, true); }
    internal static void RunManaged()
    {
        var defaults = ThemeDB.GetDefaultTheme();
        foreach (var name in new[] { "close", "close_pressed" }) Check(defaults.HasIcon(name, "Window") && defaults.GetIcon(name, "Window")!.GetSize() == new Vector2(16, 16), "Default Window has a real sized " + name + " icon.");
        foreach (var name in new[] { "embedded_border", "embedded_unfocused_border" }) Check(defaults.HasStyleBox(name, "Window"), "Default Window has " + name);
        Check(defaults.HasFont("title_font", "Window") && defaults.GetFontSize("title_font_size", "Window") == 16, "Default title font resolves its actual fallback.");
        using var skin = new Skin(); var host = new SubViewport { Size = new(600, 400), GUIEmbedSubwindows = true };
        var window = new Window { Name = "Subject", Visible = false, Theme = skin.Theme, Position = new(100, 140), Size = new(100, 70), MinSize = new(80, 50), MaxSize = new(120, 90) };
        var other = new Window { Name = "Other", Visible = false, Position = new(350, 140), Size = new(90, 60) }; host.AddChild(window); host.AddChild(other); window.Owner = host;
        using var tree = new SceneTree(host); window.Show(); other.Show(); window.GrabFocus();
        Check(window.GetPositionWithDecorations() == new Vector2i(93, 125) && window.GetSizeWithDecorations() == new Vector2i(120, 98), "Visible embedded geometry uses maximum focused/unfocused content margins.");
        other.GrabFocus(); Check(window.GetPositionWithDecorations() == new Vector2i(93, 125), "Decoration geometry is stable across focus."); window.GrabFocus();
        skin.Focused.ContentMarginLeft = 7.75f; skin.Focused.ContentMarginTop = 11.9f; skin.Focused.ContentMarginRight = 13.5f; skin.Focused.ContentMarginBottom = 17.9f; skin.Unfocused.ContentMarginTop = 15.75f;
        Check(window.GetPositionWithDecorations() == new Vector2i(93, 125) && window.GetSizeWithDecorations() == new Vector2i(121, 99), "Decoration queries truncate resolved floating margins before integer geometry arithmetic.");
        skin.Focused.ContentMarginLeft = 7; skin.Focused.ContentMarginTop = 11; skin.Focused.ContentMarginRight = 13; skin.Focused.ContentMarginBottom = 17; skin.Unfocused.ContentMarginTop = 15;
        window.Position = new(int.MinValue, 140); Reject<OverflowException>(() => window.GetPositionWithDecorations()); window.Position = new(100, 140);
        var closed = 0; window.CloseRequested += () => closed++;
        var point = ClosePoint(window); Button(host, point, true); Check(closed == 0 && window.Visible, "Close waits for release."); Motion(host, new(10, 10)); Button(host, new(10, 10), false); Check(closed == 0 && window.Position == new Vector2i(100, 140), "Dragging a close attempt outside cancels without moving the window.");
        Button(host, point, true); Motion(host, new(10, 10)); Motion(host, point); Button(host, point, false); Check(closed == 1 && window.Visible, "Returning inside and releasing requests close without automatic hiding.");
        Button(host, point, true); other.GrabFocus(); Button(host, point, false); Check(closed == 1, "A focus transfer cancels pending close."); window.GrabFocus();
        Button(host, point, true); window.Hide(); window.Show(); Button(host, point, false); Check(closed == 1, "Hiding clears captured close before showing again.");
        Button(host, point, true); window.Borderless = true; Button(host, point, false); Check(closed == 1 && window.GetPositionWithDecorations() == window.Position && window.GetSizeWithDecorations() == window.Size, "Borderless cancels capture and removes decoration bounds."); window.Borderless = false;
        Action fail = () => throw new InvalidOperationException("close observer"); window.CloseRequested += fail; Button(host, point, true); Reject<AggregateException>(() => Button(host, point, false)); window.CloseRequested -= fail; Check(closed == 2, "Close callback failure follows cleared capture and continues observers.");
        Button(host, point, false); Check(closed == 2, "Failed observer cannot leave an armed close capture.");
        var client = 0; window.WindowInput += _ => client++;
        Button(host, new(101, 160), true); Motion(host, new(111, 160)); Button(host, new(111, 160), false); Check(window.Size == new Vector2i(100, 70) && window.Position == new Vector2i(100, 140) && client >= 2, "The inside client edge belongs to content, not resize handles.");
        foreach (var edges in new[] { 1, 2, 4, 8, 5, 6, 9, 10 })
        {
            window.Size = new(100, 70); window.Position = new(100, 140);
            var at = new Vector2((edges & 1) != 0 ? 98 : (edges & 2) != 0 ? 202 : 150, (edges & 4) != 0 ? 94 : (edges & 8) != 0 ? 212 : 170);
            var delta = new Vector2((edges & 1) != 0 ? 60 : (edges & 2) != 0 ? 200 : 0, (edges & 4) != 0 ? 100 : (edges & 8) != 0 ? 200 : 0);
            Button(host, at, true); Motion(host, at + delta); Button(host, at + delta, false);
            var expectedSize = new Vector2i((edges & 1) != 0 ? 80 : (edges & 2) != 0 ? 120 : 100, (edges & 4) != 0 ? 50 : (edges & 8) != 0 ? 90 : 70);
            var expectedPosition = new Vector2i((edges & 1) != 0 ? 120 : 100, (edges & 4) != 0 ? 160 : 140);
            Check(window.Size == expectedSize && window.Position == expectedPosition, "Outside resize edges preserve the opposite constrained edge: " + edges + " got " + window.Position + " " + window.Size);
        }
        window.Size = new(100, 70); window.Position = new(100, 140); window.Unresizable = true; Button(host, new(98, 170), true); Motion(host, new(60, 170)); Button(host, new(60, 170), false); Check(window.Size == new Vector2i(100, 70), "Unresizable disables outside handles."); window.Unresizable = false;
        Button(host, new(98, 170), true); window.Unresizable = true; Motion(host, new(60, 170)); Button(host, new(60, 170), false); Check(window.Size == new Vector2i(100, 70), "A resize policy edit cancels an active resize."); window.Unresizable = false;
        using (var image = Image.CreateEmpty(28, 18, false, Image.Format.Rgba8)) { image.Fill(Colors.Magenta); skin.Close.SetImage(image); }
        tree.FlushDeferred();
        point = ClosePoint(window) + new Vector2(15, 5); Button(host, point, true); Button(host, point, false); Check(closed == 3, "A resized borrowed close icon updates the next hit rectangle.");
        Action resizeFail = () => throw new InvalidOperationException("size observer"); window.SizeChanged += resizeFail; Button(host, new(98, 170), true); Reject<AggregateException>(() => Motion(host, new(160, 170))); window.SizeChanged -= resizeFail; Button(host, new(160, 170), false); Check(window.Size.X == 80 && window.Position.X == 120, "Resize observer failure still anchors the opposite edge after size commitment.");
        point = ClosePoint(window); Button(host, point, true); using (var motion = new InputEventMouseMotion { ButtonMask = MouseButtonMask.Left })
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 32; i++) { motion.Position = (i & 1) == 0 ? point : new(10, 10); host.PushInput(motion, true); }
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { motion.Position = (i & 1) == 0 ? point : new(10, 10); host.PushInput(motion, true); }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(allocated == 0, "Prepared close motion/cancellation routing allocates " + allocated + " managed owner bytes.");
            for (var i = 0; i < 32; i++) host.PushInput(motion, true);
            before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) host.PushInput(motion, true);
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(allocated == 0, "Prepared unchanged close-motion routing allocates " + allocated + " managed owner bytes.");
        }
        Button(host, new(10, 10), false);
        point = ClosePoint(window); Button(host, point, true); window.Unresizable = true; Button(host, point, false); window.Unresizable = false; Check(closed == 4, "The resize-only policy does not cancel an unrelated close gesture.");
        window.Hide(); Check(window.GetPositionWithDecorations() == window.Position && window.GetSizeWithDecorations() == window.Size, "Hidden queries return client geometry."); window.Show(); window.MaxSize = Vector2i.Zero; window.MinSize = Vector2i.Zero; window.Title = "A complete wide title for tools"; window.KeepTitleVisible = true; Check(window.Size.X > 120, "Title measurement expands a kept title using the actual theme font and spacing.");
        var titleWidth = window.Size.X; skin.Unfocused.ContentMarginLeft = 230; tree.FlushDeferred(); Check(window.Size.X > titleWidth, "KeepTitleVisible reserves the larger unfocused style as well as the focused style."); skin.Unfocused.ContentMarginLeft = 3; tree.FlushDeferred();
        window.Owner = host; other.Hide(); using var packed = new PackedScene(); packed.Pack(host); var path = Path.Combine(Path.GetTempPath(), "electron2d-window-chrome-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { ResourceSaver.Save(packed, path); Fresh(path); } finally { File.Delete(path); }
        Console.WriteLine("Window chrome defaults, exact close capture, focus/hide/policy/error cleanup, eight constrained outside resize edges, geometry, title minimum and fresh theme archive passed.");
    }
    private static void Fresh(string path)
    {
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(WindowDecorationTests).Assembly.Location);
        start.Environment.Remove("ELECTRON2D_TEST_WINDOW_CHROME"); start.ArgumentList.Add("--window-chrome-archive"); start.ArgumentList.Add(path);
        using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Window chrome archive child"); }
        Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh Window chrome passed"), "Fresh Window chrome: " + error.GetAwaiter().GetResult());
    }
    internal static void RunArchive(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore)!; var host = (SubViewport)packed.Instantiate(); using var tree = new SceneTree(host); var window = (Window)host.GetChild(0);
        Check(window.GetThemeIcon("close")!.GetSize() == new Vector2(28, 18) && window.GetThemeStyleBox("embedded_unfocused_border")!.GetOffset() == new Vector2(3, 15), "Fresh scene reconstructs actual custom decoration resources.");
        var count = 0; window.CloseRequested += () => count++; var point = ClosePoint(window); Button(host, point, true); Check(count == 0, "Fresh close waits for release."); Button(host, point, false); Check(count == 1, "Fresh theme executes close input."); Console.WriteLine("Fresh Window chrome passed");
    }
}
