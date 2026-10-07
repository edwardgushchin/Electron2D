using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

/// <summary>A spatial collision object that participates in a scene tree's physics world.</summary>
public abstract class PhysicsBody : CollisionObject
{
    private WeakReference<CollisionObject>? _fixtureOwner;
    private readonly List<B2ShapeId> _backendShapes = [];
    private readonly List<ulong> _appliedShapeRevisions = [];
    private PhysicsSpace? _space;
    private B2BodyId _bodyID;
    private Vector2 _lastPosition;
    private float _lastRotation;
    private Transform _validatedTransform = Transform.Identity;
    private float _validatedRotation;
    private Transform _preparedTransform;
    private long _preparedGeometryEpoch = -1;
    private volatile bool _shapesDirty = true;
    private PhysicsMaterial? _materialOverride;
    private ulong _appliedMaterialRevision;

    /// <summary>Creates a detached body with no collision shapes.</summary>
    protected PhysicsBody() { }

    private PhysicsBodyRuntime? _runtime;
    internal PhysicsBodyRuntime Runtime => _runtime ??= PhysicsServer.Service.BodyRuntime(PhysicsRID);
    internal B2BodyId BackendID => _bodyID;
    internal PhysicsSpace? Space => _space;
    internal bool HasBackend => _space is not null;
    internal virtual bool CollisionResponseEnabled => true;
    internal uint EffectiveCollisionLayer => CollisionResponseEnabled ? CollisionLayer : 0;
    internal uint EffectiveCollisionMask => CollisionResponseEnabled ? CollisionMask : 0;
    internal override IReadOnlyList<B2ShapeId> BackendShapes => _backendShapes;

    internal ElectronObject? GetShapeNode(int index) => GetShapeOwnerObject(index);

    internal override void MarkShapesDirty() => _shapesDirty = true;

    internal PhysicsMaterial? MaterialOverride => _materialOverride is { IsDisposed: true } ? null : _materialOverride;

    internal void SetMaterialOverride(PhysicsMaterial? material)
    {
        EnsureMutable();
        if (material?.IsDisposed == true) throw new ObjectDisposedException(nameof(material));
        if (ReferenceEquals(material, _materialOverride)) return;
        if (_materialOverride is { } old)
        {
            old.Changed -= OnMaterialChanged;
            old.Disposed -= OnMaterialDisposed;
        }
        _materialOverride = material;
        PhysicsServer.Service.BodyRuntime(PhysicsRID).ResetMaterialOverrides();
        if (material is not null)
        {
            material.Changed += OnMaterialChanged;
            material.Disposed += OnMaterialDisposed;
        }
        MarkShapesDirty();
    }

    internal void AttachBackend(PhysicsSpace space)
    {
        if (_space is not null) throw new InvalidOperationException("A body already belongs to a physics world.");
        var transform = GlobalTransform;
        ValidatePhysicsTransform(transform);
        var definition = CreateBodyDefinition();
        if (PhysicsMadeStatic)
        {
            definition.type = B2BodyType.b2_staticBody;
            definition.linearVelocity = default;
            definition.angularVelocity = 0;
        }
        _lastPosition = transform.Origin;
        _lastRotation = _validatedRotation;
        definition.position = Shape.ToBackend(_lastPosition);
        definition.rotation = b2MakeRot(_lastRotation);
        _bodyID = b2CreateBody(space.WorldID, definition);
        _space = space;
        _shapesDirty = true;
        try { RebuildShapes(); if (PhysicsMadeStatic) OnMadeStatic(); }
        catch { DetachBackend(); throw; }
    }

    internal void DetachBackend()
    {
        PhysicsServer.Service.InvalidateBodyView(PhysicsRID);
        if (_space is null) return;
        if (this is RigidBody rigid && b2Body_GetType(_bodyID) == B2BodyType.b2_dynamicBody)
        {
            var world = b2GetWorldFromId(_space.WorldID);
            rigid.OnBackendAdvanced(world, b2GetBodyFullId(world, _bodyID));
        }
        b2DestroyBody(_bodyID);
        _backendShapes.Clear();
        _appliedShapeRevisions.Clear();
        _space = null;
        _shapesDirty = true;
    }

