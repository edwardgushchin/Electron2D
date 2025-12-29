using System.Numerics;

namespace Electron2D;

public sealed class Rigidbody : IComponent
{
    private Node _owner = null!;
    private int _lastWorldVer = -1;

    public float Mass { get; set; } = 1f;

    public void AddForce(Vector2 force)
    {
        // TODO: интеграция с PhysicsSystem/Box2D.
        // Сейчас — заглушка.
    }

    public void OnAttach(Node owner) => _owner = owner;

    public void OnDetach()
    {
        _owner = null!;
        _lastWorldVer = -1;
    }

    internal void SyncToPhysicsWorldIfNeeded()
    {
        var ver = _owner.Transform.WorldVersion;
        if (ver == _lastWorldVer) return;

        // TODO: перенести pos/rot в b2-body (без утечки b2 типов наружу).
        _lastWorldVer = ver;
    }
}