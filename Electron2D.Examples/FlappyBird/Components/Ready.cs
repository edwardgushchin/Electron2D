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
    private readonly Vector2 _initialScale;
    private float _time;
    
    public event Action OnClick;

    // ─── пульсация ───
    public  float PulseSpeed  { get; set; } = 4f;   // рад/с
    public  float ScaleFactor { get; set; } = 0.1f; // амплитуда
    private float   _pulseTime;

    // ─── fade-out ───
    private const float FadeDuration = 0.3f; // секунд
    private bool  _fading;
    private float _fadeElapsed;

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
        _initialScale = _ready.Transform.LocalScale;

        _boxCollider = new BoxCollider("readyCollider", _ready.WorldBounds.Size)
        {
            //ShowDebugOutline = true
        };
    }

    protected override void Awake()
    {
        AddChild(_ready);
        _ready.AddChild(_boxCollider);
    }

    protected override void Update(float dt)
    {
        // ─── пульсация (пока не начали fade) ───
        if (!_fading)
        {
            _pulseTime += dt;
            float scale = 1f + MathF.Sin(_pulseTime * PulseSpeed) * ScaleFactor;
            Transform.LocalScale = _initialScale * scale;
        }

        // ─── fade-out ───
        if (_fading)
        {
            _fadeElapsed += dt;
            float t = Math.Clamp(_fadeElapsed / FadeDuration, 0f, 1f);

            // альфа 255 → 0 (линейно)
            byte a = (byte)(255f * (1f - t));
            _ready.Color = _ready.Color with { A = a };

            if (t >= 1f)
            {
                _ready.IsEnabled = false;  // выключаем спрайт
                _fading          = false; // анимация завершена
            }
        }

        // ─── клик мыши ───
        if (!_fading && Input.GetMouseButtonDown(MouseButtonFlags.Left))
        {
            var mousePos = Input.GetMousePosition();
            var worldPos = Camera.ActiveCamera!.ConvertScreenToWorld(mousePos);

            if (_boxCollider.Contains(worldPos))
                OnClick?.Invoke();
        }
    }

    public void Hide()
    {
        if (_fading || !_ready.IsEnabled) return;
        _fading      = true;
        _fadeElapsed = 0f;
    }
}