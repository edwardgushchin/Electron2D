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
        _playerSprite = new Sprite(_playerTexture)
        {
            PixelsPerUnit = 236
        };
        _spriteRenderer = AddComponent<SpriteRenderer>();
        _spriteRenderer.SetSprite(_playerSprite);
    }

    double acc;
    int frames;
    
    protected override void Process(float delta)
    {
        if(Input.IsKeyDown(KeyCode.W)) Transform.TranslateY(_speed * delta);
        if(Input.IsKeyDown(KeyCode.A)) Transform.TranslateX(-_speed * delta);
        if(Input.IsKeyDown(KeyCode.S)) Transform.TranslateY(-_speed * delta);
        if(Input.IsKeyDown(KeyCode.D)) Transform.TranslateX(_speed * delta);
        
        if(Input.IsKeyDown(KeyCode.Q)) Transform.RotateLeft(_speed * delta);
        if(Input.IsKeyDown(KeyCode.E)) Transform.RotateRight(_speed * delta);
        
        acc += delta;
        frames++;

        if (acc >= 1.0)
        {
            var fps = frames / acc;
            Console.WriteLine($"FPS: {fps:F1}");
            acc = 0;
            frames = 0;
        }
    }

    protected override void ExitTree()
    {
        _playerTexture.Destroy();
    }
}