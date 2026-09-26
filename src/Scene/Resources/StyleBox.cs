namespace Electron2D;

/// <summary>Defines reusable canvas decoration, content margins and an optional pointer mask.</summary>
/// <remarks>Resources are borrowed by callers. Changes emit synchronous Changed after committing state;
/// a custom control must request redraw and minimum-size refresh when its style changes. Drawing captures
/// commands during the target item's recording scope. Resource state is serialized, while custom hooks run
/// outside its lock. Retained commands do not automatically rerecord when this resource changes.</remarks>
public abstract class StyleBox : Resource
{
    internal readonly object StyleGate = new();
    private readonly float[] _contentMargins = [-1, -1, -1, -1];
    private static readonly PropertyDescriptor[] StyleProperties =
    [
        new PropertyDescriptor<StyleBox, float>(nameof(ContentMarginLeft), style => style.ContentMarginLeft, (style, value) => style.ContentMarginLeft = value, _ => -1, stored: true),
        new PropertyDescriptor<StyleBox, float>(nameof(ContentMarginTop), style => style.ContentMarginTop, (style, value) => style.ContentMarginTop = value, _ => -1, stored: true),
        new PropertyDescriptor<StyleBox, float>(nameof(ContentMarginRight), style => style.ContentMarginRight, (style, value) => style.ContentMarginRight = value, _ => -1, stored: true),
        new PropertyDescriptor<StyleBox, float>(nameof(ContentMarginBottom), style => style.ContentMarginBottom, (style, value) => style.ContentMarginBottom = value, _ => -1, stored: true)
    ];
    /// <summary>Initializes all content-margin overrides to minus one, selecting style-specific margins.</summary>
    protected StyleBox() { }
    /// <summary>Gets or sets the left content-margin override.</summary>
    /// <value>Minus one initially; any negative value selects the style margin. Every assignment emits Changed.</value>
    public float ContentMarginLeft { get => GetContentMargin(Side.Left); set => SetContentMargin(Side.Left, value); }
    /// <summary>Gets or sets the top content-margin override.</summary>
    /// <value>Minus one initially; any negative value selects the style margin. Every assignment emits Changed.</value>
    public float ContentMarginTop { get => GetContentMargin(Side.Top); set => SetContentMargin(Side.Top, value); }
    /// <summary>Gets or sets the right content-margin override.</summary>
    /// <value>Minus one initially; any negative value selects the style margin. Every assignment emits Changed.</value>
    public float ContentMarginRight { get => GetContentMargin(Side.Right); set => SetContentMargin(Side.Right, value); }
    /// <summary>Gets or sets the bottom content-margin override.</summary>
    /// <value>Minus one initially; any negative value selects the style margin. Every assignment emits Changed.</value>
    public float ContentMarginBottom { get => GetContentMargin(Side.Bottom); set => SetContentMargin(Side.Bottom, value); }
    /// <summary>Gets a stored content-margin override, without resolving its negative fallback.</summary>
    /// <param name="margin">The requested side.</param>
    /// <returns>The signed override, initially minus one.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float GetContentMargin(Side margin) { lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); return _contentMargins[(int)margin]; } }
    /// <summary>Sets one content-margin override and emits Changed even for an equal value.</summary>
    /// <param name="margin">The requested side.</param>
    /// <param name="offset">Finite local units; a negative value selects the style margin.</param>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined or the value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    /// <exception cref="Exception">A Changed observer fails after state commitment.</exception>
    public void SetContentMargin(Side margin, float offset)
    {
        lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); ValidateFloat(offset); _contentMargins[(int)margin] = offset; }
        EmitChanged();
    }
    /// <summary>Sets all four content margins atomically and emits Changed once, including equal values.</summary>
    /// <param name="offset">The finite override for every side.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    /// <exception cref="Exception">A Changed observer fails after state commitment.</exception>
    public void SetContentMarginAll(float offset)
    {
        lock (StyleGate) { ThrowIfDisposed(); ValidateFloat(offset); Array.Fill(_contentMargins, offset); }
        EmitChanged();
    }
    /// <summary>Gets the effective content margin after resolving a negative override.</summary>
    /// <param name="margin">The requested side.</param>
    /// <returns>The override or the concrete style margin, which may itself be negative.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float GetMargin(Side margin) { lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); return MarginCore(margin); } }
    private float MarginCore(Side side) => _contentMargins[(int)side] < 0 ? StyleMargin(side) : _contentMargins[(int)side];
    internal virtual float StyleMargin(Side side) => 0;
    /// <summary>Gets the componentwise maximum of opposite margin sums and the custom minimum hook.</summary>
    /// <returns>A finite local size; the default custom hook supplies zero.</returns>
    /// <exception cref="InvalidOperationException">Computed margins or custom size overflow finite geometry.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Vector2 GetMinimumSize()
    {
        Vector2 margins;
        lock (StyleGate) { ThrowIfDisposed(); margins = new(MarginCore(Side.Left) + MarginCore(Side.Right), MarginCore(Side.Top) + MarginCore(Side.Bottom)); }
        var custom = OnGetMinimumSize();
        if (!margins.IsFinite() || !custom.IsFinite()) throw new InvalidOperationException("Style minimum size must remain finite.");
        return new(MathF.Max(margins.X, custom.X), MathF.Max(margins.Y, custom.Y));
    }
    /// <summary>Gets the effective left and top content margins.</summary>
    /// <returns>The local content offset.</returns>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Vector2 GetOffset() { lock (StyleGate) { ThrowIfDisposed(); return new(MarginCore(Side.Left), MarginCore(Side.Top)); } }
    /// <summary>Queries the decoration's draw rectangle through its typed custom hook.</summary>
    /// <param name="rect">The finite requested local rectangle.</param>
    /// <returns>The finite expanded rectangle, or the input for the default hook.</returns>
    /// <remarks>This queries bounds; it does not alter the rectangle passed to Draw.</remarks>
    /// <exception cref="ArgumentException">The input rectangle is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The custom result is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Rect2 GetDrawRect(Rect2 rect)
    {
        ThrowIfDisposed(); ValidateRect(rect); var result = OnGetDrawRect(rect);
        if (!result.IsFinite()) throw new InvalidOperationException("Style draw bounds must remain finite.");
        return result;
    }
    /// <summary>Tests a local point using the custom mask hook.</summary>
    /// <param name="point">The finite local point.</param>
    /// <param name="rect">The finite local style rectangle.</param>
    /// <returns>The hook result; true by default even outside the rectangle.</returns>
    /// <exception cref="ArgumentException">The point or rectangle is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public bool TestMask(Vector2 point, Rect2 rect)
    {
        ThrowIfDisposed(); ValidateRect(rect); if (!point.IsFinite()) throw new ArgumentException("Mask coordinates must be finite.", nameof(point));
        return OnTestMask(point, rect);
    }
    /// <summary>Records this style's decoration on a currently recording canvas item.</summary>
    /// <param name="canvasItem">The live target item, on its recording owner thread.</param>
    /// <param name="rect">The finite local rectangle.</param>
    /// <exception cref="ArgumentNullException">The target is null.</exception>
    /// <exception cref="ArgumentException">The rectangle or derived texture geometry is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The item is not recording on its owner thread, or strip geometry exceeds integer pixel range.</exception>
    /// <exception cref="ObjectDisposedException">The style, item or borrowed drawing resource is disposed.</exception>
    public void Draw(CanvasItem canvasItem, Rect2 rect)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(canvasItem); canvasItem.ValidateStyleDraw(rect); OnDraw(canvasItem, rect);
    }
    /// <summary>Gets the canvas item recording commands on the calling thread.</summary>
    /// <returns>The active item during its draw notification, Draw event or OnDraw callback; otherwise null.</returns>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public CanvasItem? GetCurrentItemDrawn() { ThrowIfDisposed(); return CanvasItem.CurrentDrawingItem; }
    /// <summary>Records custom decoration during a validated target recording scope.</summary>
    /// <param name="canvasItem">The item recording canvas commands.</param>
    /// <param name="rect">The finite local rectangle.</param>
    protected abstract void OnDraw(CanvasItem canvasItem, Rect2 rect);
    /// <summary>Supplies an additional minimum size, combined with the effective margin sums.</summary>
    /// <returns>A finite local size; zero by default.</returns>
    protected virtual Vector2 OnGetMinimumSize() => Vector2.Zero;
    /// <summary>Queries custom decoration bounds without recording commands.</summary>
    /// <param name="rect">The requested finite local rectangle.</param>
    /// <returns>The finite draw rectangle; the input by default.</returns>
    protected virtual Rect2 OnGetDrawRect(Rect2 rect) => rect;
    /// <summary>Supplies the custom pointer mask.</summary>
    /// <param name="point">The finite point to query.</param>
    /// <param name="rect">The finite style rectangle.</param>
    /// <returns>True by default, without an implicit rectangle containment test.</returns>
    protected virtual bool OnTestMask(Vector2 point, Rect2 rect) => true;
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(StyleProperties);
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        Vector4 margins;
        lock (StyleGate) { ThrowIfDisposed(); margins = new(_contentMargins[0], _contentMargins[1], _contentMargins[2], _contentMargins[3]); }
        var copy = (StyleBox)target;
        copy.ContentMarginLeft = margins.X; copy.ContentMarginTop = margins.Y; copy.ContentMarginRight = margins.Z; copy.ContentMarginBottom = margins.W;
    }
    internal static void ValidateSide(Side side) { if (side is < Side.Left or > Side.Bottom) throw new ArgumentOutOfRangeException(nameof(side)); }
    internal static void ValidateFloat(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), "Style values must be finite."); }
    internal static void ValidateRect(Rect2 rect) { if (!rect.IsFinite()) throw new ArgumentException("Style rectangles must be finite.", nameof(rect)); }
}

/// <summary>A margin-only style that intentionally records no visible decoration.</summary>
public class StyleBoxEmpty : StyleBox
{
    /// <summary>Creates an empty style with inherited negative content overrides and zero effective margins.</summary>
    public StyleBoxEmpty() { }
    /// <inheritdoc />
    protected override void OnDraw(CanvasItem canvasItem, Rect2 rect) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(StyleBoxEmpty) ? new StyleBoxEmpty() : base.CreateDuplicateInstance();
}
