using Electron2D;

namespace StartGame;

internal static class Program
{
    public static void Main()
    {
        using var engine = new Engine(new EngineConfig
        {
            Window = new WindowConfig
            {
                Title = "Electron2D: Sprite order demo",
                Width = 800,
                Height = 600,
            },

            DebugGridEnabled = true,
        });
        
        engine.SceneTree.Root.AddChild(new SpriteOrderScene());
        
        engine.Run();
    }
}