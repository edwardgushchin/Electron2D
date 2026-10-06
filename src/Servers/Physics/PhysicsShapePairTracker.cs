namespace Electron2D;

internal readonly record struct PhysicsShapePair(RID RID, CollisionObject? Other, bool IsArea,
    int OtherShape, int LocalShape)
{
    internal ulong InstanceID => Other?.InstanceID ?? 0;
}
internal readonly record struct PhysicsShapePairChange(PhysicsShapePair Pair, bool Entered, bool ObjectEvent);

internal sealed class PhysicsShapePairTracker
{
    private HashSet<PhysicsShapePair> _current = [];
    private HashSet<PhysicsShapePair> _next = [];
    private readonly Dictionary<RID, int> _counts = [];
    private readonly List<PhysicsShapePair> _removed = [];

    internal void Prepare(int capacity)
    {
        _current.EnsureCapacity(capacity); _next.EnsureCapacity(capacity);
        _counts.EnsureCapacity(checked(capacity * 2)); _removed.EnsureCapacity(capacity);
    }

    internal void Begin() => _next.Clear();
    internal void Observe(PhysicsShapePair pair) => _next.Add(pair);
    internal bool Contains(PhysicsShapePair pair) => _current.Contains(pair);
    internal HashSet<PhysicsShapePair> Current => _current;

    internal void Commit(List<PhysicsShapePairChange> changes)
    {
        _counts.Clear();
        foreach (var pair in _current)
            _counts[pair.RID] = _counts.GetValueOrDefault(pair.RID) + 1;
        foreach (var pair in _current)
        {
            if (_next.Contains(pair)) continue;
            var remaining = --_counts[pair.RID];
            if (remaining == 0 && pair.Other is not null) changes.Add(new(pair, false, true));
            changes.Add(new(pair, false, false));
        }
        foreach (var pair in _next)
        {
            if (_current.Contains(pair)) continue;
            var count = _counts.GetValueOrDefault(pair.RID);
            if (count == 0 && pair.Other is not null) changes.Add(new(pair, true, true));
            _counts[pair.RID] = count + 1;
            changes.Add(new(pair, true, false));
        }
        (_current, _next) = (_next, _current);
    }

    internal void Forget(RID rid, List<PhysicsShapePairChange> changes)
    {
        _removed.Clear();
        foreach (var pair in _current) if (pair.RID == rid) _removed.Add(pair);
        for (var index = 0; index < _removed.Count; index++)
        {
            var pair = _removed[index];
            _current.Remove(pair); _next.Remove(pair);
            // Scene-tree departure reports the object exit before its retained shape exits.
            if (index == 0 && pair.Other is not null) changes.Add(new(pair, false, true));
            changes.Add(new(pair, false, false));
        }
        _removed.Clear();
    }

    internal void Clear() { _current.Clear(); _next.Clear(); _counts.Clear(); _removed.Clear(); }
}
