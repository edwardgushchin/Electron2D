using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Renders retained two-dimensional window and offscreen canvas commands.</summary>
/// <remarks>Public static operations delegate to the retained service object; state, identity and ownership remain object-scoped.
/// Engine.Run owns startup, frame submission and shutdown on the scene owner thread. Geometry uses
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
    private readonly List<CanvasItem> _sceneCanvasItems = [];
    private readonly List<Viewport> _sceneViewports = [];
    private SceneTree? _capturedTree;
    private ulong _capturedRevision;
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
        _clearColor = ProjectSettings.GetWithOverride(ProjectSettings.DefaultClearColor);
    }

    internal static RenderingServer? Service => Volatile.Read(ref _instance);

    internal RenderHandle? RetainComputeDevice()
    {
        EnsureOwner();
        return (_backend as GPUCanvasBackend)?.RetainComputeDevice();
    }

    internal bool RenderLoopEnabledCore
    {
        get { EnsureOwner(); return _renderLoopEnabled; }
        set { EnsureOwner(); _renderLoopEnabled = value; }
    }

    internal string GetCurrentRenderingMethodCore() { EnsureOwner(); return _backend.Method; }

    internal string GetCurrentRenderingDriverNameCore() { EnsureOwner(); return _backend.Driver; }

    internal Color GetDefaultClearColorCore() { EnsureOwner(); return _clearColor; }

    internal void SetDefaultClearColorCore(Color color)
    {
        EnsureOwner();
        if (!color.IsFinite()) throw new ArgumentException("The clear color must be finite.", nameof(color));
        _clearColor = color;
    }

    internal event Action? FramePreDrawCore;

    internal event Action? FramePostDrawCore;

    internal static RenderingServer Open(Window window, System.Runtime.InteropServices.SafeHandle nativeWindow)
    {
        if (Service is not null) throw new InvalidOperationException("A rendering server is already active.");
        var settings = ProjectSettings.Service;
        var method = settings.GetWithOverrideCore(ProjectSettings.RenderingMethod);
        CanvasBackend backend;
        if (method == "compatibility") backend = new CompatibilityCanvasBackend(nativeWindow);
        else
        {
            try { backend = new GPUCanvasBackend(nativeWindow); }
            catch (Exception gpuError) when (gpuError is InvalidOperationException or NotSupportedException or DllNotFoundException &&
                settings.GetWithOverrideCore(ProjectSettings.RenderingFallback))
            {
                try { backend = new CompatibilityCanvasBackend(nativeWindow); }
                catch (Exception fallbackError) { throw new AggregateException("Both canvas backends failed to initialize.", gpuError, fallbackError); }
            }
        }
        try
        {
            DisplayServer.Service?.ApplyVSyncPolicy(backend);
            var server = new RenderingServer(window, backend);
            Volatile.Write(ref _instance, server);
            return server;
        }
        catch { backend.Dispose(); throw; }
    }

    internal DisplayServer.VSyncMode GetVSync() { EnsureOwner(); return _backend.VSync; }
    internal void SetVSync(DisplayServer.VSyncMode mode)
    {
        EnsureOwner(); if (_rendering) throw new InvalidOperationException("Presentation policy cannot change during rendering.");
        _backend.SetVSync(mode);
    }

    internal double CanvasTime { get; private set; }

    private sealed class CanvasFrame(Viewport viewport)
    {
        internal readonly Viewport Viewport = viewport;
        internal readonly List<CanvasItem> Nodes = [];
        internal readonly List<CanvasVertex> Vertices = [];
        internal readonly List<CanvasInstance> Instances = [];
        internal readonly List<CanvasBatch> Batches = [];
        internal readonly List<RenderEntry> Order = [];
        internal readonly Dictionary<CanvasItem, Transform> Repeats = [], Transforms = [];
        internal readonly List<YSortEntry> YSort = [];
        internal readonly Texture?[] TextureScratch = new Texture?[16];
        internal int State;
        internal bool Wanted, Drawn;
    }
    private Viewport _viewport;
    private Transform _canvasBasis;
    private readonly Dictionary<Viewport, CanvasFrame> _canvasFrames = new(ReferenceEqualityComparer.Instance);
    private readonly List<CanvasFrame> _activeFrames = [];
    private void UseFrame(CanvasFrame frame)
    { _viewport = frame.Viewport; _nodes = frame.Nodes; _vertices = frame.Vertices; _instances = frame.Instances; _batches = frame.Batches; _order = frame.Order; _repeatTransforms = frame.Repeats; _canvasTransforms = frame.Transforms; _ySort = frame.YSort; }
    private Color FrameClear(Viewport viewport) => viewport.TransparentBG ? default : _clearColor with { A = 1 };
    private void CaptureViewports(Node node)
    {
        CaptureScene(node);
        foreach (var viewport in _sceneViewports)
        {
            if (!ReferenceEquals(viewport, _window) && viewport is not SubViewport && viewport is not Window { Embedder: not null }) continue;
            if (!_canvasFrames.TryGetValue(viewport, out var frame)) _canvasFrames.Add(viewport, frame = new(viewport));
            viewport.RenderingOwner = this; frame.State = 0; frame.Wanted = frame.Drawn = false; _activeFrames.Add(frame);
            var size = ReferenceEquals(viewport, _window) ? _backend.GetPixelSize() : viewport is SubViewport sub ? sub.Size : ((Window)viewport).Size;
            if (size.X > 0 && size.Y > 0) _backend.Target(viewport, size, FrameClear(viewport));
        }
    }
    internal void InvalidateViewportRecordings(Viewport viewport)
    { EnsureOwner(); InvalidateViewportRecordings(_window, viewport); }
    private static void InvalidateViewportRecordings(Node node, Viewport viewport)
    { if (node is CanvasItem item && ReferenceEquals(item.CanvasViewport, viewport)) item.QueueRedraw(); for (var i = 0; i < node.GetChildCount(includeInternal: true); i++) InvalidateViewportRecordings(node.GetChild(i, includeInternal: true), viewport); }
    internal void SetWindowVisible(DisplayServer display, bool visible) { EnsureViewportMutation(); _backend.SetWindowVisible(display, visible); }
    internal void EnsureViewportMutation() { EnsureOwner(); if (_submittingTextures) throw new InvalidOperationException("Viewport targets cannot mutate during native submission."); }
    internal void ReleaseViewport(Viewport viewport) { EnsureOwner(); if (_submittingTextures) throw new InvalidOperationException("Viewport targets cannot be released during native submission."); _backend.ReleaseTarget(viewport); _canvasFrames.Remove(viewport); viewport.RenderingOwner = null; }
    internal Vector2i ViewportDimensions(Viewport viewport) { EnsureOwner(); return ReferenceEquals(viewport, _window) ? _backend.GetPixelSize() : viewport is SubViewport sub ? sub.Size : ((Window)viewport).Size; }
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
            FramePreDrawCore?.Invoke();
            if (!double.IsFinite(step) || step < 0 || !double.IsFinite(CanvasTime + step)) throw new InvalidOperationException("The render clock step is invalid.");
            CanvasTime = (CanvasTime + step) % ProjectSettings.GetWithOverride(ProjectSettings.RenderingTimeRolloverSeconds);
            _interpolationFraction = tree.PhysicsInterpolation ? (float)Engine.PhysicsInterpolationFraction : 1f;
            _activeFrames.Clear(); CaptureViewports(tree.Root);
            if (_activeFrames.Count != 0) { UseFrame(_activeFrames[0]); _nodes.Clear(); CaptureCanvasScene(tree.Root); foreach (var node in _nodes) if (!node.IsDisposed && ReferenceEquals(node.Tree, tree) && node.RenderVisible && node.RenderCanvas() is not null && node.ServerState is not { Owned: true }) node.PrepareCanvas(); }
            _activeFrames.Clear(); CaptureViewports(tree.Root);
            foreach (var viewport in _canvasFrames.Keys) if (viewport.IsDisposed || !ReferenceEquals(viewport.Tree, tree)) { _backend.ReleaseTarget(viewport); viewport.RenderingOwner = null; _canvasFrames.Remove(viewport); }
            _backend.BeginFrame();
            if (_window.Visible && _canvasFrames.TryGetValue(_window, out var root)) { root.Wanted = true; SubmitFrame(root, tree); }
            for (var i = 0; i < _activeFrames.Count; i++) { var frame = _activeFrames[i]; if (frame.Viewport is SubViewport sub && sub.RenderTargetUpdateMode is ViewportUpdateMode.Always or ViewportUpdateMode.Once) { frame.Wanted = true; SubmitFrame(frame, tree); } }
            _backend.EndFrame();
            foreach (var frame in _activeFrames) if (frame.Drawn) { UseFrame(frame); DispatchScreenVisibility(tree); }
            FramePostDrawCore?.Invoke();
        }
        finally
        {
            foreach (var frame in _activeFrames) { frame.Nodes.Clear(); frame.Vertices.Clear(); frame.Instances.Clear(); frame.Batches.Clear(); Array.Clear(frame.TextureScratch); frame.Order.Clear(); frame.Repeats.Clear(); frame.Transforms.Clear(); frame.YSort.Clear(); }
            _activeFrames.Clear(); _submittingTextures = false; _rendering = false;
        }
    }
    private void SubmitTexture(Texture? texture, SceneTree tree)
    {
        while (texture is AtlasTexture atlas) texture = atlas.RenderingTexture;
        if (texture is not ViewportTexture view || view.Bound is not { } viewport || !_canvasFrames.TryGetValue(viewport, out var frame) || !ReferenceEquals(viewport.Tree, tree)) return;
        if (viewport is SubViewport { RenderTargetUpdateMode: ViewportUpdateMode.Disabled } || viewport is Window { Visible: false }) return;
        frame.Wanted = true; SubmitFrame(frame, tree);
    }
    private void SubmitFrame(CanvasFrame frame, SceneTree tree)
    {
        if (frame.State != 0 || !frame.Wanted || frame.Viewport.IsDisposed || !ReferenceEquals(frame.Viewport.Tree, tree)) return;
        if (frame.Viewport is SubViewport { RenderTargetUpdateMode: ViewportUpdateMode.Disabled } || frame.Viewport is Window { Visible: false }) return;
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
        try { _backend.Draw(target, CollectionsMarshal.AsSpan(frame.Vertices), CollectionsMarshal.AsSpan(frame.Batches), FrameClear(frame.Viewport), frame.Viewport is not SubViewport sub || sub.RenderTargetClearMode != ViewportClearMode.Never, ReferenceEquals(frame.Viewport, _window) && _window.Visible, CanvasTime, CollectionsMarshal.AsSpan(frame.Instances)); }
        finally { _submittingTextures = false; }
        if (frame.Viewport is SubViewport completed) completed.Submitted();
        frame.State = 2; frame.Drawn = true;
    }
    private void BuildFrame(CanvasFrame frame, SceneTree tree)
    {
        UseFrame(frame); _instances.Clear(); _vertices.Clear(); _batches.Clear(); _order.Clear(); _repeatTransforms.Clear(); _canvasTransforms.Clear(); _ySort.Clear();
        var viewport = frame.Viewport; var pixels = _backend.FindTarget(viewport)!.Size;
        var framebufferTransform = ReferenceEquals(viewport, _window) ? new Transform(0f, new Vector2((float)pixels.X / _window.Size.X, (float)pixels.Y / _window.Size.Y), 0f, Vector2.Zero) : Transform.Identity;
        // Drawing callbacks may change parenting, visibility or sibling order.
        _nodes.Clear();
        CaptureCanvasScene(tree.Root);
        var visibleCount = 0;
        for (var index = 0; index < _nodes.Count; index++)
        {
            var node = _nodes[index];
            if ((node.RenderLive(tree) || node is VisibleOnScreenNotifier && !node.IsDisposed && ReferenceEquals(node.Tree, tree)) && node.RenderCanvas() is { } canvas && canvas.View(viewport, out _, out _, out _))
                _nodes[visibleCount++] = node;
        }
        _nodes.RemoveRange(visibleCount, _nodes.Count - visibleCount);
        foreach (var node in _nodes)
            if (node is VisibleOnScreenNotifier notifier) notifier.ScreenCandidate = false;
        _canvasChildOrder.Clear(); _canvasRootOrder.Clear();
        foreach (var node in _nodes) if (node.RenderParent is null && node.RenderCanvas() is { } canvas)
            {
                _canvasRootOrder.TryGetValue(canvas.RID, out var index);
                _canvasChildOrder.Add((node, node.ServerState is { Owned: true } ? 0 : index, _canvasChildOrder.Count));
                if (node.ServerState is not { Owned: true }) _canvasRootOrder[canvas.RID] = index + 1;
            }
        SortCanvasOrder(_canvasChildOrder, 0);
        var rootCount = _canvasChildOrder.Count;
        try
        {
            for (var i = 0; i < rootCount; i++)
            {
                var node = _canvasChildOrder[i].Node;
                if (node.RenderCanvas() is not { } canvas || !canvas.View(viewport, out var canvasTransform, out var canvasLayer, out var sublayer, _interpolationFraction)) continue;
                _canvasStacking = ((long)canvasLayer << 32) + (uint)(sublayer ^ int.MinValue);
                _canvasTooltipOverlay = SceneTree.IsTooltipNode(node);
                _canvasID = ReferenceEquals(canvas, viewport.DefaultCanvasRuntime) ? 0 : (ulong)canvas.RID.GetID();
                _canvasBasis = framebufferTransform * canvasTransform;
                OrderCanvas(node, _canvasBasis);
            }
        }
        finally { _canvasChildOrder.Clear(); _canvasRootOrder.Clear(); }
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

    private static Vector2 NativeMirror(CanvasItem item) => item.RenderParent is null && item.RenderCanvas() is { } canvas && canvas.Mirroring.Count != 0 && canvas.Mirroring.TryGetValue(item.GetCanvasItem(), out var size) ? size : Vector2.Zero;
    private static Vector2 RepeatSize(CanvasItem item) => item is Parallax parallax ? parallax.RepeatSize : item is ParallaxLayer layer ? layer.RepeatPeriod : NativeMirror(item);
    private void AppendOrderedCanvas(RenderEntry item, Vector2i pixels)
    {
        CanvasItem? repeatSource = null;
        for (var ancestor = item.Node; ancestor is not null; ancestor = ancestor.RenderParent)
            if ((ancestor is Parallax parallax && parallax.RepeatSize != Vector2.Zero) ||
                (ancestor is ParallaxLayer layer && layer.RepeatPeriod != Vector2.Zero) || NativeMirror(ancestor) != Vector2.Zero)
            {
                repeatSource = ancestor;
                break;
            }
        if (repeatSource is null)
        {
            var clip = GetClip(item.Node, pixels);
            if (((clip is null || clip.Value.HasArea()) && !HasEmptyOwnClip(item.Node, pixels) || item.Node.MayIgnoreClip))
            {
                AppendScreenCanvas(item.Node, item.Transform, clip, pixels);
            }
            return;
        }
        var size = RepeatSize(repeatSource);
        var times = repeatSource is Parallax repeated ? repeated.RepeatTimes : 1;
        var sourceTransform = _repeatTransforms[repeatSource];
        var start = size * -(times / 2);
        var countX = size.X == 0 ? 0 : times;
        var countY = size.Y == 0 ? 0 : times;
        var repeatedClip = GetClip(item.Node, pixels, repeatSource);
        if (!item.Node.MayIgnoreClip && (repeatedClip is { } empty && !empty.HasArea() || HasEmptyOwnClip(item.Node, pixels, repeatSource))) return;
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
        CaptureScene(node);
        _nodes.AddRange(_sceneCanvasItems);
    }

    private void CaptureScene(Node root)
    {
        var tree = root.Tree!;
        if (ReferenceEquals(tree, _capturedTree) && tree.PathRevision == _capturedRevision) return;
        _sceneCanvasItems.Clear();
        _sceneViewports.Clear();
        CaptureSceneNodes(root);
        _capturedTree = tree;
        _capturedRevision = tree.PathRevision;
    }

    private void CaptureSceneNodes(Node node)
    {
        if (node is CanvasItem item) _sceneCanvasItems.Add(item);
        if (node is Viewport viewport) _sceneViewports.Add(viewport);
        var children = node.AllChildren;
        for (var i = 0; i < children.Count; i++) CaptureSceneNodes(children[i]);
    }

    private void OrderCanvas(CanvasItem item, Transform transform, bool alreadyYSorted = false)
    {
        if (!item.RenderVisible || (item.RenderVisibilityLayer & _viewport.CanvasCullMask) == 0) return;
        if (item is ParallaxLayer layer) _repeatTransforms[layer] = transform;
        if (!alreadyYSorted)
        {
            var local = item.RenderLocal(_interpolationFraction);
            if (_viewport.SnapTransformsToPixel)
            {
                transform.Origin = CanvasGeometry.Snap(transform.Origin);
                local.Origin = CanvasGeometry.Snap(local.Origin);
            }
            transform *= local;
        }
        if (item.RenderYSort)
        {
            if (alreadyYSorted && !IsCompositor(item))
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
                if (!IsCompositor(item) || !ReferenceEquals(_ySort[index].Node, item)) OrderCanvas(_ySort[index].Node, transform * _ySort[index].Transform, alreadyYSorted: true);
            if (IsCompositor(item)) AddRenderEntry(item, transform);
            _ySort.RemoveRange(first, count);
            return;
        }
        OrderChildren(item, transform, behind: true);
        if (IsCompositor(item)) { OrderChildren(item, transform, behind: false); AddRenderEntry(item, transform); }
        else { AddRenderEntry(item, transform); OrderChildren(item, transform, behind: false); }
    }

    private void AddRenderEntry(CanvasItem item, Transform transform)
    {
        if (item is Parallax parallax) _repeatTransforms[parallax] = transform;
        else if (NativeMirror(item) != Vector2.Zero) _repeatTransforms[item] = _canvasBasis;
        _canvasTransforms[item] = transform;
        if (!IsCompositor(item) && item.ServerState?.CustomRect is { } custom)
        {
            var area = transform * custom.Abs();
            for (var parent = item; parent is not null; parent = parent.RenderParent)
                if (NativeMirror(parent) != Vector2.Zero) { area = ExpandRepeatedClip(area, parent); break; }
            var size = _backend.FindTarget(_viewport)!.Size;
            if (!area.Intersects(new Rect2(Vector2.Zero, size), includeBorders: true)) return;
        }
        _order.Add(new(item, _canvasTooltipOverlay, _canvasStacking, _canvasID, item.RenderZ, _order.Count, item.CanvasUsesWorldCoordinates ? _canvasBasis : transform, ParentGroup(item)));
    }

    private bool HasEmptyOwnClip(CanvasItem item, Vector2i pixels, CanvasItem? repeatSource = null) =>
        (item.RenderClip is not null) && GetClip(item, pixels, repeatSource, includeSelf: true) is { } clip && !clip.HasArea();

    private Rect2i? GetClip(CanvasItem item, Vector2i pixels, CanvasItem? repeatSource = null, bool includeSelf = false)
    {
        Rect2? clipped = null;
        var repeat = repeatSource is not null && !ReferenceEquals(item, repeatSource);
        for (var ancestor = item; ancestor is not null; ancestor = ancestor.RenderParent)
        {
            if (ReferenceEquals(ancestor, repeatSource)) repeat = false;
            var localClip = (includeSelf || !ReferenceEquals(ancestor, item) || ancestor.ServerState?.Clip == true || ancestor.CanvasClipRect is not null) ? ancestor.RenderClip : null;
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
        var size = RepeatSize(source);
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
        var first = CollectCanvasChildren(item); var count = _canvasChildOrder.Count;
        try { for (var i = first; i < count; i++) if (_canvasChildOrder[i].Node.RenderBehind == behind) OrderCanvas(_canvasChildOrder[i].Node, transform); }
        finally { _canvasChildOrder.RemoveRange(first, _canvasChildOrder.Count - first); }
    }

    private void CollectYSort(CanvasItem parent, Transform parentTransform)
    {
        var first = CollectCanvasChildren(parent); var count = _canvasChildOrder.Count;
        try { for (var i = first; i < count; i++) AddYSorted(_canvasChildOrder[i].Node, parentTransform); }
        finally { _canvasChildOrder.RemoveRange(first, _canvasChildOrder.Count - first); }
    }
    private void AddYSorted(CanvasItem child, Transform parentTransform)
    {
        if (!child.RenderVisible || (child.RenderVisibilityLayer & _viewport.CanvasCullMask) == 0) return;
        var local = child.RenderLocal(_interpolationFraction);
        if (_viewport.SnapTransformsToPixel) local.Origin = CanvasGeometry.Snap(local.Origin);
        var transform = parentTransform * local;
        _ySort.Add(new(child, transform, _ySort.Count));
        if (child.RenderYSort && !IsCompositor(child)) CollectYSort(child, transform);
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
                ReleaseOwnedSkeletons();
                ReleaseOwnedCanvasGraph();
                ReleaseOwnedPrograms();
                try { ReleaseOwnedMeshes(); } catch (Exception error) { (errors ??= []).Add(error); }
                try { ReleaseOwnedTextures(); } catch (Exception error) { (errors ??= []).Add(error); }
                try { _backend.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
            }
            finally
            {
                FramePreDrawCore = FramePostDrawCore = null; foreach (var viewport in _canvasFrames.Keys) viewport.RenderingOwner = null; _canvasFrames.Clear(); _activeFrames.Clear();
                _nodes.Clear(); _vertices.Clear(); _instances.Clear(); _batches.Clear(); _order.Clear(); _repeatTransforms.Clear(); _canvasTransforms.Clear(); _ySort.Clear();
                _sceneCanvasItems.Clear(); _sceneViewports.Clear(); _capturedTree = null;
                if (ReferenceEquals(Service, this)) Volatile.Write(ref _instance, null);
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

    private readonly record struct RenderEntry(CanvasItem Node, bool TooltipOverlay, long Stacking, ulong CanvasID, int Z, int Order, Transform Transform, CanvasItem? Group);
    private readonly record struct YSortEntry(CanvasItem Node, Transform Transform, int Order);
}
