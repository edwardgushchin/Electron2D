using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifySceneHierarchy(string backend)
    {
        using var pixels = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        pixels.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(pixels);
        var window = new Window { Size = new(96, 96) };
        var parent = new Node { Position = new(24, 0), Modulate = Colors.Red };
        var bridge = new SceneNode();
        var direct = new Sprite { Name = "direct", Texture = texture, Centered = false, Position = new(8, 8), Scale = new(8, 8) };
        var separate = new Sprite { Texture = texture, Centered = false, Position = new(8, 8), Scale = new(8, 8) };
        var canvas = new DirectCanvas(texture);
        var observer = new CanvasNode { Name = "observer" };
        window.AddChild(parent); parent.AddChild(direct); parent.AddChild(bridge); bridge.AddChild(separate);
        window.AddChild(canvas); window.AddChild(observer);
        var frames = 0;
        observer.ReadyAction = n =>
        {
            var server = RenderingServer.Instance!;
            server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var image = server.Readback();
                Pixel(image, 10, 10, Colors.White);
                Pixel(image, 10, 42, Colors.Cyan);
                if (++frames == 1)
                {
                    Pixel(image, 34, 10, Colors.Red);
                    parent.Hide();
                }
                else
                {
                    Pixel(image, 34, 10, Colors.Black);
                    n.Tree!.Quit();
                }
            };
        };
        Engine.Instance.Run(window);
        Check(frames == 2 && bridge.IsDisposed && separate.IsDisposed && !texture.IsDisposed, "Mixed hierarchy disposal and borrowed texture.");
        Released(window);
        Console.WriteLine($"Scene hierarchy pixel checks passed: {backend}.");
    }

    private sealed class DirectCanvas(Texture texture) : CanvasItem
    {
        public override Transform GetTransform() => new(0f, new Vector2(8, 40));
        protected override void OnDraw() => texture.DrawRect(this, new Rect(0, 0, 8, 8), false, Colors.Cyan);
    }
}
