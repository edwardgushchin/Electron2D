using Electron2D;
using Electron2D.Components;
using Electron2D.Resources;

namespace FlappyBird.Components;

public class Background : Node
{
    private readonly BackgroundParallax _backgroundParallax;
    
    public Background(string name, Texture texture) : base(name)
    {
        _backgroundParallax = new BackgroundParallax("BackgroundParallax");
        _backgroundParallax.AddLayer("sky", texture, speed: 0.5f, layerDepth: 0, copies: 10, overlapPixels: 10);
    }

    protected override void Awake()
    {
        AddChild(_backgroundParallax);
    }

    protected override void Update(float deltaTime)
    {
        _backgroundParallax.SetOffset(Speed * deltaTime);
    }

    public void ResetPosition()
    {
        _backgroundParallax.ResetOffset();
    }

    private float Speed { get; set; } = -0.5f;
}