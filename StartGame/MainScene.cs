using Electron2D;

namespace StartGame;

public class MainScene() : Node("MainScene")
{
    private Player? _player;

    protected override void Ready()
    {
        AddChild(_player = new Player());
    }
}