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
                bodyId = CreateBody(rigidbody, owner.Transform);
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

    private B2BodyId CreateBody(Rigidbody rigidbody, Transform transform)
    {
        var bodyDef = B2Types.b2DefaultBodyDef();
        bodyDef.position = ToBox2D(transform.WorldPosition);
        bodyDef.rotation = B2MathFunction.b2MakeRot(transform.WorldRotation);

        var bodyId = B2Bodies.b2CreateBody(_worldId, in bodyDef);
        B2Bodies.b2Body_SetType(bodyId, B2BodyType.b2_dynamicBody);

        var shapeDef = B2Types.b2DefaultShapeDef();

        const float defaultSize = 1f;
        var halfExtent = defaultSize * 0.5f * WorldUnitsPerBox2DUnit;
        var polygon = B2Geometries.b2MakeBox(halfExtent, halfExtent);

        var area = defaultSize * defaultSize;
        shapeDef.density = rigidbody.Mass / area;

        _ = B2Shapes.b2CreatePolygonShape(bodyId, in shapeDef, in polygon);

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