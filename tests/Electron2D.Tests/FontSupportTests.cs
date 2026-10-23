using System.Diagnostics;
using Electron2D;

internal static partial class FontSupportTests
{
    internal static FontFile Bitmap(char character, int advance, Color color)
    {
        var font = new FontFile { AllowSystemFallback = false };
        using var image = Image.CreateEmpty(5, 6, false, Image.Format.Rgba8); image.Fill(color);
        font.SetTextureImage(0, new(8, 0), 0, image);
        font.SetGlyphOffset(0, new(8, 0), character, new(0, -6)); font.SetGlyphSize(0, new(8, 0), character, new(5, 6));
        font.SetGlyphUVRect(0, new(8, 0), character, new(0, 0, 5, 6)); font.SetGlyphTextureIndex(0, new(8, 0), character, 0);
        font.SetGlyphAdvance(0, 8, character, new(advance, 8)); font.SetCacheAscent(0, 8, 6); font.SetCacheDescent(0, 8, 2);
        font.SetScriptSupportOverride("Latn", true); return font;
    }
    private static float Width(Font font, string text = "B", string language = "en")
    {
        var layout = new TextLayout();
        layout.Build(font, new(text, 8, -1, HorizontalAlignment.Left, -1, TextLineBreakFlags.None, TextJustificationFlags.None, TextDirection.Auto, TextOrientation.Horizontal), new(Language: language));
        return layout.Size.X;
    }
    internal static void Run()
    {
        using var empty = new FontFile(); var notifications = 0; empty.Changed += _ => Interlocked.Increment(ref notifications);
        Check(!empty.GetLanguageSupportOverride("missing") && !empty.GetScriptSupportOverride("Latn") && empty.GetCacheCount() == 0, "Queries are pure and do not initialize a face.");
        empty.RemoveLanguageSupportOverride("missing"); empty.SetLanguageSupportOverride("EN", false); empty.SetLanguageSupportOverride("EN", false);
        empty.SetLanguageSupportOverride("en", true); empty.SetScriptSupportOverride("*", false); empty.SetScriptSupportOverride("Latn", true);
        Check(notifications == 4 && empty.GetLanguageSupportOverrides().SequenceEqual(new[] { "EN", "en" }), "Exact case-sensitive entries include false values and equal writes are silent.");
        var keys = empty.GetScriptSupportOverrides(); keys[0] = "mutated"; Check(empty.GetScriptSupportOverrides()[0] == "*", "Lists have independent ownership.");
        Reject<ArgumentNullException>(() => empty.SetLanguageSupportOverride(null!, true)); Reject<ArgumentException>(() => empty.SetScriptSupportOverride("a\0b", true));
        Reject<ArgumentException>(() => empty.GetLanguageSupportOverride("\ud800")); Reject<ArgumentException>(() => empty.SetLanguageSupportOverride(new('x', 65537), false));
        empty.SetLanguageSupportOverride("", false); empty.SetScriptSupportOverride("unrecognized", true); Check(empty.GetScriptSupportOverride("unrecognized"), "Nonstandard keys remain stored.");
        using var copy = (FontFile)empty.Duplicate(); copy.RemoveScriptSupportOverride("Latn"); Check(empty.GetScriptSupportOverride("Latn") && !copy.GetScriptSupportOverride("Latn"), "Duplicate policy independence.");
        Parallel.For(0, 24, i => empty.SetLanguageSupportOverride("parallel" + i, true)); Check(empty.GetLanguageSupportOverrides().Length == 27, "Concurrent writers retain every entry.");
        empty.Changed += ThrowObserver; Reject<MarkerException>(() => empty.SetScriptSupportOverride("Hebr", true)); empty.Changed -= ThrowObserver;
        Check(empty.GetScriptSupportOverride("Hebr"), "Observer failure leaves committed policy.");
        var support = (PropertyDescriptor<FontFile, byte[]>)empty.GetPropertyList().Single(p => p.Name == "_font_support"); var saved = (byte[])support.GetValue(empty)!;
        foreach (var malformed in new[] { new byte[] { 2, 0, 0, 0 }, saved[..^1], saved.Concat(new byte[] { 0 }).ToArray(), new byte[] { 1, 0, 0, 0, 1, 0, 0, 0, 255, 255, 255, 255, 7 } })
        { Reject<InvalidDataException>(() => support.SetValue(empty, malformed)); Check(empty.GetScriptSupportOverride("Hebr"), "Malformed storage preserves state."); }
        foreach (var value in new byte[] { 2, 1 })
        {
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); writer.Write(1); writer.Write(2); writer.Write("en"); writer.Write(value); writer.Write("en"); writer.Write((byte)1); writer.Write(0);
            Reject<InvalidDataException>(() => support.SetValue(empty, stream.ToArray()));
        }
        using var root = Bitmap('A', 6, Colors.White); using var first = Bitmap('B', 7, Colors.Red); using var second = Bitmap('B', 11, Colors.Green);
        root.Fallbacks = [first, second]; root.SetLanguageSupportOverride("en", false); Check(!root.IsLanguageSupported("en") && root.IsScriptSupported("Latn"), "Support queries use primary policy only.");
        Check(Width(root) == 7, "Equal tiers preserve authored order."); first.SetLanguageSupportOverride("en", false); Check(Width(root) == 11, "Language support ranks real fallback advances.");
        Check(Width(root, language: "en_US") == 11, "Regional language priority key matches the shaping contract.");
        first.SetLanguageSupportOverride("en-", false);
        Check(Width(root, language: "en-US") == 11 && Width(root, language: "EN_US") == 7, "Regional prefix separators and ordinal case retain actual fallback priority.");
        first.RemoveLanguageSupportOverride("en-");
        first.SetLanguageSupportOverride("*", false); first.SetLanguageSupportOverride("en", true); Check(Width(root) == 7, "Exact overrides precede wildcard.");
        first.RemoveLanguageSupportOverride("en"); Check(Width(root) == 11, "Removal restores wildcard preference."); first.RemoveLanguageSupportOverride("*");
        first.SetScriptSupportOverride("Latn", false); Check(Width(root) == 11, "Script support has precedence over language.");
        second.SetScriptSupportOverride("Latn", false); Check(Width(root) == 7, "Unsupported faces still supply last-resort glyphs.");
        root.Fallbacks = [first]; Check(Width(root) == 7, "Overrides never forbid a sole fallback.");
        first.SetScriptSupportOverride("*", true); first.RemoveScriptSupportOverride("Latn"); Check(Width(root) == 7, "Wildcard script support executes.");
        using var primary = Bitmap('B', 3, Colors.White); primary.Fallbacks = [second]; primary.SetScriptSupportOverride("Latn", false); primary.SetLanguageSupportOverride("en", false); Check(Width(primary) == 3, "Primary face remains first.");
        using var variation = new FontVariation { BaseFont = first }; root.Fallbacks = [variation, second]; first.SetScriptSupportOverride("Latn", true); second.SetScriptSupportOverride("Latn", true);
        Check(Width(root) == 7, "Variation inherits the base policy."); first.SetLanguageSupportOverride("en", false); Check(Width(root) == 11, "Base policy invalidates realized variations.");
        first.SetLanguageSupportOverride("en", true); Check(root.GetStringSize("B", fontSize: 8).X == 7, "Public cached measurement starts with first face.");
        first.SetScriptSupportOverride("Latn", false); Check(root.GetStringSize("B", fontSize: 8).X == 11, "Public cached measurement invalidates after changes.");
        using var native = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false }; Check(native.IsScriptSupported("Latn") && native.IsScriptSupported("LatnExtra") && !native.IsScriptSupported("latn"), "HarfBuzz reports automatic Latin support.");
        Check(native.IsLanguageSupported("und"), "Automatic language support defaults true."); native.SetLanguageSupportOverride("en", false); Check(!native.IsLanguageSupported("en"), "Query uses exact source override."); native.Data = FontTestFixtures.Hebrew; Check(!native.GetLanguageSupportOverride("en") && native.GetLanguageSupportOverrides().Contains("en"), "Source replacement preserves overrides.");
        using var emojiRoot = Bitmap('B', 6, Colors.White); using var monochrome = Bitmap('A', 3, Colors.White); using var colored = new FontFile { Data = FontTestFixtures.Color, AllowSystemFallback = false };
        colored.SetScriptSupportOverride("*", false); colored.SetLanguageSupportOverride("*", false); emojiRoot.Fallbacks = [monochrome, colored];
        Check(Width(emojiRoot, "A\uFE0F") == Width(colored, "A\uFE0F") && Width(emojiRoot, "A\uFE0E") == 3, "Color emoji presentation: preferred=" + Width(emojiRoot, "A\uFE0F") + ", native=" + Width(colored, "A\uFE0F") + ", text=" + Width(emojiRoot, "A\uFE0E") + ", tier=" + colored.PrimaryData!.FallbackPriority("en", 0x5A737965, true));
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-font-support-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            native.LoadBitmapFont(FontCacheTests.Fixture(directory)); Check(native.GetLanguageSupportOverrides().Contains("en") && !native.IsLanguageSupported("en"), "Bitmap import preserves source policy.");
            var path = System.IO.Path.Combine(directory, "fallback.e2dres"); root.Fallbacks = [first, second]; ResourceSaver.Save(root, path, SaverFlags.BundleResources);
            using var loaded = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore); Check(Width(loaded) == 11, "Typed archive restores policy and fallback identity.");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(FontSupportTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_FONT_SUPPORT_CHILD"] = path;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException(); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh support policy restored."), "Fresh archive process: " + error.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(directory, true); }
        empty.Dispose(); Reject<ObjectDisposedException>(() => empty.GetLanguageSupportOverrides());
        Console.WriteLine("Font support policy checks passed.");
    }
    internal static void RunChild(string path)
    {
        using var font = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore); Check(Width(font) == 11, "Fresh policy controls actual shaping."); Console.WriteLine("Fresh support policy restored.");
    }
    private static void ThrowObserver(Resource _) => throw new MarkerException();
    private sealed class MarkerException : Exception { }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
