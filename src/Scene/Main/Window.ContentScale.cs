namespace Electron2D;

/// <summary>Selects how window content is rasterized before presentation.</summary>
public enum ContentScaleMode
{
    /// <summary>Use the client resolution, with optional content scale factor.</summary>
    Disabled = 0,
    /// <summary>Rasterize canvas geometry at the fitted client resolution.</summary>
    CanvasItems = 1,
    /// <summary>Rasterize at the logical viewport resolution, then enlarge its image.</summary>
    Viewport = 2
}
/// <summary>Selects the logical resolution and margins when window and content aspect ratios differ.</summary>
public enum ContentScaleAspect
{
    /// <summary>Stretch independently on both axes.</summary>
    Ignore = 0,
    /// <summary>Keep the authored logical size and fit it with margins.</summary>
    Keep = 1,
    /// <summary>Expand logical height for a taller client; otherwise use margins.</summary>
    KeepWidth = 2,
    /// <summary>Expand logical width for a wider client; otherwise use margins.</summary>
    KeepHeight = 3,
    /// <summary>Expand the logical area to fill the client without margins.</summary>
    Expand = 4
}
/// <summary>Selects fractional or whole-number window presentation scaling.</summary>
public enum ContentScaleStretch
{
    /// <summary>Fit content with fractional scale ratios.</summary>
    Fractional = 0,
    /// <summary>Use whole-number scaling of at least one, clipping content if the client is smaller.</summary>
    Integer = 1
}

public partial class Window
{
    private Vector2i _contentScaleSize;
    private ContentScaleMode _contentScaleMode;
    private ContentScaleAspect _contentScaleAspect;
    private ContentScaleStretch _contentScaleStretch;
    private float _contentScaleFactor = 1;
    private WindowContentLayout _contentLayout;
    private volatile bool _contentLayoutValid;
    private readonly object _contentLayoutGate = new();

    /// <summary>Gets or sets the desired content resolution before aspect fitting and scale factor.</summary>
    /// <value>Zero initially. Either zero axis disables resolution fitting; negative axes are invalid.</value>
    /// <remarks>Changes reflow the viewport and notify texture and SizeChanged observers after committing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An axis is negative or calculated dimensions are unrepresentable.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during native submission.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2i ContentScaleSize { get { ThrowIfDisposed(); return _contentScaleSize; } set { if (value.X < 0 || value.Y < 0) throw new ArgumentOutOfRangeException(nameof(value)); SetContent(ref _contentScaleSize, value); } }
    /// <summary>Gets or sets whether content uses client rasterization or a separate logical-resolution image.</summary>
    /// <value>Disabled initially. All three modes execute through the existing canvas renderer.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined or calculated dimensions are unrepresentable.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during native submission.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public ContentScaleMode ContentScaleMode { get { ThrowIfDisposed(); return _contentScaleMode; } set { Animation.Valid(value); SetContent(ref _contentScaleMode, value); } }
    /// <summary>Gets or sets the fitting policy for the content aspect ratio.</summary>
    /// <value>Ignore initially; Keep, KeepWidth, KeepHeight and Expand retain their distinct fitting rules.</value>
    /// <exception cref="ArgumentOutOfRangeException">The policy is undefined or calculated dimensions are unrepresentable.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during native submission.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public ContentScaleAspect ContentScaleAspect { get { ThrowIfDisposed(); return _contentScaleAspect; } set { Animation.Valid(value); SetContent(ref _contentScaleAspect, value); } }
    /// <summary>Gets or sets fractional or whole-number resolution fitting.</summary>
    /// <value>Fractional initially. Integer floors ContentScaleFactor and clamps it to at least one.</value>
    /// <exception cref="ArgumentOutOfRangeException">The policy is undefined or calculated dimensions are unrepresentable.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during native submission.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public ContentScaleStretch ContentScaleStretch { get { ThrowIfDisposed(); return _contentScaleStretch; } set { Animation.Valid(value); SetContent(ref _contentScaleStretch, value); } }
    /// <summary>Gets or sets the positive finite content scale factor.</summary>
    /// <value>One initially. Integer stretching stores its floored value, clamped to one.</value>
    /// <remarks>Disabled/CanvasItems scale geometry; Viewport changes the raster image resolution. Equal assignments are silent.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The factor is nonpositive, nonfinite or calculated dimensions are unrepresentable.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during native submission.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public float ContentScaleFactor { get { ThrowIfDisposed(); return _contentScaleFactor; } set { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); SetContent(ref _contentScaleFactor, value); } }

