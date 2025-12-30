using System.Numerics;

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

        if (owner is null || sprite is null)
            return;

        var ver = owner.Transform.WorldVersion;

        if (!_hasCached || ver != _lastWorldVer)
        {
            // 1) Текстура: либо задана в Sprite напрямую, либо резолвим по id.
            var tex = sprite.Texture;

            if (!tex.IsValid)
            {
                var id = sprite.TextureId;
                if (id is null)
                    return;

                tex = resources.GetTexture(id);
                if (!tex.IsValid)
                    return;
            }

            // 2) Source rect: если не задан — берём всю текстуру.
            var tr = sprite.TextureRect;
            if (tr.Width <= 0 || tr.Height <= 0)
                tr = new Rect(x: 0, y: 0, width: tex.Width, height: tex.Height);

            if (tr.Width <= 0 || tr.Height <= 0)
                return;

            var srcRect = new Rect(tr.X, tr.Y, tr.Width, tr.Height);

            var ppu = sprite.PixelsPerUnit;
            if (!(ppu > 0f))
                ppu = 100f;

            var sizeWorld = new Vector2(tr.Width / ppu, tr.Height / ppu);
            sizeWorld *= owner.Transform.WorldScale;

            // Pivot нормализованный (0..1) относительно sizeWorld.
            var originWorld = new Vector2(sizeWorld.X * sprite.Pivot.X, sizeWorld.Y * sprite.Pivot.Y);

            _cached = new SpriteCommand
            {
                Texture = tex,
                SrcRect = srcRect,
                PositionWorld = owner.Transform.WorldPosition,
                SizeWorld = sizeWorld,
                Rotation = owner.Transform.WorldRotation, // радианы; перевод в градусы делается в RenderSystem
                OriginWorld = originWorld,
                Color = _color,
                SortKey = _sortKey
            };

            _lastWorldVer = ver;
            _hasCached = true;
        }

        q.TryPush(in _cached);
    }
}
