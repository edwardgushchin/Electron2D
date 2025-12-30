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
                Title = "Electron2D Game Engine: StartGame example",
                Mode = WindowMode.ExclusiveFullscreen,
                Width = 1920,
                Height = 1080,
            },
            VSync = VSyncMode.Adaptive,
            VSyncInterval = 1
        });
        
        var mainCam = new Camera("Main")
        {
            OrthoSize = 5f,   // видно 10 units по вертикали
            Current = true
        };

        engine.SceneTree.Root.AddChild(mainCam);
        engine.SceneTree.Root.AddChild(new MainScene());

        engine.Run();
    }
}