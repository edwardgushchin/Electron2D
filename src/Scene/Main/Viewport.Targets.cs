namespace Electron2D;

public abstract partial class Viewport
{
    private RID _viewportRID;
    private static readonly object ViewportRIDGate = new();
    private static readonly Dictionary<RID, WeakReference<Viewport>> ViewportRIDs = [];
    private static readonly List<RID> StaleViewportRIDs = [];
    private static int _ridRegistrations;
    /// <summary>Returns this viewport's stable borrowed logical rendering identity.</summary>
    /// <returns>An identity independent of native target resize/recreation.</returns>
    /// <remarks>RenderingServer.ViewportGetTexture resolves its live texture. Node disposal releases the identity;
    /// caller-side server FreeRID does not own the viewport.</remarks>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    /// <exception cref="InvalidOperationException">The attached scene is queried off-owner.</exception>
    public RID GetViewportRID()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        lock (ViewportRIDGate)
        {
            if (_viewportRID.IsValid()) return _viewportRID;
            if (++_ridRegistrations >= 256)
            {
                _ridRegistrations = 0; StaleViewportRIDs.Clear(); foreach (var entry in ViewportRIDs) if (!entry.Value.TryGetTarget(out var target) || target.IsDisposed) StaleViewportRIDs.Add(entry.Key); foreach (var stale in StaleViewportRIDs) ViewportRIDs.Remove(stale); StaleViewportRIDs.Clear();
            }
            _viewportRID = RID.Allocate(); ViewportRIDs.Add(_viewportRID, new(this)); return _viewportRID;
        }
    }
    internal static Viewport ResolveViewportRID(RID rid)
    {
        lock (ViewportRIDGate) { if (ViewportRIDs.TryGetValue(rid, out var entry) && entry.TryGetTarget(out var viewport) && !viewport.IsDisposed) return viewport; ViewportRIDs.Remove(rid); throw new ArgumentException("The RID does not identify a live viewport.", nameof(rid)); }
    }
    private void ReleaseViewportRID() { lock (ViewportRIDGate) { ViewportRIDs.Remove(_viewportRID); _viewportRID = default; } }
    private ViewportTexture? _texture;
    internal RenderingServer? RenderingOwner;
    internal event Action? TextureSizeUpdated;
    private bool _transparent;
    internal virtual Transform StretchTransform => Transform.Identity;
    internal Vector2i TextureDimensions => this is SubViewport sub ? sub.Size : RenderingOwner?.ViewportDimensions(this) ?? (this is Window window ? window.Size : default);
    /// <summary>Returns this viewport's live scene-local texture view.</summary>
    /// <returns>A borrowed resource retaining stable RID identity while the target image changes.</returns>
    /// <remarks>The view remains independent of native target allocation. Ordinary drawing samples native images;
    /// GetImage explicitly synchronizes and copies completed pixels. Target loss makes the view unresolved.</remarks>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    /// <exception cref="InvalidOperationException">The attached scene is queried off-owner.</exception>
    public ViewportTexture GetTexture() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _texture is { IsDisposed: false } ? _texture : _texture = new(this); }
    /// <summary>Gets or sets whether this viewport clears to transparent black instead of opaque clear color.</summary>
    /// <value>False initially.</value>
    /// <remarks>Never clear retains prior pixels; changing this policy alone does not erase them.</remarks>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs off-owner or during native submission.</exception>
    public bool TransparentBG { get { ThrowIfDisposed(); return _transparent; } set { EnsureMutable(); RenderingOwner?.EnsureViewportMutation(); _transparent = value; } }
    internal void TextureSizeChanged()
    {
        List<Exception>? errors = null;
        if (TextureSizeUpdated is { } handlers) foreach (var handler in Delegate.EnumerateInvocationList(handlers)) try { handler(); } catch (Exception error) { AnimationNode.CollectException(ref errors, error); }
        AnimationNode.ThrowCollected("Viewport texture notification failed.", errors);
    }
    internal void InvalidateViewportRecording() { if (RenderingOwner is { } renderer) renderer.InvalidateViewportRecordings(this); else InvalidateRecording(this); }
    private static void InvalidateRecording(Node node)
    { if (node is CanvasItem item) item.QueueRedraw(); for (var i = 0; i < node.GetChildCount(includeInternal: true); i++) { var child = node.GetChild(i, includeInternal: true); if (child is not Viewport) InvalidateRecording(child); } }
    private static readonly PropertyDescriptor[] TargetProperties =
    [new PropertyDescriptor<Viewport, bool>(nameof(TransparentBG), n => n.TransparentBG, (n, v) => n.TransparentBG = v, _ => false, stored: true)];
}
