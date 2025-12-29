using Electron2D;

namespace FlappyBird;

internal abstract class Program
{
    [STAThread]
    private static void Main()
    {
        var settings = new Settings()
        {
            Fullscreen = FullscreenMode.Disabled,
            Resizable = false,
            VSync = VSyncMode.Adaptive,
        };

        using var game = new Game("FlappyBird", settings);
        
        game.Run();
    }
}