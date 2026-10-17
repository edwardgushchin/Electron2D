namespace Electron2D;

/// <summary>Retains shared scene/server identity and delegates attachment operations without exposing native solver handles or publication caches.</summary>
internal sealed partial class PhysicsColliderBackend(RID rid, CollisionObject? sceneOwner = null)
{
    private static int _attachmentCount;
    internal static int AttachmentCount => Volatile.Read(ref _attachmentCount);
    internal static bool HasAnyAttachments => AttachmentCount != 0;
    private PhysicsColliderImplementation? _implementation;
    internal PhysicsColliderImplementation? Implementation => _implementation;
    private PhysicsColliderImplementation Attached => _implementation ?? throw new InvalidOperationException("The collider has no solver attachment.");
    internal RID RID => rid;
    internal int ShapeCount => _implementation?.ShapeCount ?? 0;
    internal WeakReference<CollisionObject>? SceneOwnerReference => _sceneOwner;
    internal CollisionObject? SceneOwner => _sceneOwner is { } weak && weak.TryGetTarget(out var node) && !node.IsDisposed ? node : null;
    private readonly WeakReference<CollisionObject>? _sceneOwner = sceneOwner is null ? null : new(sceneOwner);

    internal PhysicsSpace? Space { get; private set; }
    internal long AttachmentVersion { get; private set; }
    internal float CollisionPriority { get; private set; } = 1;
    private ulong _canvasInstanceID;
    internal ulong CanvasInstanceID
    {
        get => _canvasInstanceID;
        set { Space?.EnsureExtensionHierarchyChange(); _canvasInstanceID = value; RefreshIdentity(); }
    }
    internal ObjectIdentity ObjectIdentity { get; private set; } = sceneOwner?.BorrowIdentity() ?? default;

    internal void AttachObject(ElectronObject? value)
    {
        Space?.EnsureQueryAccess();
        Space?.EnsureExtensionHierarchyChange();
        if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
        if (value is Node node) { node.Tree?.EnsureOwnerThread(); node.EnsurePhysicsObjectAccess(); }
        var identity = value?.BorrowIdentity() ?? default;
        if (identity.ID == ObjectIdentity.ID) return;
        if (Space is not null) ExternalObjectNode()?.RemovePhysicsObjectBinding(this);
        ObjectIdentity = identity;
        if (Space is not null) ExternalObjectNode()?.AddPhysicsObjectBinding(this);
        RefreshIdentity();
    }

    private Node? ExternalObjectNode() => ObjectIdentity.RawTarget is Node node &&
        !(_sceneOwner is { } weak && weak.TryGetTarget(out var scene) && ReferenceEquals(scene, node)) ? node : null;

    internal static void ValidateCollisionPriority(float value)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
    }
    internal void SetCollisionPriority(float value)
    {
        Space?.EnsureQueryAccess(); Space?.EnsureExtensionHierarchyChange(); ValidateCollisionPriority(value); CollisionPriority = value;
        PublishCollisionPriority();
    }

    private bool _identityDirty;
    private bool _priorityDirty;
    private void RefreshIdentity()
    {
        _identityDirty = true;
        try { _implementation?.RefreshIdentity(); _identityDirty = false; }
        catch { Space?.MarkColliderMetadataDirty(); throw; }
    }
    private void PublishCollisionPriority()
    {
        _priorityDirty = true;
        try { _implementation?.SetCollisionPriority(CollisionPriority); _priorityDirty = false; }
        catch { Space?.MarkColliderMetadataDirty(); throw; }
    }

    internal void Attach(PhysicsSpace space, Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)
    {
        if (Space is not null) throw new InvalidOperationException("A collider already belongs to a physics world.");
        if (ObjectIdentity.Target is Node node) { node.Tree?.EnsureOwnerThread(); node.EnsurePhysicsObjectAccess(); }
        var version = checked(AttachmentVersion + 1);
        Interlocked.Increment(ref _attachmentCount);
        Space = space;
        try
        {
            ExternalObjectNode()?.AddPhysicsObjectBinding(this);
            _implementation = space.BackendImplementation.CreateCollider(this);
            _implementation.Attach(position, rotation, configuration);
            AttachmentVersion = version;
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
        try { _implementation?.Detach(); }
        catch (Exception cleanup)
        {
            if (bindingError is not null) throw new AggregateException("Collider binding and solver cleanup failed.", bindingError, cleanup);
            throw;
        }
        finally
        {
            _implementation = null;
            _pendingMotion = null;
            _filterDirty = false;
            _identityDirty = false; _priorityDirty = false;
            if (Space is not null) { Space = null; Interlocked.Decrement(ref _attachmentCount); }
        }
        if (bindingError is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(bindingError).Throw();
    }
    internal void PrepareDefaultForces(in PhysicsBodyForceConfiguration configuration) => Attached.PrepareDefaultForces(configuration);
    internal bool HasMotionMode(PhysicsServer.BodyMode mode) => Attached.HasMotionMode(mode);
    internal void SetMotionMode(PhysicsServer.BodyMode mode) => Attached.SetMotionMode(mode);
    internal void SetContactReporting(bool enabled) => Attached.SetContactReporting(enabled);
    private bool _filterDirty;
    internal void UpdateFilter(uint layer, uint mask, bool wakeBody)
    {
        _filterDirty = true;
        try { Attached.UpdateFilter(layer, mask, wakeBody); _filterDirty = false; }
        catch { Space?.MarkColliderMetadataDirty(); throw; }
    }
    internal void PrepareMetadata(uint layer, uint mask)
    {
        if (_identityDirty) RefreshIdentity();
        if (_priorityDirty) PublishCollisionPriority();
        if (_filterDirty) UpdateFilter(layer, mask, wakeBody: false);
    }

}
