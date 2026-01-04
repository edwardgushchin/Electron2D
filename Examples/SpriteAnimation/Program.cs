using Electron2D;

namespace SpriteAnimation;

internal static class Program
{
    public static void Main()
    {
        using var engine = new Engine(new EngineConfig
        {
            Window = new WindowConfig
            {
                Title = "Electron2D: Sprite Animation Demo",
                Width = 320 * 3,
                Height = 180 * 3,
            },
            DefaultTextureFilter = FilterMode.Pixelart,
            DebugGridEnabled = true,
        });
        
        engine.SceneTree.Root.AddChild(new MainScene());
        
        engine.Run();
    }
}