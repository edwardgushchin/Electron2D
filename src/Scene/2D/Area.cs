namespace Electron2D;

/// <summary>A two-dimensional region that reports physics bodies and other areas crossing its shapes.</summary>
/// <remarks>Attach direct <see cref="CollisionShape"/> children and add the area to a scene tree.
/// Overlap snapshots update after each nonzero fixed physics step; changing a transform does not update a snapshot immediately.</remarks>
public sealed partial class Area : CollisionObject
{
    private static readonly PropertyDescriptor[] AreaProperties =
    [
        new PropertyDescriptor<Area, bool>(nameof(Monitoring), area => area.Monitoring,
            (area, value) => area.Monitoring = value, _ => true, stored: true),
        new PropertyDescriptor<Area, bool>(nameof(Monitorable), area => area.Monitorable,
            (area, value) => area.Monitorable = value, _ => true, stored: true),
        new PropertyDescriptor<Area, bool>(nameof(AudioBusOverride), area => area.AudioBusOverride,
            (area, value) => area.AudioBusOverride = value, _ => false, stored: true),
        new PropertyDescriptor<Area, string>(nameof(AudioBusName), area => area.AudioBusName,
            (area, value) => area.AudioBusName = value, _ => "Master", stored: true)
    ];

    private readonly List<ulong> _appliedShapeRevisions = [];
    private HashSet<CollisionObject> _overlaps = new(ReferenceEqualityComparer.Instance);
    private HashSet<CollisionObject> _nextOverlaps = new(ReferenceEqualityComparer.Instance);
    private readonly PhysicsShapePairTracker _shapePairs = new();
    private readonly List<PhysicsShapePairChange> _pairChanges = [];
    private bool _dispatchingOverlap;
    private bool _audioBusOverride;
    private string _audioBusName = "Master";

