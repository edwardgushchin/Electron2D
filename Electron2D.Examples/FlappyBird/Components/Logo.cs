using Electron2D;
using Electron2D.Graphics;
using Electron2D.Resources;

namespace FlappyBird.Components;

public class Logo : Node
{
    private readonly Sprite _logo;

    // ─── параметры анимации ───
    private const float MoveUp   = 2f;   // ↑ на 2 world‑units
    private const float Duration = 0.6f; // секунд

    // ─── состояние ───
    private bool   _hiding;
    private float  _elapsed;
    private Vector2 _startPos;
    private Vector2 _targetPos;
    
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

    /// <summary>
    /// Запускает анимацию: поднимаем логотип и плавно уменьшаем альфу до 0.
    /// </summary>
    public void Hide()
    {
        if (_hiding) return;
        _hiding    = true;
        _elapsed   = 0f;
        _startPos  = _logo.Transform.LocalPosition;
        _targetPos = _startPos + new Vector2(0, MoveUp);
    }

    protected override void Update(float dt)
    {
        if (!_hiding) return;

        _elapsed += dt;
        var t = Math.Clamp(_elapsed / Duration, 0f, 1f);  // линейный 0‒1
        
        var s = 0.5f - 0.5f * MathF.Cos(MathF.PI * t);

        // позиция
        _logo.Transform.LocalPosition = Vector2.Lerp(_startPos, _targetPos, s);

        // если нужен fade, раскомментируйте:
        
        var a = (byte)(255f + (0f - 255f) * s);
        _logo.Color = _logo.Color with { A = a };

        if (t >= 1f)
        {
            _logo.IsEnabled = false;
            _hiding = false; // одноразово; можно вызвать снова
        }
    }
}