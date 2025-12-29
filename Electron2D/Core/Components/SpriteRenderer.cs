using System.Numerics;

namespace Electron2D;

public sealed class SpriteRenderer
{
    private Node _owner = null!;
    private int _lastWorldVer = -1;

    // кеши
    private Vector2 _worldBounds;
    private /*ваши batch данные*/ object? _batchCache;

    internal void Attach(Node owner) => _owner = owner;

    internal void PrepareRender(/*RenderQueue q*/)
    {
        var ver = _owner.Transform.WorldVersion;
        if (ver != _lastWorldVer)
        {
            RebuildBoundsAndBatch();
            _lastWorldVer = ver;
        }

        // q.Push(_batchCache...);
    }

    private void RebuildBoundsAndBatch()
    {
        // bounds из WorldMatrix/WorldPosition + Size + Pivot
        // batch-данные (uv, color, layer, sort key) — один раз на версию
    }
}