    private void SetContent<T>(ref T field, T value)
    {
        EnsureMutable(); RenderingOwner?.EnsureViewportMutation();
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        var previous = field; field = value;
        WindowContentLayout layout;
        try { layout = CalculateContentLayout(); } catch { field = previous; throw; }
        _contentScaleFactor = layout.Factor; _contentLayout = layout; _contentLayoutValid = true;
        InvalidateViewportRecording(); _embeddedCanvas?.QueueRedraw();
        NotifyContentSizeChanged();
    }
    private void NotifyContentSizeChanged()
    {
        List<Exception>? errors = null;
        try { NotifySizeChanged(); } catch (Exception error) { CollectException(ref errors, error); }
        try { DispatchNotification(NotificationWmSizeChanged); } catch (Exception error) { CollectException(ref errors, error); }
        try { InvalidateEmbeddedDensity(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Window content size notification failed.", errors);
    }
    private void InvalidateEmbeddedDensity()
    {
        List<Exception>? errors = null;
        for (var i = 0; i < EmbeddedWindows.Count; i++)
        {
            var child = EmbeddedWindows[i];
            child.InvalidateViewportRecording(); child._embeddedCanvas?.QueueRedraw();
            try { child.TextureSizeChanged(); } catch (Exception error) { CollectException(ref errors, error); }
            try { child.InvalidateEmbeddedDensity(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Embedded content density notification failed.", errors);
    }
    internal WindowContentLayout ContentLayout
    {
        get
        {
            ThrowIfDisposed();
            if (_contentLayoutValid) return _contentLayout;
            lock (_contentLayoutGate)
            {
                if (!_contentLayoutValid) { _contentLayout = CalculateContentLayout(); _contentLayoutValid = true; }
                return _contentLayout;
            }
        }
    }
    internal float RenderDensity
    {
        get { var scale = Embedder?.StretchTransform.Scale ?? Vector2.One; return MathF.Min(MathF.Abs(scale.X), MathF.Abs(scale.Y)) * (Embedder is Window parent ? parent.RenderDensity : 1); }
    }
    internal Vector2i ContentRenderSize => new(Dimension(MathF.Ceiling(ContentLayout.TargetSize.X * RenderDensity)), Dimension(MathF.Ceiling(ContentLayout.TargetSize.Y * RenderDensity)));
    internal override Transform StretchTransform => ContentLayout.Stretch;
    internal override Transform WindowTransform => ContentLayout.Presentation;

    private WindowContentLayout CalculateContentLayout()
    {
        var factor = _contentScaleStretch == ContentScaleStretch.Integer ? MathF.Max(1, MathF.Floor(_contentScaleFactor)) : _contentScaleFactor;
        float vw = _size.X, vh = _size.Y, sw = vw, sh = vh;
        var fitted = _contentScaleMode != ContentScaleMode.Disabled && _contentScaleSize.X > 0 && _contentScaleSize.Y > 0;
        if (fitted)
        {
            vw = _contentScaleSize.X; vh = _contentScaleSize.Y;
            var desiredAspect = vw / vh; var clientAspect = (float)_size.X / _size.Y;
            if (_contentScaleAspect != ContentScaleAspect.Ignore && !Mathf.IsEqualApprox((float)desiredAspect, (float)clientAspect))
            {
                if (desiredAspect < clientAspect)
                {
                    if (_contentScaleAspect is ContentScaleAspect.KeepHeight or ContentScaleAspect.Expand) vw = vh * clientAspect;
                    else sw = sh * desiredAspect;
                }
                else
                {
                    if (_contentScaleAspect is ContentScaleAspect.KeepWidth or ContentScaleAspect.Expand) vh = vw / clientAspect;
                    else sh = sw / desiredAspect;
                }
            }
            vw = MathF.Floor(vw); vh = MathF.Floor(vh); sw = MathF.Floor(sw); sh = MathF.Floor(sh);
            if (_contentScaleStretch == ContentScaleStretch.Integer)
            {
                var fit = MathF.Max(1, MathF.Min(MathF.Floor(sw / vw), MathF.Floor(sh / vh)));
                sw = vw * fit; sh = vh * fit;
            }
        }
        var viewportMode = fitted && _contentScaleMode == ContentScaleMode.Viewport;
        var target = viewportMode ? new Vector2i(Dimension(Math.Floor(vw / factor)), Dimension(Math.Floor(vh / factor))) : new Vector2i(Dimension(sw), Dimension(sh));
        var visible = viewportMode ? (Vector2)target : new Vector2(Scalar(vw / factor), Scalar(vh / factor));
        var rect = new Rect2i(new(Dimension(Math.Max(0, Math.Round((_size.X - sw) / 2, MidpointRounding.AwayFromZero))), Dimension(Math.Max(0, Math.Round((_size.Y - sh) / 2, MidpointRounding.AwayFromZero)))), new(Dimension(sw), Dimension(sh)));
        var stretch = visible.X > 0 && visible.Y > 0 ? new Transform(new((float)target.X / visible.X, 0), new(0, (float)target.Y / visible.Y), Vector2.Zero) : Transform.Identity;
        var presentationScale = viewportMode && target.X > 0 && target.Y > 0 ? new Vector2((float)rect.Size.X / target.X, (float)rect.Size.Y / target.Y) : Vector2.One;
        var presentation = new Transform(0, presentationScale, 0, rect.Position);
        if (!stretch.IsFinite() || !presentation.IsFinite()) throw new ArgumentOutOfRangeException(nameof(ContentScaleFactor), "Content transforms exceed finite coordinates.");
        return new(target, visible, rect, stretch, presentation, factor);
    }
    private static int Dimension(double value) => double.IsFinite(value) && value >= 0 && value <= int.MaxValue ? (int)value : throw new ArgumentOutOfRangeException(nameof(ContentScaleSize), "Calculated content dimensions exceed integer coordinates.");
    private static float Scalar(double value) => double.IsFinite(value) && value <= float.MaxValue ? (float)value : throw new ArgumentOutOfRangeException(nameof(ContentScaleFactor), "Calculated content dimensions exceed finite coordinates.");
    private static readonly PropertyDescriptor[] ContentProperties =
    [
        new PropertyDescriptor<Window, Vector2i>(nameof(ContentScaleSize), w => w.ContentScaleSize, (w, v) => w.ContentScaleSize = v, _ => Vector2i.Zero, stored: true),
        new PropertyDescriptor<Window, ContentScaleMode>(nameof(ContentScaleMode), w => w.ContentScaleMode, (w, v) => w.ContentScaleMode = v, _ => ContentScaleMode.Disabled, stored: true),
        new PropertyDescriptor<Window, ContentScaleAspect>(nameof(ContentScaleAspect), w => w.ContentScaleAspect, (w, v) => w.ContentScaleAspect = v, _ => ContentScaleAspect.Ignore, stored: true),
        new PropertyDescriptor<Window, ContentScaleStretch>(nameof(ContentScaleStretch), w => w.ContentScaleStretch, (w, v) => w.ContentScaleStretch = v, _ => ContentScaleStretch.Fractional, stored: true),
        new PropertyDescriptor<Window, float>(nameof(ContentScaleFactor), w => w.ContentScaleFactor, (w, v) => w.ContentScaleFactor = v, _ => 1, stored: true)
    ];
}

internal readonly record struct WindowContentLayout(Vector2i TargetSize, Vector2 VisibleSize, Rect2i ScreenRect, Transform Stretch, Transform Presentation, float Factor);
