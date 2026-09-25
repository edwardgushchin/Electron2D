namespace Electron2D;

public sealed partial class PhysicsServer2D
{
    /// <summary>Creates a physics space independent of any scene tree.</summary>
    /// <returns>A caller-owned space RID that can be queried, stepped and freed.</returns>
    public RID SpaceCreate()
    {
        ThrowIfDisposed();
        var space = new PhysicsSpace();
        var rid = RID.Allocate();
        lock (_registryGate)
        {
            _sceneSpaces.Add(rid, space);
            _ownedSpaces.Add(rid);
        }
        return rid;
    }

    /// <summary>Advances one explicitly created space by a finite nonnegative fixed delta.</summary>
    /// <param name="space">A caller-owned space RID.</param>
    /// <param name="delta">Elapsed seconds; zero leaves the world unchanged.</param>
    /// <exception cref="ArgumentOutOfRangeException">Delta is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The caller is off the space owner thread or the world is stepping.</exception>
    public void SpaceStep(RID space, double delta)
    {
        ThrowIfDisposed();
        if (!double.IsFinite(delta) || delta < 0 || delta > float.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(delta));
        lock (_registryGate)
            if (!_ownedSpaces.Contains(space)) throw new ArgumentException("The RID is not an explicitly created space.", nameof(space));
        var world = GetSceneSpace(space);
        world.EnsureQueryAccess();
        world.Step(delta);
    }

    /// <summary>Creates a detached rigid body with layer and mask one.</summary>
    /// <returns>A caller-owned body RID.</returns>
    public RID BodyCreate() => CreateCollider(isArea: false);

    /// <summary>Creates a detached sensor Area with layer and mask one.</summary>
    /// <returns>A caller-owned Area RID.</returns>
    public RID AreaCreate() => CreateCollider(isArea: true);

    /// <summary>Creates a caller-owned circle shape with its default geometry.</summary>
    /// <returns>A live circle-shape RID.</returns>
    public RID CircleShapeCreate() => CreateShape(new CircleShape());
    /// <summary>Creates a caller-owned rectangle shape with its default geometry.</summary>
    /// <returns>A live rectangle-shape RID.</returns>
    public RID RectangleShapeCreate() => CreateShape(new RectangleShape());
    /// <summary>Creates a caller-owned capsule shape with its default geometry.</summary>
    /// <returns>A live capsule-shape RID.</returns>
    public RID CapsuleShapeCreate() => CreateShape(new CapsuleShape());
    /// <summary>Creates a caller-owned segment shape with its default geometry.</summary>
    /// <returns>A live segment-shape RID.</returns>
    public RID SegmentShapeCreate() => CreateShape(new SegmentShape());
    /// <summary>Creates a caller-owned empty convex polygon shape.</summary>
    /// <returns>A live convex-polygon-shape RID.</returns>
    public RID ConvexPolygonShapeCreate() => CreateShape(new ConvexPolygonShape());
    /// <summary>Creates a caller-owned empty paired-segment shape.</summary>
    /// <returns>A live concave-polygon-shape RID.</returns>
    public RID ConcavePolygonShapeCreate() => CreateShape(new ConcavePolygonShape());

