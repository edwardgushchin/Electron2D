using Box2D.NET;

namespace Electron2D;

/// <summary>Retains shared scene/server identity and delegates the current attachment to its selected solver owner.</summary>
internal sealed partial class PhysicsColliderBackend(RID rid, CollisionObject? sceneOwner = null)
{
    private PhysicsColliderImplementation? _implementation;
    internal PhysicsColliderImplementation? Implementation => _implementation;
    private PhysicsColliderImplementation Attached => _implementation ?? throw new InvalidOperationException("The collider has no solver attachment.");
    private CPUPhysicsColliderImplementation CPU => _implementation as CPUPhysicsColliderImplementation ?? throw new InvalidOperationException("The collider has no CPU solver attachment.");
    private GPUPhysicsColliderImplementation GPUAttachment => _implementation as GPUPhysicsColliderImplementation ?? throw new InvalidOperationException("The collider has no resident GPU attachment.");
    internal RID RID => rid;
    internal B2BodyId BodyID => (_implementation as CPUPhysicsColliderImplementation)?.BodyID ?? default;
    internal IReadOnlyList<B2ShapeId> Shapes => (_implementation as CPUPhysicsColliderImplementation)?.Shapes ?? [];
    internal int ShapeCount => _implementation?.ShapeCount ?? 0;
    internal WeakReference<CollisionObject>? SceneOwnerReference => _sceneOwner;
    internal CollisionObject? SceneOwner => _sceneOwner is { } weak && weak.TryGetTarget(out var node) && !node.IsDisposed ? node : null;
    private readonly WeakReference<CollisionObject>? _sceneOwner = sceneOwner is null ? null : new(sceneOwner);

    internal PhysicsSpace? Space { get; private set; }
    internal long AttachmentVersion { get; private set; }
    internal float CollisionPriority { get; private set; } = 1;
    private ulong _canvasInstanceID;
    internal ulong CanvasInstanceID { get => _canvasInstanceID; set { _canvasInstanceID = value; RefreshGPUIdentity(); } }
    internal ObjectIdentity ObjectIdentity { get; private set; } = sceneOwner?.BorrowIdentity() ?? default;

    internal void AttachObject(ElectronObject? value)
    {
        Space?.EnsureQueryAccess();
        if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
        if (value is Node node) { node.Tree?.EnsureOwnerThread(); node.EnsurePhysicsObjectAccess(); }
        var identity = value?.BorrowIdentity() ?? default;
        if (identity.ID == ObjectIdentity.ID) return;
        if (Space is not null) ExternalObjectNode()?.RemovePhysicsObjectBinding(this);
        ObjectIdentity = identity; RefreshGPUIdentity();
        if (Space is not null) ExternalObjectNode()?.AddPhysicsObjectBinding(this);
    }

    private Node? ExternalObjectNode() => ObjectIdentity.RawTarget is Node node &&
        !(_sceneOwner is { } weak && weak.TryGetTarget(out var scene) && ReferenceEquals(scene, node)) ? node : null;

    internal static void ValidateCollisionPriority(float value)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
    }
    internal void SetCollisionPriority(float value)
    {
        Space?.EnsureQueryAccess(); ValidateCollisionPriority(value); CollisionPriority = value;
        if (_implementation is GPUPhysicsColliderImplementation gpu) gpu.SetCollisionPriority(value);
    }

    internal void Attach(PhysicsSpace space, Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)
    {
        if (Space is not null) throw new InvalidOperationException("A collider already belongs to a physics world.");
        if (ObjectIdentity.Target is Node node) { node.Tree?.EnsureOwnerThread(); node.EnsurePhysicsObjectAccess(); }
        var version = checked(AttachmentVersion + 1);
        _implementation = space.BackendImplementation.CreateCollider(this);
        Space = space;
        try
        {
            _implementation.Attach(position, rotation, configuration);
            AttachmentVersion = version;
            ExternalObjectNode()?.AddPhysicsObjectBinding(this);
        }
        catch (Exception error)
        {
            try { Detach(); }
            catch (Exception cleanup) { throw new AggregateException("Collider attachment and cleanup failed.", error, cleanup); }
            throw;
        }
    }

    internal void Detach()
    {
        if (Space is null) return;
        Exception? bindingError = null;
        try { ExternalObjectNode()?.RemovePhysicsObjectBinding(this); }
        catch (Exception error) { bindingError = error; }
        try { _implementation!.Detach(); }
        catch (Exception cleanup)
        {
            if (bindingError is not null) throw new AggregateException("Collider binding and solver cleanup failed.", bindingError, cleanup);
            throw;
        }
        finally { _implementation = null; Space = null; }
        if (bindingError is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(bindingError).Throw();
    }
    internal bool HasMotionMode(PhysicsServer.BodyMode mode) => Attached.HasMotionMode(mode);
    internal void SetMotionMode(PhysicsServer.BodyMode mode) => Attached.SetMotionMode(mode);
    internal void SetContactReporting(bool enabled) => Attached.SetContactReporting(enabled);
    internal void UpdateFilter(uint layer, uint mask, bool wakeBody) => Attached.UpdateFilter(layer, mask, wakeBody);

    internal void RebuildShapes(IReadOnlyList<CollisionObject.ShapeSlot> slots, uint layer, uint mask,
        bool sensor, float density, float friction = 1, float bounce = 0) => Attached.RebuildShapes(slots, layer, mask, sensor, density, friction, bounce);
    internal void RebuildShapes(IReadOnlyList<PhysicsServerCollider.ShapeSlot> slots, uint layer, uint mask,
        bool sensor, float density, float friction = 1, float bounce = 0) => Attached.RebuildShapes(slots, layer, mask, sensor, density, friction, bounce);
}
