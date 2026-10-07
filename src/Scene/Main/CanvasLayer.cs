namespace Electron2D;

/// <summary>Places canvas descendants in an independent drawing layer.</summary>
/// <remarks>Layer order precedes item Z order. A layer has its own transform and optional viewport following;
/// it is a neutral scene node, not a spatial canvas item. Native ownership remains with the renderer.</remarks>
public partial class CanvasLayer : Node
{
    internal bool IsTooltipLayer { get; init; }
    private int _layer = 1;
    private bool _visible = true, _followViewportEnabled, _componentsDirty;
    private float _followViewportScale = 1, _rotation;
    private Vector2 _offset, _scale = Vector2.One;
    private Transform _transform = Transform.Identity;
    private Viewport? _viewport, _customViewport;

    /// <summary>Creates a visible layer at index one with an identity transform and no viewport following.</summary>
    public CanvasLayer() { }

    /// <summary>Gets or sets the layer's primary drawing order.</summary>
    /// <value>One by default; the inclusive range is RenderingServer.CanvasLayerMin through RenderingServer.CanvasLayerMax.
    /// Lower layers draw first, regardless of item Z.</value>
    /// <remarks>Equal layers remain separate groups. Sibling order breaks ties; ordering between unrelated
    /// equal-index layers is not a portable guarantee. Changes affect retained drawing on the next frame.</remarks>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public int Layer { get { CheckQuery(); return _layer; } set { EnsureMutable(); _layer = value; _canvasRuntime?.PublishLayer(false); } }

    /// <summary>Gets or sets whether direct canvas children and their canvas descendants are visible.</summary>
    /// <value>True initially. Nested CanvasLayer and neutral Node boundaries do not inherit this flag.</value>
    /// <remarks>Commits the value, raises VisibilityChanged, then propagates to direct canvas children.
    /// All affected children are attempted even when callbacks fail. Reentrant changes use the latest value.</remarks>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during scene capture.</exception>
    /// <exception cref="AggregateException">One or more visibility callbacks fail after the change.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public bool Visible
    {
        get { CheckQuery(); return _visible; }
        set
        {
            EnsureMutable(); if (_visible == value) return; _visible = value;
            List<Exception>? errors = null;
            try { VisibilityChanged?.Invoke(this); }
            catch (Exception error) { CollectException(ref errors, error); }
            if (!IsDisposed)
                foreach (var child in AllChildren.ToArray())
                    if (child is CanvasItem item && !item.IsDisposed && ReferenceEquals(item.Parent, this))
                        try { item.PropagateVisibilityChanged(); }
                        catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Canvas layer visibility callbacks failed.", errors);
        }
    }

    /// <summary>Gets or sets the layer transform before viewport following.</summary>
    /// <value>Identity initially; finite singular and skewed transforms are accepted.</value>
    /// <remarks>Component queries lazily decompose this matrix. Setting Offset, Rotation or Scale afterward
    /// reconstructs rotation and scale, discarding skew. PackedScene stores this canonical matrix.</remarks>
    /// <exception cref="ArgumentException">The matrix is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public Transform Transform
    {
        get { CheckQuery(); return _transform; }
        set { EnsureMutable(); Finite(value); _transform = value; _canvasRuntime?.PublishLayer(true); _componentsDirty = true; }
    }

    /// <summary>Gets or sets the layer offset in canvas units.</summary>
    /// <value>Zero initially.</value>
    /// <remarks>Setting a component rebuilds the matrix without skew, even when the component value is unchanged.</remarks>
    /// <exception cref="ArgumentException">The value or rebuilt matrix is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner, mutation occurs during scene capture, or component decomposition overflows.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public Vector2 Offset
    {
        get { CheckQuery(); UpdateComponents(); return _offset; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Offset must be finite.", nameof(value)); UpdateComponents(); SetComponents(value, _rotation, _scale); }
    }

    /// <summary>Gets or sets the layer rotation in radians.</summary>
    /// <value>Zero initially. Explicit finite angles are not normalized until matrix decomposition is needed.</value>
    /// <exception cref="ArgumentException">The value or rebuilt matrix is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner, mutation occurs during scene capture, or component decomposition overflows.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public float Rotation
    {
        get { CheckQuery(); UpdateComponents(); return _rotation; }
        set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentException("Rotation must be finite.", nameof(value)); UpdateComponents(); SetComponents(_offset, value, _scale); }
    }

