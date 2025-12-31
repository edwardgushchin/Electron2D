using System.Numerics;

namespace Electron2D;

public sealed class SpriteRenderer : IComponent
{
    // Индекс в SceneTree render-индексе. -1 => не зарегистрирован.
    internal int SceneIndex = -1;

    private Node? _owner;
    private Sprite? _sprite;

    private Color _color = new(0xFFFFFFFF);
    private uint _sortKey;

    // Кэш команды рендера (пересобирается только при необходимости)
    private bool _hasCache;
    private int _lastWorldVersion = -1;
    private SpriteSnapshot _lastSpriteSnapshot;

    private SpriteCommand _cached;
    private Vector2 _boundsMinWorld;
    private Vector2 _boundsMaxWorld;

    public Color Color
    {
        get => _color;
        set
        {
            if (_color.Equals(value)) return;
            _color = value;
            InvalidateCache();
        }
    }

    public uint SortKey
    {
        get => _sortKey;
        set
        {
            if (_sortKey == value) return;
            _sortKey = value;
            InvalidateCache();
        }
    }

    public void SetSprite(Sprite sprite)
    {
        ArgumentNullException.ThrowIfNull(sprite);

        if (ReferenceEquals(_sprite, sprite))
            return;

        _sprite = sprite;
        InvalidateCache();
    }

    public void OnAttach(Node owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        _owner = owner;
        SceneIndex = -1;

        _lastWorldVersion = -1;
        InvalidateCache();
    }

    public void OnDetach()
    {
        _owner = null;
        _sprite = null;

        SceneIndex = -1;
        _lastWorldVersion = -1;

        _hasCache = false;
        _lastSpriteSnapshot = default;
        _cached = default;
        _boundsMinWorld = default;
        _boundsMaxWorld = default;
    }

    internal void PrepareRender(RenderQueue q, ResourceSystem resources, in ViewCullRect view)
    {
        var owner = _owner;
        var sprite = _sprite;

        if (owner is null || sprite is null)
            return;

        var worldVer = owner.Transform.WorldVersion;

        if (!TryEnsureCached(owner, sprite, worldVer, resources))
            return;

        if (!view.Intersects(in _boundsMinWorld, in _boundsMaxWorld))
            return;

        q.TryPush(in _cached);
    }

    private bool TryEnsureCached(Node owner, Sprite sprite, int worldVer, ResourceSystem resources)
    {
        var snap = SpriteSnapshot.From(sprite);

        // Пересборка нужна если:
        // - нет кэша
        // - изменился трансформ (WorldVersion)
        // - изменились ключевые поля спрайта (flip/rect/ppu/pivot/texture source)
        if (_hasCache && worldVer == _lastWorldVersion && snap.Equals(_lastSpriteSnapshot))
            return true;

        // 1) Texture (с late-load поддержкой через ResourceSystem)
        var tex = sprite.Texture;
        if (!tex.IsValid)
        {
            var id = sprite.TextureId;
            if (id is null) return false;

            tex = resources.GetTexture(id);
            if (!tex.IsValid) return false;
        }

        // 2) Source rect (если не задан — вся текстура)
        if (!TryResolveSourceRect(sprite.TextureRect, tex.Width, tex.Height, out var srcRect))
            return false;

        // 3) PixelsPerUnit
        var ppu = sprite.PixelsPerUnit;
        if (!(ppu > 0f) || !float.IsFinite(ppu))
            ppu = 100f;

        // 4) Базовый размер в world-units (до масштаба)
        var sizeWorld = new Vector2(srcRect.Width / ppu, srcRect.Height / ppu);

        // 5) Применяем WorldScale (Unity-like) + отрицательный scale -> flip
        var ws = owner.Transform.WorldScale;
        if (!float.IsFinite(ws.X) || !float.IsFinite(ws.Y))
            ws = Vector2.One;

        var flip = sprite.FlipMode;

        if (ws.X < 0f) flip ^= FlipMode.Horizontal;
        if (ws.Y < 0f) flip ^= FlipMode.Vertical;

        ws = new Vector2(MathF.Abs(ws.X), MathF.Abs(ws.Y));
        sizeWorld = new Vector2(sizeWorld.X * ws.X, sizeWorld.Y * ws.Y);

        if (!(sizeWorld.X > 0f) || !(sizeWorld.Y > 0f))
            return false;

        // 6) Pivot нормализованный (0..1) относительно уже отмасштабированного sizeWorld
        var pivot = sprite.Pivot;
        if (!float.IsFinite(pivot.X) || !float.IsFinite(pivot.Y))
            pivot = Vector2.Zero;

        var originWorld = new Vector2(sizeWorld.X * pivot.X, sizeWorld.Y * pivot.Y);

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
            FlipMode = flip,
        };

        _lastWorldVersion = worldVer;
        _lastSpriteSnapshot = snap;
        _hasCache = true;

        ComputeWorldBounds(in _cached, out _boundsMinWorld, out _boundsMaxWorld);
        return true;
    }

    private void InvalidateCache()
    {
        _hasCache = false;
        // _lastWorldVersion не трогаем: кэш пересоберётся при следующем PrepareRender.
    }

    private static bool TryResolveSourceRect(in Rect rect, int texW, int texH, out Rect src)
    {
        var r = rect;

        // Если не задан — вся текстура
        if (r.Width <= 0 || r.Height <= 0)
            r = new Rect(x: 0, y: 0, width: texW, height: texH);

        if (r.Width <= 0 || r.Height <= 0)
        {
            src = default;
            return false;
        }

        src = new Rect(r.X, r.Y, r.Width, r.Height);
        return true;
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
