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
    private PhysicsSpace? _space;
    private B2BodyId _bodyID;
    private Vector2 _lastPosition;
    private float _lastRotation;
    private volatile bool _shapesDirty = true;

    /// <summary>Creates a detached body with no collision shapes.</summary>
    protected PhysicsBody() { }

    internal B2BodyId BackendID => _bodyID;
    internal bool HasBackend => _space is not null;

    internal void AttachShape(CollisionShape shape)
    {
        if (_shapes.Contains(shape)) return;
        _shapes.Add(shape);
        MarkShapesDirty();
    }

    internal void DetachShape(CollisionShape shape)
    {
        if (_shapes.Remove(shape)) MarkShapesDirty();
    }

    internal void MarkShapesDirty() => _shapesDirty = true;

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
        _space = null;
        _shapesDirty = true;
    }

    internal void PrepareBackend()
    {
        if (_space is null) return;
        ValidatePhysicsTransform();
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
        if (GlobalPosition != scenePosition) GlobalPosition = scenePosition;
        if (GlobalRotation != sceneRotation) GlobalRotation = sceneRotation;
        OnBackendAdvanced();
    }

    internal abstract B2BodyDef CreateBodyDefinition();
    internal abstract bool MovesWithSimulation { get; }
    internal virtual void OnBackendAdvanced() { }
    internal virtual void OnShapesRebuilt() { }

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
        foreach (var node in _shapes)
        {
            if (node.Disabled || node.Shape is not { IsDisposed: false } shape) continue;
            _backendShapes.Add(shape.AddToBody(_bodyID, node.Position, node.Rotation, definition));
        }

        OnShapesRebuilt();
        _shapesDirty = false;
    }

    private void ValidatePhysicsTransform()
    {
        var transform = GlobalTransform;
        if (!transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(transform.Skew))
            throw new InvalidOperationException("Physics bodies require unit global scale and zero skew.");
    }
}
