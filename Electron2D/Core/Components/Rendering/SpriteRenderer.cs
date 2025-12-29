using System.Numerics;

namespace Electron2D;

public sealed class SpriteRenderer : IComponent
{
    private Node _owner = null!;
    private int _lastWorldVer = -1;

    private string? _spriteId;

    public void SetSprite(string spriteId) => _spriteId = spriteId;

    public void OnAttach(Node owner) => _owner = owner;

    public void OnDetach()
    {
        _owner = null!;
        _lastWorldVer = -1;
        _spriteId = null;
    }

    internal void PrepareRender(RenderQueue q)
    {
        var ver = _owner.Transform.WorldVersion;
        if (ver == _lastWorldVer) return;

        // TODO: собрать SpriteCommand в очередь q (атлас/текстура через ResourceSystem).
        _lastWorldVer = ver;
    }
}