    /// <summary>Gets or sets the layer scale.</summary>
    /// <value>One on both axes initially; finite zero and negative values are accepted.</value>
    /// <remarks>Matrix decomposition represents a reflection on the Y axis. Zero scale renders degenerate geometry;
    /// coordinate queries requiring an inverse then fail explicitly.</remarks>
    /// <exception cref="ArgumentException">The value or rebuilt matrix is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner, mutation occurs during scene capture, or component decomposition overflows.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public Vector2 Scale
    {
        get { CheckQuery(); UpdateComponents(); return _scale; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Scale must be finite.", nameof(value)); UpdateComponents(); SetComponents(_offset, _rotation, value); }
    }

    /// <summary>Gets or sets whether this layer follows the viewport's default canvas transform.</summary>
    /// <value>False initially, keeping the layer fixed on screen independently of a camera.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public bool FollowViewportEnabled { get { CheckQuery(); return _followViewportEnabled; } set { EnsureMutable(); _followViewportEnabled = value; } }

    /// <summary>Gets or sets the scale applied when viewport following is enabled.</summary>
    /// <value>One initially; any finite value, including zero and negative values, is accepted.</value>
    /// <remarks>Rendering scales around the viewport center; GetFinalTransform describes logical coordinates
    /// using a scale before the layer transform. These differ for nonunit follow scale.</remarks>
    /// <exception cref="ArgumentException">The scale is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public float FollowViewportScale
    {
        get { CheckQuery(); return _followViewportScale; }
        set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentException("Follow scale must be finite.", nameof(value)); _followViewportScale = value; }
    }

