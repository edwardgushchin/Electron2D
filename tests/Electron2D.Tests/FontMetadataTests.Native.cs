using Electron2D;

internal static partial class FontMetadataTests
{
    private sealed class TextCanvas(Font normal, Font selected) : Entity
    {
        protected override void OnDraw()
        {
            DrawString(normal, new(8, 28), "A", fontSize: 20);
            DrawString(selected, new(40, 28), "A", fontSize: 20);
        }
    }
    internal static void RunNative()
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        try
        {
            foreach (var backend in new[] { "gpu", "compatibility" })
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
                using var font = new FontFile { Data = FontTestFixtures.Metadata, Hinting = FontHinting.None, SubpixelPositioning = FontSubpixelPositioning.Disabled };
                Check(font.GetSupportedFeatureList()[SS01].IsBoolean, "Discover the selector from the public font.");
                using var selected = new FontVariation { BaseFont = font, OpenTypeFeatures = new() { ["ss01"] = 1 } };
                var window = new Window { Size = new(96, 48) }; var canvas = new TextCanvas(font, selected); window.AddChild(canvas);
                var frames = 0;
                window.Ready += _ =>
                {
                    RenderingServer.SetDefaultClearColor(Colors.Black);
                    RenderingServer.FramePostDraw += () =>
                    {
                        using var image = RenderingServer.Service!.Readback();
                        Check(image.GetPixel(11, 20).R > .9f && image.GetPixel(19, 20).R < .05f, "Ordinary primary glyph keeps its narrow pixels.");
                        Check(image.GetPixel(43, 20).R > .9f && (frames == 0 ? image.GetPixel(51, 20).R > .9f : image.GetPixel(51, 20).R < .05f), "Discovered feature selects the wide glyph and can be disabled in the same consumer.");
                        if (++frames == 1) { selected.OpenTypeFeatures = new() { ["ss01"] = 0 }; canvas.QueueRedraw(); }
                        else window.Tree!.Quit();
                    };
                };
                Check(Engine.Run(window) == 0 && frames == 2, "Actual feature-driven text frames complete the native lifecycle.");
                Console.WriteLine($"Font metadata {backend}: discovered feature drives distinct glyph pixels and live disablement.");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); }
    }
}
