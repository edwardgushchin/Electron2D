using System.Numerics;
using Box2D.NET;

namespace Electron2D;

/// <summary>
/// Система физики
/// </summary>
internal sealed class PhysicsSystem
{
    #region Constants
    // Контракт: 1 world unit = 1 Box2D unit (1 метр). Никаких скрытых масштабов.
    private const float WorldUnitsPerBox2DUnit = 1f;
    private const int VelocityIterations = 8;
    #endregion

    #region Instance fields
    private readonly Dictionary<Rigidbody, B2BodyId> _bodies = new();
    private readonly List<Rigidbody> _rigidbodyBuffer = new(capacity: 128);
    private readonly List<Collider> _colliderBuffer = new(capacity: 16);
    private readonly List<Rigidbody> _removeBuffer = new(capacity: 64);
    private B2WorldId _worldId;
    private bool _initialized;
    private float _fixedDelta;
    #endregion
    
    
    #region Public API

    public void Initialize(PhysicsConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        
        _fixedDelta = config.FixedDelta;

        var worldDef = B2Types.b2DefaultWorldDef();
        _worldId = B2Worlds.b2CreateWorld(in worldDef);
        B2Worlds.b2World_SetGravity(_worldId, ToBox2D(config.Gravity));
        _initialized = true;
    }

    public void Shutdown()
    {
        if (!_initialized) return;

        _bodies.Clear();
        B2Worlds.b2DestroyWorld(_worldId);
        _initialized = false;
    }

    public void Step(float fixedDeltaTime, SceneTree sceneTree)
    {
        ArgumentNullException.ThrowIfNull(sceneTree);

        if (!_initialized)
            return;

        _ = fixedDeltaTime; // фиксированный шаг уже учитывается через _fixedDelta (PhysicsConfig.FixedDelta)

        _rigidbodyBuffer.Clear();
        CollectRigidbodies(sceneTree.Root, _rigidbodyBuffer);

        for (var i = 0; i < _rigidbodyBuffer.Count; i++)
        {
            var rigidbody = _rigidbodyBuffer[i];
            var owner = rigidbody.Owner;
            if (owner is null)
                continue;

            if (!_bodies.TryGetValue(rigidbody, out var bodyId))
            {
                bodyId = CreateBody(rigidbody, owner);
                _bodies[rigidbody] = bodyId;
            }

            SyncTransformToBodyIfNeeded(rigidbody, owner.Transform, bodyId);
            ApplyForces(rigidbody, bodyId);
        }

        B2Worlds.b2World_Step(_worldId, _fixedDelta, VelocityIterations);

        for (var i = 0; i < _rigidbodyBuffer.Count; i++)
        {
            var rigidbody = _rigidbodyBuffer[i];
            if (!_bodies.TryGetValue(rigidbody, out var bodyId))
                continue;

            var owner = rigidbody.Owner;
            if (owner is null)
                continue;

            SyncBodyToTransform(rigidbody, owner.Transform, bodyId);
        }

        CleanupMissingBodies();
    }

    #endregion
    
    #region Private helpers
    private static B2Vec2 ToBox2D(Vector2 value)
        => new()
        {
            X = value.X * WorldUnitsPerBox2DUnit,
            Y = value.Y * WorldUnitsPerBox2DUnit
        };

    private static Vector2 ToWorld(B2Vec2 value)
        => new(
            value.X / WorldUnitsPerBox2DUnit,
            value.Y / WorldUnitsPerBox2DUnit);

    private B2BodyId CreateBody(Rigidbody rigidbody, Node owner)
    {
        var transform = owner.Transform;
        var bodyDef = B2Types.b2DefaultBodyDef();
        bodyDef.position = ToBox2D(transform.WorldPosition);
        bodyDef.rotation = B2MathFunction.b2MakeRot(transform.WorldRotation);

        var bodyId = B2Bodies.b2CreateBody(_worldId, in bodyDef);
        B2Bodies.b2Body_SetType(bodyId, ToBox2DBodyType(rigidbody.BodyType));

        _colliderBuffer.Clear();
        CollectColliders(owner, _colliderBuffer);

        if (_colliderBuffer.Count == 0)
        {
            _colliderBuffer.Add(new BoxCollider());
        }

        var totalArea = 0f;
        for (var i = 0; i < _colliderBuffer.Count; i++)
            totalArea += GetColliderArea(_colliderBuffer[i]);

        if (totalArea <= 0f)
            totalArea = 1f;

        var density = rigidbody.Mass / totalArea;

        for (var i = 0; i < _colliderBuffer.Count; i++)
        {
            var collider = _colliderBuffer[i];
            var shapeDef = B2Types.b2DefaultShapeDef();
            shapeDef.isSensor = collider.IsTrigger;
            shapeDef.density = density;

            CreateShape(bodyId, shapeDef, collider);
        }

        rigidbody.MarkTransformSynced();
        return bodyId;
    }

    private static void CollectRigidbodies(Node node, List<Rigidbody> rigidbodies)
    {
        var components = node.Components;
        for (var i = 0; i < components.Length; i++)
        {
            if (components[i] is Rigidbody rigidbody)
                rigidbodies.Add(rigidbody);
        }

        var children = node.Children;
        for (var i = 0; i < children.Length; i++)
            CollectRigidbodies(children[i], rigidbodies);
    }

    private static void CollectColliders(Node node, List<Collider> colliders)
    {
        var components = node.Components;
        for (var i = 0; i < components.Length; i++)
        {
            if (components[i] is Collider collider)
                colliders.Add(collider);
        }
    }

