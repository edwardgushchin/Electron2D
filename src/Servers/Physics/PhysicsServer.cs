namespace Electron2D;

/// <summary>Owns typed two-dimensional physics resource identities and spaces.</summary>
/// <remarks>Public static operations delegate to the retained service object; its state, identity and ownership remain object-scoped.</remarks>
public sealed partial class PhysicsServer : ElectronObject
{
    private static readonly PhysicsServer SharedInstance = new();
    private readonly object _registryGate = new();
    private readonly Dictionary<RID, WeakReference<CollisionObject>> _sceneObjects = [];
    private readonly List<RID> _staleSceneObjects = [];
    private int _sceneRegistrationsSinceSweep;
    private readonly Dictionary<RID, PhysicsSpace> _sceneSpaces = [];
    private readonly Dictionary<RID, PhysicsDirectSpaceState> _directStates = [];
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

    private PhysicsServer() { }

    internal static PhysicsServer Service => SharedInstance;

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
                foreach (var stale in _staleSceneObjects) { _sceneObjects.Remove(stale); _bodyRuntimes.Remove(stale); _areaRuntimes.Remove(stale); }
            }
        }
        return rid;
    }

    internal void UnregisterSceneObject(RID rid)
    {
        ClearJointsForBody(rid);
        lock (_registryGate)
        {
            _sceneObjects.Remove(rid);
            _bodyExceptions.Remove(rid);
            _bodyRuntimes.Remove(rid);
            _areaRuntimes.Remove(rid);
        }
    }

    internal CollisionObject? ResolveSceneObject(RID rid)
    {
        lock (_registryGate)
        {
            if (!_sceneObjects.TryGetValue(rid, out var weak)) return null;
            if (weak.TryGetTarget(out var node) && !node.IsDisposed) return node;
            _sceneObjects.Remove(rid);
            _bodyRuntimes.Remove(rid);
            _areaRuntimes.Remove(rid);
            return null;
        }
    }

    internal RID RegisterSceneSpace(PhysicsSpace space)
    {
        space.SetActive(true);
        var rid = RID.Allocate();
        space.RID = rid;
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

    internal PhysicsDirectSpaceState SpaceGetDirectStateCore(RID space)
    {
        ThrowIfDisposed();
        lock (_registryGate)
        {
            if (!_sceneSpaces.ContainsKey(space))
                throw new ArgumentException("The RID does not identify a live physics space.", nameof(space));
            if (!_directStates.TryGetValue(space, out var state) || state.IsDisposed)
                _directStates[space] = state = new PhysicsDirectSpaceState(space);
            return state;
        }
    }

    /// <inheritdoc />
    protected override void ValidateDisposal() =>
        throw new InvalidOperationException("The shared physics server cannot be disposed by a consumer.");
}
