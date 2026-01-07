using Electron2D;

namespace PhysicsUnits;

internal static class Program
{
    public static void Main()
    {
        using var engine = new Engine(new EngineConfig
        {
            Window = new WindowConfig
            {
                Title = "Electron2D: Physics units demo",
                Width = 800,
                Height = 600
            },
            DebugGridEnabled = true,
            VSync = VSyncMode.Enabled
        });

        engine.SceneTree.Root.AddChild(new PhysicsUnitsScene());
        engine.Run();
    }
}