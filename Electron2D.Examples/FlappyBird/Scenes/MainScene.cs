using Electron2D;
using Electron2D.Inputs;
using Electron2D.Resources;
using FlappyBird.Components;

namespace FlappyBird.Scenes;

public class MainScene : Node
{
    private readonly Bird _bird;
    private readonly Background _background;
    
    private bool _gameOver;
    
    private const float FloorY = -1.32f; // уровень пола

    public MainScene(Texture bird, Texture background, Texture floor) : base("MainScene")
    {
        _bird = new Bird("bird", bird);
        _background = new Background("background", background, floor);
        
        AddChild(_background);
        AddChild(_bird);
    }

    protected override void Awake()
    {
        Restart();
    }
    
    private void Restart()
    {
        _bird.Transform.LocalPosition = new Vector2(0, 0);
        _bird.Velocity = new Vector2(0, 0);
        _background.ResetPosition();
        _gameOver = false;
        _bird.IsEnabled = true;
        _background.IsEnabled = true;
    }

    private void GameOver()
    {
        _gameOver = true;
        _bird.IsEnabled = false;
        _background.IsEnabled = false;
    }
    
    protected override void Update(float deltaTime)
    {
        if (Input.GetKeyDown(Scancode.Escape))
        {
            //Exit(); MainMenu!
        }

        if (_gameOver)
        {
            if(Input.GetKeyDown(Scancode.Space)) Restart();
            return;
        }
        
        if (_bird.Transform.LocalPosition.Y < FloorY) GameOver();
    }
}