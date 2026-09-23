using Mathf = Electron2D.Mathf;
using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasStrokes(string backend, string? fixture = null)
    {
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(200, 168) }; var frames = 0; var draws = 0; var changed = false;
        var node = new CanvasNode
        {
            Material = material,
            DrawAction = n =>
        {
            draws++;
            var color = changed ? Colors.Cyan : Colors.Red;
            n.DrawPolyline([new(8, 12), new(40, 12), new(40, 36)], color with { A = 0.5f }, 4);
            n.DrawMultilineColors([new(60, 12), new(80, 12), new(90, 12), new(110, 12)], [Colors.Red, Colors.Blue], 4);
            n.DrawSetTransform(new(128, 12.5f), scale: new(2, 3)); n.DrawPolyline([Vector2.Zero, new(20, 0)], Colors.Yellow, -1, true);
            n.DrawSetTransform(Vector2.Zero);
            n.DrawDashedLine(new(8, 52), new(61, 52), Colors.White, 3, 6, true);
            n.DrawDashedLine(new(8, 64), new(61, 64), Colors.White, 3, 6, false);
            n.DrawArc(new(90, 54), 14, 0, Mathf.Pi, 33, Colors.Green, 3);
            n.DrawEllipseArc(new(132, 54), 18, 8, 0, -Mathf.Tau * 2, 65, Colors.Blue, 2);
            n.DrawCircle(new(24, 100), 12, Colors.Red);
            n.DrawCircle(new(60, 100), 10, Colors.Green, false, 3);
            n.DrawCircle(new(93, 100), 4, Colors.Blue, false, 8);
            n.DrawEllipse(new(130, 100), 18, 7, Colors.Yellow);
            n.DrawEllipse(new(167, 100), 12, 6, Colors.Magenta, false, 2);
            n.DrawLine(new(8, 140), new(48, 140), Colors.White, 2, true);
            n.DrawSetTransform(new(60, 140), scale: new(1, 2)); n.DrawPolyline([Vector2.Zero, new(40, 0)], Colors.White, 2, true);
            n.DrawSetTransform(Vector2.Zero); n.DrawCircle(new(150, 142), 6, Colors.White, antialiased: true);
        }
        };
        window.AddChild(node);
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            var software = server.GetCurrentRenderingDriverName() == "software";
            server.FramePostDraw += () =>
            {
                using var image = server.Readback(); frames++;
                var dx = frames == 1 ? 0 : 2; var dy = frames == 1 ? 0 : 1; var factor = frames == 1 ? 1 : 0.5f;
                void At(int x, int y, Color color) => Pixel(image, x + dx, y + dy, new(color.R * factor, color.G * factor, color.B * factor, 1));
                var joined = (frames == 3 ? Colors.Cyan : Colors.Red) * 0.5f;
                At(20, 12, joined); At(40, 12, joined); At(40, 30, joined); At(44, 12, Colors.Black);
                At(65, 12, Colors.Red); At(95, 12, Colors.Blue); At(85, 12, Colors.Black);
                At(138, 12, Colors.Yellow); At(138, 13, Colors.Black);
                At(10, 52, Colors.White); At(16, 52, Colors.Black); At(59, 52, Colors.White); At(59, 64, Colors.Black);
                At(90, 67, Colors.Green); At(90, 40, Colors.Black); At(132, 61, Colors.Blue); At(132, 54, Colors.Black);
                At(24, 100, Colors.Red); At(40, 100, Colors.Black); At(60, 100, Colors.Black); At(60, 90, Colors.Green);
                At(93, 100, Colors.Blue); At(130, 100, Colors.Yellow); At(130, 110, Colors.Black);
                At(167, 100, Colors.Black); At(167, 94, Colors.Magenta);
                var feather = image.GetPixel(20 + dx, 138 + dy).R / factor;
                Check(feather is > 0 and < 0.8f, "Straight line has an actual alpha feather.");
                var scaledFeather = image.GetPixel(80 + dx, 137 + dy).R / factor;
                Check(scaledFeather is > 0 and < 0.9f, "Polyline feather scales with local coordinates.");
                At(150, 142, Colors.White); At(159, 142, Colors.Black);
                var circleFeather = image.GetPixel((software ? 155 : 156) + dx, 142 + dy).R / factor;
                Check(circleFeather is > 0 and < 0.9f, "Filled circle has a curved alpha feather.");
                // Software truncates fractional triangle positions before rasterization.
                if (software) At(156, 142, Colors.Black);
                At(20, 136, Colors.Black); At(80, 134, Colors.Black);
                if (frames == 1) { changed = true; node.Position = new(2, 1); node.SelfModulate = new(0.5f, 0.5f, 0.5f, 1); }
                if (frames == 2) node.QueueRedraw();
                if (frames == 3) { Check(draws == 2, "Transform/modulation reuse stroke commands; redraw replaces copied colors."); window.Tree!.Quit(); }
            };
        };
        Engine.Instance.Run(window); Released(window); Check(frames == 3, "Three stroke frames.");
        Console.WriteLine($"Canvas stroke native pixels passed: {backend}/{fixture ?? "default"}.");
    }
}
