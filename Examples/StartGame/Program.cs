using System.Numerics;
using Electron2D;

namespace StartGame;

internal class Program
{
    public static void Main()
    {
        using var engine = new Engine(new EngineConfig
        {
            Window = new WindowConfig
            {
                Title = "Electron2D: StartGame example",
            },
            DebugGridEnabled = true,
            DebugGridColor = new Color(47, 47, 56),
            DebugGridAxisColor = new Color(71, 71, 84)
        });
        
        engine.SceneTree.Root.AddChild(new MainScene());

        engine.Run();
    }
}