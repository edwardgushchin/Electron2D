using System.Diagnostics;
using Electron2D;

internal static class SystemFontTests
{
    internal static void Run()
    {
        var names = OS.GetSystemFonts();
        if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) { using var unsupported = new SystemFont(); Check(unsupported.GetStringSize("Theme text").X > 0, "Unavailable catalog uses the theme source."); return; }
        Check(names.Length > 0 && names.Distinct(StringComparer.Ordinal).Count() == names.Length, "Host catalog enumerates installed scalable families.");
        var family = names[0]; var path = OS.GetSystemFontPath(family); Check(path.Length > 0 && File.Exists(path), "Matched system font file exists.");
        var files = OS.GetSystemFontPathForText(family, "Aאב", "he", "Hebr"); Check(files.Length > 0 && files.All(File.Exists), "System text query returns real coverage sources.");
        Reject<ArgumentException>(() => OS.GetSystemFontPath("invalid\0family")); Reject<ArgumentNullException>(() => OS.GetSystemFontPathForText(family, null!));
        using var font = new SystemFont();
        Check(font.FontNames.Length == 0 && font.FontWeight == 400 && font.FontStretch == 100 && !font.FontItalic && font.AllowSystemFallback && font.Hinting == FontHinting.Light && font.SubpixelPositioning == FontSubpixelPositioning.Auto && font.GetFaceCount() == 0, "System font defaults and theme fallback are live.");
        Check(font.GetStringSize("Theme text").X > 0, "Default theme text is executable.");
        font.FontNames = [family]; Check(font.GetFaceCount() > 0 && font.GetStringSize("Catalog text").X > 0, "Selected system family uses the ordinary native text path.");
        var snapshot = font.FontNames; snapshot[0] = "changed"; Check(font.FontNames[0] == family, "Preferred family arrays have copied ownership.");
        using var variant = font.FindVariation(); Check(variant.GetStringSize("Selected face").X > 0, "Logical selected face maps through FontVariation.");
        font.FontWeight = 700; font.FontItalic = true; font.FontStretch = 80; Check(font.GetStringSize("Styled system text").X > 0 && variant.GetStringSize("Live style").X > 0, "Style matching and retained variants follow changes.");
        using var duplicate = (SystemFont)font.Duplicate(); Check(duplicate.FontNames[0] == family && duplicate.FontWeight == 700 && duplicate.FontItalic && duplicate.FontStretch == 80 && duplicate.GetStringSize("Copy").X > 0, "Resource copy rebuilds independent system source ownership.");
        using var explicitSource = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        var missing = "سلام"; Check(!explicitSource.HasChar('س'), "Fixture lacks the selected scalar.");
        var boxed = explicitSource.GetStringSize(missing); explicitSource.AllowSystemFallback = true; var shaped = explicitSource.GetStringSize(missing);
        Check(shaped.X > 0 && shaped != boxed, "Automatic system fallback changes real shaping rather than only source metadata.");
        font.Hinting = FontHinting.None; font.SubpixelPositioning = FontSubpixelPositioning.Disabled; font.KeepRoundingRemainders = false; font.ModulateColorGlyphs = true; font.Oversampling = 1.5f;
        Check(font.GetStringSize("Policies").X > 0, "Existing raster policies are applied to realized system faces.");
        var changes = 0; font.Changed += _ => changes++; font.FontWeight = 700; Check(changes == 0, "Equal style writes are silent.");
        Reject<ArgumentOutOfRangeException>(() => font.Oversampling = float.NaN); Reject<ArgumentException>(() => font.FontNames = ["invalid\0name"]);
        Stored(font); Warm(font, explicitSource); IsolatedCatalog();
        Console.WriteLine("System font catalog/matching/owned fallback checks passed.");
    }
    private static void IsolatedCatalog()
    {
        if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) return;
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-font-catalog-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            var fonts = System.IO.Path.Combine(directory, "fonts"); Directory.CreateDirectory(fonts);
            File.WriteAllBytes(System.IO.Path.Combine(fonts, "variable.ttf"), FontTestFixtures.Variable);
            var collection = System.IO.Path.Combine(directory, "collection"); Directory.CreateDirectory(collection); File.WriteAllBytes(System.IO.Path.Combine(collection, "collection.ttc"), FontTestFixtures.Collection);
            File.WriteAllBytes(System.IO.Path.Combine(fonts, "arabic.woff2"), FontTestFixtures.Arabic);
            var config = System.IO.Path.Combine(directory, "fonts.conf");
            File.WriteAllText(config, "<?xml version=\"1.0\"?><fontconfig><dir>" + System.Security.SecurityElement.Escape(fonts) + "</dir><cachedir>" + System.Security.SecurityElement.Escape(System.IO.Path.Combine(directory, "cache")) + "</cachedir></fontconfig>");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(SystemFontTests).Assembly.Location);
            start.Environment["FONTCONFIG_FILE"] = config; start.Environment["ELECTRON2D_TEST_SYSTEM_FONT_CATALOG"] = "1";
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Isolated font catalog."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Isolated font catalog passed"), error.GetAwaiter().GetResult());
            File.WriteAllText(config, "<?xml version=\"1.0\"?><fontconfig><dir>" + System.Security.SecurityElement.Escape(collection) + "</dir><cachedir>" + System.Security.SecurityElement.Escape(System.IO.Path.Combine(directory, "collection-cache")) + "</cachedir></fontconfig>");
            start.Environment["ELECTRON2D_TEST_SYSTEM_FONT_CATALOG"] = "collection";
            using var collectionChild = Process.Start(start)!; var collectionOutput = collectionChild.StandardOutput.ReadToEndAsync(); var collectionError = collectionChild.StandardError.ReadToEndAsync(); if (!collectionChild.WaitForExit(30000)) { collectionChild.Kill(true); throw new TimeoutException("Collection catalog."); }
            Check(collectionChild.ExitCode == 0 && collectionOutput.GetAwaiter().GetResult().Contains("Collection catalog passed"), collectionError.GetAwaiter().GetResult());
            File.WriteAllText(config, "<?xml version=\"1.0\"?><fontconfig></fontconfig>"); start.Environment["ELECTRON2D_TEST_SYSTEM_FONT_CATALOG"] = "empty";
            using var emptyChild = Process.Start(start)!; var emptyOutput = emptyChild.StandardOutput.ReadToEndAsync(); var emptyError = emptyChild.StandardError.ReadToEndAsync(); if (!emptyChild.WaitForExit(30000)) { emptyChild.Kill(true); throw new TimeoutException("Empty font catalog."); }
            Check(emptyChild.ExitCode == 0 && emptyOutput.GetAwaiter().GetResult().Contains("Empty catalog passed"), emptyError.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(directory, true); }
    }
    internal static void RunCatalog()
    {
        using var fixture = new FontFile { Data = FontTestFixtures.Variable }; var family = fixture.GetFontName();
        Check(OS.GetSystemFonts().Contains(family, StringComparer.Ordinal), "Isolated catalog lists actual fixture metadata.");
        using var light = new SystemFont { FontNames = [family], FontWeight = 100, AllowSystemFallback = false };
        using var heavy = new SystemFont { FontNames = [family], FontWeight = 900, AllowSystemFallback = false };
        Check(heavy.GetStringSize("AA", fontSize: 20).X > light.GetStringSize("AA", fontSize: 20).X, "Variable system style matching changes executable glyph advances: light=" + light.GetStringSize("AA", fontSize: 20).X + " heavy=" + heavy.GetStringSize("AA", fontSize: 20).X + " paths=" + System.IO.Path.GetFileName(OS.GetSystemFontPath(family, 100)) + "/" + System.IO.Path.GetFileName(OS.GetSystemFontPath(family, 900)) + " axes=" + light.GetSupportedVariationList().Count);
        using var inherited = heavy.FindVariation(); using var overridden = heavy.FindVariation(new() { [0x77676874] = 100 });
        Check(inherited.GetStringSize("AA", fontSize: 20).X == heavy.GetStringSize("AA", fontSize: 20).X && overridden.GetStringSize("AA", fontSize: 20).X == light.GetStringSize("AA", fontSize: 20).X, "Logical face and requested coordinates preserve system defaults with explicit override precedence.");
        Check(inherited.GetFaceCount() == heavy.GetFaceCount(), "Retained variants expose logical system face count.");
        using var source = new FontFile { Data = FontTestFixtures.Variable, AllowSystemFallback = true };
        Check(!source.HasChar('س') && source.GetStringSize("سلام").X > 0, "Isolated fallback uses a real separate Arabic source.");
        using (var lease = source.BeginRead())
        {
            var old = source.PrimaryData!.FindSystemFallback('س', "")!; source.AllowSystemFallback = false;
            Check(old.GetGlyphIndex('س') != 0, "Policy changes retain old owned fallback faces through active parent readers.");
        }
        Console.WriteLine("Isolated font catalog passed.");
    }
    internal static void RunCollectionCatalog()
    {
        using var source = new FontFile { Data = FontTestFixtures.Collection };
        using var font = new SystemFont { FontNames = [source.GetFontName()], FontWeight = 900, AllowSystemFallback = false };
        Check(OS.GetSystemFontPath(source.GetFontName(), 900).EndsWith("collection.ttc", StringComparison.Ordinal), "Collection path is matched by the provider.");
        Check(font.GetFaceCount() == 2, "Equally matched collection faces retain both logical indices.");
        using var logical = font.FindVariation(); using var raw = source.FindVariation(new() { [0x77676874] = 900 }, faceIndex: 0);
        Check(font.GetStringSize("AA", fontSize: 20) == raw.GetStringSize("AA", fontSize: 20) && logical.GetStringSize("AA", fontSize: 20) == raw.GetStringSize("AA", fontSize: 20), "Logical face zero maps to the selected raw collection face and requested variable style.");
        using var second = font.FindVariation(faceIndex: 1); using var secondRaw = source.FindVariation(new() { [0x77676874] = 900 }, faceIndex: 1); Check(second.GetStringSize("AA", fontSize: 20) == secondRaw.GetStringSize("AA", fontSize: 20), "Second tied logical face preserves its raw collection identity.");
        using var rejected = font.FindVariation(faceIndex: 2); Reject<ArgumentOutOfRangeException>(() => rejected.GetStringSize("A"));
        Console.WriteLine("Collection catalog passed.");
    }
    internal static void RunEmptyCatalog()
    {
        Check(OS.GetSystemFonts().Length == 0 && OS.GetSystemFontPath("Missing family").Length == 0, "Empty catalog queries have explicit absent results.");
        var previous = ThemeDB.FallbackFont;
        try
        {
            using var font = new SystemFont { FontNames = ["Unavailable family"] };
            Check(font.GetFaceCount() == 0 && font.GetStringSize("Theme fallback").X > 0, "Unavailable family renders through the theme.");
            using var replacement = new FontFile { Data = FontTestFixtures.Variable, AllowSystemFallback = false };
            ThemeDB.FallbackFont = replacement;
            Check(font.GetStringSize("AA", fontSize: 20) == replacement.GetStringSize("AA", fontSize: 20), "Theme replacement invalidates retained default system source.");
        }
        finally { ThemeDB.FallbackFont = previous; }
        Console.WriteLine("Empty catalog passed.");
    }
    private static void Warm(SystemFont font, FontFile fallback)
    {
        for (var i = 0; i < 128; i++) { font.GetStringSize("Prepared system text"); fallback.GetStringSize("سلام"); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) { font.GetStringSize("Prepared system text"); fallback.GetStringSize("سلام"); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "128 prepared system/fallback measurements allocate zero managed bytes.");
    }
    private static void Stored(SystemFont font)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-system-font-" + Guid.NewGuid() + ".e2dres");
        try
        {
            ResourceSaver.Save(font, path); using var loaded = ResourceLoader.Load<SystemFont>(path, ResourceLoader.CacheMode.Ignore);
            Check(loaded.FontNames.SequenceEqual(font.FontNames) && loaded.FontWeight == font.FontWeight && loaded.GetStringSize("Restored").X > 0, "Typed storage keeps requested settings and rematches native sources.");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(SystemFontTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_SYSTEM_FONT_CHILD"] = path;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("System font child."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh system font passed"), error.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
    }
    internal static void RunChild(string path) { using var font = ResourceLoader.Load<SystemFont>(path, ResourceLoader.CacheMode.Ignore); Check(font.FontNames.Length > 0 && font.GetStringSize("Fresh system text").X > 0, "Fresh process rematches font source."); Console.WriteLine("Fresh system font passed."); }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_SYSTEM_FONT_RENDERER") ?? "gpu";
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            var names = OS.GetSystemFonts(); var family = names.Contains("DejaVu Sans", StringComparer.Ordinal) ? "DejaVu Sans" : names[0];
            using var font = new SystemFont { FontNames = [family], FontWeight = 700, SubpixelPositioning = FontSubpixelPositioning.Disabled };
            using var fallback = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = true };
            var window = new Window { Size = new(520, 230) }; var canvas = new FontCanvas { Font = font, Fallback = fallback }; window.AddChild(canvas);
            var label = new Label { Text = "Matched system family", Position = new(18, 45), Size = new(460, 35) }; label.AddThemeFontOverride("font", font); label.AddThemeFontSizeOverride("font_size", 24); window.AddChild(label);
            var rich = new RichTextLabel { Text = "Styled system text / سلام", Position = new(18, 95), Size = new(480, 45) }; rich.AddThemeFontOverride("normal_font", font); rich.AddThemeFontSizeOverride("normal_font_size", 22); window.AddChild(rich);
            var frame = 0; long before = 0, total = 0;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(new(.08f, .09f, .12f));
                RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
                RenderingServer.FramePostDraw += () =>
                {
                    var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64) total += after - before;
                    if (frame == 2)
                    {
                        using var image = RenderingServer.Service!.Readback(); var upper = 0; var lower = 0;
                        for (var y = 5; y < 38; y++) for (var x = 15; x < 450; x++) if (image.GetPixel(x, y).R > .5f) upper++;
                        for (var y = 170; y < 204; y++) for (var x = 15; x < 350; x++) if (image.GetPixel(x, y).G > .6f) lower++;
                        Check(upper > 50 && lower > 20, "Native selected-family and automatic fallback glyph pixels.");
                        if (Environment.GetEnvironmentVariable("ELECTRON2D_SYSTEM_FONT_CAPTURE") is { } path) image.SavePNG(path);
                    }
                    canvas.QueueRedraw(); label.QueueRedraw(); rich.QueueRedraw(); if (frame >= 64) total += GC.GetAllocatedBytesForCurrentThread() - after;
                    if (++frame == 128) window.Tree!.Quit();
                };
            };
            Check(Engine.Run(window) == 0 && frame == 128 && total == 0, "64 prepared system/fallback/control render intervals: " + total);
            Console.WriteLine("64 prepared system/fallback/control render intervals: " + total + " managed bytes; backend=" + backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private sealed class FontCanvas : Control
    {
        internal required SystemFont Font;
        internal required FontFile Fallback;
        protected override void OnDraw()
        {
            DrawString(Font, new(18, 32), "System font: Aa 123", fontSize: 24);
            DrawString(Fallback, new(18, 198), "Auto fallback: سلام", fontSize: 24, modulate: Colors.Cyan);
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
