namespace Electron2D;

/// <summary>Arranges visible direct child controls along one pixel-aligned axis.</summary>
/// <remarks>Weighted expansion is constrained by child min/max bounds; residual space follows alignment.
/// Horizontal layout honors RTL. Inherited theme constants and local overrides control the gap.</remarks>
public class BoxContainer : Container
{
    private bool _vertical, _fixed, _arranging, _arrangeAgain;
    private AlignmentMode _alignment;
    private readonly List<Slot> _slots = [];
    private readonly record struct Slot(Control Child, int Minimum, int Maximum, float Ratio, int Final, bool Expand);
    private static readonly PropertyDescriptor[] BoxProperties =
    [
        new PropertyDescriptor<BoxContainer, bool>(nameof(Vertical), node => node.Vertical, (node, value) => node.Vertical = value, _ => false, stored: true),
        new PropertyDescriptor<BoxContainer, AlignmentMode>(nameof(Alignment), node => node.Alignment, (node, value) => node.Alignment = value, _ => AlignmentMode.Begin, stored: true),
        new PropertyDescriptor<BoxContainer, int>(nameof(Separation), node => node.Separation, (node, value) => node.Separation = value, node => node.InheritedThemeConstant("separation"))
    ];
    /// <summary>Creates a horizontal box aligned to its leading edge with separation four.</summary>
    public BoxContainer() { }
    /// <summary>Initializes a fixed-orientation box specialization.</summary>
    /// <param name="vertical">True for vertical orientation.</param>
    protected BoxContainer(bool vertical) { _vertical = vertical; _fixed = true; }
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    /// <summary>Gets or sets the group's leading/center/trailing alignment.</summary>
    /// <value>Begin initially; changes arrange children synchronously while attached.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned; layout callbacks do not settle or exceed pixel range.</exception>
    /// <exception cref="ObjectDisposedException">The box is disposed.</exception>
    public AlignmentMode Alignment
    {
        get { Check(); return _alignment; }
        set { EnsureMutable(); if (value is < AlignmentMode.Begin or > AlignmentMode.End) throw new ArgumentOutOfRangeException(nameof(value)); if (_alignment == value) return; _alignment = value; Arrange(); }
    }
    /// <summary>Gets or sets the primary layout orientation.</summary>
    /// <value>False for a generic box. Fixed HBox/VBox assignments reject, including equal values.</value>
    /// <exception cref="InvalidOperationException">The orientation is fixed, mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The box is disposed.</exception>
    public bool Vertical
    {
        get { Check(); return _vertical; }
        set { EnsureMutable(); if (_fixed) throw new InvalidOperationException("This box has a fixed orientation."); _vertical = value; UpdateMinimumSize(); Arrange(); }
    }
    /// <summary>Gets or sets the local signed pixel separation between child allocations.</summary>
    /// <value>Four in the built-in theme; assignments create a local typed theme override.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned; layout callbacks do not settle or exceed pixel range.</exception>
    /// <exception cref="ObjectDisposedException">The box is disposed.</exception>
    public int Separation
    {
        get => GetThemeConstant("separation");
        set { EnsureMutable(); if (Separation == value) return; if (value == InheritedThemeConstant("separation")) RemoveThemeConstantOverride("separation"); else AddThemeConstantOverride("separation", value); UpdateMinimumSize(); QueueSort(); }
    }
    /// <summary>Adds a real expanding Control spacer at the beginning or end.</summary>
    /// <param name="begin">Whether to insert before the current children.</param>
    /// <returns>The new child, owned by this scene hierarchy; it uses pointer Pass and primary ExpandFill.</returns>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The box is disposed.</exception>
    /// <exception cref="AggregateException">Structural or lifecycle callbacks fail after insertion.</exception>
    public Control AddSpacer(bool begin)
    {
        EnsureMutable(); var index = ChildCount;
        while (GetNodeOrNull($"Spacer{index}") is not null) index++;
        var spacer = new Control { Name = $"Spacer{index}", MouseFilter = MouseFilter.Pass };
        if (_vertical) spacer.SizeFlagsVertical = SizeFlags.ExpandFill; else spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(spacer); if (begin) MoveChild(spacer, 0); return spacer;
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        var minimum = Vector2.Zero; var count = 0;
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (Sortable(GetChild(index, includeInternal: true), true) && GetChild(index, includeInternal: true) is Control child)
            {
                var size = child.GetBoundMinimumSize().Ceil();
                if (_vertical) { minimum.X = MathF.Max(minimum.X, size.X); minimum.Y += size.Y + (count == 0 ? 0 : Separation); }
                else { minimum.Y = MathF.Max(minimum.Y, size.Y); minimum.X += size.X + (count == 0 ? 0 : Separation); }
                count++;
            }
        return minimum;
    }
    private void Arrange()
    {
        if (_arranging) { _arrangeAgain = true; return; }
        _arranging = true;
        try
        {
            for (var pass = 0; pass < 64; pass++)
            {
                _arrangeAgain = false;
                ArrangeOnce();
                if (!_arrangeAgain) return;
            }
            throw new InvalidOperationException("Box layout callbacks did not settle after 64 passes.");
        }
        finally { _arranging = false; _arrangeAgain = false; _slots.Clear(); }
    }
    private void ArrangeOnce()
    {
        if (!IsInsideTree || !IsVisibleInTree) return;
        _slots.Clear(); var stretchMinimum = 0; var stretchAvailable = 0; var ratioTotal = 0f;
        var size = Size.Floor(); var maximum = GetCombinedMaximumSize();
        var propagating = PropagateMaximumSize && (_vertical ? maximum.Y : maximum.X) >= 0;
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (Sortable(GetChild(index, includeInternal: true)) && GetChild(index, includeInternal: true) is Control child)
            {
                child.ContainerMaximum = propagating ? maximum : null;
                var minimum = child.GetBoundMinimumSize().Ceil(); var max = child.GetCombinedMaximumSize();
                var minAxis = Pixel(_vertical ? minimum.Y : minimum.X); var maxAxis = Pixel(_vertical ? max.Y : max.X);
                if (maxAxis >= 0 && maxAxis < minAxis) maxAxis = minAxis;
                var expand = ((_vertical ? child.SizeFlagsVertical : child.SizeFlagsHorizontal) & SizeFlags.Expand) != 0;
                _slots.Add(new(child, minAxis, maxAxis, child.SizeFlagsStretchRatio, minAxis, expand));
                stretchMinimum = checked(stretchMinimum + minAxis);
                if (expand) { stretchAvailable = checked(stretchAvailable + minAxis); ratioTotal += child.SizeFlagsStretchRatio; }
            }
        if (_slots.Count == 0) return;
        var stretchMaximum = checked(Pixel(_vertical ? size.Y : size.X) - checked((_slots.Count - 1) * Separation));
        stretchAvailable = checked(stretchAvailable + Math.Max(0, stretchMaximum - stretchMinimum));
        if (propagating) stretchAvailable = Math.Min(stretchAvailable, Pixel(_vertical ? maximum.Y : maximum.X));
        if (!float.IsFinite(ratioTotal)) throw new InvalidOperationException("Expansion weights exceed finite layout range.");
        // ponytail: Refit is quadratic in children; keep reusable slots and replace it with indexed allocation if large-GUI profiling requires it.
        while (ratioTotal > 0)
        {
            var refit = false; var error = 0f;
            for (var index = 0; index < _slots.Count; index++)
            {
                var slot = _slots[index]; if (!slot.Expand) continue;
                var pixels = stretchAvailable * slot.Ratio / ratioTotal; var final = Pixel(pixels); error += pixels - final;
                if (pixels < slot.Minimum || slot.Maximum >= 0 && pixels > slot.Maximum)
                {
                    final = pixels < slot.Minimum ? slot.Minimum : slot.Maximum;
                    _slots[index] = slot with { Final = final, Expand = false }; stretchAvailable -= final; ratioTotal -= slot.Ratio; refit = true; break;
                }
                if (error >= 1 && (slot.Maximum < 0 || final < slot.Maximum)) { final++; error--; }
                _slots[index] = slot with { Final = final };
            }
            if (!refit) break;
        }
        var remaining = stretchMaximum - stretchMinimum;
        foreach (var slot in _slots) remaining -= slot.Final - slot.Minimum;
        remaining = Math.Max(0, remaining); var rtl = IsLayoutRTL();
        var offset = _alignment == AlignmentMode.Center ? remaining / 2 : _alignment == AlignmentMode.End ? (_vertical || !rtl ? remaining : 0) : (!_vertical && rtl ? remaining : 0);
        var accumulated = 0; List<Exception>? errors = null;
        try
        {
            for (var sequence = 0; sequence < _slots.Count; sequence++)
            {
                var slot = _slots[!_vertical && rtl ? _slots.Count - 1 - sequence : sequence];
                if (sequence != 0) offset = checked(offset + Separation);
                var end = checked(offset + slot.Final);
                if (slot.Expand && sequence == _slots.Count - 1) end = Pixel(_vertical ? size.Y : size.X);
                var allocation = _vertical ? new Rect2(0, offset, size.X, end - offset) : new Rect2(offset, 0, end - offset, size.Y);
                if (propagating)
                    slot.Child.ContainerMaximum = _vertical ? new(maximum.X, MathF.Max(maximum.Y - accumulated, 0)) : new(MathF.Max(maximum.X - accumulated, 0), maximum.Y);
                if (!slot.Child.IsDisposed && ReferenceEquals(slot.Child.Parent, this))
                    try { FitChildInRect(slot.Child, allocation); } catch (Exception failure) { CollectException(ref errors, failure); }
                accumulated = checked(accumulated + end - offset + Separation); offset = end;
            }
        }
        finally { _slots.Clear(); }
        ThrowCollected("Box child layout callbacks failed.", errors);
    }
    private static int Pixel(float value)
    {
        if (!float.IsFinite(value) || (double)value > int.MaxValue || (double)value < int.MinValue) throw new InvalidOperationException("Layout exceeds integer pixel range.");
        return (int)value;
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationSortChildren) Arrange();
    }
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsHorizontal() => _vertical ? [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd] : base.GetAllowedSizeFlagsHorizontal();
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsVertical() => !_vertical ? [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd] : base.GetAllowedSizeFlagsVertical();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(BoxProperties.Where(property => !_fixed || property.Name != nameof(Vertical)));
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(BoxContainer) ? CreateBox : base.CreateSceneInstanceFactory();
    private static Node CreateBox() => new BoxContainer();
}

/// <summary>A box container with fixed horizontal orientation.</summary>
public class HBoxContainer : BoxContainer
{
    /// <summary>Creates a fixed horizontal box.</summary>
    public HBoxContainer() : base(false) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(HBoxContainer) ? CreateBox : base.CreateSceneInstanceFactory();
    private static Node CreateBox() => new HBoxContainer();
}

/// <summary>A box container with fixed vertical orientation.</summary>
public class VBoxContainer : BoxContainer
{
    /// <summary>Creates a fixed vertical box.</summary>
    public VBoxContainer() : base(true) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(VBoxContainer) ? CreateBox : base.CreateSceneInstanceFactory();
    private static Node CreateBox() => new VBoxContainer();
}
