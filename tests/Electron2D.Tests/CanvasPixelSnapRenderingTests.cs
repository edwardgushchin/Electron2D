using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasPixelSnap(string backend, string? fixture = null)
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image);
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(96, 80), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var parent = new Entity { Name = "Snap0", Position = new(8.6f, 8.6f) }; window.AddChild(parent);
        var draws = 0;
        parent.AddChild(new CanvasNode { Name = "Snap1", Position = new(0.6f, 0.6f), DrawAction = n => { draws++; n.DrawRect(new(0, 0, 3, 3), Colors.Red); } });
        var neutral = new Node(); parent.AddChild(neutral);
        neutral.AddChild(new CanvasNode { Name = "Snap2", Position = new(25.6f, 8.6f), DrawAction = n => n.DrawRect(new(0, 0, 3, 3), Colors.Green) });
        parent.AddChild(new CanvasNode { Name = "Snap3", TopLevel = true, Position = new(25.6f, 15.6f), DrawAction = n => n.DrawRect(new(0, 0, 3, 3), Colors.Blue) });
        var sort = new Entity { Name = "Snap4", YSortEnabled = true, Position = new(35, 25) }; window.AddChild(sort);
        sort.AddChild(new CanvasNode { Name = "Snap5", Position = new(0, 0.4f), DrawAction = n => n.DrawRect(new(0, 0, 10, 10), Colors.Red) });
        sort.AddChild(new CanvasNode { Name = "Snap6", Position = new(0, 0.1f), DrawAction = n => n.DrawRect(new(0, 0, 10, 10), Colors.Blue) });
        var scaled = new Entity { Name = "Snap7", Position = new(4.2f, 45.2f), Scale = new(1.6f, 1.6f) }; window.AddChild(scaled);
        var middle = new Entity { Name = "Snap8", Position = new(0.6f, 0.6f) }; scaled.AddChild(middle);
        middle.AddChild(new CanvasNode { Name = "Snap9", Position = new(0.6f, 0.6f), DrawAction = n => n.DrawRect(new(0, 0, 2, 2), Colors.Yellow) });
        var sortedScale = new Entity { Name = "Snap10", YSortEnabled = true, Position = new(4.2f, 55.2f), Scale = new(1.6f, 1.6f) }; window.AddChild(sortedScale);
        var sortedMiddle = new Entity { Name = "Snap11", YSortEnabled = true, Position = new(0.6f, 0.6f) }; sortedScale.AddChild(sortedMiddle);
        sortedMiddle.AddChild(new CanvasNode { Name = "Snap12", Position = new(0.6f, 0.6f), DrawAction = n => n.DrawRect(new(0, 0, 2, 2), Colors.Cyan) });
        window.AddChild(new CanvasNode
        {
            Name = "Snap13",
            Material = material,
            DrawAction = n =>
            {
                n.DrawSetTransformMatrix(new Transform(new(1.8f, 0.6f), new(-0.4f, 1.6f), new(70.2f, 5.2f)));
                n.DrawTextureRectRegion(texture, new(0, 0, 2, 2), new(0, 0, 2, 2));
            }
        });
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            var software = RenderingServer.GetCurrentRenderingDriverName() == "software";
            RenderingServer.FramePostDraw += () =>
            {
                using var frame = server.Readback();
                try
                {
                    Pixel(frame, 26, 9, Colors.Green); Pixel(frame, 26, 16, Colors.Blue);
                    Pixel(frame, 71, 7, Colors.White);
                    switch (++frames)
                    {
                        case 1:
                            Pixel(frame, 9, 9, Colors.Red); Pixel(frame, 40, 30, Colors.Red);
                            window.SnapTransformsToPixel = true;
                            break;
                        case 2:
                            Pixel(frame, 9, 9, Colors.Black); Pixel(frame, 10, 10, Colors.Red); Pixel(frame, 40, 30, Colors.Blue);
                            Pixel(frame, 7, 49, software ? Colors.Yellow : Colors.Black); Pixel(frame, 8, 49, Colors.Yellow);
                            Pixel(frame, 7, 58, Colors.Cyan);
                            window.SnapVerticesToPixel = true;
                            break;
                        case 3:
                            Pixel(frame, 7, 49, Colors.Black); Pixel(frame, 8, 49, Colors.Yellow);
                            Pixel(frame, 10, 10, Colors.Red); Pixel(frame, 40, 30, Colors.Blue); Pixel(frame, 71, 7, Colors.White);
                            window.SnapTransformsToPixel = false;
                            break;
                        case 4:
                            Pixel(frame, 9, 9, Colors.Red); Pixel(frame, 40, 30, Colors.Red);
                            Check(draws == 1 && parent.Position == new Vector2(8.6f, 8.6f), "Snapping replay preserves commands and logical transforms.");
                            window.Tree!.Quit(); break;
                    }
                }
                catch (Exception error) { throw new InvalidOperationException($"Pixel snapping {backend}/{fixture ?? "default"}, frame {frames}: {error.Message}", error); }
            };
        };
        Engine.Run(window); Released(window);
        Check(frames == 4, "Both snapping modes execute independently and together.");
        Console.WriteLine($"Canvas pixel snapping native checks passed: {backend}/{fixture ?? "default"}.");
    }
}
