using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Renders the active root window's retained two-dimensional canvas commands.</summary>
/// <remarks>Engine.Run owns startup, frame submission and shutdown on the scene owner thread. Geometry uses
/// source-alpha blending into an RGBA8 framebuffer. GPU initialization may fall back according to project settings.
/// Rectangles, lines and image textures are integrated. Shader materials require the GPU path. Lights, clipping, offscreen public viewports and device recovery
/// are not integrated. Owned SDL handles remain internal; DisplayServer can expose borrowed native context identities.</remarks>
public sealed class RenderingServer : ElectronObject
{
    private readonly CanvasBackend _backend;
    private readonly Window _window;
    private readonly int _ownerThread = Environment.CurrentManagedThreadId;
    private readonly List<CanvasItem> _nodes = [];
    private readonly List<CanvasVertex> _vertices = [];
    private readonly List<CanvasBatch> _batches = [];
    private readonly List<RenderEntry> _order = [];
    private readonly List<YSortEntry> _ySort = [];
    private bool _renderLoopEnabled = true;
    private bool _closing;
    private bool _rendering;
    private Color _clearColor;
    private static RenderingServer? _instance;

    private RenderingServer(Window window, CanvasBackend backend)
    {
        _window = window;
        _backend = backend;
        _clearColor = ProjectSettings.Instance.GetWithOverride(ProjectSettings.DefaultClearColor);
    }

    /// <summary>Gets the rendering service belonging to the active Engine.Run invocation.</summary>
    /// <value>Null before startup and after native cleanup.</value>
    public static RenderingServer? Instance => Volatile.Read(ref _instance);

    /// <summary>Gets or sets whether Engine.Run submits canvas frames.</summary>
    /// <value>True by default. Disabling retains existing commands and pending redraw requests.</value>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    public bool RenderLoopEnabled
    {
        get { EnsureOwner(); return _renderLoopEnabled; }
        set { EnsureOwner(); _renderLoopEnabled = value; }
    }

    /// <summary>Returns the active rendering method after startup and fallback selection.</summary>
    /// <returns>gpu or compatibility.</returns>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    public string GetCurrentRenderingMethod() { EnsureOwner(); return _backend.Method; }

    /// <summary>Returns the native driver selected by the active rendering backend.</summary>
    /// <returns>The SDL GPU or SDL_Renderer driver name.</returns>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    public string GetCurrentRenderingDriverName() { EnsureOwner(); return _backend.Driver; }

    /// <summary>Gets the color used to clear the root framebuffer.</summary>
    /// <returns>The current clear color, initialized from ProjectSettings.DefaultClearColor.</returns>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    public Color GetDefaultClearColor() { EnsureOwner(); return _clearColor; }

    /// <summary>Changes the root framebuffer clear color for subsequent frames.</summary>
    /// <param name="color">A finite color; normalized framebuffer channels clamp to zero through one.</param>
    /// <exception cref="ArgumentException">The color is not finite.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The service is disposed.</exception>
    public void SetDefaultClearColor(Color color)
    {
        EnsureOwner();
        if (!color.IsFinite()) throw new ArgumentException("The clear color must be finite.", nameof(color));
        _clearColor = color;
    }

    /// <summary>Occurs before the scene's canvas commands are prepared for a frame.</summary>
    /// <remarks>Runs synchronously on the owner thread within the scene execution barrier. A failing subscriber
    /// aborts this frame and Engine.Run cleans up before propagating the error.</remarks>
    public event Action? FramePreDraw;

    /// <summary>Occurs after the canvas frame is submitted to the active backend.</summary>
    /// <remarks>Submission does not imply the GPU has completed or the compositor has displayed the frame.
    /// Runs synchronously on the owner thread. Frame or event-pump re-entry is rejected.</remarks>
    public event Action? FramePostDraw;

    internal static RenderingServer Open(Window window, System.Runtime.InteropServices.SafeHandle nativeWindow)
    {
        if (Instance is not null) throw new InvalidOperationException("A rendering server is already active.");
        var settings = ProjectSettings.Instance;
        var method = settings.GetWithOverride(ProjectSettings.RenderingMethod);
        CanvasBackend backend;
        if (method == "compatibility") backend = new CompatibilityCanvasBackend(nativeWindow);
        else
        {
            try { backend = new GpuCanvasBackend(nativeWindow); }
            catch (Exception gpuError) when (gpuError is InvalidOperationException or NotSupportedException or DllNotFoundException &&
                settings.GetWithOverride(ProjectSettings.RenderingFallback))
            {
                try { backend = new CompatibilityCanvasBackend(nativeWindow); }
                catch (Exception fallbackError) { throw new AggregateException("Both canvas backends failed to initialize.", gpuError, fallbackError); }
            }
        }
        try
        {
            var server = new RenderingServer(window, backend);
            Volatile.Write(ref _instance, server);
            return server;
        }
        catch { backend.Dispose(); throw; }
    }