    /// <summary>Copies typed geometry into a server-owned shape RID.</summary>
    /// <param name="shape">A live server shape of the same concrete type as the supplied resource.</param>
    /// <param name="data">Caller-owned source geometry; later edits do not affect the server copy.</param>
    /// <exception cref="ArgumentException">The RID is not a live shape or the resource has another concrete type.</exception>
    /// <exception cref="ObjectDisposedException">The supplied resource is disposed.</exception>
    public void ShapeSetData(RID shape, Shape data)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(data);
        if (data.IsDisposed) throw new ObjectDisposedException(nameof(data));
        var entry = GetShape(shape);
        if (entry.Geometry.GetType() != data.GetType())
            throw new ArgumentException("Shape data must have the server shape's concrete type.", nameof(data));
        var users = new List<PhysicsServerCollider>();
        foreach (var collider in SnapshotColliders())
            if (collider.UsesShape(entry))
            {
                EnsureColliderSpaceAccessible(collider);
                users.Add(collider);
            }
        var copy = (Shape)data.Duplicate();
        var previous = entry.Geometry;
        entry.Geometry = copy;
        try
        {
            foreach (var collider in users) collider.RebuildShapes();
        }
        catch
        {
            entry.Geometry = previous;
            foreach (var collider in users) collider.RebuildShapes();
            copy.Dispose();
            throw;
        }
        previous.Dispose();
    }

    /// <summary>Returns a caller-owned duplicate of server shape geometry.</summary>
    /// <param name="shape">A live server shape RID.</param>
    /// <returns>An independent caller-owned Shape resource.</returns>
    public Shape ShapeGetData(RID shape)
    {
        ThrowIfDisposed();
        var entry = GetShape(shape);
        foreach (var collider in SnapshotColliders())
            if (collider.UsesShape(entry)) EnsureColliderSpaceAccessible(collider);
        return (Shape)entry.Geometry.Duplicate();
    }

    /// <summary>Adds a typed server shape to a body as one indexed owner slot.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="shape">A live server shape RID.</param>
    /// <param name="transform">Finite local pose, or null for identity.</param>
    /// <param name="disabled">Whether this slot initially contributes no fixtures.</param>
    public void BodyAddShape(RID body, RID shape, Transform? transform = null, bool disabled = false) =>
        AddShape(body, shape, transform ?? Transform.Identity, disabled, isArea: false);

    /// <summary>Adds a typed server shape to an Area sensor as one indexed owner slot.</summary>
    /// <param name="area">A live server Area RID.</param>
    /// <param name="shape">A live server shape RID.</param>
    /// <param name="transform">Finite local pose, or null for identity.</param>
    /// <param name="disabled">Whether this slot initially contributes no fixtures.</param>
    public void AreaAddShape(RID area, RID shape, Transform? transform = null, bool disabled = false) =>
        AddShape(area, shape, transform ?? Transform.Identity, disabled, isArea: true);

    /// <summary>Gets the number of indexed shape slots on a body, including disabled slots.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <returns>The current slot count.</returns>
    public int BodyGetShapeCount(RID body)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        return collider.ShapeCount;
    }

    /// <summary>Gets the number of indexed shape slots on an Area, including disabled slots.</summary>
    /// <param name="area">A live server Area RID.</param>
    /// <returns>The current slot count.</returns>
    public int AreaGetShapeCount(RID area)
    {
        var collider = GetCollider(area, isArea: true);
        EnsureColliderSpaceAccessible(collider);
        return collider.ShapeCount;
    }

    /// <summary>Enables or disables one indexed body shape slot.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="index">Zero-based shape-owner slot index.</param>
    /// <param name="disabled">Whether the slot contributes no fixtures.</param>
    public void BodySetShapeDisabled(RID body, int index, bool disabled)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        collider.SetShapeDisabled(index, disabled);
    }

    /// <summary>Enables or disables one indexed Area shape slot.</summary>
    /// <param name="area">A live server Area RID.</param>
    /// <param name="index">Zero-based shape-owner slot index.</param>
    /// <param name="disabled">Whether the slot contributes no sensor fixtures.</param>
    public void AreaSetShapeDisabled(RID area, int index, bool disabled)
    {
        var collider = GetCollider(area, isArea: true);
        EnsureColliderSpaceAccessible(collider);
        collider.SetShapeDisabled(index, disabled);
    }

    /// <summary>Removes one indexed body shape slot and its fixtures.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="index">Zero-based shape-owner slot index.</param>
    public void BodyRemoveShape(RID body, int index)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        collider.RemoveShapeAt(index);
    }

    /// <summary>Removes one indexed Area shape slot and its fixtures.</summary>
    /// <param name="area">A live server Area RID.</param>
    /// <param name="index">Zero-based shape-owner slot index.</param>
    public void AreaRemoveShape(RID area, int index)
    {
        var collider = GetCollider(area, isArea: true);
        EnsureColliderSpaceAccessible(collider);
        collider.RemoveShapeAt(index);
    }

    /// <summary>Moves a body into a live space, or detaches it with an empty RID.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="space">A live space RID, or default to detach.</param>
    public void BodySetSpace(RID body, RID space) => SetSpace(body, space, isArea: false);
    /// <summary>Moves an Area into a live space, or detaches it with an empty RID.</summary>
    /// <param name="area">A live server Area RID.</param>
    /// <param name="space">A live space RID, or default to detach.</param>
    public void AreaSetSpace(RID area, RID space) => SetSpace(area, space, isArea: true);

    /// <summary>Gets the current body space, or an empty RID while detached.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <returns>The current space identity or default.</returns>
    public RID BodyGetSpace(RID body)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        return collider.SpaceRID;
    }
    /// <summary>Gets the current Area space, or an empty RID while detached.</summary>
    /// <param name="area">A live server Area RID.</param>
    /// <returns>The current space identity or default.</returns>
    public RID AreaGetSpace(RID area)
    {
        var collider = GetCollider(area, isArea: true);
        EnsureColliderSpaceAccessible(collider);
        return collider.SpaceRID;
    }

    /// <summary>Changes a body's translation and rotation in scene units.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="transform">Finite global pose with unit scale and zero skew.</param>
    public void BodySetTransform(RID body, Transform transform)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        collider.SetTransform(transform);
    }

    /// <summary>Changes an Area's translation and rotation in scene units.</summary>
    /// <param name="area">A live server Area RID.</param>
    /// <param name="transform">Finite global pose with unit scale and zero skew.</param>
    public void AreaSetTransform(RID area, Transform transform)
    {
        var collider = GetCollider(area, isArea: true);
        EnsureColliderSpaceAccessible(collider);
        collider.SetTransform(transform);
    }

    /// <summary>Returns the body's current solver transform.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <returns>The current scene-unit pose, including solved dynamic movement.</returns>
    public Transform BodyGetTransform(RID body)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        return collider.GetTransform();
    }

    /// <summary>Sets the body's finite linear velocity in scene units per second.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="velocity">Finite global scene units per second.</param>
    public void BodySetLinearVelocity(RID body, Vector2 velocity)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        collider.SetLinearVelocity(velocity);
    }

    /// <summary>Changes a body among static, kinematic and dynamic modes.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="mode">One of the four declared body modes.</param>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    public void BodySetMode(RID body, BodyMode mode)
    {
        if (mode is not (BodyMode.Static or BodyMode.Kinematic or BodyMode.Rigid or BodyMode.RigidLinear))
            throw new ArgumentOutOfRangeException(nameof(mode));
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        collider.SetMode(mode);
    }

    /// <summary>Gets the body's current motion mode.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <returns>The current mode.</returns>
    public BodyMode BodyGetMode(RID body)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        return collider.Mode;
    }

    /// <summary>Sets a body's 32 collision-layer bits.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="layer">All accepted layer bits, including zero and bit 32.</param>
    public void BodySetCollisionLayer(RID body, uint layer)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        collider.SetFilter(layer, collider.CollisionMask);
    }

    /// <summary>Sets a body's 32 collision-mask bits.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="mask">All accepted mask bits, including zero and bit 32.</param>
    public void BodySetCollisionMask(RID body, uint mask)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        collider.SetFilter(collider.CollisionLayer, mask);
    }

    /// <summary>Sets an Area's 32 collision-layer bits.</summary>
    /// <param name="area">A live server Area RID.</param>
    /// <param name="layer">All accepted layer bits, including zero and bit 32.</param>
    public void AreaSetCollisionLayer(RID area, uint layer)
    {
        var collider = GetCollider(area, isArea: true);
        EnsureColliderSpaceAccessible(collider);
        collider.SetFilter(layer, collider.CollisionMask);
    }

    /// <summary>Frees a server-owned space, collider or shape RID.</summary>
    /// <param name="rid">A live caller-owned server resource identity.</param>
    /// <remarks>SceneTree-owned spaces and CollisionObject RIDs are released by their scene owners.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or has no server-owned resource.</exception>
    /// <exception cref="InvalidOperationException">The RID belongs to a scene owner or the space is being stepped.</exception>
    public void FreeRID(RID rid)
    {
        ThrowIfDisposed();
        PhysicsSpace? space = null;
        PhysicsServerCollider? collider = null;
        PhysicsServerShape? shape = null;
        lock (_registryGate)
        {
            if (_ownedSpaces.Contains(rid)) space = _sceneSpaces[rid];
            else if (_serverColliders.TryGetValue(rid, out collider)) { }
            else if (_serverShapes.TryGetValue(rid, out shape)) { }
            else if (_sceneSpaces.ContainsKey(rid) || _sceneObjects.ContainsKey(rid))
                throw new InvalidOperationException("The RID is owned by a scene tree or collision node.");
            else throw new ArgumentException("The RID is not a live server resource.", nameof(rid));
        }
        if (space is not null)
        {
            space.EnsureQueryAccess();
            space.Dispose();
            lock (_registryGate)
            {
                _ownedSpaces.Remove(rid);
                _sceneSpaces.Remove(rid);
                _directStates.Remove(rid);
            }
        }
        else if (collider is not null)
        {
            EnsureColliderSpaceAccessible(collider);
            if (collider.SpaceRID.IsValid()) GetSceneSpace(collider.SpaceRID).Remove(collider);
            lock (_registryGate) _serverColliders.Remove(rid);
        }
        else if (shape is not null)
        {
            var users = SnapshotColliders();
            foreach (var user in users)
                if (user.UsesShape(shape)) EnsureColliderSpaceAccessible(user);
            foreach (var user in users)
            {
                user.RemoveShape(shape);
            }
            shape.Geometry.Dispose();
            lock (_registryGate) _serverShapes.Remove(rid);
        }
    }

    private RID CreateCollider(bool isArea)
    {
        ThrowIfDisposed();
        var rid = RID.Allocate();
        lock (_registryGate) _serverColliders.Add(rid, new(rid, isArea));
        return rid;
    }

    private RID CreateShape(Shape geometry)
    {
        ThrowIfDisposed();
        var rid = RID.Allocate();
        lock (_registryGate) _serverShapes.Add(rid, new(rid, geometry));
        return rid;
    }

    private PhysicsServerShape GetShape(RID rid)
    {
        ThrowIfDisposed();
        lock (_registryGate)
            return _serverShapes.TryGetValue(rid, out var shape) ? shape :
                throw new ArgumentException("The RID is not a live server shape.", nameof(rid));
    }

    private PhysicsServerCollider GetCollider(RID rid, bool isArea)
    {
        ThrowIfDisposed();
        lock (_registryGate)
            return _serverColliders.TryGetValue(rid, out var collider) && collider.IsArea == isArea ? collider :
                throw new ArgumentException("The RID has the wrong collider kind or is not live.", nameof(rid));
    }

    private PhysicsServerCollider[] SnapshotColliders()
    {
        lock (_registryGate) return _serverColliders.Values.ToArray();
    }

    private void AddShape(RID owner, RID shape, Transform transform, bool disabled, bool isArea)
    {
        var collider = GetCollider(owner, isArea);
        var resource = GetShape(shape);
        EnsureColliderSpaceAccessible(collider);
        collider.AddShape(resource, transform, disabled);
    }

    private void SetSpace(RID owner, RID spaceRID, bool isArea)
    {
        var collider = GetCollider(owner, isArea);
        if (collider.SpaceRID == spaceRID) return;
        PhysicsSpace? next = spaceRID.IsValid() ? GetSceneSpace(spaceRID) : null;
        next?.EnsureQueryAccess();
        var oldRID = collider.SpaceRID;
        PhysicsSpace? old = oldRID.IsValid() ? GetSceneSpace(oldRID) : null;
        old?.EnsureQueryAccess();
        old?.Remove(collider);
        try { next?.Add(collider, spaceRID); }
        catch { old?.Add(collider, oldRID); throw; }
    }

    private void EnsureColliderSpaceAccessible(PhysicsServerCollider collider)
    {
        if (collider.SpaceRID.IsValid()) GetSceneSpace(collider.SpaceRID).EnsureQueryAccess();
    }
}
