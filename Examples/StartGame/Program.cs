using System.Text;
using Electron2D;

namespace StartGame;

internal static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        using var engine = new Engine(new EngineConfig
        {
            Window = new WindowConfig
            {
                Title = "Electron2D: StartGame example",
            },
            DebugGridEnabled = true,
        });
        
        engine.SceneTree.Root.AddChild(new MainScene());

        engine.Run();
    }
}