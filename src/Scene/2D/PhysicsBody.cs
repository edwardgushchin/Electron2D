using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>A spatial collision object that participates in a scene tree's physics world.</summary>
public abstract class PhysicsBody : CollisionObject
{
    private readonly List<CollisionShape> _shapes = [];
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
    internal bool HasBackend => _space is not null;
    internal override IReadOnlyList<B2ShapeId> BackendShapes => _backendShapes;

    internal override void AttachShape(CollisionShape shape)
    {
        if (_shapes.Contains(shape)) return;
        _shapes.Add(shape);
        MarkShapesDirty();
    }

    internal override void DetachShape(CollisionShape shape)
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
            b2Body_SetTransform(_bodyID, Shape.ToBackend(position), b2MakeRot(rotation));
            _lastPosition = position;
            _lastRotation = rotation;
        }
    }

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
    /// <returns>Zero for detached bodies and StaticBody; RigidBody reports its resolved area/world field after gravity scaling.</returns>
    /// <exception cref="InvalidOperationException">An attached body is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The body has been disposed.</exception>
    public Vector2 GetGravity()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return HasBackend ? EffectiveGravity : Vector2.Zero;
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
            if (node.Disabled || node.Shape is null || node.Shape.IsDisposed) continue;
            if (!node.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(node.Skew))
                throw new InvalidOperationException("Physics shapes require unit scale and zero skew.");
        }

        foreach (var id in _backendShapes) b2DestroyShape(id, updateBodyMass: false);
        _backendShapes.Clear();

        var definition = b2DefaultShapeDef();
        definition.filter.categoryBits = CollisionLayer;
        definition.filter.maskBits = CollisionMask;
        definition.density = MovesWithSimulation ? 1f : 0f;
        PhysicsSpace.SetMaterial(ref definition, _materialOverride);
        foreach (var node in _shapes)
        {
            if (node.Disabled || node.Shape is not { IsDisposed: false } shape) continue;
            _backendShapes.Add(shape.AddToBody(_bodyID, node.Position, node.Rotation, definition));
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
