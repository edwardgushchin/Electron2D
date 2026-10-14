using Electron2D;
using System.Diagnostics;

internal static class ProgressBarTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Rect(ProgressBar bar, StyleBox style, Rect2 expected) => Check(bar.FillRectangle(style).IsEqualApprox(expected), "Progress rectangle: expected " + expected + ", actual " + bar.FillRectangle(style));
    internal static void RunManaged()
    {
        using var background = new StyleBoxFlat { BGColor = Colors.Blue, AntiAliasing = false }; background.SetContentMarginAll(2);
        using var fill = new StyleBoxFlat { BGColor = Colors.Red, AntiAliasing = false }; fill.SetContentMargin(Side.Left, 2.25f); fill.SetContentMargin(Side.Right, 2.25f); fill.SetContentMargin(Side.Top, 1.75f); fill.SetContentMargin(Side.Bottom, 1.75f);
        using var theme = new Theme(); theme.SetStyleBox("background", "ProgressBar", background); theme.SetStyleBox("fill", "ProgressBar", fill);
        var root = new SubViewport { Size = new(300, 200) }; var bar = new ProgressBar { Name = "Progress", Theme = theme, ShowPercentage = false, Size = new(20, 12) }; root.AddChild(bar); using var tree = new SceneTree(root);
        Check(bar.MinValue == 0 && bar.MaxValue == 100 && bar.Step == .01 && bar.Value == 0 && bar.SizeFlagsVertical == Control.SizeFlags.ShrinkBegin && !bar.Indeterminate && !bar.EditorPreviewIndeterminate && bar.FillMode == ProgressFillMode.BeginToEnd, "Progress defaults preserve the base range.");
        Check(ThemeDB.IsNativeType("ProgressBar") && ThemeDB.NativeDependencies("ProgressBar").SequenceEqual(new[] { "ProgressBar", "Range", "Control", "CanvasItem", "Node", "ElectronObject" }), "Compiled theme type discovery retains the full native chain.");
        Check((int)ProgressFillMode.BottomToTop == 3 && (int)ProgressFillMode.TopToBottom == 2, "Direction identities.");
        Check(bar.GetMinimumSize() == new Vector2(4.5f, 4), "Styles determine text-free minimum.");
        bar.Value = 50;
        Rect(bar, fill, new(0, 0, 12.5f, 12)); bar.FillMode = ProgressFillMode.EndToBegin; Rect(bar, fill, new(8, 0, 12.5f, 12));
        bar.LayoutDirection = LayoutDirection.RTL; bar.FillMode = ProgressFillMode.BeginToEnd; Rect(bar, fill, new(8, 0, 12.5f, 12)); bar.FillMode = ProgressFillMode.EndToBegin; Rect(bar, fill, new(0, 0, 12.5f, 12));
        bar.FillMode = ProgressFillMode.TopToBottom; Rect(bar, fill, new(0, 0, 20, 8.5f)); bar.FillMode = ProgressFillMode.BottomToTop; Rect(bar, fill, new(0, 5, 20, 8.5f));
        bar.Value = 0; Rect(bar, fill, default); bar.Value = 100; Rect(bar, fill, new(0, 0, 20, 12.5f));
        bar.ShowPercentage = true; var font = bar.GetThemeFont("font")!; Check(bar.GetMinimumSize().Y == 4 + font.GetStringSize("100%", fontSize: bar.GetThemeFontSize("font_size")).Y, "Text minimum includes font metrics and background margins.");
        bar.LocalizeNumeralSystem = false; bar.Value = 37.5; Check(bar.PercentageText() == "38%", "Percentage rounds midpoint away from zero.");
        bar.AllowGreater = true; bar.Value = 150; Check(bar.PercentageText() == "150%" && bar.Ratio == 1, "Unbounded percentage and clamped fill remain distinct.");
        bar.AllowLesser = true; bar.Value = -.51; Check(bar.PercentageText() == "-1%" && bar.Ratio == 0, "Negative percentage follows allow flags.");
        bar.MinValue = 1; bar.MaxValue = 16; bar.ExpEdit = true; bar.Value = 4; Check(bar.PercentageText() == "50%", "Exponential percentage."); bar.MinValue = 0; bar.Value = 0; Check(bar.PercentageText() == "0%", "Zero exponential numerator uses the explicit logarithmic origin.");
        bar.ExpEdit = false; bar.MaxValue = 100; bar.AllowGreater = bar.AllowLesser = false; bar.Value = 25; bar.LocalizeNumeralSystem = true;
        var domain = TranslationServer.GetOrAddDomain("progress-test"); domain.LocaleOverride = "ar"; bar.TranslationDomain = "progress-test"; Check(bar.PercentageText() == TranslationServer.FormatNumber("25", "ar") + TranslationServer.GetPercentSign("ar"), "Domain locale, numeral localization and percent sign.");
        bar.Value = double.NaN; Reject<InvalidOperationException>(() => bar.FillRectangle(fill)); Reject<InvalidOperationException>(() => bar.PercentageText());
        Reject<ArgumentOutOfRangeException>(() => bar.FillMode = (ProgressFillMode)4); bar.Value = 50;
        Reject<InvalidOperationException>(() => Task.Run(() => bar.ShowPercentage = false).GetAwaiter().GetResult());
        bar.ShowPercentage = false; bar.TranslationDomain = ""; bar.LayoutDirection = LayoutDirection.LTR; bar.FillMode = ProgressFillMode.BeginToEnd; bar.Size = new(32, 12); bar.Indeterminate = true;
        Rect(bar, fill, default); tree.ProcessFrame(.05); Rect(bar, fill, new(0, 0, 10, 12)); tree.ProcessFrame(.1); Rect(bar, fill, new(6, 0, 24, 12));
        bar.Visible = false; tree.ProcessFrame(1); bar.Visible = true; Rect(bar, fill, new(6, 0, 24, 12));
        tree.Paused = true; tree.ProcessFrame(.1); Rect(bar, fill, new(6, 0, 24, 12)); tree.Paused = false;
        tree.EditedSceneRoot = bar; tree.ProcessFrame(.5); Rect(bar, fill, new(4, 0, 24, 12)); tree.ProcessFrame(.5); Rect(bar, fill, new(4, 0, 24, 12));
        bar.EditorPreviewIndeterminate = true; tree.ProcessFrame(.05); Rect(bar, fill, new(0, 0, 10, 12));
        tree.EditedSceneRoot = null; bar.EditorPreviewIndeterminate = false; tree.ProcessFrame(.05); Rect(bar, fill, new(0, 0, 20, 12));
        bar.FillMode = ProgressFillMode.EndToBegin; tree.ProcessFrame(.05); Rect(bar, fill, new(22, 0, 10, 12));
        bar.FillMode = ProgressFillMode.EndToBegin; Rect(bar, fill, default);
        bar.EditorPreviewIndeterminate = true; bar.Value = 37.25; bar.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root); using var copy = packed.Instantiate(); var restored = (ProgressBar)copy.GetNode("Progress");
        Check(restored.Value == 37.25 && restored.Step == .01 && restored.Indeterminate && restored.EditorPreviewIndeterminate && restored.FillMode == ProgressFillMode.EndToBegin && restored.Theme != null, "Exact type, config-before-value schema and borrowed resource graph restore.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Electron2D-progress-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            bar.Theme = null; packed.Pack(root); ResourceSaver.Save(packed, path); bar.Theme = theme; var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add(typeof(ProgressBarTests).Assembly.Location); start.ArgumentList.Add("--progress-bar-archive"); start.ArgumentList.Add(path); start.Environment.Remove("ELECTRON2D_TEST_PROGRESS_BAR");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(entireProcessTree: true); throw new TimeoutException("Fresh progress scene exceeded its deadline."); }
            Check(child.ExitCode == 0, "Fresh progress scene: " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
        var minimum = bar.GetMinimumSize(); fill.SetContentMarginAll(4); tree.ProcessFrame(0); Check(bar.GetMinimumSize().X == 8 && bar.GetMinimumSize() != minimum, "Borrowed theme mutation reflows the live bar.");
        void FailedTheme() => throw new ApplicationException("Theme observer failed.");
        bar.ThemeChanged += FailedTheme; Reject<AggregateException>(() => bar.Notify(Control.NotificationThemeChanged)); bar.ThemeChanged -= FailedTheme;
        Check(bar.GetMinimumSize().X == 8, "Theme callback failure retains required invalidation.");
        using var peer = new ProgressBar(); bar.Share(peer); peer.Value = 12.5; Check(bar.Value == 12.5, "Shared Range remains executable.");
        var disposed = new ProgressBar(); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.Indeterminate = true); Reject<ObjectDisposedException>(() => _ = disposed.ShowPercentage);
        Console.WriteLine("ProgressBar directions/RTL, fractional style rounding, text/locales, animation/visibility/pause/edited preview, guards/themes and fresh scene checks passed.");
    }
    internal static void RunArchive(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var root = scene.Instantiate(); var bar = (ProgressBar)root.GetNode("Progress"); using var tree = new SceneTree(root);
        Check(bar.Value == 37.25 && bar.Indeterminate && bar.EditorPreviewIndeterminate && bar.FillMode == ProgressFillMode.EndToBegin, "Fresh process restores exact progress type and policy.");
        tree.ProcessFrame(.05); Check(bar.FillRectangle(bar.GetThemeStyleBox("fill")!).Size.X == 10, "Fresh process executes restored animation.");
    }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var theme = new Theme(); using var background = new StyleBoxFlat { BGColor = Colors.Blue, AntiAliasing = false }; using var fill = new StyleBoxFlat { BGColor = Colors.Red, AntiAliasing = false };
        background.SetContentMarginAll(0); fill.SetContentMarginAll(0); theme.SetStyleBox("background", "ProgressBar", background); theme.SetStyleBox("fill", "ProgressBar", fill);
        theme.SetColor("font_color", "ProgressBar", Colors.Yellow); theme.SetColor("font_outline_color", "ProgressBar", Colors.White); theme.SetConstant("outline_size", "ProgressBar", 2);
        var window = new Window { Size = new(128, 144) }; var bars = new ProgressBar[4];
        for (var i = 0; i < 4; i++)
        {
            bars[i] = new ProgressBar { Name = "Progress" + i, Theme = theme, ShowPercentage = false, FillMode = (ProgressFillMode)i, Position = i < 2 ? new(8, 8 + i * 20) : new(56 + (i - 2) * 22, 8), Size = i < 2 ? new(32, 12) : new(12, 32) }; window.AddChild(bars[i]);
        }
        var text = new ProgressBar { Name = "Percentage", Theme = theme, Position = new(8, 72), Size = new(96, 28), Value = 37.5, LocalizeNumeralSystem = false }; window.AddChild(text);
        var defaultBar = new ProgressBar { Name = "DefaultProgress", Position = new(8, 108), Size = new(96, 28), Value = 63.75, LocalizeNumeralSystem = false }; window.AddChild(defaultBar);
        using var target = window.GetTexture(); var frame = 0; var passed = false;
        window.Ready += _ => RenderingServer.FramePostDraw += Capture;
        void Capture()
        {
            frame++; using var pixels = target.GetImage()!;
            for (var i = 0; i < 4; i++)
            {
                var width = i < 2 ? 32 : 12; var height = i < 2 ? 12 : 32; var ox = i < 2 ? 8 : 56 + (i - 2) * 22; var oy = i < 2 ? 8 + i * 20 : 8;
                var ratio = frame switch { 1 => 0f, 2 => .25f, 3 => .5f, 4 => 1f, _ => .5f };
                for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                    {
                        var reverse = i == 1 || i == 3; if (frame == 5 && i < 2) reverse = !reverse;
                        var axis = i < 2 ? x : y; var length = i < 2 ? width : height;
                        var inside = frame == 6 ? axis >= 4 && axis < 28 : reverse ? axis >= length * (1 - ratio) : axis < length * ratio;
                        var expected = inside ? Colors.Red : Colors.Blue;
                        Check(pixels.GetPixel(ox + x, oy + y).IsEqualApprox(expected), $"Native {backend} fill mask frame {frame}, mode {i}, ({x},{y}), actual {pixels.GetPixel(ox + x, oy + y)}, position {bars[i].Position}, RTL={bars[i].IsLayoutRTL()}.");
                    }
            }
            var yellow = 0; var white = 0;
            for (var y = 72; y < 100; y++) for (var x = 8; x < 104; x++) { var color = pixels.GetPixel(x, y); if (color.R > .8f && color.G > .8f && color.B < .2f) yellow++; if (color.R > .8f && color.G > .8f && color.B > .8f) white++; }
            Check(yellow > 0 && white > 0, "Percentage text and outline reach actual renderer.");
            Check(pixels.GetPixel(20, 120).R > pixels.GetPixel(95, 120).R, "Built-in progress fill reaches actual renderer.");
            if (frame == 3 && Environment.GetEnvironmentVariable("ELECTRON2D_PROGRESS_ARTIFACT_DIR") is { } folder) { System.IO.Directory.CreateDirectory(folder); pixels.SavePNG(System.IO.Path.Combine(folder, backend + ".png")); }
            if (frame == 6) { RenderingServer.FramePostDraw -= Capture; passed = true; window.Tree!.Quit(); return; }
            var value = frame switch { 1 => 25, 2 => 50, 3 => 100, _ => 50 };
            foreach (var bar in bars) { bar.Value = value; if (frame == 4) { var position = bar.Position; bar.LayoutDirection = LayoutDirection.RTL; bar.Position = position; } if (frame == 5) bar.Indeterminate = true; }
            if (frame == 5) window.Tree!.EditedSceneRoot = window;
        }
        Check(Engine.Run(window) == 0 && passed, "Native progress lifecycle completes."); RunWarm(backend, theme);
        Console.WriteLine("ProgressBar native full fill masks, RTL, edited preview, percentage/outline and warm active recording passed: " + backend);
    }
    private static void RunWarm(string backend, Theme theme)
    {
        var window = new Window { Size = new(180, 100) }; var animated = new ProgressBar { Name = "Animated", Theme = theme, Position = new(8, 8), Size = new(120, 20), Indeterminate = true }; var determinate = new ProgressBar { Name = "Determinate", Theme = theme, Position = new(8, 40), Size = new(120, 28), LocalizeNumeralSystem = false }; window.AddChild(animated); window.AddChild(determinate);
        var frames = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); determinate.Value = (frames & 1) == 0 ? 25 : 75; };
            RenderingServer.FramePostDraw += () => { if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before; if (++frames == 128) window.Tree!.Quit(); };
        };
        Engine.Run(window); Check(frames == 128 && allocated == 0, $"Warm {backend} progress recording allocated {allocated} bytes over 64 full frames.");
    }
}
