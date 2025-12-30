using Electron2D;

namespace StartGame;

public class Player() : Node("Player")
{
    private SpriteRenderer _spriteRenderer = null!;
    private Texture _playerTexture;
    private Sprite _playerSprite = null!;
    private const float _speed = 5f;

    protected override void EnterTree()
    {
        _playerTexture = Resources.GetTexture("player_idle.png");
        _playerSprite = new Sprite(_playerTexture);
        _spriteRenderer = AddComponent<SpriteRenderer>();
        _spriteRenderer.SetSprite(_playerSprite);
    }

    protected override void Ready()
    {
        
        
    }

    protected override void Process(float delta)
    {
        if(Input.IsKeyDown(Key.A)) Transform.Translate(x: -_speed * delta);
        if(Input.IsKeyDown(Key.D)) Transform.Translate(x: _speed * delta);
        if(Input.IsKeyDown(Key.W)) Transform.Translate(y: _speed * delta);
        if(Input.IsKeyDown(Key.S)) Transform.Translate(y: -_speed * delta);
    }

    protected override void ExitTree()
    {
        _playerTexture.Destroy();
    }
}