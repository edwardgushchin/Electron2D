using Box2D.NET;

namespace Electron2D;

/// <summary>A spatial collision object that participates in a scene tree's physics world.</summary>
public abstract partial class PhysicsBody : CollisionObject
{
    private readonly List<ulong> _appliedShapeRevisions = [];
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
    protected PhysicsBody() { InputPickable = false; }

    private PhysicsBodyRuntime? _runtime;
    internal PhysicsBodyRuntime Runtime => _runtime ??= PhysicsServer.Service.BodyRuntime(PhysicsRID);
    internal B2BodyId BackendID => Backend.BodyID;
    internal PhysicsSpace? Space => Backend.Space;
    internal bool HasBackend => Space is not null;
    internal virtual bool CollisionResponseEnabled => true;
    internal uint EffectiveCollisionLayer => CollisionResponseEnabled ? CollisionLayer : 0;
    internal uint EffectiveCollisionMask => CollisionResponseEnabled ? CollisionMask : 0;

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
        if (Space is not null) throw new InvalidOperationException("A body already belongs to a physics world.");
        var transform = GlobalTransform;
        ValidatePhysicsTransform(transform);
        var configuration = CreateBodyConfiguration();
        if (PhysicsMadeStatic)
            configuration = configuration with { Mode = PhysicsServer.BodyMode.Static, LinearVelocity = default, AngularVelocity = 0 };
        _lastPosition = transform.Origin;
        _lastRotation = _validatedRotation;
        Backend.Attach(space, _lastPosition, _lastRotation, configuration);
        _shapesDirty = true;
        try
        {
            RebuildShapes(); Runtime.RestoreSceneState();
            if (configuration.Sleeping && configuration.Mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear) Backend.SetAwake(false);
            if (PhysicsMadeStatic) OnMadeStatic();
        }
        catch { DetachBackend(); throw; }
    }

    internal void DetachBackend()
    {
        PhysicsServer.Service.InvalidateBodyView(PhysicsRID);
        if (Space is null) return;
        if (!Space.HasBackendFailure && this is RigidBody rigid && Backend.HasMotionMode(PhysicsServer.BodyMode.Rigid))
            rigid.OnBackendAdvanced();
        Backend.Detach();
        _appliedShapeRevisions.Clear();
        _shapesDirty = true;
    }

    internal void PrepareBackend()
    {
        if (Space is null) return;
        Backend.PrepareContactPolicy();
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
        Backend.SetPose(position, rotation);

    internal void CompleteBackend()
    {
        if (Space is null || !MovesWithSimulation || PhysicsMadeStatic) return;
        var (scenePosition, sceneRotation) = Backend.GetPose();
        _lastPosition = scenePosition;
        _lastRotation = sceneRotation;
        var current = GlobalTransform;
        var currentRotation = current.X == _validatedTransform.X && current.Y == _validatedTransform.Y
            ? _validatedRotation : current.Rotation;
        if (current.Origin != scenePosition || currentRotation != sceneRotation)
        {
            var solverTransform = Backend.GetTransform();
            GlobalTransform = solverTransform;
            _validatedTransform = solverTransform;
            _validatedRotation = sceneRotation;
            _preparedTransform = solverTransform;
        }
        // Pose notifications may write velocity; sample motion after those callbacks.
        OnBackendAdvanced();
    }

    internal abstract PhysicsServer.BodyMode RequestedBodyMode { get; }
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
        var mode = PhysicsMadeStatic ? PhysicsServer.BodyMode.Static : RequestedBodyMode;
        if (Backend.HasMotionMode(mode)) return;
        if (PhysicsMadeStatic) OnMadeStatic();
        Backend.SetMotionMode(mode);
        OnBodyTypeChanged();
        Runtime.RestoreSceneState();
        MarkShapesDirty();
    }

    internal virtual PhysicsBodyConfiguration CreateBodyConfiguration() => new(RequestedBodyMode);
    internal abstract bool MovesWithSimulation { get; }
    internal virtual void OnBackendAdvanced() { }
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

    internal override void OnCollisionFilterChanged() { if (Space is not null) Backend.UpdateFilter(EffectiveCollisionLayer, EffectiveCollisionMask, wakeBody: true); }

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
        Tree?.EnsurePhysicsParticipationChange(releasing: true);
        PhysicsServer.Service.EnsureJointBodyMembershipChange(PhysicsRID, releasing: true);
        base.ValidateDisposal();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Space?.Remove(this);
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
        var runtime = PhysicsServer.Service.BodyRuntime(PhysicsRID);
        Backend.RebuildShapes(ShapeSlots, EffectiveCollisionLayer, EffectiveCollisionMask, false,
            MovesWithSimulation ? 1f : 0f, runtime.GetFriction(), runtime.GetBounce());

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
