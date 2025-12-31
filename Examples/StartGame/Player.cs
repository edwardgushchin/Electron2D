using Electron2D;

namespace StartGame;

public class Player() : Node("Player")
{
    private SpriteRenderer _spriteRenderer = null!;
    private Texture _playerTexture;
    private Sprite _playerSprite = null!;
    private const float _speed = 5f;
    private bool flipX;
    private double acc;
    private int frames;

    protected override void EnterTree()
    {
        _playerTexture = Resources.GetTexture("player_idle.png");
        _playerSprite = new Sprite(_playerTexture)
        {
            PixelsPerUnit = 236
        };
        _spriteRenderer = AddComponent<SpriteRenderer>();
        _spriteRenderer.SetSprite(_playerSprite);
    }

    
    
    protected override void Process(float delta)
    {
        if(Input.IsKeyDown(KeyCode.W)) Transform.TranslateY(_speed * delta);
        if (Input.IsKeyDown(KeyCode.A))
        {
            flipX = true;
            Transform.TranslateX(-_speed * delta);
        }
        if(Input.IsKeyDown(KeyCode.S)) Transform.TranslateY(-_speed * delta);
        if (Input.IsKeyDown(KeyCode.D))
        {
            flipX = false;
            Transform.TranslateX(_speed * delta);
        }
        
        if(Input.IsKeyDown(KeyCode.Q)) Transform.RotateLeft(_speed * delta);
        if(Input.IsKeyDown(KeyCode.E)) Transform.RotateRight(_speed * delta);
        
        _playerSprite.FlipMode = flipX ? FlipMode.Horizontal : FlipMode.None;
    }

    protected override void ExitTree()
    {
        //Resources.UnloadTexture(_playerTexture);
    }
}