    /// <summary>Gets or sets the borrowed viewport used instead of the containing viewport.</summary>
    /// <value>Null initially. Null or a non-Viewport node restores the containing viewport.</value>
    /// <remarks>Runtime-only, not packed. Attached targets must be active in the same tree. Independent native
    /// windows, embedded viewports and offscreen targets are not integrated with the renderer.</remarks>
    /// <exception cref="NotSupportedException">The target is not active in this tree.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or no containing viewport exists.</exception>
    /// <exception cref="ObjectDisposedException">The layer or assigned node is disposed.</exception>
    public Node? CustomViewport
    {
        get { CheckQuery(); return _customViewport; }
        set
        {
            EnsureMutable(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
            var custom = value as Viewport;
            var target = Tree is null ? null : ResolveViewport(custom);
            var previous = _viewport;
            _customViewport = custom; _viewport = target;
            if (this is ParallaxBackground background && !ReferenceEquals(previous, target))
                background.CanvasViewportChanged(previous, target);
        }
    }

    /// <summary>Returns the logical transform from layer to viewport coordinates.</summary>
    /// <returns>The layer transform when not following; otherwise viewport CanvasTransform times follow scale times layer transform.
    /// A detached layer uses identity in place of the viewport transform.</returns>
    /// <remarks>Excludes GlobalCanvasTransform, pixel snapping and the renderer's center-based follow scaling.</remarks>
    /// <exception cref="InvalidOperationException">The query is off-owner or composition overflows finite coordinates.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public Transform GetFinalTransform()
    {
        CheckQuery();
        var result = _followViewportEnabled
            ? (_viewport?.CanvasTransform ?? Transform.Identity) * new Transform(0, Vector2.One * _followViewportScale, 0, Vector2.Zero) * _transform
            : _transform;
        if (!result.IsFinite()) throw new InvalidOperationException("Canvas layer coordinates overflowed.");
        return result;
    }

    /// <summary>Shows the layer's canvas children by setting Visible to true.</summary>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="AggregateException">A visibility callback fails after the change.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public void Show() => Visible = true;

    /// <summary>Hides the layer's canvas children by setting Visible to false.</summary>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="AggregateException">A visibility callback fails after the change.</exception>
    /// <exception cref="ObjectDisposedException">The layer is disposed.</exception>
    public void Hide() => Visible = false;

    /// <summary>Occurs after the visibility value changes and before child visibility propagation.</summary>
    /// <remarks>Synchronous on the scene owner while attached; equal assignments do not emit. Handler failure
    /// stops later handlers but does not prevent child state from being reconciled.</remarks>
    public event Action<CanvasLayer>? VisibilityChanged;

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(LayerProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(CanvasLayer) ? CreateLayer : base.CreateSceneInstanceFactory();
    /// <inheritdoc />
    /// <remarks>Disposes owned children, then clears subscriptions and borrowed viewport references.</remarks>
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally { if (disposing) { if (_canvasRuntime is { } canvas) RenderingCanvasRegistry.Remove(canvas.RID); _canvasRuntime = null; VisibilityChanged = null; _viewport = _customViewport = null; } }
    }

    internal Viewport? CanvasViewport => _viewport;
    internal override void OnTreeMembershipChanged(bool entering) => _viewport = entering ? ResolveViewport(_customViewport) : null;

    private Viewport ResolveViewport(Viewport? custom)
    {
        var viewport = custom ?? GetViewport() ?? throw new InvalidOperationException("A canvas layer requires an active viewport.");
        if (viewport.IsDisposed) throw new ObjectDisposedException(nameof(CustomViewport));
        if (!ReferenceEquals(viewport.Tree, Tree)) throw new NotSupportedException("A canvas layer target must be active in the same scene tree.");
        return viewport;
    }
    private void CheckQuery() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static void Finite(Transform value) { if (!value.IsFinite()) throw new ArgumentException("Layer transform must be finite.", nameof(value)); }
    private void UpdateComponents()
    {
        if (!_componentsDirty) return;
        var scale = _transform.Scale;
        if (!scale.IsFinite()) throw new InvalidOperationException("Canvas layer scale decomposition overflowed finite coordinates.");
        _offset = _transform.Origin; _rotation = _transform.Rotation; _scale = scale; _componentsDirty = false;
    }
    private void SetComponents(Vector2 offset, float rotation, Vector2 scale)
    {
        var transform = new Transform(rotation, scale, 0, offset); Finite(transform);
        _offset = offset; _rotation = rotation; _scale = scale; _transform = transform; _canvasRuntime?.PublishLayer(true);
    }
    private static Node CreateLayer() => new CanvasLayer();
    private static readonly PropertyDescriptor[] LayerProperties =
    [
        new PropertyDescriptor<CanvasLayer, int>(nameof(Layer), n => n.Layer, (n, v) => n.Layer = v, _ => 1, stored: true),
        new PropertyDescriptor<CanvasLayer, bool>(nameof(Visible), n => n.Visible, (n, v) => n.Visible = v, _ => true, stored: true),
        new PropertyDescriptor<CanvasLayer, Transform>(nameof(Transform), n => n.Transform, (n, v) => n.Transform = v, _ => Transform.Identity, stored: true),
        new PropertyDescriptor<CanvasLayer, Vector2>(nameof(Offset), n => n.Offset, (n, v) => n.Offset = v, _ => Vector2.Zero),
        new PropertyDescriptor<CanvasLayer, float>(nameof(Rotation), n => n.Rotation, (n, v) => n.Rotation = v, _ => 0),
        new PropertyDescriptor<CanvasLayer, Vector2>(nameof(Scale), n => n.Scale, (n, v) => n.Scale = v, _ => Vector2.One),
        new PropertyDescriptor<CanvasLayer, bool>(nameof(FollowViewportEnabled), n => n.FollowViewportEnabled, (n, v) => n.FollowViewportEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<CanvasLayer, float>(nameof(FollowViewportScale), n => n.FollowViewportScale, (n, v) => n.FollowViewportScale = v, _ => 1, stored: true),
        new PropertyDescriptor<CanvasLayer, Node?>(nameof(CustomViewport), n => n.CustomViewport, (n, v) => n.CustomViewport = v, _ => null),
    ];
}
