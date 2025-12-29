// FILE: Electron2D/Core/Components/Rendering/SpriteRenderer.cs
using System.Numerics;

namespace Electron2D;

public sealed class SpriteRenderer : IComponent
{
    private Node _owner = null!;
    private int _lastWorldVer = -1;

    private string? _spriteId;

    public float PixelPerUnit
    {
        get;
        set
        {
            field = value;
            _hasCached = false;
        }
    } = 100f;


    // кэш команды (пересобирается при изменении)
    private bool _hasCached;
    private SpriteCommand _cached;

    public void SetSprite(string spriteId)
    {
        _spriteId = spriteId;
        _hasCached = false;
    }

    public void SetSprite(string spriteId, float pixelsPerUnit)
    {
        _spriteId = spriteId;
        PixelPerUnit = pixelsPerUnit;
        _hasCached = false;
    }

    public void OnAttach(Node owner) => _owner = owner;

    public void OnDetach()
    {
        _owner = null!;
        _lastWorldVer = -1;
        _spriteId = null;
        _hasCached = false;
        _cached = default;
        PixelPerUnit = 100f;
    }

    internal void PrepareRender(RenderQueue q, ResourceSystem resources)
    {
        if (_spriteId is null)
            return;

        var ver = _owner.Transform.WorldVersion;

        if (!_hasCached || ver != _lastWorldVer)
        {
            // TODO: дальше логично заменить на resources.GetSprite(_spriteId) => (Texture, srcRectPx, pivot, ppu, tint, ...)
            var tex = resources.GetTexture(_spriteId);
            if (!tex.IsValid) return;

            var pos = _owner.Transform.WorldPosition;
            var rot = _owner.Transform.WorldRotation;

            var ppu = PixelPerUnit;
            if (ppu <= 0f) ppu = 100f;

            var sizeWorld = new Vector2(tex.Width / ppu, tex.Height / ppu);
            sizeWorld *= _owner.Transform.WorldScale;

            _cached = new SpriteCommand(
                tex: tex,
                srcPx: Vector2.Zero,
                pos: pos,
                size: sizeWorld,
                rot: rot,
                color: 0xFFFFFFFF,
                sortKey: 0);

            _lastWorldVer = ver;
            _hasCached = true;
        }

        q.TryPush(in _cached);
    }
}
