namespace Electron2D;

/// <summary>Decorates a rectangle with a horizontal or vertical pixel-aligned solid strip.</summary>
/// <remarks>The supplied position/size are truncated before signed growth is applied and truncated again.
/// Thickness replaces the perpendicular size without centering. Every setting assignment emits Changed.</remarks>
public class StyleBoxLine : StyleBox
{
    private Color _color = Colors.Black;
    private float _growBegin = 1, _growEnd = 1;
    private int _thickness = 1;
    private bool _vertical;
    private static readonly PropertyDescriptor[] LineProperties =
    [
        new PropertyDescriptor<StyleBoxLine, Color>(nameof(Color), style => style.Color, (style, value) => style.Color = value, _ => Colors.Black, stored: true),
        new PropertyDescriptor<StyleBoxLine, float>(nameof(GrowBegin), style => style.GrowBegin, (style, value) => style.GrowBegin = value, _ => 1, stored: true),
        new PropertyDescriptor<StyleBoxLine, float>(nameof(GrowEnd), style => style.GrowEnd, (style, value) => style.GrowEnd = value, _ => 1, stored: true),
        new PropertyDescriptor<StyleBoxLine, int>(nameof(Thickness), style => style.Thickness, (style, value) => style.Thickness = value, _ => 1, stored: true),
        new PropertyDescriptor<StyleBoxLine, bool>(nameof(Vertical), style => style.Vertical, (style, value) => style.Vertical = value, _ => false, stored: true)
    ];
    /// <summary>Creates a horizontal black strip, thickness one and growth one at each end.</summary>
    public StyleBoxLine() { }
    /// <summary>Gets or sets the strip color.</summary>
    /// <value>Opaque black initially. Equal assignments emit Changed.</value>
    /// <exception cref="ArgumentException">The color is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Color Color
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _color; } }
        set { lock (StyleGate) { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Style colors must be finite.", nameof(value)); _color = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the signed extension before the strip's first endpoint.</summary>
    /// <value>One initially. Fractions are stored; drawing truncates the resulting pixel position.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float GrowBegin
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _growBegin; } }
        set { lock (StyleGate) { ThrowIfDisposed(); ValidateFloat(value); _growBegin = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the signed extension after the strip's last endpoint.</summary>
    /// <value>One initially. Fractions are stored; drawing truncates the resulting pixel size.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float GrowEnd
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _growEnd; } }
        set { lock (StyleGate) { ThrowIfDisposed(); ValidateFloat(value); _growEnd = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the signed perpendicular strip width.</summary>
    /// <value>One initially. Each perpendicular style margin is half this value; zero draws no strip.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int Thickness
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _thickness; } }
        set { lock (StyleGate) { ThrowIfDisposed(); _thickness = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets whether the strip runs vertically.</summary>
    /// <value>False initially. Changing orientation moves the half-thickness margins to the other axis.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public bool Vertical
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _vertical; } }
        set { lock (StyleGate) { ThrowIfDisposed(); _vertical = value; } EmitChanged(); }
    }
    internal override float StyleMargin(Side side) => (_vertical ? side is Side.Left or Side.Right : side is Side.Top or Side.Bottom) ? _thickness / 2f : 0;
    /// <inheritdoc />
    protected override void OnDraw(CanvasItem canvasItem, Rect2 rect)
    {
        Color color; float begin, end; int thickness; bool vertical;
        lock (StyleGate) { ThrowIfDisposed(); color = _color; begin = _growBegin; end = _growEnd; thickness = _thickness; vertical = _vertical; }
        var x = Pixel(rect.Position.X); var y = Pixel(rect.Position.Y); var width = Pixel(rect.Size.X); var height = Pixel(rect.Size.Y);
        if (vertical) { y = Pixel(y - begin); height = Pixel(height + (begin + end)); width = thickness; }
        else { x = Pixel(x - begin); width = Pixel(width + (begin + end)); height = thickness; }
        canvasItem.DrawRect(new(x, y, width, height), color);
    }
    private static int Pixel(float value)
    {
        if (!float.IsFinite(value) || (double)value > int.MaxValue || (double)value < int.MinValue) throw new InvalidOperationException("Style strip exceeds integer pixel range.");
        return (int)value;
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(LineProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(StyleBoxLine) ? new StyleBoxLine() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        Color color; float begin, end; int thickness; bool vertical;
        lock (StyleGate) { ThrowIfDisposed(); color = _color; begin = _growBegin; end = _growEnd; thickness = _thickness; vertical = _vertical; }
        var copy = (StyleBoxLine)target; copy.Color = color; copy.GrowBegin = begin; copy.GrowEnd = end; copy.Thickness = thickness; copy.Vertical = vertical;
    }
}
