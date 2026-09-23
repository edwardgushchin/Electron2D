namespace Electron2D;

/// <summary>Controls whether a rectangular control receives and consumes pointer input.</summary>
public enum ControlMouseFilter
{
    /// <summary>Receive pointer input and mark it handled.</summary>
    Stop = 0,
    /// <summary>Receive pointer input and pass an unhandled event to a parent control.</summary>
    Pass = 1,
    /// <summary>Do not receive pointer input or obstruct controls behind this one.</summary>
    Ignore = 2
}

/// <summary>Controls how a control can become the keyboard input target.</summary>
public enum ControlFocusMode
{
    /// <summary>Cannot receive focus.</summary>
    None = 0,
    /// <summary>Can receive focus by pointer press or an explicit request.</summary>
    Click = 1
}

public partial class Control
{
    private ControlMouseFilter _mouseFilter;
    private ControlFocusMode _focusMode;
    private bool _mouseForcePassScrollEvents = true;

    /// <summary>Receives a temporary control-local input event after <see cref="OnGUIInput"/>.</summary>
    /// <remarks>The event is borrowed for the duration of the callback. Pointer input targets one hit control,
    /// then may bubble to parent controls; keyboard input targets only the focused control.</remarks>
    public event Action<InputEvent>? GUIInput;

    /// <summary>Occurs after this control becomes the keyboard input target.</summary>
    public event Action? FocusEntered;

    /// <summary>Occurs after this control loses keyboard focus.</summary>
    public event Action? FocusExited;

    /// <summary>Gets or sets how pointer input reaches this control.</summary>
    /// <value><see cref="ControlMouseFilter.Stop"/> by default.</value>
    public ControlMouseFilter MouseFilter
    {
        get { ThrowIfDisposed(); return _mouseFilter; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _mouseFilter = value; }
    }

    /// <summary>Lets wheel input continue to the parent even when <see cref="MouseFilter"/> is Stop.</summary>
    /// <value>True by default.</value>
    public bool MouseForcePassScrollEvents
    {
        get { ThrowIfDisposed(); return _mouseForcePassScrollEvents; }
        set { EnsureMutable(); _mouseForcePassScrollEvents = value; }
    }

    /// <summary>Gets or sets whether this control can become the keyboard input target.</summary>
    /// <value><see cref="ControlFocusMode.None"/> by default.</value>
    public ControlFocusMode FocusMode
    {
        get { ThrowIfDisposed(); return _focusMode; }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _focusMode = value;
            if (value == ControlFocusMode.None) Tree?.ReleaseGUIFocus(this);
        }
    }

    /// <summary>Marks the current GUI event handled, preventing further delivery.</summary>
    /// <exception cref="InvalidOperationException">No scene input is currently being delivered.</exception>
    public void AcceptEvent()
    {
        ThrowIfDisposed();
        (Tree ?? throw new InvalidOperationException("A control must belong to a scene tree to accept input.")).SetInputAsHandled();
    }

    /// <summary>Requests keyboard focus for this visible, attached control.</summary>
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

    /// <summary>Processes a temporary control-local event before <see cref="GUIInput"/> subscribers.</summary>
    /// <param name="inputEvent">Borrowed event valid only during synchronous dispatch.</param>
    protected virtual void OnGUIInput(InputEvent inputEvent) { }

    internal bool HitTest(Vector2 viewportPoint) => HasPoint(MakeCanvasPositionLocal(viewportPoint));
    internal void DispatchGUIInput(InputEvent inputEvent)
    {
        OnGUIInput(inputEvent);
        GUIInput?.Invoke(inputEvent);
    }
    internal void NotifyFocusEntered() => FocusEntered?.Invoke();
    internal void NotifyFocusExited() => FocusExited?.Invoke();
}
