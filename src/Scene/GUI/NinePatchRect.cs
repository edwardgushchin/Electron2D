namespace Electron2D;

/// <summary>Draws a borrowed texture as fixed borders and independently stretched or tiled center axes.</summary>
/// <remarks>The panel contributes margin sums as its intrinsic minimum size and ignores pointer input by default.
/// Geometry is retained; atlas mapping and live texture dimensions resolve at submission. Textures remain caller-owned.</remarks>
public class NinePatchRect : Control
{
    /// <summary>Controls the source mapping within one center axis.</summary>
    public enum AxisStretchMode
    {
        /// <summary>Stretches one source center across the destination center.</summary>
        Stretch = 0,
        /// <summary>Repeats at natural pixel size, clipping the last partial tile.</summary>
        Tile = 1,
        /// <summary>Rounds the repeat count and scales complete tiles to fit.</summary>
        TileFit = 2
    }

    private Texture? _texture;
    private Rect2 _regionRect;
    private bool _drawCenter = true;
    private AxisStretchMode _axisStretchHorizontal, _axisStretchVertical;
    private readonly int[] _margins = new int[4];
    private static readonly PropertyDescriptor[] PatchProperties =
    [
        new PropertyDescriptor<NinePatchRect, Texture?>(nameof(Texture), node => node.Texture, (node, value) => node.Texture = value, _ => null, stored: true),
        new PropertyDescriptor<NinePatchRect, Rect2>(nameof(RegionRect), node => node.RegionRect, (node, value) => node.RegionRect = value, _ => default, stored: true),
        new PropertyDescriptor<NinePatchRect, bool>(nameof(DrawCenter), node => node.DrawCenter, (node, value) => node.DrawCenter = value, _ => true, stored: true),
        new PropertyDescriptor<NinePatchRect, AxisStretchMode>(nameof(AxisStretchHorizontal), node => node.AxisStretchHorizontal, (node, value) => node.AxisStretchHorizontal = value, _ => AxisStretchMode.Stretch, stored: true),
        new PropertyDescriptor<NinePatchRect, AxisStretchMode>(nameof(AxisStretchVertical), node => node.AxisStretchVertical, (node, value) => node.AxisStretchVertical = value, _ => AxisStretchMode.Stretch, stored: true),
        new PropertyDescriptor<NinePatchRect, int>(nameof(PatchMarginLeft), node => node.PatchMarginLeft, (node, value) => node.PatchMarginLeft = value, _ => 0, stored: true),
        new PropertyDescriptor<NinePatchRect, int>(nameof(PatchMarginTop), node => node.PatchMarginTop, (node, value) => node.PatchMarginTop = value, _ => 0, stored: true),
        new PropertyDescriptor<NinePatchRect, int>(nameof(PatchMarginRight), node => node.PatchMarginRight, (node, value) => node.PatchMarginRight = value, _ => 0, stored: true),
        new PropertyDescriptor<NinePatchRect, int>(nameof(PatchMarginBottom), node => node.PatchMarginBottom, (node, value) => node.PatchMarginBottom = value, _ => 0, stored: true),
        new PropertyDescriptor<NinePatchRect, MouseFilter>(nameof(MouseFilter), node => node.MouseFilter, (node, value) => node.MouseFilter = value, _ => MouseFilter.Ignore, stored: true),
    ];

    /// <summary>Creates an empty centered-fill panel with zero margins and stretch modes.</summary>
    public NinePatchRect() => MouseFilter = MouseFilter.Ignore;

