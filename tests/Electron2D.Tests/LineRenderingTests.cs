using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyLine(string backend, string? fixture = null)
    {
        using var image = Image.CreateFromData(2, 1, false, Image.Format.Rgba8,
            [255, 0, 0, 255, 0, 0, 255, 255]);
        using var texture = ImageTexture.CreateFromImage(image);
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(80, 80), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var capped = new Line
        {
            Name = "Capped",
            Points = [new(12, 12), new(52, 12)],
            Width = 8,
            DefaultColor = Colors.Red,
            BeginCapMode = Line.LineCapMode.Round,
            EndCapMode = Line.LineCapMode.Round,
            Material = material,
        };
        var bent = new Line
        {
            Name = "Bent",
            Points = [new(12, 32), new(52, 32), new(52, 54)],
            Width = 8,
            DefaultColor = Colors.Green,
            JointMode = Line.LineJointMode.Bevel,
        };
        var textured = new Line
        {
            Name = "Textured",
            Points = [new(12, 68), new(52, 68)],
            Width = 8,
            Texture = texture,
            TextureMode = Line.LineTextureMode.Stretch,
        };
        window.AddChild(capped); window.AddChild(bent); window.AddChild(textured);
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var frame = server.Readback(); frames++;
                Pixel(frame, 9, 12, frames == 1 ? Colors.Red : Colors.Blue);
                Pixel(frame, 54, 12, frames == 1 ? Colors.Red : Colors.Blue);
                Pixel(frame, 7, 12, Colors.Black);
                Pixel(frame, 24, 32, Colors.Green); Pixel(frame, 52, 42, Colors.Green);
                Pixel(frame, 20, 68, Colors.Red); Pixel(frame, 44, 68, Colors.Blue);
                if (frames == 1) capped.DefaultColor = Colors.Blue;
                else window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(frames == 2, "Line drew and retained a changed color across two frames.");
        Console.WriteLine($"Line cap, joint, texture and redraw native pixels passed: {backend}/{fixture ?? "default"}.");
    }
}
