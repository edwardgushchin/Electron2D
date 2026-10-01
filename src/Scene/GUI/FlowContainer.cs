namespace Electron2D;

/// <summary>Arranges direct visible child controls in pixel-aligned wrapping rows or columns.</summary>
/// <remarks>Primary expansion uses positive child stretch weights and maximum bounds. Last-wrap alignment is
/// relative to the preceding row/column's occupied group. Layout direction and reverse fill control placement.
/// Sorting reuses prepared buffers; child callback errors do not prevent later placements.</remarks>
public class FlowContainer : Container
{
    /// <summary>Specifies primary-axis alignment of unused space.</summary>
    public enum AlignmentMode
    {
        /// <summary>Uses the leading edge.</summary>
        Begin = 0,
        /// <summary>Centers the occupied group.</summary>
        Center = 1,
        /// <summary>Uses the trailing edge.</summary>
        End = 2
    }
    /// <summary>Specifies alignment of a final unfilled wrap relative to the preceding group.</summary>
    public enum LastWrapAlignmentMode
    {
        /// <summary>Uses the ordinary alignment within the full container.</summary>
        Inherit = 0,
        /// <summary>Uses the preceding group's leading edge.</summary>
        Begin = 1,
        /// <summary>Centers within the preceding group.</summary>
        Center = 2,
        /// <summary>Uses the preceding group's trailing edge.</summary>
        End = 3
    }
    private bool _vertical, _fixed, _reverseFill, _arranging, _arrangeAgain;
    private AlignmentMode _alignment;
    private LastWrapAlignmentMode _lastWrapAlignment;
    private int _cachedCross, _lineCount;
    private readonly List<Slot> _slots = [];
    private readonly List<Line> _lines = [];
    private struct Slot
    {
        internal Control Child;
        internal int Main, Cross, MaxMain, Extra;
        internal float Ratio;
        internal bool Active;
        internal SizeFlags CrossFlags;
        internal Rect2 Rect;
    }
    private struct Line { internal int Start, Count, Cross, Used, Remaining; internal bool Filled; }
    private static readonly PropertyDescriptor[] FlowProperties =
    [
        new PropertyDescriptor<FlowContainer, bool>(nameof(Vertical), node => node.Vertical, (node, value) => node.Vertical = value, _ => false, stored: true),
        new PropertyDescriptor<FlowContainer, bool>(nameof(ReverseFill), node => node.ReverseFill, (node, value) => node.ReverseFill = value, _ => false, stored: true),
        new PropertyDescriptor<FlowContainer, AlignmentMode>(nameof(Alignment), node => node.Alignment, (node, value) => node.Alignment = value, _ => AlignmentMode.Begin, stored: true),
        new PropertyDescriptor<FlowContainer, LastWrapAlignmentMode>(nameof(LastWrapAlignment), node => node.LastWrapAlignment, (node, value) => node.LastWrapAlignment = value, _ => LastWrapAlignmentMode.Inherit, stored: true),
        new PropertyDescriptor<FlowContainer, int>(nameof(HSeparation), node => node.HSeparation, (node, value) => node.HSeparation = value, node => node.InheritedThemeConstant("h_separation")),
        new PropertyDescriptor<FlowContainer, int>(nameof(VSeparation), node => node.VSeparation, (node, value) => node.VSeparation = value, node => node.InheritedThemeConstant("v_separation"))
    ];
    /// <summary>Creates a horizontal flow with Begin/Inherit alignment and four-pixel gaps.</summary>
    public FlowContainer() { }
    /// <summary>Initializes a fixed-orientation specialization.</summary>
    /// <param name="vertical">Whether children fill columns before wrapping horizontally.</param>
    protected FlowContainer(bool vertical) { _vertical = vertical; _fixed = true; }
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    /// <summary>Gets or sets the orientation of primary filling.</summary>
    /// <value>False initially; fixed HFlow/VFlow assignments reject, including equal values.</value>
    /// <exception cref="InvalidOperationException">Orientation is fixed or mutation is off-owner/capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The flow is disposed.</exception>
    public bool Vertical
    {
        get { Check(); return _vertical; }
        set { EnsureMutable(); if (_fixed) throw new InvalidOperationException("This flow has a fixed orientation."); _vertical = value; UpdateMinimumSize(); Arrange(); }
    }
    /// <summary>Gets or sets whether the cross-axis wrap direction is reversed.</summary>
    /// <value>False initially. Horizontal reversal fills upward; vertical reversal combines with RTL.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner/capture-owned or layout does not settle.</exception>
    /// <exception cref="ObjectDisposedException">The flow is disposed.</exception>
    public bool ReverseFill
    {
        get { Check(); return _reverseFill; }
        set { EnsureMutable(); if (_reverseFill == value) return; _reverseFill = value; Arrange(); }
    }
    /// <summary>Gets or sets alignment of residual primary-axis space.</summary>
    /// <value>Begin initially; attached changes arrange synchronously.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner/capture-owned or layout does not settle.</exception>
    /// <exception cref="ObjectDisposedException">The flow is disposed.</exception>
    public AlignmentMode Alignment
    {
        get { Check(); return _alignment; }
        set { EnsureMutable(); if (value is < AlignmentMode.Begin or > AlignmentMode.End) throw new ArgumentOutOfRangeException(nameof(value)); if (_alignment == value) return; _alignment = value; Arrange(); }
    }
    /// <summary>Gets or sets relative alignment of a final unfilled wrap.</summary>
    /// <value>Inherit initially. An unfilled non-first wrap aligns relative to the preceding group.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner/capture-owned or layout does not settle.</exception>
    /// <exception cref="ObjectDisposedException">The flow is disposed.</exception>
    public LastWrapAlignmentMode LastWrapAlignment
    {
        get { Check(); return _lastWrapAlignment; }
        set { EnsureMutable(); if (value is < LastWrapAlignmentMode.Inherit or > LastWrapAlignmentMode.End) throw new ArgumentOutOfRangeException(nameof(value)); if (_lastWrapAlignment == value) return; _lastWrapAlignment = value; Arrange(); }
    }
    /// <summary>Gets or sets the signed horizontal gap in pixels.</summary>
    /// <value>Four in the built-in theme; assignments create local typed overrides.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The flow is disposed.</exception>
    public int HSeparation { get => GetThemeConstant("h_separation"); set => SetGap("h_separation", value); }
    /// <summary>Gets or sets the signed vertical gap in pixels.</summary>
    /// <value>Four in the built-in theme; assignments create local typed overrides.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The flow is disposed.</exception>
    public int VSeparation { get => GetThemeConstant("v_separation"); set => SetGap("v_separation", value); }
    private void SetGap(string name, int value)
    {
        EnsureMutable(); if (GetThemeConstant(name) == value) return;
        if (value == InheritedThemeConstant(name)) RemoveThemeConstantOverride(name); else AddThemeConstantOverride(name, value);
        UpdateMinimumSize(); QueueSort();
    }
    /// <summary>Returns the row or column count cached by the latest successful visible arrangement.</summary>
    /// <returns>Zero before first arrangement; one after an empty visible arrangement.</returns>
    /// <exception cref="InvalidOperationException">Access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The flow is disposed.</exception>
    public int GetLineCount() { Check(); return _lineCount; }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() => Minimum(false);
    internal override Vector2 GetDesiredSize() => Minimum(true);
    private Vector2 Minimum(bool desired)
    {
        var primary = 0; var count = 0;
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (Sortable(GetChild(index, includeInternal: true), true) && GetChild(index, includeInternal: true) is Control child)
            {
                var size = desired ? child.GetBoundDesiredSize() : child.GetBoundMinimumSize();
                primary = Math.Max(primary, Pixel(_vertical ? size.Y : size.X)); count++;
            }
        return count == 0 ? Vector2.Zero : _vertical ? new(_cachedCross, primary) : new(primary, _cachedCross);
    }
    private void Arrange()
    {
        if (_arranging) { _arrangeAgain = true; QueueSort(); return; }
        if (!IsInsideTree || !IsVisibleInTree) return;
        _arranging = true;
        try
        {
            for (var pass = 0; pass < 64; pass++)
            {
                _arrangeAgain = false; ArrangeOnce();
                if (!_arrangeAgain || IsDisposed || !IsInsideTree) return;
            }
            throw new InvalidOperationException("Flow layout callbacks did not settle after 64 passes.");
        }
        finally { _slots.Clear(); _lines.Clear(); _arranging = false; _arrangeAgain = false; }
    }
    private void ArrangeOnce()
    {
        _slots.Clear(); _lines.Clear(); var tree = Tree;
        var vertical = _vertical; var reverse = _reverseFill; var alignment = _alignment; var lastAlignment = _lastWrapAlignment;
        var mainGap = vertical ? VSeparation : HSeparation; var crossGap = vertical ? HSeparation : VSeparation;
        var size = Size; var available = Pixel(vertical ? size.Y : size.X); var rtl = IsLayoutRTL();
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (Sortable(GetChild(index, includeInternal: true)) && GetChild(index, includeInternal: true) is Control child)
            {
                child.ContainerMaximum = null;
                var minimum = child.GetBoundMinimumSize(); var maximum = child.GetCombinedMaximumSize();
                var ratio = child.SizeFlagsStretchRatio;
                var expand = ((vertical ? child.SizeFlagsVertical : child.SizeFlagsHorizontal) & SizeFlags.Expand) != 0 && ratio > 0;
                _slots.Add(new()
                {
                    Child = child,
                    Main = Pixel(vertical ? minimum.Y : minimum.X),
                    Cross = Pixel(vertical ? minimum.X : minimum.Y),
                    MaxMain = Pixel(vertical ? maximum.Y : maximum.X),
                    Ratio = ratio,
                    Active = expand,
                    CrossFlags = vertical ? child.SizeFlagsHorizontal : child.SizeFlagsVertical
                });
            }
        var line = new Line();
        for (var index = 0; index < _slots.Count; index++)
        {
            var slot = _slots[index]; var length = checked(line.Used + (line.Count == 0 ? 0 : mainGap));
            if (line.Count > 0 && checked(length + slot.Main) > available)
            {
                line.Filled = true; line.Remaining = checked(available - line.Used); _lines.Add(line);
                line = new() { Start = index }; length = 0;
            }
            line.Used = checked(length + slot.Main); line.Cross = Math.Max(line.Cross, slot.Cross); line.Count++;
        }
        line.Filled = line.Count > 0 && (long)line.Used + _slots[^1].Main > available;
        line.Remaining = checked(available - line.Used); _lines.Add(line);
        var cross = 0;
        for (var lineIndex = 0; lineIndex < _lines.Count; lineIndex++)
        {
            line = _lines[lineIndex]; Expand(ref line);
            var offset = AlignmentOffset(alignment, lastAlignment, line.Remaining, lineIndex > 0 && !line.Filled ? _lines[lineIndex - 1].Remaining : null);
            for (var index = line.Start; index < line.Start + line.Count; index++)
            {
                var slot = _slots[index]; var main = checked(slot.Main + slot.Extra);
                var height = (slot.CrossFlags & (SizeFlags.Fill | SizeFlags.ShrinkCenter | SizeFlags.ShrinkEnd)) != 0 ? line.Cross : slot.Cross;
                var rect = vertical ? new Rect2(cross, offset, height, main) : new Rect2(offset, cross, main, height);
                if (_lines.Count > 1 && slot.Child is TextureRect { HasSizeDependentMinimum: true }) rect.Size = slot.Child.Size;
                var position = rect.Position;
                if (reverse && !vertical) position.Y = size.Y - position.Y - rect.Size.Y;
                if ((!vertical && rtl) || vertical && rtl != reverse) position.X = size.X - position.X - rect.Size.X;
                rect.Position = position;
                if (!rect.IsFinite() || main < 0 || height < 0) throw new InvalidOperationException("Flow layout exceeds finite geometry bounds.");
                slot.Rect = rect; _slots[index] = slot;
                if (index + 1 < line.Start + line.Count) offset = checked(offset + Pixel(vertical ? rect.Size.Y : rect.Size.X) + mainGap);
            }
            _lines[lineIndex] = line;
            if (lineIndex + 1 < _lines.Count) cross = checked(cross + line.Cross + crossGap);
        }
        var cached = checked(cross + _lines[^1].Cross);
        _cachedCross = cached; _lineCount = _lines.Count;
        List<Exception>? errors = null;
        foreach (var slot in _slots)
        {
            if (IsDisposed || !IsInsideTree || !ReferenceEquals(Tree, tree)) break;
            if (slot.Child.IsDisposed || !ReferenceEquals(slot.Child.Parent, this) || !Sortable(slot.Child)) continue;
            try { FitChildInRect(slot.Child, slot.Rect); } catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Flow child layout callbacks failed.", errors);
    }
    private void Expand(ref Line line)
    {
        var remaining = Math.Max(0, line.Remaining); var ratio = 0f;
        for (var index = line.Start; index < line.Start + line.Count; index++) if (_slots[index].Active) ratio += _slots[index].Ratio;
        if (!float.IsFinite(ratio)) throw new InvalidOperationException("Flow expansion weights exceed finite range.");
        // ponytail: Capped weight refit is quadratic within a line; use indexed allocation if large-menu profiling requires it.
        while (ratio > 0)
        {
            var capped = false;
            for (var index = line.Start; index < line.Start + line.Count; index++)
            {
                var slot = _slots[index]; if (!slot.Active) continue;
                var extra = Pixel(remaining * slot.Ratio / ratio); var maximum = slot.MaxMain < 0 ? int.MaxValue : Math.Max(0, slot.MaxMain - slot.Main);
                if (extra <= maximum) continue;
                slot.Extra = maximum; slot.Active = false; _slots[index] = slot;
                remaining -= maximum; ratio -= slot.Ratio; capped = true; break;
            }
            if (capped) continue;
            for (var index = line.Start; index < line.Start + line.Count; index++)
            {
                var slot = _slots[index]; if (!slot.Active) continue;
                slot.Extra = Pixel(remaining * slot.Ratio / ratio); _slots[index] = slot;
            }
            break;
        }
        var used = 0; for (var index = line.Start; index < line.Start + line.Count; index++) used = checked(used + _slots[index].Extra);
        line.Remaining = Math.Max(0, checked(line.Remaining - used));
    }
    private static int AlignmentOffset(AlignmentMode alignment, LastWrapAlignmentMode last, int remaining, int? previous)
    {
        if (remaining <= 0) return 0;
        var fraction = (float)alignment / 2;
        return Pixel(previous is { } prior && last != LastWrapAlignmentMode.Inherit
            ? prior * fraction + (remaining - (float)prior) * ((int)last - 1) / 2 : remaining * fraction);
    }
    private static int Pixel(float value)
    {
        if (!float.IsFinite(value) || (double)value > int.MaxValue || (double)value < int.MinValue) throw new InvalidOperationException("Flow layout exceeds integer pixel range.");
        return (int)value;
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationSortChildren) { try { Arrange(); } finally { if (!IsDisposed) UpdateMinimumSize(); } }
        else if (what == NotificationTranslationChanged) QueueSort();
    }
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsHorizontal() => _vertical ? [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd] : base.GetAllowedSizeFlagsHorizontal();
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsVertical() => !_vertical ? [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd] : base.GetAllowedSizeFlagsVertical();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(FlowProperties.Where(property => !_fixed || property.Name != nameof(Vertical)));
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(FlowContainer) ? CreateFlow : base.CreateSceneInstanceFactory();
    private static Node CreateFlow() => new FlowContainer();
}

/// <summary>A wrapping flow with fixed horizontal primary orientation.</summary>
public class HFlowContainer : FlowContainer
{
    /// <summary>Creates a fixed horizontal wrapping flow.</summary>
    public HFlowContainer() : base(false) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(HFlowContainer) ? CreateFlow : base.CreateSceneInstanceFactory();
    private static Node CreateFlow() => new HFlowContainer();
}

/// <summary>A wrapping flow with fixed vertical primary orientation.</summary>
public class VFlowContainer : FlowContainer
{
    /// <summary>Creates a fixed vertical wrapping flow.</summary>
    public VFlowContainer() : base(true) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(VFlowContainer) ? CreateFlow : base.CreateSceneInstanceFactory();
    private static Node CreateFlow() => new VFlowContainer();
}
