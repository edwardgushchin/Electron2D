namespace Electron2D;

/// <summary>A live owner-thread view of one attached physics body and its last solved contacts.</summary>
/// <remarks>The server creates and caches built-in views; consumer implementations derive from PhysicsDirectBodyStateExtension. It owns no body or world. Detachment, replacement or
/// disposal invalidates access; a later attachment receives a new view. Access is permitted outside
/// solver execution, including post-solver integration callbacks. Backend storage and contact traversal stay inside the attachment adapter;
/// this view retains only its attachment identity and public contact values. Contact positions, normals and velocities
/// use global axes; the word local identifies this body rather than the collider. Caller disposal affects only the view
/// and is rejected inside a borrowed callback. Solved contacts are fully captured before user callbacks and remain
/// unchanged by subsequent pose or fixture edits. Contact-limit assignment explicitly clears the retained point count. Live field reads retain the attachment
/// internally and validate its lifetime before access; zero-contact views do not request contact snapshots. Contact impulses include all solver intervals of the completed outer physics step.</remarks>
public class PhysicsDirectBodyState : ElectronObject
{
    private readonly PhysicsBodyRuntime _runtime;
    private readonly PhysicsSpace _space;
    private readonly PhysicsColliderBackend? _extensionBackend;
    private readonly long _extensionAttachment;
    internal PhysicsBodyRuntime Runtime => _runtime;
    internal PhysicsSpace Space => _space;
    private Contact[] _contacts = [];
    private int _contactCount;
    private int _callbackDepth;
    internal readonly record struct Contact(RID Collider, ulong ColliderID, int LocalShape, int ColliderShape,
        Vector2 LocalPoint, Vector2 ColliderPoint, Vector2 Normal, Vector2 LocalVelocity, Vector2 ColliderVelocity, Vector2 Impulse,
        float Depth, WeakReference<CollisionObject>? ColliderOwner, ObjectIdentity Identity = default)
    {
        internal CollisionObject? SceneCollider => ColliderOwner is { } weak && weak.TryGetTarget(out var node) && !node.IsDisposed ? node : null;
    }
    internal ReadOnlySpan<Contact> CapturedContacts => _contacts.AsSpan(0, _contactCount);

