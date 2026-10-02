namespace Electron2D;

/// <summary>Decorates a rectangle with a borrowed nine-patch texture and independent content and drawing margins.</summary>
/// <remarks>Signed fractional margins are preserved. Atlas mapping precedes expansion. Texture, region and
/// modulation suppress equal changes; other settings emit Changed for every valid assignment. Texture pixel
/// changes are not forwarded as style changes. The style never owns or disposes its texture.</remarks>
public class StyleBoxTexture : StyleBox
{
    private Texture? _texture;
    private Rect2 _regionRect;
    private Color _modulateColor = Colors.White;
    private bool _drawCenter = true;
    private AxisStretchMode _horizontal, _vertical;
    private readonly float[] _textureMargins = new float[4], _expandMargins = new float[4];
    private static readonly PropertyDescriptor[] TextureProperties =
    [
        new PropertyDescriptor<StyleBoxTexture, Texture?>(nameof(Texture), style => style.Texture, (style, value) => style.Texture = value, _ => null, stored: true),
        new PropertyDescriptor<StyleBoxTexture, Rect2>(nameof(RegionRect), style => style.RegionRect, (style, value) => style.RegionRect = value, _ => default, stored: true),
        new PropertyDescriptor<StyleBoxTexture, Color>(nameof(ModulateColor), style => style.ModulateColor, (style, value) => style.ModulateColor = value, _ => Colors.White, stored: true),
        new PropertyDescriptor<StyleBoxTexture, bool>(nameof(DrawCenter), style => style.DrawCenter, (style, value) => style.DrawCenter = value, _ => true, stored: true),
        new PropertyDescriptor<StyleBoxTexture, AxisStretchMode>(nameof(AxisStretchHorizontal), style => style.AxisStretchHorizontal, (style, value) => style.AxisStretchHorizontal = value, _ => AxisStretchMode.Stretch, stored: true),
        new PropertyDescriptor<StyleBoxTexture, AxisStretchMode>(nameof(AxisStretchVertical), style => style.AxisStretchVertical, (style, value) => style.AxisStretchVertical = value, _ => AxisStretchMode.Stretch, stored: true),
        new PropertyDescriptor<StyleBoxTexture, float>(nameof(TextureMarginLeft), style => style.TextureMarginLeft, (style, value) => style.TextureMarginLeft = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxTexture, float>(nameof(TextureMarginTop), style => style.TextureMarginTop, (style, value) => style.TextureMarginTop = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxTexture, float>(nameof(TextureMarginRight), style => style.TextureMarginRight, (style, value) => style.TextureMarginRight = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxTexture, float>(nameof(TextureMarginBottom), style => style.TextureMarginBottom, (style, value) => style.TextureMarginBottom = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxTexture, float>(nameof(ExpandMarginLeft), style => style.ExpandMarginLeft, (style, value) => style.ExpandMarginLeft = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxTexture, float>(nameof(ExpandMarginTop), style => style.ExpandMarginTop, (style, value) => style.ExpandMarginTop = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxTexture, float>(nameof(ExpandMarginRight), style => style.ExpandMarginRight, (style, value) => style.ExpandMarginRight = value, _ => 0, stored: true),
        new PropertyDescriptor<StyleBoxTexture, float>(nameof(ExpandMarginBottom), style => style.ExpandMarginBottom, (style, value) => style.ExpandMarginBottom = value, _ => 0, stored: true)
    ];
    /// <summary>Creates an untextured style with zero texture/expansion margins, white tint and stretch axes.</summary>
    public StyleBoxTexture() { }
    /// <summary>Gets or sets the borrowed drawing texture.</summary>
    /// <value>Null initially; null draws no decoration. An equal live reference is silent.</value>
    /// <remarks>An externally disposed texture remains readable here; drawing rejects it until it is replaced.
    /// Source Changed is not forwarded; texture pixel changes use the renderer's existing texture cache.</remarks>
    /// <exception cref="ObjectDisposedException">The style or assigned texture is disposed.</exception>
    public Texture? Texture
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _texture; } }
        set
        {
            lock (StyleGate) { ThrowIfDisposed(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_texture, value)) return; _texture = value; }
            EmitChanged();
        }
    }
    /// <summary>Gets or sets the source rectangle in texture pixels.</summary>
    /// <value>An empty rectangle initially selects the texture's current source extent. Equal values are silent.</value>
    /// <exception cref="ArgumentException">The rectangle is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Rect2 RegionRect
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _regionRect; } }
        set { lock (StyleGate) { ThrowIfDisposed(); ValidateRect(value); if (_regionRect == value) return; _regionRect = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the color multiplied with the texture pixels.</summary>
    /// <value>Opaque white initially; equal values are silent.</value>
    /// <exception cref="ArgumentException">A color component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public Color ModulateColor
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _modulateColor; } }
        set { lock (StyleGate) { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Style colors must be finite.", nameof(value)); if (_modulateColor == value) return; _modulateColor = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets whether to draw the patch's center as well as its borders.</summary>
    /// <value>True initially. Every valid assignment emits Changed.</value>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public bool DrawCenter
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _drawCenter; } }
        set { lock (StyleGate) { ThrowIfDisposed(); _drawCenter = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the horizontal inner-region fill mode.</summary>
    /// <value>Stretch initially. Every valid assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public AxisStretchMode AxisStretchHorizontal
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _horizontal; } }
        set { lock (StyleGate) { ThrowIfDisposed(); if (value is < AxisStretchMode.Stretch or > AxisStretchMode.TileFit) throw new ArgumentOutOfRangeException(nameof(value)); _horizontal = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the vertical inner-region fill mode.</summary>
    /// <value>Stretch initially. Every valid assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public AxisStretchMode AxisStretchVertical
    {
        get { lock (StyleGate) { ThrowIfDisposed(); return _vertical; } }
        set { lock (StyleGate) { ThrowIfDisposed(); if (value is < AxisStretchMode.Stretch or > AxisStretchMode.TileFit) throw new ArgumentOutOfRangeException(nameof(value)); _vertical = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the signed left source border width in pixels.</summary>
    /// <value>Zero initially; fractions and negative values are retained. Every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float TextureMarginLeft { get => GetTextureMargin(Side.Left); set => SetTextureMargin(Side.Left, value); }
    /// <summary>Gets or sets the signed top source border width in pixels.</summary>
    /// <value>Zero initially; fractions and negative values are retained. Every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float TextureMarginTop { get => GetTextureMargin(Side.Top); set => SetTextureMargin(Side.Top, value); }
    /// <summary>Gets or sets the signed right source border width in pixels.</summary>
    /// <value>Zero initially; fractions and negative values are retained. Every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float TextureMarginRight { get => GetTextureMargin(Side.Right); set => SetTextureMargin(Side.Right, value); }
    /// <summary>Gets or sets the signed bottom source border width in pixels.</summary>
    /// <value>Zero initially; fractions and negative values are retained. Every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float TextureMarginBottom { get => GetTextureMargin(Side.Bottom); set => SetTextureMargin(Side.Bottom, value); }
    /// <summary>Gets one signed source border width.</summary>
    /// <param name="margin">The requested side.</param>
    /// <returns>The stored pixel value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float GetTextureMargin(Side margin) { lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); return _textureMargins[(int)margin]; } }
    /// <summary>Sets one source border width and emits Changed even for an equal assignment.</summary>
    /// <param name="margin">The requested side.</param>
    /// <param name="size">The finite signed pixel value.</param>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined or size is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public void SetTextureMargin(Side margin, float size)
    {
        lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); ValidateFloat(size); _textureMargins[(int)margin] = size; }
        EmitChanged();
    }
    /// <summary>Sets all four source border widths atomically and emits Changed once.</summary>
    /// <param name="size">The finite signed pixel value for every side.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public void SetTextureMarginAll(float size)
    {
        lock (StyleGate) { ThrowIfDisposed(); ValidateFloat(size); Array.Fill(_textureMargins, size); }
        EmitChanged();
    }
    /// <summary>Gets or sets the signed left outward drawing expansion in pixels.</summary>
    /// <value>Zero initially; fractions and negative values are retained. Every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float ExpandMarginLeft { get => GetExpandMargin(Side.Left); set => SetExpandMargin(Side.Left, value); }
    /// <summary>Gets or sets the signed top outward drawing expansion in pixels.</summary>
    /// <value>Zero initially; fractions and negative values are retained. Every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float ExpandMarginTop { get => GetExpandMargin(Side.Top); set => SetExpandMargin(Side.Top, value); }
    /// <summary>Gets or sets the signed right outward drawing expansion in pixels.</summary>
    /// <value>Zero initially; fractions and negative values are retained. Every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float ExpandMarginRight { get => GetExpandMargin(Side.Right); set => SetExpandMargin(Side.Right, value); }
    /// <summary>Gets or sets the signed bottom outward drawing expansion in pixels.</summary>
    /// <value>Zero initially; fractions and negative values are retained. Every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float ExpandMarginBottom { get => GetExpandMargin(Side.Bottom); set => SetExpandMargin(Side.Bottom, value); }
    /// <summary>Gets one signed outward drawing expansion.</summary>
    /// <param name="margin">The requested side.</param>
    /// <returns>The stored pixel value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public float GetExpandMargin(Side margin) { lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); return _expandMargins[(int)margin]; } }
    /// <summary>Sets one outward drawing expansion and emits Changed even for an equal assignment.</summary>
    /// <param name="margin">The requested side.</param>
    /// <param name="size">The finite signed pixel value.</param>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined or size is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public void SetExpandMargin(Side margin, float size)
    {
        lock (StyleGate) { ThrowIfDisposed(); ValidateSide(margin); ValidateFloat(size); _expandMargins[(int)margin] = size; }
        EmitChanged();
    }
    /// <summary>Sets all four outward drawing expansions atomically and emits Changed once.</summary>
    /// <param name="size">The finite signed pixel value for every side.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The style is disposed.</exception>
    public void SetExpandMarginAll(float size)
    {
        lock (StyleGate) { ThrowIfDisposed(); ValidateFloat(size); Array.Fill(_expandMargins, size); }
        EmitChanged();
    }
    internal override float StyleMargin(Side side) => _textureMargins[(int)side];
    /// <inheritdoc />
    protected override Rect2 OnGetDrawRect(Rect2 rect)
    {
        lock (StyleGate) { ThrowIfDisposed(); return rect.GrowIndividual(_expandMargins[0], _expandMargins[1], _expandMargins[2], _expandMargins[3]); }
    }
    /// <inheritdoc />
    protected override void OnDraw(CanvasItem canvasItem, Rect2 rect)
    {
        Texture? texture; Rect2 source; Color color; CanvasNinePatch patch; Vector4 expand;
        lock (StyleGate)
        {
            ThrowIfDisposed(); texture = _texture; source = _regionRect; color = _modulateColor;
            patch = new(new(_textureMargins[0], _textureMargins[1]), new(_textureMargins[2], _textureMargins[3]),
                _horizontal, _vertical, _drawCenter);
            expand = new(_expandMargins[0], _expandMargins[1], _expandMargins[2], _expandMargins[3]);
        }
        if (texture is null) return;
        while (texture is AtlasTexture atlas)
        {
            texture = atlas.ResolveDrawRegion(ref rect, ref source);
            if (texture is null) return;
        }
        rect = rect.GrowIndividual(expand.X, expand.Y, expand.Z, expand.W);
        canvasItem.RecordNinePatch(texture, rect, source, patch, color);
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(TextureProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(StyleBoxTexture) ? new StyleBoxTexture() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        Texture? texture; Rect2 region; Color color; bool center; AxisStretchMode horizontal, vertical; Vector4 margins, expand;
        lock (StyleGate)
        {
            ThrowIfDisposed(); texture = _texture; region = _regionRect; color = _modulateColor; center = _drawCenter; horizontal = _horizontal; vertical = _vertical;
            margins = new(_textureMargins[0], _textureMargins[1], _textureMargins[2], _textureMargins[3]);
            expand = new(_expandMargins[0], _expandMargins[1], _expandMargins[2], _expandMargins[3]);
        }
        var copy = (StyleBoxTexture)target;
        copy.Texture = deep ? (Texture?)duplicateSubresource(texture) : texture;
        copy.RegionRect = region; copy.ModulateColor = color; copy.DrawCenter = center;
        copy.AxisStretchHorizontal = horizontal; copy.AxisStretchVertical = vertical;
        copy.TextureMarginLeft = margins.X; copy.TextureMarginTop = margins.Y; copy.TextureMarginRight = margins.Z; copy.TextureMarginBottom = margins.W;
        copy.ExpandMarginLeft = expand.X; copy.ExpandMarginTop = expand.Y; copy.ExpandMarginRight = expand.Z; copy.ExpandMarginBottom = expand.W;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        lock (StyleGate) _texture = null;
        base.Dispose(disposing);
    }
}
