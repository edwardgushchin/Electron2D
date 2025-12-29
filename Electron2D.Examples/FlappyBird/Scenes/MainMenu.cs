using Electron2D;
using Electron2D.Inputs;
using Electron2D.Resources;
using FlappyBird.Components;

namespace FlappyBird.Scenes;

public class MainMenu : Node
{
    private readonly Background _background;
    private readonly Logo _logo;
    private readonly Ready _ready;
    
    public MainMenu(Texture background, Texture floor, Texture messages) : base("MainMenu")
    {
        _background = new Background("background", background, floor);
        _logo = new Logo("logo", messages);
        _ready = new Ready("ready", messages);
        _ready.OnClick += ReadyOnOnClick;
    }

    private void ReadyOnOnClick()
    {
        _logo.Hide();
    }

    protected override void Awake()
    {
        AddChild(_background);
        AddChild(_logo);
        AddChild(_ready);
    }

    protected override void Update(float deltaTime)
    {
        if (Input.GetKeyDown(Scancode.KpPlus)) _background.Speed += 0.01f;
        if (Input.GetKeyDown(Scancode.KpMinus)) _background.Speed -= 0.01f;;
    }
}