namespace Electron2D;

/// <summary>Chooses the input edge that activates a button.</summary>
public enum ButtonActionMode
{
    /// <summary>Activates on the initial press.</summary>
    ButtonPress = 0,
    /// <summary>Activates on release after a press that is still inside.</summary>
    ButtonRelease = 1
}

/// <summary>Provides mouse, touch, action and shortcut activation for concrete button controls.</summary>
/// <remarks>The base draws no decoration. Buttons borrow their shortcut and group. Attached access follows the
/// scene owner thread; mutations are rejected during capture. Committed input and group state is retained when
/// callbacks fail; remaining current phases run before aggregated errors are reported. Reentrant interactions
/// replace the older operation. Pointer/shortcut feedback is transient and is not packed.</remarks>
public abstract class BaseButton : Control
{
    /// <summary>Identifies the visual state used by concrete button drawing.</summary>
    public enum DrawMode
    {
        /// <summary>Neither pressed nor hovered.</summary>
        Normal = 0,
        /// <summary>Pressed without hover decoration.</summary>
        Pressed = 1,
        /// <summary>Hovered without being pressed.</summary>
        Hover = 2,
        /// <summary>Interaction is disabled.</summary>
        Disabled = 3,
        /// <summary>Both hovered and pressed, including shortcut feedback.</summary>
        HoverPressed = 4
    }
    private bool _disabled, _toggleMode, _pressed, _hovered, _attempt, _inside, _down, _keepOutside, _wasMouse;
    private bool _shortcutFeedback = true, _shortcutInTooltip = true, _feedback, _resourceProcessing;
    private int _touch = -1;
    private double _feedbackLeft, _feedbackDuration = -1;
    private ButtonActionMode _actionMode = ButtonActionMode.ButtonRelease;
    private MouseButtonMask _buttonMask = MouseButtonMask.Left;
    private Shortcut? _shortcut;
    private ButtonGroup? _group;
    private readonly WeakReference<BaseButton> _groupReference;
    private ulong _interaction, _groupGeneration;
    private static readonly PropertyDescriptor[] ButtonProperties =
    [
        new PropertyDescriptor<BaseButton, bool>(nameof(Disabled), b => b.Disabled, (b,v) => b.Disabled=v, _ => false, stored:true),
        new PropertyDescriptor<BaseButton, bool>(nameof(ToggleMode), b => b.ToggleMode, (b,v) => b.ToggleMode=v, _ => false, stored:true),
        new PropertyDescriptor<BaseButton, bool>(nameof(ButtonPressed), b => b.ButtonPressed, (b,v) => b.ButtonPressed=v, _ => false, stored:true),
        new PropertyDescriptor<BaseButton, ButtonActionMode>(nameof(ActionMode), b => b.ActionMode, (b,v) => b.ActionMode=v, _ => ButtonActionMode.ButtonRelease, stored:true),
        new PropertyDescriptor<BaseButton, MouseButtonMask>(nameof(ButtonMask), b => b.ButtonMask, (b,v) => b.ButtonMask=v, _ => MouseButtonMask.Left, stored:true),
        new PropertyDescriptor<BaseButton, bool>(nameof(KeepPressedOutside), b => b.KeepPressedOutside, (b,v) => b.KeepPressedOutside=v, _ => false, stored:true),
        new PropertyDescriptor<BaseButton, ButtonGroup?>(nameof(ButtonGroup), b => b.ButtonGroup, (b,v) => b.ButtonGroup=v, _ => null, stored:true),
        new PropertyDescriptor<BaseButton, Shortcut?>(nameof(Shortcut), b => b.Shortcut, (b,v) => b.Shortcut=v, _ => null, stored:true),
        new PropertyDescriptor<BaseButton, bool>(nameof(ShortcutFeedback), b => b.ShortcutFeedback, (b,v) => b.ShortcutFeedback=v, _ => true, stored:true),
        new PropertyDescriptor<BaseButton, bool>(nameof(ShortcutInTooltip), b => b.ShortcutInTooltip, (b,v) => b.ShortcutInTooltip=v, _ => true, stored:true),
        new PropertyDescriptor<BaseButton, FocusMode>(nameof(FocusMode), b => b.FocusMode, (b,v) => b.FocusMode=v, _ => FocusMode.All, stored:true)
    ];
    /// <summary>Initializes a momentary button activated on release, with left-button input and full focus.</summary>
    protected BaseButton() { _groupReference = new(this); FocusMode = FocusMode.All; }
    /// <summary>Occurs when the configured edge activates the button, after the pressed hook.</summary>
    public event Action? Pressed;
    /// <summary>Occurs when an eligible input first begins holding the button.</summary>
    public event Action? ButtonDown;
    /// <summary>Occurs when holding ends, including disable and focus cancellation.</summary>
    public event Action? ButtonUp;
    /// <summary>Occurs after a signalled toggle and its hook; the argument is the committed toggle state.</summary>
    public event Action<bool>? Toggled;
    /// <summary>Handles activation before <see cref="Pressed"/>.</summary>
    protected virtual void OnPressed() { }
    /// <summary>Handles a signalled toggle before <see cref="Toggled"/>.</summary>
    /// <param name="toggledOn">The committed toggle state.</param>
    protected virtual void OnToggled(bool toggledOn) { }
    private void CheckButton() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    internal void ValidateGroupMutation() => EnsureMutable();
    internal ButtonGroup? MembershipGroup => _group;
    internal ulong MembershipGeneration => _groupGeneration;
    internal ulong InteractionGeneration => _interaction;
    internal bool WasPressedByMouse => _wasMouse;
    private bool Current(ulong generation, SceneTree? tree) => !IsDisposed && generation == _interaction && ReferenceEquals(Tree, tree);