    private static float GetColliderArea(Collider collider) => collider switch
    {
        BoxCollider box => MathF.Abs(box.Size.X * box.Size.Y),
        CircleCollider circle => MathF.PI * circle.Radius * circle.Radius,
        PolygonCollider polygon => MathF.Abs(ComputePolygonArea(polygon.Points)),
        _ => 0f
    };

    private static float ComputePolygonArea(Vector2[] points)
    {
        if (points.Length < 3)
            return 0f;

        var area = 0f;
        for (var i = 0; i < points.Length; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Length];
            area += (p1.X * p2.Y) - (p2.X * p1.Y);
        }

        return 0.5f * area;
    }

    private static void CreateShape(B2BodyId bodyId, B2ShapeDef shapeDef, Collider collider)
    {
        switch (collider)
        {
            case BoxCollider box:
                CreateBoxShape(bodyId, ref shapeDef, box);
                break;
            case CircleCollider circle:
                CreateCircleShape(bodyId, ref shapeDef, circle);
                break;
            case PolygonCollider polygon:
                CreatePolygonShape(bodyId, ref shapeDef, polygon);
                break;
        }
    }

    private static void CreateBoxShape(B2BodyId bodyId, ref B2ShapeDef shapeDef, BoxCollider box)
    {
        var halfExtent = box.Size * 0.5f * WorldUnitsPerBox2DUnit;
        var offset = ToBox2D(box.Offset);
        var rotation = B2MathFunction.b2MakeRot(0f);
        var polygon = B2Geometries.b2MakeOffsetBox(halfExtent.X, halfExtent.Y, offset, rotation);
        _ = B2Shapes.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);
    }

    private static void CreateCircleShape(B2BodyId bodyId, ref B2ShapeDef shapeDef, CircleCollider circle)
    {
        var segments = Math.Max(3, circle.Segments);
        var points = new B2Vec2[segments];
        var offset = ToBox2D(circle.Offset);
        var radius = circle.Radius * WorldUnitsPerBox2DUnit;

        for (var i = 0; i < segments; i++)
        {
            var angle = (MathF.Tau / segments) * i;
            var x = MathF.Cos(angle) * radius + offset.X;
            var y = MathF.Sin(angle) * radius + offset.Y;
            points[i] = new B2Vec2 { X = x, Y = y };
        }

        var hull = B2Hulls.b2ComputeHull(points, points.Length);
        var polygon = B2Geometries.b2MakePolygon(in hull, 0f);
        _ = B2Shapes.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);
    }

    private static void CreatePolygonShape(B2BodyId bodyId, ref B2ShapeDef shapeDef, PolygonCollider polygonCollider)
    {
        var points = polygonCollider.Points;
        if (points.Length < 3)
            return;

        var vertices = new B2Vec2[points.Length];
        var offset = ToBox2D(polygonCollider.Offset);

        for (var i = 0; i < points.Length; i++)
        {
            var point = points[i];
            vertices[i] = new B2Vec2
            {
                X = point.X * WorldUnitsPerBox2DUnit + offset.X,
                Y = point.Y * WorldUnitsPerBox2DUnit + offset.Y
            };
        }

        var hull = B2Hulls.b2ComputeHull(vertices, vertices.Length);
        var polygon = B2Geometries.b2MakePolygon(in hull, 0f);
        _ = B2Shapes.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);
    }

    private static B2BodyType ToBox2DBodyType(PhysicsBodyType bodyType) => bodyType switch
    {
        PhysicsBodyType.Static => B2BodyType.b2_staticBody,
        PhysicsBodyType.Kinematic => B2BodyType.b2_kinematicBody,
        _ => B2BodyType.b2_dynamicBody
    };

    private static void ApplyForces(Rigidbody rigidbody, B2BodyId bodyId)
    {
        var force = rigidbody.ConsumePendingForce();
        if (force == Vector2.Zero)
            return;

        B2Bodies.b2Body_ApplyForceToCenter(bodyId, ToBox2D(force), true);
    }

    private static void SyncTransformToBodyIfNeeded(Rigidbody rigidbody, Transform transform, B2BodyId bodyId)
    {
        if (!rigidbody.NeedsTransformSync)
            return;

        var position = ToBox2D(transform.WorldPosition);
        var rotation = B2MathFunction.b2MakeRot(transform.WorldRotation);
        B2Bodies.b2Body_SetTransform(bodyId, position, rotation);
        rigidbody.MarkTransformSynced();
    }

    private static void SyncBodyToTransform(Rigidbody rigidbody, Transform transform, B2BodyId bodyId)
    {
        var position = B2Bodies.b2Body_GetPosition(bodyId);
        var rotation = B2Bodies.b2Body_GetRotation(bodyId);

        transform.WorldPosition = ToWorld(position);
        transform.WorldRotation = B2MathFunction.b2Rot_GetAngle(in rotation);
        rigidbody.MarkTransformSynced();
    }

    private void CleanupMissingBodies()
    {
        if (_bodies.Count == 0)
            return;

        _removeBuffer.Clear();
        foreach (var entry in _bodies)
        {
            if (!_rigidbodyBuffer.Contains(entry.Key))
                _removeBuffer.Add(entry.Key);
        }

        for (var i = 0; i < _removeBuffer.Count; i++)
        {
            var rigidbody = _removeBuffer[i];
            if (_bodies.Remove(rigidbody, out var bodyId))
                B2Bodies.b2DestroyBody(bodyId);
        }
    }
}

#endregion