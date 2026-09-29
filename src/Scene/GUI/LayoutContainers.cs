namespace Electron2D;

/// <summary>Fits direct child controls inside four themed margins.</summary>
/// <remarks>Locally visible children contribute to minimum size; visible-in-tree children are arranged on the next
/// container sort. Margin constants default to zero and can be overridden through the inherited typed theme API.</remarks>
public class MarginContainer : Container
{
    private readonly List<Control> _children = [];
    private bool _arranging;

    /// <summary>Creates an empty container with four zero-valued theme margins.</summary>
    public MarginContainer() { }

    /// <summary>Gets the resolved signed margin on one side.</summary>
    /// <param name="side">The side to query.</param>
    /// <returns>The themed margin in logical pixels.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined.</exception>
    public int GetMarginSize(Side side) => side switch
    {
        Side.Left => GetThemeConstant("margin_left"),
        Side.Top => GetThemeConstant("margin_top"),
        Side.Right => GetThemeConstant("margin_right"),
        Side.Bottom => GetThemeConstant("margin_bottom"),
        _ => throw new ArgumentOutOfRangeException(nameof(side))
    };

    private Vector2 MarginSum() => new(GetMarginSize(Side.Left) + (float)GetMarginSize(Side.Right),
        GetMarginSize(Side.Top) + (float)GetMarginSize(Side.Bottom));

    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        var margins = MarginSum(); var maximum = ChildMaximum(margins); var minimum = Vector2.Zero;
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (GetChild(index, includeInternal: true) is Control child && Sortable(child, true))
            {
                child.ContainerMaximum = maximum;
                minimum = minimum.Max(child.GetBoundMinimumSize());
            }
        return minimum + margins;
    }

    internal override Vector2 GetDesiredSize()
    {
        var desired = Vector2.Zero;
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (GetChild(index, includeInternal: true) is Control child && Sortable(child, true))
                desired = desired.Max(child.GetDesiredSize());
        return desired + MarginSum();
    }

    private Vector2? ChildMaximum(Vector2 margins)
    {
        if (!PropagateMaximumSize) return null;
        var maximum = GetCombinedMaximumSize();
        var inner = new Vector2(maximum.X < 0 ? -1 : MathF.Max(0, maximum.X - margins.X),
            maximum.Y < 0 ? -1 : MathF.Max(0, maximum.Y - margins.Y));
        if (!inner.IsFinite()) throw new InvalidOperationException("Margin content bounds exceeded finite geometry.");
        return inner;
    }

    private void Arrange()
    {
        if (!IsInsideTree || !IsVisibleInTree) return;
        if (_arranging) { QueueSort(); return; }
        _arranging = true; var expectedTree = Tree;
        try
        {
            for (var index = 0; index < GetChildCount(includeInternal: true); index++)
                if (GetChild(index, includeInternal: true) is Control child && Sortable(child)) _children.Add(child);
            var left = GetMarginSize(Side.Left); var top = GetMarginSize(Side.Top);
            var margins = MarginSum(); var size = (Size - margins).Max(Vector2.Zero); var maximum = ChildMaximum(margins);
            List<Exception>? errors = null;
            foreach (var child in _children)
            {
                if (IsDisposed || !IsInsideTree || !ReferenceEquals(Tree, expectedTree)) break;
                if (child.IsDisposed || !ReferenceEquals(child.Parent, this) || !Sortable(child)) continue;
                child.ContainerMaximum = maximum;
                try { FitChildInRect(child, new(new Vector2(left, top), size)); }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            ThrowCollected("Margin child layout callbacks failed.", errors);
        }
        finally { _children.Clear(); _arranging = false; }
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationSortChildren) Arrange();
    }

    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsHorizontal() => [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd];
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsVertical() => [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd];
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(MarginContainer) ? CreateMargin : base.CreateSceneInstanceFactory();
    private static Node CreateMargin() => new MarginContainer();
}

/// <summary>Centers each direct child at its minimum size or around the local origin.</summary>
public class CenterContainer : Container
{
    private readonly List<Control> _children = [];
    private bool _arranging, _useTopLeft;
    private static readonly PropertyDescriptor[] CenterProperties =
    [
        new PropertyDescriptor<CenterContainer, bool>(nameof(UseTopLeft), node => node.UseTopLeft, (node, value) => node.UseTopLeft = value, _ => false, stored: true)
    ];

