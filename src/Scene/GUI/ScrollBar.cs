namespace Electron2D;

/// <summary>Base for themed horizontal and vertical controls that scroll an inherited numeric range.</summary>
/// <remarks>Buttons and arrow keys use CustomStep when nonnegative, otherwise Step. Wheel, page clicks and gestures
/// have their own increments. Pointer and highlight state is transient. Theme resources are borrowed; attached bars
/// retain their declared state textures and poll revisions so a missed resource callback cannot leave stale geometry.</remarks>
public abstract class ScrollBar : Range
{
    internal const int PageDivisor = 8;
    private enum Highlight { None, Decrement, Range, Increment }
    private readonly bool _vertical;
    private float _customStep = -1, _dragPosition, _dragRatio;
    private bool _decrementActive, _incrementActive, _dragActive, _attached, _minimumDirty;
    private Highlight _highlight;
    private ulong _interaction;
    private readonly record struct ResourceState(Resource Resource, long Revision, bool Disposed);
    private List<ResourceState> _states = [], _nextStates = [];
    private readonly List<Texture> _resident = [];
    private static readonly string[] Styles = ["scroll", "scroll_focus", "grabber", "grabber_highlight", "grabber_pressed"];
    private static readonly string[] Icons = ["decrement", "decrement_highlight", "decrement_pressed", "increment", "increment_highlight", "increment_pressed"];
    private static readonly PropertyDescriptor[] ScrollProperties =
    [
        new PropertyDescriptor<ScrollBar, float>(nameof(CustomStep), b=>b.CustomStep, (b,v)=>b.CustomStep=v, _=>-1, stored:true),
        new PropertyDescriptor<ScrollBar, double>(nameof(Step), b=>b.Step, (b,v)=>b.Step=v, _=>0, stored:true),
        new PropertyDescriptor<ScrollBar, FocusMode>(nameof(FocusMode), b=>b.FocusMode, (b,v)=>b.FocusMode=v, _=>FocusMode.Accessibility, stored:true),
        new PropertyDescriptor<ScrollBar, SizeFlags>(nameof(SizeFlagsHorizontal), b=>b.SizeFlagsHorizontal, (b,v)=>b.SizeFlagsHorizontal=v, b=>b._vertical?SizeFlags.ShrinkBegin:SizeFlags.Fill, stored:true),
        new PropertyDescriptor<ScrollBar, SizeFlags>(nameof(SizeFlagsVertical), b=>b.SizeFlagsVertical, (b,v)=>b.SizeFlagsVertical=v, b=>b._vertical?SizeFlags.Fill:SizeFlags.ShrinkBegin, stored:true),
        new PropertyDescriptor<ScrollBar, double>(nameof(Value), b=>b.Value, (b,v)=>b.Value=v, _=>0, stored:true)
    ];
    /// <summary>Creates a fixed-orientation scroll bar with continuous range values and accessibility focus.</summary>
    /// <param name="vertical">True for top-to-bottom scrolling; false for left-to-right scrolling.</param>
    protected ScrollBar(bool vertical = true)
    {
        _vertical = vertical; Step = 0; FocusMode = FocusMode.Accessibility;
        SizeFlagsHorizontal = vertical ? SizeFlags.ShrinkBegin : SizeFlags.Fill;
        SizeFlagsVertical = vertical ? SizeFlags.Fill : SizeFlags.ShrinkBegin;
    }
    /// <summary>Gets or sets the amount used by increment/decrement buttons and orientation-matched arrow keys.</summary>
    /// <value>Minus one initially. Nonnegative values override Step; negative values and NaN use Step.</value>
    /// <remarks>Raw float values, including infinity, are retained. Assignment is silent and does not resnap the range.
    /// Repeated key events repeat input; holding a pointer button does not invent a timed repeat.</remarks>
    /// <exception cref="InvalidOperationException">Access is off the scene owner thread or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public float CustomStep
    {
        get { CheckScroll(); return _customStep; }
        set { EnsureMutable(); _customStep = value; }
    }
    /// <summary>Occurs after a scrolling operation changes the final value by more than the approximate-equality tolerance.</summary>
    /// <remarks>Inherited ValueChanged runs first. Programmatic Value assignment does not emit Scrolling.
    /// Callback errors do not suppress the remaining current phases; they are aggregated after committed state.</remarks>
    public event Action? Scrolling;
    private void CheckScroll() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private bool Current(ulong generation, SceneTree? tree) => !IsDisposed && generation == _interaction && ReferenceEquals(Tree, tree);
    internal void Scroll(double amount) => ScrollTo(Value + amount);
    internal void ScrollTo(double position)
    {
        EnsureMutable(); var generation = ++_interaction; var tree = Tree; List<Exception>? errors = null;
        Apply(position, false, generation, tree, ref errors); ThrowCollected("Scroll bar callbacks failed.", errors);
    }
    private void Apply(double position, bool ratio, ulong generation, SceneTree? tree, ref List<Exception>? errors)
    {
        var previous = Value;
        try { if (ratio) Ratio = position; else Value = position; } catch (Exception e) { CollectException(ref errors, e); }
        if (Current(generation, tree) && !Mathf.IsEqualApprox(previous, Value))
            try { Scrolling?.Invoke(); } catch (Exception e) { CollectException(ref errors, e); }
    }
    private double ButtonStep => _customStep >= 0 ? _customStep : Step;
    private double Axis(Vector2 value) => _vertical ? value.Y : value.X;
    private StyleBox Style(string name) => GetThemeStyleBox(name) ?? throw new InvalidOperationException($"Scroll bar requires the '{name}' style.");
    private Texture Icon(string name) => GetThemeIcon(name) ?? throw new InvalidOperationException($"Scroll bar requires the '{name}' icon.");
    private double GrabberMinimum() => Axis(Style("grabber").GetMinimumSize());
    private double AreaSize() => Axis(Size) - Axis(Style("scroll").GetMinimumSize()) - Axis(Icon("decrement").GetSize()) - Axis(Icon("increment").GetSize()) - GrabberMinimum();
    private double GrabberSize()
    {
        var range = (float)(MaxValue - MinValue); if (range <= 0) return 0;
        var page = (float)(Page > 0 ? Page : 0); var fraction = page / range;
        // A range outside float precision can produce infinity/infinity; keep recorded geometry finite.
        return (float.IsNaN(fraction) ? 0 : fraction) * AreaSize() + GrabberMinimum();
    }
    private double GrabberOffset() { var ratio = Ratio; return AreaSize() * (double.IsNaN(ratio) ? 0 : ratio); }
    private double LeadingMargin() => Style("scroll").GetMargin(_vertical ? Side.Top : Side.Left);
    private int LeadingPadding => Math.Max(GetThemeConstant(_vertical ? "padding_left" : "padding_top"), 0);
    private int TrailingPadding => Math.Max(GetThemeConstant(_vertical ? "padding_right" : "padding_bottom"), 0);
    private static float FiniteFloat(double value)
    {
        if (!double.IsFinite(value) || value is > float.MaxValue or < -float.MaxValue)
            throw new InvalidOperationException("Scroll bar geometry exceeds finite canvas coordinates.");
        return (float)value;
    }
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Required theme items are missing or minimum geometry is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">A borrowed theme resource is disposed.</exception>
    protected override Vector2 OnGetMinimumSize()
    {
        var increment = Icon("increment").GetSize(); var decrement = Icon("decrement").GetSize(); var track = Style("scroll").GetMinimumSize();
        var primary = Axis(increment) + Axis(decrement) + Axis(track) + GrabberMinimum();
        var cross = Math.Max(_vertical ? increment.X : increment.Y, _vertical ? track.X : track.Y) + (double)LeadingPadding + TrailingPadding;
        return _vertical ? new(FiniteFloat(cross), FiniteFloat(primary)) : new(FiniteFloat(primary), FiniteFloat(cross));
    }
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">The input event is null.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable, input acceptance has no scene dispatch, or a required theme resource is missing.</exception>
    /// <exception cref="ObjectDisposedException">The control, input event or a borrowed theme resource is disposed.</exception>
    /// <exception cref="AggregateException">A scrolling callback fails after the committed change.</exception>
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(inputEvent); ObjectDisposedException.ThrowIf(inputEvent.IsDisposed, inputEvent);
        var generation = ++_interaction; var tree = Tree; List<Exception>? errors = null;
        if (inputEvent is InputEventPanGesture pan)
        {
            AcceptEvent(); var delta = _vertical ? pan.Delta.Y : pan.Delta.X != 0 ? pan.Delta.X : pan.Delta.Y;
            if (delta != 0) Apply(Value + (delta < 0 ? -1 : 1) * Math.Max(Math.Abs(delta), Step), false, generation, tree, ref errors);
        }
        if (inputEvent is InputEventMouseButton button)
        {
            AcceptEvent();
            if (button.Pressed && button.ButtonIndex is MouseButton.WheelDown or MouseButton.WheelUp)
            {
                var amount = (Page != 0 ? Page / PageDivisor : (MaxValue - MinValue) / 16) * button.Factor;
                Apply(Value + (button.ButtonIndex == MouseButton.WheelUp ? -1 : 1) * Math.Max(amount, Step), false, generation, tree, ref errors);
            }
            if (button.ButtonIndex != MouseButton.Left) { ThrowCollected("Scroll bar wheel callbacks failed.", errors); return; }
            if (button.Pressed)
            {
                var offset = Axis(button.Position); var decrement = Axis(Icon("decrement").GetSize()); var increment = Axis(Icon("increment").GetSize());
                var grabberOffset = GrabberOffset(); var grabberSize = GrabberSize(); var total = Axis(Size);
                if (offset < decrement) { _decrementActive = true; Apply(Value - ButtonStep, false, generation, tree, ref errors); }
                else if (offset > total - increment) { _incrementActive = true; Apply(Value + ButtonStep, false, generation, tree, ref errors); }
                else
                {
                    offset -= decrement + LeadingMargin();
                    if (offset < grabberOffset) Apply(Math.Clamp(Value - (Page != 0 ? Page : (MaxValue - MinValue) / 16), MinValue, MaxValue - Page), false, generation, tree, ref errors);
                    else if (offset - grabberOffset < grabberSize) { _dragActive = true; _dragPosition = (float)offset; _dragRatio = (float)Ratio; }
                    else Apply(Math.Clamp(Value + (Page != 0 ? Page : (MaxValue - MinValue) / 16), MinValue, MaxValue - Page), false, generation, tree, ref errors);
                }
            }
            else { _decrementActive = false; _incrementActive = false; _dragActive = false; }
            if (Current(generation, tree)) QueueRedraw();
        }
        if (inputEvent is InputEventMouseMotion motion)
        {
            AcceptEvent();
            if (_dragActive)
            {
                var offset = Axis(motion.Position) - Axis(Icon("decrement").GetSize()) - LeadingMargin();
                var area = AreaSize();
                if (area > 0 && double.IsFinite(area)) Apply(_dragRatio + (offset - _dragPosition) / area, true, generation, tree, ref errors);
            }
            else
            {
                var offset = Axis(motion.Position); var next = offset < Axis(Icon("decrement").GetSize()) ? Highlight.Decrement :
                    offset > Axis(Size) - Axis(Icon("increment").GetSize()) ? Highlight.Increment : Highlight.Range;
                if (next != _highlight) { _highlight = next; QueueRedraw(); }
            }
        }
        if (Current(generation, tree) && inputEvent.IsPressed())
        {
            if (inputEvent.IsAction("ui_left", true)) { if (!_vertical) Apply(Value - ButtonStep, false, generation, tree, ref errors); }
            else if (inputEvent.IsAction("ui_right", true)) { if (!_vertical) Apply(Value + ButtonStep, false, generation, tree, ref errors); }
            else if (inputEvent.IsAction("ui_up", true)) { if (_vertical) Apply(Value - ButtonStep, false, generation, tree, ref errors); }
            else if (inputEvent.IsAction("ui_down", true)) { if (_vertical) Apply(Value + ButtonStep, false, generation, tree, ref errors); }
            else if (inputEvent.IsAction("ui_home", true)) Apply(MinValue, false, generation, tree, ref errors);
            else if (inputEvent.IsAction("ui_end", true)) Apply(MaxValue, false, generation, tree, ref errors);
        }
        ThrowCollected("Scroll bar input callbacks failed.", errors);
    }
    private void DrawScrollBar()
    {
        PollResources();
        var decrement = Icon(_decrementActive ? "decrement_pressed" : _highlight == Highlight.Decrement ? "decrement_highlight" : "decrement");
        var increment = Icon(_incrementActive ? "increment_pressed" : _highlight == Highlight.Increment ? "increment_highlight" : "increment");
        var grabber = Style(_dragActive ? "grabber_pressed" : _highlight == Highlight.Range ? "grabber_highlight" : "grabber");
        var leading = FiniteFloat(Axis(decrement.GetSize())); var primary = FiniteFloat(Axis(Size) - leading - Axis(increment.GetSize()));
        var offset = _vertical ? new Vector2(0, leading) : new Vector2(leading, 0); var area = _vertical ? new Vector2(Size.X, primary) : new Vector2(primary, Size.Y);
        var thumbLength = FiniteFloat(GrabberSize()); var thumbOffset = FiniteFloat(GrabberOffset() + Axis(Icon("decrement").GetSize()) + LeadingMargin());
        var before = LeadingPadding; var cross = FiniteFloat((_vertical ? Size.X : Size.Y) - (double)before - TrailingPadding);
        var thumb = _vertical ? new Rect2(before, thumbOffset, cross, thumbLength) : new Rect2(thumbOffset, before, thumbLength, cross);
        // Drawing uses selected-state icon dimensions; grabber metrics remain based on normal resources.
        thumb.Position = _vertical ? new(before, FiniteFloat(GrabberOffset() + leading + LeadingMargin())) : new(FiniteFloat(GrabberOffset() + leading + LeadingMargin()), before);
        DrawTexture(decrement, Vector2.Zero); DrawStyleBox(Style(HasFocus(true) ? "scroll_focus" : "scroll"), new(offset, area));
        DrawTexture(increment, offset + (_vertical ? new Vector2(0, primary) : new Vector2(primary, 0))); DrawStyleBox(grabber, thumb);
    }
    private void CancelInteraction() { _interaction++; _dragActive = false; _decrementActive = false; _incrementActive = false; }
    private void PollResources()
    {
        _nextStates.Clear();
        try
        {
            foreach (var name in Styles)
            {
                var style = GetThemeStyleBox(name); AddResource(style);
                if (style is StyleBoxTexture { IsDisposed: false } textured) AddResource(textured.Texture);
            }
            foreach (var name in Icons) AddResource(GetThemeIcon(name));
            var changed = _states.Count != _nextStates.Count;
            if (!changed) for (var i = 0; i < _states.Count; i++) if (_states[i] != _nextStates[i]) { changed = true; break; }
            (_states, _nextStates) = (_nextStates, _states); _nextStates.Clear(); SyncResidency();
            if (changed) { _minimumDirty = true; InvalidateCanvas(); }
        }
        catch { ReleaseResidency(); throw; }
    }
    private void AddResource(Resource? resource)
    {
        while (resource is not null)
        {
            foreach (var state in _nextStates) if (ReferenceEquals(state.Resource, resource)) return;
            var disposed = resource.IsDisposed; _nextStates.Add(new(resource, resource.ChangeRevision, disposed));
            if (disposed || resource is not AtlasTexture atlas) return;
            resource = atlas.Atlas;
        }
    }
    private void SyncResidency()
    {
        if (!_attached) { ReleaseResidency(); return; }
        for (var i = _resident.Count - 1; i >= 0; i--)
        {
            var retained = false; foreach (var state in _states) if (ReferenceEquals(state.Resource, _resident[i]) && !state.Disposed) { retained = true; break; }
            if (retained) continue; _resident[i].ReleaseRendererCacheResidency(); _resident.RemoveAt(i);
        }
        foreach (var state in _states)
            if (!state.Disposed && state.Resource is Texture texture && !_resident.Contains(texture)) { texture.AcquireRendererCacheResidency(); _resident.Add(texture); }
    }
    private void ReleaseResidency() { foreach (var texture in _resident) texture.ReleaseRendererCacheResidency(); _resident.Clear(); }
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Required theme geometry or resources are invalid.</exception>
    /// <exception cref="AggregateException">Inherited notifications or required lifecycle phases fail.</exception>
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { base.OnNotification(what); DrawScrollBar(); return; }
        List<Exception>? errors = null; try { base.OnNotification(what); } catch (Exception e) { CollectException(ref errors, e); }
        if (!IsDisposed) try
            {
                switch (what)
                {
                    case NotificationEnterTree: PollResources(); SetInternalProcessing(true, false); break;
                    case NotificationExitTree: CancelInteraction(); _highlight = Highlight.None; ReleaseResidency(); _states.Clear(); SetInternalProcessing(false, false); break;
                    case NotificationThemeChanged: PollResources(); break;
                    case NotificationInternalProcess:
                        PollResources(); if (_minimumDirty) { _minimumDirty = false; UpdateMinimumSize(); QueueRedraw(); }
                        break;
                    case NotificationVisibilityChanged: if (!IsVisibleInTree) CancelInteraction(); break;
                    case NotificationPaused:
                    case NotificationDisabled: if (!CanProcess()) CancelInteraction(); break;
                    case NotificationMouseExit: _highlight = Highlight.None; QueueRedraw(); break;
                    case NotificationFocusEnter:
                    case NotificationFocusExit: QueueRedraw(); break;
                }
            }
            catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Scroll bar notification callbacks failed.", errors);
    }
    internal override void OnTreeMembershipChanged(bool entering)
    {
        _attached = entering; CancelInteraction(); if (!entering) ReleaseResidency(); base.OnTreeMembershipChanged(entering);
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(p => p.Name != nameof(Step) && p.Name != nameof(FocusMode) && p.Name != nameof(SizeFlagsHorizontal) && p.Name != nameof(SizeFlagsVertical) && p.Name != nameof(Value)).Concat(ScrollProperties);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _attached = false; CancelInteraction(); ReleaseResidency(); _states.Clear(); _nextStates.Clear();
        try { base.Dispose(disposing); } finally { Scrolling = null; }
    }
}

/// <summary>A horizontal scroll bar whose value grows from left to right independently of layout direction.</summary>
public class HScrollBar : ScrollBar
{
    /// <summary>Creates a horizontal bar with horizontal Fill and vertical ShrinkBegin sizing.</summary>
    public HScrollBar() : base(false) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(HScrollBar) ? CreateBar : base.CreateSceneInstanceFactory();
    private static Node CreateBar() => new HScrollBar();
}
/// <summary>A vertical scroll bar whose value grows from top to bottom.</summary>
public class VScrollBar : ScrollBar
{
    /// <summary>Creates a vertical bar with horizontal ShrinkBegin and vertical Fill sizing.</summary>
    public VScrollBar() : base(true) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(VScrollBar) ? CreateBar : base.CreateSceneInstanceFactory();
    private static Node CreateBar() => new VScrollBar();
}
