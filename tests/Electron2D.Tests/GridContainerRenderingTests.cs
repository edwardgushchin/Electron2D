using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyGridLayout(string backend)
    {
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(256, 128) }; var host = new Entity(); window.AddChild(host);
        var grid = new GridContainer { Position = new(3, 3), Size = new(61, 41), Columns = 2 }; host.AddChild(grid);
        var a = new NinePatchRect { Name = "A", Texture = texture, Modulate = Colors.Red, CustomMinimumSize = new(10, 8), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var b = new NinePatchRect { Name = "B", Texture = texture, Modulate = Colors.Blue, CustomMinimumSize = new(10, 8), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var c = new NinePatchRect { Name = "C", Texture = texture, Modulate = Colors.Green, CustomMinimumSize = new(10, 8), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        grid.AddChild(a); grid.AddChild(b); grid.AddChild(c); var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                if (frames == 1)
                {
                    Check(a.Size == new Vector2(29, 19) && b.Size == new Vector2(28, 19) && c.Size == new Vector2(29, 18), "Native grid assigns pixel remainders to the first expanded column and row.");
                    Check(b.Position == new Vector2(33, 0) && c.Position == new Vector2(0, 23), "Native grid preserves column and row separation.");
                    Pixel(pixels, 31, 21, Colors.Red); Pixel(pixels, 32, 5, Colors.Black); Pixel(pixels, 36, 5, Colors.Blue);
                    Pixel(pixels, 5, 22, Colors.Black); Pixel(pixels, 5, 26, Colors.Green); Pixel(pixels, 36, 26, Colors.Black);
                    grid.LayoutDirection = LayoutDirection.RTL; grid.Position = new(3, 3);
                }
                else if (frames == 2)
                {
                    Check(a.Position == new Vector2(32, 0) && b.Position == Vector2.Zero && c.Position == new Vector2(32, 23), "Native grid mirrors columns while retaining row order and remainders.");
                    Pixel(pixels, 5, 5, Colors.Blue); Pixel(pixels, 35, 5, Colors.Red); Pixel(pixels, 5, 26, Colors.Black); Pixel(pixels, 35, 26, Colors.Green);
                    b.Hide();
                }
                else if (frames == 3)
                {
                    Check(a.Size.Y == 41 && c.Size.Y == 41 && c.Position == Vector2.Zero, "Hidden grid child releases its cell and the remaining row expands.");
                    Pixel(pixels, 5, 40, Colors.Green); Pixel(pixels, 50, 40, Colors.Red); Pixel(pixels, 32, 40, Colors.Black);
                    b.Show(); grid.MoveChild(c, 0); grid.LayoutDirection = LayoutDirection.LTR; grid.Position = new(3, 3);
                }
                else if (frames == 4)
                {
                    Check(c.Position == Vector2.Zero && a.Position == new Vector2(33, 0) && b.Position == new Vector2(0, 23), "Native grid follows restored visibility and child order.");
                    Pixel(pixels, 5, 5, Colors.Green); Pixel(pixels, 36, 5, Colors.Red); Pixel(pixels, 5, 26, Colors.Blue); Pixel(pixels, 36, 26, Colors.Black);
                    grid.Size = new(65, 45);
                }
                else if (frames == 5)
                {
                    Check(c.Size == new Vector2(31, 21) && a.Size == new Vector2(30, 21) && b.Size == new Vector2(31, 20) && b.Position.Y == 25, "Native grid resize redistributes both axes.");
                    Pixel(pixels, 66, 5, Colors.Red); Pixel(pixels, 5, 46, Colors.Blue); Pixel(pixels, 5, 26, Colors.Black); Pixel(pixels, 36, 5, Colors.Black);
                    File.WriteAllBytes($"/tmp/electron2d-grid-layout-{backend}.png", pixels.SavePNGToBuffer()); grid.Columns = 3;
                }
                else if (frames == 6)
                {
                    Check(c.Size == new Vector2(19, 45) && a.Position == new Vector2(23, 0) && b.Position == new Vector2(46, 0), "Changing grid columns reflows the retained children.");
                    Pixel(pixels, 5, 5, Colors.Green); Pixel(pixels, 28, 5, Colors.Red); Pixel(pixels, 50, 46, Colors.Blue); Pixel(pixels, 23, 5, Colors.Black);
                    c.Hide(); grid.Columns = 1; grid.VSeparation = 0;
                    a.CustomMinimumSize = b.CustomMinimumSize = new(10, 10);
                    a.CustomMaximumSize = new(-1, 20); b.CustomMaximumSize = new(-1, 80); grid.Size = new(20, 100);
                }
                else
                {
                    Check(a.Position == Vector2.Zero && a.Size == new Vector2(20, 20) && b.Position == new Vector2(0, 20) && b.Size == new Vector2(20, 80), "Capped grid rows advance by their final allocated height without overlapping.");
                    Pixel(pixels, 5, 22, Colors.Red); Pixel(pixels, 5, 23, Colors.Blue); Pixel(pixels, 5, 102, Colors.Blue); Pixel(pixels, 5, 103, Colors.Black);
                    window.Tree!.Quit();
                }
            };
        };
        Engine.Run(window); Released(window);
        VerifyGridWarm(backend, texture);
        Console.WriteLine($"Grid native remainders, RTL, visibility, reorder, resize, column reflow, capped rows and warmed process/layout/render passed: {backend}.");
    }

    private static void VerifyGridWarm(string backend, Texture texture)
    {
        var window = new Window { Size = new(256, 128) }; var grid = new GridContainer { Position = new(3, 3), Size = new(42, 32), Columns = 2 }; window.AddChild(grid);
        var first = new NinePatchRect { Name = "A", Texture = texture, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        grid.AddChild(first);
        grid.AddChild(new NinePatchRect { Name = "B", Texture = texture, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        grid.AddChild(new NinePatchRect { Name = "C", Texture = texture, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        var frames = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            var tree = window.Tree!;
            tree.ProcessFrameStarted += frameTree =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                grid.Size = frames % 2 == 0 ? new(40, 30) : new(42, 32);
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                Check(first.Size == (frames % 2 == 0 ? new Vector2(18, 13) : new Vector2(19, 14)), "Warmed grid deferred layout finishes before native rendering.");
                if (++frames == 128) tree.Quit();
            };
        };
        Engine.Run(window); Released(window);
        Check(frames == 128 && allocated == 0, $"Warmed {backend} grid ProcessFrameStarted-to-FramePostDraw resize/layout/render allocated {allocated} bytes over 64 measured frames.");
    }
}
