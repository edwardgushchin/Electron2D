using System.Numerics;

namespace Electron2D;

public sealed class SpriteRenderer : IComponent
{
    // Индекс в SceneTree render-индексе. -1 => не зарегистрирован.
    internal int SceneIndex = -1;

    private Node? _owner;
    private int _lastWorldVer = -1;

    private Sprite? _sprite;

    private bool _hasCached;
    private SpriteCommand _cached;
    
    private FlipMode _lastSpriteFlip;
    
    private Vector2 _boundsMinWorld;
    private Vector2 _boundsMaxWorld;
    private bool _hasBounds;


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
        SceneIndex = -1;
        _lastWorldVer = -1;
        _hasCached = false;
    }

    public void OnDetach()
    {
        _owner = null;
        _sprite = null;
        SceneIndex = -1;
        _lastWorldVer = -1;
        _hasCached = false;
        _cached = default;
        _hasBounds = false;
        _boundsMinWorld = default;
        _boundsMaxWorld = default;
    }

    internal void PrepareRender(RenderQueue q, ResourceSystem resources, in ViewCullRect view)
    {
        var owner = _owner;
        var sprite = _sprite;

        if (owner is null || sprite is null) return;
        
        var spriteFlip = sprite.FlipMode;
        if (spriteFlip != _lastSpriteFlip)
        {
            _lastSpriteFlip = spriteFlip;
            _hasCached = false;
        }

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
            if (flipFromScaleX) spriteFlip ^= FlipMode.Horizontal;
            if (flipFromScaleY) spriteFlip ^= FlipMode.Vertical;

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
                FlipMode = spriteFlip,
            };
            
            //_lastWorldVer = ver;
            ComputeWorldBounds(in _cached, out _boundsMinWorld, out _boundsMaxWorld);
            _hasCached = true;
        }
        
        if (!_hasBounds)
        {
            ComputeWorldBounds(in _cached, out _boundsMinWorld, out _boundsMaxWorld);
            _hasBounds = true;
        }

        if (!view.Intersects(in _boundsMinWorld, in _boundsMaxWorld))
            return;

        q.TryPush(in _cached);
    }
    
    private static void ComputeWorldBounds(in SpriteCommand cmd, out Vector2 min, out Vector2 max)
    {
        var pos = cmd.PositionWorld;
        var size = cmd.SizeWorld;
        var origin = cmd.OriginWorld;

        // Local rect relative to pivot (pivot at 0,0)
        var minRelX = -origin.X;
        var maxRelX = size.X - origin.X;
        var minRelY = -origin.Y;
        var maxRelY = size.Y - origin.Y;

        var rot = cmd.Rotation;

        // Fast path: no rotation
        if (rot == 0f)
        {
            min = new Vector2(pos.X + minRelX, pos.Y + minRelY);
            max = new Vector2(pos.X + maxRelX, pos.Y + maxRelY);
            return;
        }

        var c = MathF.Cos(rot);
        var s = MathF.Sin(rot);

        // Rotate corners, get AABB in local, then translate by pos
        // (x',y') = (x*c - y*s, x*s + y*c)
        var x1 = minRelX * c - minRelY * s; var y1 = minRelX * s + minRelY * c;
        var x2 = maxRelX * c - minRelY * s; var y2 = maxRelX * s + minRelY * c;
        var x3 = minRelX * c - maxRelY * s; var y3 = minRelX * s + maxRelY * c;
        var x4 = maxRelX * c - maxRelY * s; var y4 = maxRelX * s + maxRelY * c;

        var minX = MathF.Min(MathF.Min(x1, x2), MathF.Min(x3, x4));
        var maxX = MathF.Max(MathF.Max(x1, x2), MathF.Max(x3, x4));
        var minY = MathF.Min(MathF.Min(y1, y2), MathF.Min(y3, y4));
        var maxY = MathF.Max(MathF.Max(y1, y2), MathF.Max(y3, y4));

        min = new Vector2(pos.X + minX, pos.Y + minY);
        max = new Vector2(pos.X + maxX, pos.Y + maxY);
    }
}
