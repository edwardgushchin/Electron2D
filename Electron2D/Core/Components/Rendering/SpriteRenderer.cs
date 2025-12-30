using System.Numerics;
using SDL3;

namespace Electron2D;

public sealed class SpriteRenderer : IComponent
{
    private Node? _owner;
    private int _lastWorldVer = -1;

    private Sprite? _sprite;

    private bool _hasCached;
    private SpriteCommand _cached;

    public Color Color
    {
        get => _color;
        set { _color = value; _hasCached = false; }
    }
    
    private Color _color = new(0xFFFFFFFF);

    public uint SortKey
    {
        get => _sortKey;
        set { _sortKey = value; _hasCached = false; }
    }
    
    private uint _sortKey;

    public void SetSprite(Sprite sprite)
    {
        ArgumentNullException.ThrowIfNull(sprite);
        _sprite = sprite;
        _hasCached = false;
    }

    public void OnAttach(Node owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
        _lastWorldVer = -1;
        _hasCached = false;
    }

    public void OnDetach()
    {
        _owner = null;
        _sprite = null;
        _lastWorldVer = -1;
        _hasCached = false;
        _cached = default;
    }

    internal void PrepareRender(RenderQueue q, ResourceSystem resources)
    {
        var owner = _owner;
        var sprite = _sprite;

        if (owner is null || sprite is null) return;

        var ver = owner.Transform.WorldVersion;

        if (!_hasCached || ver != _lastWorldVer)
        {
            // 1) Texture
            var tex = sprite.Texture;

            if (!tex.IsValid)
            {
                var id = sprite.TextureId;
                if (id is null) return;

                tex = resources.GetTexture(id);
                if (!tex.IsValid) return;
            }

            // 2) Source rect (если не задан — вся текстура)
            var tr = sprite.TextureRect;
            if (tr.Width <= 0 || tr.Height <= 0)
                tr = new Rect(x: 0, y: 0, width: tex.Width, height: tex.Height);

            if (tr.Width <= 0 || tr.Height <= 0)
                return;

            var srcRect = new Rect(tr.X, tr.Y, tr.Width, tr.Height);

            // 3) PixelsPerUnit
            var ppu = sprite.PixelsPerUnit;
            if (!(ppu > 0f) || !float.IsFinite(ppu))
                ppu = 100f;

            // 4) Base size in world units (before scale)
            var sizeWorld = new Vector2(tr.Width / ppu, tr.Height / ppu);

            // 5) Apply Transform scale (Unity-like)
            var ws = owner.Transform.WorldScale;
            if (!float.IsFinite(ws.X) || !float.IsFinite(ws.Y))
                ws = Vector2.One;

            // Отрицательный scale превращаем в flip (W/H должны быть положительными)
            var flipFromScaleX = ws.X < 0f;
            var flipFromScaleY = ws.Y < 0f;

            ws = new Vector2(MathF.Abs(ws.X), MathF.Abs(ws.Y));
            sizeWorld = new Vector2(sizeWorld.X * ws.X, sizeWorld.Y * ws.Y);

            // Нулевой размер — ничего не рисуем
            if (!(sizeWorld.X > 0f) || !(sizeWorld.Y > 0f))
                return;

            // Pivot нормализованный (0..1) относительно УЖЕ отмасштабированного sizeWorld.
            var originWorld = new Vector2(sizeWorld.X * sprite.Pivot.X, sizeWorld.Y * sprite.Pivot.Y);

            // 6) Combine flips: Sprite.FlipMode + flip from negative scale
            var sdlFlip = (SDL.FlipMode)sprite.FlipMode;
            if (flipFromScaleX) sdlFlip ^= SDL.FlipMode.Horizontal;
            if (flipFromScaleY) sdlFlip ^= SDL.FlipMode.Vertical;

            _cached = new SpriteCommand
            {
                Texture = tex,
                SrcRect = srcRect,
                PositionWorld = owner.Transform.WorldPosition,
                SizeWorld = sizeWorld,
                Rotation = owner.Transform.WorldRotation, // rad
                OriginWorld = originWorld,
                Color = _color,
                SortKey = _sortKey,

                // ВАЖНО: сохраняем уже "итоговый" flip (с учётом Scale)
                FlipMode = (FlipMode)sdlFlip,
            };

            _lastWorldVer = ver;
            _hasCached = true;
        }

        q.TryPush(in _cached);
    }

}
