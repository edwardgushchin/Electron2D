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
                Title = "My 2D Game",
                Width = 800,
                Height = 600,
                VSync = VSyncMode.Enabled,
                Mode = WindowMode.Windowed,
                State = WindowState.Normal
            },
            Physics = new PhysicsConfig
            {
                Gravity = new Vector2(0, -9.81f),
                FixedDelta = 1f / 60f
            },
            TimeScale = 1f,
            PixelPerUnit = 128f
        });
        
        var mainCam = new Camera("Main")
        {
            OrthoSize = 5f,   // видно 10 units по вертикали
            Current = true
        };

        engine.SceneTree.Root.AddChild(mainCam);
        engine.SceneTree.Root.AddChild(new MainScene());
        
        engine.SceneTree.OnWindowCloseRequested.Connect(_ =>
        {
            // показать “Are you sure?” через свой UI, не выходить сразу
            Console.WriteLine("Are you sure?");
        });

        engine.SceneTree.OnQuitRequested.Connect(() =>
        {
            Console.WriteLine("Are you sure? 2");
        });

        engine.Run();
    }
}