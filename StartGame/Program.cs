using System.Numerics;
using Electron2D;

namespace StartGame;

internal class Program
{
    public static void Main()
    {
        var config = new EngineConfig
        {
            Window = new WindowConfig
            {
                Title = "My 2D Game",
                Width = 1280,
                Height = 720,
                VSync = true
            },
            Physics = new PhysicsConfig
            {
                Gravity = new Vector2(0, -9.81f),
                FixedDelta = 1f / 60f
            }
        };

        using var engine = new Engine(config);

        var scene = new Scene();
        scene.Root.AddChild(new Game());

        engine.Run(scene);
    }
}