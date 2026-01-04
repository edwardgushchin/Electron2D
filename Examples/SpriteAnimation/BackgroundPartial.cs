using Electron2D;

namespace SpriteAnimation;

public class BackgroundPartial(Sprite partial, string name) : Node(name)
{
    protected override void EnterTree()
    {
        var renderer = AddComponent<SpriteRenderer>();
        renderer.SetSprite(partial);
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
        //RemoveComponent<SpriteRenderer>
    }

    protected override void Destroy()
    {
        base.Destroy();
    }
}