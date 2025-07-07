using Electron2D;
using Electron2D.Resources;
using FlappyBird.Components;

namespace FlappyBird.Scenes;

public class MainMenu : Node
{
    private readonly Background _background;
    private readonly Logo _logo;
    private readonly Ready _ready;
    
    public MainMenu(Texture background, Texture messages) : base("MainMenu")
    {
        _background = new Background("background", background);
        _logo = new Logo("logo", messages);
        _ready = new Ready("ready", messages);
    }

    protected override void Awake()
    {
        AddChild(_background);
        AddChild(_logo);
        AddChild(_ready);
    }
}