using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyFlowLayout(string backend)
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(128, 96), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var host = new Control(); window.AddChild(host);
        var flow = new FlowContainer { Position = new(3, 3), Size = new(40, 40), VSeparation = 3 }; host.AddChild(flow);
        var a = new NinePatchRect { Name = "A", Texture = texture, Modulate = Colors.Red, CustomMinimumSize = new(10, 10) };
        var b = new NinePatchRect { Name = "B", Texture = texture, Modulate = Colors.Blue, CustomMinimumSize = new(20, 12) };
        var c = new NinePatchRect { Name = "C", Texture = texture, Modulate = Colors.Green, CustomMinimumSize = new(15, 8) };
        flow.AddChild(a); flow.AddChild(b); flow.AddChild(c); var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback();
                if (frames == 0)
                {
                    Check(flow.GetLineCount() == 2 && c.Position == new Vector2(0, 15), "Native flow wraps before third child.");
                    Pixel(pixels, 4, 4, Colors.Red); Pixel(pixels, 16, 4, Colors.Black); Pixel(pixels, 18, 4, Colors.Blue); Pixel(pixels, 4, 17, Colors.Black); Pixel(pixels, 4, 19, Colors.Green);
                    flow.Alignment = AlignmentMode.Center;
                }
                else if (frames == 1)
                {
                    Check(a.Position.X == 3 && c.Position.X == 12, "Native center alignment uses residual space independently per wrap.");
                    Pixel(pixels, 4, 4, Colors.Black); Pixel(pixels, 7, 4, Colors.Red); Pixel(pixels, 16, 19, Colors.Green);
                    flow.LastWrapAlignment = FlowContainer.LastWrapAlignmentMode.End;
                }
                else if (frames == 2)
                {
                    Check(c.Position.X == 22, "Native last-wrap end aligns relative to previous occupied group.");
                    Pixel(pixels, 16, 19, Colors.Black); Pixel(pixels, 26, 19, Colors.Green);
                    flow.LayoutDirection = LayoutDirection.RTL; flow.Position = new(3, 3);
                }
                else if (frames == 3)
                {
                    Check(a.Position.X == 27 && b.Position.X == 3 && c.Position.X == 3, "Native RTL mirrors allocation rectangles.");
                    Pixel(pixels, 7, 4, Colors.Blue); Pixel(pixels, 31, 4, Colors.Red); Pixel(pixels, 7, 19, Colors.Green);
                    flow.ReverseFill = true;
                }
                else if (frames == 4)
                {
                    Check(a.Position.Y == 28 && c.Position.Y == 17, "Native reverse fill wraps upward.");
                    Pixel(pixels, 31, 32, Colors.Red); Pixel(pixels, 7, 21, Colors.Green); Pixel(pixels, 31, 4, Colors.Black);
                    flow.Alignment = AlignmentMode.Begin; flow.LastWrapAlignment = FlowContainer.LastWrapAlignmentMode.Inherit; flow.Vertical = true;
                }
                else
                {
                    Check(flow.GetLineCount() == 1 && b.Position.Y == 13 && c.Position.Y == 28, "Native vertical filling changes primary axis while RTL/reverse cancel.");
                    Pixel(pixels, 4, 4, Colors.Red); Pixel(pixels, 4, 17, Colors.Blue); Pixel(pixels, 4, 32, Colors.Green); window.Tree!.Quit();
                }
                var profile = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy" ? $"{backend}-dummy" : backend;
                pixels.SavePNG($"/tmp/e2d-flow-{profile}-{frames++}.png");
            };
        };
        Engine.Instance.Run(window); Released(window); Check(frames == 6, "Native flow completed all visual states.");
        VerifyFlowWarm(backend, texture);
        Console.WriteLine($"Flow native wrap/alignment/last-wrap/RTL/reverse/orientation and warm layout/render passed ({backend}).");
    }
    private static void VerifyFlowWarm(string backend, Texture texture)
    {
        var window = new Window { Size = new(128, 96), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var flow = new HFlowContainer { Position = new(3, 3), Size = new(40, 40), VSeparation = 3 }; window.AddChild(flow);
        foreach (var (name, size) in new[] { ("A", new Vector2(10, 10)), ("B", new Vector2(20, 12)), ("C", new Vector2(15, 8)) }) flow.AddChild(new NinePatchRect { Name = name, Texture = texture, CustomMinimumSize = size });
        var frames = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames < 84) flow.Size = new(frames % 2 == 0 ? 40 : 80, 40);
            };
            RenderingServer.Instance!.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                if (frames is >= 20 and < 84) active += bytes;
                if (frames >= 84) idle += bytes;
                if (++frames == 148) window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(active == 0 && idle == 0, $"Flow warmed layout/record/render allocated {active}/{idle} managed bytes ({backend}).");
    }
}
