namespace Electron2D;

public abstract partial class CanvasItem
{
    private readonly object _canvasItemRIDGate = new();
    private RID _canvasItemRID;
    internal RID AttachedSkeleton { get; private set; }
    /// <summary>Returns this scene-owned stable weak canvas identity, independent of native startup.</summary>
    /// <returns>A borrowed RID valid until node disposal; renderer FreeRID cannot release scene ownership.</returns>
    /// <exception cref="InvalidOperationException">An attached node is read off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public RID GetCanvasItem() { lock (_canvasItemRIDGate) { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _canvasItemRID.IsValid() ? _canvasItemRID : _canvasItemRID = RenderingCanvasItemRegistry.Register(this); } }
    internal void AttachSkeleton(RID skeleton) { EnsureMutable(); AttachedSkeleton = skeleton; }
}
