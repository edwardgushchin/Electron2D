namespace Electron2D;

/// <summary>A configurable native root window that owns scene children.</summary>
/// <remarks>Pass a detached window to <see cref="Engine.Run(Window)"/> or <see cref="Engine.RunAsync(Window)"/>. The runtime opens its native window before
/// scene entry and releases it after scene teardown. One native root window is supported. The client size uses pixels
/// on Wayland, Android, iOS, tvOS and browsers, and native window units elsewhere. The root canvas renders after scene processing;
/// embedded child windows use a containing viewport configured with GUIEmbedSubwindows.</remarks>
public partial class Window : Viewport
{
    /// <summary>Notifies this window immediately before its visibility-change event.</summary>
    public const int NotificationVisibilityChanged = 30;

    private static readonly PropertyDescriptor[] WindowProperties =
    [
        new PropertyDescriptor<Window, bool>(nameof(Visible), w => w.Visible, (w, v) => w.Visible = v, _ => true, stored: true),
        new PropertyDescriptor<Window, Vector2i>(nameof(Position), w => w.Position, (w, v) => w.Position = v, _ => Vector2i.Zero, stored: true),
        new PropertyDescriptor<Window, WindowInitialPosition>(nameof(InitialPosition), w => w.InitialPosition, (w, v) => w.InitialPosition = v, _ => WindowInitialPosition.Absolute, stored: true),
        new PropertyDescriptor<Window, string>(nameof(Title), w => w.Title, (w, v) => w.Title = v, _ => "", stored: true),
        new PropertyDescriptor<Window, Vector2i>(nameof(Size), w => w.Size, (w, v) => w.Size = v, _ => new(100, 100), stored: true),
        new PropertyDescriptor<Window, Vector2i>(nameof(MinSize), w => w.MinSize, (w, v) => w.MinSize = v, _ => Vector2i.Zero, stored: true),
        new PropertyDescriptor<Window, Vector2i>(nameof(MaxSize), w => w.MaxSize, (w, v) => w.MaxSize = v, _ => Vector2i.Zero, stored: true),
        new PropertyDescriptor<Window, WindowMode>(nameof(Mode), w => w.Mode, (w, v) => w.Mode = v, _ => WindowMode.Windowed, stored: true),
        new PropertyDescriptor<Window, bool>(nameof(Unresizable), w => w.Unresizable, (w, v) => w.Unresizable = v, _ => false, stored: true),
        new PropertyDescriptor<Window, bool>(nameof(Borderless), w => w.Borderless, (w, v) => w.Borderless = v, _ => false, stored: true),
        new PropertyDescriptor<Window, bool>(nameof(AlwaysOnTop), w => w.AlwaysOnTop, (w, v) => w.AlwaysOnTop = v, _ => false, stored: true),
        new PropertyDescriptor<Window, bool>(nameof(Unfocusable), w => w.Unfocusable, (w, v) => w.Unfocusable = v, _ => false, stored: true),
    ];

    private bool _visible = true;
    private DisplayServer? _display;
    private RenderingServer? _renderer;
    private string _title = "";
    private Vector2i _size = new(100, 100);
    private Vector2i _minSize;
    private Vector2i _maxSize;
    private Vector2i? _screenPosition;
    internal bool HasAuthoredPosition => _screenPosition is not null;
    private bool _mouseInWindow, _mouseInContent;

    /// <summary>Creates a detached visible window with an empty title and a 100 by 100 client area.</summary>
    /// <remarks>Initial pixel-snapping choices read active project settings. No native resources are acquired until <see cref="Engine.Run(Window)"/>.</remarks>
    public Window()
    {
        SnapTransformsToPixel = ProjectSettings.GetWithOverride(ProjectSettings.SnapTransformsToPixel);
        SnapVerticesToPixel = ProjectSettings.GetWithOverride(ProjectSettings.SnapVerticesToPixel);
    }

