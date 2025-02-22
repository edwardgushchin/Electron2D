using Electron2D;

namespace StartGame;

internal static class Kernel
{
    private static Game? _game;
    
    [STAThread]
    private static void Main()
    {
        var settings = new Settings
        {
            Width = 1024,
            Height = 768,
            Fullscreen = FullscreenMode.Disabled,
            Resizable = true,
            VSync = VSyncMode.Adaptive,
            LogLevel = LogLevel.Info
        };
        
        _game = new Game("Electron2D Game", ref settings);
        
        var mainScene = new MainScene();

        _game.AddScene(mainScene, "MainScene");
        
        _game.LoadScene("MainScene");
        
        _game.Play();
    }

    public static void Exit()
    {
        _game!.Stop();
    }
}