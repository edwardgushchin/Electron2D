using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>A spatial collision object that participates in a scene tree's physics world.</summary>
public abstract class PhysicsBody : CollisionObject
{
    private readonly List<ICollisionGeometry> _shapes = [];
    private readonly List<B2ShapeId> _backendShapes = [];
    private readonly List<ulong> _appliedShapeRevisions = [];
    private PhysicsSpace? _space;
    private B2BodyId _bodyID;
    private Vector2 _lastPosition;
    private float _lastRotation;
    private volatile bool _shapesDirty = true;
    private PhysicsMaterial? _materialOverride;
    private ulong _appliedMaterialRevision;

    /// <summary>Creates a detached body with no collision shapes.</summary>
    protected PhysicsBody() { }

    internal B2BodyId BackendID => _bodyID;
    internal PhysicsSpace? Space => _space;
    internal bool HasBackend => _space is not null;
    internal override IReadOnlyList<B2ShapeId> BackendShapes => _backendShapes;

    internal Entity? GetShapeNode(int index) => (uint)index < (uint)_shapes.Count ? _shapes[index].Node : null;

    internal override void AttachShape(ICollisionGeometry shape)
    {
        if (_shapes.Contains(shape)) return;
        _shapes.Add(shape);
        MarkShapesDirty();
    }