    internal void PrepareBackend()
    {
        if (_space is null) return;
        var transform = GlobalTransform;
        var geometryEpoch = Shape.GeometryEpoch;
        if (!_shapesDirty && geometryEpoch == _preparedGeometryEpoch && transform == _preparedTransform &&
            (_materialOverride is null || !_materialOverride.IsDisposed && _materialOverride.Revision == _appliedMaterialRevision)) return;
        ValidatePhysicsTransform(transform);
        if (_materialOverride is { IsDisposed: true })
        {
            _materialOverride = null;
            PhysicsServer.Service.BodyRuntime(PhysicsRID).ResetMaterialOverrides();
            MarkShapesDirty();
        }
        if ((_materialOverride?.Revision ?? 0) != _appliedMaterialRevision)
        {
            PhysicsServer.Service.BodyRuntime(PhysicsRID).ResetMaterialOverrides();
            MarkShapesDirty();
        }
        if (!_shapesDirty)
            for (var index = 0; index < ShapeSlots.Count; index++)
                if (ShapeSlots[index].Revision != _appliedShapeRevisions[index]) { MarkShapesDirty(); break; }
        if (_shapesDirty) RebuildShapes();
        var position = transform.Origin;
        var rotation = _validatedRotation;
        if (position != _lastPosition || rotation != _lastRotation)
        {
            ApplySceneTransform(position, rotation);
            _lastPosition = position;
            _lastRotation = rotation;
        }
        _preparedTransform = transform;
        _preparedGeometryEpoch = geometryEpoch;
    }

    internal virtual void ApplySceneTransform(Vector2 position, float rotation) =>
        b2Body_SetTransform(_bodyID, Shape.ToBackend(position), b2MakeRot(rotation));

    internal void CompleteBackend(B2World? world = null)
    {
        if (_space is null || !MovesWithSimulation || PhysicsMadeStatic) return;
        world ??= b2GetWorldFromId(_space.WorldID);
        var backendBody = b2GetBodyFullId(world, _bodyID);
        var backendTransform = b2GetBodyTransformQuick(world, backendBody);
        var position = backendTransform.p;
        var rotation = backendTransform.q;
        var scenePosition = new Vector2(position.X * PhysicsSpace.UnitsPerMeter, position.Y * PhysicsSpace.UnitsPerMeter);
        var sceneRotation = b2Rot_GetAngle(rotation);
        _lastPosition = scenePosition;
        _lastRotation = sceneRotation;
        var current = GlobalTransform;
        var currentRotation = current.X == _validatedTransform.X && current.Y == _validatedTransform.Y
            ? _validatedRotation : current.Rotation;
        if (current.Origin != scenePosition || currentRotation != sceneRotation)
        {
            var solverTransform = new Transform(sceneRotation, Vector2.One, 0, scenePosition);
            GlobalTransform = solverTransform;
            _validatedTransform = solverTransform;
            _validatedRotation = sceneRotation;
            _preparedTransform = solverTransform;
        }
        OnBackendAdvanced(world, backendBody);
    }

    internal abstract B2BodyType RequestedBodyType { get; }
    internal virtual void OnMadeStatic() { }
    internal virtual void OnBodyTypeChanged() { }

    internal override void UpdatePhysicsParticipation()
    {
        if (!IsInsideTree) return;
        if (PhysicsRemoved)
        {
            if (HasBackend) Tree?.UnregisterPhysicsBody(this);
            return;
        }
        if (!HasBackend) { Tree?.RegisterPhysicsBody(this); return; }
        var type = PhysicsMadeStatic ? B2BodyType.b2_staticBody : RequestedBodyType;
        if (b2Body_GetType(BackendID) == type) return;
        if (PhysicsMadeStatic) OnMadeStatic();
        b2Body_SetType(BackendID, type);
        OnBodyTypeChanged();
        MarkShapesDirty();
    }

    internal abstract B2BodyDef CreateBodyDefinition();
    internal abstract bool MovesWithSimulation { get; }
    internal virtual void OnBackendAdvanced(B2World world, B2Body body) { }
    internal virtual void OnShapesRebuilt() => PhysicsServer.Service.BodyRuntime(PhysicsRID).ApplyMassProfile();
    internal virtual Vector2 EffectiveGravity => Vector2.Zero;