    /// <summary>Creates a container that centers children within its rectangle.</summary>
    public CenterContainer() { }

    /// <summary>Gets or sets whether child centers use the container's top-left origin.</summary>
    /// <value>False initially; changes queue minimum-size and layout updates.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The container is disposed.</exception>
    public bool UseTopLeft
    {
        get { ThrowIfDisposed(); return _useTopLeft; }
        set { EnsureMutable(); if (_useTopLeft == value) return; _useTopLeft = value; UpdateMinimumSize(); QueueSort(); }
    }

    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        if (_useTopLeft) return Vector2.Zero;
        var minimum = Vector2.Zero;
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (GetChild(index, includeInternal: true) is Control child && Sortable(child, true))
                minimum = minimum.Max(child.GetBoundMinimumSize());
        return minimum;
    }

    internal override Vector2 GetDesiredSize()
    {
        if (_useTopLeft) return Vector2.Zero;
        var desired = Vector2.Zero;
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (GetChild(index, includeInternal: true) is Control child && Sortable(child, true))
                desired = desired.Max(child.GetBoundDesiredSize());
        return desired;
    }

    private void Arrange()
    {
        if (!IsInsideTree || !IsVisibleInTree) return;
        if (_arranging) { QueueSort(); return; }
        _arranging = true; var expectedTree = Tree;
        try
        {
            for (var index = 0; index < GetChildCount(includeInternal: true); index++)
                if (GetChild(index, includeInternal: true) is Control child && Sortable(child)) _children.Add(child);
            List<Exception>? errors = null;
            foreach (var child in _children)
            {
                if (IsDisposed || !IsInsideTree || !ReferenceEquals(Tree, expectedTree)) break;
                if (child.IsDisposed || !ReferenceEquals(child.Parent, this) || !Sortable(child)) continue;
                try
                {
                    var minimum = child.GetBoundMinimumSize();
                    var offset = (_useTopLeft ? minimum * -.5f : (Size - minimum) * .5f).Floor();
                    FitChildInRect(child, new(offset, minimum));
                }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            ThrowCollected("Center child layout callbacks failed.", errors);
        }
        finally { _children.Clear(); _arranging = false; }
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationSortChildren) Arrange();
    }

    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsHorizontal() => [];
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsVertical() => [];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(CenterProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(CenterContainer) ? CreateCenter : base.CreateSceneInstanceFactory();
    private static Node CreateCenter() => new CenterContainer();
}

/// <summary>Fits direct child controls inside a configurable width-to-height aspect ratio.</summary>
public class AspectRatioContainer : Container
{
    /// <summary>Aligns surplus space along an axis.</summary>
    public enum AlignmentMode
    {
        /// <summary>Places the child at the leading edge.</summary>
        Begin = 0,
        /// <summary>Centers the child.</summary>
        Center = 1,
        /// <summary>Places the child at the trailing edge.</summary>
        End = 2
    }

    /// <summary>Chooses how the aspect rectangle is sized against the container.</summary>
    public enum StretchMode
    {
        /// <summary>Uses all available width to determine height.</summary>
        WidthControlsHeight = 0,
        /// <summary>Uses all available height to determine width.</summary>
        HeightControlsWidth = 1,
        /// <summary>Fits the complete aspect rectangle inside both axes.</summary>
        Fit = 2,
        /// <summary>Covers both axes and permits overflow.</summary>
        Cover = 3
    }

    private readonly List<Control> _children = [];
    private bool _arranging;
    private float _ratio = 1;
    private StretchMode _stretchMode = StretchMode.Fit;
    private AlignmentMode _alignmentHorizontal = AlignmentMode.Center, _alignmentVertical = AlignmentMode.Center;
    private static readonly PropertyDescriptor[] AspectProperties =
    [
        new PropertyDescriptor<AspectRatioContainer, float>(nameof(Ratio), node => node.Ratio, (node, value) => node.Ratio = value, _ => 1, stored: true),
        new PropertyDescriptor<AspectRatioContainer, StretchMode>(nameof(Stretch), node => node.Stretch, (node, value) => node.Stretch = value, _ => StretchMode.Fit, stored: true),
        new PropertyDescriptor<AspectRatioContainer, AlignmentMode>(nameof(AlignmentHorizontal), node => node.AlignmentHorizontal, (node, value) => node.AlignmentHorizontal = value, _ => AlignmentMode.Center, stored: true),
        new PropertyDescriptor<AspectRatioContainer, AlignmentMode>(nameof(AlignmentVertical), node => node.AlignmentVertical, (node, value) => node.AlignmentVertical = value, _ => AlignmentMode.Center, stored: true)
    ];

