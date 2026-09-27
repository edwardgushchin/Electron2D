namespace Electron2D;

/// <summary>Base for horizontal and vertical themed controls that edit a shared numeric range.</summary>
/// <remarks>Pointer dragging, wheel input, keyboard actions and controller repeat use the inherited range policy.
/// Rendering is pixel-aligned and consumes borrowed styles/icons from the current theme. Drag state, repeat timing
/// and hover state are transient. Inactive lifecycle transitions cancel interactions without inventing release events.</remarks>
public abstract class Slider : Range
{
    /// <summary>Specifies which side of the track receives tick marks.</summary>
    public enum TickPosition
    {
        /// <summary>Draws ticks below a horizontal track or right of a vertical track.</summary>
        BottomRight = 0,
        /// <summary>Draws mirrored ticks above a horizontal track or left of a vertical track.</summary>
        TopLeft = 1,
        /// <summary>Draws ticks on both sides.</summary>
        Both = 2,
        /// <summary>Centers ticks across the track.</summary>
        Center = 3
    }

    private const int MaximumTickCommands = 1_048_576;
    private readonly bool _vertical;
    private bool _editable = true, _scrollable = true, _ticksOnBorders, _mouseInside, _grabActive, _repeatEnabled;
    private int _tickCount, _grabPosition;
    private TickPosition _ticksPosition;
    private double _grabRatio, _ratioBeforeDrag;
    private float _repeatDelay = .5f;
    private ulong _interactionGeneration;
    private static readonly PropertyDescriptor[] SliderProperties =
    [
        new PropertyDescriptor<Slider, double>(nameof(Step), node => node.Step, (node, value) => node.Step = value, _ => 1, stored: true),
        new PropertyDescriptor<Slider, FocusMode>(nameof(FocusMode), node => node.FocusMode, (node, value) => node.FocusMode = value, _ => FocusMode.All, stored: true),
        new PropertyDescriptor<Slider, SizeFlags>(nameof(SizeFlagsHorizontal), node => node.SizeFlagsHorizontal, (node, value) => node.SizeFlagsHorizontal = value, node => node._vertical ? SizeFlags.ShrinkBegin : SizeFlags.Fill, stored: true),
        new PropertyDescriptor<Slider, SizeFlags>(nameof(SizeFlagsVertical), node => node.SizeFlagsVertical, (node, value) => node.SizeFlagsVertical = value, node => node._vertical ? SizeFlags.Fill : SizeFlags.ShrinkBegin, stored: true),
        new PropertyDescriptor<Slider, bool>(nameof(Editable), node => node.Editable, (node, value) => node.Editable = value, _ => true, stored: true),
        new PropertyDescriptor<Slider, bool>(nameof(Scrollable), node => node.Scrollable, (node, value) => node.Scrollable = value, _ => true, stored: true),
        new PropertyDescriptor<Slider, int>(nameof(TickCount), node => node.TickCount, (node, value) => node.TickCount = value, _ => 0, stored: true),
        new PropertyDescriptor<Slider, bool>(nameof(TicksOnBorders), node => node.TicksOnBorders, (node, value) => node.TicksOnBorders = value, _ => false, stored: true),
        new PropertyDescriptor<Slider, TickPosition>(nameof(TicksPosition), node => node.TicksPosition, (node, value) => node.TicksPosition = value, _ => TickPosition.BottomRight, stored: true),
        new PropertyDescriptor<Slider, double>(nameof(Value), node => node.Value, (node, value) => node.Value = value, _ => 0, stored: true)
    ];