    /// <summary>Gets the gravity applied by the last fixed physics step in scene units per second squared.</summary>
    /// <returns>Zero for detached bodies and StaticBody; RigidBody and CharacterBody report resolved Area/world gravity.</returns>
    /// <exception cref="InvalidOperationException">An attached body is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The body has been disposed.</exception>
    public Vector2 GetGravity()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return HasBackend ? EffectiveGravity : Vector2.Zero;
    }

    /// <summary>Adds another physics body to this body's collision-exception list.</summary>
    /// <param name="body">A live scene physics body.</param>
    /// <exception cref="ArgumentNullException">The body is null.</exception>
    public void AddCollisionExceptionWith(PhysicsBody body)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(body);
        PhysicsServer.BodyAddCollisionException(GetRID(), body.GetRID());
    }

    /// <summary>Removes another body from this body's collision-exception list.</summary>
    /// <param name="body">A live scene physics body.</param>
    /// <exception cref="ArgumentNullException">The body is null.</exception>
    public void RemoveCollisionExceptionWith(PhysicsBody body)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(body);
        PhysicsServer.BodyRemoveCollisionException(GetRID(), body.GetRID());
    }

    /// <summary>Returns deduplicated explicit and active-joint body exceptions.</summary>
    /// <returns>A caller-owned array with explicit entries first, then active joint targets; server-only or freed entries have null scene objects.</returns>
    /// <exception cref="InvalidOperationException">An attached body is read off its scene owner thread.</exception>
    public PhysicsBody?[] GetCollisionExceptions()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        var entries = PhysicsServer.Service.GetBodyCollisionExceptions(GetRID());
        var result = new PhysicsBody?[entries.Length];
        for (var index = 0; index < entries.Length; index++)
            result[index] = PhysicsServer.Service.ResolveSceneObject(entries[index]) as PhysicsBody;
        return result;
    }

    /// <summary>Moves this body along a global displacement until its first eligible body collision.</summary>
    /// <param name="motion">Finite global displacement in scene units.</param>
    /// <param name="testOnly">When true, report without changing the scene pose.</param>
    /// <param name="safeMargin">Nonnegative contact recovery margin in scene units.</param>
    /// <param name="recoveryAsCollision">Whether initial depenetration can produce a collision result.</param>
    /// <returns>A caller-owned collision snapshot, or null when motion is unobstructed.</returns>
    public KinematicCollision? MoveAndCollide(Vector2 motion, bool testOnly = false,
        float safeMargin = 0.08f, bool recoveryAsCollision = false)
    {
        EnsureMutable();
        ValidateMotion(motion, safeMargin);
        if (!HasBackend) throw new InvalidOperationException("A body must be attached before moving through physics.");
        var from = GlobalTransform;
        var data = PhysicsServer.Service.TestMotionData(GetRID(), from, motion, safeMargin,
            recoveryAsCollision, [], []);
        if (!testOnly && data.Travel != Vector2.Zero)
            GlobalTransform = new Transform(from.Rotation, Vector2.One, 0, from.Origin + data.Travel);
        return data.Collided ? new KinematicCollision(data) : null;
    }

    /// <summary>Tests motion from a supplied global pose without moving this body.</summary>
    /// <param name="from">Finite unit-scale global starting pose.</param>
    /// <param name="motion">Finite global displacement in scene units.</param>
    /// <param name="collision">Optional caller-owned result updated on a completed test.</param>
    /// <param name="safeMargin">Nonnegative contact recovery margin in scene units.</param>
    /// <param name="recoveryAsCollision">Whether initial depenetration counts as a collision.</param>
    /// <returns>Whether motion or requested recovery reaches a body contact.</returns>
    public bool TestMove(Transform from, Vector2 motion, KinematicCollision? collision = null,
        float safeMargin = 0.08f, bool recoveryAsCollision = false)
    {
        EnsureMutable();
        if (!HasBackend) return false;
        ValidateMotion(motion, safeMargin);
        if (!from.IsFinite() || !from.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(from.Skew))
            throw new ArgumentException("Body motion requires finite translation, unit scale and zero skew.", nameof(from));
        if (collision?.IsDisposed == true) throw new ObjectDisposedException(nameof(collision));
        var data = PhysicsServer.Service.TestMotionData(GetRID(), from, motion, safeMargin,
            recoveryAsCollision, [], []);
        collision?.Set(data);
        return data.Collided;
    }

    private static void ValidateMotion(Vector2 motion, float margin)
    {
        if (!motion.IsFinite() || !float.IsFinite(motion.Length()))
            throw new ArgumentOutOfRangeException(nameof(motion));
        if (!float.IsFinite(margin) || margin < 0) throw new ArgumentOutOfRangeException(nameof(margin));
    }

    internal override void OnCollisionFilterChanged() => MarkShapesDirty();

    /// <inheritdoc />
    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        UpdatePhysicsParticipation();
    }

    /// <inheritdoc />
    protected override void OnExitTree()
    {
        try { Tree?.UnregisterPhysicsBody(this); }
        finally { base.OnExitTree(); }
    }

    /// <summary>Checks scene and dependent joint world ownership before beginning disposal.</summary>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping.</exception>
    protected override void ValidateDisposal()
    {
        Tree?.EnsurePhysicsParticipationChange();
        PhysicsServer.Service.EnsureJointBodyMembershipChange(PhysicsRID);
        base.ValidateDisposal();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _space?.Remove(this);
            if (_materialOverride is { } material)
            {
                material.Changed -= OnMaterialChanged;
                material.Disposed -= OnMaterialDisposed;
                _materialOverride = null;
            }
        }
        base.Dispose(disposing);
    }

    private void RebuildShapes()
    {
        foreach (var node in ShapeSlots)
        {
            if (!node.Active) continue;
            if (!node.Transform.IsFinite() || !node.Transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(node.Transform.Skew))
                throw new InvalidOperationException("Physics shapes require unit scale and zero skew.");
        }

        foreach (var id in _backendShapes) b2DestroyShape(id, updateBodyMass: false);
        _backendShapes.Clear();

        var definition = b2DefaultShapeDef();
        definition.updateBodyMass = false;
        definition.filter.categoryBits = EffectiveCollisionLayer;
        definition.filter.maskBits = EffectiveCollisionMask;
        definition.density = MovesWithSimulation ? 1f : 0f;
        var runtime = PhysicsServer.Service.BodyRuntime(PhysicsRID);
        PhysicsSpace.SetMaterial(ref definition, runtime.GetFriction(), runtime.GetBounce());
        for (var index = 0; index < ShapeSlots.Count; index++)
        {
            var node = ShapeSlots[index];
            if (!node.Active) continue;
            var contact = node.OneWay;
            definition.userData = new B2UserData(new PhysicsFixtureTag(GetRID(), index, contact) { SceneOwner = _fixtureOwner ??= new(this) });
            definition.enablePreSolveEvents = contact is not null ||
                PhysicsServer.Service.HasBodyCollisionExceptions(GetRID());
            node.Shape.AppendToBody(_bodyID, node.Transform.Origin, node.Transform.Rotation, definition, _backendShapes);
        }

        OnShapesRebuilt();
        _appliedShapeRevisions.Clear();
        foreach (var node in ShapeSlots) _appliedShapeRevisions.Add(node.Revision);
        _appliedMaterialRevision = _materialOverride?.Revision ?? 0;
        _shapesDirty = false;
    }

    private void ValidatePhysicsTransform(Transform transform)
    {
        if (transform.X == _validatedTransform.X && transform.Y == _validatedTransform.Y) return;
        if (!transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(transform.Skew))
            throw new InvalidOperationException("Physics bodies require unit global scale and zero skew.");
        _validatedTransform = transform;
        _validatedRotation = transform.Rotation;
    }

    private void OnMaterialChanged(Resource _) { if (IsDisposed) return; PhysicsServer.Service.BodyRuntime(PhysicsRID).ResetMaterialOverrides(); MarkShapesDirty(); }

    private void OnMaterialDisposed(ElectronObject _)
    {
        if (IsDisposed) return;
        _materialOverride = null;
        PhysicsServer.Service.BodyRuntime(PhysicsRID).ResetMaterialOverrides();
        MarkShapesDirty();
    }
}
