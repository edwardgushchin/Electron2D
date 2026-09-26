namespace Electron2D;

/// <summary>Specifies the spatial fill policy of a texture progress control.</summary>
public enum TextureProgressFillMode
{
    /// <summary>Fills from the left edge.</summary>
    LeftToRight = 0,
    /// <summary>Fills from the right edge.</summary>
    RightToLeft = 1,
    /// <summary>Fills from the top edge.</summary>
    TopToBottom = 2,
    /// <summary>Fills from the bottom edge.</summary>
    BottomToTop = 3,
    /// <summary>Fills clockwise from the configured angle.</summary>
    Clockwise = 4,
    /// <summary>Fills counterclockwise from the configured angle.</summary>
    CounterClockwise = 5,
    /// <summary>Fills from the horizontal center toward both edges.</summary>
    BilinearLeftAndRight = 6,
    /// <summary>Fills from the vertical center toward both edges.</summary>
    BilinearTopAndBottom = 7,
    /// <summary>Fills in both angular directions from the configured angle.</summary>
    ClockwiseAndCounterClockwise = 8
}

/// <summary>Displays the shared range ratio through borrowed under, progress and over textures.</summary>
/// <remarks>Native texture sizes are used unless nine-patch stretching is enabled. Linear, centered and radial
/// fills share the retained canvas texture/polygon pipeline. Texture ownership remains with the caller.</remarks>
public partial class TextureProgressBar : Range
{
    private readonly Texture?[] _textures = new Texture?[3];
    private readonly Color[] _tints = [Colors.White, Colors.White, Colors.White];
    private readonly int[] _margins = new int[4];
    private bool _ninePatchStretch;
    private TextureProgressFillMode _fillMode;
    private Vector2 _textureProgressOffset, _radialCenterOffset;
    private float _radialInitialAngle, _radialFillDegrees = 360;
    private static readonly PropertyDescriptor[] ProgressProperties =
    [
        new PropertyDescriptor<TextureProgressBar, Texture?>(nameof(TextureUnder), node => node.TextureUnder, (node, value) => node.TextureUnder = value, _ => null, stored: true),
        new PropertyDescriptor<TextureProgressBar, Texture?>(nameof(TextureProgress), node => node.TextureProgress, (node, value) => node.TextureProgress = value, _ => null, stored: true),
        new PropertyDescriptor<TextureProgressBar, Texture?>(nameof(TextureOver), node => node.TextureOver, (node, value) => node.TextureOver = value, _ => null, stored: true),
        new PropertyDescriptor<TextureProgressBar, Color>(nameof(TintUnder), node => node.TintUnder, (node, value) => node.TintUnder = value, _ => Colors.White, stored: true),
        new PropertyDescriptor<TextureProgressBar, Color>(nameof(TintProgress), node => node.TintProgress, (node, value) => node.TintProgress = value, _ => Colors.White, stored: true),
        new PropertyDescriptor<TextureProgressBar, Color>(nameof(TintOver), node => node.TintOver, (node, value) => node.TintOver = value, _ => Colors.White, stored: true),
        new PropertyDescriptor<TextureProgressBar, TextureProgressFillMode>(nameof(FillMode), node => node.FillMode, (node, value) => node.FillMode = value, _ => TextureProgressFillMode.LeftToRight, stored: true),
        new PropertyDescriptor<TextureProgressBar, bool>(nameof(NinePatchStretch), node => node.NinePatchStretch, (node, value) => node.NinePatchStretch = value, _ => false, stored: true),
        new PropertyDescriptor<TextureProgressBar, Vector2>(nameof(TextureProgressOffset), node => node.TextureProgressOffset, (node, value) => node.TextureProgressOffset = value, _ => default, stored: true),
        new PropertyDescriptor<TextureProgressBar, Vector2>(nameof(RadialCenterOffset), node => node.RadialCenterOffset, (node, value) => node.RadialCenterOffset = value, _ => default, stored: true),
        new PropertyDescriptor<TextureProgressBar, float>(nameof(RadialInitialAngle), node => node.RadialInitialAngle, (node, value) => node.RadialInitialAngle = value, _ => 0, stored: true),
        new PropertyDescriptor<TextureProgressBar, float>(nameof(RadialFillDegrees), node => node.RadialFillDegrees, (node, value) => node.RadialFillDegrees = value, _ => 360, stored: true),
        new PropertyDescriptor<TextureProgressBar, int>(nameof(StretchMarginLeft), node => node.StretchMarginLeft, (node, value) => node.StretchMarginLeft = value, _ => 0, stored: true),
        new PropertyDescriptor<TextureProgressBar, int>(nameof(StretchMarginTop), node => node.StretchMarginTop, (node, value) => node.StretchMarginTop = value, _ => 0, stored: true),
        new PropertyDescriptor<TextureProgressBar, int>(nameof(StretchMarginRight), node => node.StretchMarginRight, (node, value) => node.StretchMarginRight = value, _ => 0, stored: true),
        new PropertyDescriptor<TextureProgressBar, int>(nameof(StretchMarginBottom), node => node.StretchMarginBottom, (node, value) => node.StretchMarginBottom = value, _ => 0, stored: true),
        new PropertyDescriptor<TextureProgressBar, double>(nameof(Step), node => node.Step, (node, value) => node.Step = value, _ => 1, stored: true),
        new PropertyDescriptor<TextureProgressBar, MouseFilter>(nameof(MouseFilter), node => node.MouseFilter, (node, value) => node.MouseFilter = value, _ => MouseFilter.Pass, stored: true),
        new PropertyDescriptor<TextureProgressBar, SizeFlags>(nameof(SizeFlagsVertical), node => node.SizeFlagsVertical, (node, value) => node.SizeFlagsVertical = value, _ => SizeFlags.Fill, stored: true),
    ];
    /// <summary>Creates an empty progress control with integer step, vertical Fill and pass-through pointer input.</summary>
    public TextureProgressBar() { Step = 1; MouseFilter = MouseFilter.Pass; SizeFlagsVertical = SizeFlags.Fill; }
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    /// <summary>Gets or sets the borrowed TextureUnder drawing resource.</summary>
    /// <value>Null initially.</value>
    /// <exception cref="ObjectDisposedException">The control or supplied texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    public Texture? TextureUnder { get { Check(); return _textures[0]; } set { SetTexture(0, value); } }
    /// <summary>Gets or sets the borrowed TextureProgress drawing resource.</summary>
    /// <value>Null initially.</value>
    /// <exception cref="ObjectDisposedException">The control or supplied texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    public Texture? TextureProgress { get { Check(); return _textures[1]; } set { SetTexture(1, value); } }
    /// <summary>Gets or sets the borrowed TextureOver drawing resource.</summary>
    /// <value>Null initially.</value>
    /// <exception cref="ObjectDisposedException">The control or supplied texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    public Texture? TextureOver { get { Check(); return _textures[2]; } set { SetTexture(2, value); } }
    /// <summary>Gets or sets the finite TintUnder color multiplier.</summary>
    /// <value>Opaque white initially.</value>
    /// <exception cref="ArgumentException">The color is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public Color TintUnder { get { Check(); return _tints[0]; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Tint must be finite.", nameof(value)); if (_tints[0] == value) return; _tints[0] = value; QueueRedraw(); } }
    /// <summary>Gets or sets the finite TintProgress color multiplier.</summary>
    /// <value>Opaque white initially.</value>
    /// <exception cref="ArgumentException">The color is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public Color TintProgress { get { Check(); return _tints[1]; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Tint must be finite.", nameof(value)); if (_tints[1] == value) return; _tints[1] = value; QueueRedraw(); } }
    /// <summary>Gets or sets the finite TintOver color multiplier.</summary>
    /// <value>Opaque white initially.</value>
    /// <exception cref="ArgumentException">The color is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public Color TintOver { get { Check(); return _tints[2]; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Tint must be finite.", nameof(value)); if (_tints[2] == value) return; _tints[2] = value; QueueRedraw(); } }
    private void SetTexture(int slot, Texture? texture)
    {
        EnsureMutable(); if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
        if (ReferenceEquals(_textures[slot], texture)) return;
        if (_textures[slot] is { } previous) { previous.Changed -= TextureUpdated; previous.Disposed -= TextureDisposed; }
        _textures[slot] = texture;
        if (texture is not null) { texture.Changed += TextureUpdated; texture.Disposed += TextureDisposed; }
        UpdateMinimumSize(); QueueRedraw();
    }
    private void TextureUpdated(Resource _) { if (!IsDisposed) { UpdateMinimumSize(); QueueRedraw(); } }
    private void TextureDisposed(ElectronObject texture)
    {
        if (IsDisposed) return;
        for (var slot = 0; slot < _textures.Length; slot++) if (ReferenceEquals(_textures[slot], texture)) SetTexture(slot, null);
    }
    /// <summary>Gets or sets the defined linear, bilateral or radial fill policy.</summary>
    /// <value>LeftToRight initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public TextureProgressFillMode FillMode
    {
        get { Check(); return _fillMode; }
        set { EnsureMutable(); if (value is < TextureProgressFillMode.LeftToRight or > TextureProgressFillMode.ClockwiseAndCounterClockwise) throw new ArgumentOutOfRangeException(nameof(value)); if (_fillMode == value) return; _fillMode = value; QueueRedraw(); }
    }
    /// <summary>Gets or sets whether textures use the control size and configured stretch borders.</summary>
    /// <value>False initially; radial progress uses stretched polygon geometry instead of nine-patch borders.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool NinePatchStretch
    {
        get { Check(); return _ninePatchStretch; }
        set { EnsureMutable(); if (_ninePatchStretch == value) return; _ninePatchStretch = value; QueueRedraw(); UpdateMinimumSize(); }
    }
    /// <summary>Gets or sets the finite progress drawing offset.</summary>
    /// <value>Zero initially.</value>
    /// <exception cref="ArgumentException">The vector is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public Vector2 TextureProgressOffset { get { Check(); return _textureProgressOffset; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Offset must be finite.", nameof(value)); if (_textureProgressOffset == value) return; _textureProgressOffset = value; QueueRedraw(); } }
    /// <summary>Gets or sets the finite radial center offset in original progress-texture pixels.</summary>
    /// <value>Zero initially.</value>
    /// <exception cref="ArgumentException">The vector is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public Vector2 RadialCenterOffset { get { Check(); return _radialCenterOffset; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Offset must be finite.", nameof(value)); if (_radialCenterOffset == value) return; _radialCenterOffset = value; QueueRedraw(); } }
    /// <summary>Gets or sets the finite initial radial angle in degrees, clockwise from twelve o'clock.</summary>
    /// <value>Zero initially; values outside [0,360] wrap while exactly 360 is retained.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public float RadialInitialAngle
    {
        get { Check(); return _radialInitialAngle; }
        set { EnsureMutable(); Finite(value); if (value < 0 || value > 360) value = Mathf.PosMod(value, 360); if (_radialInitialAngle == value) return; _radialInitialAngle = value; QueueRedraw(); }
    }
    /// <summary>Gets or sets the finite total radial sweep in degrees.</summary>
    /// <value>360 initially; clamps to [0,360].</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public float RadialFillDegrees
    {
        get { Check(); return _radialFillDegrees; }
        set { EnsureMutable(); Finite(value); value = Math.Clamp(value, 0, 360); if (_radialFillDegrees == value) return; _radialFillDegrees = value; QueueRedraw(); }
    }
    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    /// <summary>Gets or sets the signed left nine-patch stretch margin.</summary>
    /// <value>Zero initially.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public int StretchMarginLeft { get => GetStretchMargin(Side.Left); set => SetStretchMargin(Side.Left, value); }
    /// <summary>Gets or sets the signed top nine-patch stretch margin.</summary>
    /// <value>Zero initially.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public int StretchMarginTop { get => GetStretchMargin(Side.Top); set => SetStretchMargin(Side.Top, value); }
    /// <summary>Gets or sets the signed right nine-patch stretch margin.</summary>
    /// <value>Zero initially.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public int StretchMarginRight { get => GetStretchMargin(Side.Right); set => SetStretchMargin(Side.Right, value); }
    /// <summary>Gets or sets the signed bottom nine-patch stretch margin.</summary>
    /// <value>Zero initially.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public int StretchMarginBottom { get => GetStretchMargin(Side.Bottom); set => SetStretchMargin(Side.Bottom, value); }
    /// <summary>Gets a signed stretch margin.</summary>
    /// <param name="margin">A defined side.</param>
    /// <returns>The stored pixel width.</returns>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public int GetStretchMargin(Side margin) { Check(); SideValid(margin); return _margins[(int)margin]; }
    /// <summary>Sets a signed margin and invalidates drawing/minimum size.</summary>
    /// <param name="margin">A defined side.</param>
    /// <param name="value">The new pixel width; equal assignments are silent.</param>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public void SetStretchMargin(Side margin, int value) { EnsureMutable(); SideValid(margin); if (_margins[(int)margin] == value) return; _margins[(int)margin] = value; QueueRedraw(); UpdateMinimumSize(); }
    private static void SideValid(Side side) { if (side is < Side.Left or > Side.Bottom) throw new ArgumentOutOfRangeException(nameof(side)); }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        if (_ninePatchStretch) return new((float)_margins[0] + _margins[2], (float)_margins[1] + _margins[3]);
        var size = Vector2.One;
        foreach (var texture in _textures) if (texture is { IsDisposed: false }) size = size.Max(texture.GetSize());
        return size;
    }
    /// <inheritdoc />
    protected override void OnDraw()
    {
        var ratio = Ratio;
        if (!double.IsFinite(ratio)) throw new InvalidOperationException("A nonfinite ratio cannot be rendered.");
        if (_textures[0] is { } under) DrawLayer(under, 0, 1);
        if (_textures[1] is { } progress) DrawLayer(progress, 1, ratio);
        if (_textures[2] is { } over) DrawLayer(over, 2, 1);
    }
    private void DrawLayer(Texture texture, int slot, double ratio)
    {
        var radial = _fillMode is TextureProgressFillMode.Clockwise or TextureProgressFillMode.CounterClockwise or TextureProgressFillMode.ClockwiseAndCounterClockwise;
        if (_ninePatchStretch && (slot != 1 || !radial)) { DrawNinePatch(texture, ratio, slot == 1, _tints[slot]); return; }
        if (slot != 1) { DrawTexture(texture, Vector2.Zero, _tints[slot]); return; }
        if (radial) { DrawRadial(texture, ratio); return; }
        var size = texture.GetSize(); var region = FillRect(size, (float)ratio);
        DrawTextureRectRegion(texture, new(region.Position + _textureProgressOffset, region.Size), region, _tints[slot]);
    }
    private Rect2 FillRect(Vector2 size, float ratio)
    {
        var horizontal = _fillMode is TextureProgressFillMode.LeftToRight or TextureProgressFillMode.RightToLeft or TextureProgressFillMode.BilinearLeftAndRight;
        var reverse = _fillMode is TextureProgressFillMode.RightToLeft or TextureProgressFillMode.BottomToTop;
        var bilateral = _fillMode is TextureProgressFillMode.BilinearLeftAndRight or TextureProgressFillMode.BilinearTopAndBottom;
        var extent = (horizontal ? size.X : size.Y) * ratio;
        var offset = reverse ? (horizontal ? size.X : size.Y) - extent : bilateral ? ((horizontal ? size.X : size.Y) - extent) / 2 : 0;
        return horizontal ? new(new(offset, 0), new(extent, size.Y)) : new(new(0, offset), new(size.X, extent));
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(property => property.Name != nameof(Step) && property.Name != nameof(MouseFilter) && property.Name != nameof(Value) && property.Name != nameof(SizeFlagsVertical))
        .Concat(ProgressProperties).Concat([new PropertyDescriptor<TextureProgressBar, double>(nameof(Value), node => node.Value, (node, value) => node.Value = value, _ => 0, stored: true)]);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(TextureProgressBar) ? CreateProgress : base.CreateSceneInstanceFactory();
    private static Node CreateProgress() => new TextureProgressBar();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        foreach (var texture in _textures) if (texture is not null) { texture.Changed -= TextureUpdated; texture.Disposed -= TextureDisposed; }
        try { base.Dispose(disposing); }
        finally { Array.Clear(_textures); }
    }
}
