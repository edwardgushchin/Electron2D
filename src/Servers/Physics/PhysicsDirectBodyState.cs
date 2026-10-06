using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

/// <summary>A live owner-thread view of one attached physics body and its last solved contacts.</summary>
/// <remarks>The server creates and caches this view. It owns no body or world. Detachment, replacement or
/// disposal invalidates access; a later attachment receives a new view. Access is permitted outside native
/// solver execution, including post-solver integration callbacks. Contact positions, normals and velocities
/// use global axes; the word local identifies this body rather than the collider. Caller disposal affects only the view
/// and is rejected inside a borrowed callback. Whole-step tangential contact impulse aggregation remains incomplete.</remarks>
public sealed class PhysicsDirectBodyState : ElectronObject
{
    private readonly PhysicsBodyRuntime _runtime;
    private readonly PhysicsSpace _space;
    private readonly B2BodyId _id;
    private B2ContactData[] _rawContacts = [];
    private Contact[] _contacts = [];
    private int _contactCount;
    private int _callbackDepth;
    private readonly record struct Contact(RID Collider, ulong ColliderID, int LocalShape, int ColliderShape,
        Vector2 LocalPoint, Vector2 ColliderPoint, Vector2 Normal, Vector2 LocalVelocity, Vector2 ColliderVelocity, Vector2 Impulse);

