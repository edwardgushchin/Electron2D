using Electron2D;

internal static partial class DistanceFieldChecks
{
    private static void Compatibility()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using (var plain = new Window { Size = new(80, 32) })
        {
            var solid = new Solid(); plain.AddChild(solid);
            plain.Ready += _ =>
            {
                RenderingServer.CanvasItemSetDistanceFieldMode(solid.GetCanvasItem(), true);
                RenderingServer.FramePostDraw += () => { using var pixels = plain.GetTexture().GetImage()!; Match(pixels.GetPixel(8, 8), Colors.Blue, "Compatibility untextured mode"); plain.Tree!.Quit(); };
            };
            Engine.Run(plain);
        }
        using var image = Image.CreateFromData(4, 1, false, Image.Format.Rgba8, Samples); using var texture = ImageTexture.CreateFromImage(image);
        using var root = new Window { Size = new(80, 32) }; var painter = new Painter(texture); root.AddChild(painter);
        root.Ready += _ => RenderingServer.CanvasItemSetDistanceFieldMode(painter.GetCanvasItem(), true);
        try { Engine.Run(root); throw new InvalidOperationException("Compatibility unexpectedly executed MSDF drawing."); }
        catch (NotSupportedException error) when (error.Message.Contains("Glyph distance", StringComparison.Ordinal)) { }
        Check(root.IsDisposed && !RenderingServer.IsAvailable, "Unsupported MSDF submission cleans up.");
        Console.WriteLine("Compatibility explicitly rejects retained MSDF output and cleans up.");
    }
    private static void Sessions()
    {
        using var image = Image.CreateFromData(4, 1, false, Image.Format.Rgba8, Samples); using var texture = ImageTexture.CreateFromImage(image);
        using var node = new Painter(texture) { Position = new(8, 8), TextureFilter = TextureFilter.Nearest };
        foreach (var enabled in new[] { true, false })
        {
            using var root = new Window { Size = new(80, 32) }; root.AddChild(node); var frames = 0;
            root.Ready += _ =>
            {
                OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Background);
                if (enabled) RenderingServer.CanvasItemSetDistanceFieldMode(node.GetCanvasItem(), true);
                RenderingServer.FramePostDraw += () =>
                {
                    using var pixels = root.GetTexture().GetImage()!; Match(pixels.GetPixel(32, 12), Expected(1, 0, enabled), "session/default/" + enabled);
                    if (++frames == 2) { root.RemoveChild(node); root.Tree!.Quit(); }
                };
            };
            Engine.Run(root); Check(!node.IsDisposed && !RenderingServer.IsAvailable, "Detached borrowed item survives renderer teardown.");
        }
        Console.WriteLine("Borrowed item retains authored geometry while distance-field mode resets across renderer sessions.");
    }
}
