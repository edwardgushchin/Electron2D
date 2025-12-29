namespace Electron2D;

public sealed class Rigidbody
{
    private Node _owner = null!;
    private int _lastWorldVer = -1;

    internal void Attach(Node owner) => _owner = owner;

    internal void SyncToPhysicsWorldIfNeeded()
    {
        var ver = _owner.Transform.WorldVersion;
        if (ver == _lastWorldVer) return;

        // Перенести pos/rot (и scale если нужно) в b2-body
        // Никаких b2 типов наружу.

        _lastWorldVer = ver;
    }
}