using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Renders retained two-dimensional window and offscreen canvas commands.</summary>
/// <remarks>Engine.Run owns startup, frame submission and shutdown on the scene owner thread. Geometry uses
/// source-alpha blending into an RGBA8 framebuffer. GPU initialization may fall back according to project settings.
/// CanvasLayer groups are ordered before per-canvas item Z/Y order. Rectangles, strokes, curves, filled polygons, short primitives, image textures, retained animation intervals and Control descendant clipping are integrated. Shader materials require the GPU path. Independent single-layer viewport targets and texture dependencies execute on both backends. Lights, general canvas clipping and device recovery
/// are not integrated. Owned two-dimensional texture RIDs support copied images, compatible updates, replacement,
/// placeholders and explicit free; resource texture RIDs remain borrowed. Screen notifier bounds and processing enablers follow submitted canvas culling. Owned SDL handles remain internal; DisplayServer can expose borrowed native context identities.</remarks>
public sealed partial class RenderingServer : ElectronObject
{
    /// <summary>Specifies the smallest canvas-layer index, drawn before every greater layer index.</summary>
    public const int CanvasLayerMin = int.MinValue;

    /// <summary>Specifies the largest canvas-layer index, drawn after every smaller layer index.</summary>
    public const int CanvasLayerMax = int.MaxValue;

    private readonly CanvasBackend _backend;
    private readonly Window _window;
    private readonly int _ownerThread = Environment.CurrentManagedThreadId;
    private List<CanvasItem> _nodes = [];
    private List<CanvasVertex> _vertices = [];
    private List<CanvasBatch> _batches = [];
    private List<RenderEntry> _order = [];
    private Dictionary<CanvasItem, Transform> _repeatTransforms = [];
    private Dictionary<CanvasItem, Transform> _canvasTransforms = [];
    private float _interpolationFraction = 1f;
    private List<YSortEntry> _ySort = [];
    private readonly List<AnimatedTexture> _animatedChanges = [];
    private long _canvasStacking;
    private bool _canvasTooltipOverlay;
    private ulong _canvasID;
    private bool _renderLoopEnabled = true;
    private bool _closing;
    private bool _rendering;
    private bool _submittingTextures;
    private Color _clearColor;
    private static RenderingServer? _instance;

