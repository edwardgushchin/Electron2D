namespace Electron2D;

internal readonly record struct PhysicsShapePair(RID RID, ObjectIdentity Identity, bool IsArea,
    int OtherShape, int LocalShape)
{
    internal bool InTree { get; init; } = Identity.Target is Node { IsInsideTree: true };
    internal ElectronObject? Other => Identity.Target;
    internal ulong InstanceID => Identity.ID;
    internal bool EmitsShape => InTree || Identity.RawTarget is not Node;
    public bool Equals(PhysicsShapePair other) => RID == other.RID && InstanceID == other.InstanceID &&
        IsArea == other.IsArea && OtherShape == other.OtherShape && LocalShape == other.LocalShape;
    public override int GetHashCode() => HashCode.Combine(RID, InstanceID, IsArea, OtherShape, LocalShape);
}
internal readonly record struct PhysicsShapePairChange(PhysicsShapePair Pair, bool Entered, bool ObjectEvent)
{
    internal Node? Node { get; } = Pair.Identity.RawTarget as Node;
}

internal sealed class PhysicsShapePairTracker
{
    private HashSet<PhysicsShapePair> _current = [];
    private HashSet<PhysicsShapePair> _next = [];
    private readonly Dictionary<(ulong, bool), int> _counts = [];
    private readonly List<PhysicsShapePair> _removed = [];

    internal void Prepare(int capacity)
    {
        _current.EnsureCapacity(capacity); _next.EnsureCapacity(capacity);
        _counts.EnsureCapacity(checked(capacity * 2)); _removed.EnsureCapacity(capacity);
    }

    internal void Begin() => _next.Clear();
    internal void Observe(PhysicsShapePair pair) => _next.Add(pair);
    internal bool Contains(PhysicsShapePair pair) => _current.TryGetValue(pair, out var current) && current.InTree == pair.InTree;
    internal HashSet<PhysicsShapePair> Current => _current;

    internal T[] Objects<T>(bool area) where T : Node
    {
        var objects = new List<T>();
        foreach (var pair in _current)
            if (pair.IsArea == area && pair.Other is T node && !objects.Contains(node)) objects.Add(node);
        return objects.ToArray();
    }
    internal bool HasObjects(bool area)
    {
        foreach (var pair in _current) if (pair.IsArea == area && pair.InstanceID != 0) return true;
        return false;
    }
    internal bool ContainsObject(Node? node, bool area)
    {
        if (node is null || node.IsDisposed) return false;
        foreach (var pair in _current)
            if (pair.IsArea == area && pair.InstanceID == node.InstanceID && pair.InTree) return true;
        return false;
    }
    private void CountObjects()
    {
        _counts.Clear();
        foreach (var pair in _current)
            if (pair.InTree && pair.InstanceID != 0)
            { var key = (pair.InstanceID, pair.IsArea); _counts[key] = _counts.GetValueOrDefault(key) + 1; }
    }
    private void RemoveEvents(PhysicsShapePair pair, List<PhysicsShapePairChange> changes)
    {
        if (pair.InTree && pair.InstanceID != 0 && --_counts[(pair.InstanceID, pair.IsArea)] == 0)
            changes.Add(new(pair, false, true));
        changes.Add(new(pair, false, false));
    }

    internal void Commit(List<PhysicsShapePairChange> changes)
    {
        if (_next.SetEquals(_current)) { (_current, _next) = (_next, _current); return; }
        CountObjects();
        foreach (var pair in _current)
            if (!_next.Contains(pair)) RemoveEvents(pair, changes);
        foreach (var pair in _next)
        {
            if (_current.Contains(pair)) continue;
            if (pair.InTree && pair.InstanceID != 0)
            {
                var key = (pair.InstanceID, pair.IsArea); var count = _counts.GetValueOrDefault(key);
                if (count == 0) changes.Add(new(pair, true, true));
                _counts[key] = count + 1;
            }
            changes.Add(new(pair, true, false));
        }
        (_current, _next) = (_next, _current);
    }

    internal void Forget(RID rid, List<PhysicsShapePairChange> changes)
    {
        _removed.Clear(); CountObjects();
        foreach (var pair in _current) if (pair.RID == rid) _removed.Add(pair);
        // Object departure precedes its retained shape exits.
        foreach (var pair in _removed)
        {
            _current.Remove(pair); _next.Remove(pair);
            if (pair.InTree && pair.InstanceID != 0 && --_counts[(pair.InstanceID, pair.IsArea)] == 0)
                changes.Add(new(pair, false, true));
        }
        foreach (var pair in _removed) changes.Add(new(pair, false, false));
        _removed.Clear();
    }

    internal void ObjectTreeChanged(Node node, bool entering, List<PhysicsShapePairChange> changes)
    {
        _removed.Clear();
        foreach (var pair in _current)
            if (pair.InstanceID == node.InstanceID && pair.InTree != entering) _removed.Add(pair);
        var bodyEvent = false; var areaEvent = false;
        foreach (var pair in _removed)
        {
            var next = pair with { InTree = entering };
            _current.Remove(pair); _current.Add(next);
            ref var sent = ref (pair.IsArea ? ref areaEvent : ref bodyEvent);
            if (!sent) { changes.Add(new(entering ? next : pair, entering, true)); sent = true; }
            changes.Add(new(entering ? next : pair, entering, false));
        }
        _removed.Clear();
    }

    internal void Clear() { _current.Clear(); _next.Clear(); _counts.Clear(); _removed.Clear(); }
}
