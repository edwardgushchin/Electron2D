namespace Electron2D;

/// <summary>Owns typed two-dimensional physics resource identities and spaces.</summary>
public sealed partial class PhysicsServer2D : ElectronObject
{
    private static readonly PhysicsServer2D SharedInstance = new();
    private readonly object _registryGate = new();
    private readonly Dictionary<RID, WeakReference<CollisionObject>> _sceneObjects = [];
    private readonly List<RID> _staleSceneObjects = [];
    private int _sceneRegistrationsSinceSweep;
    private readonly Dictionary<RID, PhysicsSpace> _sceneSpaces = [];
    private readonly Dictionary<RID, PhysicsDirectSpaceState2D> _directStates = [];
    private readonly HashSet<RID> _ownedSpaces = [];
    private readonly Dictionary<RID, PhysicsServerShape> _serverShapes = [];
    private readonly Dictionary<RID, PhysicsServerCollider> _serverColliders = [];

    /// <summary>Specifies a server-created body's solver motion policy.</summary>
    public enum BodyMode
    {
        /// <summary>A stationary body.</summary>
        Static = 0,
        /// <summary>A manually moved body with contact velocity.</summary>
        Kinematic = 1,
        /// <summary>A freely simulated rigid body.</summary>
        Rigid = 2,
        /// <summary>A freely simulated body with rotation locked.</summary>
        RigidLinear = 3
    }

    private PhysicsServer2D() { }

    /// <summary>Gets the process-wide server for physics resources and scene spaces.</summary>
    /// <value>The shared server; consumer disposal is rejected.</value>
    public static PhysicsServer2D Instance => SharedInstance;

    internal RID RegisterSceneObject(CollisionObject node)
    {
        var rid = RID.Allocate();
        lock (_registryGate)
        {
            _sceneObjects.Add(rid, new(node));
            if (++_sceneRegistrationsSinceSweep >= 256)
            {
                _sceneRegistrationsSinceSweep = 0;
                _staleSceneObjects.Clear();
                foreach (var pair in _sceneObjects)
                    if (!pair.Value.TryGetTarget(out var target) || target.IsDisposed)
                        _staleSceneObjects.Add(pair.Key);
                foreach (var stale in _staleSceneObjects) _sceneObjects.Remove(stale);
            }
        }
        return rid;
    }

    internal void UnregisterSceneObject(RID rid)
    {
        lock (_registryGate)
        {
            _sceneObjects.Remove(rid);
            _bodyExceptions.Remove(rid);
        }
    }

    internal CollisionObject? ResolveSceneObject(RID rid)
    {
        lock (_registryGate)
        {
            if (!_sceneObjects.TryGetValue(rid, out var weak)) return null;
            if (weak.TryGetTarget(out var node) && !node.IsDisposed) return node;
            _sceneObjects.Remove(rid);
            return null;
        }
    }

    internal RID RegisterSceneSpace(PhysicsSpace space)
    {
        var rid = RID.Allocate();
        lock (_registryGate) _sceneSpaces.Add(rid, space);
        return rid;
    }

    internal void UnregisterSceneSpace(RID rid)
    {
        lock (_registryGate)
        {
            _sceneSpaces.Remove(rid);
            _directStates.Remove(rid);
        }
    }

    internal PhysicsSpace GetSceneSpace(RID rid)
    {
        lock (_registryGate)
            return _sceneSpaces.TryGetValue(rid, out var space) ? space :
                throw new ArgumentException("The RID does not identify a live physics space.", nameof(rid));
    }

    /// <summary>Returns a live direct-query view of a physics space.</summary>
    /// <param name="space">A live space RID.</param>
    /// <returns>The cached query view, or a fresh view if a caller disposed the previous one.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live physics space.</exception>
    public PhysicsDirectSpaceState2D SpaceGetDirectState(RID space)
    {
        ThrowIfDisposed();
        lock (_registryGate)
        {
            if (!_sceneSpaces.ContainsKey(space))
                throw new ArgumentException("The RID does not identify a live physics space.", nameof(space));
            if (!_directStates.TryGetValue(space, out var state) || state.IsDisposed)
                _directStates[space] = state = new PhysicsDirectSpaceState2D(space);
            return state;
        }
    }

    /// <inheritdoc />
    protected override void ValidateDisposal() =>
        throw new InvalidOperationException("The shared physics server cannot be disposed by a consumer.");
}