    private RenderingServer(Window window, CanvasBackend backend)
    {
        _window = window;
        _viewport = window;
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
            try { backend = new GPUCanvasBackend(nativeWindow); }
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

    private sealed class CanvasFrame(Viewport viewport)
    {
        internal readonly Viewport Viewport = viewport;
        internal readonly List<CanvasItem> Nodes = [];
        internal readonly List<CanvasVertex> Vertices = [];
        internal readonly List<CanvasBatch> Batches = [];
        internal readonly List<RenderEntry> Order = [];
        internal readonly Dictionary<CanvasItem, Transform> Repeats = [], Transforms = [];
        internal readonly List<YSortEntry> YSort = [];
        internal readonly Texture?[] TextureScratch = new Texture?[16];
        internal int State;
        internal bool Wanted, Drawn;
    }
    private Viewport _viewport;
    private readonly Dictionary<Viewport, CanvasFrame> _canvasFrames = new(ReferenceEqualityComparer.Instance);
    private readonly List<CanvasFrame> _activeFrames = [];
    private void UseFrame(CanvasFrame frame)
    { _viewport = frame.Viewport; _nodes = frame.Nodes; _vertices = frame.Vertices; _batches = frame.Batches; _order = frame.Order; _repeatTransforms = frame.Repeats; _canvasTransforms = frame.Transforms; _ySort = frame.YSort; }
    private Color FrameClear(Viewport viewport) => viewport.TransparentBG ? default : _clearColor with { A = 1 };
    private void CaptureViewports(Node node)
    {
        if (node is Viewport viewport && (ReferenceEquals(viewport, _window) || viewport is SubViewport))
        {
            if (!_canvasFrames.TryGetValue(viewport, out var frame)) _canvasFrames.Add(viewport, frame = new(viewport));
            viewport.RenderingOwner = this; frame.State = 0; frame.Wanted = frame.Drawn = false; _activeFrames.Add(frame);
            var size = ReferenceEquals(viewport, _window) ? _backend.GetPixelSize() : ((SubViewport)viewport).Size;
            if (size.X > 0 && size.Y > 0) _backend.Target(viewport, size, FrameClear(viewport));
        }
        for (var i = 0; i < node.GetChildCount(includeInternal: true); i++) CaptureViewports(node.GetChild(i, includeInternal: true));
    }
    internal void InvalidateViewportRecordings(Viewport viewport)
    { EnsureOwner(); InvalidateViewportRecordings(_window, viewport); }
    private static void InvalidateViewportRecordings(Node node, Viewport viewport)
    { if (node is CanvasItem item && ReferenceEquals(item.CanvasViewport, viewport)) item.QueueRedraw(); for (var i = 0; i < node.GetChildCount(includeInternal: true); i++) InvalidateViewportRecordings(node.GetChild(i, includeInternal: true), viewport); }
    internal void SetWindowVisible(DisplayServer display, bool visible) { EnsureViewportMutation(); _backend.SetWindowVisible(display, visible); }
    internal void EnsureViewportMutation() { EnsureOwner(); if (_submittingTextures) throw new InvalidOperationException("Viewport targets cannot mutate during native submission."); }
    internal void ReleaseViewport(Viewport viewport) { EnsureOwner(); if (_submittingTextures) throw new InvalidOperationException("Viewport targets cannot be released during native submission."); _backend.ReleaseTarget(viewport); _canvasFrames.Remove(viewport); viewport.RenderingOwner = null; }
    internal Vector2i ViewportDimensions(Viewport viewport) { EnsureOwner(); return ReferenceEquals(viewport, _window) ? _backend.GetPixelSize() : ((SubViewport)viewport).Size; }
    internal Image? ReadbackViewport(Viewport viewport)
    { EnsureOwner(); if (_submittingTextures) throw new InvalidOperationException("Viewport readback cannot reenter native submission."); var target = _backend.FindTarget(viewport); return target?.HasFrame == true ? _backend.Readback(target) : null; }
    internal void Render(SceneTree tree, double step)
    {
        EnsureOwner(); if (!_renderLoopEnabled) return;
        if (_rendering) throw new InvalidOperationException("Canvas rendering cannot be re-entered.");
        _rendering = true;
        try
        {
            AnimatedTexture.AdvanceAll(this, Stopwatch.GetTimestamp(), _animatedChanges);
            FramePreDraw?.Invoke();
            if (!double.IsFinite(step) || step < 0 || !double.IsFinite(CanvasTime + step)) throw new InvalidOperationException("The render clock step is invalid.");
            CanvasTime = (CanvasTime + step) % ProjectSettings.Instance.GetWithOverride(ProjectSettings.RenderingTimeRolloverSeconds);
            _interpolationFraction = tree.PhysicsInterpolation ? (float)Engine.Instance.PhysicsInterpolationFraction : 1f;
            _activeFrames.Clear(); CaptureViewports(tree.Root);
            if (_activeFrames.Count != 0) { UseFrame(_activeFrames[0]); _nodes.Clear(); Capture(tree.Root); foreach (var node in _nodes) if (!node.IsDisposed && ReferenceEquals(node.Tree, tree) && node.IsVisibleInTree) node.PrepareCanvas(); }
            _activeFrames.Clear(); CaptureViewports(tree.Root);
            foreach (var viewport in _canvasFrames.Keys) if (viewport.IsDisposed || !ReferenceEquals(viewport.Tree, tree)) { _backend.ReleaseTarget(viewport); viewport.RenderingOwner = null; _canvasFrames.Remove(viewport); }
            _backend.BeginFrame();
            if (_window.Visible && _canvasFrames.TryGetValue(_window, out var root)) { root.Wanted = true; SubmitFrame(root, tree); }
            for (var i = 0; i < _activeFrames.Count; i++) { var frame = _activeFrames[i]; if (frame.Viewport is SubViewport sub && sub.RenderTargetUpdateMode is ViewportUpdateMode.Always or ViewportUpdateMode.Once) { frame.Wanted = true; SubmitFrame(frame, tree); } }
            _backend.EndFrame();
            foreach (var frame in _activeFrames) if (frame.Drawn) { UseFrame(frame); DispatchScreenVisibility(tree); }
            FramePostDraw?.Invoke();
        }
        finally
        {
            foreach (var frame in _activeFrames) { frame.Nodes.Clear(); frame.Vertices.Clear(); frame.Batches.Clear(); Array.Clear(frame.TextureScratch); frame.Order.Clear(); frame.Repeats.Clear(); frame.Transforms.Clear(); frame.YSort.Clear(); }
            _activeFrames.Clear(); _submittingTextures = false; _rendering = false;
        }
    }
    private void SubmitTexture(Texture? texture, SceneTree tree)
    {
        while (texture is AtlasTexture atlas) texture = atlas.RenderingTexture;
        if (texture is not ViewportTexture view || view.Bound is not { } viewport || !_canvasFrames.TryGetValue(viewport, out var frame) || !ReferenceEquals(viewport.Tree, tree)) return;
        if (viewport is SubViewport { RenderTargetUpdateMode: ViewportUpdateMode.Disabled }) return;
        frame.Wanted = true; SubmitFrame(frame, tree);
    }
    private void SubmitFrame(CanvasFrame frame, SceneTree tree)
    {
        if (frame.State != 0 || !frame.Wanted || frame.Viewport.IsDisposed || !ReferenceEquals(frame.Viewport.Tree, tree)) return;
        if (frame.Viewport is SubViewport { RenderTargetUpdateMode: ViewportUpdateMode.Disabled }) return;
        frame.State = 1; BuildFrame(frame, tree);
        foreach (var batch in frame.Batches)
        {
            SubmitTexture(batch.Texture, tree);
            if (batch.Material is { } material)
            {
                material.CopyTextures(frame.TextureScratch);
                for (var i = 0; i < material.Textures.Length; i++) SubmitTexture(frame.TextureScratch[i], tree);
                Array.Clear(frame.TextureScratch);
            }
        }
        foreach (var child in _activeFrames) if (child.Viewport is SubViewport { RenderTargetUpdateMode: ViewportUpdateMode.WhenParentVisible } && ReferenceEquals(child.Viewport.Parent?.GetViewport(), frame.Viewport)) { child.Wanted = true; SubmitFrame(child, tree); }
        var target = _backend.FindTarget(frame.Viewport); if (target is null) return;
        _submittingTextures = true;
        try { _backend.Draw(target, CollectionsMarshal.AsSpan(frame.Vertices), CollectionsMarshal.AsSpan(frame.Batches), FrameClear(frame.Viewport), frame.Viewport is not SubViewport sub || sub.RenderTargetClearMode != ViewportClearMode.Never, ReferenceEquals(frame.Viewport, _window) && _window.Visible, CanvasTime); }
        finally { _submittingTextures = false; }
        if (frame.Viewport is SubViewport completed) completed.Submitted();
        frame.State = 2; frame.Drawn = true;
    }
    private void BuildFrame(CanvasFrame frame, SceneTree tree)
    {
        UseFrame(frame); _vertices.Clear(); _batches.Clear(); _order.Clear(); _repeatTransforms.Clear(); _canvasTransforms.Clear(); _ySort.Clear();
        var viewport = frame.Viewport; var pixels = _backend.FindTarget(viewport)!.Size;
        var framebufferTransform = ReferenceEquals(viewport, _window) ? new Transform(0f, new Vector2((float)pixels.X / _window.Size.X, (float)pixels.Y / _window.Size.Y), 0f, Vector2.Zero) : Transform.Identity;
        // Drawing callbacks may change parenting, visibility or sibling order.
        _nodes.Clear();
        Capture(tree.Root);
        for (var i = _nodes.Count - 1; i >= 0; i--) if (!ReferenceEquals(_nodes[i].CanvasViewport, viewport)) _nodes.RemoveAt(i);
        foreach (var node in _nodes)
            if (node is VisibleOnScreenNotifier notifier) notifier.ScreenCandidate = false;
        foreach (var node in _nodes)
            if (node.GetParentItem() is null)
            {
                var layer = node.GetCanvasLayerNode();
                if (layer is not null && !ReferenceEquals(layer.CanvasViewport, _viewport)) continue;
                _canvasStacking = layer is null ? 0 : ((long)layer.Layer << 32) + (uint)layer.GetIndex(includeInternal: true);
                _canvasTooltipOverlay = SceneTree.IsTooltipNode(node);
                _canvasID = layer?.InstanceID ?? 0;
                OrderCanvas(node, framebufferTransform * _viewport.GetCanvasRenderTransform(layer, _interpolationFraction));
            }
        _order.Sort(static (x, y) =>
        {
            var order = x.TooltipOverlay.CompareTo(y.TooltipOverlay); if (order != 0) return order;
            order = x.Stacking.CompareTo(y.Stacking); if (order != 0) return order;
            order = x.CanvasID.CompareTo(y.CanvasID); if (order != 0) return order;
            order = x.Z.CompareTo(y.Z); return order != 0 ? order : x.Order.CompareTo(y.Order);
        });
        _submittingTextures = true;
        ComposeFrame(pixels);
    }

    private void AppendOrderedCanvas(RenderEntry item, Vector2i pixels)
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
            return;
        }
        var size = repeatSource is Parallax current ? current.RepeatSize : ((ParallaxLayer)repeatSource).RepeatPeriod;
        var times = repeatSource is Parallax repeated ? repeated.RepeatTimes : 1;
        var sourceTransform = _repeatTransforms[repeatSource];
        var start = size * -(times / 2);
        var countX = size.X == 0 ? 0 : times;
        var countY = size.Y == 0 ? 0 : times;
        var repeatedClip = GetClip(item.Node, pixels, repeatSource);
        if (repeatedClip is { } empty && !empty.HasArea() || HasEmptyOwnClip(item.Node, pixels, repeatSource)) return;
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

