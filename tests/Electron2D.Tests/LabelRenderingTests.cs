using Electron2D;

internal static partial class RenderingRuntimeTests
{
    // Independent C/FreeType 2.13.3 oracle: tools/coverage/font-raster-oracle.c, profile16/0/128.
    private static readonly byte[] LabelOuterOutline = Convert.FromBase64String("AAAAAAAZwf3/7X4AAAAAAAAAAAAArf//////QQAAAAAAAAAAE/r//////6EAAAAAAAAAAGn////////yCQAAAAAAAADE/////////1cAAAAAAAAf/v////////+xAAAAAAAAef//////////+hIAAAAAANP///////////9nAAAAAC7/////////////wQAAAACJ//////////////4eAAAB4v//////////////dwAAPv///////////////9EAAJn//////9Go+P//////LADl//////8/ALD//////3gA4f/////gAgBT//////91AEjg///VPAAAAI/y//qzDw==");

    private static void VerifyLabels(string backend)
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, SubpixelPositioning = FontSubpixelPositioning.Disabled };
        using var normal = new LabelSettings { Font = font };
        using var effects = new LabelSettings
        {
            Font = font,
            FontColor = Colors.White,
            OutlineSize = 4,
            OutlineColor = Colors.Red,
            ShadowColor = Colors.Yellow,
            ShadowOffset = new(20, 0),
            ShadowSize = 0
        };
        effects.AddStackedOutline(); effects.SetStackedOutlineSize(0, 4); effects.SetStackedOutlineColor(0, Colors.Blue);
        effects.AddStackedShadow(); effects.SetStackedShadowOffset(0, new(40, 0)); effects.SetStackedShadowColor(0, Colors.Green);
        Action<Resource> missedSettings = _ => throw new ApplicationException("expected missed label settings notification");
        effects.Changed += missedSettings;
        using var missedFont = new FontFile { Data = FontTestFixtures.OpenSans, SubpixelPositioning = FontSubpixelPositioning.Disabled };
        Action<Resource> missedFontChange = _ => throw new ApplicationException("expected missed label font notification");
        missedFont.Changed += missedFontChange;
        using var missed = new LabelSettings { Font = missedFont };
        using var theme = new Theme(); theme.SetFont("font", nameof(Label), font); theme.SetColor("font_color", nameof(Label), Colors.Magenta);
        var window = new Window { Size = new(480, 240) }; var frames = 0; uint originalLigature = 0;
        var centered = new Label("A") { Name = "CenteredDefault", Position = new(10, 10), Size = new(60, 30), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextureFilter = TextureFilter.Nearest };
        var rtl = new Label("A") { Name = "RTL", Position = new(100, 10), Size = new(60, 30), LayoutDirection = LayoutDirection.RTL, TextureFilter = TextureFilter.Nearest };
        var wrapped = new Label("AV AV AV") { Name = "Wrapped", Position = new(10, 60), Size = new(30, 80), LabelSettings = normal, AutowrapMode = TextAutowrapMode.WordSmart, ClipText = true, TextureFilter = TextureFilter.Nearest };
        var visible = new Label("AA") { Name = "Visible", Position = new(60, 60), Size = new(80, 30), LabelSettings = normal, VisibleCharactersBehavior = TextVisibleCharactersBehavior.CharsAfterShaping, VisibleCharacters = 1, TextureFilter = TextureFilter.Nearest };
        var ellipsis = new Label("WWWWWWWW") { Name = "Ellipsis", Position = new(170, 60), Size = new(11, 30), LabelSettings = normal, EllipsisChar = "A", TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce, ClipText = true, TextureFilter = TextureFilter.Nearest };
        var clipped = new Label("A") { Name = "Clipped", Position = new(240, 60), Size = new(7, 23), LabelSettings = normal, ClipText = true, TextureFilter = TextureFilter.Nearest };
        var child = new CanvasNode { Name = "OutsideChild", Position = new(20, 0), DrawAction = canvas => canvas.DrawRect(new(0, 0, 4, 4), Colors.Red) }; clipped.AddChild(child);
        var decorated = new Label("A") { Name = "Effects", Position = new(320, 60), Size = new(80, 30), LabelSettings = effects, TextureFilter = TextureFilter.Nearest };
        var observed = new Label("ffi") { Name = "MissedFont", Position = new(10, 170), Size = new(80, 30), LabelSettings = missed, TextureFilter = TextureFilter.Nearest };
        var themed = new Label("A") { Name = "ThemeColor", Position = new(100, 170), Size = new(30, 30), Theme = theme, TextureFilter = TextureFilter.Nearest };
        window.AddChild(centered); window.AddChild(rtl); window.AddChild(wrapped); window.AddChild(visible); window.AddChild(ellipsis);
        window.AddChild(clipped); window.AddChild(decorated); window.AddChild(observed); window.AddChild(themed);
        window.Ready += _ =>
        {
            // Apply the intended rectangles after text policy and parent-direction resolution.
            rtl.Position = new(100, 10); wrapped.Size = new(30, 80); ellipsis.Size = new(11, 30); clipped.Size = new(7, 23);
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                if (frames == 1) File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-labels-{backend}.png"), pixels.SavePNGToBuffer());
                FontRasterPixels(pixels, FontOracleA16, 11, 12, new(34, 19), 1, Colors.White);
                FontRasterPixels(pixels, FontOracleA16, 11, 12, new(149, 16), 1, Colors.White);
                Check(wrapped.GetLineCount() == 3 && wrapped.GetVisibleLineCount() == 3, "The label wraps three real lines inside its available height.");
                Check(FontInk(pixels, new(10, 65, 26, 15)) > 40 && FontInk(pixels, new(10, 91, 26, 15)) > 40 && FontInk(pixels, new(10, 117, 26, 15)) > 40,
                    "Word-smart wrapping records all three visible baselines.");
                for (var y = 0; y < 12; y++) for (var x = 0; x < 11; x++)
                    {
                        var amount = FontOracleA16[y * 11 + x] / 255f;
                        Pixel(pixels, 60 + x, 66 + y, new(amount, amount, amount, 1));
                    }
                Check((FontInk(pixels, new(72, 66, 10, 12)) > 10) == (frames > 1), "After-shaping visibility changes the second glyph without replacing the line.");
                FontRasterPixels(pixels, FontOracleA16, 11, 12, new(170, 66), 1, Colors.White);
                FontRasterPixels(pixels, FontOracleA16, 11, 12, new(240, 66), 1, Colors.White, frames == 1 ? new Rect2i(240, 60, 7, 23) : null);
                Pixel(pixels, 261, 61, frames == 1 ? Colors.Black : Colors.Red);
                LabelEffectPixels(pixels, new(320, 78), frames == 1 ? Colors.White : Colors.Cyan);
                FontRasterPixels(pixels, FontOracleA16, 11, 12, new(340, 66), 1, Colors.Yellow);
                FontRasterPixels(pixels, FontOracleA16, 11, 12, new(360, 66), 1, Colors.Green);
                FontRasterPixels(pixels, FontOracleA16, 11, 12, new(100, 176), 1, frames == 1 ? Colors.Magenta : Colors.Blue);
                var ligature = LabelPixelHash(pixels, new(10, 174, 55, 19));
                if (frames == 1)
                {
                    originalLigature = ligature;
                    visible.VisibleCharacters = -1; clipped.ClipText = false;
                    Reject<ApplicationException>(() => effects.FontColor = Colors.Cyan);
                    Reject<ApplicationException>(() => missedFont.OpenTypeFeatureOverrides = new() { ["liga"] = 0 });
                    theme.SetColor("font_color", nameof(Label), Colors.Blue);
                }
                else
                {
                    Check(ligature != originalLigature, "Font-generation polling redraws changed shaping even when an earlier font observer prevents notification delivery.");
                    window.Tree!.Quit();
                }
            };
        };
        Engine.Run(window); Released(window);
        Check(!font.IsDisposed && !normal.IsDisposed && !effects.IsDisposed && !theme.IsDisposed, "Labels release consumers without disposing borrowed settings, fonts or themes.");
        effects.Changed -= missedSettings; missedFont.Changed -= missedFontChange;
        VerifyLabelWarm(backend, font);
        Console.WriteLine($"Native labels verify default/theme/settings fonts, center/RTL, wrapping, visibility, ellipsis, clipping descendants, stacked effects, missed callbacks and warm frames: {backend}.");
    }

    private static void LabelEffectPixels(Image pixels, Vector2 baseline, Color fill)
    {
        for (var y = -15; y <= 3; y++) for (var x = -4; x <= 14; x++)
            {
                var outer = LabelAlpha(LabelOuterOutline, 16, 16, x + 3, y + 14);
                var inner = LabelAlpha(FontOracleA16Outline, 14, 14, x + 2, y + 13);
                var face = LabelAlpha(FontOracleA16, 11, 12, x, y + 12);
                var r = inner; var g = 0f; var b = outer * (1 - inner);
                r = fill.R * face + r * (1 - face); g = fill.G * face + g * (1 - face); b = fill.B * face + b * (1 - face);
                Pixel(pixels, (int)baseline.X + x, (int)baseline.Y + y, new(r, g, b, 1));
            }
    }
    private static float LabelAlpha(byte[] alpha, int width, int height, int x, int y) =>
        (uint)x < (uint)width && (uint)y < (uint)height ? alpha[y * width + x] / 255f : 0;
    private static uint LabelPixelHash(Image pixels, Rect2i region)
    {
        var hash = 2166136261u;
        for (var y = region.Position.Y; y < region.End.Y; y++) for (var x = region.Position.X; x < region.End.X; x++)
                hash = unchecked((hash ^ (uint)MathF.Round(pixels.GetPixel(x, y).R * 255)) * 16777619u);
        return hash;
    }

    private static void VerifyLabelWarm(string backend, Font font)
    {
        using var settings = new LabelSettings { Font = font, OutlineSize = 2, OutlineColor = Colors.Red, ShadowColor = Colors.Blue, ShadowOffset = new(2, 1) };
        settings.AddStackedOutline(); settings.SetStackedOutlineSize(0, 2); settings.SetStackedOutlineColor(0, Colors.Green);
        var window = new Window { Size = new(320, 160) }; var frames = 0; long before = 0, allocated = 0;
        var label = new Label("AV ffi AV ffi")
        {
            Name = "Active",
            Position = new(10, 10),
            Size = new(100, 130),
            LabelSettings = settings,
            AutowrapMode = TextAutowrapMode.WordSmart,
            ClipText = true,
            VisibleCharactersBehavior = TextVisibleCharactersBehavior.CharsAfterShaping,
            TextureFilter = TextureFilter.Nearest
        };
        window.AddChild(label);
        window.Ready += _ =>
        {
            var tree = window.Tree!;
            tree.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread(); var even = (frames & 1) == 0;
                label.Text = even ? "AV ffi AV ffi" : "ffi AV ffi AV"; label.Size = new(even ? 100 : 70, 130);
                label.HorizontalAlignment = even ? HorizontalAlignment.Left : HorizontalAlignment.Center;
                label.VisibleCharacters = even ? -1 : 9; settings.FontColor = even ? Colors.White : Colors.Cyan;
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames == 128) tree.Quit();
            };
        };
        Engine.Run(window); Released(window);
        Check(frames == 128 && allocated == 0, $"Warmed {backend} active Label text/width/alignment/visibility/settings, layout/record/render allocated {allocated} bytes over64 ProcessFrameStarted-to-FramePostDraw frames.");
    }
}
