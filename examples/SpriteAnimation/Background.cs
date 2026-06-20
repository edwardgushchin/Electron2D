using Electron2D;

namespace SpriteAnimation;

public class Background(string name) : Node(name)
{
    private BackgroundPartial _bottomBackground = null!;
    private BackgroundPartial _midlleBackground = null!;
    private BackgroundPartial _topBackground = null!;
    
    protected override void EnterTree()
    {
        var _layer1 = Resources.GetTexture(Path.Combine("background", "background_layer_1.png"));
        var _layer2 = Resources.GetTexture(Path.Combine("background", "background_layer_2.png"));
        var _layer3 = Resources.GetTexture(Path.Combine("background", "background_layer_3.png"));

        _bottomBackground = new BackgroundPartial(new Sprite(_layer1), "layer1", layer: 0);
        _midlleBackground = new BackgroundPartial(new Sprite(_layer2), "layer2", layer: 1);
        _topBackground    = new BackgroundPartial(new Sprite(_layer3), "layer3", layer: 2);

        
        AddChild(_bottomBackground);
        AddChild(_midlleBackground);
        AddChild(_topBackground);
    }
    
    protected override void Ready()
    {
        base.Ready();
    }
    
    protected override void Process(float delta)
    {
        base.Process(delta);
    }
    
    protected override void ExitTree()
    {
        base.ExitTree();
    }

    protected override void Destroy()
    {
        base.Destroy();
    }
}