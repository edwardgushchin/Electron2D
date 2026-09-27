using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Renders the active root window's retained two-dimensional canvas commands.</summary>
/// <remarks>Engine.Run owns startup, frame submission and shutdown on the scene owner thread. Geometry uses
/// source-alpha blending into an RGBA8 framebuffer. GPU initialization may fall back according to project settings.
/// CanvasLayer groups are ordered before per-canvas item Z/Y order. Rectangles, strokes, curves, filled polygons, short primitives, image textures, retained animation intervals and Control descendant clipping are integrated. Shader materials require the GPU path. Lights, general canvas clipping, offscreen public viewports and device recovery
/// are not integrated. Screen notifier bounds and processing enablers follow submitted canvas culling. Owned SDL handles remain internal; DisplayServer can expose borrowed native context identities.</remarks>
public sealed partial class RenderingServer : ElectronObject
{
    /// <summary>Specifies the smallest canvas-layer index, drawn before every greater layer index.</summary>
    public const int CanvasLayerMin = int.MinValue;

    /// <summary>Specifies the largest canvas-layer index, drawn after every smaller layer index.</summary>
    public const int CanvasLayerMax = int.MaxValue;

    private readonly CanvasBackend _backend;
    private readonly Window _window;
    private readonly int _ownerThread = Environment.CurrentManagedThreadId;
    private readonly List<CanvasItem> _nodes = [];
    private readonly List<CanvasVertex> _vertices = [];
    private readonly List<CanvasBatch> _batches = [];
    private readonly List<RenderEntry> _order = [];
    private readonly Dictionary<CanvasItem, Transform> _repeatTransforms = [];
    private readonly Dictionary<CanvasItem, Transform> _canvasTransforms = [];
    private float _interpolationFraction = 1f;
    private readonly List<YSortEntry> _ySort = [];
    private readonly List<AnimatedTexture> _animatedChanges = [];
    private long _canvasStacking;
    private ulong _canvasID;
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
    /// <remarks>Runs after animated textures advance and emit frame-change notifications, synchronously on the
    /// owner thread within the scene execution barrier. A failing subscriber
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

    internal double CanvasTime { get; private set; }

