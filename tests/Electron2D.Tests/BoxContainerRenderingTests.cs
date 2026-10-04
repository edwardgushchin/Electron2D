using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyBoxLayout(string backend)
    {
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(256, 128) }; var host = new Entity(); window.AddChild(host);
        var box = new HBoxContainer { Position = new(3, 3), Size = new(60, 20) }; host.AddChild(box);
        var a = new NinePatchRect { Name = "A", Texture = texture, Modulate = Colors.Red, CustomMinimumSize = new(10, 8), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var b = new NinePatchRect { Name = "B", Texture = texture, Modulate = Colors.Blue, CustomMinimumSize = new(10, 8), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(a); box.AddChild(b); var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                if (frames == 1)
                {
                    Check(a.Size.X == 28 && b.Position.X == 32, "Native weighted box layout.");
                    Pixel(pixels, 5, 5, Colors.Red); Pixel(pixels, 32, 5, Colors.Black); Pixel(pixels, 36, 5, Colors.Blue);
                    box.LayoutDirection = LayoutDirection.RTL; box.Position = new(3, 3);
                }
                else if (frames == 2)
                {
                    Pixel(pixels, 5, 5, Colors.Blue); Pixel(pixels, 36, 5, Colors.Red); b.Hide();
                }
                else if (frames == 3)
                {
                    Check(a.Size.X == 60, "Hidden sibling reallocates full primary size."); Pixel(pixels, 50, 5, Colors.Red);
                    a.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
                }
                else
                {
                    Check(a.Size.Y == 8 && a.Position.Y == 6, "Native cross-axis shrink."); Pixel(pixels, 5, 5, Colors.Black); Pixel(pixels, 5, 11, Colors.Red);
                    File.WriteAllBytes($"/tmp/electron2d-box-layout-{backend}.png", pixels.SavePNGToBuffer()); window.Tree!.Quit();
                }
            };
        };
        Engine.Run(window); Released(window);
        VerifyBoxWarm(backend, texture);
        Console.WriteLine($"Box layout native weighted sizing, RTL, visibility, shrink and allocations passed: {backend}.");
    }
    private static void VerifyBoxWarm(string backend, Texture texture)
    {
        var window = new Window { Size = new(256, 128) }; var box = new VBoxContainer { Position = new(3, 3), Size = new(40, 30) }; window.AddChild(box);
        box.AddChild(new NinePatchRect { Name = "A", Texture = texture, SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        box.AddChild(new NinePatchRect { Name = "B", Texture = texture, SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        var frames = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!;
            RenderingServer.FramePreDraw += () => { before = GC.GetAllocatedBytesForCurrentThread(); box.Size = frames % 2 == 0 ? new(40, 30) : new(42, 32); box.Alignment = frames % 2 == 0 ? AlignmentMode.Begin : AlignmentMode.Center; };
            RenderingServer.FramePostDraw += () =>
            {
                if (++frames > 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frames == 128) window.Tree!.Quit();
            };
        };
        Engine.Run(window); Released(window); Check(allocated == 0, $"Warmed {backend} box resize/layout/render allocated {allocated} bytes.");
    }
}
