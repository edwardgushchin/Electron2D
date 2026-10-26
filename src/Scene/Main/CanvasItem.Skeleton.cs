namespace Electron2D;

public abstract partial class CanvasItem
{
    private readonly object _canvasItemGate = new();
    private RID _canvasItemRID;
    internal void BindOwnedCanvasIdentity(RID rid) { _canvasItemRID = rid; }
    internal RID AttachedSkeleton { get; private set; }
    /// <summary>Returns this scene-owned stable weak canvas identity, independent of native startup.</summary>
    /// <returns>A borrowed RID valid until node disposal; renderer FreeRID cannot release scene ownership.</returns>
    /// <remarks>The owner or any active processing group may read this logical identity with coordinated lifetime changes.
    /// First use shares the existing item gate and does not create a native graphics resource.</remarks>
    /// <exception cref="InvalidOperationException">An attached query is outside the owner or active processing groups.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public RID GetCanvasItem() { lock (_canvasItemGate) { ThrowIfDisposed(); Tree?.EnsureProcessingReadAccess(); return _canvasItemRID.IsValid() ? _canvasItemRID : _canvasItemRID = RenderingCanvasItemRegistry.Register(this); } }
    internal void AttachSkeleton(RID skeleton) { EnsureMutable(); AttachedSkeleton = skeleton; }
}
