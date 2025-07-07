using Electron2D;

namespace StartGame;

internal static class Kernel
{
    private static Game? _game;

    [STAThread]
    private static void Main()
    {
        _game = new MyGame();
        _game.Run();
    }
}