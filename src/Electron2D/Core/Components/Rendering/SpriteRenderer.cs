using System.Numerics;

namespace Electron2D;

public sealed class SpriteRenderer : IComponent
{
    #region Constants
    private const int UnregisteredSceneIndex = -1;
    #endregion

    #region Fields
    // Индекс в SceneTree render-индексе. -1 => не зарегистрирован.
    internal int SceneIndex = UnregisteredSceneIndex;

    private Node? _owner;
    private Sprite? _sprite;

    private Color _color = new(0xFFFFFFFF);
    private uint _sortKey;
    private ushort _layer;
    private ushort _order;
    private RenderSpace _space = RenderSpace.World;

    // Кэш команды рендера (пересобирается только при необходимости)
    private bool _hasValidCache;
    private int _lastWorldVersion = -1;
    private SpriteSnapshot _lastSpriteSnapshot;

    private Vector2 _baseSizeWorldUnscaled; // размер в мире при scale=(1,1)
    private Vector2 _pivot;                 // pivot из Sprite
    private Vector2 _lastAbsScale;          // abs(worldScale)
    private byte _lastScaleSignMask;        // bit0 = x<0, bit1 = y<0

    private SpriteCommand _cachedCommand;
    private Vector2 _worldBoundsMin;
    private Vector2 _worldBoundsMax;
    #endregion

    #region Properties
    public Color Color
    {
        get => _color;
        set
        {
            if (_color.Equals(value))
                return;

            _color = value;
            InvalidateRenderCache();
        }
    }

    internal uint SortKey
    {
        get => _sortKey;
        set
        {
            if (_sortKey == value)
                return;

            _sortKey = value;
            InvalidateRenderCache();
        }
    }
    
    /// <summary>Слой отрисовки. Чем больше — тем позже рисуется (поверх).</summary>
    public int Layer
    {
        get => _layer;
        set
        {
            var v = (ushort)Math.Clamp(value, 0, ushort.MaxValue);
            if (_layer == v) return;
            _layer = v;
            _sortKey = DrawOrder.Pack(_layer, _order);
            InvalidateRenderCache();
        }
    }

    /// <summary>Порядок внутри слоя. Чем больше — тем позже рисуется.</summary>
    public int Order
    {
        get => _order;
        set
        {
            var v = (ushort)Math.Clamp(value, 0, ushort.MaxValue);
            if (_order == v) return;
            _order = v;
            _sortKey = DrawOrder.Pack(_layer, _order);
            InvalidateRenderCache();
        }
    }
    
    /// <summary>
    /// Где рисовать спрайт: World (через камеру) или Screen (UI, (0,0)=top-left, y-down, единицы = пиксели render-space).
    /// </summary>
    public RenderSpace Space
    {
        get => _space;
        set
        {
            if (_space == value)
                return;

            _space = value;
            InvalidateRenderCache();
        }
    }

    #endregion

    #region Public API
    public void SetSprite(Sprite sprite)
    {
        ArgumentNullException.ThrowIfNull(sprite);

        if (ReferenceEquals(_sprite, sprite))
            return;

        _sprite = sprite;
        InvalidateRenderCache();
    }

    public void OnAttach(Node owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        _owner = owner;
        SceneIndex = UnregisteredSceneIndex;

        ResetCacheState();
    }

    public void OnDetach()
    {
        _owner = null;
        _sprite = null;

        SceneIndex = UnregisteredSceneIndex;

        ResetCacheState();
    }
    #endregion

    #region Internal API
    internal void PrepareRender(RenderQueue queue, ResourceSystem resources, in ViewCullRect view)
    {
        var owner = _owner;
        var sprite = _sprite;

        if (owner is null || sprite is null)
            return;

        owner.Transform.GetWorldTRS(out var worldPosition, out var worldRotation, out var worldScale, out var worldVersion);

        if (!TryEnsureCached(sprite, resources, in worldPosition, worldRotation, in worldScale, worldVersion))
            return;

        if (!view.Intersects(in _worldBoundsMin, in _worldBoundsMax))
            return;

        queue.TryPush(in _cachedCommand);
    }
    #endregion

    #region Private helpers
    private static byte GetScaleSignMask(in Vector2 scale)
    {
        byte mask = 0;
        if (scale.X < 0) mask |= 1;
        if (scale.Y < 0) mask |= 2;
        return mask;
    }

