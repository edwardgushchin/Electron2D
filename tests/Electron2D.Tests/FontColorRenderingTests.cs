using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyFontColors(string backend)
    {
        using var font = new FontFile { Data = FontTestFixtures.Color, SubpixelPositioning = FontSubpixelPositioning.Disabled };
        var window = new Window { Size = new(96, 40) };
        var node = new CanvasNode
        {
            TextureFilter = TextureFilter.Nearest,
            DrawAction = canvas =>
            {
                var tint = new Color(.5f, 1, 1, .5f);
                font.ModulateColorGlyphs = false; canvas.DrawChar(font, new(8, 20), "A", modulate: tint);
                font.ModulateColorGlyphs = true; canvas.DrawChar(font, new(32, 20), "A", modulate: tint);
                canvas.DrawCharOutline(font, new(56, 20), "A", size: -1, modulate: tint);
            }
        };
        window.AddChild(node);
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback();
                Pixel(pixels, 11, 12, new(.5f, 0, 0)); Pixel(pixels, 19, 12, new(0, 0, .5f));
                Pixel(pixels, 35, 12, new(.25f, 0, 0)); Pixel(pixels, 43, 12, new(0, 0, .5f));
                Pixel(pixels, 59, 12, new(.5f, 0, 0)); Pixel(pixels, 67, 12, new(0, 0, .5f));
                window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(!font.IsDisposed, "Color glyph drawing borrows the font.");
        Console.WriteLine($"Native COLR glyphs preserve palette RGB, always modulate alpha and honor explicit RGB modulation: {backend}.");
    }
}
