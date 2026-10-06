using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyTabBarRendering(string backend)
    {
        using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); image.Fill(Colors.Blue); using var icon = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(340, 148) };
        var bar = new TabBar { Name = "MainTabs", Position = new(12, 12), Size = new(260, 40), CustomMaximumSize = new(260, -1), TabCloseDisplayPolicy = TabBar.CloseButtonDisplayPolicy.ShowAlways };
        bar.AddTab("Alpha", icon); bar.AddTab("Beta"); bar.AddTab("Gamma");
        var overflow = new TabBar { Name = "OverflowTabs", Position = new(12, 72), Size = new(160, 40), CustomMaximumSize = new(160, -1), LayoutDirection = LayoutDirection.RTL, ScrollToSelected = false, MaxTabWidth = 80 };
        for (var i = 0; i < 5; i++) overflow.AddTab("Long title " + i);
        window.AddChild(bar); window.AddChild(overflow); var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                var selectedRect = bar.GetTabRect(bar.CurrentTab); var selected = pixels.GetPixel((int)(bar.Position.X + selectedRect.Position.X + 4), 28);
                Check(selected.R > .15f && selected.R < .22f, $"Native {backend} selected tab uses its themed style: {selected}.");
                var glyphs = 0; for (var x = (int)bar.Position.X; x < bar.Position.X + 200; x++) for (var y = 17; y < 46; y++) { var color = pixels.GetPixel(x, y); if (color.R > .65f && color.G > .65f && color.B > .65f) glyphs++; }
                Check(glyphs > 40, $"Native {backend} tabs draw shaped titles and close icons.");
                Check(overflow.GetOffsetButtonsVisible() && (frames > 1 || overflow.GetTabRect(0).Position.X >= 60 && overflow.GetTabIdxAtPoint(overflow.GetTabRect(0).GetCenter()) == 0), "RTL overflow has mirrored tabs and live navigation.");
                var outside = pixels.GetPixel(280, 30); Check(outside.R < .02f && outside.G < .02f && outside.B < .02f, $"Native {backend} tab strip leaves pixels outside bounds clear.");
                if (frames == 1) { bar.CurrentTab = 1; bar.SetTabDisabled(0, true); overflow.EnsureTabVisible(4); return; }
                Check(overflow.GetTabOffset() > 0 && overflow.GetTabIdxAtPoint(overflow.GetTabRect(4).GetCenter()) == 4, "Native frame reveals a trailing RTL tab.");
                var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-tabs-{backend}.png"); File.WriteAllBytes(path, pixels.SavePNGToBuffer()); window.Tree!.Quit();
            };
        };
        Engine.Run(window); Released(window); Check(frames == 2, $"Native {backend} tab frames completed.");
        VerifyTabBarWarm(backend);
        Console.WriteLine($"TabBar themed text/icons, selection, close, RTL/clipping and live scroll rendered on {backend}.");
    }
    private static void VerifyTabBarWarm(string backend)
    {
        var window = new Window { Size = new(240, 100) }; var bar = new TabBar { Name = "MainTabs", Position = new(12, 12), Size = new(200, 40), CustomMaximumSize = new(200, -1), TabCloseDisplayPolicy = TabBar.CloseButtonDisplayPolicy.ShowAlways };
        bar.AddTab("AAA"); bar.AddTab("BBB"); window.AddChild(bar); var frames = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); bar.CurrentTab = frames & 1; };
            RenderingServer.FramePostDraw += () => { if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before; if (++frames == 128) window.Tree!.Quit(); };
        };
        Engine.Run(window); Released(window); Check(frames == 128 && allocated == 0, $"Native {backend} warmed selection/layout/record/render allocated {allocated} managed bytes over 64 active frames.");
        Console.WriteLine($"TabBar {backend}: 64 warmed active selection frames, {allocated} managed bytes.");
    }
}
