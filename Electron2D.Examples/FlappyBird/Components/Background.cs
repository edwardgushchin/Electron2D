using Electron2D;
using Electron2D.Components;
using Electron2D.Resources;

namespace FlappyBird.Components;

public class Background : Node
{
    private readonly BackgroundParallax _backgroundParallax;
    
    public Background(string name, Texture sky, Texture floor) : base(name)
    {
        _backgroundParallax = new BackgroundParallax("BackgroundParallax");
        _backgroundParallax.AddLayer("sky", sky, speed: 0.2f, layerDepth: 0, copies: 10, overlapPixels: 10);
        _backgroundParallax.AddLayer("floor", floor, speed: 0.5f, layerDepth: 0, copies: 5, overlapPixels: 23,  -2.5f);
    }

    protected override void Awake()
    {
        AddChild(_backgroundParallax);
    }

    protected override void Update(float deltaTime)
    {
        _backgroundParallax.SetOffset(-Speed * deltaTime);
    }

    public void ResetPosition()
    {
        _backgroundParallax.ResetOffset();
    }

    public float Speed { get; set; } = 0.5f;
}