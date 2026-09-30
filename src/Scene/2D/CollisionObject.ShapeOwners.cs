namespace Electron2D;

public abstract partial class CollisionObject
{
    private readonly SortedDictionary<uint, ShapeOwner> _shapeOwners = [];
    private readonly List<ShapeSlot> _shapeSlots = [];
    private readonly Dictionary<ICollisionGeometry, uint> _childOwners = new(ReferenceEqualityComparer.Instance);

    internal sealed class ShapeOwner(uint id, ElectronObject? owner)
    {
        internal uint ID { get; } = id;
        internal readonly WeakReference<ElectronObject>? Object = owner is null ? null : new(owner);
        internal Transform Transform = Transform.Identity;
        internal bool Disabled;
        internal bool OneWay;
        internal float Margin;
        internal Vector2 Direction = Vector2.Down;
        internal readonly List<ShapeSlot> Shapes = [];
        internal ElectronObject? GetObject() => Object?.TryGetTarget(out var value) == true && !value.IsDisposed ? value : null;
    }

    internal sealed class ShapeSlot(ShapeOwner owner, Shape shape, int index)
    {
        internal ShapeOwner Owner { get; } = owner;
        internal PhysicsServerShape? ServerShape;
        internal Shape Shape => ServerShape?.Geometry ?? shape;
        internal Transform? TransformOverride;
        internal bool? DisabledOverride;
        internal bool? OneWayOverride;
        internal float MarginOverride;
        internal Vector2 DirectionOverride;
        internal Transform Transform => TransformOverride ?? Owner.Transform;
        internal int Index = index;
        internal ulong Revision => Shape.IsDisposed ? ulong.MaxValue : Shape.GeometryRevision;
        internal bool Active => !(DisabledOverride ?? Owner.Disabled) && !Shape.IsDisposed;
        internal OneWayContactData? OneWay => (OneWayOverride ?? Owner.OneWay)
            ? new((OneWayOverride.HasValue ? DirectionOverride : Owner.Direction).Rotated(Transform.Rotation),
                OneWayOverride.HasValue ? MarginOverride : Owner.Margin) : null;
    }

    internal IReadOnlyList<ShapeSlot> ShapeSlots => _shapeSlots;
    internal ElectronObject? GetShapeOwnerObject(int index) =>
        (uint)index < (uint)_shapeSlots.Count ? _shapeSlots[index].Owner.GetObject() : null;

    /// <summary>Creates an empty shape-owner group for a weakly referenced object identity.</summary>
    /// <param name="owner">Owner object, or null; its lifetime does not control the group's shapes.</param>
    /// <returns>Zero when the registry is empty, otherwise one above its largest current owner ID.</returns>
    /// <remarks>Removed highest IDs can be reused. A group starts enabled with identity transform and no one-way response.</remarks>
    public uint CreateShapeOwner(ElectronObject? owner)
    {
        EnsureMutable();
        if (owner?.IsDisposed == true) throw new ObjectDisposedException(nameof(owner));
        uint id = 0;
        foreach (var key in _shapeOwners.Keys)
            id = key == uint.MaxValue ? throw new InvalidOperationException("Shape owner IDs are exhausted.") : key + 1;
        _shapeOwners.Add(id, new(id, owner));
        return id;
    }

    /// <summary>Removes an owner group and all its shapes, renumbering later global indices.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    public void RemoveShapeOwner(uint ownerID)
    {
        EnsureMutable(); var owner = LookupShapeOwner(ownerID); ClearOwnerShapes(owner);
        _shapeOwners.Remove(ownerID);
        foreach (var pair in _childOwners)
            if (pair.Value == ownerID) { _childOwners.Remove(pair.Key); break; }
    }

    /// <summary>Returns a caller-owned array of current owner IDs in ascending order.</summary>
    /// <returns>Includes empty and disabled groups.</returns>
    public uint[] GetShapeOwners() { ReadOwners(); return _shapeOwners.Keys.ToArray(); }

    /// <summary>Gets a group's weakly referenced owner object.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <returns>The live object, or null when absent, collected or disposed.</returns>
    public ElectronObject? ShapeOwnerGetOwner(uint ownerID) => LookupShapeOwner(ownerID).GetObject();