    internal void Render(SceneTree tree)
    {
        EnsureOwner();
        if (!_renderLoopEnabled || !_window.Visible) return;
        if (_rendering) throw new InvalidOperationException("Canvas rendering cannot be re-entered.");
        _rendering = true;
        try
        {
            FramePreDraw?.Invoke();
            _nodes.Clear(); _order.Clear(); _vertices.Clear(); _batches.Clear();
            Capture(tree.Root);
            foreach (var node in _nodes)
                if (!node.IsDisposed && ReferenceEquals(node.Tree, tree) && node.IsVisibleInTree)
                    node.PrepareCanvas();
            var pixels = _backend.GetPixelSize();
            var client = _window.Size;
            if (pixels.X <= 0 || pixels.Y <= 0) return;
            var viewportTransform = new Transform(0f, new Vector2((float)pixels.X / client.X, (float)pixels.Y / client.Y), 0f, Vector2.Zero);
            // Drawing callbacks may change parenting, visibility or sibling order.
            _nodes.Clear();
            Capture(tree.Root);
            foreach (var node in _nodes)
                if (node.GetParentItem() is null)
                    OrderCanvas(node, viewportTransform);
            _order.Sort(static (x, y) => { var z = x.Z.CompareTo(y.Z); return z != 0 ? z : x.Order.CompareTo(y.Order); });
            foreach (var item in _order)
                item.Node.AppendCanvas(_vertices, _batches, item.Transform);
            foreach (var batch in _batches)
                if (batch.Material is not null && _backend.Method != "gpu")
                    throw new NotSupportedException("A shader material requires GPU rendering; compatibility fallback cannot draw it.");
            _backend.Draw(CollectionsMarshal.AsSpan(_vertices), CollectionsMarshal.AsSpan(_batches), _clearColor, present: true);
            FramePostDraw?.Invoke();
        }
        finally { _nodes.Clear(); _order.Clear(); _ySort.Clear(); _rendering = false; }
    }

    private void Capture(Node node)
    {
        if (node is CanvasItem item) _nodes.Add(item);
        for (var i = 0; i < node.ChildCount; i++) Capture(node.GetChild(i));
    }

    private void OrderCanvas(CanvasItem item, Transform transform, bool alreadyYSorted = false)
    {
        if (!item.IsVisibleInTree) return;
        if (!alreadyYSorted)
        {
            var local = item.GetTransform();
            if (_window.SnapTransformsToPixel)
            {
                transform.Origin = CanvasGeometry.Snap(transform.Origin);
                local.Origin = CanvasGeometry.Snap(local.Origin);
            }
            transform *= local;
        }
        if (item.YSortEnabled)
        {
            if (alreadyYSorted)
            {
                _order.Add(new(item, item.EffectiveZIndex, _order.Count, transform));
                return;
            }
            var first = _ySort.Count;
            _ySort.Add(new(item, Transform.Identity, first));
            CollectYSort(item, Transform.Identity);
            var count = _ySort.Count - first;
            CollectionsMarshal.AsSpan(_ySort).Slice(first, count).Sort(static (left, right) =>
                Mathf.IsEqualApprox(left.Transform.Origin.Y, right.Transform.Origin.Y) ? left.Order.CompareTo(right.Order) : left.Transform.Origin.Y.CompareTo(right.Transform.Origin.Y));
            for (var index = first; index < first + count; index++)
                OrderCanvas(_ySort[index].Node, transform * _ySort[index].Transform, alreadyYSorted: true);
            _ySort.RemoveRange(first, count);
            return;
        }
        OrderChildren(item, transform, behind: true);
        _order.Add(new(item, item.EffectiveZIndex, _order.Count, transform));
        OrderChildren(item, transform, behind: false);
    }

    private void OrderChildren(CanvasItem item, Transform transform, bool behind)
    {
        for (var index = 0; index < item.ChildCount; index++)
            if (item.GetChild(index) is CanvasItem { TopLevel: false } child && child.ShowBehindParent == behind)
                OrderCanvas(child, transform);
    }

    private void CollectYSort(CanvasItem parent, Transform parentTransform)
    {
        for (var index = 0; index < parent.ChildCount; index++)
        {
            if (parent.GetChild(index) is not CanvasItem { TopLevel: false } child || !child.Visible) continue;
            var local = child.GetTransform();
            if (_window.SnapTransformsToPixel) local.Origin = CanvasGeometry.Snap(local.Origin);
            var transform = parentTransform * local;
            _ySort.Add(new(child, transform, _ySort.Count));
            if (child.YSortEnabled) CollectYSort(child, transform);
        }
    }

    internal Image Readback() { EnsureOwner(); return _backend.Readback(); }
    internal nint GetNativeHandle(DisplayServer.HandleType type) { EnsureOwner(); return _backend.GetNativeHandle(type); }
    internal void Close() { _closing = true; Dispose(); }

    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        EnsureOwner();
        if (!_closing || _rendering) throw new InvalidOperationException("Rendering lifetime belongs to Engine.Run.");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { _backend.Dispose(); }
            finally
            {
                FramePreDraw = FramePostDraw = null;
                _nodes.Clear(); _vertices.Clear(); _batches.Clear(); _order.Clear(); _ySort.Clear();
                if (ReferenceEquals(Instance, this)) Volatile.Write(ref _instance, null);
            }
        }
        base.Dispose(disposing);
    }

    private void EnsureOwner()
    {
        ThrowIfDisposed();
        if (_ownerThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Rendering requires the scene owner thread.");
    }

    private readonly record struct RenderEntry(CanvasItem Node, int Z, int Order, Transform Transform);
    private readonly record struct YSortEntry(CanvasItem Node, Transform Transform, int Order);
}