    private bool TryEnsureCached(
        Sprite sprite,
        ResourceSystem resources,
        in Vector2 worldPosition,
        float worldRotation,
        in Vector2 worldScale,
        int worldVersion)
    {
        // 1) Проверяем изменения Sprite
        var snapshot = SpriteSnapshot.From(sprite);

        // Быстрый путь: sprite не менялся, текстура валидна
        if (_hasValidCache && snapshot.Equals(_lastSpriteSnapshot) && _cachedCommand.Texture.IsValid)
        {
            if (worldVersion != _lastWorldVersion)
            {
                UpdateTransformCache(in worldPosition, worldRotation, in worldScale);
                _lastWorldVersion = worldVersion;
            }

            return true;
        }

        // 2) Rebuild статической части (sprite изменился / кэша нет / текстура пока невалидна)
        var texture = sprite.Texture;
        if (!texture.IsValid)
        {
            if (snapshot.TextureId is null)
                return false;

            // late resolve
            texture = resources.GetTexture(snapshot.TextureId);
            if (!texture.IsValid)
                return false;
        }

        // Src rect
        var sourceRect = snapshot.TextureRect;
        if (sourceRect.IsEmpty)
            sourceRect = new Rect(0, 0, texture.Width, texture.Height);

        // Сохраняем статические данные
        _baseSizeWorldUnscaled = _space == RenderSpace.Screen
            ? new Vector2(sourceRect.Width, sourceRect.Height) // UI: единицы = пиксели render-space
            : new Vector2(sourceRect.Width / snapshot.PixelsPerUnit, sourceRect.Height / snapshot.PixelsPerUnit);
        _pivot = snapshot.Pivot;

        var absScale = AbsVector2(in worldScale);
        var sizeWorld = _baseSizeWorldUnscaled * absScale;
        var originWorld = sizeWorld * _pivot;

        var flip = snapshot.Flip;
        if (worldScale.X < 0) flip ^= FlipMode.Horizontal;
        if (worldScale.Y < 0) flip ^= FlipMode.Vertical;

        _cachedCommand = new SpriteCommand
        {
            Texture = texture,
            SrcRect = sourceRect,
            PositionWorld = worldPosition,
            Rotation = worldRotation,
            SizeWorld = sizeWorld,
            OriginWorld = originWorld,
            Color = _color,
            SortKey = _sortKey,
            FlipMode = flip,
            FilterMode = snapshot.FilterMode,
            Space = _space
        };

        ComputeWorldBounds(in _cachedCommand, out _worldBoundsMin, out _worldBoundsMax);

        _lastSpriteSnapshot = snapshot;
        _lastWorldVersion = worldVersion;
        _lastAbsScale = absScale;
        _lastScaleSignMask = GetScaleSignMask(in worldScale);
        _hasValidCache = true;

        return true;
    }

    private static Vector2 AbsVector2(in Vector2 v) => new(MathF.Abs(v.X), MathF.Abs(v.Y));

    private void UpdateTransformCache(in Vector2 worldPosition, float worldRotation, in Vector2 worldScale)
    {
        var absScale = AbsVector2(in worldScale);
        var signMask = GetScaleSignMask(in worldScale);

        // Если absScale и знак scale не менялись — статическая геометрия та же
        if (absScale == _lastAbsScale && signMask == _lastScaleSignMask)
        {
            var previousPosition = _cachedCommand.PositionWorld;
            var previousRotation = _cachedCommand.Rotation;

            _cachedCommand.PositionWorld = worldPosition;
            _cachedCommand.Rotation = worldRotation;

            // Самый дешёвый кейс: только перенос (rotation тот же) — просто сдвигаем bounds
            if (worldRotation == previousRotation)
            {
                var delta = worldPosition - previousPosition;
                _worldBoundsMin += delta;
                _worldBoundsMax += delta;
                return;
            }

            // Rotation поменялся — bounds нужно пересчитать
            ComputeWorldBounds(in _cachedCommand, out _worldBoundsMin, out _worldBoundsMax);
            return;
        }

        // Scale (abs) или знак поменялись — пересчитать size/origin/flip и bounds
        var sizeWorld = _baseSizeWorldUnscaled * absScale;
        var originWorld = sizeWorld * _pivot;

        _cachedCommand.PositionWorld = worldPosition;
        _cachedCommand.Rotation = worldRotation;
        _cachedCommand.SizeWorld = sizeWorld;
        _cachedCommand.OriginWorld = originWorld;

        var flip = _lastSpriteSnapshot.Flip;
        if (worldScale.X < 0) flip ^= FlipMode.Horizontal;
        if (worldScale.Y < 0) flip ^= FlipMode.Vertical;
        _cachedCommand.FlipMode = flip;

        ComputeWorldBounds(in _cachedCommand, out _worldBoundsMin, out _worldBoundsMax);

        _lastAbsScale = absScale;
        _lastScaleSignMask = signMask;
    }

    private void InvalidateRenderCache()
    {
        _hasValidCache = false;
        // _lastWorldVersion не трогаем: кэш пересоберётся при следующем PrepareRender.
    }

    private void ResetCacheState()
    {
        _hasValidCache = false;
        _lastWorldVersion = -1;

        _lastSpriteSnapshot = default;
        _baseSizeWorldUnscaled = default;
        _pivot = default;
        _lastAbsScale = default;
        _lastScaleSignMask = 0;

        _cachedCommand = default;
        _worldBoundsMin = default;
        _worldBoundsMax = default;
    }

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

        // Один вызов вместо Sin+Cos по отдельности (если вы на .NET 7+)
        var (s, c) = MathF.SinCos(rot);

        // X' = c*x - s*y
        // Y' = s*x + c*y
        var ax = c;
        var bx = -s;

        var minX = (ax >= 0f ? x0 * ax : x1 * ax) + (bx >= 0f ? y0 * bx : y1 * bx);
        var maxX = (ax >= 0f ? x1 * ax : x0 * ax) + (bx >= 0f ? y1 * bx : y0 * bx);

        var ay = s;
        var by = c;

        var minY = (ay >= 0f ? x0 * ay : x1 * ay) + (by >= 0f ? y0 * by : y1 * by);
        var maxY = (ay >= 0f ? x1 * ay : x0 * ay) + (by >= 0f ? y1 * by : y0 * by);

        min = new Vector2(pos.X + minX, pos.Y + minY);
        max = new Vector2(pos.X + maxX, pos.Y + maxY);
    }
    #endregion
}
