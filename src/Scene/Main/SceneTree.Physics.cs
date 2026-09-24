namespace Electron2D;

public sealed partial class SceneTree
{
    private PhysicsSpace? _physicsSpace;

    internal void RegisterPhysicsBody(PhysicsBody body)
    {
        EnsureOwnerThread();
        (_physicsSpace ??= new PhysicsSpace()).Add(body);
    }

    internal void UnregisterPhysicsBody(PhysicsBody body)
    {
        EnsureOwnerThread();
        _physicsSpace?.Remove(body);
    }
}