    internal PhysicsDirectBodyState(PhysicsBodyRuntime runtime, PhysicsSpace space, B2BodyId id)
    { _runtime = runtime; _space = space; _id = id; PrepareContacts(runtime.ContactLimit); }
    internal bool Matches(PhysicsSpace space, B2BodyId id) => ReferenceEquals(_space, space) && _id == id;
    internal bool CallbackActive => _callbackDepth != 0;
    internal void BeginCallback() => _callbackDepth++;
    internal void EndCallback() => _callbackDepth--;

    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        if (_callbackDepth != 0) throw new InvalidOperationException("A borrowed body view cannot be disposed inside its callback.");
        base.ValidateDisposal();
    }
    private B2BodyId Access()
    {
        ThrowIfDisposed();
        _space.EnsureQueryAccess();
        try
        {
            var owner = _runtime.Owners;
            if ((owner.Scene?.Space ?? owner.Server?.Space) != _space ||
                (owner.Scene?.BackendID ?? owner.Server!.BackendID) != _id)
                throw new ObjectDisposedException(nameof(PhysicsDirectBodyState), "The backend attachment ended.");
        }
        catch (ArgumentException) { throw new ObjectDisposedException(nameof(PhysicsDirectBodyState), "The body was released."); }
        return _id;
    }
    private static Vector2 ToScene(B2Vec2 value) => new(value.X * PhysicsSpace.UnitsPerMeter, value.Y * PhysicsSpace.UnitsPerMeter);
    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void Finite(Vector2 value) { if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); }
    private Contact At(int index)
    {
        Access();
        if ((uint)index >= (uint)_contactCount) throw new ArgumentOutOfRangeException(nameof(index));
        return _contacts[index];
    }

    /// <summary>Gets or sets angular velocity in radians per second; assignment wakes a dynamic body.</summary>
    /// <value>Angular velocity in radians per second; assignment wakes a dynamic body.</value>
    public float AngularVelocity { get { return b2Body_GetAngularVelocity(Access()); } set { var id = Access(); Finite(value); b2Body_SetAwake(id, true); b2Body_SetAngularVelocity(id, value); } }

    /// <summary>Gets or sets global-axis velocity in scene units per second; assignment wakes a dynamic body.</summary>
    /// <value>Global-axis velocity in scene units per second; assignment wakes a dynamic body.</value>
    public Vector2 LinearVelocity { get { return ToScene(b2Body_GetLinearVelocity(Access())); } set { var id = Access(); Finite(value); b2Body_SetAwake(id, true); b2Body_SetLinearVelocity(id, Shape.ToBackend(value)); } }

    /// <summary>Gets center-of-mass offset from the body origin along global axes, in scene units.</summary>
    /// <value>Center-of-mass offset from the body origin along global axes, in scene units.</value>
    public Vector2 CenterOfMass { get { var id = Access(); return ToScene(b2Body_GetWorldCenterOfMass(id) - b2Body_GetPosition(id)); } }

    /// <summary>Gets center-of-mass offset in the body local coordinates, in scene units.</summary>
    /// <value>Center-of-mass offset in the body local coordinates, in scene units.</value>
    public Vector2 CenterOfMassLocal { get { return ToScene(b2Body_GetLocalCenterOfMass(Access())); } }

    /// <summary>Gets inverse dynamic mass in reciprocal kilograms; zero for static or kinematic bodies.</summary>
    /// <value>Inverse dynamic mass in reciprocal kilograms; zero for static or kinematic bodies.</value>
    public float InverseMass { get { return PhysicsBodyRuntime.Simulation(Access()).invMass; } }

    /// <summary>Gets inverse rotational inertia in reciprocal kilograms times squared scene units; zero when rotation is locked.</summary>
    /// <value>Inverse rotational inertia in reciprocal kilograms times squared scene units; zero when rotation is locked.</value>
    public float InverseInertia { get { var id = Access(); return b2Body_GetMotionLocks(id).angularZ ? 0 : PhysicsBodyRuntime.Simulation(id).invInertia * 0.0001f; } }

    /// <summary>Gets or sets whether the body is asleep; setting false wakes it.</summary>
    /// <value>Whether the body is asleep; setting false wakes it.</value>
    public bool Sleeping { get { return !b2Body_IsAwake(Access()); } set { b2Body_SetAwake(Access(), !value); } }

    /// <summary>Gets the last nonzero physics step in seconds; zero before the first step.</summary>
    /// <value>The last nonzero physics step in seconds; zero before the first step.</value>
    public float Step { get { Access(); return _space.LastStep; } }

    /// <summary>Gets last resolved gravity including body gravity scale, in scene units per squared second.</summary>
    /// <value>Last resolved gravity including body gravity scale, in scene units per squared second.</value>
    public Vector2 TotalGravity { get { Access(); return _runtime.Owners.Scene?.EffectiveGravity ?? _runtime.Gravity; } }

    /// <summary>Gets last resolved linear damping per second.</summary>
    /// <value>Last resolved linear damping per second.</value>
    public float TotalLinearDamp { get { Access(); return _runtime.Owners.Scene is RigidBody rigid ? rigid.ResolvedLinearDamp : _runtime.LinearDamp; } }

    /// <summary>Gets last resolved angular damping per second.</summary>
    /// <value>Last resolved angular damping per second.</value>
    public float TotalAngularDamp { get { Access(); return _runtime.Owners.Scene is RigidBody rigid ? rigid.ResolvedAngularDamp : _runtime.AngularDamp; } }

    /// <summary>Gets or sets the 32 body collision-category bits.</summary>
    /// <value>The 32 body collision-category bits.</value>
    public uint CollisionLayer { get { Access(); var owner = _runtime.Owners; return owner.Scene?.CollisionLayer ?? owner.Server!.CollisionLayer; } set { Access(); var owner = _runtime.Owners; if (owner.Scene is { } scene) scene.CollisionLayer = value; else owner.Server!.SetFilter(value, owner.Server.CollisionMask); } }

    /// <summary>Gets or sets the 32 accepted body collision-category bits.</summary>
    /// <value>The 32 accepted body collision-category bits.</value>
    public uint CollisionMask { get { Access(); var owner = _runtime.Owners; return owner.Scene?.CollisionMask ?? owner.Server!.CollisionMask; } set { Access(); var owner = _runtime.Owners; if (owner.Scene is { } scene) scene.CollisionMask = value; else owner.Server!.SetFilter(owner.Server.CollisionLayer, value); } }

    /// <summary>Gets or sets finite global body pose; assignment requires unit scale and zero skew.</summary>
    /// <value>Finite global body pose; assignment requires unit scale and zero skew.</value>
    public Transform Transform { get { var id = Access(); return new(b2Rot_GetAngle(b2Body_GetRotation(id)), Vector2.One, 0, ToScene(b2Body_GetPosition(id))); } set { Access(); ValidateTransform(value); var owner = _runtime.Owners; if (owner.Scene is { } scene) { scene.GlobalTransform = value; scene.PrepareBackend(); } else owner.Server!.SetTransform(value); } }

    /// <summary>Gets the persistent global force in scene units times kilograms per squared second.</summary>
    /// <returns>Gets the persistent global force in scene units times kilograms per squared second.</returns>
    public Vector2 GetConstantForce() { Access(); return _runtime.Owners.Scene is RigidBody rigid ? rigid.ConstantForce : _runtime.ConstantForce; }

    /// <summary>Gets persistent torque in kilograms times squared scene units per squared second.</summary>
    /// <returns>Gets persistent torque in kilograms times squared scene units per squared second.</returns>
    public float GetConstantTorque() { Access(); return _runtime.Owners.Scene is RigidBody rigid ? rigid.ConstantTorque : _runtime.ConstantTorque; }

    /// <summary>Gets the last solved contact-point count, capped by the body contact limit.</summary>
    /// <returns>Gets the last solved contact-point count, capped by the body contact limit.</returns>
    public int GetContactCount() { Access(); return _contactCount; }

    /// <summary>Gets the live direct-query view of this body space.</summary>
    /// <returns>Gets the live direct-query view of this body space.</returns>
    public PhysicsDirectSpaceState GetSpaceState() { Access(); return PhysicsServer.SpaceGetDirectState(_space.RID); }

    /// <summary>Gets point velocity at a global-axis offset from the body origin, in scene units per second.</summary>
    /// <param name="localPosition">Finite global-axis offset from the body origin in scene units.</param>
    /// <returns>Gets point velocity at a global-axis offset from the body origin, in scene units per second.</returns>
    public Vector2 GetVelocityAtLocalPosition(Vector2 localPosition)
    {
        var id = Access(); Finite(localPosition);
        var result = ToScene(b2Body_GetWorldPointVelocity(id, WorldPoint(id, localPosition)));
        Finite(result); return result;
    }

    /// <summary>Gets the contact collider RID.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the contact collider RID.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public RID GetContactCollider(int contactIndex) { return At(contactIndex).Collider; }

    /// <summary>Gets the collider instance ID, or zero for a server-only collider.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the collider instance ID, or zero for a server-only collider.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public ulong GetContactColliderID(int contactIndex) { return At(contactIndex).ColliderID; }

    /// <summary>Gets this body shape index.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets this body shape index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public int GetContactLocalShape(int contactIndex) { return At(contactIndex).LocalShape; }

    /// <summary>Gets the collider shape index.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the collider shape index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public int GetContactColliderShape(int contactIndex) { return At(contactIndex).ColliderShape; }

    /// <summary>Gets this body contact position in global scene coordinates.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets this body contact position in global scene coordinates.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactLocalPosition(int contactIndex) { return At(contactIndex).LocalPoint; }

    /// <summary>Gets the collider contact position in global scene coordinates.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the collider contact position in global scene coordinates.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactColliderPosition(int contactIndex) { return At(contactIndex).ColliderPoint; }

    /// <summary>Gets the global contact normal pointing away from the collider.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the global contact normal pointing away from the collider.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactLocalNormal(int contactIndex) { return At(contactIndex).Normal; }

    /// <summary>Gets this body global-axis velocity at its contact point.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets this body global-axis velocity at its contact point.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactLocalVelocityAtPosition(int contactIndex) { return At(contactIndex).LocalVelocity; }

    /// <summary>Gets the collider global-axis velocity at its contact point.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the collider global-axis velocity at its contact point.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactColliderVelocityAtPosition(int contactIndex) { return At(contactIndex).ColliderVelocity; }

    /// <summary>Gets the contact impulse applied to this body in scene units times kilograms per second.</summary>
    /// <remarks>The normal component sums the solver substeps. The tangential component currently describes the
    /// final substep; the backend does not expose its whole-step aggregate.</remarks>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the contact impulse applied to this body in scene units times kilograms per second.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactImpulse(int contactIndex) { return At(contactIndex).Impulse; }

    /// <summary>Gets a live scene collider, or null for a server-only or released object.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets a live scene collider, or null for a server-only or released object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public CollisionObject? GetContactColliderObject(int contactIndex) { return PhysicsServer.Service.ResolveSceneObject(At(contactIndex).Collider); }

    /// <summary>Sets the persistent global force, replacing the previous value.</summary>
    /// <param name="force">Finite force in scene units times kilograms per squared second.</param>
    public void SetConstantForce(Vector2 force)
    {
        var id = Access(); Finite(force);
        if (_runtime.Owners.Scene is RigidBody rigid) rigid.ConstantForce = force;
        else _runtime.ConstantForce = force;
        b2Body_SetAwake(id, true);
    }
    /// <summary>Sets persistent torque, replacing the previous value.</summary>
    /// <param name="torque">Finite torque in kilograms times squared scene units per squared second.</param>
    public void SetConstantTorque(float torque)
    {
        var id = Access(); Finite(torque);
        if (_runtime.Owners.Scene is RigidBody rigid) rigid.ConstantTorque = torque;
        else _runtime.ConstantTorque = torque;
        b2Body_SetAwake(id, true);
    }
    /// <summary>Adds a persistent central force without adding torque.</summary>
    /// <param name="force">Finite force in scene units times kilograms per squared second, zero by default.</param>
    public void AddConstantCentralForce(Vector2 force = default) => SetConstantForce(GetConstantForce() + force);
    /// <summary>Adds persistent rotational force.</summary>
    /// <param name="torque">Finite torque in kilograms times squared scene units per squared second.</param>
    public void AddConstantTorque(float torque) => SetConstantTorque(GetConstantTorque() + torque);
    /// <summary>Adds a persistent positioned force and its moment.</summary>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <param name="position">Finite global-axis offset from the body origin, zero by default.</param>
    public void AddConstantForce(Vector2 force, Vector2 position = default)
    {
        Access();
        PhysicsServer.BodyAddConstantForce(_runtime.RID, force, position);
    }
    /// <summary>Applies a force accumulator for the next solver step without adding torque.</summary>
    /// <param name="force">Finite global force in scene units times kilograms per squared second, zero by default.</param>
    public void ApplyCentralForce(Vector2 force = default)
    {
        Access();
        PhysicsServer.BodyApplyCentralForce(_runtime.RID, force);
    }
    /// <summary>Applies an instantaneous central impulse.</summary>
    /// <param name="impulse">Finite global impulse in scene units times kilograms per second.</param>
    public void ApplyCentralImpulse(Vector2 impulse) { Access(); PhysicsServer.BodyApplyCentralImpulse(_runtime.RID, impulse); }
    /// <summary>Applies a positioned force accumulator for the next solver step.</summary>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <param name="position">Finite global-axis offset from the body origin, zero by default.</param>
    public void ApplyForce(Vector2 force, Vector2 position = default)
    {
        Access();
        PhysicsServer.BodyApplyForce(_runtime.RID, force, position);
    }
    /// <summary>Applies an instantaneous positioned impulse.</summary>
    /// <param name="impulse">Finite global impulse in scene units times kilograms per second.</param>
    /// <param name="position">Finite global-axis offset from the body origin, zero by default.</param>
    /// <exception cref="ArgumentOutOfRangeException">An input or resulting velocity is nonfinite.</exception>
    public void ApplyImpulse(Vector2 impulse, Vector2 position = default)
    {
        Access();
        PhysicsServer.BodyApplyImpulse(_runtime.RID, impulse, position);
    }
    /// <summary>Applies torque accumulated for the next solver step.</summary>
    /// <param name="torque">Finite torque in kilograms times squared scene units per squared second.</param>
    public void ApplyTorque(float torque) { Access(); PhysicsServer.BodyApplyTorque(_runtime.RID, torque); }
    /// <summary>Applies an instantaneous torque impulse.</summary>
    /// <param name="impulse">Finite angular impulse in kilograms times squared scene units per second.</param>
    public void ApplyTorqueImpulse(float impulse)
    {
        Access();
        PhysicsServer.BodyApplyTorqueImpulse(_runtime.RID, impulse);
    }
    /// <summary>Applies one tick of resolved gravity followed by linear and angular damping to velocity.</summary>
    /// <remarks>Each call applies another tick. Accumulated and constant forces are not included.
    /// This operation is useful with custom integration; it wakes a dynamic body and preserves solver contact response.</remarks>
    public void IntegrateForces()
    {
        Access();
        var linear = (LinearVelocity + TotalGravity * Step) * MathF.Max(0, 1 - Step * TotalLinearDamp);
        var angular = AngularVelocity * MathF.Max(0, 1 - Step * TotalAngularDamp);
        Finite(linear); Finite(angular);
        LinearVelocity = linear; AngularVelocity = angular;
    }
    private static B2Vec2 WorldPoint(B2BodyId id, Vector2 offset)
    {
        var point = b2Body_GetPosition(id) + Shape.ToBackend(offset);
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) throw new ArgumentOutOfRangeException(nameof(offset));
        return point;
    }
    private static void ValidateTransform(Transform transform)
    {
        if (!transform.IsFinite() || !transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(transform.Skew))
            throw new ArgumentException("A body pose requires finite translation, unit scale and zero skew.", nameof(transform));
    }
    internal void PrepareContacts(int limit)
    {
        if (_rawContacts.Length < limit) Array.Resize(ref _rawContacts, limit);
        if (_contacts.Length < limit) Array.Resize(ref _contacts, limit);
    }

    internal void CaptureContacts()
    {
        _contactCount = 0;
        var limit = _runtime.ContactLimit;
        if (limit == 0) return;
        var pairs = b2Body_GetContactData(_id, _rawContacts, limit);
        for (var index = 0; index < pairs && _contactCount < limit; index++)
        {
            ref readonly var contact = ref _rawContacts[index];
            var first = b2Shape_GetBody(contact.shapeIdA) == _id;
            var own = b2Shape_GetUserData(first ? contact.shapeIdA : contact.shapeIdB).GetRef<PhysicsFixtureTag>();
            var other = b2Shape_GetUserData(first ? contact.shapeIdB : contact.shapeIdA).GetRef<PhysicsFixtureTag>();
            if (own is null || other is null) continue;
            var collider = b2Shape_GetBody(first ? contact.shapeIdB : contact.shapeIdA);
            var normal = contact.manifold.normal;
            for (var pointIndex = 0; pointIndex < contact.manifold.pointCount && _contactCount < limit; pointIndex++)
            {
                var point = contact.manifold.points[pointIndex];
                var a = point.point - normal * (point.separation * 0.5f);
                var b = point.point + normal * (point.separation * 0.5f);
                var local = first ? a : b;
                var remote = first ? b : a;
                var impulse = normal * point.totalNormalImpulse + b2RightPerp(normal) * point.tangentImpulse;
                _contacts[_contactCount++] = new(other.ColliderRID,
                    PhysicsServer.Service.ResolveSceneObject(other.ColliderRID)?.InstanceID ?? 0,
                    own.ShapeIndex, other.ShapeIndex, ToScene(local), ToScene(remote),
                    new(first ? -normal.X : normal.X, first ? -normal.Y : normal.Y),
                    ToScene(b2Body_GetWorldPointVelocity(_id, local)), ToScene(b2Body_GetWorldPointVelocity(collider, remote)),
                    ToScene(first ? -impulse : impulse));
            }
        }
    }
}
