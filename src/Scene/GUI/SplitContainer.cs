using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Arranges visible ordinary controls in resizable horizontal or vertical panels.</summary>
/// <remarks>Offsets are relative to weighted default split positions. Internal drag controls overlay panels;
/// their resources and custom child controls follow ordinary borrowed/scene ownership. Native accessibility
/// and editor drag-area highlighting retain their separate dependencies.</remarks>
public partial class SplitContainer : Container
{
    /// <summary>Controls the grabber icon and split-bar separation.</summary>
    public enum DraggerVisibility
    {
        /// <summary>Draws the icon according to autohide; its size can increase separation.</summary>
        Visible = 0,
        /// <summary>Hides the icon while retaining separation and pointer dragging.</summary>
        Hidden = 1,
        /// <summary>Hides the icon and uses zero separation, retaining a minimum pointer target.</summary>
        HiddenCollapsed = 2
    }
    private bool _vertical, _fixed, _collapsed, _touch, _nested, _enabled = true;
    private int _marginBegin, _marginEnd, _dragOffset, _generation;
    private DraggerVisibility _visibility;
    private bool _arranging, _again, _syncing, _ready, _offsetPending, _canPreserve;
    private readonly List<int> _offsets = [0], _defaults = [], _positions = [], _desired = [];
    private readonly List<Control> _children = [], _nextChildren = [];
    private readonly List<Dragger> _draggers = [];
    private readonly List<Slot> _slots = [];
    private struct Slot { internal Control Child; internal float Min, Max, Ratio, Final; internal bool Expand, Active, Priority; internal Rect2 Rect; }
    /// <summary>Creates a horizontal split with one retained internal drag area and zero offset.</summary>
    public SplitContainer() : this(false, false) { }
    /// <summary>Creates a fixed-orientation specialization.</summary>
    /// <param name="vertical">Whether panels stack vertically.</param>
    protected SplitContainer(bool vertical) : this(vertical, true) { }
    private SplitContainer(bool vertical, bool fixedOrientation)
    {
        _vertical = vertical; _fixed = fixedOrientation; _ready = true;
        EnsureDraggers();
    }
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void Changed(bool minimum = false, bool immediate = false)
    {
        _generation++; _again |= _arranging;
        if (minimum) UpdateMinimumSize();
        QueueSort();
        if (immediate) Arrange();
    }
    /// <summary>Gets or sets Vertical configuration.</summary>
    /// <value>False; fixed HSplit/VSplit assignments reject, including equal values.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner, capture-owned, fixed or callbacks fail.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public bool Vertical
    {
        get { Check(); return _vertical; }
        set { EnsureMutable(); if (_fixed) throw new InvalidOperationException("This split has a fixed orientation."); if (_vertical == value) return; _vertical = value; Changed(true, true); }
    }
    /// <summary>Gets or sets Collapsed configuration.</summary>
    /// <value>False; true uses zero visual offsets and hides/disables drag areas.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner, capture-owned, fixed or callbacks fail.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public bool Collapsed
    {
        get { Check(); return _collapsed; }
        set { EnsureMutable(); if (_collapsed == value) return; _collapsed = value; Changed(); }
    }
    /// <summary>Gets or sets DraggingEnabled configuration.</summary>
    /// <value>True; disabling ends active drags before updating pointer targets.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner, capture-owned, fixed or callbacks fail.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public bool DraggingEnabled
    {
        get { Check(); return _enabled; }
        set { EnsureMutable(); if (_enabled == value) return; _enabled = value; List<Exception>? errors = null; if (!value) try { StopDrags(); } catch (Exception error) { CollectException(ref errors, error); } try { Changed(false, true); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("Split dragging configuration failed.", errors); }
    }
    /// <summary>Gets or sets TouchDraggerEnabled configuration.</summary>
    /// <value>False; true adds overlapping TextureRect handles and suppresses ordinary grabber icons.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner, capture-owned, fixed or callbacks fail.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public bool TouchDraggerEnabled
    {
        get { Check(); return _touch; }
        set { EnsureMutable(); if (_touch == value) return; _touch = value; List<Exception>? errors = null; foreach (var dragger in _draggers) try { dragger.UpdateTouch(); } catch (Exception error) { CollectException(ref errors, error); } try { Changed(true); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("Split touch configuration failed.", errors); }
    }
    /// <summary>Gets or sets DragNestedIntersections configuration.</summary>
    /// <value>False; true enables orthogonal ancestor/descendant joint pointer handles.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner, capture-owned, fixed or callbacks fail.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public bool DragNestedIntersections
    {
        get { Check(); return _nested; }
        set { EnsureMutable(); if (_nested == value) return; _nested = value; Changed(); UpdateIntersections(); }
    }
    /// <summary>Gets or sets DragAreaMarginBegin configuration.</summary>
    /// <value>Zero; signed cross-axis inset at the beginning of the drag area.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner, capture-owned, fixed or callbacks fail.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public int DragAreaMarginBegin
    {
        get { Check(); return _marginBegin; }
        set { EnsureMutable(); if (_marginBegin == value) return; _marginBegin = value; Changed(); }
    }
    /// <summary>Gets or sets DragAreaMarginEnd configuration.</summary>
    /// <value>Zero; signed cross-axis inset at the end of the drag area.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner, capture-owned, fixed or callbacks fail.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public int DragAreaMarginEnd
    {
        get { Check(); return _marginEnd; }
        set { EnsureMutable(); if (_marginEnd == value) return; _marginEnd = value; Changed(); }
    }
    /// <summary>Gets or sets DragAreaOffset configuration.</summary>
    /// <value>Zero; signed displacement of the pointer target along the split axis.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner, capture-owned, fixed or callbacks fail.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public int DragAreaOffset
    {
        get { Check(); return _dragOffset; }
        set { EnsureMutable(); if (_dragOffset == value) return; _dragOffset = value; Changed(); }
    }
    /// <summary>Gets or sets DraggerVisibilityMode configuration.</summary>
    /// <value>Visible initially; hiding the icon does not disable dragging.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner, capture-owned, fixed or callbacks fail.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The enum value is undefined.</exception>
    public DraggerVisibility DraggerVisibilityMode
    {
        get { Check(); return _visibility; }
        set { EnsureMutable(); if (value is < DraggerVisibility.Visible or > DraggerVisibility.HiddenCollapsed) throw new ArgumentOutOfRangeException(nameof(value)); if (_visibility == value) return; _visibility = value; Changed(true); }
    }
    /// <summary>Gets or sets the first relative split offset.</summary>
    /// <value>Zero initially. This is the scalar projection of SplitOffsets[0].</value>
    /// <exception cref="ArgumentOutOfRangeException">The offset array is empty.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public int SplitOffset { get { Check(); return Offset(0); } set { EnsureMutable(); SetOffset(0, value); } }
    /// <summary>Gets or sets copied relative offsets for every split.</summary>
    /// <value>A caller-owned array, initially [0]. Layout grows a short array; longer pending arrays are retained.</value>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public int[] SplitOffsets
    {
        get { Check(); return _offsets.ToArray(); }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value);
            if (_offsets.Count == value.Length && CollectionsMarshal.AsSpan(_offsets).SequenceEqual(value)) return;
            _offsets.Clear(); _offsets.AddRange(value); _offsetPending = value.Length > 1 && _children.Count - 1 != value.Length; Changed();
        }
    }
    private int Offset(int index) { if ((uint)index >= (uint)_offsets.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _offsets[index]; }
    private void SetOffset(int index, int value) { if (Offset(index) == value) return; _offsets[index] = value; Changed(); }
    /// <summary>Clamps all stored offsets to current bounds and prioritizes one split when resolving overlaps.</summary>
    /// <param name="priorityIndex">The split that pushes conflicting neighbours; zero initially.</param>
    /// <remarks>Fewer than two panels is a no-op after validating the stored index. Does not emit drag events.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the offset or active split array.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/capture-owned or geometry is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The split is disposed.</exception>
    /// <exception cref="Exception">User callbacks fail after configuration commits.</exception>
    public void ClampSplitOffset(int priorityIndex = 0)
    {
        EnsureMutable(); _ = Offset(priorityIndex); Synchronize(); if (_children.Count < 2) return;
        PrepareSlots(); DefaultPositions(); Positions(priorityIndex); Changed();
    }
    /// <summary>Returns the first borrowed internal drag area.</summary>
    /// <returns>The same control as the first element of GetDragAreaControls.</returns>
    /// <remarks>Do not remove/dispose required drag areas; custom children may be added through ordinary Node API.</remarks>
    /// <exception cref="InvalidOperationException">Access is off-owner or a required internal area was removed.</exception>
    /// <exception cref="ObjectDisposedException">The split or drag area is disposed.</exception>
    public Control GetDragAreaControl() { Check(); ValidateDraggers(); return _draggers[0]; }
    /// <summary>Returns a snapshot of the borrowed internal drag areas, in split order.</summary>
    /// <returns>A caller-owned array. At least one drag area is retained, including while there are no panels.</returns>
    /// <remarks>Required internal controls must not be removed/disposed. They overlay panels and can host custom children.</remarks>
    /// <exception cref="InvalidOperationException">Access is off-owner or required areas were removed.</exception>
    /// <exception cref="ObjectDisposedException">The split or required area is disposed.</exception>
    public Control[] GetDragAreaControls() { Check(); ValidateDraggers(); return [.. _draggers]; }
    /// <summary>Occurs after pointer dragging starts; offsets are clamped to visual positions first.</summary>
    public event Action? DragStarted;
    /// <summary>Occurs after pointer motion commits and clamps the active offset.</summary>
    /// <remarks>The argument is the relative offset of the dragger that moved, not its index.</remarks>
    public event Action<int>? Dragged;
    /// <summary>Occurs once when a started pointer drag ends or is cancelled.</summary>
    public event Action? DragEnded;
    private void StopDrags()
    {
        List<Exception>? errors = null;
        for (var i = _draggers.Count - 1; i >= 0; i--)
        {
            if (i >= _draggers.Count) continue;
            try { _draggers[i].Stop(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Split drag cancellation callbacks failed.", errors);
    }
    private Texture Grabber => GetThemeIcon(_fixed ? "grabber" : _vertical ? "v_grabber" : "h_grabber") ?? throw new InvalidOperationException("A split grabber icon is required.");
    private Texture TouchIcon => GetThemeIcon(_fixed ? "touch_dragger" : _vertical ? "v_touch_dragger" : "h_touch_dragger") ?? throw new InvalidOperationException("A split touch icon is required.");
    private int Separation => _visibility == DraggerVisibility.HiddenCollapsed ? 0 : _touch ? GetThemeConstant("separation") : Math.Max(GetThemeConstant("separation"), _vertical ? Grabber.GetHeight() : Grabber.GetWidth());
    private static int Pixel(float value)
    {
        if (!float.IsFinite(value) || (double)value > int.MaxValue || (double)value < int.MinValue) throw new InvalidOperationException("Split geometry exceeds integer pixel range.");
        return (int)value;
    }
    private int AxisSize => Pixel(_vertical ? Size.Y : Size.X);
    private int Axis(Vector2 value) => Pixel(_vertical ? value.Y : value.X);
    private Rect2 AxisRect(float begin, float main, float cross) => _vertical ? new(0, begin, cross, main) : new(begin, 0, main, cross);
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() => Minimum(false);
    internal override Vector2 GetDesiredSize() => Minimum(true);
    private Vector2 Minimum(bool desired)
    {
        var main = 0; var cross = 0; var count = 0;
        for (var i = 0; i < GetChildCount(); i++)
            if (GetChild(i) is Control child && Sortable(child, true))
            {
                var size = desired ? child.GetBoundDesiredSize() : child.GetBoundMinimumSize(); main = checked(main + Axis(size)); cross = Math.Max(cross, Pixel(_vertical ? size.X : size.Y)); count++;
            }
        main = checked(main + Separation * Math.Max(0, count - 1));
        return _vertical ? new(cross, main) : new(main, cross);
    }
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsHorizontal() => _vertical ? [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd] : base.GetAllowedSizeFlagsHorizontal();
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsVertical() => !_vertical ? [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd] : base.GetAllowedSizeFlagsVertical();
    private static readonly PropertyDescriptor[] SplitProperties =
    [
        new PropertyDescriptor<SplitContainer, bool>(nameof(Vertical), c => c.Vertical, (c, v) => c.Vertical = v, _ => false, stored: true),
        new PropertyDescriptor<SplitContainer, bool>(nameof(Collapsed), c => c.Collapsed, (c, v) => c.Collapsed = v, _ => false, stored: true),
        new PropertyDescriptor<SplitContainer, bool>(nameof(DraggingEnabled), c => c.DraggingEnabled, (c, v) => c.DraggingEnabled = v, _ => true, stored: true),
        new PropertyDescriptor<SplitContainer, bool>(nameof(TouchDraggerEnabled), c => c.TouchDraggerEnabled, (c, v) => c.TouchDraggerEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<SplitContainer, bool>(nameof(DragNestedIntersections), c => c.DragNestedIntersections, (c, v) => c.DragNestedIntersections = v, _ => false, stored: true),
        new PropertyDescriptor<SplitContainer, int>(nameof(DragAreaMarginBegin), c => c.DragAreaMarginBegin, (c, v) => c.DragAreaMarginBegin = v, _ => 0, stored: true),
        new PropertyDescriptor<SplitContainer, int>(nameof(DragAreaMarginEnd), c => c.DragAreaMarginEnd, (c, v) => c.DragAreaMarginEnd = v, _ => 0, stored: true),
        new PropertyDescriptor<SplitContainer, int>(nameof(DragAreaOffset), c => c.DragAreaOffset, (c, v) => c.DragAreaOffset = v, _ => 0, stored: true),
        new PropertyDescriptor<SplitContainer, DraggerVisibility>(nameof(DraggerVisibilityMode), c => c.DraggerVisibilityMode, (c, v) => c.DraggerVisibilityMode = v, _ => DraggerVisibility.Visible, stored: true),
        new PropertyDescriptor<SplitContainer, int[]>(nameof(SplitOffsets), c => c.SplitOffsets, (c, v) => c.SplitOffsets = v, _ => [0], stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SplitProperties.Where(p => !_fixed || p.Name != nameof(Vertical)));
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(SplitContainer) ? CreateSplit : base.CreateSceneInstanceFactory();
    private static Node CreateSplit() => new SplitContainer();
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null;
        try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (_ready && !IsDisposed) try
            {
                if (what == NotificationChildOrderChanged) Synchronize();
                else if (what == NotificationSortChildren) Arrange();
                else if (what is NotificationTranslationChanged or NotificationLayoutDirectionChanged) Changed();
                else if (what == NotificationThemeChanged) { UpdateMinimumSize(); foreach (var dragger in _draggers) { dragger.UpdateTouch(); dragger.QueueRedraw(); } }
                else if (what == NotificationEnterTree) { _leaving = false; SetInternalProcessing(true, false); }
                else if (what == NotificationExitTree)
                {
                    _leaving = true;
                    try { StopDrags(); } catch (Exception error) { CollectException(ref errors, error); }
                    try { ClearIntersections(); } catch (Exception error) { CollectException(ref errors, error); }
                    ReleaseResidency(); SetInternalProcessing(false, false);
                }
                else if (what == NotificationInternalProcess) { PollResources(); UpdateIntersections(); }
                else if (what == NotificationVisibilityChanged && !IsVisibleInTree) StopDrags();
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Split notification callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null;
        if (disposing) try { _ready = false; _leaving = true; StopDrags(); ClearIntersections(); } catch (Exception error) { CollectException(ref errors, error); }
        foreach (var child in _children) child.VisibilityChanged -= ChildVisibility;
        try { base.Dispose(disposing); }
        catch (Exception error) { CollectException(ref errors, error); }
        finally { _children.Clear(); _nextChildren.Clear(); _draggers.Clear(); _slots.Clear(); _intersections.Clear(); _wanted.Clear(); DragStarted = null; Dragged = null; DragEnded = null; ReleaseResidency(); }
        ThrowCollected("Split disposal callbacks failed.", errors);
    }
}

/// <summary>Arranges resizable panels with fixed horizontal orientation.</summary>
public class HSplitContainer : SplitContainer
{
    /// <summary>Creates a fixed horizontal split.</summary>
    public HSplitContainer() : base(false) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(HSplitContainer) ? CreateSplit : base.CreateSceneInstanceFactory();
    private static Node CreateSplit() => new HSplitContainer();
}

/// <summary>Arranges resizable panels with fixed vertical orientation.</summary>
public class VSplitContainer : SplitContainer
{
    /// <summary>Creates a fixed vertical split.</summary>
    public VSplitContainer() : base(true) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(VSplitContainer) ? CreateSplit : base.CreateSceneInstanceFactory();
    private static Node CreateSplit() => new VSplitContainer();
}