    private void Capture(Node node)
    {
        if (node is CanvasItem item) _nodes.Add(item);
        for (var i = 0; i < node.GetChildCount(includeInternal: true); i++) Capture(node.GetChild(i, includeInternal: true));
    }

    private void OrderCanvas(CanvasItem item, Transform transform, bool alreadyYSorted = false)
    {
        if (!item.IsVisibleInTree || (item.VisibilityLayer & _viewport.CanvasCullMask) == 0) return;
        if (item is ParallaxLayer layer) _repeatTransforms[layer] = transform;
        if (!alreadyYSorted)
        {
            var local = item.GetInterpolatedVisualTransform(_interpolationFraction);
            if (_viewport.SnapTransformsToPixel)
            {
                transform.Origin = CanvasGeometry.Snap(transform.Origin);
                local.Origin = CanvasGeometry.Snap(local.Origin);
            }
            transform *= local;
        }
        if (item.YSortEnabled)
        {
            if (alreadyYSorted && item is not CanvasGroup)
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
                if (item is not CanvasGroup || !ReferenceEquals(_ySort[index].Node, item)) OrderCanvas(_ySort[index].Node, transform * _ySort[index].Transform, alreadyYSorted: true);
            if (item is CanvasGroup) AddRenderEntry(item, transform);
            _ySort.RemoveRange(first, count);
            return;
        }
        OrderChildren(item, transform, behind: true);
        if (item is CanvasGroup) { OrderChildren(item, transform, behind: false); AddRenderEntry(item, transform); }
        else { AddRenderEntry(item, transform); OrderChildren(item, transform, behind: false); }
    }

