using Electron2D;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using IOPath = System.IO.Path;

internal static class WindowsSystemFontChecks
{
    private static int _checks;
    private static string _probe = "";
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run(string mode, string[] args)
    {
        if (mode == "linux") { Linux(args[0]); return; }
        if (mode == "restore")
        {
            using var restored = ResourceLoader.Load<SystemFont>(args[0], ResourceLoader.CacheMode.Ignore);
            Check(restored.FontNames.SequenceEqual(new[] { "Cambria Math" }) && restored.GetFontName() == "Cambria Math" && restored.GetCharSize('A', 32).X > 0, "Fresh process rematches the stored collection family.");
            Console.WriteLine("Fresh Windows system font passed."); return;
        }
        _probe = args[0]; var output = args[1]; Directory.CreateDirectory(output);
        var identity = Engine.GetSingleton<OS>("OS"); var names = OS.GetSystemFonts();
        Check(OS.HasFeature("system_fonts") && names.Length > 0, "Native catalog advertises executable availability.");
        Check(names.SequenceEqual(Native("names").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(value => value.TrimEnd('\r'))), "Copied ordered aliases match the independent DirectWrite catalog.");
        names[0] = "modified snapshot"; Check(!OS.GetSystemFonts().Contains(names[0], StringComparer.Ordinal), "Catalog arrays have independent ownership.");
        Reject<ArgumentNullException>(() => OS.GetSystemFontPath(null!)); Reject<ArgumentException>(() => OS.GetSystemFontPath("bad\0family"));
        Reject<ArgumentException>(() => OS.GetSystemFontPathForText("Arial", "bad\0text")); Reject<ArgumentNullException>(() => OS.GetSystemFontPathForText("Arial", "A", null!));
        Reject<ArgumentException>(() => OS.GetSystemFontPathForText("Arial", "A", "en", new string('x', 65537)));
        Reject<ArgumentNullException>(() => OS.GetSystemFontPathForText("Arial", null!));
        Reject<ArgumentException>(() => OS.GetSystemFontPathForText("Arial", "A", "bad\0locale"));
        Reject<ArgumentException>(() => OS.GetSystemFontPathForText("Arial", new string('A', 65537)));
        Reject<ArgumentException>(() => OS.GetSystemFontPathForText("Arial", "A", "en", "bad\0script"));
        Check(OS.GetSystemFontPathForText("Arial", "\ud800").All(File.Exists), "Malformed UTF16 input does not expose invalid source identities.");
        foreach (var query in new[] { ("Arial", 400, 100, false), ("Arial", 700, 100, true), ("Arial", 9999, 1, false), ("Times New Roman", 400, 100, false), ("Consolas", 400, 100, false), ("Cambria Math", 400, 100, false) })
        {
            var (family, weight, stretch, italic) = query;
            var expected = Native("match", family, weight.ToString(), stretch.ToString(), italic ? "1" : "0").Split('\t');
            Check(string.Equals(OS.GetSystemFontPath(family, weight, stretch, italic), expected[0], StringComparison.OrdinalIgnoreCase), "Style query selects the exact native font file: " + family);
            using var font = new SystemFont { FontNames = [family], FontWeight = weight, FontStretch = stretch, FontItalic = italic };
            Check(font.GetFontName() == expected[2] && font.GetFontWeight() == int.Parse(expected[3], CultureInfo.InvariantCulture), "Selected native file/collection face reaches SystemFont metadata: " + family);
            ReadOnlySpan<float> widths = [50, 62.5f, 75, 87.5f, 100, 112.5f, 125, 150, 200];
            Check(((font.GetFontStyle() & FontStyle.Italic) != 0) == (expected[5] != "0") && Math.Abs(font.GetFontStretch() - widths[int.Parse(expected[4]) - 1]) <= 1, "Actual italic and width metadata match the selected native face.");
            var advance = Native("advance", family, weight.ToString(), stretch.ToString(), italic ? "1" : "0", "65").Split('\t');
            var expectedWidth = 32f * int.Parse(advance[0], CultureInfo.InvariantCulture) / int.Parse(advance[1], CultureInfo.InvariantCulture);
            var width = font.GetCharSize('A', 32).X;
            Check(Math.Abs(width - expectedWidth) < .05f, $"Unhinted advance matches native design metrics: {family}, expected={expectedWidth}, actual={width}.");
        }
        foreach (var query in new[] { ("Arial", "Aسلام😀", "ar"), ("Segoe UI", "A漢字", "ja-JP"), ("Uninstalled Electron2D fixture family", "A😀", "en-US"), ("", "Aאב", "he-IL"), ("Arial", "A\u0301👩‍💻", "en-US") })
        {
            var (family, text, locale) = query;
            var expected = Native("text", family, "400", "100", "0", text, locale).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(value => value.Split('\t')[0]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var actual = OS.GetSystemFontPathForText(family, text, locale);
            Check(actual.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase), "Actual scalar/locale fallback order matches native mapping.");
            if (actual.Length > 0) { actual[0] = "changed"; Check(OS.GetSystemFontPathForText(family, text, locale).SequenceEqual(expected, StringComparer.OrdinalIgnoreCase), "Fallback path snapshots are independent of the cache."); }
        }
        Check(OS.GetSystemFontPathForText("Arial", "").SequenceEqual(new[] { OS.GetSystemFontPath("Arial") }), "Empty text retains the preferred family query.");
        var originalCulture = CultureInfo.CurrentUICulture;
        var localeFiles = new List<string>();
        try
        {
            foreach (var locale in new[] { "ja-JP", "zh-CN" })
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                var expected = Native("text", "Arial", "400", "100", "0", "漢", locale).Split('\t')[0];
                var actual = OS.GetSystemFontPathForText("Arial", "漢")[0]; localeFiles.Add(actual);
                Check(string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase), "Default locale queries key the effective current UI culture.");
            }
            Check(!string.Equals(localeFiles[0], localeFiles[1], StringComparison.OrdinalIgnoreCase), "Installed Japanese and Chinese fallback controls choose distinct native sources.");
        }
        finally { CultureInfo.CurrentUICulture = originalCulture; }
        var alias = OS.GetSystemFonts().First(value => value.Any(character => character > 127));
        Check(string.Equals(OS.GetSystemFontPath(alias), Native("match", alias, "400", "100", "0").Split('\t')[0], StringComparison.OrdinalIgnoreCase), "Localized family aliases retain their native file identity.");
        using (var variable = new SystemFont { FontNames = ["Bahnschrift"], FontWeight = 700, FontStretch = 75 })
        {
            var design = Native("advance", "Bahnschrift", "700", "75", "0", "65").Split('\t');
            var condensed = variable.GetCharSize('A', 32).X;
            Check(Math.Abs(condensed - 32f * int.Parse(design[0]) / int.Parse(design[1])) < .05f, "Variable weight/width coordinates reach the selected source.");
            variable.FontWeight = 400; variable.FontStretch = 100; design = Native("advance", "Bahnschrift", "400", "100", "0", "65").Split('\t');
            Check(Math.Abs(variable.GetCharSize('A', 32).X - 32f * int.Parse(design[0]) / int.Parse(design[1])) < .05f && Math.Abs(variable.GetCharSize('A', 32).X - condensed) > .1f, "Live variable style changes rematch and replace the glyph advance.");
        }
        Check(OS.GetSystemFontPath("").Length > 0 && OS.GetSystemFontPath("Uninstalled Electron2D fixture family").Length > 0, "Empty/unknown families use native defaults.");
        var concurrent = Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(() => OS.GetSystemFontPath("Arial", index % 2 == 0 ? 400 : 700)))).GetAwaiter().GetResult();
        Check(concurrent.All(File.Exists), "Concurrent cold and cached catalog queries retain valid native ownership.");
        using var math = new SystemFont { FontNames = ["Cambria Math"] };
        Check(int.Parse(Native("match", "Cambria Math", "400", "100", "0").Split('\t')[1]) > 0 && math.GetFontName() == "Cambria Math", "A nonzero native collection index selects the actual family.");
        using var variation = math.FindVariation(); Check(variation.GetFontName() == math.GetFontName() && variation.GetCharSize('A', 32).X == math.GetCharSize('A', 32).X, "Logical variation maps the selected collection face.");
        using var copy = (SystemFont)math.Duplicate(); Check(copy.GetFontName() == "Cambria Math", "Duplication reconstructs independent system font ownership.");
        var saved = IOPath.Combine(output, "selected-font.e2dres"); ResourceSaver.Save(math, saved);
        Check(Child(saved).Contains("Fresh Windows system font passed.", StringComparison.Ordinal), "Fresh archive reload rematches the host catalog.");
        using var primary = new FontFile { Data = Fixture(), AllowSystemFallback = false };
        Check(!primary.HasChar('س') && primary.GetCharSize('س', 32).X == 0, "Explicit Latin source does not claim the missing scalar.");
        primary.AllowSystemFallback = true;
        var fallback = Native("text", primary.GetFontName(), "400", "100", "0", "سلام", CultureInfo.CurrentUICulture.Name).Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Split('\t');
        using var file = new FontFile { Data = File.ReadAllBytes(fallback[0]), AllowSystemFallback = false };
        using var reference = file.FindVariation(faceIndex: int.Parse(fallback[1], CultureInfo.InvariantCulture));
        Check(primary.GetCharSize('س', 32).X > 0 && Math.Abs(primary.GetCharSize('س', 32).X - reference.GetCharSize('س', 32).X) < .05f && !primary.HasChar('س'), "Automatic fallback executes while explicit support remains unchanged.");
        primary.AllowSystemFallback = false; Check(primary.GetCharSize('س', 32).X == 0, "Disabling fallback retires the lookup policy."); primary.AllowSystemFallback = true;
        Check(ReferenceEquals(identity, Engine.GetSingleton<OS>("OS")) && !DisplayServer.IsAvailable, "Catalog queries preserve OS identity without a display.");
        if (mode != "catalog") Render(mode, primary, reference);
        Console.WriteLine($"Windows system fonts: {_checks} checks passed; profile={mode}.");
    }

    private static void Linux(string observer)
    {
        _probe = observer;
        var names = OS.GetSystemFonts();
        Check(OS.HasFeature("system_fonts") && names.Length > 0, "Linux still discovers its actual Fontconfig catalog.");
        Check(names.SequenceEqual(names.Order(StringComparer.Ordinal)) && names.Distinct(StringComparer.Ordinal).Count() == names.Length, "Linux aliases retain sorted copied ownership.");
        var family = names.Contains("DejaVu Sans", StringComparer.Ordinal) ? "DejaVu Sans" : names[0];
        var path = OS.GetSystemFontPath(family);
        Check(path == Native("--format=%{file}", family) && File.Exists(path), "Linux native file selection matches independent fc-match.");
        var paths = OS.GetSystemFontPathForText(family, "Aאב", "he");
        Check(paths.Length > 0 && paths.All(File.Exists), "Linux coverage/locale requests still resolve actual files.");
        paths[0] = "modified"; Check(OS.GetSystemFontPathForText(family, "Aאב", "he").All(File.Exists), "Linux snapshots do not mutate the cache.");
        Reject<ArgumentException>(() => OS.GetSystemFontPath("bad\0family"));
        Check(!DisplayServer.IsAvailable, "Linux discovery does not initialize a display.");
        Console.WriteLine($"Linux system font catalog: {_checks} checks passed.");
    }

    private static byte[] Fixture() { using var source = Assembly.GetExecutingAssembly().GetManifestResourceStream("Locale.Regular.ttf")!; using var output = new MemoryStream(); source.CopyTo(output); return output.ToArray(); }
    private static string Native(params string[] arguments) { var start = new ProcessStartInfo(_probe); foreach (var argument in arguments) start.ArgumentList.Add(argument); return Process(start); }
    private static string Child(string path)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!); if (IOPath.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
        start.ArgumentList.Add(path); start.Environment["ELECTRON2D_TEST_WINDOWS_FONTS"] = "restore"; return Process(start);
    }
    private static string Process(ProcessStartInfo start)
    {
        start.UseShellExecute = false; start.RedirectStandardOutput = start.RedirectStandardError = true; start.CreateNoWindow = true; start.StandardOutputEncoding = start.StandardErrorEncoding = Encoding.UTF8;
        using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Font observer/child timed out."); }
        var value = output.GetAwaiter().GetResult(); var failure = error.GetAwaiter().GetResult();
        if (process.ExitCode != 0) throw new InvalidOperationException("Font observer/child failed: " + value + failure); return value.TrimEnd('\r', '\n');
    }

    private static void Render(string backend, FontFile primary, Font reference)
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var font = new SystemFont { FontNames = ["Arial"] };
        var root = new Window { Title = "Electron2D system font fixture", Size = new(620, 240) };
        var canvas = new FontCanvas { Font = font, Primary = primary, Reference = reference }; root.AddChild(canvas);
        var label = new Label { Text = "Installed native family", Position = new(18, 50), Size = new(550, 40) }; label.AddThemeFontOverride("font", font); root.AddChild(label);
        var rich = new RichTextLabel { Text = "Rich fallback سلام", Position = new(18, 100), Size = new(550, 50) }; rich.AddThemeFontOverride("normal_font", primary); root.AddChild(rich);
        var frame = 0; long before = 0, active = 0, idle = 0;
        void Pre() => before = GC.GetAllocatedBytesForCurrentThread();
        void Post()
        {
            var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64 && frame < 128) active += after - before; else if (frame >= 128) idle += after - before;
            if (frame == 4)
            {
                using var image = root.GetTexture().GetImage()!; var system = 0; var automatic = 0; var different = 0;
                for (var y = 0; y < 45; y++) for (var x = 15; x < 450; x++) if (image.GetPixel(x, y).R > .5f) system++;
                for (var y = 170; y < 225; y++) for (var x = 0; x < 250; x++) { var first = image.GetPixel(20 + x, y); if (first.G > .6f) automatic++; if (first != image.GetPixel(320 + x, y)) different++; }
                Check(system > 40 && automatic > 20 && different == 0, $"Actual selected-family/fallback pixels match independent explicit source: system={system}, fallback={automatic}, different={different}.");
            }
            if (frame < 127) { canvas.QueueRedraw(); label.QueueRedraw(); rich.QueueRedraw(); }
            var tail = GC.GetAllocatedBytesForCurrentThread() - after; if (frame >= 64 && frame < 128) active += tail; else if (frame >= 128) idle += tail;
            if (++frame == 192) root.Tree!.Quit();
        }
        try
        {
            root.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post; };
            Check(Engine.Run(root) == 0 && frame == 192, "Selected-family, Label, RichTextLabel and automatic fallback render through the actual backend.");
            Check(active == 0 && idle == 0, $"Prepared 64 active/64 idle render intervals: active={active}, idle={idle} managed bytes.");
            Console.WriteLine($"Prepared render intervals: active={active}, idle={idle} managed bytes; backend={backend}.");
        }
        finally { if (RenderingServer.IsAvailable) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; } ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class FontCanvas : Control
    {
        internal required Font Font;
        internal required Font Primary;
        internal required Font Reference;
        protected override void OnDraw() { DrawString(Font, new(18, 35), "System font Aa 123", fontSize: 28); DrawString(Primary, new(20, 210), "سلام", fontSize: 28, modulate: Colors.Cyan); DrawString(Reference, new(320, 210), "سلام", fontSize: 28, modulate: Colors.Cyan); }
    }
}
