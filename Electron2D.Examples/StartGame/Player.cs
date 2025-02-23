using Electron2D;
using Electron2D.Input;

namespace StartGame;

public class Player(string name, float speed) : GameObject(name)
{
    private Sprite _sprite;
    private float _speed = speed;

    protected override void Awake()
    {
        _sprite = new Sprite(Texture.LoadFromFile(Path.Combine("assets", "player.png")));

        AddComponent(_sprite);
    }

    protected override void Start()
    {
        
    }
    
    protected override void Update(float deltaTime)
    {
        if (Keyboard.GetKeyDown(Keycode.A)) 
            Transform.Position += Vector3.Left * _speed * deltaTime;
        
        if (Keyboard.GetKeyDown(Keycode.D))
            Transform.Position += Vector3.Right * _speed * deltaTime;
        
        if (Keyboard.GetKeyDown(Keycode.W))
            Transform.Position += Vector3.Up * _speed * deltaTime;
        
        if (Keyboard.GetKeyDown(Keycode.S))
            Transform.Position += Vector3.Down * _speed * deltaTime;

        //_sprite.Position = Transform.Position;
        
        Logger.Info($"Player update: {Transform.Position.ToString()}");
    }
}