    /// <summary>Gets or sets the borrowed panel texture.</summary>
    /// <value>Null initially.</value>
    /// <remarks>Changed assignments commit subscriptions, redraw and minimum-size invalidation, then emit TextureChanged.
    /// Equal references are silent. Texture content changes redraw without emitting TextureChanged; disposal clears the binding.</remarks>
    /// <exception cref="ObjectDisposedException">The panel or supplied texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="AggregateException">A notification fails after the binding has committed.</exception>
    public Texture? Texture
    {
        get { CheckQuery(); return _texture; }
        set
        {
            EnsureMutable();
            if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
            if (ReferenceEquals(_texture, value)) return;
            if (_texture is { } previous) { previous.Changed -= TextureUpdated; previous.Disposed -= TextureDisposed; }
            _texture = value;
            if (_texture is { } next) { next.Changed += TextureUpdated; next.Disposed += TextureDisposed; }
            List<Exception>? errors = null;
            try { QueueRedraw(); UpdateMinimumSize(); } catch (Exception error) { CollectException(ref errors, error); }
            try { TextureChanged?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Nine-patch texture notifications failed.", errors);
        }
    }

    /// <summary>Occurs after an actual texture assignment or disposal clears the borrowed texture.</summary>
    /// <remarks>Content updates do not emit this event. Binding is already committed if a subscriber throws.</remarks>
    public event Action? TextureChanged;
    /// <summary>Gets or sets the finite logical source rectangle; an all-zero size selects the whole texture.</summary>
    /// <value>default initially.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The panel is disposed.</exception>
    /// <exception cref="ArgumentException">The rectangle or endpoint is nonfinite.</exception>
    public Rect2 RegionRect
    {
        get { CheckQuery(); return _regionRect; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite() || !value.End.IsFinite()) throw new ArgumentException("The source region must be finite.", nameof(value));
            if (_regionRect == value) return;
            _regionRect = value;
            NotifyItemRectChanged();
        }
    }
    /// <summary>Gets or sets whether the center patch is drawn.</summary>
    /// <value>true initially.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The panel is disposed.</exception>
    public bool DrawCenter
    {
        get { CheckQuery(); return _drawCenter; }
        set
        {
            EnsureMutable();
            if (_drawCenter == value) return;
            _drawCenter = value;
            QueueRedraw();
        }
    }
    /// <summary>Gets or sets the center-axis horizontal stretch policy.</summary>
    /// <value>AxisStretchMode.Stretch initially.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The panel is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    public AxisStretchMode AxisStretchHorizontal
    {
        get { CheckQuery(); return _axisStretchHorizontal; }
        set
        {
            EnsureMutable();
            ValidateMode(value);
            if (_axisStretchHorizontal == value) return;
            _axisStretchHorizontal = value;
            QueueRedraw();
        }
    }
    /// <summary>Gets or sets the center-axis vertical stretch policy.</summary>
    /// <value>AxisStretchMode.Stretch initially.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The panel is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    public AxisStretchMode AxisStretchVertical
    {
        get { CheckQuery(); return _axisStretchVertical; }
        set
        {
            EnsureMutable();
            ValidateMode(value);
            if (_axisStretchVertical == value) return;
            _axisStretchVertical = value;
            QueueRedraw();
        }
    }
    /// <summary>Gets or sets the signed left border width in texture pixels.</summary>
    /// <value>Zero initially; contributes to the corresponding intrinsic minimum-size sum.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The panel is disposed.</exception>
    public int PatchMarginLeft { get => GetPatchMargin(Side.Left); set => SetPatchMargin(Side.Left, value); }
    /// <summary>Gets or sets the signed top border width in texture pixels.</summary>
    /// <value>Zero initially; contributes to the corresponding intrinsic minimum-size sum.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The panel is disposed.</exception>
    public int PatchMarginTop { get => GetPatchMargin(Side.Top); set => SetPatchMargin(Side.Top, value); }
    /// <summary>Gets or sets the signed right border width in texture pixels.</summary>
    /// <value>Zero initially; contributes to the corresponding intrinsic minimum-size sum.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The panel is disposed.</exception>
    public int PatchMarginRight { get => GetPatchMargin(Side.Right); set => SetPatchMargin(Side.Right, value); }
    /// <summary>Gets or sets the signed bottom border width in texture pixels.</summary>
    /// <value>Zero initially; contributes to the corresponding intrinsic minimum-size sum.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The panel is disposed.</exception>
    public int PatchMarginBottom { get => GetPatchMargin(Side.Bottom); set => SetPatchMargin(Side.Bottom, value); }
    /// <summary>Gets the signed border width of one side.</summary>
    /// <param name="margin">Left, Top, Right or Bottom.</param>
    /// <returns>The stored pixel width, including negative values.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined.</exception>
    /// <exception cref="InvalidOperationException">An attached query is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The panel is disposed.</exception>
    public int GetPatchMargin(Side margin) { CheckQuery(); ValidateSide(margin); return _margins[(int)margin]; }

    /// <summary>Sets one signed border width and invalidates drawing and intrinsic minimum size.</summary>
    /// <param name="margin">Left, Top, Right or Bottom.</param>
    /// <param name="value">Pixel width; zero initially. Equal assignments are silent.</param>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The panel is disposed.</exception>
    public void SetPatchMargin(Side margin, int value)
    {
        EnsureMutable(); ValidateSide(margin);
        if (_margins[(int)margin] == value) return;
        _margins[(int)margin] = value; QueueRedraw(); UpdateMinimumSize();
    }
    private void CheckQuery() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static void ValidateSide(Side side) { if (side is < Side.Left or > Side.Bottom) throw new ArgumentOutOfRangeException(nameof(side)); }
    private static void ValidateMode(AxisStretchMode mode) { if (mode is < AxisStretchMode.Stretch or > AxisStretchMode.TileFit) throw new ArgumentOutOfRangeException(nameof(mode)); }
    private void TextureUpdated(Resource texture) { if (!IsDisposed && ReferenceEquals(texture, _texture)) { QueueRedraw(); UpdateMinimumSize(); } }
    private void TextureDisposed(ElectronObject texture) { if (!IsDisposed && ReferenceEquals(texture, _texture)) Texture = null; }

    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() => new((float)_margins[0] + _margins[2], (float)_margins[1] + _margins[3]);
    /// <inheritdoc />
    protected override void OnDraw()
    {
        if (_texture is { IsDisposed: false } texture)
            RecordNinePatch(texture, new(Vector2.Zero, Size), _regionRect,
                new(new(_margins[0], _margins[1]), new(_margins[2], _margins[3]), _axisStretchHorizontal, _axisStretchVertical, _drawCenter));
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(property => property.Name != nameof(MouseFilter)).Concat(PatchProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(NinePatchRect) ? CreatePatch : base.CreateSceneInstanceFactory();
    private static Node CreatePatch() => new NinePatchRect();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_texture is { } texture) { texture.Changed -= TextureUpdated; texture.Disposed -= TextureDisposed; }
        try { base.Dispose(disposing); }
        finally { _texture = null; TextureChanged = null; }
    }
}