    internal PhysicsDirectBodyState(PhysicsBodyRuntime runtime, PhysicsSpace space)
    {
        _runtime = runtime; _space = space;
        PrepareContacts(runtime.ContactLimit);
    }
    internal PhysicsDirectBodyState(RID body)
    {
        _runtime = PhysicsServer.Service.BodyRuntime(body);
        _space = _runtime.Space ?? throw new InvalidOperationException("A body extension requires a current physics attachment.");
        _ = PhysicsServer.Service.BodyGetDirectStateCore(body);
        _extensionBackend = _runtime.Backend; _extensionAttachment = _extensionBackend.AttachmentVersion;
    }
    internal bool CallbackActive => _callbackDepth != 0;
    internal bool HasCapturedContacts => _contactCount != 0;
    internal void BeginCallback() => _callbackDepth++;
    internal void EndCallback() => _callbackDepth--;

    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        if (_callbackDepth != 0) throw new InvalidOperationException("A borrowed body view cannot be disposed inside its callback.");
        base.ValidateDisposal();
    }
    internal void Access()
    {
        ThrowIfDisposed();
        _space.EnsureQueryAccess();
        if (_extensionBackend is null) _runtime.ValidateView(this, _space);
        else _runtime.ValidateAttachment(_space, _extensionBackend, _extensionAttachment);
    }
    private static void ValidateTransform(Transform value)
    {
        if (!value.IsFinite() || !value.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(value.Skew))
            throw new ArgumentException("A body transform requires finite translation, unit scale and zero skew.", nameof(value));
    }
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
    /// <remarks>Reads include virtual surface rotation. On a StaticBody, assignment updates its stored
    /// constant surface speed; on an AnimatableBody it also replaces the current target-derived component.</remarks>
    public float AngularVelocity
    {
        get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadAngularVelocity() : _runtime.ViewAngularVelocity; }
        set { Access(); Finite(value); if (this is PhysicsDirectBodyStateExtension extension) { extension.WriteAngularVelocity(value); return; } _runtime.SetAngularVelocity(value); }
    }

    /// <summary>Gets or sets global-axis velocity in scene units per second; assignment wakes a dynamic body.</summary>
    /// <value>Global-axis velocity in scene units per second; assignment wakes a dynamic body.</value>
    /// <remarks>Reads include virtual surface motion. On a StaticBody, assignment updates its stored
    /// constant surface velocity; on an AnimatableBody it also replaces the current target-derived component.</remarks>
    public Vector2 LinearVelocity
    {
        get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadLinearVelocity() : _runtime.ViewLinearVelocity; }
        set { Access(); Finite(value); if (this is PhysicsDirectBodyStateExtension extension) { extension.WriteLinearVelocity(value); return; } _runtime.SetLinearVelocity(value); }
    }

    /// <summary>Gets center-of-mass offset from the body origin along global axes, in scene units.</summary>
    /// <value>Center-of-mass offset from the body origin along global axes, in scene units.</value>
    public Vector2 CenterOfMass { get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadCenterOfMass() : _runtime.ViewCenterOfMass; } }

    /// <summary>Gets center-of-mass offset in the body local coordinates, in scene units.</summary>
    /// <value>Center-of-mass offset in the body local coordinates, in scene units.</value>
    public Vector2 CenterOfMassLocal { get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadCenterOfMassLocal() : _runtime.ViewCenterOfMassLocal; } }

    /// <summary>Gets inverse dynamic mass in reciprocal kilograms; zero for static or kinematic bodies.</summary>
    /// <value>Inverse dynamic mass in reciprocal kilograms; zero for static or kinematic bodies.</value>
    public float InverseMass { get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadInverseMass() : _runtime.ViewInverseMass; } }

    /// <summary>Gets inverse rotational inertia in reciprocal kilograms times squared scene units; zero when rotation is locked.</summary>
    /// <value>Inverse rotational inertia in reciprocal kilograms times squared scene units; zero when rotation is locked.</value>
    public float InverseInertia { get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadInverseInertia() : _runtime.ViewInverseInertia; } }

    /// <summary>Gets or sets whether the body is asleep; setting false wakes it.</summary>
    /// <value>Whether the body is asleep; setting false wakes it.</value>
    /// <remarks>Explicit sleep clears dynamic velocity. Static and kinematic roles ignore sleep assignments.</remarks>
    public bool Sleeping { get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadSleeping() : _runtime.ViewSleeping; } set { Access(); if (this is PhysicsDirectBodyStateExtension extension) { extension.WriteSleeping(value); return; } _runtime.SetSleeping(value); } }

    /// <summary>Gets the last nonzero physics step in seconds; zero before the first step.</summary>
    /// <value>The last nonzero physics step in seconds; zero before the first step.</value>
    public float Step { get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadStep() : _space.LastStep; } }

    /// <summary>Gets last resolved gravity including body gravity scale, in scene units per squared second.</summary>
    /// <value>Last resolved gravity including body gravity scale, in scene units per squared second.</value>
    public Vector2 TotalGravity { get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadTotalGravity() : _runtime.Owners.Scene?.EffectiveGravity ?? _runtime.Gravity; } }

    /// <summary>Gets last resolved linear damping per second.</summary>
    /// <value>Last resolved linear damping per second.</value>
    public float TotalLinearDamp { get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadTotalLinearDamp() : _runtime.Owners.Scene is RigidBody rigid ? rigid.ResolvedLinearDamp : _runtime.LinearDamp; } }

    /// <summary>Gets last resolved angular damping per second.</summary>
    /// <value>Last resolved angular damping per second.</value>
    public float TotalAngularDamp { get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadTotalAngularDamp() : _runtime.Owners.Scene is RigidBody rigid ? rigid.ResolvedAngularDamp : _runtime.AngularDamp; } }

    /// <summary>Gets or sets the 32 body collision-category bits.</summary>
    /// <value>The 32 body collision-category bits.</value>
    public uint CollisionLayer { get { Access(); if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadCollisionLayer(); var owner = _runtime.Owners; return owner.Scene?.CollisionLayer ?? owner.Server!.CollisionLayer; } set { Access(); if (this is PhysicsDirectBodyStateExtension extension) { extension.WriteCollisionLayer(value); return; } var owner = _runtime.Owners; if (owner.Scene is { } scene) scene.CollisionLayer = value; else owner.Server!.SetFilter(value, owner.Server.CollisionMask); } }

    /// <summary>Gets or sets the 32 accepted body collision-category bits.</summary>
    /// <value>The 32 accepted body collision-category bits.</value>
    public uint CollisionMask { get { Access(); if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadCollisionMask(); var owner = _runtime.Owners; return owner.Scene?.CollisionMask ?? owner.Server!.CollisionMask; } set { Access(); if (this is PhysicsDirectBodyStateExtension extension) { extension.WriteCollisionMask(value); return; } var owner = _runtime.Owners; if (owner.Scene is { } scene) scene.CollisionMask = value; else owner.Server!.SetFilter(owner.Server.CollisionLayer, value); } }

    /// <summary>Gets or sets finite global body pose; assignment requires unit scale and zero skew.</summary>
    /// <value>Finite global body pose; assignment requires unit scale and zero skew.</value>
    /// <remarks>Raw kinematic transforms after their first pose queue a target for the next nonzero active step.
    /// Scene bodies retain their own transform presentation policy.</remarks>
    public Transform Transform { get { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.ReadTransform() : _runtime.ViewTransform; } set { Access(); ValidateTransform(value); if (this is PhysicsDirectBodyStateExtension extension) { extension.WriteTransform(value); return; } _runtime.SetTransform(value); } }

    /// <summary>Gets the persistent global force in scene units times kilograms per squared second.</summary>
    /// <returns>Gets the persistent global force in scene units times kilograms per squared second.</returns>
    public Vector2 GetConstantForce() { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.InvokeGetConstantForce() : _runtime.Owners.Scene is RigidBody rigid ? rigid.ConstantForce : _runtime.ConstantForce; }

    /// <summary>Gets persistent torque in kilograms times squared scene units per squared second.</summary>
    /// <returns>Gets persistent torque in kilograms times squared scene units per squared second.</returns>
    public float GetConstantTorque() { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.InvokeGetConstantTorque() : _runtime.Owners.Scene is RigidBody rigid ? rigid.ConstantTorque : _runtime.ConstantTorque; }

    /// <summary>Gets the last solved contact-point count, capped by the body contact limit.</summary>
    /// <returns>Gets the last solved contact-point count, capped by the body contact limit.</returns>
    public int GetContactCount() { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.InvokeGetContactCount() : _contactCount; }

    /// <summary>Gets the live direct-query view of this body space.</summary>
    /// <returns>Gets the live direct-query view of this body space.</returns>
    public PhysicsDirectSpaceState GetSpaceState() { Access(); return this is PhysicsDirectBodyStateExtension extension ? extension.InvokeGetSpaceState() : PhysicsServer.SpaceGetDirectState(_space.RID); }

    /// <summary>Gets point velocity at a global-axis offset from the body origin, in scene units per second.</summary>
    /// <param name="localPosition">Finite global-axis offset from the body origin in scene units.</param>
    /// <returns>Gets point velocity at a global-axis offset from the body origin, in scene units per second.</returns>
    public Vector2 GetVelocityAtLocalPosition(Vector2 localPosition)
    {
        Access(); Finite(localPosition); return this is PhysicsDirectBodyStateExtension extension ? extension.InvokeGetVelocityAtLocalPosition(localPosition) : _runtime.GetViewPointVelocity(localPosition);
    }

    /// <summary>Gets the contact collider RID.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the contact collider RID.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public RID GetContactCollider(int contactIndex) { if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadContact(contactIndex).ColliderRID; return At(contactIndex).Collider; }

    /// <summary>Gets the collider instance ID, or zero for a server-only collider.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the collider instance ID, or zero for a server-only collider.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public ulong GetContactColliderID(int contactIndex) { if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadContact(contactIndex).ColliderID; return At(contactIndex).ColliderID; }

    /// <summary>Gets this body shape index.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets this body shape index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public int GetContactLocalShape(int contactIndex) { if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadContact(contactIndex).LocalShape; return At(contactIndex).LocalShape; }

    /// <summary>Gets the collider shape index.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the collider shape index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public int GetContactColliderShape(int contactIndex) { if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadContact(contactIndex).ColliderShape; return At(contactIndex).ColliderShape; }

    /// <summary>Gets this body contact position in global scene coordinates.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets this body contact position in global scene coordinates.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactLocalPosition(int contactIndex) { if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadContact(contactIndex).LocalPosition; return At(contactIndex).LocalPoint; }

    /// <summary>Gets the collider contact position in global scene coordinates.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the collider contact position in global scene coordinates.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactColliderPosition(int contactIndex) { if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadContact(contactIndex).ColliderPosition; return At(contactIndex).ColliderPoint; }

    /// <summary>Gets the global contact normal pointing away from the collider.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the global contact normal pointing away from the collider.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactLocalNormal(int contactIndex) { if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadContact(contactIndex).Normal; return At(contactIndex).Normal; }

    /// <summary>Gets this body global-axis velocity at its contact point.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets this body global-axis velocity at its contact point.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactLocalVelocityAtPosition(int contactIndex) { if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadContact(contactIndex).LocalVelocity; return At(contactIndex).LocalVelocity; }

    /// <summary>Gets the collider global-axis velocity at its contact point.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the collider global-axis velocity at its contact point.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactColliderVelocityAtPosition(int contactIndex) { if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadContact(contactIndex).ColliderVelocity; return At(contactIndex).ColliderVelocity; }

    /// <summary>Gets the contact impulse applied to this body in scene units times kilograms per second.</summary>
    /// <remarks>Normal and signed tangential impulses include warm starting and all solver substeps and
    /// internal kinematic intervals. Contacts that were not solved this frame report zero impulse.</remarks>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets the contact impulse applied to this body in scene units times kilograms per second.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public Vector2 GetContactImpulse(int contactIndex) { if (this is PhysicsDirectBodyStateExtension extension) return extension.ReadContact(contactIndex).Impulse; return At(contactIndex).Impulse; }

    /// <summary>Gets a live scene collider, or null for a server-only or released object.</summary>
    /// <param name="contactIndex">Zero-based retained contact index.</param>
    /// <returns>Gets a live scene collider, or null for a server-only or released object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the retained snapshot.</exception>
    public CollisionObject? GetContactColliderObject(int contactIndex) { return PhysicsServer.Service.ResolveSceneObject(this is PhysicsDirectBodyStateExtension extension ? extension.ReadContact(contactIndex).ColliderRID : At(contactIndex).Collider); }

    /// <summary>Returns the sampled, weakly borrowed object association of a retained contact.</summary>
    /// <typeparam name="T">The desired engine-object type.</typeparam>
    /// <param name="contactIndex">Index in the completed contact snapshot.</param>
    /// <returns>The live assigned object of that type, or null if absent, incompatible, disposed or collected.</returns>
    public T? GetContactColliderObject<T>(int contactIndex) where T : ElectronObject => (this is PhysicsDirectBodyStateExtension extension ? extension.ReadContact(contactIndex).ColliderObject : At(contactIndex).Identity.Target) as T;

    /// <summary>Sets the persistent global force, replacing the previous value.</summary>
    /// <param name="force">Finite force in scene units times kilograms per squared second.</param>
    public void SetConstantForce(Vector2 force)
    {
        Access(); Finite(force); if (this is PhysicsDirectBodyStateExtension extension) { extension.InvokeSetConstantForce(force); return; }
        _runtime.SetViewConstantForce(force);
    }
    /// <summary>Sets persistent torque, replacing the previous value.</summary>
    /// <param name="torque">Finite torque in kilograms times squared scene units per squared second.</param>
    public void SetConstantTorque(float torque)
    {
        Access(); Finite(torque); if (this is PhysicsDirectBodyStateExtension extension) { extension.InvokeSetConstantTorque(torque); return; }
        _runtime.SetViewConstantTorque(torque);
    }
    /// <summary>Adds a persistent central force without adding torque.</summary>
    /// <param name="force">Finite force in scene units times kilograms per squared second, zero by default.</param>
    public void AddConstantCentralForce(Vector2 force = default) { Access(); Finite(force); if (this is PhysicsDirectBodyStateExtension extension) extension.InvokeAddConstantCentralForce(force); else SetConstantForce(GetConstantForce() + force); }
    /// <summary>Adds persistent rotational force.</summary>
    /// <param name="torque">Finite torque in kilograms times squared scene units per squared second.</param>
    public void AddConstantTorque(float torque) { Access(); Finite(torque); if (this is PhysicsDirectBodyStateExtension extension) extension.InvokeAddConstantTorque(torque); else SetConstantTorque(GetConstantTorque() + torque); }
    /// <summary>Adds a persistent positioned force and its moment.</summary>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <param name="position">Finite global-axis offset from the body origin, zero by default.</param>
    public void AddConstantForce(Vector2 force, Vector2 position = default)
    {
        Access(); Finite(force); Finite(position); if (this is PhysicsDirectBodyStateExtension extension) { extension.InvokeAddConstantForce(force, position); return; }
        PhysicsServer.BodyAddConstantForce(_runtime.RID, force, position);
    }
    /// <summary>Applies a force accumulator for the next solver step without adding torque.</summary>
    /// <param name="force">Finite global force in scene units times kilograms per squared second, zero by default.</param>
    public void ApplyCentralForce(Vector2 force = default)
    {
        Access(); Finite(force); if (this is PhysicsDirectBodyStateExtension extension) { extension.InvokeApplyCentralForce(force); return; }
        PhysicsServer.BodyApplyCentralForce(_runtime.RID, force);
    }
    /// <summary>Applies an instantaneous central impulse.</summary>
    /// <param name="impulse">Finite global impulse in scene units times kilograms per second.</param>
    public void ApplyCentralImpulse(Vector2 impulse) { Access(); Finite(impulse); if (this is PhysicsDirectBodyStateExtension extension) { extension.InvokeApplyCentralImpulse(impulse); return; } PhysicsServer.BodyApplyCentralImpulse(_runtime.RID, impulse); }
    /// <summary>Applies a positioned force accumulator for the next solver step.</summary>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <param name="position">Finite global-axis offset from the body origin, zero by default.</param>
    public void ApplyForce(Vector2 force, Vector2 position = default)
    {
        Access(); Finite(force); Finite(position); if (this is PhysicsDirectBodyStateExtension extension) { extension.InvokeApplyForce(force, position); return; }
        PhysicsServer.BodyApplyForce(_runtime.RID, force, position);
    }
    /// <summary>Applies an instantaneous positioned impulse.</summary>
    /// <param name="impulse">Finite global impulse in scene units times kilograms per second.</param>
    /// <param name="position">Finite global-axis offset from the body origin, zero by default.</param>
    /// <exception cref="ArgumentOutOfRangeException">An input or resulting velocity is nonfinite.</exception>
    public void ApplyImpulse(Vector2 impulse, Vector2 position = default)
    {
        Access(); Finite(impulse); Finite(position); if (this is PhysicsDirectBodyStateExtension extension) { extension.InvokeApplyImpulse(impulse, position); return; }
        PhysicsServer.BodyApplyImpulse(_runtime.RID, impulse, position);
    }
    /// <summary>Applies torque accumulated for the next solver step.</summary>
    /// <param name="torque">Finite torque in kilograms times squared scene units per squared second.</param>
    public void ApplyTorque(float torque) { Access(); Finite(torque); if (this is PhysicsDirectBodyStateExtension extension) { extension.InvokeApplyTorque(torque); return; } PhysicsServer.BodyApplyTorque(_runtime.RID, torque); }
    /// <summary>Applies an instantaneous torque impulse.</summary>
    /// <param name="impulse">Finite angular impulse in kilograms times squared scene units per second.</param>
    public void ApplyTorqueImpulse(float impulse)
    {
        Access(); Finite(impulse); if (this is PhysicsDirectBodyStateExtension extension) { extension.InvokeApplyTorqueImpulse(impulse); return; }
        PhysicsServer.BodyApplyTorqueImpulse(_runtime.RID, impulse);
    }
    /// <summary>Applies one tick of resolved gravity followed by linear and angular damping to velocity.</summary>
    /// <remarks>Each call applies another tick. Accumulated and constant forces are not included.
    /// This operation is useful with custom integration; it wakes a dynamic body and preserves solver contact response.</remarks>
    public void IntegrateForces()
    {
        Access(); if (this is PhysicsDirectBodyStateExtension extension) { extension.InvokeIntegrateForces(); return; }
        var linear = (LinearVelocity + TotalGravity * Step) * MathF.Max(0, 1 - Step * TotalLinearDamp);
        var angular = AngularVelocity * MathF.Max(0, 1 - Step * TotalAngularDamp);
        Finite(linear); Finite(angular);
        LinearVelocity = linear; AngularVelocity = angular;
    }
    internal void PrepareContacts(int limit)
    {
        if (_contacts.Length < limit) Array.Resize(ref _contacts, limit);
        _contactCount = 0;
    }

    internal void CaptureContacts()
    {
        _contactCount = 0;
        _runtime.CaptureViewContacts(this);
    }

    internal void BeginContactSnapshot() => _contactCount = 0;
    internal void RestoreContacts(ReadOnlySpan<Contact> contacts)
    { PrepareContacts(contacts.Length); contacts.CopyTo(_contacts); _contactCount = contacts.Length; }

    internal int SelectContactSlot(float depth, int limit)
    {
        if (_contactCount < limit) return _contactCount++;
        // ponytail: O(candidates * limit); use a retained min-heap if large report limits become a measured bottleneck.
        var slot = 0;
        for (var i = 1; i < _contactCount; i++) if (_contacts[i].Depth < _contacts[slot].Depth) slot = i;
        return depth > _contacts[slot].Depth ? slot : -1;
    }

    internal void StoreContact(int slot, in Contact contact) => _contacts[slot] = contact;
}
