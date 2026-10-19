namespace Electron2D;

public sealed partial class PhysicsServer
{
    // Empty lists retain warmed storage until their body owner retires.
    private readonly Dictionary<RID, List<RID>> _bodyExceptions = [];
    private readonly Dictionary<RID, List<RID>> _jointBodyExceptions = [];

    internal void BodyAddCollisionExceptionCore(RID body, RID exceptedBody)
    {
        ThrowIfDisposed();
        var runtime = BodyRuntime(body); runtime.EnsureAccess(writing: true);
        bool changed;
        lock (_registryGate)
        {
            if (!_bodyExceptions.TryGetValue(body, out var entries))
                _bodyExceptions.Add(body, entries = []);
            changed = !entries.Contains(exceptedBody);
            if (changed) entries.Add(exceptedBody);
        }
        if (changed) InvalidateBodyContacts(body); else runtime.Wake();
    }

    internal void BodyRemoveCollisionExceptionCore(RID body, RID exceptedBody)
    {
        ThrowIfDisposed();
        var runtime = BodyRuntime(body); runtime.EnsureAccess(writing: true);
        bool changed;
        lock (_registryGate) changed = _bodyExceptions.TryGetValue(body, out var entries) && entries.Remove(exceptedBody);
        if (changed) InvalidateBodyContacts(body); else runtime.Wake();
    }

    internal void CopyReplayExceptions(RID body, List<RID> destination)
    {
        lock (_registryGate) { destination.Clear(); if (_bodyExceptions.TryGetValue(body, out var source)) { destination.EnsureCapacity(source.Count); foreach (var item in source) destination.Add(item); } }
    }
    internal bool ReplayExceptionsMatch(RID body, List<RID> saved)
    {
        lock (_registryGate) { if (!_bodyExceptions.TryGetValue(body, out var source)) return saved.Count == 0; return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(source).SequenceEqual(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(saved)); }
    }

    internal RID[] GetBodyCollisionExceptions(RID body)
    {
        ThrowIfDisposed();
        BodyRuntime(body).EnsureAccess();
        return CopyBodyCollisionExceptions(body);
    }

    internal RID[] GetBodyCollisionExceptions(RID body, PhysicsSpace context)
    {
        ThrowIfDisposed(); context.EnsureImplementationInputReadAccess();
        var runtime = BodyRuntime(body);
        if (runtime.Owners.Scene?.Tree is { } tree) context.EnsureSceneInputOwner(tree);
        if (runtime.Space is { } attached && !ReferenceEquals(attached, context))
            throw new ArgumentException("The body belongs to another physics world.", nameof(body));
        return CopyBodyCollisionExceptions(body);
    }

    private RID[] CopyBodyCollisionExceptions(RID body)
    {
        lock (_registryGate)
        {
            var result = _bodyExceptions.TryGetValue(body, out var entries) ? new List<RID>(entries) : [];
            if (_jointBodyExceptions.TryGetValue(body, out var joints))
                foreach (var target in joints)
                    if (!result.Contains(target)) result.Add(target);
            return result.ToArray();
        }
    }

    internal bool HasBodyCollisionExceptions(RID body)
    {
        lock (_registryGate)
            return _bodyExceptions.TryGetValue(body, out var entries) && entries.Count != 0 ||
                _jointBodyExceptions.TryGetValue(body, out var joints) && joints.Count != 0;
    }

    internal bool BodiesExcepted(RID first, RID second)
    {
        // ponytail: A small per-body list and registry lock suffice until large-world contact profiling says otherwise.
        lock (_registryGate)
            return _bodyExceptions.TryGetValue(first, out var firstEntries) && firstEntries.Contains(second) ||
                _bodyExceptions.TryGetValue(second, out var secondEntries) && secondEntries.Contains(first) ||
                _jointBodyExceptions.TryGetValue(first, out var joints) && joints.Contains(second);
    }

    internal void JointCollisionContribution(RID first, RID second, bool add)
    {
        lock (_registryGate)
        {
            Update(first, second); Update(second, first);
        }
        TryInvalidate(first); TryInvalidate(second);

        void Update(RID body, RID target)
        {
            if (!_jointBodyExceptions.TryGetValue(body, out var entries))
            {
                if (!add) return;
                _jointBodyExceptions.Add(body, entries = []);
            }
            // Repeated entries retain one contribution per joint, independent of explicit exceptions.
            if (add) entries.Add(target); else entries.Remove(target);
        }
        void TryInvalidate(RID body)
        {
            try { InvalidateBodyContacts(body); }
            catch (ArgumentException) { }
        }
    }

    private void InvalidateBodyContacts(RID body)
    {
        var runtime = BodyRuntime(body); var owners = runtime.Owners;
        if (runtime.Space?.HasBackendFailure == true) return;
        if (runtime.Space is null)
        {
            if (owners.Scene is { } scene) scene.MarkShapesDirty(); else owners.Server!.MarkShapesDirty();
        }
        else
        {
            runtime.Backend.InvalidateCollisionExceptions(); runtime.Backend.WakeTouching();
        }
        runtime.Wake();
    }

    internal void CopyCollisionExceptions(List<(RID From, RID To)> destination, bool includeJoints = false)
    {
        lock (_registryGate)
        {
            destination.Clear();
            foreach (var pair in _bodyExceptions) foreach (var target in pair.Value) destination.Add((pair.Key, target));
            if (includeJoints)
                foreach (var pair in _jointBodyExceptions) foreach (var target in pair.Value) destination.Add((pair.Key, target));
        }
    }
}
