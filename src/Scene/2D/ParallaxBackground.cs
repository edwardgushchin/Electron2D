namespace Electron2D;

/// <summary>Places independently scrolling spatial layers behind the main canvas.</summary>
/// <remarks>Direct <see cref="ParallaxLayer"/> children receive camera-relative offsets on the owning scene thread.
/// A layer can also be scrolled manually without a camera. Rendering uses the inherited canvas-layer order.</remarks>
public sealed class ParallaxBackground : CanvasLayer
{
    private Vector2 _scrollOffset, _baseOffset, _limitBegin, _limitEnd, _screenOffset, _finalOffset;
    private Vector2 _baseScale = Vector2.One;
    private float _cameraScale = 1;
    private bool _ignoreCameraZoom;

    /// <summary>Creates a background at layer minus one hundred, behind the default canvas.</summary>
    public ParallaxBackground() => Layer = -100;

    /// <summary>Gets or sets the manual scroll offset; an active camera overwrites it on its next update.</summary>
    /// <value>Zero initially, in canvas units.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public Vector2 ScrollOffset
    {
        get { CheckState(); return _scrollOffset; }
        set { EnsureMutable(); Finite(value); _scrollOffset = value; UpdateScroll(); }
    }

    /// <summary>Gets or sets the base displacement applied before camera scrolling.</summary>
    /// <value>Zero initially, in canvas units.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public Vector2 ScrollBaseOffset
    {
        get { CheckState(); return _baseOffset; }
        set { EnsureMutable(); Finite(value); _baseOffset = value; UpdateScroll(); }
    }

    /// <summary>Gets or sets the per-axis multiplier for the camera or manual scroll offset.</summary>
    /// <value>One on each axis initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public Vector2 ScrollBaseScale
    {
        get { CheckState(); return _baseScale; }
        set { EnsureMutable(); Finite(value); _baseScale = value; UpdateScroll(); }
    }

    /// <summary>Gets or sets the lower scroll limit. An axis is limited only when its begin is below its end.</summary>
    /// <value>Zero initially, in canvas units.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public Vector2 ScrollLimitBegin
    {
        get { CheckState(); return _limitBegin; }
        set { EnsureMutable(); Finite(value); _limitBegin = value; UpdateScroll(); }
    }

    /// <summary>Gets or sets the upper scroll limit. The visible viewport size participates in clamping.</summary>
    /// <value>Zero initially, disabling both axes.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public Vector2 ScrollLimitEnd
    {
        get { CheckState(); return _limitEnd; }
        set { EnsureMutable(); Finite(value); _limitEnd = value; UpdateScroll(); }
    }

    /// <summary>Gets or sets whether camera zoom is removed from child layer positions and scales.</summary>
    /// <value>False initially. A changed policy applies at the next scroll update.</value>
    public bool ScrollIgnoreCameraZoom
    {
        get { CheckState(); return _ignoreCameraZoom; }
        set { EnsureMutable(); _ignoreCameraZoom = value; }
    }

    internal void CameraMoved(Transform canvas, Vector2 screenOffset)
    {
        _screenOffset = screenOffset;
        _cameraScale = canvas.Scale.X * .5f + canvas.Scale.Y * .5f;
        _scrollOffset = canvas.Origin;
        UpdateScroll();
    }

    internal void CanvasViewportChanged(Viewport? oldViewport, Viewport? newViewport)
    {
        oldViewport?.UnregisterParallaxBackground(this);
        newViewport?.RegisterParallaxBackground(this);
        UpdateScroll();
    }

    internal void RefreshLayer(ParallaxLayer layer)
    {
        if (!IsInsideTree || !ReferenceEquals(layer.Parent, this)) return;
        var (offset, scale) = GetLayerMotion();
        layer.ApplyBackground(offset, scale);
    }

    internal override void OnTreeMembershipChanged(bool entering)
    {
        if (!entering)
        {
            try { CanvasViewport?.UnregisterParallaxBackground(this); }
            finally { base.OnTreeMembershipChanged(false); }
            return;
        }
        base.OnTreeMembershipChanged(true);
        CanvasViewport!.RegisterParallaxBackground(this);
        UpdateScroll();
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Where(property => property.Name != nameof(Layer)).Concat(BackgroundProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateBackground;

    private static Node CreateBackground() => new ParallaxBackground();

    private void UpdateScroll()
    {
        if (!IsInsideTree || CanvasViewport is not { } viewport) return;
        var scroll = -(_baseOffset + _scrollOffset * _baseScale);
        var size = viewport.GetVisibleRect().Size;
        if (_limitBegin.X < _limitEnd.X)
            scroll.X = scroll.X < _limitBegin.X ? _limitBegin.X : scroll.X + size.X > _limitEnd.X ? _limitEnd.X - size.X : scroll.X;
        if (_limitBegin.Y < _limitEnd.Y)
            scroll.Y = scroll.Y < _limitBegin.Y ? _limitBegin.Y : scroll.Y + size.Y > _limitEnd.Y ? _limitEnd.Y - size.Y : scroll.Y;
        _finalOffset = -scroll;
        if (!_finalOffset.IsFinite()) throw new InvalidOperationException("Background scroll overflowed finite coordinates.");
        var (offset, scale) = GetLayerMotion();
        List<Exception>? errors = null;
        foreach (var child in Children.ToArray())
            if (child is ParallaxLayer layer && !layer.IsDisposed && ReferenceEquals(layer.Parent, this))
                try { layer.ApplyBackground(offset, scale); }
                catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("One or more background layers failed to update.", errors);
    }

    private (Vector2 Offset, float Scale) GetLayerMotion()
    {
        var scale = _ignoreCameraZoom ? 1f : _cameraScale;
        var offset = _ignoreCameraZoom ? (_finalOffset + _screenOffset * (_cameraScale - 1)) / _cameraScale : _finalOffset;
        if (!offset.IsFinite() || !float.IsFinite(scale)) throw new InvalidOperationException("Background camera transform is invalid.");
        return (offset, scale);
    }

    private void CheckState() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static void Finite(Vector2 value)
    {
        if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value), "Scroll coordinates must be finite.");
    }

    private static readonly PropertyDescriptor[] BackgroundProperties =
    [
        new PropertyDescriptor<ParallaxBackground, int>(nameof(Layer), n => n.Layer, (n, v) => n.Layer = v, _ => -100, stored: true),
        new PropertyDescriptor<ParallaxBackground, Vector2>(nameof(ScrollOffset), n => n.ScrollOffset, (n, v) => n.ScrollOffset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<ParallaxBackground, Vector2>(nameof(ScrollBaseOffset), n => n.ScrollBaseOffset, (n, v) => n.ScrollBaseOffset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<ParallaxBackground, Vector2>(nameof(ScrollBaseScale), n => n.ScrollBaseScale, (n, v) => n.ScrollBaseScale = v, _ => Vector2.One, stored: true),
        new PropertyDescriptor<ParallaxBackground, Vector2>(nameof(ScrollLimitBegin), n => n.ScrollLimitBegin, (n, v) => n.ScrollLimitBegin = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<ParallaxBackground, Vector2>(nameof(ScrollLimitEnd), n => n.ScrollLimitEnd, (n, v) => n.ScrollLimitEnd = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<ParallaxBackground, bool>(nameof(ScrollIgnoreCameraZoom), n => n.ScrollIgnoreCameraZoom, (n, v) => n.ScrollIgnoreCameraZoom = v, _ => false, stored: true),
    ];
}