    internal override void DetachShape(ICollisionGeometry shape)
    {
        if (_shapes.Remove(shape)) MarkShapesDirty();
    }

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
        ValidatePhysicsTransform();
        var definition = CreateBodyDefinition();
        _lastPosition = GlobalPosition;
        _lastRotation = GlobalRotation;
        definition.position = Shape.ToBackend(_lastPosition);
        definition.rotation = b2MakeRot(_lastRotation);
        _bodyID = b2CreateBody(space.WorldID, definition);
        _space = space;
        _shapesDirty = true;
        try { RebuildShapes(); }
        catch { DetachBackend(); throw; }
    }

    internal void DetachBackend()
    {
        if (_space is null) return;
        b2DestroyBody(_bodyID);
        _backendShapes.Clear();
        _appliedShapeRevisions.Clear();
        _space = null;
        _shapesDirty = true;
    }

    internal void PrepareBackend()
    {
        if (_space is null) return;
        ValidatePhysicsTransform();
        if (_materialOverride is { IsDisposed: true })
        {
            _materialOverride = null;
            MarkShapesDirty();
        }
        if ((_materialOverride?.Revision ?? 0) != _appliedMaterialRevision) MarkShapesDirty();
        if (!_shapesDirty)
            for (var index = 0; index < _shapes.Count; index++)
                if (_shapes[index].GeometryRevision != _appliedShapeRevisions[index]) { MarkShapesDirty(); break; }
        if (_shapesDirty) RebuildShapes();
        var position = GlobalPosition;
        var rotation = GlobalRotation;
        if (position != _lastPosition || rotation != _lastRotation)
        {
            ApplySceneTransform(position, rotation);
            _lastPosition = position;
            _lastRotation = rotation;
        }
    }

    internal virtual void ApplySceneTransform(Vector2 position, float rotation) =>
        b2Body_SetTransform(_bodyID, Shape.ToBackend(position), b2MakeRot(rotation));

    internal void CompleteBackend()
    {
        if (_space is null || !MovesWithSimulation) return;
        var position = b2Body_GetPosition(_bodyID);
        var rotation = b2Body_GetRotation(_bodyID);
        var scenePosition = new Vector2(position.X * PhysicsSpace.UnitsPerMeter, position.Y * PhysicsSpace.UnitsPerMeter);
        var sceneRotation = b2Rot_GetAngle(rotation);
        _lastPosition = scenePosition;
        _lastRotation = sceneRotation;
        if (GlobalPosition != scenePosition || GlobalRotation != sceneRotation)
            GlobalTransform = new Transform(sceneRotation, Vector2.One, 0, scenePosition);
        OnBackendAdvanced();
    }

    internal abstract B2BodyDef CreateBodyDefinition();
    internal abstract bool MovesWithSimulation { get; }
    internal virtual void OnBackendAdvanced() { }
    internal virtual void OnShapesRebuilt() { }
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
        PhysicsServer2D.Instance.BodyAddCollisionException(GetRID(), body.GetRID());
    }

    /// <summary>Removes another body from this body's collision-exception list.</summary>
    /// <param name="body">A live scene physics body.</param>
    /// <exception cref="ArgumentNullException">The body is null.</exception>
    public void RemoveCollisionExceptionWith(PhysicsBody body)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(body);
        PhysicsServer2D.Instance.BodyRemoveCollisionException(GetRID(), body.GetRID());
    }

    /// <summary>Returns current scene body exceptions in insertion order.</summary>
    /// <returns>A caller-owned array; server-only or freed entries have null scene objects.</returns>
    /// <exception cref="InvalidOperationException">An attached body is read off its scene owner thread.</exception>
    public PhysicsBody?[] GetCollisionExceptions()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        var entries = PhysicsServer2D.Instance.GetBodyCollisionExceptions(GetRID());
        var result = new PhysicsBody?[entries.Length];
        for (var index = 0; index < entries.Length; index++)
            result[index] = PhysicsServer2D.Instance.ResolveSceneObject(entries[index]) as PhysicsBody;
        return result;
    }

    /// <summary>Moves this body along a global displacement until its first eligible body collision.</summary>
    /// <param name="motion">Finite global displacement in scene units.</param>
    /// <param name="testOnly">When true, report without changing the scene pose.</param>
    /// <param name="safeMargin">Nonnegative contact recovery margin in scene units.</param>
    /// <param name="recoveryAsCollision">Whether initial depenetration can produce a collision result.</param>
    /// <returns>A caller-owned collision snapshot, or null when motion is unobstructed.</returns>
    public KinematicCollision2D? MoveAndCollide(Vector2 motion, bool testOnly = false,
        float safeMargin = 0.08f, bool recoveryAsCollision = false)
    {
        EnsureMutable();
        ValidateMotion(motion, safeMargin);
        if (!HasBackend) throw new InvalidOperationException("A body must be attached before moving through physics.");
        var from = GlobalTransform;
        var data = PhysicsServer2D.Instance.TestMotionData(GetRID(), from, motion, safeMargin,
            recoveryAsCollision, [], []);
        if (!testOnly && data.Travel != Vector2.Zero)
            GlobalTransform = new Transform(from.Rotation, Vector2.One, 0, from.Origin + data.Travel);
        return data.Collided ? new KinematicCollision2D(data) : null;
    }

    /// <summary>Tests motion from a supplied global pose without moving this body.</summary>
    /// <param name="from">Finite unit-scale global starting pose.</param>
    /// <param name="motion">Finite global displacement in scene units.</param>
    /// <param name="collision">Optional caller-owned result updated on a completed test.</param>
    /// <param name="safeMargin">Nonnegative contact recovery margin in scene units.</param>
    /// <param name="recoveryAsCollision">Whether initial depenetration counts as a collision.</param>
    /// <returns>Whether motion or requested recovery reaches a body contact.</returns>
    public bool TestMove(Transform from, Vector2 motion, KinematicCollision2D? collision = null,
        float safeMargin = 0.08f, bool recoveryAsCollision = false)
    {
        EnsureMutable();
        if (!HasBackend) return false;
        ValidateMotion(motion, safeMargin);
        if (!from.IsFinite() || !from.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(from.Skew))
            throw new ArgumentException("Body motion requires finite translation, unit scale and zero skew.", nameof(from));
        if (collision?.IsDisposed == true) throw new ObjectDisposedException(nameof(collision));
        var data = PhysicsServer2D.Instance.TestMotionData(GetRID(), from, motion, safeMargin,
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
        Tree?.RegisterPhysicsBody(this);
    }

    /// <inheritdoc />
    protected override void OnExitTree()
    {
        try { Tree?.UnregisterPhysicsBody(this); }
        finally { base.OnExitTree(); }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _space?.Remove(this);
            _shapes.Clear();
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
        foreach (var node in _shapes)
        {
            if (!node.IsActive) continue;
            if (!node.Node.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(node.Node.Skew))
                throw new InvalidOperationException("Physics shapes require unit scale and zero skew.");
        }

        foreach (var id in _backendShapes) b2DestroyShape(id, updateBodyMass: false);
        _backendShapes.Clear();

        var definition = b2DefaultShapeDef();
        definition.filter.categoryBits = CollisionLayer;
        definition.filter.maskBits = CollisionMask;
        definition.density = MovesWithSimulation ? 1f : 0f;
        PhysicsSpace.SetMaterial(ref definition, _materialOverride);
        for (var index = 0; index < _shapes.Count; index++)
        {
            var node = _shapes[index];
            if (!node.IsActive) continue;
            var contact = node.OneWayContact;
            definition.userData = new B2UserData(new PhysicsFixtureTag(GetRID(), index, contact));
            definition.enablePreSolveEvents = contact is not null ||
                PhysicsServer2D.Instance.HasBodyCollisionExceptions(GetRID());
            node.AppendToBody(_bodyID, definition, _backendShapes);
        }

        OnShapesRebuilt();
        _appliedShapeRevisions.Clear();
        foreach (var node in _shapes) _appliedShapeRevisions.Add(node.GeometryRevision);
        _appliedMaterialRevision = _materialOverride?.Revision ?? 0;
        _shapesDirty = false;
    }

    private void ValidatePhysicsTransform()
    {
        var transform = GlobalTransform;
        if (!transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(transform.Skew))
            throw new InvalidOperationException("Physics bodies require unit global scale and zero skew.");
    }

    private void OnMaterialChanged(Resource _) => MarkShapesDirty();

    private void OnMaterialDisposed(ElectronObject _)
    {
        _materialOverride = null;
        MarkShapesDirty();
    }
}