    /// <summary>Initializes the fixed orientation, unit step, full focus and orientation-specific sizing defaults.</summary>
    /// <param name="vertical">True for a vertical control; false for a horizontal control.</param>
    protected Slider(bool vertical = true)
    {
        _vertical = vertical; Step = 1; FocusMode = FocusMode.All;
        SizeFlagsHorizontal = vertical ? SizeFlags.ShrinkBegin : SizeFlags.Fill;
        SizeFlagsVertical = vertical ? SizeFlags.Fill : SizeFlags.ShrinkBegin;
    }
    /// <summary>Occurs before a pointer press changes the value and activates its drag.</summary>
    /// <remarks>Callback failures still attempt the value/notification phases unless a callback invalidates this interaction.</remarks>
    public event Action? DragStarted;
    /// <summary>Occurs for an editable left-button release with whether the current ratio differs approximately from the press ratio.</summary>
    /// <remarks>A release can notify without an active grab. Lifecycle cancellation does not emit this event.</remarks>
    public event Action<bool>? DragEnded;
    private void CheckSlider() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }

    /// <summary>Gets or sets whether input can change the range.</summary>
    /// <value>True initially. A changed assignment cancels dragging; disabling also cancels controller repeat.</value>
    /// <exception cref="InvalidOperationException">Access is off the scene owner thread or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The slider is disposed.</exception>
    public bool Editable
    {
        get { CheckSlider(); return _editable; }
        set { EnsureMutable(); if (_editable == value) return; _editable = value; _grabActive = false; _interactionGeneration++; if (!value) StopRepeat(); QueueRedraw(); }
    }
    /// <summary>Gets or sets whether wheel up/down changes the value by one Step.</summary>
    /// <value>True initially. This policy does not affect pointer dragging or action input.</value>
    /// <exception cref="InvalidOperationException">Access is off the scene owner thread or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The slider is disposed.</exception>
    public bool Scrollable { get { CheckSlider(); return _scrollable; } set { EnsureMutable(); _scrollable = value; } }
    /// <summary>Gets or sets the stored tick count.</summary>
    /// <value>Zero initially. Signed values are retained; counts below two draw no ticks.</value>
    /// <remarks>Drawing rejects more than 1,048,576 actual tick commands before recording any slider decoration.</remarks>
    /// <exception cref="InvalidOperationException">Access is off the scene owner thread or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The slider is disposed.</exception>
    public int TickCount
    {
        get { CheckSlider(); return _tickCount; }
        set { EnsureMutable(); if (_tickCount == value) return; _tickCount = value; QueueRedraw(); }
    }
    /// <summary>Gets or sets whether the first and last ticks are drawn.</summary>
    /// <value>False initially.</value>
    /// <exception cref="InvalidOperationException">Access is off the scene owner thread or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The slider is disposed.</exception>
    public bool TicksOnBorders
    {
        get { CheckSlider(); return _ticksOnBorders; }
        set { EnsureMutable(); if (_ticksOnBorders == value) return; _ticksOnBorders = value; QueueRedraw(); }
    }
    /// <summary>Gets or sets tick placement relative to the track.</summary>
    /// <value>BottomRight initially. Unrecognized numeric enum values are retained and draw no ticks.</value>
    /// <exception cref="InvalidOperationException">Access is off the scene owner thread or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The slider is disposed.</exception>
    public TickPosition TicksPosition
    {
        get { CheckSlider(); return _ticksPosition; }
        set { EnsureMutable(); if (_ticksPosition == value) return; _ticksPosition = value; QueueRedraw(); }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The theme lacks its track or grabber, or the minimum exceeds finite integer pixel coordinates.</exception>
    /// <exception cref="ObjectDisposedException">A borrowed style or icon is disposed.</exception>
    protected override Vector2 OnGetMinimumSize()
    {
        var style = RequiredStyle("slider").GetMinimumSize(); var grabber = RequiredIcon("grabber").GetSize();
        var width = Pixel(style.X); var height = Pixel(style.Y);
        return _vertical ? new(Math.Max(width, Pixel(grabber.X)), height) : new(width, Math.Max(height, Pixel(grabber.Y)));
    }
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">The input event is null.</exception>
    /// <exception cref="ArgumentException">Pointer coordinates are nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner/capture-owned, a required theme item is missing, or coordinates exceed the integer pixel range.</exception>
    /// <exception cref="ObjectDisposedException">The slider, input event or a borrowed resource is disposed.</exception>
    /// <exception cref="AggregateException">Input callbacks or required value/acceptance phases fail after committed interaction state.</exception>
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(inputEvent);
        if (!_editable) return;
        if (inputEvent is InputEventMouseButton button)
        {
            if (button.ButtonIndex == MouseButton.Left)
            {
                if (button.Pressed) BeginDrag(button.Position);
                else EndDrag();
            }
            else if (_scrollable && button.Pressed && button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
                Wheel(button.ButtonIndex == MouseButton.WheelUp);
            return;
        }
        if (inputEvent is InputEventMouseMotion motion)
        {
            if (_grabActive)
            {
                if (!motion.Position.IsFinite()) throw new ArgumentException("Slider pointer coordinates must be finite.", nameof(inputEvent));
                var grabber = RequiredIcon("grabber_highlight"); var center = GetThemeConstant("center_grabber") != 0;
                var size = Pixel(_vertical ? Size.Y : Size.X);
                double travel = size - (center ? 0d : _vertical ? grabber.GetHeight() : grabber.GetWidth());
                if (travel <= 0) return;
                double offset = (_vertical ? motion.Position.Y : motion.Position.X) - _grabPosition;
                if (_vertical || IsLayoutRTL()) offset = -offset;
                Ratio = _grabRatio + offset / travel;
            }
            return;
        }
        string? action = null; var direction = 0;
        if (ActionPressed(inputEvent, "ui_left")) { if (_vertical) return; action = "ui_left"; direction = IsLayoutRTL() ? 1 : -1; }
        else if (ActionPressed(inputEvent, "ui_right")) { if (_vertical) return; action = "ui_right"; direction = IsLayoutRTL() ? -1 : 1; }
        else if (ActionPressed(inputEvent, "ui_up")) { if (!_vertical) return; action = "ui_up"; direction = 1; }
        else if (ActionPressed(inputEvent, "ui_down")) { if (!_vertical) return; action = "ui_down"; direction = -1; }
        if (action is not null)
        {
            if (inputEvent is InputEventJoypadMotion or InputEventJoypadButton)
            {
                if (!Input.Instance.IsActionJustPressedByEvent(action, inputEvent, exactMatch: true)) return;
                _repeatEnabled = true; SetInternalProcessing(true, false);
            }
            SetValueAndAccept(Value + direction * Step);
        }
        else if (ExactPressed(inputEvent, "ui_home")) SetValueAndAccept(MinValue);
        else if (ExactPressed(inputEvent, "ui_end")) SetValueAndAccept(MaxValue);
    }
    private static bool ActionPressed(InputEvent inputEvent, string action) => InputMap.Instance.HasAction(action) && inputEvent.IsActionPressed(action, allowEcho: true);
    private static bool ExactPressed(InputEvent inputEvent, string action) => InputMap.Instance.HasAction(action) && inputEvent.IsAction(action, exactMatch: true) && inputEvent.IsPressed();
    private static bool Held(string action) => InputMap.Instance.HasAction(action) && Input.Instance.IsActionPressed(action);
    private static bool Released(string action) => InputMap.Instance.HasAction(action) && Input.Instance.IsActionJustReleased(action);
    private void SetValueAndAccept(double value)
    {
        var tree = Tree; List<Exception>? errors = null;
        try { Value = value; } catch (Exception error) { CollectException(ref errors, error); }
        try { if (tree is { IsDisposed: false }) tree.SetInputAsHandled(); else if (!IsDisposed) AcceptEvent(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Slider action callbacks failed.", errors);
    }
    private bool InteractionCurrent(ulong generation, SceneTree? tree) => !IsDisposed && _editable && generation == _interactionGeneration && ReferenceEquals(Tree, tree);
    private void BeginDrag(Vector2 position)
    {
        if (!position.IsFinite()) throw new ArgumentException("Slider pointer coordinates must be finite.", nameof(position));
        var grabber = RequiredIcon(_mouseInside || HasFocus(true) ? "grabber_highlight" : "grabber");
        var pixel = Pixel(_vertical ? position.Y : position.X); var tree = Tree; var generation = ++_interactionGeneration;
        _grabPosition = pixel; _ratioBeforeDrag = Ratio; _grabActive = false; List<Exception>? errors = null;
        try { DragStarted?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        if (InteractionCurrent(generation, tree))
        {
            var ratioReady = false;
            try
            {
                var center = GetThemeConstant("center_grabber") != 0;
                var grabberExtent = center ? 0d : _vertical ? grabber.GetHeight() : grabber.GetWidth();
                var travel = (_vertical ? Size.Y : Size.X) - grabberExtent;
                var ratio = (pixel - grabberExtent / 2) / travel;
                if (_vertical || IsLayoutRTL()) ratio = 1 - ratio;
                ratioReady = true;
                SetRatioWithoutLocalSignals(ratio);
            }
            catch (Exception error) { CollectException(ref errors, error); }
            if (ratioReady && InteractionCurrent(generation, tree))
            {
                _grabActive = true; _grabRatio = Ratio;
                try { NotifySharedValueForInteraction(); } catch (Exception error) { CollectException(ref errors, error); }
            }
        }
        ThrowCollected("Slider drag callbacks failed.", errors);
    }
    private void EndDrag()
    {
        _grabActive = false; _interactionGeneration++; List<Exception>? errors = null;
        try { DragEnded?.Invoke(!Mathf.IsEqualApprox(_ratioBeforeDrag, Ratio)); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Slider release callbacks failed.", errors);
    }
    private void Wheel(bool increase)
    {
        var generation = _interactionGeneration; var tree = Tree; List<Exception>? errors = null;
        try { if (IsInsideTree && IsVisibleInTree && GetFocusModeWithOverride() != FocusMode.None) GrabFocus(); } catch (Exception error) { CollectException(ref errors, error); }
        if (InteractionCurrent(generation, tree)) try { Value += increase ? Step : -Step; } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Slider wheel callbacks failed.", errors);
    }
    private void StopRepeat()
    {
        _repeatEnabled = false; _repeatDelay = .5f;
        if (!IsDisposed) SetInternalProcessing(false, false);
    }
    private void CancelInteraction()
    {
        _interactionGeneration++; _grabActive = false; _mouseInside = false; StopRepeat();
    }
    private void ProcessRepeat()
    {
        if (!_repeatEnabled) return;
        if (!_editable || !IsInsideTree || !IsVisibleInTree || !HasFocus() || Released("ui_left") || Released("ui_right") || Released("ui_up") || Released("ui_down")) { StopRepeat(); return; }
        var decrease = _vertical ? "ui_down" : "ui_left"; var increase = _vertical ? "ui_up" : "ui_right";
        if (!Held(decrease) && !Held(increase)) { StopRepeat(); return; }
        _repeatDelay = (float)(_repeatDelay - ProcessDeltaTime);
        if (_repeatDelay > 0) return;
        _repeatDelay = .05f + _repeatDelay;
        var direction = !_vertical && IsLayoutRTL() ? -1 : 1; var generation = _interactionGeneration; var tree = Tree;
        List<Exception>? errors = null;
        try { if (Held(decrease)) Value -= direction * Step; } catch (Exception error) { CollectException(ref errors, error); }
        if (_repeatEnabled && InteractionCurrent(generation, tree)) try { if (Held(increase)) Value += direction * Step; } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Slider repeat callbacks failed.", errors);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Drawing lacks a required theme resource, exceeds integer pixel coordinates, or requests more than 1,048,576 tick commands.</exception>
    /// <exception cref="OverflowException">Signed theme offsets overflow checked pixel arithmetic.</exception>
    /// <exception cref="ObjectDisposedException">Drawing uses a disposed borrowed style or icon.</exception>
    /// <exception cref="AggregateException">Theme, lifecycle or repeated-value callbacks fail after required phases are attempted.</exception>
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { base.OnNotification(what); DrawSlider(); return; }
        List<Exception>? errors = null;
        try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed)
            try
            {
                switch (what)
                {
                    case NotificationInternalProcess: ProcessRepeat(); break;
                    case NotificationMouseEnter: _mouseInside = true; QueueRedraw(); break;
                    case NotificationMouseExit: _mouseInside = false; QueueRedraw(); break;
                    case NotificationFocusEnter: QueueRedraw(); break;
                    case NotificationFocusExit: StopRepeat(); QueueRedraw(); break;
                    case NotificationVisibilityChanged:
                    case NotificationExitTree: CancelInteraction(); break;
                }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Slider notification callbacks failed.", errors);
    }
    internal override void OnTreeMembershipChanged(bool entering)
    {
        CancelInteraction(); base.OnTreeMembershipChanged(entering);
    }
    private StyleBox RequiredStyle(string name) => GetThemeStyleBox(name) ?? throw new InvalidOperationException($"Slider drawing requires the '{name}' style.");
    private Texture RequiredIcon(string name) => GetThemeIcon(name) ?? throw new InvalidOperationException($"Slider drawing requires the '{name}' icon.");
    private static int Pixel(double value)
    {
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue) throw new InvalidOperationException("Slider geometry exceeds integer pixel range.");
        return (int)value;
    }
    private static int RoundedPixel(double value) => Pixel(Math.Round(value, MidpointRounding.AwayFromZero));
    private void DrawSlider()
    {
        var knownTicks = _ticksPosition is >= TickPosition.BottomRight and <= TickPosition.Center;
        var tickCommands = _tickCount > 1 && knownTicks ? ((long)_tickCount - (_ticksOnBorders ? 0 : 2)) * (_ticksPosition == TickPosition.Both ? 2 : 1) : 0;
        if (tickCommands > MaximumTickCommands) throw new InvalidOperationException("Slider tick drawing exceeds the finite command budget.");
        var width = Pixel(Size.X); var height = Pixel(Size.Y); var ratio = Ratio; if (double.IsNaN(ratio)) ratio = 0;
        var style = RequiredStyle("slider"); var highlighted = _editable && (_mouseInside || HasFocus(true));
        var grabber = RequiredIcon(!_editable ? "grabber_disabled" : highlighted ? "grabber_highlight" : "grabber");
        var grabberArea = RequiredStyle(highlighted ? "grabber_area_highlight" : "grabber_area");
        var grabberWidth = grabber.GetWidth(); var grabberHeight = grabber.GetHeight();
        var center = GetThemeConstant("center_grabber") != 0; var grabberOffset = GetThemeConstant("grabber_offset"); var tickOffset = GetThemeConstant("tick_offset");
        var tick = _tickCount > 1 ? RequiredIcon("tick") : null; var tickWidth = tick?.GetWidth() ?? 0; var tickHeight = tick?.GetHeight() ?? 0;
        if (_vertical)
        {
            var trackWidth = Pixel(style.GetMinimumSize().X); double travel = checked(height - (center ? 0 : grabberHeight)); var shift = center ? grabberHeight / 2 : 0;
            DrawStyleBox(style, new(checked(width / 2 - trackWidth / 2), 0, trackWidth, height));
            DrawStyleBox(grabberArea, new(checked((width - trackWidth) / 2), RoundedPixel(height - travel * ratio - grabberHeight / 2 + shift), trackWidth, RoundedPixel(travel * ratio + grabberHeight / 2 - shift)));
            if (knownTicks && tick is not null)
                for (var index = _ticksOnBorders ? 0 : 1; index < _tickCount - (_ticksOnBorders ? 0 : 1); index++)
                {
                    var offset = Pixel(index * travel / (_tickCount - 1) + (grabberHeight / 2 - tickHeight / 2) - shift);
                    if (_ticksPosition is TickPosition.BottomRight or TickPosition.Both) DrawTexture(tick, new(checked(trackWidth + (width - trackWidth) / 2 + tickOffset), offset));
                    if (_ticksPosition is TickPosition.TopLeft or TickPosition.Both) DrawTextureRect(tick, new(checked((width - trackWidth) / 2 - tickWidth - tickOffset), offset, checked(-tickWidth), tickHeight), false);
                    if (_ticksPosition == TickPosition.Center) DrawTexture(tick, new(checked((width - tickWidth) / 2 + tickOffset), offset));
                }
            DrawTexture(grabber, new(checked(width / 2 - grabberWidth / 2 + grabberOffset), Pixel(height - ratio * travel - grabberHeight + shift)));
        }
        else
        {
            var trackHeight = Pixel(style.GetMinimumSize().Y); double travel = width - (center ? 0 : grabber.GetSize().X); var shift = center ? checked(-grabberWidth) / 2 : 0; var rtl = IsLayoutRTL();
            var trackY = checked((height - trackHeight) / 2); var fill = Pixel(travel * (rtl ? 1 - ratio : ratio) + grabberWidth / 2 + shift);
            DrawStyleBox(style, new(0, trackY, width, trackHeight));
            DrawStyleBox(grabberArea, rtl ? new(fill, trackY, checked(width - fill), trackHeight) : new(0, trackY, fill, trackHeight));
            if (knownTicks && tick is not null)
                for (var index = _ticksOnBorders ? 0 : 1; index < _tickCount - (_ticksOnBorders ? 0 : 1); index++)
                {
                    var offset = Pixel(index * travel / (_tickCount - 1) + (grabberWidth / 2 - tickWidth / 2) + shift);
                    if (_ticksPosition is TickPosition.BottomRight or TickPosition.Both) DrawTexture(tick, new(offset, checked(trackHeight + (height - trackHeight) / 2 + tickOffset)));
                    if (_ticksPosition is TickPosition.TopLeft or TickPosition.Both) DrawTextureRect(tick, new(offset, checked((height - trackHeight) / 2 - tickHeight - tickOffset), tickWidth, checked(-tickHeight)), false);
                    if (_ticksPosition == TickPosition.Center) DrawTexture(tick, new(offset, checked((height - tickHeight) / 2 + tickOffset)));
                }
            DrawTexture(grabber, new(Pixel((rtl ? 1 - ratio : ratio) * travel + shift), checked(height / 2 - grabberHeight / 2 + grabberOffset)));
        }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors()
        .Where(property => property.Name != nameof(Step) && property.Name != nameof(FocusMode) && property.Name != nameof(SizeFlagsHorizontal) && property.Name != nameof(SizeFlagsVertical) && property.Name != nameof(Value)).Concat(SliderProperties);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _interactionGeneration++; _grabActive = false; _repeatEnabled = false;
        try { base.Dispose(disposing); }
        finally { DragStarted = null; DragEnded = null; }
    }
}

/// <summary>A horizontal slider with horizontal Fill and vertical ShrinkBegin sizing defaults.</summary>
public class HSlider : Slider
{
    /// <summary>Creates a horizontal slider with unit step and full keyboard/controller focus.</summary>
    public HSlider() : base(vertical: false) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(HSlider) ? CreateSlider : base.CreateSceneInstanceFactory();
    private static Node CreateSlider() => new HSlider();
}

/// <summary>A vertical slider with horizontal ShrinkBegin and vertical Fill sizing defaults.</summary>
public class VSlider : Slider
{
    /// <summary>Creates a vertical slider with unit step and full keyboard/controller focus.</summary>
    public VSlider() : base(vertical: true) { }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(VSlider) ? CreateSlider : base.CreateSceneInstanceFactory();
    private static Node CreateSlider() => new VSlider();
}
