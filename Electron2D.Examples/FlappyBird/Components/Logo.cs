using Electron2D;
using Electron2D.Graphics;
using Electron2D.Resources;

namespace FlappyBird;

public class Logo : Node
{
    private readonly Sprite _logo;
    
    public Logo(string name, Texture texture) : base(name)
    {
        _logo = new Sprite("logo", texture);
        _logo.SourceRect = new Rect
        {
            X = 0,
            Y = 0,
            Width = texture.Width,
            Height = 50
        };
        _logo.Transform.LocalPosition = new Vector2(0, 1.8f);
    }

    protected override void Awake()
    {
        AddChild(_logo);
    }
}