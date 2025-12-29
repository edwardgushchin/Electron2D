// FILE: Electron2D/Core/Components/Rendering/SpriteRenderer.cs
using System.Numerics;

namespace Electron2D;

public sealed class SpriteRenderer : IComponent
{
    private Node _owner = null!;
    private int _lastWorldVer = -1;

    private string? _spriteId;

    // кэш команды (пересобирается при изменении)
    private bool _hasCached;
    private SpriteCommand _cached;

    public void SetSprite(string spriteId)
    {
        _spriteId = spriteId;
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
    }

    internal void PrepareRender(RenderQueue q, ResourceSystem resources)
    {
        if (_spriteId is null)
            return;

        var ver = _owner.Transform.WorldVersion;

        if (!_hasCached || ver != _lastWorldVer)
        {
            // TODO: resources.GetSprite(_spriteId) должен вернуть (Texture, srcPx, sizeWorld, color, sortKey, ...)
            var tex = resources.GetTexture(_spriteId);

            var pos = _owner.Transform.WorldPosition;
            var rot = _owner.Transform.WorldRotation;

            // TODO: src/size/sortKey — временно заглушки
            _cached = new SpriteCommand(
                tex: tex,
                srcPx: Vector2.Zero,
                pos: pos,
                size: Vector2.One,
                rot: rot,
                color: 0xFFFFFFFF,
                sortKey: 0);

            _lastWorldVer = ver;
            _hasCached = true;
        }

        q.TryPush(in _cached);
    }
}