    /// <summary>Gets or sets whether a spatial stream inside this Area routes to its audio bus.</summary>
    /// <value>False initially; routing also requires the player's area-mask and this Area's collision layer to overlap.</value>
    /// <exception cref="InvalidOperationException">An attached area is accessed off-owner or mutated during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The area is disposed.</exception>
    public bool AudioBusOverride
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _audioBusOverride; }
        set { EnsureMutable(); _audioBusOverride = value; }
    }

    /// <summary>Gets or sets the requested bus for spatial streams in this Area.</summary>
    /// <value>Master initially; a missing bus resolves to Master at playback time.</value>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="InvalidOperationException">An attached area is accessed off-owner or mutated during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The area is disposed.</exception>
    public string AudioBusName
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _audioBusName; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); _audioBusName = value; }
    }
    internal PhysicsSpace? Space => Backend.Space;
    private Vector2 _lastPosition;
    private float _lastRotation;
    private volatile bool _shapesDirty = true;
    private bool _monitoring = true;
    private bool _monitorable = true;

    /// <summary>Creates a detached area that monitors bodies and is monitorable by other areas.</summary>
    public Area() { }

    /// <summary>Gets or sets whether this area detects overlapping bodies and areas.</summary>
    /// <value>True by default. Disabling takes effect on the next physics step.</value>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool Monitoring
    {
        get { ThrowIfDisposed(); return _monitoring; }
        set { EnsureMutable(); if (_monitoring == value) return; if (_dispatchingOverlap) throw new InvalidOperationException("Change monitoring after overlap callbacks return."); _monitoring = value; }
    }

    /// <summary>Gets or sets whether other monitoring areas can detect this area.</summary>
    /// <value>True by default. Disabling takes effect on their next physics step.</value>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool Monitorable
    {
        get { ThrowIfDisposed(); return _monitorable; }
        set { EnsureMutable(); if (_monitorable == value) return; if (_dispatchingOverlap) throw new InvalidOperationException("Change monitorable policy after overlap callbacks return."); _monitorable = value; }
    }

    /// <summary>Occurs after a fixed step when another area begins overlapping this area.</summary>
    public event Action<Area>? AreaEntered;

    /// <summary>Occurs after a fixed step when another area stops overlapping this area.</summary>
    public event Action<Area>? AreaExited;

    /// <summary>Occurs after a fixed step when a physics body begins overlapping this area.</summary>
    public event Action<Entity>? BodyEntered;

    /// <summary>Occurs after a fixed step when a physics body stops overlapping this area.</summary>
    public event Action<Entity>? BodyExited;

    /// <summary>Occurs when a logical Area shape pair starts overlapping.</summary>
    /// <remarks>Arguments are collider RID, nullable scene Area, collider global shape index and local global shape index.
    /// Server-only colliders have a null scene object. Monitoring must be enabled.</remarks>
    public event Action<RID, Area?, int, int>? AreaShapeEntered;
    /// <summary>Occurs when a retained logical Area shape pair stops overlapping.</summary>
    /// <remarks>Arguments preserve the departed RID and both sampled global shape indices.</remarks>
    public event Action<RID, Area?, int, int>? AreaShapeExited;
    /// <summary>Occurs when a logical body/Area shape pair starts overlapping.</summary>
    /// <remarks>Arguments are body RID, nullable spatial body, body global shape index and local global shape index.</remarks>
    public event Action<RID, Entity?, int, int>? BodyShapeEntered;
    /// <summary>Occurs when a retained logical body/Area shape pair stops overlapping.</summary>
    /// <remarks>Server-only bodies have a null scene object; departure preserves sampled indices.</remarks>
    public event Action<RID, Entity?, int, int>? BodyShapeExited;

    /// <summary>Returns the current area-overlap snapshot.</summary>
    /// <returns>A caller-owned array updated from the last nonzero fixed step.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public Area[] GetOverlappingAreas()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        var result = new List<Area>();
        foreach (var other in _overlaps)
            if (other is Area area) result.Add(area);
        return result.ToArray();
    }

    /// <summary>Returns the current body-overlap snapshot.</summary>
    /// <returns>A caller-owned array of spatial bodies updated from the last nonzero fixed step.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public Entity[] GetOverlappingBodies()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        var result = new List<Entity>();
        foreach (var other in _overlaps)
            if (other is PhysicsBody body) result.Add(body);
        return result.ToArray();
    }

    /// <summary>Tests whether the last fixed step found any overlapping areas.</summary>
    /// <returns>True when at least one monitorable area is in the snapshot.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool HasOverlappingAreas()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        foreach (var other in _overlaps)
            if (other is Area) return true;
        return false;
    }

    /// <summary>Tests whether the last fixed step found any overlapping bodies.</summary>
    /// <returns>True when at least one physics body is in the snapshot.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool HasOverlappingBodies()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        foreach (var other in _overlaps)
            if (other is PhysicsBody) return true;
        return false;
    }

    /// <summary>Tests whether the given scene node is in the current area-overlap snapshot.</summary>
    /// <param name="area">The area node to test.</param>
    /// <returns>False for null, foreign, detached or nonarea nodes.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool OverlapsArea(Node? area)
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return area is Area other && _overlaps.Contains(other);
    }

    /// <summary>Tests whether the given scene node is in the current body-overlap snapshot.</summary>
    /// <param name="body">The body node to test.</param>
    /// <returns>False for null, foreign, detached or nonbody nodes.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool OverlapsBody(Node? body)
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return body is PhysicsBody other && _overlaps.Contains(other);
    }

    internal override void MarkShapesDirty() => _shapesDirty = true;
    internal override void OnCollisionFilterChanged() => MarkShapesDirty();

    internal void AttachBackend(PhysicsSpace space)
    {
        if (Space is not null) throw new InvalidOperationException("An area already belongs to a physics world.");
        ValidatePhysicsTransform();
        _lastPosition = GlobalPosition;
        _lastRotation = GlobalRotation;
        Backend.Attach(space, _lastPosition, _lastRotation, new(PhysicsServer.BodyMode.Static));
        _shapesDirty = true;
        try { RebuildShapes(); }
        catch { DetachBackend(); throw; }
    }

    internal void DetachBackend()
    {
        PhysicsServer.Service.FindAreaRuntime(PhysicsRID)?.Reset();
        if (Space is null) return;
        Backend.Detach();
        _appliedShapeRevisions.Clear();
        _shapesDirty = true;
    }

    internal void PrepareBackend()
    {
        if (Space is null) return;
        ValidatePhysicsTransform();
        if (!_shapesDirty)
            for (var index = 0; index < ShapeSlots.Count; index++)
                if (ShapeSlots[index].Revision != _appliedShapeRevisions[index]) { MarkShapesDirty(); break; }
        if (_shapesDirty) RebuildShapes();
        var position = GlobalPosition;
        var rotation = GlobalRotation;
        if (position != _lastPosition || rotation != _lastRotation)
        {
            Backend.SetPose(position, rotation);
            _lastPosition = position;
            _lastRotation = rotation;
        }
    }

    internal void PrepareOverlaps(int objects, int pairs)
    {
        _overlaps.EnsureCapacity(objects); _nextOverlaps.EnsureCapacity(objects);
        _shapePairs.Prepare(pairs); _pairChanges.EnsureCapacity(checked(pairs * 4));
    }

    internal void BeginOverlapScan() { _nextOverlaps.Clear(); _shapePairs.Begin(); }
    internal void Observe(PhysicsShapePair pair)
    {
        _shapePairs.Observe(pair);
        if (pair.Other is { } other) _nextOverlaps.Add(other);
    }
    internal void CommitOverlapScan(List<PhysicsSpace.OverlapEvent> events)
    {
        _pairChanges.Clear(); _shapePairs.Commit(_pairChanges);
        foreach (var change in _pairChanges) events.Add(new(this, change));
        _pairChanges.Clear(); (_overlaps, _nextOverlaps) = (_nextOverlaps, _overlaps);
    }
    internal void Forget(CollisionObject other, List<PhysicsSpace.OverlapEvent> events) => ForgetRID(other.PhysicsRID, events);
    internal void ForgetRID(RID rid, List<PhysicsSpace.OverlapEvent> events)
    {
        _pairChanges.Clear(); _shapePairs.Forget(rid, _pairChanges);
        foreach (var change in _pairChanges)
        {
            if (change.Pair.Other is { } other) { _overlaps.Remove(other); _nextOverlaps.Remove(other); }
            events.Add(new(this, change));
        }
        _pairChanges.Clear();
    }
    internal void EnsureMonitorConfigurationChange()
    {
        if (_dispatchingOverlap) throw new InvalidOperationException("Area monitor configuration cannot change during an overlap callback.");
    }

    internal void ClearOverlaps() { _overlaps.Clear(); _nextOverlaps.Clear(); _shapePairs.Clear(); _pairChanges.Clear(); }
    internal bool ContainsOverlap(PhysicsShapePairChange change) => change.ObjectEvent
        ? change.Pair.Other is { } other && _overlaps.Contains(other) : _shapePairs.Contains(change.Pair);
    internal void RaiseOverlap(PhysicsShapePairChange change)
    {
        _dispatchingOverlap = true;
        try
        {
            var pair = change.Pair;
            if (change.ObjectEvent)
            {
                if (pair.Other is Area area) { if (change.Entered) AreaEntered?.Invoke(area); else AreaExited?.Invoke(area); }
                else if (pair.Other is Entity body) { if (change.Entered) BodyEntered?.Invoke(body); else BodyExited?.Invoke(body); }
            }
            else if (pair.IsArea)
            {
                if (change.Entered) AreaShapeEntered?.Invoke(pair.RID, pair.Other as Area, pair.OtherShape, pair.LocalShape);
                else AreaShapeExited?.Invoke(pair.RID, pair.Other as Area, pair.OtherShape, pair.LocalShape);
            }
            else
            {
                if (change.Entered) BodyShapeEntered?.Invoke(pair.RID, pair.Other, pair.OtherShape, pair.LocalShape);
                else BodyShapeExited?.Invoke(pair.RID, pair.Other, pair.OtherShape, pair.LocalShape);
            }
        }
        finally { _dispatchingOverlap = false; }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(AreaProperties).Concat(FieldProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Area)
        ? CreateArea : base.CreateSceneInstanceFactory();

    private static Node CreateArea() => new Area();

    internal override void UpdatePhysicsParticipation()
    {
        if (!IsInsideTree) return;
        if (PhysicsRemoved)
        {
            if (Space is not null) Tree?.UnregisterPhysicsArea(this);
        }
        else if (Space is null) Tree?.RegisterPhysicsArea(this);
    }

    /// <inheritdoc />
    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        UpdatePhysicsParticipation();
    }

    /// <inheritdoc />
    protected override void OnExitTree()
    {
        try { Tree?.UnregisterPhysicsArea(this); }
        finally { base.OnExitTree(); }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Space?.Remove(this);
            ClearOverlaps();
        }
        base.Dispose(disposing);
    }

    private void RebuildShapes()
    {
        Backend.RebuildShapes(ShapeSlots, CollisionLayer, CollisionMask, true, 0);
        _appliedShapeRevisions.Clear();
        foreach (var node in ShapeSlots) _appliedShapeRevisions.Add(node.Revision);
        _shapesDirty = false;
    }

    private void ValidatePhysicsTransform()
    {
        var transform = GlobalTransform;
        if (!transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(transform.Skew))
            throw new InvalidOperationException("Physics areas require unit global scale and zero skew.");
    }
}
