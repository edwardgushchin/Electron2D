using Electron2D;
using Path = System.IO.Path;

internal static class FontVariationTests
{
    private static readonly uint Weight = OpenTypeFeatureTags.Resolve("weight");
    internal static void Run()
    {
        using var source = new FontFile { Data = FontTestFixtures.Variable };
        using var font = new FontVariation { BaseFont = source };
        var axes = source.GetSupportedVariationList(); Check(Weight == 0x77676874 && axes[Weight] == new FontVariationAxis(100, 900, 100), "axis metadata and aliases"); axes.Clear(); Check(source.GetSupportedVariationList().Count == 1, "copied axes");
        using var factory = source.FindVariation(new() { [Weight] = 900 }); Check(factory.GetStringSize("A", fontSize: 20).X == 20 && ReferenceEquals(factory.BaseFont, source), "typed caller-owned instance factory");
        font.SetVariationOpenType(new() { ["weight"] = 100 });
        var original = source.GetStringSize("AA", fontSize: 20).X;
        Check(original == 20 && font.GetStringSize("AA", fontSize: 20).X == original, "default instance advances");
        var map = new Dictionary<uint, float> { [Weight] = 900 }; font.VariationOpenType = map; map[Weight] = 100;
        Check(font.GetStringSize("AA", fontSize: 20).X == 40 && source.GetStringSize("AA", fontSize: 20).X == original, "independent FT/HB design coordinates");
        font.VariationOpenType = new() { [Weight] = 10000, [0x78787878] = 5 }; Check(font.GetStringSize("A", fontSize: 20).X == 20, "clamped known and ignored unknown axes");
        font.VariationOpenType.Clear(); Check(font.VariationOpenType.Count == 2, "copied coordinates");
        font.SpacingGlyph = 3; font.SpacingSpace = 5; font.SpacingTop = 4; font.SpacingBottom = -1;
        Check(font.GetStringSize("AA", fontSize: 20).X == 43 && font.GetHeight(20) == source.GetHeight(20) + 3, "signed glyph and line spacing");
        Check(font.GetStringSize("A", fontSize: 20).X == 20 && font.GetStringSize("A A", fontSize: 20).X == 68, "terminal spacing and explicit space width"); font.SpacingSpace = 0; Check(font.GetStringSize("A A", fontSize: 20).X == 66, "zero space setting uses glyph spacing"); font.SpacingSpace = 5;
        Reject<ArgumentOutOfRangeException>(() => font.SetSpacing(TextSpacingType.Max, 1)); Reject<ArgumentOutOfRangeException>(() => font.VariationOpenType = new() { [Weight] = float.NaN }); Reject<ArgumentOutOfRangeException>(() => font.VariationEmbolden = float.PositiveInfinity); Reject<ArgumentOutOfRangeException>(() => font.VariationFaceIndex = -1);
        using var nested = new FontVariation { BaseFont = font }; Check(nested.GetStringSize("AA", fontSize: 20).X == original, "nested instance overrides settings rather than composes them");
        Reject<ArgumentException>(() => font.BaseFont = nested); Reject<ArgumentException>(() => source.Fallbacks = [font]); Reject<ArgumentException>(() => font.Fallbacks = [font]);
        using var fallback = new FontFile { Data = FontTestFixtures.Arabic }; source.Fallbacks = [fallback]; Check(font.HasChar(0x633), "inherited base fallbacks"); using var emptyFallback = new FontFile(); font.Fallbacks = [emptyFallback]; Check(!font.HasChar(0x633), "explicit fallbacks replace inherited list"); font.Fallbacks = [];
        var old = font.PrimaryData!; using (font.BeginRead()) { font.VariationOpenType = new() { [Weight] = 100 }; Check(old.GetGlyphIndex('A') != 0, "active reader retains retired face"); }
        Reject<ObjectDisposedException>(() => old.GetGlyphIndex('A'));
        source.Data = FontTestFixtures.OpenSans; Check(font.GetFontName() == "Open Sans" && font.GetStringSize("ffi").X > 0, "base replacement rebuilds instance");
        using var features = new FontVariation { BaseFont = source, OpenTypeFeatures = new() { ["liga"] = 0 } }; var shaped = new List<NativeShapedGlyph>(); Check(Layout(features, "ffi").GlyphCount == 3, "feature override controls shaping"); using var fallbackFeatures = new FontVariation { BaseFont = emptyFallback, Fallbacks = [source], OpenTypeFeatures = new() { ["liga"] = 0 } }; Check(Layout(fallbackFeatures, "ffi").GlyphCount == 3, "span features apply to fallback faces");
        features.OpenTypeFeatures.Clear(); Check(features.GetOpenTypeFeatures()["liga"] == 0, "copied features");
        var changes = 0; font.Changed += _ => changes++; font.VariationOpenType = font.VariationOpenType; font.PaletteCustomColors = font.PaletteCustomColors; font.OpenTypeFeatures = font.OpenTypeFeatures; Check(changes == 0, "equal copied configuration is silent"); source.Hinting = FontHinting.None; Check(changes == 1, "borrowed base change notification");
        using var defaultInstance = new FontVariation(); var previousTheme = ThemeDB.FallbackFont;
        try { ThemeDB.FallbackFont = source; Check(defaultInstance.GetFontName() == source.GetFontName(), "selected theme fallback"); using var nestedTheme = new FontVariation { BaseFont = defaultInstance }; Check(nestedTheme.GetFontName() == "Open Sans", "nested default source"); source.Data = FontTestFixtures.Variable; Check(nestedTheme.GetFontName() == "Electron2D Variation Test", "nested default source change invalidation"); source.Data = FontTestFixtures.OpenSans; ThemeDB.FallbackFont = defaultInstance; Reject<InvalidOperationException>(() => defaultInstance.GetStringSize("A")); } finally { ThemeDB.FallbackFont = previousTheme; }
        var weak = Unowned(source); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); Check(!weak.IsAlive, "borrowed source subscriptions do not root unowned instances"); source.EmitChanged();
        Depth(); Synthetic(source); Palettes(); Collection(); Markup(source); Stored();
        using var clone = (FontVariation)font.Duplicate(); Check(ReferenceEquals(clone.BaseFont, source) && clone.SpacingGlyph == 3, "borrowed shallow duplicate"); clone.ResetState(); Check(clone.BaseFont == null && clone.SpacingGlyph == 0 && clone.VariationOpenType.Count == 0, "reset configuration");
        using var themed = new FontVariation(); Check(themed.GetStringSize("A").X > 0, "lazy theme font");
        var disposed = new FontFile { Data = FontTestFixtures.Variable }; using var dependent = new FontVariation { BaseFont = disposed }; dependent.GetStringSize("A"); disposed.Dispose(); Reject<ObjectDisposedException>(() => dependent.GetStringSize("A"));
        Console.WriteLine("Font variations: axes, shaping, outlines, palettes, collection, ownership, markup and resource storage passed.");
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference Unowned(Font source) { var font = new FontVariation { BaseFont = source }; font.GetStringSize("A"); return new WeakReference(font); }
    private static void Depth()
    {
        using var leaf = new FontFile { Data = FontTestFixtures.Variable }; var chain = new List<FontVariation>(); Font current = leaf;
        try { for (var i = 0; i < 64; i++) { var next = new FontVariation { BaseFont = current }; chain.Add(next); current = next; } Check(current.GetStringSize("A").X > 0, "maximum base depth executes"); using var excessive = new FontVariation(); Reject<ArgumentException>(() => excessive.BaseFont = current); using var fallback = new FontFile(); Reject<ArgumentException>(() => fallback.Fallbacks = [current]); }
        finally { for (var i = chain.Count - 1; i >= 0; i--) chain[i].Dispose(); }
    }
    private static TextLayout Layout(Font font, string text) => font.GetLayout(text, HorizontalAlignment.Left, -1, 16, -1, TextLineBreakFlags.None, TextJustificationFlags.None, TextDirection.LTR, TextOrientation.Horizontal, false);
    private static void Synthetic(FontFile source)
    {
        using var normal = new FontVariation { BaseFont = source }; using var bold = new FontVariation { BaseFont = source, VariationEmbolden = 1 }; using var thin = new FontVariation { BaseFont = source, VariationEmbolden = -.5f }; using var slant = new FontVariation { BaseFont = source, VariationTransform = new(new(1, .3f), new(0, 1), new(500, 500)) };
        var glyph = normal.PrimaryData!.GetGlyphIndex('A'); var a = normal.PrimaryData.GetGlyph(glyph, 32, 0, 1); var b = bold.PrimaryData!.GetGlyph(glyph, 32, 0, 1); var t = thin.PrimaryData!.GetGlyph(glyph, 32, 0, 1); var s = slant.PrimaryData!.GetGlyph(glyph, 32, 0, 1);
        Check(Alpha(b) > Alpha(a) && Alpha(t) < Alpha(a) && Hash(s) != Hash(a), $"positive/negative embolden and outline basis: normal={a.Size}/{Alpha(a)} bold={b.Size}/{Alpha(b)} thin={t.Size}/{Alpha(t)} slant={s.Size}");
        Check(Math.Abs(bold.GetCharSize('A', 32).X - normal.GetCharSize('A', 32).X - .5f) < .02f, "synthetic advance adjustment");
        using var shift = new FontVariation { BaseFont = source, BaselineOffset = .25f }; var shaped = new List<NativeShapedGlyph>(); shift.PrimaryData!.Shape(['A'], 0, 1, 32, NativeTextDirection.LTR, 0, "", shaped); Check(shaped[0].YOffset < 0 && shaped[0].XOffset == 0, "positive baseline shifts raster down");
        shift.PrimaryData.Shape(['A'], 0, 1, 32, NativeTextDirection.TTB, 0, "", shaped); Check(shaped[0].XOffset != 0 && shaped[0].YAdvance < 0, "vertical baseline and signed advances");
        bold.VariationEmbolden = float.MaxValue; Reject<InvalidOperationException>(() => bold.PrimaryData!.GetGlyph(glyph, 32, 0, 1)); bold.VariationEmbolden = 1;
        slant.VariationTransform = new(new(1, .3f), new(0, 1), default); var untranslated = slant.PrimaryData!.GetGlyph(glyph, 32, 0, 1); Check(untranslated.Size == s.Size && untranslated.Offset == s.Offset, "outline translation ignored");
    }
    private static ulong Hash(FontGlyph glyph) { using var image = ((ImageTexture)glyph.Texture!).GetImage(); ulong hash = 14695981039346656037; foreach (var b in image!.GetData()) { hash ^= b; hash = unchecked(hash * 1099511628211); } return hash; }
    private static long Alpha(FontGlyph glyph) { using var image = ((ImageTexture)glyph.Texture!).GetImage(); var bytes = image!.GetData(); long total = 0; for (var i = 3; i < bytes.Length; i += 4) total += bytes[i]; return total; }
    private static void Palettes()
    {
        using var source = new FontFile { Data = FontTestFixtures.Palette }; using var font = new FontVariation { BaseFont = source, PaletteIndex = 1 };
        Check(source.GetPaletteCount() == 2 && source.GetPaletteName(1) == "Alternate" && source.GetPaletteColors(0)[0] == Colors.Red, "palette metadata"); var colors = source.GetPaletteColors(1); colors[0] = Colors.Red; Check(source.GetPaletteColors(1)[0] == Colors.Lime, "copied predefined palette");
        var glyph = font.PrimaryData!.GetGlyphIndex('A'); var raster = font.PrimaryData.GetGlyph(glyph, 32, 0, 1); using var image = ((ImageTexture)raster.Texture!).GetImage(); Check(image!.GetPixel(5, 10).G > .9f && image!.GetPixel(25, 10).R > .9f, "selected palette rendered");
        font.PaletteCustomColors = [Colors.Transparent, Colors.Magenta]; var overridden = font.PrimaryData!.GetGlyph(glyph, 32, 0, 1); using var pixels = ((ImageTexture)overridden.Texture!).GetImage(); Check(pixels!.GetPixel(5, 10).G > .9f && pixels!.GetPixel(25, 10).B > .9f, "transparent preserve and custom palette override");
        font.PaletteCustomColors = [new(float.MaxValue, -1, .5f, 1)]; Check(font.PrimaryData!.GetGlyph(glyph, 32, 0, 1).Colored, "finite palette channels clamp safely"); Reject<ArgumentOutOfRangeException>(() => source.GetPaletteColors(2));
    }
    private static void Collection()
    {
        using var source = new FontFile { Data = FontTestFixtures.Collection }; using var font = new FontVariation { BaseFont = source, VariationFaceIndex = 1 }; Check(font.GetFaceCount() == 2 && font.GetFontStyleName() == "Heavy" && font.GetStringSize("A", fontSize: 20).X == 20 && source.GetStringSize("A", fontSize: 20).X == 10, "collection face identity"); font.VariationFaceIndex = 99; Reject<InvalidOperationException>(() => font.GetStringSize("A")); font.VariationFaceIndex = 0; Check(font.GetStringSize("A", fontSize: 20).X == 10, "invalid face recovery");
    }
    private static void Markup(Font source)
    {
        using var rich = new RichTextLabel { Size = new(500, 200), BBCodeEnabled = true }; rich.AddThemeFontOverride("normal_font", source); rich.Text = "[font gl=3 sp=5 top=4 bt=2 emb=.5 sln=.3 fi=0 otf='liga=0']ffi A[/font]"; Check(rich.GetParsedText() == "ffi A" && rich.GetContentWidth() > 0 && rich.GetContentHeight() > source.GetHeight(), "advanced font markup"); rich.Text = "[otf=liga=0]ffi[/otf]"; Check(rich.GetParsedText() == "ffi" && rich.GetContentWidth() > 0, "generic font feature tag"); rich.Text = "A"; var ordinary = rich.GetContentHeight(); rich.Text = "[font top=-2 bt=-1]A[/font]"; Check(rich.GetContentHeight() == ordinary - 3, "negative rich span line spacing");
    }
    private static void Stored()
    {
        using var source = new FontFile { Data = FontTestFixtures.Variable }; using var font = new FontVariation { BaseFont = source, VariationOpenType = new() { [Weight] = 900 }, SpacingGlyph = 2, BaselineOffset = .1f, PaletteCustomColors = [Colors.Red] };
        var path = Path.Combine(Path.GetTempPath(), "e2d-variation-" + Guid.NewGuid() + ".e2dres");
        try { ResourceSaver.Save(font, path); using var copy = ResourceLoader.Load<FontVariation>(path, ResourceLoader.CacheMode.Ignore); Check(copy.GetStringSize("AA", fontSize: 20).X == 42 && copy.BaselineOffset == .1f && copy.PaletteCustomColors[0] == Colors.Red, "typed resource codecs"); var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(FontVariationTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_FONT_VARIATION"); start.Environment.Remove("ELECTRON2D_TEST_FONT_VARIATION_HOST"); start.Environment["ELECTRON2D_TEST_FONT_VARIATION_CHILD"] = path; using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("variation resource"); } Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh font variation passed"), error.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path) { using var font = ResourceLoader.Load<FontVariation>(path, ResourceLoader.CacheMode.Ignore); Check(font.GetStringSize("AA", fontSize: 20).X == 42 && font.BaseFont is FontFile, "fresh native font factory/codecs"); Console.WriteLine("Fresh font variation passed"); }
    private sealed class Canvas : Entity { internal Action<Canvas> Paint = null!; protected override void OnDraw() => Paint(this); }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_VARIATION_RENDERER") ?? "gpu";
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            using var source = new FontFile { Data = FontTestFixtures.Variable, SubpixelPositioning = FontSubpixelPositioning.Disabled };
            using var narrow = source.FindVariation(new() { [Weight] = 100 }); using var wide = source.FindVariation(new() { [Weight] = 900 });
            using var latin = new FontFile { Data = FontTestFixtures.OpenSans }; using var slant = latin.FindVariation(strength: .6f, transform: new(new(1, .3f), new(0, 1), default), baselineOffset: .1f);
            using var color = new FontFile { Data = FontTestFixtures.Palette }; using var palette = color.FindVariation(paletteIndex: 1, customColors: [Colors.Transparent, Colors.Magenta]);
            var window = new Window { Size = new(480, 300) };
            var canvas = new Canvas { Paint = c => { c.DrawString(latin, new(16, 35), "Font instances", fontSize: 24); c.DrawString(narrow, new(16, 82), "AAA", fontSize: 32); c.DrawString(wide, new(16, 132), "AAA", fontSize: 32); c.DrawString(slant, new(16, 180), "Slant and bold ffi", fontSize: 28); c.DrawString(palette, new(16, 235), "AA", fontSize: 32); } }; window.AddChild(canvas);
            var rich = new RichTextLabel { Position = new(150, 65), Size = new(310, 90), BBCodeEnabled = true, Text = "[font emb=.6 sln=.25 gl=2 top=3 otf='liga=0']Styled rich text[/font]" }; window.AddChild(rich);
            var frame = 0; long before = 0, total = 0;
            window.Ready += _ => { RenderingServer.SetDefaultClearColor(new(.08f, .09f, .12f)); RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread(); RenderingServer.FramePostDraw += () => { var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64) total += after - before; if (frame == 2) { using var image = RenderingServer.Service!.Readback(); if (Environment.GetEnvironmentVariable("ELECTRON2D_VARIATION_CAPTURE") is { } path) image.SavePNG(path); Check(image.GetPixel(20, 220).G > .8f && image.GetPixel(42, 220).B > .8f, "native selected/custom palette pixels"); } canvas.QueueRedraw(); rich.QueueRedraw(); if (frame >= 64) total += GC.GetAllocatedBytesForCurrentThread() - after; if (++frame == 128) window.Tree!.Quit(); }; };
            Check(Engine.Run(window) == 0 && frame == 128 && total == 0, "prepared variation render allocation: " + total);
            Console.WriteLine("64 prepared font variation/rich markup/render intervals: " + total + " managed bytes; backend=" + backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