    /// <summary>Gets or sets the native window title.</summary>
    /// <remarks>A changed title requests configuration-warning refresh after the native and managed values commit.
    /// Undecorated native popups retain logical title metadata because their native role has no title bar.</remarks>
    /// <exception cref="Exception">A configuration-warning subscriber fails after title assignment.</exception>
    /// <value>An empty string by default.</value>
    /// <exception cref="ArgumentNullException">The new title is null.</exception>
    /// <exception cref="ArgumentException">The title contains a null character.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public string Title
    {
        get { ThrowIfDisposed(); return _display?.WindowGetTitle() ?? _title; }
        set
        {
            EnsureMutable();
            ArgumentNullException.ThrowIfNull(value);
            if (value.Contains('\0'))
                throw new ArgumentException("A window title cannot contain a null character.", nameof(value));
            if (Title == value)
                return;
            _display?.WindowSetTitleCore(value);
            _title = value;
            UpdateEmbeddedContents();
            QueueEmbeddedRedraw();
            UpdateConfigurationWarnings();
            BeginEventEmission(TitleChanged)?.Invoke();
        }
    }

    /// <summary>Gets the observed client size or requests a positive client size.</summary>
    /// <value>100 by 100 before configuration or native activation.</value>
    /// <remarks>Native changes may be asynchronous or constrained by the compositor and size limits.
    /// SizeChanged follows committed size changes; desktop position and child canvas transforms do not affect size.
    /// On Android, iOS, tvOS and browsers the native surface determines the initial observed size,
    /// regardless of the requested size. These profiles and Wayland expose physical client pixels.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Either component is nonpositive.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2i Size
    {
        get { ThrowIfDisposed(); return _size; }
        set
        {
            EnsureMutable();
            if (value.X <= 0 || value.Y <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Client dimensions must be positive.");
            RenderingOwner?.EnsureViewportMutation();
            value = value.Max(GetWindowMinimumSize());
            EnsureMutable();
            if (_maxSize.X > 0) value.X = Math.Min(value.X, _maxSize.X);
            if (_maxSize.Y > 0) value.Y = Math.Min(value.Y, _maxSize.Y);
            RequestMeasuredWindowSize(value);
        }
    }

    /// <summary>Gets or sets nonnegative minimum client dimensions; zero means no limit on that axis.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is negative or exceeds a nonzero maximum.</exception>
    /// <exception cref="NotSupportedException">A nonzero Android, iOS, tvOS or browser limit is configured at native startup.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2i MinSize
    {
        get { ThrowIfDisposed(); return _display?.WindowGetMinSizeCore() ?? _minSize; }
        set { EnsureMutable(); ValidateLimits(value, MaxSize); _display?.WindowSetMinSizeCore(value); _minSize = value; if (_keepTitleVisible) UpdateEmbeddedContents(); }
    }

    /// <summary>Gets or sets nonnegative maximum client dimensions; zero means no limit on that axis.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is negative or a nonzero maximum is below the minimum.</exception>
    /// <exception cref="NotSupportedException">A nonzero Android, iOS, tvOS or browser limit is configured at native startup.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2i MaxSize
    {
        get { ThrowIfDisposed(); return _display?.WindowGetMaxSizeCore() ?? _maxSize; }
        set { EnsureMutable(); ValidateLimits(MinSize, value); _display?.WindowSetMaxSizeCore(value); _maxSize = value; if (_keepTitleVisible) UpdateEmbeddedContents(); }
    }

    /// <summary>Gets or requests the client origin in native desktop or embedded-host coordinates.</summary>
    /// <value>The configured position before startup, or zero if no position was requested.</value>
    /// <remarks>Leaving it unset lets the system place
    /// the window. PackedScene preserves this unset policy; an explicit zero position remains an authored request.
    /// A preconfigured native root position is applied at startup and can fail on an unsupported platform. Embedded children use their containing viewport coordinates.</remarks>
    /// <exception cref="NotSupportedException">The active compositor does not expose or accept global window positions, including Wayland.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2i Position
    {
        get { ThrowIfDisposed(); return _display?.WindowGetPositionCore() ?? _screenPosition ?? Vector2i.Zero; }
        set { EnsureMutable(); RenderingOwner?.EnsureViewportMutation(); value = ClampEmbeddedPosition(value); _display?.WindowSetPositionCore(value); _screenPosition = value; _popupClientPosition = null; _popupPlacementRoot = null; _embeddedCanvas?.QueueRedraw(); }
    }

    /// <summary>Gets or sets native or embedded visibility.</summary>
    /// <value>True by default. A native failure leaves managed visibility unchanged.</value>
    /// <remarks>A native child destroys its host on hide and allocates a new identity on show, preserving the viewport texture.
    /// Hiding or retiring a native parent first retires its popup descendants, preserving independent native children.
    /// Popup observer failures are aggregated after required retirement and visibility changes.
    /// Transparent native children require the compatibility renderer. GPU work drains and root swapchain storage releases before hiding. Showing reclaims
    /// presentation storage; independent offscreen targets remain live. The current Wayland Vulkan
    /// profile rejects remapping a previously presented surface until native unmap acknowledgement is available.</remarks>
    /// <exception cref="NotSupportedException">The active profile cannot safely remap a previously presented window or present a requested transparent child.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    /// <exception cref="AggregateException">Visibility callbacks fail after the new visibility commits.</exception>
    public bool Visible
    {
        get { ThrowIfDisposed(); return _visible; }
        set
        {
            EnsureMutable();
            if (TryNativeVisibility(value)) return;
            if (_visible == value) return;
            List<Exception>? errors = null;
            if (IsInsideTree && Parent is not null && Embedder is null)
            {
                if (value) OpenNativeChild();
                else try { CloseNative(); } catch (Exception error) when (_display is null) { CollectException(ref errors, error); }
            }
            if (_display is { } display)
            {
                if (!value && display.RetireNativePopupChildren() is { } failure) CollectException(ref errors, failure);
                if (_renderer is { } renderer) renderer.SetWindowVisible(display, value);
                else display.SetWindowVisible(value);
            }
            _visible = value;
            if (value) Tree?.RegisterNativePopup(this);
            try { UpdateExclusiveSelection(); } catch (Exception error) { CollectException(ref errors, error); }
            try { EmbeddedVisibilityChanged(); } catch (Exception error) { CollectException(ref errors, error); }
            try { Notify(NotificationVisibilityChanged); } catch (Exception error) { CollectException(ref errors, error); }
            if (IsDisposed) { ThrowCollected("Window visibility callbacks failed.", errors); return; }
            try { BeginEventEmission(VisibilityChanged)?.Invoke(); }
            catch (Exception error) { CollectException(ref errors, error); }
            foreach (var node in EnumerateDepthFirst())
            {
                if (node is not CanvasItem item || item.IsDisposed || item.Parent is CanvasItem) continue;
                try { item.PropagateVisibilityChanged(); }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            try { AfterVisibilityChanged(value); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Window visibility callbacks failed.", errors);
        }
    }

    /// <summary>Shows this window, acquiring no native resources before Engine.Run.</summary>
    /// <exception cref="NotSupportedException">The active Wayland Vulkan profile cannot safely remap a previously presented surface.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    /// <exception cref="AggregateException">Visibility callbacks fail after the new visibility commits.</exception>
    public void Show() => Visible = true;

    /// <summary>Hides this window without disposing it or its scene.</summary>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    /// <exception cref="AggregateException">Visibility callbacks fail after the new visibility commits.</exception>
    public void Hide() => Visible = false;

    /// <summary>Occurs synchronously after this window's visibility changes.</summary>
    /// <remarks>Runs on the owner thread before canvas visibility propagation. Callback failures are aggregated after canvas roots are attempted.</remarks>
    public event Action? VisibilityChanged;

    /// <summary>Occurs when the native system or embedded host requests window closure.</summary>
    /// <remarks>Handlers may disable SceneTree.AutoAcceptQuit to keep running, or call SceneTree.Quit with an exit code.
    /// Containing-window notifications precede all signal delegates; failures are aggregated after remaining receivers.
    /// The root default quit decision follows delivery even on failure; a child request does not imply application quit. Handler failures terminate Engine.Run with cleanup.</remarks>
    public event Action? CloseRequested;

    /// <summary>Occurs when the Android system requests back navigation for this window.</summary>
    /// <remarks>Containing-window notifications run first, stopping at nested windows. For the root window,
    /// SceneTree.QuitOnGoBack is evaluated next, before public subscribers. Notifications and later subscribers
    /// are attempted despite earlier callback errors. Blocking this window's signals suppresses subscribers and
    /// automatic quit, while notifications still run. Manual node notification does not emit this event.</remarks>
    public event Action? GoBackRequested;

    /// <summary>Occurs after Title commits a different value.</summary>
    public event Action? TitleChanged;

    /// <summary>Occurs when the window gains keyboard focus, after containing-window notifications.</summary>
    public event Action? FocusEntered;

    /// <summary>Occurs when the window loses keyboard focus, after containing-window notifications and before pressed input is released.</summary>
    public event Action? FocusExited;

    /// <summary>Gets the native window identity while running.</summary>
    /// <returns>Zero for the active native root, a positive identity for an active native child, and minus one while detached, embedded or a native child is hidden.</returns>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public int GetWindowID() { ThrowIfDisposed(); return _display?.WindowID ?? DisplayServer.InvalidWindowId; }

    /// <summary>Reports whether the active native window has keyboard focus.</summary>
    /// <returns>The platform's current focus observation.</returns>
    /// <exception cref="InvalidOperationException">The window is inactive or accessed off-thread.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public bool HasFocus() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return Embedder is not null ? Tree!.HasEmbeddedWindowFocus(this) : GetDisplay().WindowIsFocusedCore(); }

    /// <summary>Requests keyboard focus and foreground placement from the native system.</summary>
    /// <remarks>The operating system may deny focus. Wayland submits no foreground activation request through this
    /// operation. Inspect HasFocus for the observed result.</remarks>
    /// <exception cref="InvalidOperationException">The window is inactive, accessed off-thread, or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void GrabFocus() { EnsureMutable(); if (Embedder is not null) Tree!.FocusEmbeddedWindow(this); else GetDisplay().WindowMoveToForegroundCore(); }

    /// <summary>Requests a platform attention indication until this window is focused.</summary>
    /// <exception cref="InvalidOperationException">The window is inactive, accessed off-thread, or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void RequestAttention() => GetDisplay().WindowRequestAttentionCore();

    /// <inheritdoc />
    public override Rect2 GetVisibleRect()
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingReadAccess(); return new Rect2(Vector2.Zero, ContentLayout.VisibleSize);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(WindowProperties).Concat(InputProperties).Concat(ContentProperties).Concat(PopupProperties).Concat(ThemeProperties).Concat(ThemeOwner.Properties<Window>());

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Window)
        ? CreateDefaultWindow : base.CreateSceneInstanceFactory();

    private static Node CreateDefaultWindow() => new Window();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ReferenceEquals(_focusedWindow, this)) _focusedWindow = null;
            _themeOwner?.Dispose(); ThemeChanged = null;
            AboutToPopup = null;
            CloseRequested = null;
            GoBackRequested = null;
            VisibilityChanged = null;
            WindowInput = null;
            NonclientWindowInput = null;
            TitleChanged = null;
            FocusEntered = null;
            FocusExited = null;
            MouseEntered = null;
            MouseExited = null;
            DpiChanged = null;
            FilesDropped = null;
        }
        base.Dispose(disposing);
    }

    internal DisplayServer? NativeDisplay => _display;

    internal void OpenNative(bool disableVSync = false)
    {
        _display = DisplayServer.OpenForRendering(_title, _size, hidden: !Visible, transparent: Transparent);
        _display.SceneWindow = this;
        ConfigureNative();
        if (disableVSync) DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled);
        _renderer = RenderingServer.Open(this, _display.AcquireRenderingWindow());
        _display.SetGraphicsHandleQuery(_renderer.GetNativeHandle);
    }

    private void ConfigureNative()
    {
        if (_keepTitleVisible) throw new NotSupportedException("Native title measurement is unavailable.");
        var display = _display ?? throw new InvalidOperationException("The native window is absent.");
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsBrowser())
        {
            if (_minSize.X != 0 || _minSize.Y != 0 || _maxSize.X != 0 || _maxSize.Y != 0)
                throw new NotSupportedException("Mobile, TV and browser surfaces do not support window size limits.");
        }
        else
        {
            display.WindowSetMinSizeCore(_minSize);
            display.WindowSetMaxSizeCore(_maxSize);
            display.WindowSetSizeCore(_size);
        }
        foreach (var flag in new[] { WindowFlag.ResizeDisabled, WindowFlag.Borderless, WindowFlag.AlwaysOnTop, WindowFlag.NoFocus, WindowFlag.Popup, WindowFlag.Transparent, WindowFlag.PopupWmHint, WindowFlag.MinimizeDisabled, WindowFlag.MaximizeDisabled })
            if (GetFlag(flag))
                display.WindowSetFlagCore(flag, true);
        if (_currentScreen is { } screen)
            display.WindowSetCurrentScreenCore(screen);
        if (!display.IsNativePopup || _popupClientPosition is null) ApplyInitialNativePosition();
        if (_mode != WindowMode.Windowed)
            display.WindowSetModeCore(_mode);
        _size = display.WindowGetSizeCore(); _contentLayoutValid = false;
        display.CloseRequestedCore += HandleClose;
        display.QuitRequestedCore += HandleClose;
        display.GoBackRequestedCore += HandleGoBack;
        display.WindowRectChangedCore += HandleRect;
        display.WindowFocusChangedCore += HandleFocus;
        display.WindowMouseEnteredCore += HandleMouseEntered;
        display.WindowMouseExitedCore += HandleMouseExited;
        display.WindowDpiChangedCore += HandleDPIChanged;
        display.TextInputCore += HandleTextInput;
        display.TextEditingCore += HandleTextEditing;
        display.FilesDroppedCore += HandleFilesDropped;
    }

    internal Vector2 GetClientMousePosition()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        if (Embedder is { } embedder) return (embedder.GetWindow() ?? throw new InvalidOperationException("Pointer queries require an active containing native Window.")).GetClientMousePosition();
        EnsureNativeOpen(); return _display!.GetClientMousePosition();
    }
    internal void WarpClientMouse(Vector2i position)
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        if (Embedder is { } embedder) { (embedder.GetWindow() ?? throw new InvalidOperationException("Pointer warping requires an active containing native Window.")).WarpClientMouse(position); return; }
        EnsureNativeOpen(); _display!.WarpClientMouse(position);
    }

    internal void EnsureNativeOpen()
    {
        if (_display is null)
            throw new InvalidOperationException("Activate a root Window through Engine.Run.");
    }

    internal void CloseNative()
    {
        if (_display is not { } display)
            return;
        _renderer?.EnsureNativeWindowMutation();
        List<Exception>? errors = null;
        if (display.PrepareNativeWindowRetirement() is { } retirementFailure) CollectException(ref errors, retirementFailure);
        if (display.IsNativePopup) { _popupClientPosition = display.PopupClientOrigin; _popupPlacementRoot = display.NativePopupRoot.SceneWindow; }
        Tree?.UnregisterNativePopup(this);
        if (ReferenceEquals(_focusedWindow, this)) _focusedWindow = null;
        try
        {
            if (display.WindowID == DisplayServer.MainWindowId) _renderer?.Close();
            else
            {
                try { _mode = display.WindowGetModeCore(); _size = display.WindowGetSizeCore(); }
                catch (Exception error) { CollectException(ref errors, error); }
                _renderer?.UnregisterNativeWindow(display);
            }
        }
        catch (Exception error) { CollectException(ref errors, error); }
        finally { _renderer = null; display.ReleaseRenderingWindow(); }
        display.CloseRequestedCore -= HandleClose;
        display.QuitRequestedCore -= HandleClose;
        display.GoBackRequestedCore -= HandleGoBack;
        display.WindowRectChangedCore -= HandleRect;
        display.WindowFocusChangedCore -= HandleFocus;
        display.WindowMouseEnteredCore -= HandleMouseEntered;
        display.WindowMouseExitedCore -= HandleMouseExited;
        display.WindowDpiChangedCore -= HandleDPIChanged;
        display.TextInputCore -= HandleTextInput;
        display.TextEditingCore -= HandleTextEditing;
        display.FilesDroppedCore -= HandleFilesDropped;
        try { display.Dispose(); }
        catch (Exception error) { CollectException(ref errors, error); }
        finally { _display = null; _contentLayoutValid = false; }
        ThrowCollected("Native window retirement failed.", errors);
    }

    internal void PumpEvents() => GetDisplay().ProcessEventsCore();

    internal void Render(SceneTree tree, double step) => tree.RenderCanvas(_renderer ?? throw new InvalidOperationException("Rendering has not started."), step);

    private DisplayServer GetDisplay()
    {
        ThrowIfDisposed();
        return _display ?? throw new InvalidOperationException("The window is not active.");
    }

    private void HandleGoBack() => EmitWindowEvent(NotificationWmGoBackRequest);

    private void HandleClose()
    {
        try { RequestEmbeddedClose(); }
        finally
        {
            if (Parent is null) Tree?.AcceptWindowClose();
        }
    }

    private void HandleRect(Rect2i rect)
    {
        List<Exception>? errors = null;
        if ((_screenPosition ?? Vector2i.Zero) != rect.Position)
        {
            _screenPosition = rect.Position;
            try { PropagateWindowNotification(NotificationWmPositionChanged); } catch (Exception error) { CollectException(ref errors, error); }
        }
        try { CommitSize(rect.Size); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Window rectangle callbacks failed.", errors);
    }
    private void HandleFocus(bool focused)
    {
        if (focused) _focusedWindow = this;
        else if (ReferenceEquals(_focusedWindow, this)) _focusedWindow = null;
        EmitWindowEvent(focused ? NotificationWmWindowFocusIn : NotificationWmWindowFocusOut);
    }
    private void HandleMouseEntered()
    {
        List<Exception>? errors = null;
        try { EnterWindowMouse(); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed && Visible)
        {
            try { Tree?.EnterGUIViewport(this); } catch (Exception error) { CollectException(ref errors, error); }
            try { NotifyMouseEntered(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Window pointer-enter callbacks failed.", errors);
    }

    private void HandleMouseExited() => ExitWindowMouse();
    internal void EnterWindowMouse()
    {
        EnsureMutable(); if (_mouseInWindow) return;
        _mouseInWindow = true;
        PropagateWindowNotification(NotificationWmMouseEnter);
    }
    internal void ExitWindowMouse()
    {
        EnsureMutable(); if (!_mouseInWindow) return;
        _mouseInWindow = false;
        List<Exception>? errors = null;
        try { NotifyMouseExited(); } catch (Exception error) { CollectException(ref errors, error); }
        try { PropagateWindowNotification(NotificationWmMouseExit); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Window pointer-exit callbacks failed.", errors);
    }
    private void HandleDPIChanged() => EmitWindowEvent(NotificationWmDpiChange);

    private void EmitWindowEvent(int what)
    {
        EnsureMutable();
        List<Exception>? errors = null;
        if (what == NotificationWmWindowFocusOut) try { Tree?.CancelEmbeddedDecorationTree(this); } catch (Exception error) { CollectException(ref errors, error); }
        try { PropagateWindowNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (what == NotificationWmGoBackRequest)
            try { if (!IsBlockingSignals() && Parent is null) Tree?.AcceptWindowGoBack(); }
            catch (Exception error) { CollectException(ref errors, error); }
        var handlers = what switch
        {
            NotificationWmWindowFocusIn => FocusEntered,
            NotificationWmWindowFocusOut => FocusExited,
            NotificationWmCloseRequest => CloseRequested,
            NotificationWmGoBackRequest => GoBackRequested,
            NotificationWmDpiChange => DpiChanged,
            _ => null
        };
        try { EmitWindowSignal(BeginEventEmission(handlers)); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Window event callbacks failed.", errors);
    }
    private static void EmitWindowSignal(Action? handlers)
    {
        List<Exception>? errors = null;
        if (handlers is not null)
            foreach (var handler in Delegate.EnumerateInvocationList(handlers))
                try { handler(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Window signal callbacks failed.", errors);
    }
    private void HandleTextInput(string text)
    { if (_display?.HasExclusiveNativeChild() != true) Tree?.DispatchCommittedText(Tree.GetEmbeddedTextWindow(this), text); }
    private void HandleTextEditing(string text, Vector2i selection)
    { if (text.Length == 0 || _display?.HasExclusiveNativeChild() != true) Tree?.DispatchIMEComposition(Tree.GetEmbeddedTextWindow(this), text, selection); }
    private void HandleFilesDropped(IReadOnlyList<string> paths) => BeginEventEmission(FilesDropped)?.Invoke(paths);

    private void CommitSize(Vector2i size)
    {
        if (_size == size)
            return;
        _size = size; _contentLayoutValid = false;
        InvalidateViewportRecording(); _embeddedCanvas?.QueueRedraw();
        NotifyContentSizeChanged();
    }

    private static void ValidateLimits(Vector2i minimum, Vector2i maximum)
    {
        if (minimum.X < 0 || minimum.Y < 0 || maximum.X < 0 || maximum.Y < 0 ||
            maximum.X != 0 && maximum.X < minimum.X || maximum.Y != 0 && maximum.Y < minimum.Y)
            throw new ArgumentOutOfRangeException(nameof(maximum), "Window limits must be nonnegative and ordered on each axis.");
    }
}
