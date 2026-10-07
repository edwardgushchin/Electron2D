namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal RID SpaceCreateCore()
    {
        ThrowIfDisposed();
        var space = new PhysicsSpace();
        var rid = RID.Allocate();
        space.RID = rid;
        lock (_registryGate)
        {
            _sceneSpaces.Add(rid, space);
            _ownedSpaces.Add(rid);
        }
        return rid;
    }

    internal void SpaceStepCore(RID space, double delta)
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

    internal RID BodyCreateCore() => CreateCollider(isArea: false);

    internal RID AreaCreateCore() => CreateCollider(isArea: true);

    internal RID CircleShapeCreateCore() => CreateShape(new CircleShape());
    internal RID RectangleShapeCreateCore() => CreateShape(new RectangleShape());
    internal RID CapsuleShapeCreateCore() => CreateShape(new CapsuleShape());
    internal RID SegmentShapeCreateCore() => CreateShape(new SegmentShape());
    internal RID SeparationRayShapeCreateCore() => CreateShape(new SeparationRayShape());
    internal RID ConvexPolygonShapeCreateCore() => CreateShape(new ConvexPolygonShape());
    internal RID ConcavePolygonShapeCreateCore() => CreateShape(new ConcavePolygonShape());

    internal void ShapeSetDataCore(RID shape, Shape data)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(data);
        if (data.IsDisposed) throw new ObjectDisposedException(nameof(data));
        var entry = GetShape(shape);
        if (!entry.OwnsGeometry)
            throw new InvalidOperationException("A Shape resource owns this RID and must be edited through the resource.");
        if (entry.Geometry.GetType() != data.GetType())
            throw new ArgumentException("Shape data must have the server shape's concrete type.", nameof(data));
        var users = new List<PhysicsServerCollider>();
        foreach (var collider in SnapshotColliders())
            if (collider.UsesShape(entry))
            {
                EnsureColliderSpaceAccessible(collider);
                users.Add(collider);
            }
        var sceneUsers = SceneShapeUsers(entry);
        foreach (var scene in sceneUsers) EnsureSceneShapeAccess(scene, writing: true);
        var copy = (Shape)data.Duplicate();
        var previous = entry.Geometry;
        copy.BindServerOwnedRID(shape);
        entry.Geometry = copy;
        try
        {
            foreach (var collider in users) collider.RebuildShapes();
            foreach (var scene in sceneUsers) scene.MarkShapesDirty();
        }
        catch
        {
            entry.Geometry = previous;
            foreach (var collider in users) collider.RebuildShapes();
            copy.ReleaseServerGeometry();
            throw;
        }
        previous.ReleaseServerGeometry();
    }

    internal Shape ShapeGetDataCore(RID shape)
    {
        ThrowIfDisposed();
        var entry = GetShape(shape);
        foreach (var collider in SnapshotColliders())
            if (collider.UsesShape(entry)) EnsureColliderSpaceAccessible(collider);
        foreach (var scene in SceneShapeUsers(entry)) EnsureSceneShapeAccess(scene);
        return (Shape)entry.Geometry.Duplicate();
    }

    internal void BodyAddShapeCore(RID body, RID shape, Transform? transform = null, bool disabled = false) =>
    AddShape(body, shape, transform ?? Transform.Identity, disabled, isArea: false);

    internal void AreaAddShapeCore(RID area, RID shape, Transform? transform = null, bool disabled = false) =>
    AddShape(area, shape, transform ?? Transform.Identity, disabled, isArea: true);

    internal int BodyGetShapeCountCore(RID body)
    {
        var owners = ShapeOwners(body, isArea: false);
        return owners.Scene?.ShapeSlots.Count ?? owners.Server!.ShapeCount;
    }

    internal int AreaGetShapeCountCore(RID area)
    {
        var owners = ShapeOwners(area, isArea: true);
        return owners.Scene?.ShapeSlots.Count ?? owners.Server!.ShapeCount;
    }

    internal void BodySetShapeDisabledCore(RID body, int index, bool disabled)
    {
        var owners = ShapeOwners(body, isArea: false, writing: true);
        if (owners.Scene is { } scene)
        {
            var slot = scene.GlobalShapeSlot(index);
            if ((slot.DisabledOverride ?? slot.Owner.Disabled) == disabled) return;
            slot.DisabledOverride = disabled; scene.MarkShapesDirty();
        }
        else owners.Server!.SetShapeDisabled(index, disabled);
    }

    internal void AreaSetShapeDisabledCore(RID area, int index, bool disabled)
    {
        var owners = ShapeOwners(area, isArea: true, writing: true);
        if (owners.Scene is { } scene)
        {
            var slot = scene.GlobalShapeSlot(index);
            if ((slot.DisabledOverride ?? slot.Owner.Disabled) == disabled) return;
            slot.DisabledOverride = disabled; scene.MarkShapesDirty();
        }
        else owners.Server!.SetShapeDisabled(index, disabled);
    }

    internal void BodyRemoveShapeCore(RID body, int index)
    {
        var owners = ShapeOwners(body, isArea: false, writing: true);
        if (owners.Scene is { } scene) scene.RemoveGlobalShape(index); else owners.Server!.RemoveShapeAt(index);
    }

    internal void AreaRemoveShapeCore(RID area, int index)
    {
        var owners = ShapeOwners(area, isArea: true, writing: true);
        if (owners.Scene is { } scene) scene.RemoveGlobalShape(index); else owners.Server!.RemoveShapeAt(index);
    }

    internal void BodySetSpaceCore(RID body, RID space) => SetSpace(body, space, isArea: false);
    internal void AreaSetSpaceCore(RID area, RID space) => SetSpace(area, space, isArea: true);

    internal RID BodyGetSpaceCore(RID body)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        return collider.SpaceRID;
    }
    internal RID AreaGetSpaceCore(RID area)
    {
        var collider = GetCollider(area, isArea: true);
        EnsureColliderSpaceAccessible(collider);
        return collider.SpaceRID;
    }

    internal void AreaSetTransformCore(RID area, Transform transform)
    {
        ThrowIfDisposed(); var runtime = AreaRuntime(area); runtime.EnsureAccess(true); var owners = runtime.Owners;
        PhysicsServerCollider.ValidateTransform(transform);
        if (owners.Scene is { } scene) scene.GlobalTransform = transform; else owners.Server!.SetTransform(transform);
    }

    internal void BodySetModeCore(RID body, BodyMode mode)
    {
        if (mode is not (BodyMode.Static or BodyMode.Kinematic or BodyMode.Rigid or BodyMode.RigidLinear))
            throw new ArgumentOutOfRangeException(nameof(mode));
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        collider.SetMode(mode);
    }

    internal BodyMode BodyGetModeCore(RID body)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        return collider.Mode;
    }

    internal void BodySetCollisionLayerCore(RID body, uint layer)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        collider.SetFilter(layer, collider.CollisionMask);
    }

    internal void BodySetCollisionMaskCore(RID body, uint mask)
    {
        var collider = GetCollider(body, isArea: false);
        EnsureColliderSpaceAccessible(collider);
        collider.SetFilter(collider.CollisionLayer, mask);
    }

    internal void AreaSetCollisionLayerCore(RID area, uint layer)
    {
        ThrowIfDisposed(); var runtime = AreaRuntime(area); runtime.EnsureAccess(true); var owners = runtime.Owners;
        if (owners.Scene is { } scene) scene.CollisionLayer = layer; else owners.Server!.SetFilter(layer, owners.Server.CollisionMask);
    }

    internal void AreaSetMonitorableCore(RID area, bool monitorable)
    {
        ThrowIfDisposed(); var runtime = AreaRuntime(area); runtime.EnsureAccess(true); var owners = runtime.Owners;
        if (owners.Scene is { } scene) scene.Monitorable = monitorable; else owners.Server!.Monitorable = monitorable;
    }

    internal void FreeRIDCore(RID rid)
    {
        ThrowIfDisposed();
        PhysicsSpace? space = null;
        PhysicsServerCollider? collider = null;
        PhysicsServerShape? shape = null;
        PhysicsJointRuntime? joint = null;
        lock (_registryGate)
        {
            if (_ownedSpaces.Contains(rid)) space = _sceneSpaces[rid];
            else if (_serverColliders.TryGetValue(rid, out collider)) { }
            else if (_serverShapes.TryGetValue(rid, out shape)) { }
            else if (_jointRuntimes.TryGetValue(rid, out joint))
            {
                if (joint.Scene is not null) throw new InvalidOperationException("The joint RID is owned by its scene node.");
            }
            else if (_sceneSpaces.ContainsKey(rid) || _sceneObjects.ContainsKey(rid))
                throw new InvalidOperationException("The RID is owned by a scene tree or collision node.");
            else throw new ArgumentException("The RID is not a live server resource.", nameof(rid));
        }
        if (space is not null)
        {
            space.EnsureReleaseAccess();
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
            if (!collider.IsArea) EnsureJointBodyMembershipChange(rid);
            try { if (collider.SpaceRID.IsValid()) GetSceneSpace(collider.SpaceRID).Remove(collider); }
            finally
            {
                if (!collider.IsArea) ClearJointsForBody(rid);
                lock (_registryGate)
                {
                    _serverColliders.Remove(rid);
                    if (_bodyRuntimes.Remove(rid, out var runtime)) runtime.Released = true;
                    _areaRuntimes.Remove(rid);
                    _bodyExceptions.Remove(rid);
                }
            }
        }
        else if (joint is not null)
        {
            joint.EnsureAccess(); joint.Clear();
            lock (_registryGate) _jointRuntimes.Remove(rid);
        }
        else if (shape is not null)
        {
            if (!shape.OwnsGeometry)
                throw new InvalidOperationException("A Shape resource owns this RID and its lifetime.");
            var users = SnapshotColliders();
            foreach (var user in users)
                if (user.UsesShape(shape)) EnsureColliderSpaceAccessible(user);
            var sceneUsers = SceneShapeUsers(shape);
            foreach (var scene in sceneUsers) EnsureSceneShapeAccess(scene, writing: true);
            foreach (var user in users) user.RemoveShape(shape);
            foreach (var scene in sceneUsers) scene.RemoveServerShape(shape);
            try { shape.Geometry.ReleaseServerGeometry(); }
            finally { lock (_registryGate) _serverShapes.Remove(rid); }
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
        geometry.BindServerOwnedRID(rid);
        lock (_registryGate) _serverShapes.Add(rid, new(rid, geometry));
        return rid;
    }

    private PhysicsServerShape GetShape(RID rid)
    {
        ThrowIfDisposed();
        lock (_registryGate)
            return _serverShapes.TryGetValue(rid, out var shape) && !shape.Geometry.IsDisposed ? shape :
                throw new ArgumentException("The RID is not a live server shape.", nameof(rid));
    }

    internal Shape GetShapeGeometry(RID rid) => GetShape(rid).Geometry;

    internal RID RegisterBorrowedShape(Shape geometry)
    {
        var rid = RID.Allocate();
        lock (_registryGate) _serverShapes.Add(rid, new(rid, geometry, ownsGeometry: false));
        return rid;
    }

    internal void MarkBorrowedShapeDirty(RID rid)
    {
        var shape = GetShape(rid);
        foreach (var user in SnapshotColliders())
            if (user.UsesShape(shape)) user.MarkShapesDirty();
    }

    internal void UnregisterBorrowedShape(RID rid)
    {
        PhysicsServerShape? shape;
        lock (_registryGate)
        {
            if (!_serverShapes.Remove(rid, out shape)) return;
        }
        foreach (var user in SnapshotColliders())
            if (user.UsesShape(shape)) user.MarkShapesDirty();
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
        var owners = ShapeOwners(owner, isArea, writing: true);
        var resource = GetShape(shape);
        if (owners.Scene is { } scene) scene.AddServerShape(resource, transform, disabled);
        else owners.Server!.AddShape(resource, transform, disabled);
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