    /// <summary>Gets a group's local transform relative to this collision object.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <returns>The configured local pose.</returns>
    public Transform ShapeOwnerGetTransform(uint ownerID) => LookupShapeOwner(ownerID).Transform;

    /// <summary>Sets a finite translated/rotated unit-scale local pose for all group shapes.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <param name="transform">Finite identity-scale, zero-skew local transform.</param>
    /// <exception cref="ArgumentException">The transform is nonfinite, scaled or skewed.</exception>
    public void ShapeOwnerSetTransform(uint ownerID, Transform transform)
    {
        EnsureMutable(); var owner = LookupShapeOwner(ownerID); ValidateOwnerTransform(transform);
        owner.Transform = transform;
        foreach (var slot in owner.Shapes) slot.TransformOverride = null;
        MarkShapesDirty();
    }

    /// <summary>Sets whether a group contributes active collision fixtures.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <param name="disabled">True removes active response/sensing without removing indexed shapes.</param>
    public void ShapeOwnerSetDisabled(uint ownerID, bool disabled)
    {
        EnsureMutable(); var owner = LookupShapeOwner(ownerID); owner.Disabled = disabled;
        foreach (var slot in owner.Shapes) slot.DisabledOverride = null;
        MarkShapesDirty();
    }

    /// <summary>Tests a group's disabled policy.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <returns>The stored disabled flag.</returns>
    public bool IsShapeOwnerDisabled(uint ownerID) => LookupShapeOwner(ownerID).Disabled;

    /// <summary>Sets one-way response on a body group; Area setters have no effect.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <param name="enable">Whether response uses the configured pass-through direction.</param>
    public void ShapeOwnerSetOneWayCollision(uint ownerID, bool enable)
    {
        EnsureMutable(); if (this is Area) return; var owner = LookupShapeOwner(ownerID); owner.OneWay = enable;
        ResetOneWayOverrides(owner); MarkShapesDirty();
    }

    /// <summary>Tests a group's one-way policy.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <returns>False for unconfigured groups and Area groups.</returns>
    public bool IsShapeOwnerOneWayCollisionEnabled(uint ownerID) => LookupShapeOwner(ownerID).OneWay;

    /// <summary>Sets finite nonnegative one-way recovery margin in scene units; Area setters have no effect.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <param name="margin">Nonnegative finite recovery depth.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public void ShapeOwnerSetOneWayCollisionMargin(uint ownerID, float margin)
    {
        EnsureMutable(); if (this is Area) return; var owner = LookupShapeOwner(ownerID);
        if (!float.IsFinite(margin) || margin < 0) throw new ArgumentOutOfRangeException(nameof(margin));
        owner.Margin = margin; ResetOneWayOverrides(owner); MarkShapesDirty();
    }

    /// <summary>Gets a group's one-way recovery margin.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <returns>Scene units; zero for a new manual group.</returns>
    public float GetShapeOwnerOneWayCollisionMargin(uint ownerID) => LookupShapeOwner(ownerID).Margin;

    /// <summary>Sets the normalized local pass-through direction; Area setters have no effect.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <param name="direction">Finite local vector; zero disables a usable pass-through direction; the group rotation rotates it into body coordinates.</param>
    /// <exception cref="ArgumentException">The vector is nonfinite.</exception>
    public void ShapeOwnerSetOneWayCollisionDirection(uint ownerID, Vector2 direction)
    {
        EnsureMutable(); if (this is Area) return; var owner = LookupShapeOwner(ownerID);
        owner.Direction = CollisionShape.NormalizeOneWayDirection(direction); ResetOneWayOverrides(owner); MarkShapesDirty();
    }

    /// <summary>Gets a group's normalized local pass-through direction.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <returns>Down by default, before group rotation.</returns>
    public Vector2 GetShapeOwnerOneWayCollisionDirection(uint ownerID) => LookupShapeOwner(ownerID).Direction;

    /// <summary>Adds a borrowed shape to an owner and appends one global collision slot.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <param name="shape">A live resource; its owner retains disposal responsibility.</param>
    /// <exception cref="ArgumentNullException">The shape is null.</exception>
    /// <exception cref="ObjectDisposedException">The shape is disposed.</exception>
    public void ShapeOwnerAddShape(uint ownerID, Shape shape)
    {
        EnsureMutable(); var owner = LookupShapeOwner(ownerID); ArgumentNullException.ThrowIfNull(shape);
        if (shape.IsDisposed) throw new ObjectDisposedException(nameof(shape));
        AddOwnerShape(owner, shape);
    }