    /// <summary>Creates a centered, fitting square-ratio container.</summary>
    public AspectRatioContainer() { }

    /// <summary>Gets or sets the finite positive target width divided by height.</summary>
    /// <value>One initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The ratio is not finite and positive.</exception>
    public float Ratio
    {
        get { ThrowIfDisposed(); return _ratio; }
        set { EnsureMutable(); if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_ratio == value) return; _ratio = value; QueueSort(); }
    }

    /// <summary>Gets or sets the aspect sizing policy.</summary>
    /// <value>Fit initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The policy is undefined.</exception>
    public StretchMode Stretch
    {
        get { ThrowIfDisposed(); return _stretchMode; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_stretchMode == value) return; _stretchMode = value; QueueSort(); }
    }

    /// <summary>Gets or sets horizontal alignment of the aspect rectangle.</summary>
    /// <value>Center initially; RTL mirrors the result.</value>
    /// <exception cref="ArgumentOutOfRangeException">The alignment is undefined.</exception>
    public AlignmentMode AlignmentHorizontal
    {
        get { ThrowIfDisposed(); return _alignmentHorizontal; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_alignmentHorizontal == value) return; _alignmentHorizontal = value; QueueSort(); }
    }

    /// <summary>Gets or sets vertical alignment of the aspect rectangle.</summary>
    /// <value>Center initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The alignment is undefined.</exception>
    public AlignmentMode AlignmentVertical
    {
        get { ThrowIfDisposed(); return _alignmentVertical; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_alignmentVertical == value) return; _alignmentVertical = value; QueueSort(); }
    }

    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        var minimum = Vector2.Zero;
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (GetChild(index, includeInternal: true) is Control child && Sortable(child, true))
                minimum = minimum.Max(child.GetBoundMinimumSize());
        return minimum;
    }

    private void Arrange()
    {
        if (!IsInsideTree || !IsVisibleInTree) return;
        if (_arranging) { QueueSort(); return; }
        _arranging = true; var expectedTree = Tree;
        try
        {
            for (var index = 0; index < GetChildCount(includeInternal: true); index++)
                if (GetChild(index, includeInternal: true) is Control child && Sortable(child)) _children.Add(child);
            List<Exception>? errors = null;
            foreach (var child in _children)
            {
                if (IsDisposed || !IsInsideTree || !ReferenceEquals(Tree, expectedTree)) break;
                if (child.IsDisposed || !ReferenceEquals(child.Parent, this) || !Sortable(child)) continue;
                try
                {
                    var widthScale = Size.X / _ratio;
                    var scale = _stretchMode switch
                    {
                        StretchMode.WidthControlsHeight => widthScale,
                        StretchMode.HeightControlsWidth => Size.Y,
                        StretchMode.Cover => MathF.Max(widthScale, Size.Y),
                        _ => MathF.Min(widthScale, Size.Y)
                    };
                    var childSize = new Vector2(_ratio * scale, scale).Max(child.GetBoundMinimumSize());
                    if (!childSize.IsFinite()) throw new InvalidOperationException("Aspect layout exceeded finite geometry.");
                    var offset = (Size - childSize) * new Vector2((int)_alignmentHorizontal * .5f, (int)_alignmentVertical * .5f);
                    if (IsLayoutRTL()) offset.X = Size.X - offset.X - childSize.X;
                    FitChildInRect(child, new(offset, childSize));
                }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            ThrowCollected("Aspect child layout callbacks failed.", errors);
        }
        finally { _children.Clear(); _arranging = false; }
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationSortChildren) Arrange();
    }

    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsHorizontal() => [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd];
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsVertical() => [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(AspectProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(AspectRatioContainer) ? CreateAspect : base.CreateSceneInstanceFactory();
    private static Node CreateAspect() => new AspectRatioContainer();
}
