using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyPolygonNode(string backend, string? fixture = null)
    {
        using var image = Image.CreateFromData(2, 1, false, Image.Format.Rgba8,
            [255, 0, 0, 255, 0, 255, 0, 255]);
        using var texture = ImageTexture.CreateFromImage(image);
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(64, 40), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var filled = new Polygon
        {
            Name = "Filled",
            Vertices = [new(0, 0), new(20, 0), new(20, 20), new(0, 20)],
            Position = new(4, 4),
            Color = Colors.Red,
            Material = material,
            Polygons = [[0, 1, 2], [0, 2, 3]],
        };
        var textured = new Polygon
        {
            Name = "Textured",
            Vertices = [new(0, 0), new(20, 0), new(20, 20), new(0, 20)],
            Position = new(32, 4),
            Texture = texture,
            UV = [new(0, 0), new(2, 0), new(2, 1), new(0, 1)],
        };
        window.AddChild(filled); window.AddChild(textured);
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var frame = server.Readback(); frames++;
                Pixel(frame, 8, 8, frames == 1 ? Colors.Red : Colors.Blue);
                Pixel(frame, 22, 22, frames == 1 ? Colors.Red : Colors.Blue);
                Pixel(frame, 28, 8, Colors.Black);
                Pixel(frame, 35, 8, Colors.Red); Pixel(frame, 49, 8, Colors.Green);
                if (frames == 1) filled.Color = Colors.Blue;
                else window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(frames == 2, "Polygon node rendered and redrew across two frames.");
        Console.WriteLine($"Polygon node native pixels passed: {backend}/{fixture ?? "default"}.");
    }
}
