using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Draws a flat-color decoration with independent borders, rounded corners, skew and a fading shadow.</summary>
/// <remarks>All valid assignments emit Changed, including equal values. Content margins fall back to the
/// stored border widths. Geometry adapts borders and radii to the destination without changing resource state.
/// Consumers request redraw after mutations. Reusable triangle storage is serialized by the style lock;
/// drawing invokes no user callbacks while that lock is held. Feather size uses the current unit-oversampling
/// canvas policy; text/viewport oversampling overrides remain a separate integration.</remarks>
public partial class StyleBoxFlat : StyleBox
{
    private Color _background = new Color(.6f, .6f, .6f, 1);
    private Color _borderColor = new Color(.8f, .8f, .8f, 1);
    private Color _shadowColor = new Color(0, 0, 0, .6f);
    private bool _drawCenter = true;
    private bool _borderBlend = false;
    private bool _antiAliasing = true;
    private float _antiAliasingSize = 1;
    private int _cornerDetail = 8;
    private int _shadowSize = 0;
    private Vector2 _shadowOffset = default;
    private Vector2 _skew = default;
    private readonly int[] _borders = new int[4], _radii = new int[4];
    private readonly float[] _expansion = new float[4];
    private readonly List<CanvasVertex> _triangles = [];
    private static readonly PropertyDescriptor[] FlatProperties =
    [
        new PropertyDescriptor<StyleBoxFlat, Color>(nameof(BGColor), style => style.BGColor, (style, value) => style.BGColor = value, _ => new Color(.6f, .6f, .6f, 1), stored: true),
        new PropertyDescriptor<StyleBoxFlat, Color>(nameof(BorderColor), style => style.BorderColor, (style, value) => style.BorderColor = value, _ => new Color(.8f, .8f, .8f, 1), stored: true),
        new PropertyDescriptor<StyleBoxFlat, Color>(nameof(ShadowColor), style => style.ShadowColor, (style, value) => style.ShadowColor = value, _ => new Color(0, 0, 0, .6f), stored: true),
        new PropertyDescriptor<StyleBoxFlat, bool>(nameof(DrawCenter), style => style.DrawCenter, (style, value) => style.DrawCenter = value, _ => true, stored: true),
        new PropertyDescriptor<StyleBoxFlat, bool>(nameof(BorderBlend), style => style.BorderBlend, (style, value) => style.BorderBlend = value, _ => false, stored: true),
        new PropertyDescriptor<StyleBoxFlat, bool>(nameof(AntiAliasing), style => style.AntiAliasing, (style, value) => style.AntiAliasing = value, _ => true, stored: true),
        new PropertyDescriptor<StyleBoxFlat, float>(nameof(AntiAliasingSize), style => style.AntiAliasingSize, (style, value) => style.AntiAliasingSize = value, _ => 1, stored: true),
        new PropertyDescriptor<StyleBoxFlat, int>(nameof(CornerDetail), style => style.CornerDetail, (style, value) => style.CornerDetail = value, _ => 8, stored: true),
        new PropertyDescriptor<StyleBoxFlat, int>(nameof(ShadowSize), style => style.ShadowSize, (style, value) => style.ShadowSize = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, Vector2>(nameof(ShadowOffset), style => style.ShadowOffset, (style, value) => style.ShadowOffset = value, _ => default, stored: true),
        new PropertyDescriptor<StyleBoxFlat, Vector2>(nameof(Skew), style => style.Skew, (style, value) => style.Skew = value, _ => default, stored: true),
        new PropertyDescriptor<StyleBoxFlat, int>(nameof(BorderWidthLeft), style => style.BorderWidthLeft, (style, value) => style.BorderWidthLeft = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, int>(nameof(BorderWidthTop), style => style.BorderWidthTop, (style, value) => style.BorderWidthTop = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, int>(nameof(BorderWidthRight), style => style.BorderWidthRight, (style, value) => style.BorderWidthRight = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, int>(nameof(BorderWidthBottom), style => style.BorderWidthBottom, (style, value) => style.BorderWidthBottom = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, int>(nameof(CornerRadiusTopLeft), style => style.CornerRadiusTopLeft, (style, value) => style.CornerRadiusTopLeft = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, int>(nameof(CornerRadiusTopRight), style => style.CornerRadiusTopRight, (style, value) => style.CornerRadiusTopRight = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, int>(nameof(CornerRadiusBottomRight), style => style.CornerRadiusBottomRight, (style, value) => style.CornerRadiusBottomRight = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, int>(nameof(CornerRadiusBottomLeft), style => style.CornerRadiusBottomLeft, (style, value) => style.CornerRadiusBottomLeft = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, float>(nameof(ExpandMarginLeft), style => style.ExpandMarginLeft, (style, value) => style.ExpandMarginLeft = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, float>(nameof(ExpandMarginTop), style => style.ExpandMarginTop, (style, value) => style.ExpandMarginTop = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, float>(nameof(ExpandMarginRight), style => style.ExpandMarginRight, (style, value) => style.ExpandMarginRight = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxFlat, float>(nameof(ExpandMarginBottom), style => style.ExpandMarginBottom, (style, value) => style.ExpandMarginBottom = value, _ => 0, stored: true)
    ];
    /// <summary>Creates a gray filled style with zero borders/radii/shadow and enabled one-unit antialiasing.</summary>
    public StyleBoxFlat() { }
    /// <summary>Gets or sets the background fill color.</summary>
    /// <value>Opaque 0.6 gray initially. Every valid assignment emits Changed.</value>
    /// <exception cref="ArgumentException">A color component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Color BGColor
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _background; } }
        set { lock (StyleGate) { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Style colors must be finite.", nameof(value)); _background = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the border color.</summary>
    /// <value>Opaque 0.8 gray initially. Every valid assignment emits Changed.</value>
    /// <exception cref="ArgumentException">A color component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Color BorderColor
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _borderColor; } }
        set { lock (StyleGate) { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Style colors must be finite.", nameof(value)); _borderColor = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the shadow color.</summary>
    /// <value>Black with alpha 0.6 initially. Every valid assignment emits Changed.</value>
    /// <exception cref="ArgumentException">A color component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Color ShadowColor
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _shadowColor; } }
        set { lock (StyleGate) { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Style colors must be finite.", nameof(value)); _shadowColor = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets whether to fill the center and the shadow center.</summary>
    /// <value>True initially. Every valid assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public bool DrawCenter
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _drawCenter; } }
        set { lock (StyleGate) { ThrowIfDisposed(); _drawCenter = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets whether the border blends toward the center color, or transparent when the center is disabled.</summary>
    /// <value>False initially. Every valid assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public bool BorderBlend
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _borderBlend; } }
        set { lock (StyleGate) { ThrowIfDisposed(); _borderBlend = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets whether curved or skewed edges use an alpha feather.</summary>
    /// <value>True initially; sharp unskewed rectangles do not need a feather. Every valid assignment emits Changed.</value>
    /// <remarks>Changed is followed by PropertyListChanged when the style remains live, including after a Changed
    /// failure. A single observer error is rethrown; errors from both phases are aggregated after delivery.</remarks>
    /// <exception cref="AggregateException">Observers fail in both notification phases.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public bool AntiAliasing
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _antiAliasing; } }
        set
        {
            lock (StyleGate) { ThrowIfDisposed(); _antiAliasing = value; }
            Exception? changedError = null, listError = null;
            try { EmitChanged(); } catch (Exception error) { changedError = error; }
            try { if (!IsDisposed) NotifyPropertyListChanged(); } catch (Exception error) { listError = error; }
            ThrowCombined(changedError, listError);
        }
    }
    /// <summary>Gets or sets the local alpha-feather width.</summary>
    /// <value>One initially. Finite values clamp to [0.01, 10]. Every valid assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float AntiAliasingSize
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _antiAliasingSize; } }
        set { lock (StyleGate) { ThrowIfDisposed(); ValidateFloat(value); _antiAliasingSize = Math.Clamp(value, .01f, 10f); } EmitChanged(); }
    }
    /// <summary>Gets or sets the number of segments per rounded quadrant.</summary>
    /// <value>Eight initially. Values clamp to [1, 20]. Every valid assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int CornerDetail
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _cornerDetail; } }
        set { lock (StyleGate) { ThrowIfDisposed(); _cornerDetail = Math.Clamp(value, 1, 20); } EmitChanged(); }
    }
    /// <summary>Gets or sets the signed shadow extent.</summary>
    /// <value>Zero initially. Only positive sizes draw a shadow. Every valid assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int ShadowSize
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _shadowSize; } }
        set { lock (StyleGate) { ThrowIfDisposed(); _shadowSize = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the local offset of the shadow.</summary>
    /// <value>Zero initially. Every valid assignment emits Changed.</value>
    /// <exception cref="ArgumentException">A coordinate is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Vector2 ShadowOffset
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _shadowOffset; } }
        set { lock (StyleGate) { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Style coordinates must be finite.", nameof(value)); _shadowOffset = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the horizontal and vertical shear coefficients around the style center.</summary>
    /// <value>Zero initially; each axis shears against the other coordinate. Every valid assignment emits Changed.</value>
    /// <exception cref="ArgumentException">A coordinate is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Vector2 Skew
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _skew; } }
        set { lock (StyleGate) { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Style coordinates must be finite.", nameof(value)); _skew = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the signed Left border width in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int BorderWidthLeft { get => GetBorderWidth(Side.Left); set => SetBorderWidth(Side.Left, value); }
    /// <summary>Gets or sets the signed Top border width in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int BorderWidthTop { get => GetBorderWidth(Side.Top); set => SetBorderWidth(Side.Top, value); }
    /// <summary>Gets or sets the signed Right border width in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int BorderWidthRight { get => GetBorderWidth(Side.Right); set => SetBorderWidth(Side.Right, value); }
    /// <summary>Gets or sets the signed Bottom border width in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int BorderWidthBottom { get => GetBorderWidth(Side.Bottom); set => SetBorderWidth(Side.Bottom, value); }
    /// <summary>Gets the stored signed border width.</summary>
    /// <param name="margin">The requested margin.</param>
    /// <returns>The stored local-unit value, without geometry adaptation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The margin is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int GetBorderWidth(Side margin) { lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); return _borders[(int)margin]; } }
    /// <summary>Sets one signed border width and emits Changed.</summary>
    /// <param name="margin">The requested margin.</param>
    /// <param name="width">The signed local-unit value.</param>
    /// <exception cref="ArgumentOutOfRangeException">The margin is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public void SetBorderWidth(Side margin, int width)
    {
        lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); _borders[(int)margin] = width; }
        EmitChanged();
    }
    /// <summary>Sets all four border widths atomically and emits Changed once.</summary>
    /// <param name="width">The signed local-unit value for all four entries.</param>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public void SetBorderWidthAll(int width)
    {
        lock (StyleGate) { ThrowIfDisposed(); Array.Fill(_borders, width); }
        EmitChanged();
    }
    /// <summary>Gets or sets the signed TopLeft corner radius in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int CornerRadiusTopLeft { get => GetCornerRadius(Corner.TopLeft); set => SetCornerRadius(Corner.TopLeft, value); }
    /// <summary>Gets or sets the signed TopRight corner radius in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int CornerRadiusTopRight { get => GetCornerRadius(Corner.TopRight); set => SetCornerRadius(Corner.TopRight, value); }
    /// <summary>Gets or sets the signed BottomRight corner radius in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int CornerRadiusBottomRight { get => GetCornerRadius(Corner.BottomRight); set => SetCornerRadius(Corner.BottomRight, value); }
    /// <summary>Gets or sets the signed BottomLeft corner radius in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int CornerRadiusBottomLeft { get => GetCornerRadius(Corner.BottomLeft); set => SetCornerRadius(Corner.BottomLeft, value); }
    /// <summary>Gets the stored signed corner radius.</summary>
    /// <param name="corner">The requested corner.</param>
    /// <returns>The stored local-unit value, without geometry adaptation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The corner is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int GetCornerRadius(Corner corner) { lock (StyleGate) { ThrowIfDisposed(); ValidateCorner(corner); return _radii[(int)corner]; } }
    /// <summary>Sets one signed corner radius and emits Changed.</summary>
    /// <param name="corner">The requested corner.</param>
    /// <param name="radius">The signed local-unit value.</param>
    /// <exception cref="ArgumentOutOfRangeException">The corner is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public void SetCornerRadius(Corner corner, int radius)
    {
        lock (StyleGate) { ThrowIfDisposed(); ValidateCorner(corner); _radii[(int)corner] = radius; }
        EmitChanged();
    }
    /// <summary>Sets all four corner radii atomically and emits Changed once.</summary>
    /// <param name="radius">The signed local-unit value for all four entries.</param>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public void SetCornerRadiusAll(int radius)
    {
        lock (StyleGate) { ThrowIfDisposed(); Array.Fill(_radii, radius); }
        EmitChanged();
    }
    /// <summary>Gets or sets the signed Left outward expansion in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float ExpandMarginLeft { get => GetExpandMargin(Side.Left); set => SetExpandMargin(Side.Left, value); }
    /// <summary>Gets or sets the signed Top outward expansion in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float ExpandMarginTop { get => GetExpandMargin(Side.Top); set => SetExpandMargin(Side.Top, value); }
    /// <summary>Gets or sets the signed Right outward expansion in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float ExpandMarginRight { get => GetExpandMargin(Side.Right); set => SetExpandMargin(Side.Right, value); }
    /// <summary>Gets or sets the signed Bottom outward expansion in local units.</summary>
    /// <value>Zero initially; every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float ExpandMarginBottom { get => GetExpandMargin(Side.Bottom); set => SetExpandMargin(Side.Bottom, value); }
    /// <summary>Gets the stored signed outward expansion.</summary>
    /// <param name="margin">The requested margin.</param>
    /// <returns>The stored local-unit value, without geometry adaptation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The margin is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float GetExpandMargin(Side margin) { lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); return _expansion[(int)margin]; } }
    /// <summary>Sets one signed outward expansion and emits Changed.</summary>
    /// <param name="margin">The requested margin.</param>
    /// <param name="size">The signed local-unit value.</param>
    /// <exception cref="ArgumentOutOfRangeException">The margin is undefined; or the value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public void SetExpandMargin(Side margin, float size)
    {
        lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); ValidateFloat(size); _expansion[(int)margin] = size; }
        EmitChanged();
    }
    /// <summary>Sets all four outward expansions atomically and emits Changed once.</summary>
    /// <param name="size">The signed local-unit value for all four entries.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public void SetExpandMarginAll(float size)
    {
        lock (StyleGate) { ThrowIfDisposed(); ValidateFloat(size); Array.Fill(_expansion, size); }
        EmitChanged();
    }
    /// <summary>Gets the smallest of the four stored border widths, including negative values.</summary>
    /// <returns>The minimum stored width before destination adaptation.</returns>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public int GetBorderWidthMin() { lock (StyleGate) { ThrowIfDisposed(); return Math.Min(Math.Min(_borders[0], _borders[1]), Math.Min(_borders[2], _borders[3])); } }
    internal override float StyleMargin(Side side) => _borders[(int)side];
    private static void ValidateCorner(Corner corner) { if (corner is < Corner.TopLeft or > Corner.BottomLeft) throw new ArgumentOutOfRangeException(nameof(corner)); }
    /// <inheritdoc />
    protected override Rect2 OnGetDrawRect(Rect2 rect)
    {
        lock (StyleGate)
        {
            ThrowIfDisposed(); rect = rect.GrowIndividual(_expansion[0], _expansion[1], _expansion[2], _expansion[3]);
            return _shadowSize > 0 ? rect.Merge(new(rect.Grow(_shadowSize).Position + _shadowOffset, rect.Grow(_shadowSize).Size)) : rect;
        }
    }
    /// <inheritdoc />
    protected override void OnDraw(CanvasItem canvasItem, Rect2 rect)
    {
        lock (StyleGate)
        {
            ThrowIfDisposed();
            try
            {
                BuildGeometry(rect);
                if (_triangles.Count != 0) canvasItem.DrawTriangleArray(CollectionsMarshal.AsSpan(_triangles), null);
            }
            finally { _triangles.Clear(); }
        }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(FlatProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(StyleBoxFlat) ? new StyleBoxFlat() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        Color background, borderColor, shadowColor; bool center, blend, aa; float aaSize; int detail, shadowSize; Vector2 shadowOffset, skew;
        Span<int> borders = stackalloc int[4]; Span<int> radii = stackalloc int[4]; Span<float> expansion = stackalloc float[4];
        lock (StyleGate)
        {
            ThrowIfDisposed(); background = _background; borderColor = _borderColor; shadowColor = _shadowColor;
            center = _drawCenter; blend = _borderBlend; aa = _antiAliasing; aaSize = _antiAliasingSize;
            detail = _cornerDetail; shadowSize = _shadowSize; shadowOffset = _shadowOffset; skew = _skew;
            _borders.CopyTo(borders); _radii.CopyTo(radii); _expansion.CopyTo(expansion);
        }
        var copy = (StyleBoxFlat)target;
        copy.BGColor = background; copy.BorderColor = borderColor; copy.ShadowColor = shadowColor;
        copy.DrawCenter = center; copy.BorderBlend = blend; copy.AntiAliasing = aa; copy.AntiAliasingSize = aaSize;
        copy.CornerDetail = detail; copy.ShadowSize = shadowSize; copy.ShadowOffset = shadowOffset; copy.Skew = skew;
        for (var index = 0; index < 4; index++)
        {
            copy.SetBorderWidth((Side)index, borders[index]);
            copy.SetCornerRadius((Corner)index, radii[index]);
            copy.SetExpandMargin((Side)index, expansion[index]);
        }
    }
}
