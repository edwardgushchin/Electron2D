namespace Electron2D;

/// <summary>Clips and scrolls ordinary child controls with themed horizontal and vertical bars.</summary>
/// <remarks>Five implementation controls are real internal children. Ordinary child queries and scene capture omit them,
/// while rendering, input and processing retain them. A single ordinary container child is the usual content shape.</remarks>
public partial class ScrollContainer : Container
{
    /// <summary>Controls whether an axis may scroll and when its bar consumes space.</summary>
    public enum ScrollMode
    {
        /// <summary>Fit the child minimum and disable axis scrolling.</summary>
        Disabled = 0,
        /// <summary>Show a bar when content exceeds the available extent.</summary>
        Auto = 1,
        /// <summary>Always show the bar.</summary>
        ShowAlways = 2,
        /// <summary>Allow scrolling without showing the bar.</summary>
        ShowNever = 3,
        /// <summary>Reserve bar space even when the bar is hidden.</summary>
        Reserve = 4,
        /// <summary>Prefer the content's desired size, bounded by this control's maximum.</summary>
        MaximizeFirst = 5
    }

    /// <summary>Selects which edges can display a directional scroll hint.</summary>
    public enum ScrollHintMode
    {
        /// <summary>Show no hints.</summary>
        Disabled = 0,
        /// <summary>Show hints on both eligible edges.</summary>
        All = 1,
        /// <summary>Show only top and leading-side hints.</summary>
        TopAndLeft = 2,
        /// <summary>Show only bottom and trailing-side hints.</summary>
        BottomAndRight = 3
    }

    private sealed class Hint : Control
    {
        private Texture? _texture;
        private Color _color;
        private bool _tile, _flipH, _flipV;

        internal Hint() { MouseFilter = MouseFilter.Ignore; FocusMode = FocusMode.None; Visible = false; }

