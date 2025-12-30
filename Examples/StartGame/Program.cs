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
            }
        });

        engine.SceneTree.Root.AddChild(new Camera("Main"));
        engine.SceneTree.Root.AddChild(new MainScene());

        engine.Run();
    }
}