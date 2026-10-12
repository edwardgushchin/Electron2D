namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Identifies the geometry carried by a physics shape RID.</summary>
    public enum ShapeType
    {
        /// <summary>An infinite solid half-plane.</summary>
        WorldBoundary = 0,
        /// <summary>A directed separation ray.</summary>
        SeparationRay = 1,
        /// <summary>A two-sided segment.</summary>
        Segment = 2,
        /// <summary>A filled circle.</summary>
        Circle = 3,
        /// <summary>A filled rectangle.</summary>
        Rectangle = 4,
        /// <summary>A rounded capsule.</summary>
        Capsule = 5,
        /// <summary>A filled convex polygon.</summary>
        ConvexPolygon = 6,
        /// <summary>A hollow collection of paired edges.</summary>
        ConcavePolygon = 7,
        /// <summary>Extension-owned geometry; built-in factories do not produce this type.</summary>
        Custom = 8
    }

    internal ShapeType ShapeGetTypeCore(RID shape) => GetShape(shape).Geometry.GetGeometry().Kind switch
    {
        PhysicsShapeGeometry.ShapeKind.WorldBoundary => ShapeType.WorldBoundary,
        PhysicsShapeGeometry.ShapeKind.SeparationRay => ShapeType.SeparationRay,
        PhysicsShapeGeometry.ShapeKind.Segment => ShapeType.Segment,
        PhysicsShapeGeometry.ShapeKind.Circle => ShapeType.Circle,
        PhysicsShapeGeometry.ShapeKind.Rectangle => ShapeType.Rectangle,
        PhysicsShapeGeometry.ShapeKind.Capsule => ShapeType.Capsule,
        PhysicsShapeGeometry.ShapeKind.ConvexPolygon => ShapeType.ConvexPolygon,
        PhysicsShapeGeometry.ShapeKind.ConcavePolygon => ShapeType.ConcavePolygon,
        PhysicsShapeGeometry.ShapeKind.Custom => ShapeType.Custom,
        _ => throw new NotSupportedException("The geometry has no registered shape type.")
    };

    internal RID SpaceCreateCore(Backend backend = Backend.CPU, bool allowCPUFallback = false)
    {
        return CreateRegisteredSpace(backend, allowCPUFallback, owned: true).RID;
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
    internal RID WorldBoundaryShapeCreateCore() => CreateShape(new WorldBoundaryShape());
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
                EnsureColliderSpaceAccessible(collider, writing: true);
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
            if (!disabled) PhysicsShapeBackend.RequireSupported(scene is PhysicsBody sceneBody ? sceneBody.Space : ((Area)scene).Space, slot.Shape);
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
            if (!disabled) PhysicsShapeBackend.RequireSupported(scene is PhysicsBody sceneBody ? sceneBody.Space : ((Area)scene).Space, slot.Shape);
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
        var owners = ShapeOwners(body, isArea: false);
        return (owners.Scene?.Backend.Space ?? owners.Server?.Space)?.RID ?? default;
    }
    internal RID AreaGetSpaceCore(RID area)
    {
        var owners = ShapeOwners(area, isArea: true);
        return (owners.Scene?.Backend.Space ?? owners.Server?.Space)?.RID ?? default;
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
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureAccess(true);
        var owners = runtime.Owners;
        if (owners.Scene is { } scene) scene.SetServerMode(mode); else owners.Server!.SetMode(mode);
    }

    internal BodyMode BodyGetModeCore(RID body)
    {
        var runtime = ParameterRuntime(body, writing: false);
        var owners = runtime.Owners;
        return owners.Scene?.EffectiveBodyMode ?? owners.Server!.Mode;
    }

    internal void BodySetCollisionLayerCore(RID body, uint layer)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureAccess(writing: true);
        var owners = runtime.Owners; owners.Server?.EnsureAuthoringAccess();
        if (owners.Scene is { } scene) scene.CollisionLayer = layer;
        else owners.Server!.SetFilter(layer, owners.Server.CollisionMask);
    }

    internal uint BodyGetCollisionLayerCore(RID body)
    {
        var owners = ShapeOwners(body, isArea: false);
        return owners.Scene?.CollisionLayer ?? owners.Server!.CollisionLayer;
    }

    internal void BodySetCollisionMaskCore(RID body, uint mask)
    {
        ThrowIfDisposed(); var runtime = BodyRuntime(body); runtime.EnsureAccess(writing: true);
        var owners = runtime.Owners; owners.Server?.EnsureAuthoringAccess();
        if (owners.Scene is { } scene) scene.CollisionMask = mask;
        else owners.Server!.SetFilter(owners.Server.CollisionLayer, mask);
    }

    internal uint BodyGetCollisionMaskCore(RID body)
    {
        var owners = ShapeOwners(body, isArea: false);
        return owners.Scene?.CollisionMask ?? owners.Server!.CollisionMask;
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
            space.EnsureWorldRelease();
            try { space.Dispose(); }
            finally
            {
                lock (_registryGate)
                {
                    _ownedSpaces.Remove(rid);
                    _sceneSpaces.Remove(rid);
                    _directStates.Remove(rid);
                }
            }
        }
        else if (collider is not null)
        {
            collider.EnsureGeometryLifetimeAccess();
            if (collider.SpaceRID.IsValid())
            {
                var owner = GetSceneSpace(collider.SpaceRID);
                owner.EnsureReleaseAccess(); owner.EnsureExtensionHierarchyChange();
            }
            if (!collider.IsArea) EnsureJointBodyMembershipChange(rid, releasing: true);
            collider.Releasing = true;
            List<Exception>? errors = null;
            try
            {
                if (collider.Space is { } owner) PhysicsSpace.Cleanup(() => owner.Remove(collider), ref errors);
                if (!collider.IsArea) PhysicsSpace.Cleanup(() => ClearJointsForBody(rid), ref errors);
            }
            finally
            {
                lock (_registryGate)
                {
                    _serverColliders.Remove(rid);
                    if (_bodyRuntimes.Remove(rid, out var runtime)) runtime.Released = true;
                    _areaRuntimes.Remove(rid);
                    _bodyExceptions.Remove(rid);
                    _jointBodyExceptions.Remove(rid);
                }
            }
            PhysicsSpace.FinishCleanup(errors);
        }
        else if (joint is not null)
        {
            joint.EnsureAccess(releasing: true);
            joint.EnsureWriteAccess();
            try { joint.Clear(); }
            finally { lock (_registryGate) _jointRuntimes.Remove(rid); }
        }
        else if (shape is not null)
        {
            if (!shape.OwnsGeometry)
                throw new InvalidOperationException("A Shape resource owns this RID and its lifetime.");
            var users = SnapshotColliders();
            foreach (var user in users)
                if (user.UsesShape(shape)) EnsureColliderSpaceAccessible(user, writing: true);
            var sceneUsers = SceneShapeUsers(shape);
            foreach (var scene in sceneUsers) EnsureSceneShapeAccess(scene, writing: true);
            List<Exception>? errors = null;
            try
            {
                foreach (var user in users) PhysicsSpace.Cleanup(() => user.RemoveShape(shape), ref errors);
                foreach (var scene in sceneUsers) PhysicsSpace.Cleanup(() => scene.RemoveServerShape(shape), ref errors);
                PhysicsSpace.Cleanup(shape.Geometry.ReleaseServerGeometry, ref errors);
            }
            finally { lock (_registryGate) _serverShapes.Remove(rid); }
            PhysicsSpace.FinishCleanup(errors);
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
        if (!disabled) PhysicsShapeBackend.RequireSupported(owners.Scene is PhysicsBody body ? body.Space : owners.Scene is Area area ? area.Space : owners.Server?.Space, resource.Geometry);
        if (owners.Scene is { } scene) scene.AddServerShape(resource, transform, disabled);
        else owners.Server!.AddShape(resource, transform, disabled);
    }

    private void SetSpace(RID owner, RID spaceRID, bool isArea)
    {
        var owners = ShapeOwners(owner, isArea, writing: true);
        var old = owners.Scene?.Backend.Space ?? owners.Server?.Space;
        old?.EnsureExtensionHierarchyChange();
        if ((old?.RID ?? default) == spaceRID) return;
        PhysicsSpace? next = spaceRID.IsValid() ? GetSceneSpace(spaceRID) : null;
        next?.EnsureAttachmentAccess();
        if (next is not null)
        {
            if (owners.Server is { } server) server.ValidateGeometry(next);
            else owners.Scene!.ValidateSpaceGeometry(next);
        }
        old?.EnsureQueryAccess();
        Remove(old);
        try { Add(next); }
        catch (Exception error)
        {
            List<Exception> errors = [error];
            try { RemoveCurrent(next); } catch (Exception cleanup) { errors.Add(cleanup); }
            try { Add(old); }
            catch (Exception rollback)
            {
                errors.Add(rollback);
                try { RemoveCurrent(old); } catch (Exception cleanup) { errors.Add(cleanup); }
            }
            if (errors.Count == 1) throw;
            throw new AggregateException("Physics space attachment or restoration failed.", errors);
        }
        void Add(PhysicsSpace? space)
        {
            if (space is null) return;
            if (owners.Scene is PhysicsBody body) space.Add(body);
            else if (owners.Scene is Area area) space.Add(area);
            else space.Add(owners.Server!);
        }
        void Remove(PhysicsSpace? space)
        {
            if (space is null) return;
            if (owners.Scene is PhysicsBody body) space.Remove(body);
            else if (owners.Scene is Area area) space.Remove(area);
            else space.Remove(owners.Server!);
        }
        void RemoveCurrent(PhysicsSpace? space)
        {
            if (space is not null && ReferenceEquals(owners.Scene?.Backend.Space ?? owners.Server?.Space, space)) Remove(space);
        }
    }

    private static void EnsureColliderSpaceAccessible(PhysicsServerCollider collider, bool writing = false)
    {
        if (writing) collider.EnsureGeometryLifetimeAccess();
        collider.Space?.EnsureQueryAccess();
    }
}
