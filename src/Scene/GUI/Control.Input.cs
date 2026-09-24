namespace Electron2D;

/// <summary>Controls whether a rectangular control receives and consumes pointer input.</summary>
public enum MouseFilter
{
    /// <summary>Receive pointer input and mark it handled.</summary>
    Stop = 0,
    /// <summary>Receive pointer input and pass an unhandled event to a parent control.</summary>
    Pass = 1,
    /// <summary>Do not receive pointer input or obstruct controls behind this one.</summary>
    Ignore = 2
}

/// <summary>Controls how a control can become the keyboard input target.</summary>
public enum FocusMode
{
    /// <summary>Cannot receive focus.</summary>
    None = 0,
    /// <summary>Can receive focus by pointer press or an explicit request.</summary>
    Click = 1,
    /// <summary>Can also receive focus through keyboard or controller navigation.</summary>
    All = 2
}

/// <summary>Controls whether focus eligibility is inherited, disabled, or restored in a control subtree.</summary>
public enum ControlFocusBehaviorRecursive
{
    /// <summary>Follow the direct parent control, or allow focus when there is none.</summary>
    Inherited = 0,
    /// <summary>Disable focus unless a descendant explicitly enables it.</summary>
    Disabled = 1,
    /// <summary>Allow focus regardless of the parent control's policy.</summary>
    Enabled = 2
}

/// <summary>Controls whether pointer input is inherited, disabled, or restored in a control subtree.</summary>
public enum ControlMouseBehaviorRecursive
{
    /// <summary>Follow the direct parent control, or allow pointer input when there is none.</summary>
    Inherited = 0,
    /// <summary>Ignore pointer input unless a descendant explicitly enables it.</summary>
    Disabled = 1,
    /// <summary>Allow pointer input regardless of the parent control's policy.</summary>
    Enabled = 2
}

public partial class Control
{
    /// <summary>Identifies the system cursor shown over a control.</summary>
    public enum CursorShape
    {
        /// <summary>Arrow pointer.</summary>
        Arrow = 0,
        /// <summary>Text selection.</summary>
        IBeam = 1,
        /// <summary>Clickable link.</summary>
        PointingHand = 2,
        /// <summary>Crosshair.</summary>
        Cross = 3,
        /// <summary>Nonblocking wait.</summary>
        Wait = 4,
        /// <summary>Blocking wait.</summary>
        Busy = 5,
        /// <summary>Drag.</summary>
        Drag = 6,
        /// <summary>Drop allowed.</summary>
        CanDrop = 7,
        /// <summary>Drop forbidden.</summary>
        Forbidden = 8,
        /// <summary>Vertical resize.</summary>
        VSize = 9,
        /// <summary>Horizontal resize.</summary>
        HSize = 10,
        /// <summary>Northeast-southwest diagonal resize.</summary>
        BDiagSize = 11,
        /// <summary>Northwest-southeast diagonal resize.</summary>
        FDiagSize = 12,
        /// <summary>Move in any direction.</summary>
        Move = 13,
        /// <summary>Vertical split resize.</summary>
        VSplit = 14,
        /// <summary>Horizontal split resize.</summary>
        HSplit = 15,
        /// <summary>Help.</summary>
        Help = 16,
    }

    /// <summary>Pointer entered this control or a reachable child control.</summary>
    public const int NotificationMouseEnter = 41;
    /// <summary>Pointer exited this control and all reachable child controls.</summary>
    public const int NotificationMouseExit = 42;
    /// <summary>This control gained keyboard focus, before <see cref="FocusEntered"/>.</summary>
    public const int NotificationFocusEnter = 43;
    /// <summary>This control lost keyboard focus, before <see cref="FocusExited"/>.</summary>
    public const int NotificationFocusExit = 44;
    /// <summary>This control became the directly hovered control.</summary>
    public const int NotificationMouseEnterSelf = 60;
    /// <summary>This control stopped being the directly hovered control.</summary>
    public const int NotificationMouseExitSelf = 61;

    private MouseFilter _mouseFilter;
    private FocusMode _focusMode;
    private ControlMouseBehaviorRecursive _mouseBehaviorRecursive;
    private ControlFocusBehaviorRecursive _focusBehaviorRecursive;
    private bool _mouseForcePassScrollEvents = true;
    private CursorShape _mouseDefaultCursorShape;

    /// <summary>Receives a temporary control-local input event after <see cref="OnGUIInput"/>.</summary>
    /// <remarks>The event is borrowed for the duration of the callback. Pointer input targets one hit control,
    /// then may bubble to parent controls; keyboard input targets only the focused control.</remarks>
    public event Action<InputEvent>? GUIInput;

    /// <summary>Occurs after <see cref="NotificationFocusEnter"/> when this control becomes the keyboard input target.</summary>
    public event Action? FocusEntered;

