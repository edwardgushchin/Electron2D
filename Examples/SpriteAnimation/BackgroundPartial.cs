using Electron2D;

namespace SpriteAnimation;

public class BackgroundPartial(Sprite partial, string name, int layer) : Node(name)
{
    protected override void EnterTree()
    {
        var renderer = AddComponent<SpriteRenderer>();
        renderer.Layer = layer;
        renderer.Order = 0;
        renderer.SetSprite(partial);
    }
}