    /// <summary>Gets the number of shapes in a group, including disabled or disposed resources.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <returns>The logical shape count, independent of compound native fixtures.</returns>
    public int ShapeOwnerGetShapeCount(uint ownerID) => LookupShapeOwner(ownerID).Shapes.Count;

    /// <summary>Gets a group's borrowed shape at a local index.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <param name="shapeIndex">Zero-based group-local index.</param>
    /// <returns>The current borrowed resource identity; server-backed geometry belongs to its server RID.</returns>
    /// <remarks>Raw server replacement updates this slot. A server-owned geometry view expires on data replacement/free
    /// and cannot be disposed by a borrower.</remarks>
    public Shape ShapeOwnerGetShape(uint ownerID, int shapeIndex) => Slot(ownerID, shapeIndex).Shape;

    /// <summary>Gets the global collision index of a group-local shape.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <param name="shapeIndex">Zero-based group-local index.</param>
    /// <returns>An append-order global index; later indices shift when any shape is removed.</returns>
    public int ShapeOwnerGetShapeIndex(uint ownerID, int shapeIndex) => Slot(ownerID, shapeIndex).Index;

    /// <summary>Removes one group-local shape and renumbers later global indices.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    /// <param name="shapeIndex">Zero-based group-local index.</param>
    public void ShapeOwnerRemoveShape(uint ownerID, int shapeIndex)
    {
        EnsureMutable(); var slot = Slot(ownerID, shapeIndex); slot.Owner.Shapes.RemoveAt(shapeIndex);
        _shapeSlots.RemoveAt(slot.Index); Reindex(); MarkShapesDirty();
    }

    /// <summary>Removes all shapes from a group without removing its identity or configuration.</summary>
    /// <param name="ownerID">A current owner ID.</param>
    public void ShapeOwnerClearShapes(uint ownerID) { EnsureMutable(); ClearOwnerShapes(LookupShapeOwner(ownerID)); }

    /// <summary>Finds the group owning a current global collision index.</summary>
    /// <param name="shapeIndex">A current zero-based global index.</param>
    /// <returns>The owner ID of that logical slot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The global index is absent.</exception>
    public uint ShapeFindOwner(int shapeIndex)
    {
        ReadOwners(); if ((uint)shapeIndex >= (uint)_shapeSlots.Count) throw new ArgumentOutOfRangeException(nameof(shapeIndex));
        return _shapeSlots[shapeIndex].Owner.ID;
    }

    internal ShapeSlot GlobalShapeSlot(int index)
    {
        ReadOwners();
        return (uint)index < (uint)_shapeSlots.Count ? _shapeSlots[index] : throw new ArgumentOutOfRangeException(nameof(index));
    }

    internal void AddServerShape(PhysicsServerShape shape, Transform transform, bool disabled)
    {
        ValidateOwnerTransform(transform);
        var id = CreateShapeOwner(null);
        var owner = _shapeOwners[id]; owner.Transform = transform; owner.Disabled = disabled;
        AddOwnerShape(owner, shape.Geometry);
        owner.Shapes[0].ServerShape = shape;
    }

    internal void RemoveGlobalShape(int index)
    {
        var slot = GlobalShapeSlot(index); slot.Owner.Shapes.Remove(slot);
        _shapeSlots.RemoveAt(index); Reindex(); MarkShapesDirty();
    }

    internal void ClearGlobalShapes()
    {
        foreach (var owner in _shapeOwners.Values) owner.Shapes.Clear();
        _shapeSlots.Clear(); MarkShapesDirty();
    }

    internal bool UsesServerShape(PhysicsServerShape shape)
    {
        foreach (var slot in _shapeSlots)
            if (ReferenceEquals(slot.ServerShape, shape) || ReferenceEquals(slot.Shape, shape.Geometry)) return true;
        return false;
    }

    internal void RemoveServerShape(PhysicsServerShape shape)
    {
        for (var index = _shapeSlots.Count - 1; index >= 0; index--)
            if (ReferenceEquals(_shapeSlots[index].ServerShape, shape) || ReferenceEquals(_shapeSlots[index].Shape, shape.Geometry)) RemoveGlobalShape(index);
    }

