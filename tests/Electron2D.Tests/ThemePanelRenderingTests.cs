using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyThemePanels(string backend)
    {
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var inherited = new Theme(); using var windowTheme = new Theme();
        using var blue = new StyleBoxFlat { BGColor = Colors.Blue, AntiAliasing = false };
        using var red = new StyleBoxFlat { BGColor = Colors.Red, AntiAliasing = false }; red.SetContentMarginAll(4);
        using var yellow = new StyleBoxFlat { BGColor = Colors.Yellow, AntiAliasing = false };
        using var local = new StyleBoxFlat { BGColor = Colors.Magenta, AntiAliasing = false, ContentMarginLeft = 6, ContentMarginTop = 3, ContentMarginRight = 2, ContentMarginBottom = 5 };
        using var green = new StyleBoxFlat { BGColor = Colors.Green, AntiAliasing = false };
        using var windowPanel = new StyleBoxFlat { BGColor = Colors.Blue, AntiAliasing = false }; windowPanel.SetContentMarginAll(2);
        inherited.SetStyleBox("panel", "Panel", blue); inherited.SetStyleBox("panel", "PanelContainer", red);
        windowTheme.SetStyleBox("panel", "Panel", green); windowTheme.SetStyleBox("panel", "PanelContainer", windowPanel);
        var window = new Window { Size = new(160, 110) };
        var baseline = new Panel { Name = "DefaultPanel", Position = new(5, 5), Size = new(30, 20) }; window.AddChild(baseline);
        var host = new Control { Name = "ThemeHost", Size = new(150, 100) }; window.AddChild(host);
        var panel = new Panel { Name = "Panel", Position = new(5, 40), Size = new(30, 20) }; host.AddChild(panel);
        var container = new PanelContainer { Name = "Container", Position = new(45, 40), Size = new(40, 30) }; host.AddChild(container);
        var child = new NinePatchRect { Name = "Content", Texture = texture, Modulate = Colors.Green, CustomMinimumSize = new(8, 6) }; container.AddChild(child);
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                // Resource notifications and the resulting layout use separate deferred batches.
                if (++frames % 2 != 0) return;
                using var pixels = server.Readback(); var phase = frames / 2;
                if (phase == 1 || phase == 7)
                {
                    Pixel(pixels, 15, 15, new(.06f, .06f, .06f, 1)); Pixel(pixels, 15, 50, new(.06f, .06f, .06f, 1));
                    Check(panel.GetMinimumSize() == Vector2.Zero && container.GetMinimumSize() == new Vector2(8, 6), "Default Panel has no intrinsic minimum and PanelContainer starts with zero content margins.");
                    Check(child.Position == Vector2.Zero && child.Size == new Vector2(40, 30), "Default panel content fills the container.");
                    Pixel(pixels, 46, 41, Colors.Green);
                    if (phase == 1) host.Theme = inherited; else window.Tree!.Quit();
                }
                else if (phase == 2 || phase == 5)
                {
                    Pixel(pixels, 15, 50, Colors.Blue); Pixel(pixels, 46, 41, Colors.Red); Pixel(pixels, 50, 45, Colors.Green);
                    Check(child.Position == new Vector2(4, 4) && child.Size == new Vector2(32, 22) && container.GetMinimumSize() == new Vector2(16, 14), "Inherited theme supplies panel drawing and four-sided content layout.");
                    if (phase == 2) { panel.AddThemeStyleBoxOverride("panel", yellow); container.AddThemeStyleBoxOverride("panel", local); }
                    else { host.Theme = null; window.Theme = windowTheme; }
                }
                else if (phase == 3)
                {
                    Pixel(pixels, 15, 50, Colors.Yellow); Pixel(pixels, 50, 45, Colors.Magenta); Pixel(pixels, 52, 45, Colors.Green);
                    Check(child.Position == new Vector2(6, 3) && child.Size == new Vector2(32, 22), "Local style overrides replace inherited drawing and asymmetric content offsets.");
                    local.BGColor = Colors.Cyan; local.ContentMarginLeft = 8; local.ContentMarginRight = 4;
                }
                else if (phase == 4)
                {
                    Pixel(pixels, 15, 50, Colors.Yellow); Pixel(pixels, 52, 45, Colors.Cyan); Pixel(pixels, 54, 45, Colors.Green);
                    Check(child.Position == new Vector2(8, 3) && child.Size == new Vector2(28, 22) && container.GetMinimumSize() == new Vector2(20, 14), "Borrowed style changes automatically redraw and refresh deferred content layout.");
                    File.WriteAllBytes($"/tmp/electron2d-theme-panels-{backend}.png", pixels.SavePNGToBuffer());
                    panel.RemoveThemeStyleBoxOverride("panel"); container.RemoveThemeStyleBoxOverride("panel");
                }
                else
                {
                    Pixel(pixels, 15, 15, Colors.Green); Pixel(pixels, 15, 50, Colors.Green); Pixel(pixels, 46, 41, Colors.Blue); Pixel(pixels, 48, 43, Colors.Green);
                    Check(child.Position == new Vector2(2, 2) && child.Size == new Vector2(36, 26), "Clearing a Control theme reveals the inherited Window theme.");
                    window.Theme = null;
                }
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(!inherited.IsDisposed && !local.IsDisposed && !texture.IsDisposed, "Theme panel consumers retain borrowed resource ownership.");
        VerifyThemePanelWarm(backend, texture);
        Console.WriteLine($"Theme panels native defaults, Control/Window inheritance, overrides, borrowed mutations, margins, clearing and warmed frames passed: {backend}.");
    }

    private static void VerifyThemePanelWarm(string backend, Texture texture)
    {
        using var style = new StyleBoxFlat { BGColor = Colors.Red, AntiAliasing = false }; style.SetContentMarginAll(4);
        var window = new Window { Size = new(160, 100) }; var container = new PanelContainer { Position = new(5, 5), Size = new(44, 34) };
        container.AddThemeStyleBoxOverride("panel", style); window.AddChild(container);
        var child = new NinePatchRect { Texture = texture, Modulate = Colors.Green }; container.AddChild(child);
        var frames = 0; var draws = 0; long before = 0, allocated = 0; container.Draw += _ => draws++;
        window.Ready += _ =>
        {
            var tree = window.Tree!;
            tree.ProcessFrameStarted += frameTree =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                style.BGColor = frames % 2 == 0 ? Colors.Red : Colors.Blue;
                style.SetContentMarginAll(frames % 2 == 0 ? 4 : 5);
                container.Size = frames % 2 == 0 ? new(40, 30) : new(44, 34);
            };
            RenderingServer.Instance!.FramePostDraw += () =>
            {
                if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                Check(child.Position == (frames % 2 == 0 ? new Vector2(4, 4) : new Vector2(5, 5)) && child.Size == (frames % 2 == 0 ? new Vector2(32, 22) : new Vector2(34, 24)), "Active style changes reach the native frame's content layout.");
                if (++frames == 128) tree.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(frames == 128 && draws == 128 && allocated == 0, $"Warmed {backend} theme notification/layout/recording/render allocated {allocated} bytes over 64 ProcessFrameStarted-to-FramePostDraw frames; recordings={draws}.");
    }
}
