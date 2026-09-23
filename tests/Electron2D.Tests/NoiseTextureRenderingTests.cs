using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyNoiseTexture(string backend)
    {
        using var noise = new LiveNoise();
        using var ramp = new Gradient { Colors = [Colors.Red, Colors.Blue] };
        using var texture = new NoiseTexture
        {
            Width = 2,
            Height = 1,
            GenerateMipmaps = false,
            Noise = noise,
            ColorRamp = ramp,
        };
        var window = new Window { Size = new(64, 32), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        window.AddChild(new Sprite { Texture = texture, Centered = false, Position = new(4, 4), Scale = new(16, 16) });
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!;
            server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var frame = server.Readback();
                frames++;
                Pixel(frame, 8, 8, Colors.Red);
                Pixel(frame, 24, 8, frames == 1 ? Colors.Blue : Colors.Red);
                if (frames == 1) noise.Constant = true;
                else window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window);
        Released(window);
        Check(frames == 2, "Noise source changes update a retained Sprite texture.");
        Console.WriteLine($"Noise texture native pixels passed: {backend}.");
    }

    private sealed class LiveNoise : Noise
    {
        private bool _constant;
        internal bool Constant { set { _constant = value; EmitChanged(); } }
        public override float GetNoise1D(float x) => _constant ? 0 : x * 2 - 1;
        public override float GetNoise2D(float x, float y) => GetNoise1D(x);
    }
}
