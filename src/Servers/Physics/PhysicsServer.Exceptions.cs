namespace Electron2D;

public sealed partial class PhysicsServer
{
    private readonly Dictionary<RID, List<RID>> _bodyExceptions = [];
    private readonly Dictionary<RID, List<RID>> _jointBodyExceptions = [];

    internal void BodyAddCollisionExceptionCore(RID body, RID exceptedBody)
    {
        ThrowIfDisposed();
        var space = GetBodySpace(body);
        space?.EnsureQueryAccess();
        lock (_registryGate)
        {
            if (!_bodyExceptions.TryGetValue(body, out var entries))
                _bodyExceptions.Add(body, entries = []);
            if (entries.Contains(exceptedBody)) return;
            entries.Add(exceptedBody);
        }
        InvalidateBodyContacts(body);
    }

    internal void BodyRemoveCollisionExceptionCore(RID body, RID exceptedBody)
    {
        ThrowIfDisposed();
        var space = GetBodySpace(body);
        space?.EnsureQueryAccess();
        lock (_registryGate)
        {
            if (!_bodyExceptions.TryGetValue(body, out var entries) || !entries.Remove(exceptedBody)) return;
            if (entries.Count == 0) _bodyExceptions.Remove(body);
        }
        InvalidateBodyContacts(body);
    }

    internal RID[] GetBodyCollisionExceptions(RID body)
    {
        ThrowIfDisposed();
        var space = GetBodySpace(body);
        space?.EnsureQueryAccess();
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
            if (entries.Count == 0) _jointBodyExceptions.Remove(body);
        }
        void TryInvalidate(RID body)
        {
            try { InvalidateBodyContacts(body); }
            catch (ArgumentException) { }
        }
    }

    private PhysicsSpace? GetBodySpace(RID body)
    {
        if (ResolveSceneObject(body) is PhysicsBody sceneBody)
            return sceneBody.Space;
        return GetCollider(body, isArea: false).Space;
    }

    private void InvalidateBodyContacts(RID body)
    {
        if (ResolveSceneObject(body) is PhysicsBody sceneBody)
        {
            sceneBody.Space?.InvalidateGPUExceptions();
            if (sceneBody.Space?.GPUStore is not null) return;
            sceneBody.MarkShapesDirty();
            return;
        }
        var collider = GetCollider(body, isArea: false);
        collider.Space?.InvalidateGPUExceptions();
        if (collider.Space?.GPUStore is null) collider.MarkShapesDirty();
    }

    internal void ObserveGPUExceptions(PhysicsSpace space)
    {
        lock (_registryGate)
        {
            foreach (var pair in _bodyExceptions) foreach (var target in pair.Value) space.ObserveGPUException(pair.Key, target);
        }
    }
}
