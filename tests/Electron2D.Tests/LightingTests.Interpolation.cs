using Electron2D;

internal static partial class LightingTests
{
    private static void NativeInterpolation()
    {
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var polygon = new OccluderPolygon { Closed = false, Polygon = [new(0, 0), new(0, 32)] };
        var window = new Window { Size = new(64, 32), Title = "Retained occluder interpolation" }; window.AddChild(new Surface { Area = new(0, 0, 64, 32) }); window.AddChild(new PointLight { Texture = texture, Position = new(32, 16), TextureScale = 4, ShadowEnabled = true });
        var previous = 8f; var current = 8f; var frames = 0; var sampled = 0; var transitioning = 0; RID occluder = default;
        window.Ready += _ =>
        {
            RenderingServer.CanvasSetModulate(window.FindWorld()!.Canvas, new(0, 0, 0)); window.Tree!.PhysicsInterpolation = true;
            occluder = RenderingServer.CanvasLightOccluderCreate(); RenderingServer.CanvasLightOccluderAttachToCanvas(occluder, window.FindWorld()!.Canvas); RenderingServer.CanvasLightOccluderSetPolygon(occluder, polygon.GetRID()); RenderingServer.CanvasLightOccluderSetTransform(occluder, new(0, new Vector2(current, 0))); RenderingServer.CanvasLightOccluderSetInterpolated(occluder, true);
            window.Tree.PhysicsFrameStarted += _ => { previous = current; current = current == 8 ? 24 : 8; RenderingServer.CanvasLightOccluderSetTransform(occluder, new(0, new Vector2(current, 0))); };
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames is >= 4 and <= 24)
                {
                    var fraction = (float)Engine.PhysicsInterpolationFraction;
                    var x = Mathf.Lerp(previous, current, fraction);
                    if (MathF.Abs(x - 16.5f) > .1f)
                    {
                        using var pixels = window.GetTexture().GetImage()!; var pixel = pixels.GetPixel(16, 16);
                        Check(pixel.R < .1f == (x > 16.5f), $"Interpolated occluder pixel: {previous}->{current}, alpha={fraction}, x={x}, pixel={pixel}"); sampled++;
                        if (fraction is > .05f and < .95f) transitioning++;
                    }
                }
                if (frames == 26) { Check(sampled > 8 && transitioning > 2, "Real physics ticks and intermediate presentation samples"); RenderingServer.FreeRID(occluder); window.Tree!.Quit(); }
            };
        };
        Check(Engine.Run(window) == 0 && frames == 26, "Retained interpolation workflow completed");
        Console.WriteLine($"Retained occluder interpolation pixels passed: {sampled} samples, {transitioning} intermediate fractions.");
    }
}
