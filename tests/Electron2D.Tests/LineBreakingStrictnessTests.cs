using Electron2D;
using System.Globalization;
using System.Text;
using System.Text.Json;

internal static class LineBreakingStrictnessTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject(Action action) { try { action(); } catch (ArgumentOutOfRangeException) { return; } throw new InvalidOperationException("Invalid strictness was accepted"); }
    internal static void Run(string profile)
    {
        var mode = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_STRICTNESS_MODE") ?? "0", CultureInfo.InvariantCulture);
        Check(mode is >= 0 and <= 3, "Fixture mode"); VerifySettings();
        ProjectSettings.Set(ProjectSettings.LineBreakingStrictness, (mode + 1) % 4);
        Check(!NativeTextBreak.IsNonprinting('A'), "Unrelated native Unicode initialization");
        NativeTextBreak.Fill("", "ja", [0], new bool[1], new bool[1], new bool[1]);
        var feature = Environment.GetEnvironmentVariable("ELECTRON2D_STRICTNESS_FEATURE") == "1";
        if (feature) { ProjectSettings.SetFeatureOverride(ProjectSettings.LineBreakingStrictness, "strictness-fixture", mode); ProjectSettings.AddCustomFeature("strictness-fixture"); }
        else ProjectSettings.Set(ProjectSettings.LineBreakingStrictness, mode);
        Check(ProjectSettings.GetWithOverride(ProjectSettings.LineBreakingStrictness) == mode, "Effective runtime setting before first nonempty boundary request");
        Boundaries("あぁい", "ja@lb=strict", [2, 3]);
        CheckDefault(mode);
        if (feature) ProjectSettings.RemoveCustomFeature("strictness-fixture");
        ProjectSettings.Set(ProjectSettings.LineBreakingStrictness, (mode + 1) % 4);
        CheckDefault(mode);
        Boundaries("あぁい", "ja_JP", mode == 3 ? [2, 3] : [1, 2, 3]);
        Boundaries("あぁい", "ja@calendar=japanese", [1, 2, 3]);
        foreach (var item in new[] { ("loose", new[] { 1, 2, 3 }), ("normal", new[] { 1, 2, 3 }), ("strict", new[] { 2, 3 }) })
            Boundaries("あぁい", "ja@lb=" + item.Item1, item.Item2);
        Boundaries("a\nb", "en", [2, 3]); Boundaries("ภาษาไทยภาษาไทย", "th", [4, 7, 11, 14]);
        VerifyConsumers(mode); VerifyWarmCache(mode);
        if (profile != "managed") Native(profile, mode);
        Console.WriteLine(JsonSerializer.Serialize(new { profile = "line-breaking-strictness", backend = profile, mode, feature, cachedPolicySurvivesEdits = true }));
    }
    private static void VerifySettings()
    {
        Check(ProjectSettings.Get(ProjectSettings.LineBreakingStrictness) == 0 && ProjectSettings.IsRestartRequired(ProjectSettings.LineBreakingStrictness), "Auto default and restart metadata");
        Reject(() => ProjectSettings.Set(ProjectSettings.LineBreakingStrictness, -1)); Reject(() => ProjectSettings.Set(ProjectSettings.LineBreakingStrictness, 4));
        Reject(() => ProjectSettings.SetFeatureOverride(ProjectSettings.LineBreakingStrictness, "invalid", int.MaxValue));
        Check(ProjectSettings.Get(ProjectSettings.LineBreakingStrictness) == 0 && ProjectSettings.GetFeatureOverrides(ProjectSettings.LineBreakingStrictness).Count == 0, "Invalid values leave base and overrides unchanged");
        var folder = System.IO.Directory.CreateTempSubdirectory("electron2d-line-strictness-");
        try
        {
            using var authored = new ProjectSettingsRegistry(folder.FullName, System.IO.Path.Combine(folder.FullName, "user"));
            Check(authored.Get(ProjectSettings.LineBreakingStrictness) == 0 && authored.IsRestartRequired(ProjectSettings.LineBreakingStrictness), "Independent built-in registration");
            authored.Set(ProjectSettings.LineBreakingStrictness, 2); authored.SetFeatureOverride(ProjectSettings.LineBreakingStrictness, "ja-project", 3);
            var file = System.IO.Path.Combine(folder.FullName, "strictness.e2d"); authored.SaveCustom(file);
            using var restored = new ProjectSettingsRegistry(folder.FullName, System.IO.Path.Combine(folder.FullName, "user")); restored.LoadCustom(file);
            Check(restored.Get(ProjectSettings.LineBreakingStrictness) == 2, "Base save/load"); restored.AddCustomFeature("ja-project");
            Check(restored.GetWithOverride(ProjectSettings.LineBreakingStrictness) == 3, "Override save/load"); restored.Reset(ProjectSettings.LineBreakingStrictness);
            Check(restored.Get(ProjectSettings.LineBreakingStrictness) == 0, "Reset restores Auto");
        }
        finally { folder.Delete(true); }
    }
    private static void CheckDefault(int mode)
    {
        Boundaries("あぁい", "ja", mode == 3 ? [2, 3] : [1, 2, 3]);
        Boundaries("あーい", "ja", mode == 3 ? [2, 3] : [1, 2, 3]);
        Boundaries("あ・い", "ja", mode == 1 ? [1, 2, 3] : [2, 3]);
    }
    private static int[] Offsets(string text)
    {
        var result = new List<int> { 0 }; var offset = 0; foreach (var rune in text.EnumerateRunes()) { offset += rune.Utf16SequenceLength; result.Add(offset); }
        return result.ToArray();
    }
    private static void Boundaries(string text, string language, int[] expected)
    {
        var offsets = Offsets(text); var line = new bool[offsets.Length]; var words = new bool[offsets.Length]; var ends = new bool[offsets.Length];
        NativeTextBreak.Fill(text, language, offsets, line, words, ends);
        var actual = Enumerable.Range(1, offsets.Length - 1).Where(i => line[i]).ToArray();
        Check(actual.SequenceEqual(expected), "Native oracle boundaries " + language + ": " + string.Join(',', actual));
    }
    private static TextParagraph Paragraph(Font font, string language, string text = "あぁい")
    {
        var result = new TextParagraph { Width = font.GetStringSize("あ", fontSize: 16).X + .25f, BreakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound };
        result.AddString(text, font, 16, language); return result;
    }
    private static void VerifyConsumers(int mode)
    {
        using var font = new FontFile { Data = FontTestFixtures.CJK, AllowSystemFallback = false };
        using var paragraph = Paragraph(font, "ja"); var expected = mode == 3 ? 2 : 3;
        Check(paragraph.GetLineCount() == expected && paragraph.GetLineRange(0) == new Vector2i(0, mode == 3 ? 2 : 1), "Public paragraph consumes selected native rules");
        var server = TextServerManager.GetPrimaryInterface(); var shaped = server.CreateShapedText();
        try
        {
            server.ShapedTextAddString(shaped, "あぁい", [font], 16, language: "ja");
            var breaks = server.ShapedTextGetLineBreaks(shaped, paragraph.Width);
            Check(breaks.SequenceEqual(mode == 3 ? [0, 2, 2, 3] : new[] { 0, 1, 1, 2, 2, 3 }), "Public retained TextServer wrapping");
        }
        finally { server.FreeRID(shaped); }
        using var settings = new LabelSettings { Font = font, FontSize = 16 }; var root = new Node();
        var label = new Label("あぁい") { LabelSettings = settings, Language = "ja", AutowrapMode = TextAutowrapMode.Word, Size = new(paragraph.Width, 96) }; root.AddChild(label);
        using var tree = new SceneTree(root); Check(label.GetLineCount() == expected, "Actual scene Label consumes project defaults");
        label.Text = "あ・い"; Check(label.GetLineCount() == (mode == 1 ? 3 : 2), "Scene Label distinguishes Loose from Normal and Auto");
    }
    private static void VerifyWarmCache(int mode)
    {
        var offsets = Offsets("あぁい"); var line = new bool[offsets.Length]; var words = new bool[offsets.Length]; var ends = new bool[offsets.Length];
        for (var i = 0; i < 256; i++) NativeTextBreak.Fill("あぁい", "ja", offsets, line, words, ends);
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 1024; i++) NativeTextBreak.Fill("あぁい", "ja", offsets, line, words, ends);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0 && line[1] == (mode != 3), "1024 prepared boundary requests owner/all bytes " + ownerBytes + "/" + allBytes);
        Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => { for (var i = 0; i < 16; i++) CheckDefault(mode); }))).GetAwaiter().GetResult();
    }
    private static void Native(string renderer, int mode)
    {
        var old = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var font = new FontFile { Data = FontTestFixtures.CJK, AllowSystemFallback = false };
        using var actual = Paragraph(font, "ja"); using var expected = Paragraph(font, "ja@lb=" + new[] { "auto", "loose", "normal", "strict" }[mode]);
        using var actualDot = Paragraph(font, "ja", "あ・い"); using var expectedDot = Paragraph(font, "ja@lb=" + new[] { "auto", "loose", "normal", "strict" }[mode], "あ・い");
        var root = new Window { Size = new(128, 192) }; root.AddChild(new Drawing(actual, expected, actualDot, expectedDot)); var frames = 0; long owner = 0, ownerBytes = -1;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames == 8)
                {
                    using var image = root.GetTexture().GetImage()!; var visible = 0;
                    for (var y = 0; y < 186; y++) for (var x = 0; x < 60; x++) { var pixel = image.GetPixel(x, y); Check(pixel == image.GetPixel(x + 64, y), "Project default and explicit locale native pixels"); if (pixel.R > .1) visible++; }
                    Check(visible > 20 && actual.GetLineCount() == (mode == 3 ? 2 : 3) && actualDot.GetLineCount() == (mode == 1 ? 3 : 2), "Native wrapped CJK glyphs are visible");
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_STRICTNESS_CAPTURE") is { } capture) image.SavePNG(capture);
                }
                if (frames == 32) owner = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 96) { ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; Check(ownerBytes == 0, "64 prepared native retained-text frames allocate zero owner bytes"); root.Tree!.Quit(); }
            };
        };
        try { Check(Engine.Run(root) == 0 && frames == 96 && ownerBytes == 0, "Real native text lifecycle"); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, old); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class Drawing(TextParagraph actual, TextParagraph expected, TextParagraph actualDot, TextParagraph expectedDot) : Entity
    {
        protected override void OnDraw() { actual.Draw(this, new(4, 4)); expected.Draw(this, new(68, 4)); actualDot.Draw(this, new(4, 100)); expectedDot.Draw(this, new(68, 100)); }
    }
}
