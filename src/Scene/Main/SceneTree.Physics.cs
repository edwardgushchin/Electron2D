namespace Electron2D;

public sealed partial class SceneTree
{
    private PhysicsSpace? _physicsSpace;
    private RID _physicsSpaceRID;
    private World2D? _physicsWorld2D;

    internal World2D GetPhysicsWorld2D()
    {
        EnsureOwnerThread();
        EnsurePhysicsSpace();
        return _physicsWorld2D is { IsDisposed: false } world ? world :
            _physicsWorld2D = new World2D(_physicsSpaceRID);
    }

    private PhysicsSpace EnsurePhysicsSpace()
    {
        if (_physicsSpace is { } current) return current;
        var space = new PhysicsSpace();
        try
        {
            _physicsSpaceRID = PhysicsServer.Instance.RegisterSceneSpace(space);
            _physicsSpace = space;
            return space;
        }
        catch { space.Dispose(); throw; }
    }

    internal void EnsurePhysicsParticipationChange() => _physicsSpace?.EnsureQueryAccess();

    internal string ResolveSpatialAudioBus(Vector2 position, uint areaMask, string requestedBus)
    {
        EnsureOwnerThread();
        return areaMask == 0 || _physicsSpace is null
            ? requestedBus : _physicsSpace.FindAudioBusOverride(position, areaMask) ?? requestedBus;
    }

    internal void RegisterPhysicsBody(PhysicsBody body)
    {
        EnsureOwnerThread();
        EnsurePhysicsSpace().Add(body);
    }

    internal void UnregisterPhysicsBody(PhysicsBody body)
    {
        EnsureOwnerThread();
        _physicsSpace?.Remove(body);
    }

    internal void RegisterPhysicsJoint(Joint joint)
    {
        EnsureOwnerThread();
        EnsurePhysicsSpace().Add(joint);
    }

    internal void UnregisterPhysicsJoint(Joint joint)
    {
        EnsureOwnerThread();
        _physicsSpace?.Remove(joint);
    }

    internal void RegisterPhysicsArea(Area area)
    {
        EnsureOwnerThread();
        EnsurePhysicsSpace().Add(area);
    }

    internal void UnregisterPhysicsArea(Area area)
    {
        EnsureOwnerThread();
        _physicsSpace?.Remove(area);
    }
}