    private static void ResetOneWayOverrides(ShapeOwner owner)
    {
        foreach (var slot in owner.Shapes) slot.OneWayOverride = null;
    }

    private void ReadOwners() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private ShapeOwner LookupShapeOwner(uint id)
    {
        ReadOwners(); return _shapeOwners.TryGetValue(id, out var owner) ? owner :
            throw new ArgumentOutOfRangeException(nameof(id), "The shape owner ID is absent.");
    }
    private ShapeSlot Slot(uint ownerID, int index)
    {
        var owner = LookupShapeOwner(ownerID);
        return (uint)index < (uint)owner.Shapes.Count ? owner.Shapes[index] : throw new ArgumentOutOfRangeException(nameof(index));
    }
    private void AddOwnerShape(ShapeOwner owner, Shape shape)
    {
        var slot = new ShapeSlot(owner, shape, _shapeSlots.Count);
        owner.Shapes.Add(slot); _shapeSlots.Add(slot); MarkShapesDirty();
    }
    private void ClearOwnerShapes(ShapeOwner owner)
    {
        foreach (var slot in owner.Shapes) _shapeSlots.Remove(slot);
        owner.Shapes.Clear(); Reindex(); MarkShapesDirty();
    }
    private void Reindex() { for (var index = 0; index < _shapeSlots.Count; index++) _shapeSlots[index].Index = index; }
    internal static void ValidateOwnerTransform(Transform transform)
    {
        if (!transform.IsFinite() || !transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(transform.Skew))
            throw new ArgumentException("A collision owner requires finite translation, unit scale and zero skew.", nameof(transform));
    }

    internal void AttachShape(ICollisionGeometry child)
    {
        if (_childOwners.ContainsKey(child)) return;
        var id = CreateShapeOwner(child.Node); _childOwners.Add(child, id);
        var owner = _shapeOwners[id]; owner.Transform = child.Node.Transform; owner.Disabled = child.Disabled;
        if (this is not Area) { owner.OneWay = child.OneWayCollision; owner.Margin = child.OneWayCollisionMargin; owner.Direction = child.OneWayCollisionDirection; }
        foreach (var shape in child.OwnerShapes) AddOwnerShape(owner, shape);
    }
    internal void DetachShape(ICollisionGeometry child)
    {
        if (!_childOwners.Remove(child, out var id)) return;
        if (_shapeOwners.Remove(id, out var owner)) ClearOwnerShapes(owner);
    }
    internal void ReplaceChildShapes(ICollisionGeometry child)
    {
        if (!_childOwners.TryGetValue(child, out var id)) return;
        var owner = _shapeOwners[id]; ClearOwnerShapes(owner);
        foreach (var shape in child.OwnerShapes) AddOwnerShape(owner, shape);
    }
    internal void ChildTransformChanged(ICollisionGeometry child)
    {
        if (!_childOwners.TryGetValue(child, out var id)) return;
        var owner = _shapeOwners[id]; owner.Transform = child.Node.Transform;
        foreach (var slot in owner.Shapes) slot.TransformOverride = null;
        MarkShapesDirty();
    }
    internal uint? ChildOwnerID(ICollisionGeometry child) => _childOwners.TryGetValue(child, out var id) ? id : null;
    internal void ChildDisabledChanged(ICollisionGeometry child)
    { if (ChildOwnerID(child) is { } id) ShapeOwnerSetDisabled(id, child.Disabled); }
    internal void ChildOneWayChanged(ICollisionGeometry child)
    { if (ChildOwnerID(child) is { } id) ShapeOwnerSetOneWayCollision(id, child.OneWayCollision); }
    internal void ChildMarginChanged(ICollisionGeometry child)
    { if (ChildOwnerID(child) is { } id) ShapeOwnerSetOneWayCollisionMargin(id, child.OneWayCollisionMargin); }
    internal void ChildDirectionChanged(ICollisionGeometry child)
    { if (ChildOwnerID(child) is { } id) ShapeOwnerSetOneWayCollisionDirection(id, child.OneWayCollisionDirection); }
    internal void SynchronizeChildOwner(ICollisionGeometry child)
    {
        ChildTransformChanged(child); ChildDisabledChanged(child); ChildOneWayChanged(child);
        ChildMarginChanged(child); ChildDirectionChanged(child);
    }
}
