using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace : IDisposable
{
    internal const float MetersPerUnit = 0.01f;
    internal const float UnitsPerMeter = 100f;
    private const ulong RoughMaterial = 1;
    private const ulong AbsorbentMaterial = 2;

    private readonly List<PhysicsBody> _bodies = [];
    private readonly List<Area> _areas = [];
    private readonly List<PhysicsServerCollider> _serverColliders = [];
    private readonly List<Area> _fieldAreas = [];
    private readonly List<OverlapEvent> _overlapEvents = [];
    private readonly List<ContactEvent> _contactEvents = [];
    private readonly List<RigidBody> _sleepEvents = [];
    private readonly Dictionary<(ulong, ulong), OneWayPair> _oneWayPairs = [];
    private readonly List<(ulong, ulong)> _staleOneWayPairs = [];
    private readonly B2WorldId _worldID;
    private readonly Vector2 _defaultGravity;
    private readonly int _ownerThreadID = Environment.CurrentManagedThreadId;
    private readonly float _defaultLinearDamp;
    private readonly float _defaultAngularDamp;
    private bool _stepping;
    private bool _dispatching;
    private bool _dispatchingContacts;
    private bool _disposed;
    private long _contactStep;

    internal readonly record struct OverlapEvent(Area Area, CollisionObject Other, bool Entered);
    internal readonly record struct ContactEvent(RigidBody Receiver, PhysicsBody Other, bool Entered);
    private readonly record struct OneWayPair(bool Allowed, long SeenStep);

    internal PhysicsSpace()
    {
        var settings = ProjectSettings.Instance;
        _defaultGravity = settings.GetWithOverride(ProjectSettings.Physics2DDefaultGravityVector) *
            settings.GetWithOverride(ProjectSettings.Physics2DDefaultGravity);
        _defaultLinearDamp = settings.GetWithOverride(ProjectSettings.Physics2DDefaultLinearDamp);
        _defaultAngularDamp = settings.GetWithOverride(ProjectSettings.Physics2DDefaultAngularDamp);
        if (!_defaultGravity.IsFinite()) throw new InvalidOperationException("Default physics gravity exceeds the finite simulation range.");
        var definition = b2DefaultWorldDef();
        definition.gravity = Shape.ToBackend(_defaultGravity);
        definition.restitutionThreshold = 0;
        definition.frictionCallback = CombineFriction;
        definition.restitutionCallback = CombineBounce;
        _worldID = b2CreateWorld(definition);
        b2World_SetPreSolveCallback(_worldID, PreSolveContact, this);
    }

    internal B2WorldId WorldID => _worldID;
    internal IReadOnlyList<PhysicsBody> Bodies => _bodies;
    internal IReadOnlyList<Area> Areas => _areas;
    internal IReadOnlyList<PhysicsServerCollider> ServerColliders => _serverColliders;

    internal void EnsureQueryAccess()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (Environment.CurrentManagedThreadId != _ownerThreadID)
            throw new InvalidOperationException("Physics queries require the space owner thread.");
        if (_stepping) throw new InvalidOperationException("A physics space cannot be queried while stepping.");
    }

    internal void PrepareForQuery()
    {
        EnsureQueryAccess();
        foreach (var body in _bodies) body.PrepareBackend();
        foreach (var area in _areas) area.PrepareBackend();
        foreach (var collider in _serverColliders) collider.PrepareBackend();
    }

    internal void Add(PhysicsBody body)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (_stepping) throw new InvalidOperationException("Physics bodies cannot enter a world while it is stepping.");
        _bodies.EnsureCapacity(_bodies.Count + 1);
        body.AttachBackend(this);
        _bodies.Add(body);
    }

    internal void Remove(PhysicsBody body)
    {
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Physics bodies cannot leave a world while it is stepping.");
        if (!_bodies.Remove(body)) return;
        if (body is RigidBody departing) departing.CaptureBackendSleep();
        body.DetachBackend();
        if (body is RigidBody removed) removed.ClearContactState();
        foreach (var other in _bodies)
            if (other is RigidBody rigid) rigid.ForgetContact(body, _contactEvents);
        foreach (var area in _areas) area.Forget(body, _overlapEvents);
        DispatchEvents();
    }

    internal void Add(Area area)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (_stepping) throw new InvalidOperationException("Physics areas cannot enter a world while it is stepping.");
        _areas.EnsureCapacity(_areas.Count + 1);
        area.AttachBackend(this);
        _areas.Add(area);
    }

    internal void Add(PhysicsServerCollider collider, RID spaceRID)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (_stepping) throw new InvalidOperationException("Server colliders cannot enter while stepping.");
        _serverColliders.EnsureCapacity(_serverColliders.Count + 1);
        collider.AttachBackend(this, spaceRID);
        _serverColliders.Add(collider);
    }

    internal void Remove(PhysicsServerCollider collider)
    {
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Server colliders cannot leave while stepping.");
        if (_serverColliders.Remove(collider)) collider.DetachBackend();
    }

    internal void Remove(Area area)
    {
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Physics areas cannot leave a world while it is stepping.");
        if (!_areas.Remove(area)) return;
        area.DetachBackend();
        area.ClearOverlaps();
        foreach (var other in _areas) other.Forget(area, _overlapEvents);
        DispatchEvents();
    }

    internal void Step(double delta)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (delta == 0 || (_bodies.Count == 0 && _areas.Count == 0 && _serverColliders.Count == 0)) return;
        if (_stepping) throw new InvalidOperationException("A physics world cannot step recursively.");
        _stepping = true;
        List<Exception>? errors = null;
        try
        {
            foreach (var body in _bodies) body.PrepareBackend();
            foreach (var area in _areas) area.PrepareBackend();
            foreach (var collider in _serverColliders) collider.PrepareBackend();
            ApplyAreaFields(delta);
            foreach (var body in _bodies)
            {
                if (body is RigidBody rigid) rigid.ApplyConstantForces();
                else if (body is AnimatableBody animatable) animatable.PrepareMotion(delta);
                else if (body is CharacterBody character) character.PrepareMotion(delta);
            }
            _contactStep++;
            b2World_Step(_worldID, (float)delta, 4);
            foreach (var body in _bodies)
            {
                try
                {
                    body.CompleteBackend();
                    if (body is AnimatableBody animatable) animatable.SyncPose();
                    else if (body is CharacterBody character) character.CaptureSolverPose();
                }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
            foreach (var body in _bodies)
            {
                if (body is not RigidBody rigid) continue;
                if (rigid.TakeSleepChange()) _sleepEvents.Add(rigid);
                rigid.CollectContacts(this, _contactEvents);
            }
            ScanAreas();
        }
        catch (Exception error) { (errors ??= []).Add(error); }
        finally { PruneOneWayPairs(); _stepping = false; }
        try { DispatchEvents(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Physics-world step failed.", errors);
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("A physics world cannot be disposed during a step.");
        foreach (var body in _bodies)
        {
            if (body is RigidBody departing) departing.CaptureBackendSleep();
            body.DetachBackend();
            if (body is RigidBody rigid) rigid.ClearContactState();
        }
        foreach (var area in _areas) { area.DetachBackend(); area.ClearOverlaps(); }
        foreach (var collider in _serverColliders) collider.DetachBackend();
        _bodies.Clear();
        _areas.Clear();
        _serverColliders.Clear();
        _fieldAreas.Clear();
        _overlapEvents.Clear();
        _contactEvents.Clear();
        _sleepEvents.Clear();
        _oneWayPairs.Clear();
        _staleOneWayPairs.Clear();
        b2DestroyWorld(_worldID);
        _disposed = true;
    }

    private static bool PreSolveContact(B2ShapeId first, B2ShapeId second, B2Vec2 point,
        B2Vec2 normal, object context) => ((PhysicsSpace)context).AllowBodyContact(first, second, normal);

    private bool AllowBodyContact(B2ShapeId first, B2ShapeId second, B2Vec2 normal)
    {
        var firstTag = b2Shape_GetUserData(first).GetRef<PhysicsFixtureTag>();
        var secondTag = b2Shape_GetUserData(second).GetRef<PhysicsFixtureTag>();
        if (firstTag is not null && secondTag is not null &&
            PhysicsServer2D.Instance.BodiesExcepted(firstTag.ColliderRID, secondTag.ColliderRID)) return false;
        var firstData = firstTag?.OneWay;
        var secondData = secondTag?.OneWay;
        if (firstData is null && secondData is null) return true;

        var firstKey = PackShapeID(first);
        var secondKey = PackShapeID(second);
        var key = firstKey < secondKey ? (firstKey, secondKey) : (secondKey, firstKey);
        if (_oneWayPairs.TryGetValue(key, out var previous))
        {
            _oneWayPairs[key] = previous with { SeenStep = _contactStep };
            return previous.Allowed;
        }

        var allowed = (firstData is null || FacesContact(first, firstData, normal, firstSurface: true)) &&
            (secondData is null || FacesContact(second, secondData, normal, firstSurface: false));
        _oneWayPairs.Add(key, new(allowed, _contactStep));
        return allowed;
    }

    private static bool FacesContact(B2ShapeId shape, OneWayContactData data, B2Vec2 normal, bool firstSurface)
    {
        var local = data.LocalDirection;
        var direction = b2RotateVector(b2Body_GetRotation(b2Shape_GetBody(shape)), new(local.X, local.Y));
        var facing = normal.X * direction.X + normal.Y * direction.Y;
        return firstSurface ? facing < -1e-6f : facing > 1e-6f;
    }

    private static ulong PackShapeID(B2ShapeId shape) =>
        ((ulong)(uint)shape.index1 << 32) | ((ulong)shape.world0 << 16) | shape.generation;

    private void PruneOneWayPairs()
    {
        _staleOneWayPairs.Clear();
        foreach (var pair in _oneWayPairs)
            if (pair.Value.SeenStep != _contactStep) _staleOneWayPairs.Add(pair.Key);
        foreach (var key in _staleOneWayPairs) _oneWayPairs.Remove(key);
    }

    private void ScanAreas()
    {
        // ponytail: Pairwise shape scans are quadratic; use a broad-phase candidate index if large worlds show a measured cost.
        foreach (var area in _areas)
        {
            area.BeginOverlapScan();
            if (area.Monitoring)
            {
                foreach (var body in _bodies)
                    if ((area.CollisionMask & body.CollisionLayer) != 0 && ShapesOverlap(area, body))
                        area.Observe(body);
                foreach (var other in _areas)
                    if (!ReferenceEquals(area, other) && other.Monitorable &&
                        (area.CollisionMask & other.CollisionLayer) != 0 && ShapesOverlap(area, other))
                        area.Observe(other);
            }
            area.CommitOverlapScan(_overlapEvents);
        }
    }

    private void ApplyAreaFields(double delta)
    {
        // ponytail: Stable insertion order is quadratic in field areas; use indexed sorting if large-world profiling needs it.
        _fieldAreas.Clear();
        foreach (var area in _areas)
        {
            if (!area.HasFieldOverrides) continue;
            var index = _fieldAreas.Count;
            _fieldAreas.Add(area);
            while (index > 0 && _fieldAreas[index - 1].Priority < area.Priority)
            {
                _fieldAreas[index] = _fieldAreas[index - 1];
                index--;
            }
            _fieldAreas[index] = area;
        }

        foreach (var body in _bodies)
        {
            if (body is not RigidBody && body is not CharacterBody) continue;
            var gravity = Vector2.Zero;
            var linearDamp = 0f;
            var angularDamp = 0f;
            var gravityDone = false;
            var linearDone = false;
            var angularDone = false;
            foreach (var area in _fieldAreas)
            {
                if ((area.CollisionMask & body.CollisionLayer) == 0 || !ShapesOverlap(area, body)) continue;
                if (!gravityDone)
                {
                    var mode = area.GravitySpaceOverride;
                    if (mode is Area.SpaceOverride.Combine or Area.SpaceOverride.CombineReplace)
                        gravity += area.ComputeGravity(body.GlobalPosition);
                    else if (mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine)
                        gravity = area.ComputeGravity(body.GlobalPosition);
                    gravityDone = mode is Area.SpaceOverride.CombineReplace or Area.SpaceOverride.Replace;
                }
                if (!linearDone)
                {
                    var mode = area.LinearDampSpaceOverride;
                    if (mode is Area.SpaceOverride.Combine or Area.SpaceOverride.CombineReplace)
                        linearDamp += area.LinearDamp;
                    else if (mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine)
                        linearDamp = area.LinearDamp;
                    linearDone = mode is Area.SpaceOverride.CombineReplace or Area.SpaceOverride.Replace;
                }
                if (!angularDone)
                {
                    var mode = area.AngularDampSpaceOverride;
                    if (mode is Area.SpaceOverride.Combine or Area.SpaceOverride.CombineReplace)
                        angularDamp += area.AngularDamp;
                    else if (mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine)
                        angularDamp = area.AngularDamp;
                    angularDone = mode is Area.SpaceOverride.CombineReplace or Area.SpaceOverride.Replace;
                }
                if (gravityDone && linearDone && angularDone) break;
            }
            if (!gravityDone) gravity += _defaultGravity;
            if (!linearDone) linearDamp += _defaultLinearDamp;
            if (!angularDone) angularDamp += _defaultAngularDamp;
            if (body is RigidBody rigid)
                rigid.ApplyAreaFields(gravity, linearDamp, angularDamp, _defaultGravity, delta);
            else
                ((CharacterBody)body).SetResolvedGravity(gravity);
        }
    }

    private bool ShapesOverlap(CollisionObject area, CollisionObject other)
    {
        var world = b2GetWorldFromId(_worldID);
        var areaShapes = area.BackendShapes;
        var otherShapes = other.BackendShapes;
        for (var areaIndex = 0; areaIndex < areaShapes.Count; areaIndex++)
        {
            var areaID = areaShapes[areaIndex];
            var aabbA = b2Shape_GetAABB(areaID);
            for (var otherIndex = 0; otherIndex < otherShapes.Count; otherIndex++)
            {
                var otherID = otherShapes[otherIndex];
                var aabbB = b2Shape_GetAABB(otherID);
                if (aabbA.upperBound.X < aabbB.lowerBound.X || aabbA.lowerBound.X > aabbB.upperBound.X ||
                    aabbA.upperBound.Y < aabbB.lowerBound.Y || aabbA.lowerBound.Y > aabbB.upperBound.Y)
                    continue;
                var shapeA = b2GetShape(world, areaID);
                var shapeB = b2GetShape(world, otherID);
                var input = new B2DistanceInput
                {
                    proxyA = b2MakeShapeDistanceProxy(shapeA),
                    proxyB = b2MakeShapeDistanceProxy(shapeB),
                    transformA = b2Body_GetTransform(b2Shape_GetBody(areaID)),
                    transformB = b2Body_GetTransform(b2Shape_GetBody(otherID)),
                    useRadii = true
                };
                var cache = new B2SimplexCache();
                if (b2ShapeDistance(ref input, ref cache, null, 0).distance <= 0.1f * B2_LINEAR_SLOP)
                    return true;
            }
        }
        return false;
    }

    internal PhysicsBody? FindBody(B2BodyId id)
    {
        // ponytail: Linear lookup is sufficient for small scenes; index body IDs if contact-heavy worlds show a measured cost.
        foreach (var body in _bodies)
            if (body.BackendID == id) return body;
        return null;
    }

    private void DispatchEvents()
    {
        List<Exception>? errors = null;
        try { DispatchContactEvents(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        try { DispatchOverlapEvents(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Physics callbacks failed.", errors);
    }

    private void DispatchContactEvents()
    {
        if (_dispatchingContacts || (_sleepEvents.Count == 0 && _contactEvents.Count == 0)) return;
        _dispatchingContacts = true;
        List<Exception>? errors = null;
        try
        {
            foreach (var body in _sleepEvents)
            {
                if (!body.IsInsideTree) continue;
                try { body.RaiseSleepingStateChanged(); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
            _sleepEvents.Clear();
            for (var index = 0; index < _contactEvents.Count; index++)
            {
                var change = _contactEvents[index];
                if (!change.Receiver.IsInsideTree || !change.Receiver.ContactMonitor ||
                    (change.Entered && !change.Receiver.ContainsContact(change.Other))) continue;
                try { change.Receiver.RaiseContact(change.Other, change.Entered); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        finally
        {
            _sleepEvents.Clear();
            _contactEvents.Clear();
            _dispatchingContacts = false;
        }
        if (errors is not null) throw new AggregateException("Rigid-body contact callbacks failed.", errors);
    }

    private void DispatchOverlapEvents()
    {
        if (_dispatching || _overlapEvents.Count == 0) return;
        _dispatching = true;
        List<Exception>? errors = null;
        try
        {
            for (var index = 0; index < _overlapEvents.Count; index++)
            {
                var change = _overlapEvents[index];
                if (!change.Area.IsInsideTree || (change.Entered && !change.Area.ContainsOverlap(change.Other))) continue;
                try { change.Area.RaiseOverlap(change.Other, change.Entered); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        finally
        {
            _overlapEvents.Clear();
            _dispatching = false;
        }
        if (errors is not null) throw new AggregateException("Physics-area overlap callbacks failed.", errors);
    }

    internal static void SetMaterial(ref B2ShapeDef definition, PhysicsMaterial? material)
    {
        var friction = material?.ComputedFriction ?? 1f;
        var bounce = material?.ComputedBounce ?? 0f;
        definition.material.friction = MathF.Abs(friction);
        definition.material.restitution = MathF.Abs(bounce);
        definition.material.userMaterialId = (friction < 0 ? RoughMaterial : 0) |
            (bounce < 0 ? AbsorbentMaterial : 0);
    }

    private static float CombineFriction(float a, ulong aFlags, float b, ulong bFlags) =>
        MathF.Abs(MathF.Min((aFlags & RoughMaterial) != 0 ? -a : a,
            (bFlags & RoughMaterial) != 0 ? -b : b));

    private static float CombineBounce(float a, ulong aFlags, float b, ulong bFlags) =>
        Math.Clamp(((aFlags & AbsorbentMaterial) != 0 ? -a : a) +
            ((bFlags & AbsorbentMaterial) != 0 ? -b : b), 0f, 1f);
}