    internal void Render(SceneTree tree, double step)
    {
        EnsureOwner();
        if (!_renderLoopEnabled || !_window.Visible) return;
        if (_rendering) throw new InvalidOperationException("Canvas rendering cannot be re-entered.");
        _rendering = true;
        try
        {
            AnimatedTexture.AdvanceAll(this, Stopwatch.GetTimestamp(), _animatedChanges);
            FramePreDraw?.Invoke();
            if (!double.IsFinite(step) || step < 0 || !double.IsFinite(CanvasTime + step))
                throw new InvalidOperationException("The render clock step is invalid.");
            CanvasTime = (CanvasTime + step) % ProjectSettings.Instance.GetWithOverride(ProjectSettings.RenderingTimeRolloverSeconds);
            _interpolationFraction = tree.PhysicsInterpolation ? (float)Engine.Instance.PhysicsInterpolationFraction : 1f;
            _nodes.Clear(); _order.Clear(); _repeatTransforms.Clear(); _canvasTransforms.Clear(); _vertices.Clear(); _batches.Clear();
            Capture(tree.Root);
            foreach (var node in _nodes)
                if (!node.IsDisposed && ReferenceEquals(node.Tree, tree) && node.IsVisibleInTree)
                    node.PrepareCanvas();
            var pixels = _backend.GetPixelSize();
            var client = _window.Size;
            if (pixels.X <= 0 || pixels.Y <= 0) return;
            var framebufferTransform = new Transform(0f, new Vector2((float)pixels.X / client.X, (float)pixels.Y / client.Y), 0f, Vector2.Zero);
            // Drawing callbacks may change parenting, visibility or sibling order.
            _nodes.Clear();
            Capture(tree.Root);
            foreach (var node in _nodes)
                if (node is VisibleOnScreenNotifier notifier) notifier.ScreenCandidate = false;
            foreach (var node in _nodes)
                if (node.GetParentItem() is null)
                {
                    var layer = node.GetCanvasLayerNode();
                    if (layer is not null && !ReferenceEquals(layer.CanvasViewport, _window)) continue;
                    _canvasStacking = layer is null ? 0 : ((long)layer.Layer << 32) + (uint)layer.GetIndex();
                    _canvasID = layer?.InstanceID ?? 0;
                    OrderCanvas(node, framebufferTransform * _window.GetCanvasRenderTransform(layer, _interpolationFraction));
                }
            _order.Sort(static (x, y) =>
            {
                var order = x.Stacking.CompareTo(y.Stacking); if (order != 0) return order;
                order = x.CanvasID.CompareTo(y.CanvasID); if (order != 0) return order;
                order = x.Z.CompareTo(y.Z); return order != 0 ? order : x.Order.CompareTo(y.Order);
            });
            foreach (var item in _order)
            {
                CanvasItem? repeatSource = null;
                for (var ancestor = item.Node; ancestor is not null; ancestor = ancestor.GetParentItem())
                    if ((ancestor is Parallax parallax && parallax.RepeatSize != Vector2.Zero) ||
                        (ancestor is ParallaxLayer layer && layer.RepeatPeriod != Vector2.Zero))
                    {
                        repeatSource = ancestor;
                        break;
                    }
                if (repeatSource is null)
                {
                    var clip = GetClip(item.Node, pixels);
                    if ((clip is null || clip.Value.HasArea()) && !HasEmptyOwnClip(item.Node, pixels))
                    {
                        AppendScreenCanvas(item.Node, item.Transform, clip, pixels);
                    }
                    continue;
                }
                var size = repeatSource is Parallax current ? current.RepeatSize : ((ParallaxLayer)repeatSource).RepeatPeriod;
                var times = repeatSource is Parallax repeated ? repeated.RepeatTimes : 1;
                var sourceTransform = _repeatTransforms[repeatSource];
                var start = size * -(times / 2);
                var countX = size.X == 0 ? 0 : times;
                var countY = size.Y == 0 ? 0 : times;
                var repeatedClip = GetClip(item.Node, pixels, repeatSource);
                if (repeatedClip is { } empty && !empty.HasArea() || HasEmptyOwnClip(item.Node, pixels, repeatSource)) continue;
                for (long y = 0; y <= countY; y++)
                    for (long x = 0; x <= countX; x++)
                    {
                        var transform = item.Transform;
                        var displacement = repeatSource is Parallax
                            ? start + new Vector2(x * size.X, y * size.Y)
                            : new Vector2(x * size.X, y * size.Y);
                        transform.Origin += sourceTransform.BasisXform(displacement);
                        if (!transform.IsFinite()) throw new InvalidOperationException("Parallax repetition overflowed finite coordinates.");
                        AppendScreenCanvas(item.Node, transform, repeatedClip, pixels);
                    }
            }
            foreach (var batch in _batches)
                if (batch.Material is not null && _backend.Method != "gpu")
                    throw new NotSupportedException("A shader material requires GPU rendering; compatibility fallback cannot draw it.");
            _backend.Draw(CollectionsMarshal.AsSpan(_vertices), CollectionsMarshal.AsSpan(_batches), _clearColor, present: true, CanvasTime);
            DispatchScreenVisibility(tree);
            FramePostDraw?.Invoke();
        }
        finally { _nodes.Clear(); _order.Clear(); _repeatTransforms.Clear(); _canvasTransforms.Clear(); _ySort.Clear(); _rendering = false; }
    }

    private void Capture(Node node)
    {
        if (node is CanvasItem item) _nodes.Add(item);
        for (var i = 0; i < node.ChildCount; i++) Capture(node.GetChild(i));
    }

