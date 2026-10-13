namespace Electron2D;

public sealed partial class RenderingServer
{
    private sealed class ServerViewportState(bool owned)
    {
        internal readonly bool Owned = owned;
        internal bool Active = !owned, DisableCanvas, ParentAssigned;
        internal Viewport? Parent;
        internal int ViewCount = 1;
        internal Vector2i? Size;
        internal ViewportUpdateMode Update = ViewportUpdateMode.Always;
        internal ViewportClearMode Clear;
    }
    private readonly Dictionary<Viewport, ServerViewportState> _serverViewports = new(ReferenceEqualityComparer.Instance);
    private readonly List<Viewport> _staleViewports = [];
    private Viewport ServerViewport(RID rid)
    {
        EnsureOwner(); var view = Viewport.ResolveViewportRID(rid);
        if (_serverViewports.TryGetValue(view, out var state) && state.Owned) return view;
        if (!view.IsInsideTree || !ReferenceEquals(view.Tree, _window.Tree)) throw new InvalidOperationException("Viewport belongs to another scene or rendering session.");
        return view;
    }
    private ServerViewportState ChangeViewport(RID rid, out Viewport view)
    {
        EnsureServerViewportChange(); view = ServerViewport(rid);
        if (!_serverViewports.TryGetValue(view, out var state)) _serverViewports.Add(view, state = new(false));
        view.RenderingOwner = this;
        return state;
    }
    private RID ViewportCreateCore()
    {
        EnsureServerViewportChange(); var view = new SubViewport(); view.SetServerSize(default); view.RenderingOwner = this;
        _serverViewports.Add(view, new(true)); return view.GetViewportRID();
    }
    private ViewportUpdateMode UpdateMode(Viewport view) => view is SubViewport sub ? sub.RenderTargetUpdateMode : _serverViewports.TryGetValue(view, out var state) ? state.Update : ViewportUpdateMode.Always;
    private ViewportClearMode ClearMode(Viewport view) => view is SubViewport sub ? sub.RenderTargetClearMode : _serverViewports.TryGetValue(view, out var state) ? state.Clear : ViewportClearMode.Always;
    private bool ViewportLive(Viewport view, SceneTree tree) => !view.IsDisposed && (ReferenceEquals(view.Tree, tree) || _serverViewports.TryGetValue(view, out var state) && state.Owned);
    private bool ViewportActive(Viewport view) => !_serverViewports.TryGetValue(view, out var state) || state.Active && state.ViewCount != 0;
    private bool ViewportScheduled(Viewport view, int depth = 0)
    {
        var size = TargetSize(view);
        if (view.IsDisposed || !ViewportActive(view) || view is Window { Visible: false } || size.X < 2 || size.Y < 2 || !_canvasFrames.TryGetValue(view, out var frame)) return false;
        var mode = UpdateMode(view); if (mode == ViewportUpdateMode.Disabled) return false;
        return frame.Wanted || mode is ViewportUpdateMode.Always or ViewportUpdateMode.Once || mode == ViewportUpdateMode.WhenParentVisible && depth < 1024 && RenderParent(view) is { } parent && ViewportScheduled(parent, depth + 1);
    }
    private Viewport? RenderParent(Viewport view) => _serverViewports.TryGetValue(view, out var state) && state.ParentAssigned ? state.Parent : view.Parent?.GetViewport();
    private Vector2i TargetSize(Viewport view) => _serverViewports.TryGetValue(view, out var state) && state.Size is { } size ? size : view is SubViewport sub ? sub.Size : ((Window)view).ContentRenderSize;
    private void ViewportSetParentCore(RID rid, RID parent)
    {
        var state = ChangeViewport(rid, out var view); var target = parent.IsValid() ? ServerViewport(parent) : null;
        var depth = 0;
        for (var current = target; current is not null; current = RenderParent(current))
        { if (ReferenceEquals(current, view)) throw new ArgumentException("Viewport parenting cannot form a cycle.", nameof(parent)); if (++depth > 1024) throw new InvalidOperationException("Viewport parent depth exceeds the supported limit."); }
        state.Parent = target; state.ParentAssigned = true;
    }
    private void ViewportSetSizeCore(RID rid, int width, int height, int viewCount)
    {
        if (width < 0 || height < 0 || viewCount < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (viewCount > 1) throw new NotSupportedException("Canvas viewport targets currently support one image layer.");
        if (width > 16384 || height > 16384) throw new NotSupportedException("Canvas targets support at most 16384 pixels per axis.");
        var state = ChangeViewport(rid, out var view); var size = new Vector2i(width, height);
        if (view is SubViewport { Parent: SubViewportContainer { Stretch: true } }) throw new InvalidOperationException("A stretched container owns its child viewport size.");
        if (TargetSize(view) != size || state.ViewCount != viewCount) _backend.ReleaseTarget(view);
        state.ViewCount = viewCount;
        if (view is SubViewport sub) sub.SetServerSize(size);
        else { state.Size = size; view.TextureSizeChanged(); }
    }
    private void ViewportSetUpdateCore(RID rid, ViewportUpdateMode mode)
    { Animation.Valid(mode); var state = ChangeViewport(rid, out var view); if (view is SubViewport sub) sub.RenderTargetUpdateMode = mode; else state.Update = mode; }
    private void ViewportSetClearCore(RID rid, ViewportClearMode mode)
    { Animation.Valid(mode); var state = ChangeViewport(rid, out var view); if (view is SubViewport sub) sub.RenderTargetClearMode = mode; else state.Clear = mode; }
    private void ViewportSubmitted(Viewport view)
    {
        if (view is SubViewport sub) sub.Submitted();
        else if (_serverViewports.TryGetValue(view, out var state)) { if (state.Update == ViewportUpdateMode.Once) state.Update = ViewportUpdateMode.Disabled; if (state.Clear == ViewportClearMode.Once) state.Clear = ViewportClearMode.Never; }
    }
    private void EnsureServerViewportChange()
    { EnsureTextureChange(); if (_rendering && !_frameSubmitted && _activeFrames.Count != 0) throw new InvalidOperationException("Viewport resources cannot mutate during canvas capture or submission."); }
    private bool ReleaseViewportRID(RID rid)
    {
        if (Viewport.FindViewportRID(rid) is not { } view) return false;
        EnsureServerViewportChange();
        if (!_serverViewports.TryGetValue(view, out var state) || !state.Owned) throw new InvalidOperationException("Scene viewports cannot be freed by the renderer.");
        DisposeServerViewport(view); return true;
    }
    private void RemoveViewportState(Viewport view)
    {
        _serverViewports.Remove(view);
        foreach (var state in _serverViewports.Values) if (ReferenceEquals(state.Parent, view)) state.Parent = null;
        var rid = view.RegisteredViewportRID;
        foreach (var canvas in _ownedCanvasRIDs) RenderingCanvasRegistry.Resolve(canvas).Attachments.Remove(rid);
        foreach (var canvas in _serverSceneCanvases) canvas.Attachments.Remove(rid);
    }
    private static void DisposeServerViewport(Viewport view)
    { var texture = view.GetTexture(); try { view.Dispose(); } finally { texture.Dispose(); } }
    private void ReleaseServerViewports()
    {
        List<Exception>? errors = null;
        foreach (var view in _serverViewports.Where(pair => pair.Value.Owned).Select(pair => pair.Key).ToArray())
            try { DisposeServerViewport(view); } catch (Exception error) { (errors ??= []).Add(error); }
        foreach (var view in _serverViewports.Keys) if (ReferenceEquals(view.RenderingOwner, this)) view.RenderingOwner = null;
        _serverViewports.Clear(); _staleViewports.Clear();
        if (errors is not null) throw new AggregateException("Viewport cleanup failed.", errors);
    }

    /// <summary>Creates an inactive, zero-size offscreen viewport owned by the active renderer.</summary>
    /// <returns>A stable identity released by FreeRID or renderer shutdown.</returns>
    /// <remarks>Initially clears Always and updates WhenVisible. Attach canvases, set a size and activate before rendering. No scene node is attached.</remarks>
    /// <exception cref="InvalidOperationException">No renderer is active, or mutation is off-owner, during submission or shutdown.</exception>
    public static RID ViewportCreate() => RequireService().ViewportCreateCore();
    /// <summary>Gets the current update policy, including the successful Once transition.</summary>
    /// <param name="viewport">Live owned or attached scene viewport.</param>
    /// <returns>The current update mode.</returns>
    /// <exception cref="ArgumentException">The identity is stale or not a viewport.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, off-owner, or the viewport belongs to another session.</exception>
    public static ViewportUpdateMode ViewportGetUpdateMode(RID viewport) { var service = RequireService(); return service.UpdateMode(service.ServerViewport(viewport)); }
    /// <summary>Activates or suspends viewport rendering while retaining its completed image.</summary>
    /// <param name="viewport">Live owned or attached scene viewport.</param><param name="active">Whether updates may submit.</param>
    /// <exception cref="ArgumentException">The identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or mutation violates ownership/submission.</exception>
    public static void ViewportSetActive(RID viewport, bool active) => RequireService().ChangeViewport(viewport, out _).Active = active;
    /// <summary>Sets pixel retention; Once becomes Never after a successful update.</summary>
    /// <param name="viewport">Live viewport.</param><param name="clearMode">Defined clear policy.</param>
    /// <exception cref="ArgumentException">The identity or enumeration is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or mutation violates ownership/submission.</exception>
    public static void ViewportSetClearMode(RID viewport, ViewportClearMode clearMode) => RequireService().ViewportSetClearCore(viewport, clearMode);
    /// <summary>Suppresses canvas geometry while continuing viewport clear and update policies.</summary>
    /// <param name="viewport">Live viewport.</param><param name="disable">Whether canvas geometry is suppressed.</param>
    /// <exception cref="ArgumentException">The identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or mutation violates ownership/submission.</exception>
    public static void ViewportSetDisable2D(RID viewport, bool disable) => RequireService().ChangeViewport(viewport, out _).DisableCanvas = disable;
    /// <summary>Selects the rendering parent used by WhenParentVisible without changing scene hierarchy.</summary>
    /// <param name="viewport">Live viewport.</param><param name="parentViewport">Live parent, or an invalid RID to detach.</param>
    /// <exception cref="ArgumentException">An identity is invalid or parenting forms a cycle.</exception>
    /// <exception cref="InvalidOperationException">Mutation violates ownership/submission or the parent depth limit.</exception>
    public static void ViewportSetParentViewport(RID viewport, RID parentViewport) => RequireService().ViewportSetParentCore(viewport, parentViewport);
    /// <summary>Sets exact nonnegative target dimensions and image layer count without resizing a native window.</summary>
    /// <param name="viewport">Live viewport.</param><param name="width">Native width in pixels.</param><param name="height">Native height in pixels.</param><param name="viewCount">One image layer, or zero to suspend output.</param>
    /// <remarks>Axes smaller than two suspend submission. Resizing retires completed target pixels while retaining logical RID identity. Scene SubViewport.Size subsequently retains its ordinary minimum-two policy.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A dimension or layer count is negative.</exception>
    /// <exception cref="NotSupportedException">More than one layer or an axis above 16384 is requested.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or mutation violates ownership/submission.</exception>
    public static void ViewportSetSize(RID viewport, int width, int height, int viewCount = 1) => RequireService().ViewportSetSizeCore(viewport, width, height, viewCount);
    /// <summary>Sets transparent-black clearing instead of opaque renderer clear color.</summary>
    /// <param name="viewport">Live viewport.</param><param name="enabled">Whether clear pixels are transparent.</param>
    /// <exception cref="ArgumentException">The identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or mutation violates ownership/submission.</exception>
    public static void ViewportSetTransparentBackground(RID viewport, bool enabled) { var service = RequireService(); service.ChangeViewport(viewport, out var view); view.TransparentBG = enabled; }
    /// <summary>Sets when the viewport submits; Once becomes Disabled only after success.</summary>
    /// <param name="viewport">Live viewport.</param><param name="updateMode">Defined update policy.</param>
    /// <exception cref="ArgumentException">The identity or enumeration is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or mutation violates ownership/submission.</exception>
    public static void ViewportSetUpdateMode(RID viewport, ViewportUpdateMode updateMode) => RequireService().ViewportSetUpdateCore(viewport, updateMode);
}
