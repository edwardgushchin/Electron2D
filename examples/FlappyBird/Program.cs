using Electron2D;

namespace FlappyBird;

internal static class Program
{
    public static void Main()
    {
        using var engine = new Engine(new EngineConfig
        {
            Window = new WindowConfig
            {
                Title = "Electron2D: FlappyBird",
                Width = 288,
                Height = 512,
            },
            
            VSync = VSyncMode.Enabled,
        });
        
        engine.Run();
    }
}
