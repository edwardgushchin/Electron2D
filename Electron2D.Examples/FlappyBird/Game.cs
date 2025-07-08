using Electron2D;
using Electron2D.Inputs;
using Electron2D.Resources;
using FlappyBird.Scenes;

namespace FlappyBird;

public class Game : Electron2D.Game
{
    private readonly MainScene _mainScene;
    private readonly MainMenu _mainMenu;
    
    public Game(string title, Settings? settings) : base(title, settings)
    {
        var birdTexture = ResourceManager.LoadTexture("bird", Path.Combine(@"assets\sprites", "redbird-downflap.png"));
        var backgroundTexture = ResourceManager.LoadTexture("background", Path.Combine(@"assets\sprites", "background-day.png"));
        var floorTexture = ResourceManager.LoadTexture("floor", Path.Combine(@"assets\sprites", "base.png"));
        var logoTexture = ResourceManager.LoadTexture("logo", Path.Combine(@"assets\sprites", "message.png"));
        var readyTexture = ResourceManager.LoadTexture("ready", Path.Combine(@"assets\sprites", "message.png"));
        
        //_mainScene = new MainScene(messagesTexture, backgroundTexture, floorTexture);
        _mainMenu = new MainMenu(backgroundTexture, floorTexture, logoTexture, readyTexture);
    }

    protected override void Initialize()
    {
       RootNode.AddChild(_mainMenu);
    }
}