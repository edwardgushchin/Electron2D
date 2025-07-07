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
            Resizable = true,
            VSync = VSyncMode.Adaptive,
            Width = 800,
            Height = 600,
        };
        
        var game = new Game("FlappyBird", settings);
        game.Run();
    }
}