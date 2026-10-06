namespace Electron2D;

public partial class Window
{
    private WindowMode _mode;
    private uint _flags;
    private int? _currentScreen;

    /// <summary>Gets the observed native mode, or configures a presentation-mode request.</summary>
    /// <value>Windowed by default; the configured request while detached.</value>
    /// <remarks>Native changes may be asynchronous or denied by the window manager. Wayland exclusive fullscreen
    /// becomes ordinary fullscreen; restoring from Wayland minimization is not guaranteed.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public WindowMode Mode
    {
        get { ThrowIfDisposed(); return _display is null ? _mode : _display.WindowGetModeCore(); }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown window mode.");
            _display?.WindowSetModeCore(value);
            _mode = value;
        }
    }

    /// <summary>Gets the observed display index, or requests placement on a zero-based display index.</summary>
    /// <value>The requested index while detached, or zero before configuration. No startup move occurs when unset.</value>
    /// <remarks>Not stored by PackedScene. Availability is checked at activation. Wayland accepts its current
    /// screen only; a request for another screen fails without moving the window.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is negative or unavailable on the active backend.</exception>
    /// <exception cref="NotSupportedException">The compositor does not allow moving to the requested screen.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="AggregateException">Moving fails and restoring the native state also fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public int CurrentScreen
    {
        get { ThrowIfDisposed(); return _display?.WindowGetCurrentScreenCore() ?? _currentScreen ?? 0; }
        set
        {
            EnsureMutable();
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _display?.WindowSetCurrentScreenCore(value);
            _currentScreen = value;
        }
    }

    /// <summary>Gets or sets the policy preventing user border resizing.</summary>
    /// <value>False by default. Programmatic Size requests remain allowed.</value>
    /// <remarks>Uses GetFlag and SetFlag, including their lifecycle and failure contract.</remarks>
    public bool Unresizable { get => GetFlag(WindowFlag.ResizeDisabled); set => SetFlag(WindowFlag.ResizeDisabled, value); }

    /// <summary>Gets or sets the policy removing native window borders and title bar.</summary>
    /// <value>False by default.</value>
    /// <remarks>Uses GetFlag and SetFlag, including their lifecycle and failure contract.</remarks>
    public bool Borderless { get => GetFlag(WindowFlag.Borderless); set => SetFlag(WindowFlag.Borderless, value); }

    /// <summary>Gets or sets the policy requesting placement above ordinary windows.</summary>
    /// <value>False by default.</value>
    /// <remarks>Uses GetFlag and SetFlag. Enabling it fails for an active Wayland top-level window or at startup
    /// when preconfigured; a rejected request leaves the policy unchanged.</remarks>
    public bool AlwaysOnTop { get => GetFlag(WindowFlag.AlwaysOnTop); set => SetFlag(WindowFlag.AlwaysOnTop, value); }

    /// <summary>Gets or sets the policy preventing keyboard focus.</summary>
    /// <value>False by default.</value>
    /// <remarks>Uses GetFlag and SetFlag. Enabling it fails for an active Wayland top-level window or at startup
    /// when preconfigured; a rejected request leaves the policy unchanged.</remarks>
    public bool Unfocusable { get => GetFlag(WindowFlag.NoFocus); set => SetFlag(WindowFlag.NoFocus, value); }

    /// <summary>Gets a configured window policy.</summary>
    /// <param name="flag">One individual policy identifier.</param>
    /// <returns>The last accepted request, initially false; native window-manager overrides do not change it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The identifier is undefined or Max.</exception>
    /// <exception cref="NotSupportedException">The defined policy has no executable integration.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public bool GetFlag(WindowFlag flag)
    {
        ThrowIfDisposed();
        ValidateFlag(flag);
        return (_flags & (1u << (int)flag)) != 0;
    }

    /// <summary>Configures an executable policy and requests its native application when active.</summary>
    /// <param name="flag">One individual policy identifier.</param>
    /// <param name="enabled">The desired state.</param>
    /// <remarks>An unchanged request does nothing. Failed native requests do not commit the configured policy.
    /// Detached configuration is checked against platform capabilities during Engine.Run startup.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The identifier is undefined or Max.</exception>
    /// <exception cref="NotSupportedException">The policy has no executable integration or the native platform rejects it.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void SetFlag(WindowFlag flag, bool enabled)
    {
        EnsureMutable();
        if (GetFlag(flag) == enabled)
            return;
        if (flag == WindowFlag.Popup && IsInsideTree && Visible) throw new InvalidOperationException("Popup policy cannot change while visible.");
        _display?.WindowSetFlagCore(flag, enabled);
        var bit = 1u << (int)flag;
        _flags = enabled ? _flags | bit : _flags & ~bit;
        if (flag == WindowFlag.Borderless && (_keepTitleVisible || ClampToEmbedder)) UpdateEmbeddedContents();
        QueueEmbeddedRedraw();
    }

    /// <summary>Reports whether the current resize policy permits native maximization.</summary>
    /// <returns>Whether resizing is allowed; the compositor may impose further restrictions.</returns>
    /// <exception cref="InvalidOperationException">An active window is accessed off-thread.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public bool IsMaximizeAllowed() { ThrowIfDisposed(); return _display?.WindowIsMaximizeAllowedCore() ?? (!Unresizable && !MaximizeDisabled); }

    /// <summary>Gets the outer window origin, including native borders when visible and active.</summary>
    /// <returns>Desktop coordinates; Position while hidden or detached.</returns>
    /// <exception cref="NotSupportedException">The active Wayland compositor does not disclose global positions.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or native geometry is unavailable.</exception>
    /// <exception cref="OverflowException">The outer position exceeds integer coordinates.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2i GetPositionWithDecorations() => _display is null || !Visible ? Position : GetDisplay().WindowGetPositionWithDecorationsCore();

    /// <summary>Gets the outer window size, including native borders when visible and active.</summary>
    /// <returns>Native window units; on Wayland, client pixels because decoration extents are unavailable.
    /// Hidden or detached windows return Size.</returns>
    /// <exception cref="InvalidOperationException">The caller is not the owner or native geometry is unavailable.</exception>
    /// <exception cref="OverflowException">The outer size exceeds integer dimensions.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2i GetSizeWithDecorations() => _display is null || !Visible ? Size : GetDisplay().WindowGetSizeWithDecorationsCore();

    /// <summary>Requests centering of the active client area in its current screen's usable rectangle.</summary>
    /// <remarks>Requires global positioning. Wayland rejects the request.</remarks>
    /// <exception cref="NotSupportedException">Global positioning is unavailable.</exception>
    /// <exception cref="InvalidOperationException">The window is inactive, accessed off-thread, or native geometry fails.</exception>
    /// <exception cref="OverflowException">The computed position exceeds integer coordinates.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void MoveToCenter()
    {
        EnsureMutable();
        var area = GetDisplay().ScreenGetUsableRectCore(CurrentScreen);
        var size = Size;
        Position = new Vector2i(checked((int)((long)area.Position.X + ((long)area.Size.X - size.X) / 2)),
            checked((int)((long)area.Position.Y + ((long)area.Size.Y - size.Y) / 2)));
    }

    /// <summary>Enables or disables native text input for the active window.</summary>
    /// <param name="active">Whether to accept committed text and composition updates.</param>
    /// <remarks>Enable while a text field owns focus. During Engine.Run, committed text and composition reach the
    /// focused Control through its typed hooks and events. Disabling clears native composition state; the text
    /// control owns its own preedit cleanup when focus or native window focus changes.</remarks>
    /// <exception cref="InvalidOperationException">The window is inactive, accessed off-thread, or the request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void SetIMEActive(bool active) { EnsureMutable(); if (Embedder is { } host) host.GetWindow()!.SetIMEActive(active); else GetDisplay().WindowSetIMEActiveCore(active); }

    /// <summary>Requests native IME candidate placement at a client-coordinate caret.</summary>
    /// <param name="position">Caret position in client pixels on Wayland and native window units elsewhere.</param>
    /// <remarks>The native candidate area is one by ten logical units with zero cursor offset. Text input must
    /// be active for a popup to appear; placement remains subject to input-method policy.</remarks>
    /// <exception cref="InvalidOperationException">The window is inactive, accessed off-thread, or the request fails.</exception>
    /// <exception cref="OverflowException">The position cannot be represented in native coordinates.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void SetIMEPosition(Vector2i position) { EnsureMutable(); if (Embedder is { } host) host.GetWindow()!.SetIMEPosition((Vector2i)(GetScreenTransform() * (Vector2)position)); else GetDisplay().WindowSetIMEPositionCore(position); }

    /// <summary>Requests a native taskbar progress indication for the active window.</summary>
    /// <param name="state">The progress indication to show.</param>
    /// <exception cref="ArgumentOutOfRangeException">The state is undefined.</exception>
    /// <exception cref="NotSupportedException">Taskbar integration is unavailable, including Wayland.</exception>
    /// <exception cref="InvalidOperationException">The window is inactive, accessed off-thread, or the request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void SetTaskbarProgressState(DisplayServer.ProgressState state) { EnsureMutable(); GetDisplay().WindowSetTaskbarProgressStateCore(state); }

    /// <summary>Requests a native taskbar progress fraction for the active window.</summary>
    /// <param name="value">A finite fraction from zero to one, inclusive.</param>
    /// <exception cref="ArgumentOutOfRangeException">The fraction is nonfinite or outside zero through one.</exception>
    /// <exception cref="NotSupportedException">Taskbar integration is unavailable, including Wayland.</exception>
    /// <exception cref="InvalidOperationException">The window is inactive, accessed off-thread, or the request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void SetTaskbarProgressValue(float value) { EnsureMutable(); GetDisplay().WindowSetTaskbarProgressValueCore(value); }

    /// <summary>Occurs on an effective native pointer entry before subsequent frame callbacks.</summary>
    /// <remarks>Delivered synchronously on the owner thread. Root delivery is native. Embedded pointer entry/exit signaling retains its separate coverage gap.</remarks>
    public event Action? MouseEntered;

    /// <summary>Occurs on an effective native pointer exit before subsequent frame callbacks.</summary>
    /// <remarks>Delivered synchronously on the owner thread. Root delivery is native. Embedded pointer entry/exit signaling retains its separate coverage gap.</remarks>
    public event Action? MouseExited;

    /// <summary>Occurs when the native window's display content scale changes.</summary>
    /// <remarks>Delivered in native order on the owner thread; this is not a physical-DPI measurement.</remarks>
    public event Action? DpiChanged;

    /// <summary>Occurs when a native file drop completes, with paths in arrival order.</summary>
    /// <remarks>The managed snapshot remains valid after delivery. Subscribers run on the owner thread before
    /// subsequent frame callbacks. Failing subscribers do not prevent later queued native events; Run reports
    /// callback failures and releases its resources after the queue drains.</remarks>
    public event Action<IReadOnlyList<string>>? FilesDropped;

    private void ValidateFlag(WindowFlag flag)
    {
        if (flag < WindowFlag.ResizeDisabled || flag >= WindowFlag.Max)
            throw new ArgumentOutOfRangeException(nameof(flag), flag, "Unknown window flag.");
        if (flag is WindowFlag.Transparent or WindowFlag.Popup or WindowFlag.PopupWmHint or WindowFlag.MinimizeDisabled or WindowFlag.MaximizeDisabled) return;
        if (flag is not (WindowFlag.ResizeDisabled or WindowFlag.Borderless or WindowFlag.AlwaysOnTop or WindowFlag.NoFocus))
            throw new NotSupportedException($"Window flag {flag} requires a native capability that is not integrated.");
    }
}
