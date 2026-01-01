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
    
    private Vector2 _baseSizeWorld;   // размер в мире при scale=(1,1)
    private Vector2 _pivot;           // pivot из Sprite
    private Vector2 _lastAbsScale;    // abs(worldScale)
    private byte _lastScaleSignMask;  // bit0 = x<0, bit1 = y<0

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

    internal void PrepareRender(RenderQueue queue, ResourceSystem resources, in ViewCullRect view)
    {
        var owner = _owner;
        var sprite = _sprite;

        if (owner == null || sprite == null)
            return;

        owner.Transform.GetWorldTRS(out var wPos, out var wRot, out var wScale, out var worldVer);

        if (!TryEnsureCached(sprite, resources, in wPos, wRot, in wScale, worldVer))
            return;

        if (!view.Intersects(in _boundsMinWorld, in _boundsMaxWorld))
            return;

        queue.TryPush(in _cached);
    }
    
    private static byte GetScaleSignMask(in Vector2 s)
    {
        byte m = 0;
        if (s.X < 0) m |= 1;
        if (s.Y < 0) m |= 2;
        return m;
    }

    private bool TryEnsureCached(
        Sprite sprite,
        ResourceSystem resources,
        in Vector2 wPos,
        float wRot,
        in Vector2 wScale,
        int worldVer)
    {
        // 1) Проверяем изменения Sprite (как у вас сейчас)
        var snap = SpriteSnapshot.From(sprite);

        // Быстрый путь: sprite не менялся, текстура валидна
        if (_hasCache && snap.Equals(_lastSpriteSnapshot) && _cached.Texture.IsValid)
        {
            if (worldVer != _lastWorldVersion)
            {
                UpdateTransformCache(in wPos, wRot, in wScale);
                _lastWorldVersion = worldVer;
            }
            return true;
        }

        // 2) Rebuild статической части (sprite изменился / кэша нет / текстура пока невалидна)
        var tex = sprite.Texture;
        if (!tex.IsValid)
        {
            if (snap.TextureId == null)
                return false;

            // late resolve
            tex = resources.GetTexture(snap.TextureId);
            if (!tex.IsValid)
                return false;
        }

        // Src rect
        Rect srcRect = snap.TextureRect;
        if (srcRect.IsEmpty)
            srcRect = new Rect(0, 0, tex.Width, tex.Height);

        // Сохраняем статические данные
        _baseSizeWorld = new Vector2(srcRect.Width / snap.PixelsPerUnit, srcRect.Height / snap.PixelsPerUnit);
        _pivot = snap.Pivot;

        var absScale = Abs(in wScale);
        var sizeWorld = _baseSizeWorld * absScale;
        var originWorld = sizeWorld * _pivot;

        var flip = snap.Flip;
        if (wScale.X < 0) flip ^= FlipMode.Horizontal;
        if (wScale.Y < 0) flip ^= FlipMode.Vertical;

        _cached = new SpriteCommand
        {
            Texture = tex,
            SrcRect = srcRect,
            PositionWorld = wPos,
            Rotation = wRot,
            SizeWorld = sizeWorld,
            OriginWorld = originWorld,
            Color = _color,
            SortKey = _sortKey,
            FlipMode = flip
        };

        ComputeWorldBounds(in _cached, out _boundsMinWorld, out _boundsMaxWorld);

        _lastSpriteSnapshot = snap;
        _lastWorldVersion = worldVer;
        _lastAbsScale = absScale;
        _lastScaleSignMask = GetScaleSignMask(in wScale);
        _hasCache = true;

        return true;
    }
    
    private static Vector2 Abs(in Vector2 v) => new(MathF.Abs(v.X), MathF.Abs(v.Y));
    
    private void UpdateTransformCache(in Vector2 wPos, float wRot, in Vector2 wScale)
    {
        var absScale = Abs(in wScale);
        var signMask = GetScaleSignMask(in wScale);

        // Если absScale и знак scale не менялись — статическая геометрия та же
        if (absScale == _lastAbsScale && signMask == _lastScaleSignMask)
        {
            var oldPos = _cached.PositionWorld;
            var oldRot = _cached.Rotation;

            _cached.PositionWorld = wPos;
            _cached.Rotation = wRot;

            // Самый дешёвый кейс: только перенос (rotation тот же) — просто сдвигаем bounds
            if (wRot == oldRot)
            {
                var delta = wPos - oldPos;
                _boundsMinWorld += delta;
                _boundsMaxWorld += delta;
                return;
            }

            // Rotation поменялся — bounds нужно пересчитать
            ComputeWorldBounds(in _cached, out _boundsMinWorld, out _boundsMaxWorld);
            return;
        }

        // Scale (abs) или знак поменялись — пересчитать size/origin/flip и bounds
        var sizeWorld = _baseSizeWorld * absScale;
        var originWorld = sizeWorld * _pivot;

        _cached.PositionWorld = wPos;
        _cached.Rotation = wRot;
        _cached.SizeWorld = sizeWorld;
        _cached.OriginWorld = originWorld;

        var flip = _lastSpriteSnapshot.Flip;
        if (wScale.X < 0) flip ^= FlipMode.Horizontal;
        if (wScale.Y < 0) flip ^= FlipMode.Vertical;
        _cached.FlipMode = flip;

        ComputeWorldBounds(in _cached, out _boundsMinWorld, out _boundsMaxWorld);

        _lastAbsScale = absScale;
        _lastScaleSignMask = signMask;
    }

    private void InvalidateCache()
    {
        _hasCache = false;
        // _lastWorldVersion не трогаем: кэш пересоберётся при следующем PrepareRender.
    }

    /*private static bool TryResolveSourceRect(in Rect rect, int texW, int texH, out Rect src)
    {
        var r = rect;

        // Если не задан — вся текстура
        if (r.Width <= 0 || r.Height <= 0)
            r = new Rect(0, 0, texW, texH);

        if (r.Width <= 0 || r.Height <= 0)
        {
            src = default;
            return false;
        }

        src = new Rect(r.X, r.Y, r.Width, r.Height);
        return true;
    }*/
    
    private static void ComputeWorldBounds(in SpriteCommand cmd, out Vector2 min, out Vector2 max)
    {
        var pos = cmd.PositionWorld;
        var size = cmd.SizeWorld;
        var origin = cmd.OriginWorld;

        // Local rect relative to pivot (pivot at 0,0)
        var x0 = -origin.X;
        var x1 = size.X - origin.X;
        var y0 = -origin.Y;
        var y1 = size.Y - origin.Y;

        var rot = cmd.Rotation;

        // Fast path: no rotation
        if (rot == 0f)
        {
            min = new Vector2(pos.X + x0, pos.Y + y0);
            max = new Vector2(pos.X + x1, pos.Y + y1);
            return;
        }

        // Один вызов вместо Cos+Sin по отдельности (если вы на .NET 7+)
        var (s, c) = MathF.SinCos(rot);

        // Мы хотим min/max для:
        // X' = c*x - s*y = a*x + b*y, где a=c, b=-s
        // Y' = s*x + c*y = a*x + b*y, где a=s, b=c

        // --- X' ---
        {
            var a = c;
            var b = -s;

            var minX = (a >= 0f ? x0 * a : x1 * a) + (b >= 0f ? y0 * b : y1 * b);
            var maxX = (a >= 0f ? x1 * a : x0 * a) + (b >= 0f ? y1 * b : y0 * b);

            // --- Y' ---
            a = s;
            b = c;

            var minY = (a >= 0f ? x0 * a : x1 * a) + (b >= 0f ? y0 * b : y1 * b);
            var maxY = (a >= 0f ? x1 * a : x0 * a) + (b >= 0f ? y1 * b : y0 * b);

            min = new Vector2(pos.X + minX, pos.Y + minY);
            max = new Vector2(pos.X + maxX, pos.Y + maxY);
        }
    }
}