    private void AddRenderEntry(CanvasItem item, Transform transform)
    {
        if (item is Parallax parallax) _repeatTransforms[parallax] = transform;
        _canvasTransforms[item] = transform;
        _order.Add(new(item, _canvasTooltipOverlay, _canvasStacking, _canvasID, item.EffectiveZIndex, _order.Count, transform, ParentGroup(item)));
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
        for (var index = 0; index < item.GetChildCount(includeInternal: true); index++)
            if (item.GetChild(index, includeInternal: true) is CanvasItem { TopLevel: false } child && child.ShowBehindParent == behind)
                OrderCanvas(child, transform);
    }

    private void CollectYSort(CanvasItem parent, Transform parentTransform)
    {
        for (var index = 0; index < parent.GetChildCount(includeInternal: true); index++)
        {
            if (parent.GetChild(index, includeInternal: true) is not CanvasItem { TopLevel: false } child || !child.Visible || (child.VisibilityLayer & _viewport.CanvasCullMask) == 0) continue;
            var local = child.GetInterpolatedVisualTransform(_interpolationFraction);
            if (_viewport.SnapTransformsToPixel) local.Origin = CanvasGeometry.Snap(local.Origin);
            var transform = parentTransform * local;
            _ySort.Add(new(child, transform, _ySort.Count));
            if (child.YSortEnabled && child is not CanvasGroup) CollectYSort(child, transform);
        }
    }

    internal Image Readback() { EnsureOwner(); return _backend.Readback(_backend.FindTarget(_window) ?? throw new InvalidOperationException("No canvas frame has completed.")); }
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
            List<Exception>? errors = null;
            try
            {
                try { ReleaseOwnedMultiMeshes(); } catch (Exception error) { (errors ??= []).Add(error); }
                try { ReleaseOwnedMeshes(); } catch (Exception error) { (errors ??= []).Add(error); }
                try { ReleaseOwnedTextures(); } catch (Exception error) { (errors ??= []).Add(error); }
                try { _backend.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
            }
            finally
            {
                FramePreDraw = FramePostDraw = null; foreach (var viewport in _canvasFrames.Keys) viewport.RenderingOwner = null; _canvasFrames.Clear(); _activeFrames.Clear();
                _nodes.Clear(); _vertices.Clear(); _batches.Clear(); _order.Clear(); _repeatTransforms.Clear(); _canvasTransforms.Clear(); _ySort.Clear();
                if (ReferenceEquals(Instance, this)) Volatile.Write(ref _instance, null);
            }
            if (errors is not null) throw new AggregateException("Rendering cleanup failed.", errors);
        }
        base.Dispose(disposing);
    }

    private void EnsureOwner()
    {
        ThrowIfDisposed();
        if (_ownerThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Rendering requires the scene owner thread.");
    }

    private readonly record struct RenderEntry(CanvasItem Node, bool TooltipOverlay, long Stacking, ulong CanvasID, int Z, int Order, Transform Transform, CanvasGroup? Group);
    private readonly record struct YSortEntry(CanvasItem Node, Transform Transform, int Order);
}