    /// <summary>Occurs after <see cref="NotificationFocusExit"/> when this control loses keyboard focus.</summary>
    public event Action? FocusExited;

    /// <summary>Occurs when the pointer enters this control or its reachable child area.</summary>
    /// <remarks>Raised after <see cref="NotificationMouseEnter"/> on the scene owner thread. Ancestors enter before descendants.</remarks>
    public event Action? MouseEntered;

    /// <summary>Occurs when the pointer leaves this control and its reachable child area.</summary>
    /// <remarks>Raised after <see cref="NotificationMouseExit"/> on the scene owner thread. Descendants exit before ancestors.</remarks>
    public event Action? MouseExited;

    /// <summary>Gets or sets how pointer input reaches this control.</summary>
    /// <value><see cref="MouseFilter.Stop"/> by default.</value>
    public MouseFilter MouseFilter
    {
        get { ThrowIfDisposed(); return _mouseFilter; }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_mouseFilter == value) return;
            _mouseFilter = value;
            Tree?.RefreshGUIHover();
        }
    }

    /// <summary>Gets or sets the inherited pointer input policy for this control and its descendants.</summary>
    /// <value><see cref="ControlMouseBehaviorRecursive.Inherited"/> by default. An enabled descendant overrides a disabled ancestor.</value>
    public ControlMouseBehaviorRecursive MouseBehaviorRecursive
    {
        get { ThrowIfDisposed(); return _mouseBehaviorRecursive; }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_mouseBehaviorRecursive == value) return;
            _mouseBehaviorRecursive = value;
            Tree?.RefreshGUIHover();
        }
    }

    /// <summary>Gets or sets the cursor shape used when this control is hovered.</summary>
    /// <value>Arrow by default. A change on a hovered control refreshes the native cursor immediately.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined cursor shape.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    public CursorShape MouseDefaultCursorShape
    {
        get { ThrowIfDisposed(); return _mouseDefaultCursorShape; }
        set
        {
            EnsureMutable();
            if ((uint)value > (uint)CursorShape.Help) throw new ArgumentOutOfRangeException(nameof(value));
            if (_mouseDefaultCursorShape == value) return;
            _mouseDefaultCursorShape = value;
            Tree?.RefreshGUICursor(this);
        }
    }

    /// <summary>Lets wheel input continue to the parent even when <see cref="MouseFilter"/> is Stop.</summary>
    /// <value>True by default.</value>
    public bool MouseForcePassScrollEvents
    {
        get { ThrowIfDisposed(); return _mouseForcePassScrollEvents; }
        set { EnsureMutable(); _mouseForcePassScrollEvents = value; }
    }

    /// <summary>Gets or sets whether this control can become the keyboard input target.</summary>
    /// <value><see cref="FocusMode.None"/> by default.</value>
    public FocusMode FocusMode
    {
        get { ThrowIfDisposed(); return _focusMode; }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _focusMode = value;
            if (value == FocusMode.None) Tree?.ReleaseGUIFocus(this);
        }
    }

    /// <summary>Gets or sets the inherited keyboard focus policy for this control and its descendants.</summary>
    /// <value><see cref="ControlFocusBehaviorRecursive.Inherited"/> by default. An enabled descendant overrides a disabled ancestor.</value>
    public ControlFocusBehaviorRecursive FocusBehaviorRecursive
    {
        get { ThrowIfDisposed(); return _focusBehaviorRecursive; }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_focusBehaviorRecursive == value) return;
            _focusBehaviorRecursive = value;
            Tree?.RefreshGUIFocus();
        }
    }

    internal MouseFilter EffectiveMouseFilter =>
        IsMouseBehaviorEnabled() ? _mouseFilter : MouseFilter.Ignore;

    internal FocusMode EffectiveFocusMode =>
        IsFocusBehaviorEnabled() ? _focusMode : FocusMode.None;

    /// <summary>Returns the pointer filter after applying the inherited recursive policy.</summary>
    /// <returns><see cref="MouseFilter.Ignore"/> when this control's pointer behavior is disabled; otherwise <see cref="MouseFilter"/>.</returns>
    public MouseFilter GetMouseFilterWithOverride() { ThrowIfDisposed(); return EffectiveMouseFilter; }

    /// <summary>Returns the focus mode after applying the inherited recursive policy.</summary>
    /// <returns><see cref="FocusMode.None"/> when this control's focus behavior is disabled; otherwise <see cref="FocusMode"/>.</returns>
    public FocusMode GetFocusModeWithOverride() { ThrowIfDisposed(); return EffectiveFocusMode; }

    private bool IsMouseBehaviorEnabled() => _mouseBehaviorRecursive switch
    {
        ControlMouseBehaviorRecursive.Enabled => true,
        ControlMouseBehaviorRecursive.Disabled => false,
        _ => Parent is not Control parent || parent.IsMouseBehaviorEnabled()
    };

    private bool IsFocusBehaviorEnabled() => _focusBehaviorRecursive switch
    {
        ControlFocusBehaviorRecursive.Enabled => true,
        ControlFocusBehaviorRecursive.Disabled => false,
        _ => Parent is not Control parent || parent.IsFocusBehaviorEnabled()
    };

    /// <summary>Marks the current GUI event handled, preventing further delivery.</summary>
    /// <exception cref="InvalidOperationException">No scene input is currently being delivered.</exception>
    public void AcceptEvent()
    {
        ThrowIfDisposed();
        (Tree ?? throw new InvalidOperationException("A control must belong to a scene tree to accept input.")).SetInputAsHandled();
    }

    /// <summary>Requests keyboard focus for this visible control in a root viewport.</summary>
    /// <param name="hideFocus">Whether <see cref="HasFocus"/> should ignore this focus when requested.</param>
    public void GrabFocus(bool hideFocus = false)
    {
        ThrowIfDisposed();
        var tree = Tree ?? throw new InvalidOperationException("Focus requires an active scene tree.");
        tree.SetGUIFocus(this, hideFocus);
    }

    /// <summary>Returns whether this control has keyboard focus.</summary>
    /// <param name="ignoreHiddenFocus">Treat focus hidden by a pointer request as absent.</param>
    /// <returns>True if this is the current keyboard input target.</returns>
    public bool HasFocus(bool ignoreHiddenFocus = false)
    {
        ThrowIfDisposed();
        return Tree?.HasGUIFocus(this, ignoreHiddenFocus) ?? false;
    }

    /// <summary>Releases keyboard focus if held by this control.</summary>
    public void ReleaseFocus() { ThrowIfDisposed(); Tree?.ReleaseGUIFocus(this); }

    /// <summary>Tests a point in this control's local rectangle. Override for a custom hit shape.</summary>
    /// <param name="point">Point in control-local coordinates.</param>
    /// <returns>True for points inside the half-open rectangle from zero to <see cref="Size"/>.</returns>
    protected virtual bool HasPoint(Vector2 point) => point.X >= 0 && point.Y >= 0 && point.X < Size.X && point.Y < Size.Y;

    /// <summary>Returns the cursor shape for a position in this control's local coordinates.</summary>
    /// <param name="atPosition">Finite local coordinates; the point need not be inside the rectangle.</param>
    /// <returns>The virtual override's shape, or <see cref="MouseDefaultCursorShape"/>.</returns>
    /// <exception cref="ArgumentException">The local position is not finite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The virtual override returns an undefined shape.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    public CursorShape GetCursorShape(Vector2 atPosition = default)
    {
        ThrowIfDisposed();
        if (!atPosition.IsFinite()) throw new ArgumentException("Cursor position must be finite.", nameof(atPosition));
        var shape = OnGetCursorShape(atPosition);
        if ((uint)shape > (uint)CursorShape.Help) throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown cursor shape.");
        return shape;
    }

    /// <summary>Chooses a cursor for a control-local position; defaults to <see cref="MouseDefaultCursorShape"/>.</summary>
    /// <param name="atPosition">Finite coordinates local to this control.</param>
    /// <returns>The shape selected for the given position.</returns>
    protected virtual CursorShape OnGetCursorShape(Vector2 atPosition) => _mouseDefaultCursorShape;

    /// <summary>Processes a temporary control-local event before <see cref="GUIInput"/> subscribers.</summary>
    /// <param name="inputEvent">Borrowed event valid only during synchronous dispatch.</param>
    protected virtual void OnGUIInput(InputEvent inputEvent) { }

    internal bool HitTest(Vector2 viewportPoint) => HasPoint(MakeCanvasPositionLocal(viewportPoint));
    internal void DispatchGUIInput(InputEvent inputEvent)
    {
        OnGUIInput(inputEvent);
        GUIInput?.Invoke(inputEvent);
    }
    internal void NotifyFocusEntered() => NotifyFocusChange(NotificationFocusEnter);
    internal void NotifyFocusExited() => NotifyFocusChange(NotificationFocusExit);
    internal void NotifyMouseEntered() => MouseEntered?.Invoke();
    internal void NotifyMouseExited() => MouseExited?.Invoke();

    private void NotifyFocusChange(int notification)
    {
        List<Exception>? errors = null;
        try { DispatchNotification(notification); } catch (Exception error) { CollectException(ref errors, error); }
        try
        {
            if (notification == NotificationFocusEnter)
            {
                if (Tree?.HasGUIFocus(this, ignoreHiddenFocus: false) == true) FocusEntered?.Invoke();
            }
            else FocusExited?.Invoke();
        }
        catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) QueueRedraw(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Control focus callbacks failed.", errors);
    }
}
