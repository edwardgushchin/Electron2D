namespace Electron2D;

/// <summary>Provides a themed fixed, dynamic or following touch joystick that publishes four analog input actions.</summary>
/// <remarks>One signed contact index is active at a time. Action contributions are independent of other controls,
/// physical bindings and manual action presses. Geometry configuration resets silently; lifecycle cancellation retires
/// contributions without producing a tap or flick. Runtime contact/vector state is not scene storage.</remarks>
public partial class VirtualJoystick : Control
{
    private readonly VirtualActionBinding[] _bindings;
    private readonly string[] _actions = ["ui_left", "ui_right", "ui_up", "ui_down"];
    private bool IsEdited => Tree?.EditedSceneRoot is { } root && (ReferenceEquals(root, this) || root.IsAncestorOf(this));
    private int? _finger;
    private bool _moved, _hasInput, _flickCanceled;
    private ulong _revision;
    private float _joystickSize = 100, _tipSize = 50, _deadzone, _clampzone = 1;
    private Vector2 _initial = new(.5f, .5f), _basePosition, _tipPosition, _inputVector;
    private VirtualJoystickMode _mode;
    private JoystickVisibility _visibility;
    /// <summary>Creates a fixed, always-visible joystick with built-in directional actions.</summary>
    public VirtualJoystick()
    { _bindings = [new(this, 0), new(this, 1), new(this, 2), new(this, 3)]; MouseFilter = MouseFilter.Stop; }
    private void CheckJoystick() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    /// <summary>Gets or sets the joystick base movement policy.</summary>
    /// <value>Fixed initially.</value>
    public VirtualJoystickMode JoystickMode { get { CheckJoystick(); return _mode; } set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _mode = value; } }
    /// <summary>Gets or sets the base diameter in control units.</summary>
    /// <value>100 initially; finite nonnegative values. Changes reset the active contact silently.</value>
    public float JoystickSize { get { CheckJoystick(); return _joystickSize; } set { EnsureMutable(); Finite(value); if (value < 0 || _clampzone > 0 && !float.IsFinite(value * .5f * _clampzone)) throw new ArgumentOutOfRangeException(nameof(value)); if (_joystickSize == value) return; _joystickSize = value; Reset(); } }
    /// <summary>Gets or sets the movable tip diameter.</summary>
    /// <value>50 initially; finite nonnegative values. Changes reset the active contact silently.</value>
    public float TipSize { get { CheckJoystick(); return _tipSize; } set { EnsureMutable(); Finite(value); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_tipSize == value) return; _tipSize = value; Reset(); } }
    /// <summary>Gets or sets the fraction of the clamp radius which yields zero output.</summary>
    /// <value>Zero initially; finite authored ratios. Values at or above one suppress output within the clamp radius.</value>
    public float DeadzoneRatio { get { CheckJoystick(); return _deadzone; } set { EnsureMutable(); Finite(value); _deadzone = value; } }
    /// <summary>Gets or sets the fraction of base radius limiting tip movement.</summary>
    /// <value>One initially; finite authored ratios. Nonpositive effective radii yield zero output.</value>
    public float ClampzoneRatio { get { CheckJoystick(); return _clampzone; } set { EnsureMutable(); Finite(value); if (value > 0 && !float.IsFinite(_joystickSize * .5f * value)) throw new ArgumentOutOfRangeException(nameof(value)); _clampzone = value; } }
    /// <summary>Gets or sets the base's normalized initial offset within the control.</summary>
    /// <value>(0.5, 0.5) initially. Finite authored values reset the active contact silently.</value>
    public Vector2 InitialOffsetRatio { get { CheckJoystick(); return _initial; } set { EnsureMutable(); if (!value.IsFinite() || !(Size * value).IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); if (_initial == value) return; _initial = value; Reset(); } }
    /// <summary>Gets or sets whether decorations remain visible without an active contact.</summary>
    /// <value>Always initially; ordinary Control visibility still applies.</value>
    public JoystickVisibility VisibilityMode { get { CheckJoystick(); return _visibility; } set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _visibility = value; QueueRedraw(); } }
    /// <summary>Gets or sets the negative horizontal action name.</summary>
    /// <value>ui_left initially. Empty/unknown names do not publish action state.</value>
    public string ActionLeft { get { CheckJoystick(); return _actions[0]; } set => SetAction(0, value); }
    /// <summary>Gets or sets the positive horizontal action name.</summary>
    /// <value>ui_right initially.</value>
    public string ActionRight { get { CheckJoystick(); return _actions[1]; } set => SetAction(1, value); }
    /// <summary>Gets or sets the negative vertical action name.</summary>
    /// <value>ui_up initially.</value>
    public string ActionUp { get { CheckJoystick(); return _actions[2]; } set => SetAction(2, value); }
    /// <summary>Gets or sets the positive vertical action name.</summary>
    /// <value>ui_down initially.</value>
    public string ActionDown { get { CheckJoystick(); return _actions[3]; } set => SetAction(3, value); }
    private void SetAction(int index, string action)
    { EnsureMutable(); ArgumentNullException.ThrowIfNull(action); if (_actions[index] == action) return; _bindings[index].Release(); _actions[index] = action; if (_finger is not null) PublishActions(); }
    /// <summary>Occurs after a touch contact, geometry and directional action state are committed.</summary>
    public event Action? Pressed;
    /// <summary>Occurs after the active contact and its action contributions are released.</summary>
    public event Action<Vector2>? Released;
    /// <summary>Occurs for a released contact that never moved and was not canceled.</summary>
    public event Action? Tapped;
    /// <summary>Occurs on a moved contact released with nonzero directional input.</summary>
    public event Action<Vector2>? Flicked;
    /// <summary>Occurs when an active directional movement returns to the dead zone.</summary>
    public event Action? FlickCanceled;
    private void PublishActions()
    {
        _bindings[0].Set(_actions[0], Math.Max(0, -_inputVector.X)); _bindings[1].Set(_actions[1], Math.Max(0, _inputVector.X));
        _bindings[2].Set(_actions[2], Math.Max(0, -_inputVector.Y)); _bindings[3].Set(_actions[3], Math.Max(0, _inputVector.Y));
    }
    private void Reset(bool notify = false)
    {
        var active = _finger is not null; var vector = _inputVector; SetInternalProcessing(false, false); _revision++; _finger = null; _hasInput = _moved = _flickCanceled = false; _inputVector = Vector2.Zero;
        foreach (var binding in _bindings) binding.Release(); _basePosition = Size * _initial; _tipPosition = _basePosition;
        List<Exception>? errors = null;
        try { if (!IsDisposed) QueueRedraw(); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (notify && active && !IsDisposed) BeginEventEmission(Released)?.Invoke(vector); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Joystick reset callbacks failed.", errors);
    }
    private void UpdateJoystick(Vector2 position)
    {
        if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position));
        var offset = position - _basePosition; var length = offset.Length(); var radius = Math.Max(0, _joystickSize * .5f * _clampzone);
        if (!float.IsFinite(length) || !float.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(position));
        var direction = length > 0 ? offset / length : Vector2.Zero;
        if (_mode == VirtualJoystickMode.Following && length > radius && new Rect2(Vector2.Zero, Size).HasPoint(position)) _basePosition = position - direction * radius;
        if (length > radius) { length = radius; offset = direction * length; }
        _tipPosition = _basePosition + offset; var wasInput = _hasInput;
        _hasInput = radius > 0 && (double)length > (double)_deadzone * radius;
        _inputVector = _hasInput ? direction * (float)Math.Clamp(((double)length / radius - _deadzone) / (1.0 - _deadzone), 0, 1) : Vector2.Zero;
        if (!_inputVector.IsFinite()) { _inputVector = Vector2.Zero; _hasInput = false; }
        var canceled = !_flickCanceled && wasInput && !_hasInput;
        if (canceled) _flickCanceled = true; else if (_flickCanceled && !wasInput && _hasInput) _flickCanceled = false;
        PublishActions(); var revision = _revision; List<Exception>? errors = null;
        try { if (canceled) BeginEventEmission(FlickCanceled)?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed && revision == _revision) QueueRedraw(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Joystick motion callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent input)
    {
        if (IsEdited) { if (_finger is not null) Reset(); return; }
        if (input is InputEventScreenTouch touch)
        {
            if (touch.Pressed && !touch.Canceled && _finger is null && new Rect2(Vector2.Zero, Size).HasPoint(touch.Position))
            {
                if (_mode == VirtualJoystickMode.Fixed && !new Rect2(_basePosition - Vector2.One * (_joystickSize * .5f), Vector2.One * _joystickSize).HasPoint(touch.Position)) return;
                if (_mode is VirtualJoystickMode.Dynamic or VirtualJoystickMode.Following) _basePosition = touch.Position;
                _finger = touch.Index; SetInternalProcessing(true, false); _revision++; var revision = _revision; List<Exception>? errors = null;
                try { UpdateJoystick(touch.Position); } catch (Exception error) { CollectException(ref errors, error); }
                try { if (!IsDisposed && revision == _revision && _finger == touch.Index) BeginEventEmission(Pressed)?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
                ThrowCollected("Joystick press callbacks failed.", errors);
            }
            else if ((!touch.Pressed || touch.Canceled) && _finger == touch.Index)
            {
                var vector = _inputVector; var tap = !_flickCanceled && !_moved && !touch.Canceled; var flick = _hasInput && _moved && !touch.Canceled;
                var revision = _revision + 1; List<Exception>? errors = null; try { Reset(notify: true); } catch (Exception error) { CollectException(ref errors, error); }
                try { if (!IsDisposed && _finger is null && revision == _revision) { if (tap) BeginEventEmission(Tapped)?.Invoke(); else if (flick) BeginEventEmission(Flicked)?.Invoke(vector); } } catch (Exception error) { CollectException(ref errors, error); }
                ThrowCollected("Joystick release callbacks failed.", errors);
            }
        }
        else if (input is InputEventScreenDrag drag && _finger == drag.Index) { _moved = true; UpdateJoystick(drag.Position); }
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null; try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed) try
            {
                if (what is NotificationEnterTree or NotificationResized) Reset();
                else if (what == NotificationInternalProcess && _finger is not null && (EffectiveMouseFilter == MouseFilter.Ignore || GetViewport()?.GUIDisableInput == true || IsEdited)) Reset(notify: !IsEdited);
                else if (what is NotificationExitTree or NotificationPaused or NotificationDisabled or NotificationWmWindowFocusOut or NotificationApplicationFocusOut || what == NotificationVisibilityChanged && !IsVisibleInTree) Reset(notify: what != NotificationExitTree);
                else if (what == NotificationDraw && (IsEdited || _visibility == JoystickVisibility.Always || _finger is not null))
                {
                    var active = _finger is not null;
                    if (GetThemeStyleBox(active ? "pressed_joystick" : "normal_joystick") is { } joystick) DrawStyleBox(joystick, new(_basePosition - Vector2.One * (_joystickSize * .5f), Vector2.One * _joystickSize));
                    if (GetThemeStyleBox(active ? "pressed_tip" : "normal_tip") is { } tip) DrawStyleBox(tip, new(_tipPosition - Vector2.One * (_tipSize * .5f), Vector2.One * _tipSize));
                }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Joystick notification callbacks failed.", errors);
    }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    { CheckJoystick(); var warnings = new List<string>(base.GetConfigurationWarnings()); foreach (var action in _actions) if (action.Length != 0 && !InputMap.HasAction(action)) warnings.Add("A virtual-joystick action is not registered: " + action); return [.. warnings]; }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    { foreach (var binding in _bindings) binding.Release(); _finger = null; _revision++; try { base.Dispose(disposing); } finally { Pressed = Tapped = FlickCanceled = null; Released = Flicked = null; } }
}