    /// <summary>Gets or sets whether GUI and shortcut activation is disabled.</summary>
    /// <value>False initially. Disabling releases a held button before redraw and minimum-size invalidation.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    /// <exception cref="AggregateException">A release or invalidation callback fails after state commits.</exception>
    public bool Disabled
    {
        get { CheckButton(); return _disabled; }
        set
        {
            EnsureMutable(); if (_disabled == value) return; _disabled = value; _interaction++;
            List<Exception>? errors = null;
            if (value) { if (!_toggleMode) _pressed = false; ClearAttempt(); ReleaseHeld(ref errors); }
            if (!IsDisposed) { try { QueueRedraw(); } catch (Exception e) { CollectException(ref errors, e); } }
            if (!IsDisposed) { try { UpdateMinimumSize(); } catch (Exception e) { CollectException(ref errors, e); } }
            ThrowCollected("Button disable callbacks failed.", errors);
        }
    }
    /// <summary>Gets or sets whether activation toggles a persistent pressed state.</summary>
    /// <value>False initially. Turning it off first performs an ordinary signalled unpress.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    /// <exception cref="AggregateException">A toggle callback fails after state commits.</exception>
    public bool ToggleMode
    {
        get { CheckButton(); return _toggleMode; }
        set
        {
            EnsureMutable(); List<Exception>? errors = null;
            var expected = _interaction + (!value && _toggleMode && _pressed ? 1UL : 0UL);
            if (!value) try { ButtonPressed = false; } catch (Exception e) { CollectException(ref errors, e); }
            if (!IsDisposed && _interaction == expected) { _toggleMode = value; _interaction++; UpdateConfigurationWarnings(); }
            ThrowCollected("Button toggle-mode callbacks failed.", errors);
        }
    }
    /// <summary>Gets whether the button is held or toggled, or changes its persistent toggle state.</summary>
    /// <value>False initially; setting has no effect outside toggle mode.</value>
    /// <remarks>Setting true unpresses group peers, emits the group's Pressed, then this button's Toggled.
    /// It does not emit this button's Pressed. Explicit false is allowed even when the group disallows user unpress.</remarks>
    /// <exception cref="InvalidOperationException">This button or a group peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The button or its borrowed group is disposed.</exception>
    /// <exception cref="AggregateException">A committed group or toggle callback fails.</exception>
    public bool ButtonPressed
    {
        get { CheckButton(); return _toggleMode ? _pressed : _attempt; }
        set
        {
            EnsureMutable(); if (!_toggleMode || _pressed == value) return; if (value) _group?.ValidateMembers();
            var generation = ++_interaction; var tree = Tree; _pressed = value; QueueRedraw(); List<Exception>? errors = null;
            if (value) UnpressGroup(generation, tree, ref errors);
            if (Current(generation, tree)) NotifyToggled(generation, tree, ref errors);
            ThrowCollected("Button state callbacks failed.", errors);
        }
    }
    /// <summary>Changes persistent toggle state without callbacks or unpressing group peers.</summary>
    /// <param name="pressed">The new toggle state.</param>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public void SetPressedNoSignal(bool pressed) { EnsureMutable(); if (!_toggleMode || _pressed == pressed) return; _pressed = pressed; _interaction++; QueueRedraw(); }
    /// <summary>Gets or sets the input edge that activates the button.</summary>
    /// <value>ButtonRelease initially. Unknown values are retained and activate on neither edge.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public ButtonActionMode ActionMode { get { CheckButton(); return _actionMode; } set { EnsureMutable(); _actionMode = value; } }
    /// <summary>Gets or sets the mouse-button bit mask accepted by this control.</summary>
    /// <value>Left initially. Unknown bits are retained.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public MouseButtonMask ButtonMask { get { CheckButton(); return _buttonMask; } set { EnsureMutable(); _buttonMask = value; } }
    /// <summary>Gets or sets whether a held pointer outside the shape still draws pressed.</summary>
    /// <value>False initially. This affects appearance only; release outside does not activate.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public bool KeepPressedOutside { get { CheckButton(); return _keepOutside; } set { EnsureMutable(); _keepOutside = value; } }
    /// <summary>Gets or sets the borrowed shortcut used by the shortcut input stage.</summary>
    /// <value>Null initially. Changing identity enables or disables shortcut input delivery.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The button or supplied shortcut is disposed.</exception>
    public Shortcut? Shortcut
    {
        get { CheckButton(); return _shortcut; }
        set { EnsureMutable(); if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value); if (ReferenceEquals(_shortcut, value)) return; _shortcut = value; ShortcutInputEnabled = value is not null; }
    }
    /// <summary>Gets or sets whether shortcut activation briefly draws hover-pressed feedback.</summary>
    /// <value>True initially. Changing this does not stop an already active highlight.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public bool ShortcutFeedback { get { CheckButton(); return _shortcutFeedback; } set { EnsureMutable(); _shortcutFeedback = value; } }
    /// <summary>Gets or sets whether the default tooltip includes the shortcut name and events.</summary>
    /// <value>True initially. A user-supplied tooltip control takes precedence.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public bool ShortcutInTooltip { get { CheckButton(); return _shortcutInTooltip; } set { EnsureMutable(); _shortcutInTooltip = value; } }
    /// <summary>Gets or sets the borrowed radio-button group.</summary>
    /// <value>Null initially. Assignment changes membership without changing any pressed state.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The button or supplied group is disposed.</exception>
    public ButtonGroup? ButtonGroup
    {
        get { CheckButton(); return _group; }
        set
        {
            EnsureMutable(); value?.ValidateMembers(); _group?.Remove(_groupReference);
            _group = value; _groupGeneration++; _interaction++; _group?.Add(_groupReference); QueueRedraw(); UpdateConfigurationWarnings();
        }
    }
    /// <summary>Returns whether the pointer has entered and not yet left this control.</summary>
    /// <returns>The current hover state, independent of disabled state.</returns>
    /// <exception cref="InvalidOperationException">An attached button is queried off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public bool IsHovered() { CheckButton(); return _hovered; }
    /// <summary>Returns the current visual button state.</summary>
    /// <returns>Disabled, shortcut feedback, hover and press-attempt state in that priority order.</returns>
    /// <exception cref="InvalidOperationException">An attached button is queried off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public DrawMode GetDrawMode()
    {
        CheckButton(); if (_disabled) return DrawMode.Disabled; if (_feedback) return DrawMode.HoverPressed;
        if (!_attempt && _hovered) return _pressed ? DrawMode.HoverPressed : DrawMode.Hover;
        var pressing = _attempt ? (_inside || _keepOutside) ^ _pressed : _pressed;
        return pressing ? DrawMode.Pressed : DrawMode.Normal;
    }
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">The input event is null.</exception>
    /// <exception cref="ArgumentException">Pointer motion contains nonfinite coordinates.</exception>
    /// <exception cref="InvalidOperationException">The button or a group peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The button, input or borrowed group is disposed.</exception>
    /// <exception cref="AggregateException">An input, toggle or activation callback fails after committed state.</exception>
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(inputEvent); ObjectDisposedException.ThrowIf(inputEvent.IsDisposed, inputEvent);
        if (_disabled) return;
        if (inputEvent.Device != InputEvent.DeviceIdEmulation)
        {
            if (inputEvent is InputEventScreenTouch touch)
            {
                if (_touch == -1 && touch.Pressed)
                {
                    _touch = touch.Index; _attempt = true; _inside = HasPoint(touch.Position); ActionEvent(inputEvent); return;
                }
                if (_touch == touch.Index && !touch.Pressed) { _touch = -1; ActionEvent(inputEvent); return; }
            }
            if (inputEvent is InputEventScreenDrag drag && drag.Index == _touch && _attempt) UpdateInside(drag.Position);
        }
        var masked = inputEvent is InputEventMouseButton mouse && (int)mouse.ButtonIndex is >= 1 and <= 32 && ((uint)_buttonMask & (1u << ((int)mouse.ButtonIndex - 1))) != 0;
        if (masked || inputEvent.IsAction("ui_accept", true) && !inputEvent.IsEcho())
        {
            var previous = _wasMouse; _wasMouse = masked;
            try { ActionEvent(inputEvent); } finally { _wasMouse = previous; }
        }
        else if (inputEvent is InputEventMouseMotion motion && _attempt) UpdateInside(motion.Position);
    }
    private void UpdateInside(Vector2 point)
    {
        if (!point.IsFinite()) throw new ArgumentException("Button pointer coordinates must be finite.", nameof(point));
        var inside = HasPoint(point); if (_inside == inside) return; _inside = inside; QueueRedraw();
    }
    private void ActionEvent(InputEvent input)
    {
        _group?.ValidateMembers(); var generation = ++_interaction; var tree = Tree; var pressed = input.IsPressed();
        var accept = input is not (InputEventMouseButton or InputEventScreenTouch or InputEventScreenDrag);
        List<Exception>? errors = null;
        if (pressed && (accept || _hovered || input is InputEventScreenTouch && _inside))
        {
            _attempt = true; _inside = true;
            if (!_down) { _down = true; try { ButtonDown?.Invoke(); } catch (Exception e) { CollectException(ref errors, e); } }
        }
        if (Current(generation, tree) && _attempt && _inside &&
            (pressed && _actionMode == ButtonActionMode.ButtonPress || input.IsReleased() && _actionMode == ButtonActionMode.ButtonRelease))
        {
            if (_toggleMode)
            {
                if (_actionMode == ButtonActionMode.ButtonPress) ClearAttempt();
                _pressed = !_pressed; UnpressGroup(generation, tree, ref errors);
                if (Current(generation, tree)) NotifyToggled(generation, tree, ref errors);
            }
            if (Current(generation, tree)) NotifyPressed(generation, tree, ref errors);
        }
        if (Current(generation, tree))
        {
            if (!pressed) { ClearAttempt(); ReleaseHeld(ref errors); }
            if (!IsDisposed) QueueRedraw();
        }
        ThrowCollected("Button input callbacks failed.", errors);
    }
    private void UnpressGroup(ulong generation, SceneTree? tree, ref List<Exception>? errors)
    {
        var group = _group; if (group is null) return;
        if (_toggleMode && !group.AllowUnpress) _pressed = true;
        try { group.UnpressOthers(this); } catch (Exception e) { CollectException(ref errors, e); }
        if (Current(generation, tree) && ReferenceEquals(_group, group))
            try { group.NotifyPressed(this); } catch (Exception e) { CollectException(ref errors, e); }
    }
    private void NotifyToggled(ulong generation, SceneTree? tree, ref List<Exception>? errors)
    {
        var pressed = _pressed;
        try { OnToggled(pressed); } catch (Exception e) { CollectException(ref errors, e); }
        if (Current(generation, tree)) try { Toggled?.Invoke(pressed); } catch (Exception e) { CollectException(ref errors, e); }
    }
    private void NotifyPressed(ulong generation, SceneTree? tree, ref List<Exception>? errors)
    {
        try { OnPressed(); } catch (Exception e) { CollectException(ref errors, e); }
        if (Current(generation, tree)) try { Pressed?.Invoke(); } catch (Exception e) { CollectException(ref errors, e); }
    }
    private void ClearAttempt() { _attempt = false; _inside = false; _touch = -1; }
    private void ReleaseHeld(ref List<Exception>? errors)
    {
        if (!_down) return; _down = false;
        try { ButtonUp?.Invoke(); } catch (Exception e) { CollectException(ref errors, e); }
    }
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">The input event is null.</exception>
    /// <exception cref="InvalidOperationException">The button or a group peer cannot be mutated, or acceptance is outside input dispatch.</exception>
    /// <exception cref="ObjectDisposedException">The button, input, shortcut or group is disposed.</exception>
    /// <exception cref="AggregateException">An activation or input-acceptance phase fails after state commits.</exception>
    protected override void OnShortcutInput(InputEvent inputEvent)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(inputEvent);
        if (_disabled || !inputEvent.IsPressed() || !IsVisibleInTree || inputEvent.IsEcho() || _shortcut is null ||
            !IsFocusOwnerInShortcutContext() || !_shortcut.MatchesEvent(inputEvent)) return;
        _group?.ValidateMembers(); var generation = ++_interaction; var tree = Tree; List<Exception>? errors = null;
        if (_toggleMode)
        {
            _pressed = !_pressed; UnpressGroup(generation, tree, ref errors);
            if (Current(generation, tree)) NotifyToggled(generation, tree, ref errors);
        }
        if (Current(generation, tree)) NotifyPressed(generation, tree, ref errors);
        if (tree is { IsDisposed: false }) try { tree.SetInputAsHandled(); } catch (Exception e) { CollectException(ref errors, e); }
        if (Current(generation, tree))
        {
            QueueRedraw();
            if (_shortcutFeedback && IsInsideTree)
            {
                if (_feedbackDuration < 0) _feedbackDuration = ProjectSettings.Instance.GetWithOverride(ProjectSettings.ButtonShortcutFeedbackHighlightTime);
                _feedback = true; _feedbackLeft = _feedbackDuration; UpdateInternalProcessing();
            }
        }
        ThrowCollected("Button shortcut callbacks failed.", errors);
    }
    internal void SetButtonResourceProcessing(bool enabled) { _resourceProcessing = enabled; UpdateInternalProcessing(); }
    private void UpdateInternalProcessing() => SetInternalProcessing(_resourceProcessing || _feedback, false);
    internal void CancelButtonInteraction()
    {
        if (IsDisposed) return; _interaction++; ClearAttempt(); List<Exception>? errors = null; ReleaseHeld(ref errors); QueueRedraw();
        ThrowCollected("Button cancellation callbacks failed.", errors);
    }
    /// <inheritdoc />
    /// <exception cref="AggregateException">Inherited notifications or button cleanup callbacks fail after required state maintenance.</exception>
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null;
        try { base.OnNotification(what); } catch (Exception e) { CollectException(ref errors, e); }
        if (!IsDisposed)
            try
            {
                switch (what)
                {
                    case NotificationMouseEnter: _hovered = true; QueueRedraw(); break;
                    case NotificationMouseExit: _hovered = false; QueueRedraw(); break;
                    case NotificationFocusEnter: QueueRedraw(); break;
                    case NotificationFocusExit: CancelButtonInteraction(); break;
                    case NotificationPaused:
                    case NotificationDisabled:
                        if (!CanProcess())
                            try { CancelButtonInteraction(); }
                            finally { Tree?.ReleaseGUITouchFocus(this); }
                        break;
                    case NotificationVisibilityChanged:
                        if (IsVisibleInTree) break;
                        goto case NotificationExitTree;
                    case NotificationExitTree:
                        _interaction++; if (!_toggleMode) _pressed = false; _hovered = false; ClearAttempt(); _down = false; break;
                    case NotificationInternalProcess:
                        if (_feedback)
                        {
                            _feedbackLeft -= Tree?.CurrentUnscaledProcessStep ?? GetUnscaledProcessDelta(false);
                            if (_feedbackLeft < 0) { _feedback = false; UpdateInternalProcessing(); QueueRedraw(); }
                        }
                        break;
                }
            }
            catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Button notification callbacks failed.", errors);
    }
    internal override void OnTreeMembershipChanged(bool entering)
    {
        _interaction++; ClearAttempt(); _down = false; _hovered = false; base.OnTreeMembershipChanged(entering);
    }
    internal override Control? CreateTooltipControl(string text)
    {
        var custom = base.CreateTooltipControl(text); if (custom is not null || !_shortcutInTooltip || _shortcut is null) return custom;
        var events = _shortcut.HasValidEvent(); var name = _shortcut.ResourceName;
        if (!events && name.Length == 0) return null;
        var result = Atr(name); if (events) result += " (" + _shortcut.GetAsText() + ")";
        if (text.Length != 0 && !string.Equals(name, text, StringComparison.OrdinalIgnoreCase)) result += "\n" + Atr(text);
        return new Label { Text = result, AutoTranslateMode = NodeAutoTranslateMode.Disabled, ThemeTypeVariation = "TooltipLabel" };
    }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        return _group is not null && !_toggleMode ? [.. warnings, "ButtonGroup requires ToggleMode for radio-button behavior."] : warnings;
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(p => p.Name != nameof(FocusMode)).Concat(ButtonProperties);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _interaction++; _groupGeneration++; _group?.Remove(_groupReference); _group = null; _shortcut = null;
        ClearAttempt(); _down = false; _feedback = false;
        try { base.Dispose(disposing); } finally { Pressed = null; Toggled = null; ButtonDown = null; ButtonUp = null; }
    }
}