        internal void Configure(Texture texture, Color color, Rect2 rect, bool tile, bool flipH, bool flipV, bool visible)
        {
            if (!ReferenceEquals(_texture, texture) || _color != color || _tile != tile || _flipH != flipH || _flipV != flipV)
            {
                _texture = texture; _color = color; _tile = tile; _flipH = flipH; _flipV = flipV; QueueRedraw();
            }
            SetContainerRect(rect);
            Visible = visible;
        }

        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationDraw && _texture is { } texture)
            {
                var rect = new Rect2(0, 0, _flipH ? -Size.X : Size.X, _flipV ? -Size.Y : Size.Y);
                DrawTextureRect(texture, rect, _tile, _color);
            }
        }
    }

    private readonly Hint _topLeft = new() { Name = "_scroll_hint_top_left" };
    private readonly Hint _bottomRight = new() { Name = "_scroll_hint_bottom_right" };
    private readonly HScrollBar _hBar = new() { Name = "_h_scroll" };
    private readonly VScrollBar _vBar = new() { Name = "_v_scroll" };
    private readonly PanelContainer _focusPanel = new() { Name = "_focus" };
    private ScrollMode _horizontalMode = ScrollMode.Auto, _verticalMode = ScrollMode.Auto;
    private ScrollHintMode _hintMode;
    private bool _drawFocusBorder, _followFocus, _horizontalByDefault, _tileHint, _arranging;
    private int _deadzone;
    private Vector2 _largest, _appliedScroll;
    private Viewport? _focusViewport;

    private static readonly PropertyDescriptor[] ScrollProperties =
    [
        new PropertyDescriptor<ScrollContainer, bool>(nameof(FollowFocus), c => c.FollowFocus, (c, v) => c.FollowFocus = v, _ => false, stored: true),
        new PropertyDescriptor<ScrollContainer, bool>(nameof(DrawFocusBorder), c => c.DrawFocusBorder, (c, v) => c.DrawFocusBorder = v, _ => false, stored: true),
        new PropertyDescriptor<ScrollContainer, int>(nameof(ScrollHorizontal), c => c.ScrollHorizontal, (c, v) => c.ScrollHorizontal = v, _ => 0, stored: true),
        new PropertyDescriptor<ScrollContainer, int>(nameof(ScrollVertical), c => c.ScrollVertical, (c, v) => c.ScrollVertical = v, _ => 0, stored: true),
        new PropertyDescriptor<ScrollContainer, float>(nameof(ScrollHorizontalCustomStep), c => c.ScrollHorizontalCustomStep, (c, v) => c.ScrollHorizontalCustomStep = v, _ => -1, stored: true),
        new PropertyDescriptor<ScrollContainer, float>(nameof(ScrollVerticalCustomStep), c => c.ScrollVerticalCustomStep, (c, v) => c.ScrollVerticalCustomStep = v, _ => -1, stored: true),
        new PropertyDescriptor<ScrollContainer, ScrollMode>(nameof(HorizontalScrollMode), c => c.HorizontalScrollMode, (c, v) => c.HorizontalScrollMode = v, _ => ScrollMode.Auto, stored: true),
        new PropertyDescriptor<ScrollContainer, ScrollMode>(nameof(VerticalScrollMode), c => c.VerticalScrollMode, (c, v) => c.VerticalScrollMode = v, _ => ScrollMode.Auto, stored: true),
        new PropertyDescriptor<ScrollContainer, bool>(nameof(ScrollHorizontalByDefault), c => c.ScrollHorizontalByDefault, (c, v) => c.ScrollHorizontalByDefault = v, _ => false, stored: true),
        new PropertyDescriptor<ScrollContainer, int>(nameof(ScrollDeadzone), c => c.ScrollDeadzone, (c, v) => c.ScrollDeadzone = v, _ => 0, stored: true),
        new PropertyDescriptor<ScrollContainer, ScrollHintMode>(nameof(HintMode), c => c.HintMode, (c, v) => c.HintMode = v, _ => ScrollHintMode.Disabled, stored: true),
        new PropertyDescriptor<ScrollContainer, bool>(nameof(TileScrollHint), c => c.TileScrollHint, (c, v) => c.TileScrollHint = v, _ => false, stored: true),
        new PropertyDescriptor<ScrollContainer, bool>(nameof(ClipContents), c => c.ClipContents, (c, v) => c.ClipContents = v, _ => true, stored: true),
        new PropertyDescriptor<ScrollContainer, bool>(nameof(PropagateMaximumSize), c => c.PropagateMaximumSize, (c, v) => c.PropagateMaximumSize = v, _ => false, stored: true)
    ];

    /// <summary>Creates a clipped container with five themed implementation children.</summary>
    public ScrollContainer()
    {
        ClipContents = true;
        PropagateMaximumSize = false;
        _deadzone = ProjectSettings.GetWithOverride(ProjectSettings.DefaultScrollDeadzone);
        _hBar.FocusMode = FocusMode.None;
        _vBar.FocusMode = FocusMode.None;
        _focusPanel.MouseFilter = MouseFilter.Ignore;
        _focusPanel.FocusMode = FocusMode.None;
        _focusPanel.Visible = false;
        AddChild(_topLeft, InternalMode.Back);
        AddChild(_bottomRight, InternalMode.Back);
        AddChild(_hBar, InternalMode.Back);
        AddChild(_vBar, InternalMode.Back);
        AddChild(_focusPanel, InternalMode.Back);
        _hBar.ValueChanged += _ => ScrollMoved();
        _vBar.ValueChanged += _ => ScrollMoved();
    }

    /// <summary>Occurs when a touch drag first exceeds the configured deadzone.</summary>
    public event Action? ScrollStarted;
    /// <summary>Occurs when a begun touch drag and its inertia finish or are cancelled.</summary>
    public event Action? ScrollEnded;

    /// <summary>Gets the owned horizontal scroll bar.</summary>
    /// <returns>The live internal bar; callers may customize its range theme.</returns>
    public HScrollBar GetHScrollBar() { ThrowIfDisposed(); return _hBar; }
    /// <summary>Gets the owned vertical scroll bar.</summary>
    /// <returns>The live internal bar; callers may customize its range theme.</returns>
    public VScrollBar GetVScrollBar() { ThrowIfDisposed(); return _vBar; }

    /// <summary>Gets or sets the horizontal scroll offset in integer logical pixels.</summary>
    /// <value>Truncates the live bar value toward zero on read; assignment clamps through its inherited range.</value>
    public int ScrollHorizontal
    {
        get { ThrowIfDisposed(); return (int)_hBar.Value; }
        set { EnsureMutable(); _hBar.Value = value; CancelDrag(); }
    }
    /// <summary>Gets or sets the vertical scroll offset in integer logical pixels.</summary>
    /// <value>Truncates the live bar value toward zero on read; assignment clamps through its inherited range.</value>
    public int ScrollVertical
    {
        get { ThrowIfDisposed(); return (int)_vBar.Value; }
        set { EnsureMutable(); _vBar.Value = value; CancelDrag(); }
    }
    /// <summary>Gets or sets the horizontal bar's custom arrow step.</summary>
    /// <value>Minus one initially; nonnegative values override the bar's Step.</value>
    public float ScrollHorizontalCustomStep { get => _hBar.CustomStep; set { EnsureMutable(); _hBar.CustomStep = value; } }
    /// <summary>Gets or sets the vertical bar's custom arrow step.</summary>
    /// <value>Minus one initially; nonnegative values override the bar's Step.</value>
    public float ScrollVerticalCustomStep { get => _vBar.CustomStep; set { EnsureMutable(); _vBar.CustomStep = value; } }
    /// <summary>Gets or sets horizontal scrolling and bar visibility policy.</summary>
    /// <value>Auto initially. Undefined enum values are rejected.</value>
    /// <exception cref="ArgumentOutOfRangeException">The requested mode is undefined.</exception>
    public ScrollMode HorizontalScrollMode
    {
        get { ThrowIfDisposed(); return _horizontalMode; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_horizontalMode == value) return; _horizontalMode = value; UpdateMinimumSize(); QueueSort(); }
    }
    /// <summary>Gets or sets vertical scrolling and bar visibility policy.</summary>
    /// <value>Auto initially. Undefined enum values are rejected.</value>
    /// <exception cref="ArgumentOutOfRangeException">The requested mode is undefined.</exception>
    public ScrollMode VerticalScrollMode
    {
        get { ThrowIfDisposed(); return _verticalMode; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_verticalMode == value) return; _verticalMode = value; UpdateMinimumSize(); QueueSort(); }
    }
    /// <summary>Gets or sets whether an unmodified vertical wheel scrolls horizontally.</summary>
    /// <value>False initially; Shift reverses the choice for one event.</value>
    public bool ScrollHorizontalByDefault { get { ThrowIfDisposed(); return _horizontalByDefault; } set { EnsureMutable(); _horizontalByDefault = value; } }
    /// <summary>Gets or sets the signed touch-drag deadzone in logical pixels.</summary>
    /// <value>Sampled from <see cref="ProjectSettings.DefaultScrollDeadzone"/> at construction; later project changes do not change this instance.</value>
    public int ScrollDeadzone { get { ThrowIfDisposed(); return _deadzone; } set { EnsureMutable(); _deadzone = value; } }
    /// <summary>Gets or sets which eligible content edges display scroll hints.</summary>
    /// <value>Disabled initially; undefined enum values are rejected.</value>
    /// <exception cref="ArgumentOutOfRangeException">The requested mode is undefined.</exception>
    public ScrollHintMode HintMode
    {
        get { ThrowIfDisposed(); return _hintMode; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_hintMode == value) return; _hintMode = value; UpdateHints(); }
    }
    /// <summary>Gets or sets whether hint textures tile instead of stretch.</summary>
    /// <value>False initially. Only the two hint controls are affected.</value>
    public bool TileScrollHint { get { ThrowIfDisposed(); return _tileHint; } set { EnsureMutable(); if (_tileHint == value) return; _tileHint = value; UpdateHints(); } }
    /// <summary>Gets or sets automatic scrolling to a newly focused descendant.</summary>
    /// <value>False initially; focus events from the containing viewport drive this policy.</value>
    public bool FollowFocus { get { ThrowIfDisposed(); return _followFocus; } set { EnsureMutable(); _followFocus = value; } }
    /// <summary>Gets or sets whether focused content receives a separate border overlay.</summary>
    /// <value>False initially; the focus style's expanded drawing bounds are kept inside the clip.</value>
    public bool DrawFocusBorder
    {
        get { ThrowIfDisposed(); return _drawFocusBorder; }
        set { EnsureMutable(); if (_drawFocusBorder == value) return; _drawFocusBorder = value; UpdateMinimumSize(); QueueSort(); QueueRedraw(); }
    }

    private bool HEnabled => _horizontalMode != ScrollMode.Disabled;
    private bool VEnabled => _verticalMode != ScrollMode.Disabled;
    private bool HVisible => ReferenceEquals(_hBar.Parent, this) && _hBar.Visible;
    private bool VVisible => ReferenceEquals(_vBar.Parent, this) && _vBar.Visible;

    private (float Left, float Top, float Right, float Bottom) Margins()
    {
        var panel = GetThemeStyleBox("panel") ?? throw new InvalidOperationException("Scroll container requires a panel style.");
        var focus = _drawFocusBorder
            ? GetThemeStyleBox("focus") ?? throw new InvalidOperationException("Scroll container requires a focus style while its border is enabled.")
            : null;
        float SideMargin(Side side) => focus is null ? panel.GetMargin(side) : Math.Max(panel.GetMargin(side), focus.GetMargin(side));
        return (SideMargin(Side.Left), SideMargin(Side.Top), SideMargin(Side.Right), SideMargin(Side.Bottom));
    }

    private Vector2 LargestChildSize(bool desired)
    {
        var largest = Vector2.Zero;
        for (var index = 0; index < ChildCount; index++)
        {
            var node = GetChild(index);
            if (Sortable(node, localVisibility: true) && node is Control child)
                largest = largest.Max(desired ? child.GetBoundDesiredSize() : child.GetBoundMinimumSize());
        }
        return largest;
    }

    private Vector2 Minimum(bool desired)
    {
        var largest = LargestChildSize(desired);
        if (!desired) _largest = largest;
        var (left, top, right, bottom) = Margins();
        var verticalBar = _vBar.GetMinimumSize().X + GetThemeConstant("scrollbar_h_separation");
        var horizontalBar = _hBar.GetMinimumSize().Y + GetThemeConstant("scrollbar_v_separation");
        var verticalNeeded = _verticalMode is ScrollMode.ShowAlways or ScrollMode.Reserve ||
            _verticalMode is ScrollMode.Auto or ScrollMode.MaximizeFirst && largest.Y > Size.Y;
        var horizontalNeeded = _horizontalMode is ScrollMode.ShowAlways or ScrollMode.Reserve ||
            _horizontalMode is ScrollMode.Auto or ScrollMode.MaximizeFirst && largest.X > Size.X;
        var max = GetCombinedMaximumSize();
        var x = _horizontalMode switch
        {
            ScrollMode.Disabled => largest.X,
            ScrollMode.MaximizeFirst => max.X >= 0 ? Math.Min(largest.X, max.X) : largest.X,
            _ => 0
        };
        var y = _verticalMode switch
        {
            ScrollMode.Disabled => largest.Y,
            ScrollMode.MaximizeFirst => max.Y >= 0 ? Math.Min(largest.Y, max.Y) : largest.Y,
            _ => 0
        };
        if (_horizontalMode is ScrollMode.Disabled or ScrollMode.MaximizeFirst && verticalNeeded) x += verticalBar;
        if (_verticalMode is ScrollMode.Disabled or ScrollMode.MaximizeFirst && horizontalNeeded) y += horizontalBar;
        return new(x + left + right, y + top + bottom);
    }

    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() => Minimum(desired: false);
    internal override Vector2 GetDesiredSize() => Minimum(desired: true);

    private void ScrollMoved()
    {
        if (IsDisposed) return;
        if (IsInsideTree) QueueSort();
        UpdateHints();
    }

    private void Arrange()
    {
        if (_arranging || !IsInsideTree || !IsVisibleInTree) return;
        _arranging = true;
        try
        {
            _largest = LargestChildSize(desired: false);
            var (left, top, right, bottom) = Margins();
            var inner = (Size - new Vector2(left + right, top + bottom)).Max(Vector2.Zero);
            _hBar.Visible = _horizontalMode == ScrollMode.ShowAlways ||
                _horizontalMode is ScrollMode.Auto or ScrollMode.Reserve or ScrollMode.MaximizeFirst && _largest.X > inner.X;
            _vBar.Visible = _verticalMode == ScrollMode.ShowAlways ||
                _verticalMode is ScrollMode.Auto or ScrollMode.Reserve or ScrollMode.MaximizeFirst && _largest.Y > inner.Y;
            var hHeight = HVisible ? _hBar.GetBoundMinimumSize().Y : 0;
            var vWidth = VVisible ? _vBar.GetBoundMinimumSize().X : 0;
            _hBar.MaxValue = _largest.X;
            _hBar.Page = Math.Max(0, inner.X - vWidth);
            _vBar.MaxValue = _largest.Y;
            _vBar.Page = Math.Max(0, inner.Y - hHeight);
            var rtl = IsLayoutRTL();
            var leftBar = rtl ? vWidth : 0;
            _hBar.SetContainerRect(new(left + leftBar, Size.Y - bottom - hHeight,
                Math.Max(0, inner.X - vWidth), hHeight));
            _vBar.SetContainerRect(new(rtl ? left : Size.X - right - vWidth, top,
                vWidth, Math.Max(0, inner.Y - hHeight)));
            var reserveV = VVisible || _verticalMode == ScrollMode.Reserve;
            var reserveH = HVisible || _horizontalMode == ScrollMode.Reserve;
            var reservedWidth = reserveV ? _vBar.GetMinimumSize().X + GetThemeConstant("scrollbar_h_separation") : 0;
            var reservedHeight = reserveH ? _hBar.GetMinimumSize().Y + GetThemeConstant("scrollbar_v_separation") : 0;
            var contentArea = (inner - new Vector2(reservedWidth, reservedHeight)).Max(Vector2.Zero);
            var offset = new Vector2(left + (rtl ? reservedWidth : 0), top);
            var scroll = new Vector2(ScrollHorizontal, ScrollVertical);
            for (var index = 0; index < ChildCount; index++)
            {
                var node = GetChild(index);
                if (!Sortable(node) || node is not Control child) continue;
                var minimum = child.GetCombinedMinimumSize();
                var maximum = child.GetCombinedMaximumSize();
                var width = minimum.X; var height = minimum.Y;
                if ((child.SizeFlagsHorizontal & SizeFlags.Expand) != 0) width = Math.Max(contentArea.X, width);
                if ((child.SizeFlagsVertical & SizeFlags.Expand) != 0) height = Math.Max(contentArea.Y, height);
                if (maximum.X >= 0) width = Math.Min(width, maximum.X);
                if (maximum.Y >= 0) height = Math.Min(height, maximum.Y);
                var position = (offset - scroll).Floor();
                FitChildInRect(child, new(position, new(width, height)));
            }
            _appliedScroll = scroll;
            if (_drawFocusBorder)
            {
                var focus = GetThemeStyleBox("focus") ?? throw new InvalidOperationException("Scroll container requires a focus style while its border is enabled.");
                var bounds = focus.GetDrawRect(new(Vector2.Zero, Size));
                var insetLeft = Math.Max(0, -bounds.Position.X);
                var insetTop = Math.Max(0, -bounds.Position.Y);
                var insetRight = Math.Max(0, bounds.End.X - Size.X);
                var insetBottom = Math.Max(0, bounds.End.Y - Size.Y);
                _focusPanel.SetContainerRect(new(insetLeft, insetTop,
                    Math.Max(0, Size.X - insetLeft - insetRight),
                    Math.Max(0, Size.Y - insetTop - insetBottom)));
            }
            UpdateHints();
            UpdateMaximumSize();
            QueueRedraw();
        }
        finally { _arranging = false; }
    }

    private void UpdateHints()
    {
        if (IsDisposed || _topLeft.IsDisposed || _bottomRight.IsDisposed) return;
        var (left, top, right, bottom) = Margins();
        var inner = (Size - new Vector2(left + right, top + bottom)).Max(Vector2.Zero);
        var verticalAbove = _vBar.Value > 1;
        var verticalBelow = _vBar.Value < _largest.Y - inner.Y - 1;
        var horizontalBefore = _hBar.Value > 1;
        var horizontalAfter = _hBar.Value < _largest.X - inner.X - 1;
        var vertical = verticalAbove || verticalBelow;
        var horizontal = horizontalBefore || horizontalAfter;
        var rtl = IsLayoutRTL();
        var vTexture = GetThemeIcon("scroll_hint_vertical") ?? throw new InvalidOperationException("Scroll container requires a vertical hint icon.");
        var hTexture = GetThemeIcon("scroll_hint_horizontal") ?? throw new InvalidOperationException("Scroll container requires a horizontal hint icon.");
        if (vertical)
        {
            var size = vTexture.GetSize();
            var color = GetThemeColor("scroll_hint_vertical_color");
            _topLeft.Configure(vTexture, color, new(0, 0, Size.X, size.Y), _tileHint, false, false,
                !horizontal && _hintMode is ScrollHintMode.All or ScrollHintMode.TopAndLeft && verticalAbove);
            _bottomRight.Configure(vTexture, color, new(0, Size.Y - size.Y, Size.X, size.Y), _tileHint, false, true,
                !horizontal && _hintMode is ScrollHintMode.All or ScrollHintMode.BottomAndRight && verticalBelow);
        }
        else
        {
            var size = hTexture.GetSize();
            var color = GetThemeColor("scroll_hint_horizontal_color");
            _topLeft.Configure(hTexture, color, new(rtl ? Size.X - size.X : 0, 0, size.X, Size.Y), _tileHint, false, false,
                horizontal && (_hintMode == ScrollHintMode.All ||
                (rtl ? _hintMode == ScrollHintMode.BottomAndRight : _hintMode == ScrollHintMode.TopAndLeft)) && horizontalBefore);
            _bottomRight.Configure(hTexture, color, new(rtl ? 0 : Size.X - size.X, 0, size.X, Size.Y), _tileHint, true, false,
                horizontal && (_hintMode == ScrollHintMode.All ||
                (rtl ? _hintMode == ScrollHintMode.TopAndLeft : _hintMode == ScrollHintMode.BottomAndRight)) && horizontalAfter);
        }
    }

    private void FocusChanged(Control focused)
    {
        if (_followFocus && IsAncestorOf(focused)) EnsureControlVisible(focused);
        if (_drawFocusBorder) QueueRedraw();
    }

    /// <summary>Scrolls just enough to bring a descendant control into view.</summary>
    /// <param name="control">A live descendant, whose transformed bounds determine the adjustment.</param>
    /// <exception cref="ArgumentException">The control is not a descendant.</exception>
    public void EnsureControlVisible(Control control)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(control);
        if (!IsAncestorOf(control)) throw new ArgumentException("The control must be a descendant.", nameof(control));
        var transform = GetGlobalTransform().AffineInverse() * control.GetGlobalTransform();
        var rect = transform * new Rect2(Vector2.Zero, control.Size);
        rect.Position += _appliedScroll - new Vector2(ScrollHorizontal, ScrollVertical);
        var side = VVisible ? _vBar.Size.X : 0;
        var bottom = HVisible ? _hBar.Size.Y : 0;
        var rtl = IsLayoutRTL();
        var x = Math.Max(Math.Min(rect.Position.X - (rtl ? side : 0), 0), rect.End.X - Size.X + (rtl ? 0 : side));
        var y = Math.Max(Math.Min(rect.Position.Y, 0), rect.End.Y - Size.Y + bottom);
        ScrollHorizontal += (int)x;
        ScrollVertical += (int)y;
    }

    /// <summary>Warns when the ordinary content has zero or multiple locally visible direct controls.</summary>
    /// <returns>The inherited warnings followed by the single-content recommendation, when needed.</returns>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        var count = 0;
        for (var index = 0; index < ChildCount; index++)
            if (Sortable(GetChild(index), localVisibility: true)) count++;
        return count == 1 ? warnings : [.. warnings,
            "ScrollContainer is intended to work with a single child control. Use a container as child, or set a custom minimum size."];
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (IsDisposed) return;
        switch (what)
        {
            case NotificationReady:
                _focusViewport = GetViewport();
                if (_focusViewport is not null) _focusViewport.GUIFocusChanged += FocusChanged;
                Arrange();
                break;
            case NotificationExitTree:
                if (_focusViewport is not null) _focusViewport.GUIFocusChanged -= FocusChanged;
                _focusViewport = null;
                CancelDrag();
                break;
            case NotificationSortChildren:
                Arrange();
                break;
            case NotificationThemeChanged:
                if (GetThemeStyleBox("focus") is { } style) _focusPanel.AddThemeStyleBoxOverride("panel", style);
                UpdateHints();
                break;
            case NotificationDraw:
                DrawStyleBox(GetThemeStyleBox("panel") ?? throw new InvalidOperationException("Scroll container requires a panel style."), new(Vector2.Zero, Size));
                _focusPanel.Visible = _drawFocusBorder && (HasFocus(true) || _focusViewport?.GetGUIFocusOwner() is { } focus && IsAncestorOf(focus));
                break;
            case NotificationInternalProcess:
                ProcessTouchDrag(ProcessDeltaTime);
                break;
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Where(property => property.Name is not (nameof(ClipContents) or nameof(PropagateMaximumSize))).Concat(ScrollProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(ScrollContainer) ? CreateContainer : base.CreateSceneInstanceFactory();
    private static Node CreateContainer() => new ScrollContainer();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_focusViewport is not null) _focusViewport.GUIFocusChanged -= FocusChanged;
        _focusViewport = null;
        try { base.Dispose(disposing); }
        finally { ScrollStarted = null; ScrollEnded = null; }
    }
}
