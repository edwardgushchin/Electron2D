namespace Electron2D;

public sealed partial class PhysicsServer2D
{
    private readonly Dictionary<RID, List<RID>> _bodyExceptions = [];

    /// <summary>Excludes two bodies from ordinary contact and motion tests when either body lists the other.</summary>
    /// <param name="body">The live scene or server body that owns the exception entry.</param>
    /// <param name="exceptedBody">Any RID value; an unresolvable target remains inert.</param>
    /// <exception cref="ArgumentException">The owner RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">The attached body is off-owner or its world is stepping.</exception>
    public void BodyAddCollisionException(RID body, RID exceptedBody)
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

    /// <summary>Removes one body-owned collision exception entry.</summary>
    /// <param name="body">The live scene or server body that owns the entry.</param>
    /// <param name="exceptedBody">The RID to remove; an absent entry is a no-op.</param>
    /// <exception cref="ArgumentException">The owner RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">The attached body is off-owner or its world is stepping.</exception>
    public void BodyRemoveCollisionException(RID body, RID exceptedBody)
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
            return _bodyExceptions.TryGetValue(body, out var entries) ? entries.ToArray() : [];
    }

    internal bool HasBodyCollisionExceptions(RID body)
    {
        lock (_registryGate)
            return _bodyExceptions.TryGetValue(body, out var entries) && entries.Count != 0;
    }

    internal bool BodiesExcepted(RID first, RID second)
    {
        // ponytail: A small per-body list and registry lock suffice until large-world contact profiling says otherwise.
        lock (_registryGate)
            return _bodyExceptions.TryGetValue(first, out var firstEntries) && firstEntries.Contains(second) ||
                _bodyExceptions.TryGetValue(second, out var secondEntries) && secondEntries.Contains(first);
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
            sceneBody.MarkShapesDirty();
            return;
        }
        GetCollider(body, isArea: false).MarkShapesDirty();
    }
}
