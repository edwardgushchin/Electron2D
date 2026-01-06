using Electron2D;

namespace SpriteAnimation;

internal static class Program
{
    private const int VirtualW = 320;
    private const int VirtualH = 180;
    private const int WindowScale = 3;

    public static void Main()
    {
        using var engine = new Engine(new EngineConfig
        {
            Window = new WindowConfig
            {
                Title = "Electron2D: Sprite Animation Demo",
                Mode = WindowMode.BorderlessFullscreen, // или ExclusiveFullscreen
                Resizable = false,
                Width = VirtualW * WindowScale,
                Height = VirtualH * WindowScale
            },
            Presentation = new RenderPresentationConfig
            {
                VirtualWidth = VirtualW,
                VirtualHeight = VirtualH,
                Mode = PresentationMode.IntegerScale
            },

            // Рекомендую включить VSync для стабильного ощущения и чтобы не гонять GPU/CPU зря.
            VSync = VSyncMode.Enabled,
            VSyncInterval = 1,

            TextureFilter = FilterMode.Pixelart,
            DebugGridEnabled = true,
        });

        engine.SceneTree.Root.AddChild(new MainScene());
        engine.Run();
    }
}
