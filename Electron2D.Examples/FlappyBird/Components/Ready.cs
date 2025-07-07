using Electron2D;
using Electron2D.Components;
using Electron2D.Graphics;
using Electron2D.Inputs;
using Electron2D.Physics;
using Electron2D.Resources;

namespace FlappyBird;

public class Ready : Node
{
    private readonly Sprite _ready;
    private readonly BoxCollider _boxCollider;
    private Vector2 _initialScale;
    private float _time;

    public float PulseSpeed { get; set; } = 4f;         // скорость пульсации
    public float ScaleFactor { get; set; } = 0.1f;      // амплитуда пульсации

    public Ready(string name, Texture texture) : base(name)
    {
        _ready = new Sprite("ready", texture);
        _ready.SourceRect = new Rect
        {
            X = 0,
            Y = 100,
            Width = texture.Width,
            Height = 53
        };

        // начальный масштаб
        _ready.Transform.LocalScale *= 1.2f;
        _initialScale = _ready.Transform.LocalScale;

        _boxCollider = new BoxCollider("readyCollider", _ready.WorldBounds.Size)
        {
            ShowDebugOutline = true
        };
    }

    protected override void Awake()
    {
        AddChild(_ready);
        AddChild(_boxCollider);
    }

    protected override void Update(float deltaTime)
    {
        _time += deltaTime;

        var scale = 1f + MathF.Sin(_time * PulseSpeed) * ScaleFactor;
        
        Transform.LocalScale = _initialScale * scale;

        var mousePos = Input.GetMousePosition();
        var worldPos = Camera.ActiveCamera!.ConvertScreenToWorld(mousePos);
        
        //Transform.LocalRotation += 1f * deltaTime;
        
        //Console.WriteLine($"4: {vertex[3].X}, {vertex[3].Y}");

        if (Input.GetMouseButtonDown(MouseButtonFlags.Left))
        {
            //var bounds = _ready.WorldBounds;
            if (_boxCollider.Contains(worldPos))
            {
                // Логика нажатия
                Console.WriteLine("You are ready!");
            }
        }
    }
}