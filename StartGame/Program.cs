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
                Width = 1280,
                Height = 720,
                VSync = true,
                Mode = WindowMode.Exclusive,
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
        
        engine.SceneTree.AddChild(new MainScene());

        engine.Run();
    }
}