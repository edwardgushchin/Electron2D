using System.Runtime.InteropServices;
using Electron2D;
using Electron2D.Input;
using SDL3;

namespace StartGame;

public class MainScene : Scene
{
    private Player _player;
    private readonly float _playerSpeed = 20f;

    protected override void Awake()
    {
        _player = new Player("Player", _playerSpeed);
        AddGameObject(_player);
    }

    protected override void Start()
    {
        
    }

    protected override void OnKeyDown(uint id, Keycode key, Keymod mod, bool repeat)
    {
        if(key == Keycode.Escape) Kernel.Exit();
        
        if(key == Keycode.Backspace) 
            ClearColor = Color.Azure;
    }

    protected override void OnQuit()
    {
        Kernel.Exit();
    }
}