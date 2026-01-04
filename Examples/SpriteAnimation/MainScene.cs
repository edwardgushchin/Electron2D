using Electron2D;

namespace SpriteAnimation;

public class MainScene() : Node("MainScene")
{
    private Background _background = null!;
    private Camera _camera = null!;
    
    protected override void EnterTree()
    {
        _background = new Background("Background");
        _camera = new Camera("Main Camera")
        {
            OrthoSize = 0.9f
        };
        
        AddChild(_background);
        AddChild(_camera);
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