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
                Title = "Electron2D: StartGame example",
                Mode = WindowMode.BorderlessFullscreen
            },
            DebugGridEnabled = true,
            VSync = VSyncMode.Enabled
        });
        
        engine.SceneTree.Root.AddChild(new MainScene());

        engine.Run();
    }
}