    private void OrderCanvas(CanvasItem item, Transform transform, bool alreadyYSorted = false)
    {
        if (!item.IsVisibleInTree || (item.VisibilityLayer & _window.CanvasCullMask) == 0) return;
        if (item is ParallaxLayer layer) _repeatTransforms[layer] = transform;
        if (!alreadyYSorted)
        {
            var local = item.GetInterpolatedVisualTransform(_interpolationFraction);
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
                AddRenderEntry(item, transform);
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
        AddRenderEntry(item, transform);
        OrderChildren(item, transform, behind: false);
    }

    private void AddRenderEntry(CanvasItem item, Transform transform)
    {
        if (item is Parallax parallax) _repeatTransforms[parallax] = transform;
        _canvasTransforms[item] = transform;
        _order.Add(new(item, _canvasStacking, _canvasID, item.EffectiveZIndex, _order.Count, transform));
    }

    private bool HasEmptyOwnClip(CanvasItem item, Vector2i pixels, CanvasItem? repeatSource = null) =>
        (item is Control { ClipContents: true } || item.CanvasClipRect is not null) && GetClip(item, pixels, repeatSource, includeSelf: true) is { } clip && !clip.HasArea();

    private Rect2i? GetClip(CanvasItem item, Vector2i pixels, CanvasItem? repeatSource = null, bool includeSelf = false)
    {
        Rect2? clipped = null;
        var repeat = repeatSource is not null && !ReferenceEquals(item, repeatSource);
        for (var ancestor = item; ancestor is not null; ancestor = ancestor.GetParentItem())
        {
            if (ReferenceEquals(ancestor, repeatSource)) repeat = false;
            var localClip = ancestor.CanvasClipRect;
            if (localClip is null && (includeSelf || !ReferenceEquals(ancestor, item)) && ancestor is Control { ClipContents: true } control)
                localClip = new Rect2(Vector2.Zero, control.Size);
            if (localClip is not { } localArea) continue;
            var transform = _canvasTransforms[ancestor];
            var area = transform * localArea;
            if (repeat) area = ExpandRepeatedClip(area, repeatSource!);
            if (!area.Position.IsFinite() || !area.Size.IsFinite())
                throw new InvalidOperationException("Canvas clipping overflowed finite coordinates.");
            clipped = clipped is { } prior ? prior.Intersection(area) : area;
        }
        if (clipped is not { } bounds) return null;
        bounds = bounds.Intersection(new Rect2(Vector2.Zero, new Vector2(pixels.X, pixels.Y)));
        if (bounds.Size.X < .5f || bounds.Size.Y < .5f) return new Rect2i(Vector2i.Zero, Vector2i.Zero);
        var x = (int)MathF.Round(bounds.Position.X, MidpointRounding.AwayFromZero);
        var y = (int)MathF.Round(bounds.Position.Y, MidpointRounding.AwayFromZero);
        var width = (int)MathF.Round(bounds.Size.X, MidpointRounding.AwayFromZero);
        var height = (int)MathF.Round(bounds.Size.Y, MidpointRounding.AwayFromZero);
        return new Rect2i(x, y, Math.Min(width, pixels.X - x), Math.Min(height, pixels.Y - y));
    }

    private Rect2 ExpandRepeatedClip(Rect2 area, CanvasItem source)
    {
        var size = source is Parallax parallax ? parallax.RepeatSize : ((ParallaxLayer)source).RepeatPeriod;
        var times = source is Parallax repeated ? repeated.RepeatTimes : 1;
        var basis = _repeatTransforms[source];
        var start = size * -(times / 2);
        var first = area with { Position = area.Position + basis.BasisXform(start) };
        var x = basis.BasisXform(new Vector2(times * size.X, 0));
        var y = basis.BasisXform(new Vector2(0, times * size.Y));
        return first.Merge(first with { Position = first.Position + x })
            .Merge(first with { Position = first.Position + y })
            .Merge(first with { Position = first.Position + x + y });
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
            if (parent.GetChild(index) is not CanvasItem { TopLevel: false } child || !child.Visible || (child.VisibilityLayer & _window.CanvasCullMask) == 0) continue;
            var local = child.GetInterpolatedVisualTransform(_interpolationFraction);
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
                _nodes.Clear(); _vertices.Clear(); _batches.Clear(); _order.Clear(); _repeatTransforms.Clear(); _canvasTransforms.Clear(); _ySort.Clear();
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

    private readonly record struct RenderEntry(CanvasItem Node, long Stacking, ulong CanvasID, int Z, int Order, Transform Transform);
    private readonly record struct YSortEntry(CanvasItem Node, Transform Transform, int Order);
}
