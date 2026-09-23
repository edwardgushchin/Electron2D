namespace Electron2D;

/// <summary>A configurable native root window that owns scene children.</summary>
/// <remarks>Pass a detached window to <see cref="Engine.Run"/>. The runtime opens its native window before
/// scene entry and releases it after scene teardown. One root window is supported. The client size uses pixels
/// on Wayland and native window units elsewhere. The root canvas renders after scene processing;
/// embedded windows are not implemented.</remarks>
public partial class Window : Viewport
{
    private static readonly PropertyDescriptor[] WindowProperties =
    [
        new PropertyDescriptor<Window, bool>(nameof(Visible), w => w.Visible, (w, v) => w.Visible = v, _ => true, stored: true),
        new PropertyDescriptor<Window, string>(nameof(Title), w => w.Title, (w, v) => w.Title = v, _ => "", stored: true),
        new PropertyDescriptor<Window, Vector2I>(nameof(Size), w => w.Size, (w, v) => w.Size = v, _ => new(100, 100), stored: true),
        new PropertyDescriptor<Window, Vector2I>(nameof(MinSize), w => w.MinSize, (w, v) => w.MinSize = v, _ => Vector2I.Zero, stored: true),
        new PropertyDescriptor<Window, Vector2I>(nameof(MaxSize), w => w.MaxSize, (w, v) => w.MaxSize = v, _ => Vector2I.Zero, stored: true),
        new PropertyDescriptor<Window, ModeEnum>(nameof(Mode), w => w.Mode, (w, v) => w.Mode = v, _ => ModeEnum.Windowed, stored: true),
        new PropertyDescriptor<Window, bool>(nameof(Unresizable), w => w.Unresizable, (w, v) => w.Unresizable = v, _ => false, stored: true),
        new PropertyDescriptor<Window, bool>(nameof(Borderless), w => w.Borderless, (w, v) => w.Borderless = v, _ => false, stored: true),
        new PropertyDescriptor<Window, bool>(nameof(AlwaysOnTop), w => w.AlwaysOnTop, (w, v) => w.AlwaysOnTop = v, _ => false, stored: true),
        new PropertyDescriptor<Window, bool>(nameof(Unfocusable), w => w.Unfocusable, (w, v) => w.Unfocusable = v, _ => false, stored: true),
    ];

    private bool _visible = true;
    private DisplayServer? _display;
    private RenderingServer? _renderer;
    private string _title = "";
    private Vector2I _size = new(100, 100);
    private Vector2I _minSize;
    private Vector2I _maxSize;
    private Vector2I? _screenPosition;

    /// <summary>Creates a detached visible window with an empty title and a 100 by 100 client area.</summary>
    /// <remarks>Initial pixel-snapping choices read active project settings. No native resources are acquired until <see cref="Engine.Run"/>.</remarks>
    public Window()
    {
        SnapTransformsToPixel = ProjectSettings.Instance.GetWithOverride(ProjectSettings.SnapTransformsToPixel);
        SnapVerticesToPixel = ProjectSettings.Instance.GetWithOverride(ProjectSettings.SnapVerticesToPixel);
    }

    /// <summary>Gets or sets the native window title.</summary>
    /// <remarks>A changed title requests configuration-warning refresh after the native and managed values commit.</remarks>
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
            _display?.WindowSetTitle(value);
            _title = value;
            UpdateConfigurationWarnings();
            TitleChanged?.Invoke();
        }
    }

    /// <summary>Gets the observed client size or requests a positive client size.</summary>
    /// <value>100 by 100 before configuration or native activation.</value>
    /// <remarks>Native changes may be asynchronous or constrained by the compositor and size limits.
    /// SizeChanged follows committed size changes; desktop position and child canvas transforms do not affect size.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Either component is nonpositive.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2I Size
    {
        get { ThrowIfDisposed(); return _size; }
        set
        {
            EnsureMutable();
            if (value.X <= 0 || value.Y <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Client dimensions must be positive.");
            if (_display is not null)
            {
                _display.WindowSetSize(value);
                return;
            }
            CommitSize(value);
        }
    }

    /// <summary>Gets or sets nonnegative minimum client dimensions; zero means no limit on that axis.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is negative or exceeds a nonzero maximum.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2I MinSize
    {
        get { ThrowIfDisposed(); return _display?.WindowGetMinSize() ?? _minSize; }
        set { EnsureMutable(); ValidateLimits(value, MaxSize); _display?.WindowSetMinSize(value); _minSize = value; }
    }

    /// <summary>Gets or sets nonnegative maximum client dimensions; zero means no limit on that axis.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is negative or a nonzero maximum is below the minimum.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2I MaxSize
    {
        get { ThrowIfDisposed(); return _display?.WindowGetMaxSize() ?? _maxSize; }
        set { EnsureMutable(); ValidateLimits(MinSize, value); _display?.WindowSetMaxSize(value); _maxSize = value; }
    }

    /// <summary>Gets or requests the client origin in native desktop coordinates.</summary>
    /// <value>The configured position before startup, or zero if no position was requested.</value>
    /// <remarks>Leaving it unset lets the system place
    /// the window. A preconfigured position is applied at startup and can fail on an unsupported platform.</remarks>
    /// <exception cref="NotSupportedException">The active compositor does not expose or accept global window positions, including Wayland.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2I Position
    {
        get { ThrowIfDisposed(); return _display?.WindowGetPosition() ?? _screenPosition ?? Vector2I.Zero; }
        set { EnsureMutable(); _display?.WindowSetPosition(value); _screenPosition = value; }
    }

    /// <summary>Gets or sets the root window's native visibility.</summary>
    /// <value>True by default. A native failure leaves managed visibility unchanged.</value>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    /// <exception cref="AggregateException">Visibility callbacks fail after the new visibility commits.</exception>
    public bool Visible
    {
        get { ThrowIfDisposed(); return _visible; }
        set
        {
            EnsureMutable();
            if (_visible == value) return;
            _display?.SetWindowVisible(value);
            _visible = value;
            List<Exception>? errors = null;
            try { VisibilityChanged?.Invoke(); }
            catch (Exception error) { CollectException(ref errors, error); }
            foreach (var node in EnumerateDepthFirst())
            {
                if (node is not CanvasItem item || item.IsDisposed || item.Parent is CanvasItem) continue;
                try { item.PropagateVisibilityChanged(); }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            ThrowCollected("Window visibility callbacks failed.", errors);
        }
    }

    /// <summary>Shows this window, acquiring no native resources before Engine.Run.</summary>
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

    /// <summary>Occurs when the system requests closure of this root window.</summary>
    /// <remarks>Handlers may disable SceneTree.AutoAcceptQuit to keep running, or call SceneTree.Quit with an exit code.
    /// The default quit decision follows the signal. Handler failures terminate Engine.Run with cleanup.</remarks>
    public event Action? CloseRequested;

    /// <summary>Occurs after Title commits a different value.</summary>
    public event Action? TitleChanged;

    /// <summary>Occurs when the window gains native keyboard focus.</summary>
    public event Action? FocusEntered;

    /// <summary>Occurs when the window loses native keyboard focus, before pressed input is released.</summary>
    public event Action? FocusExited;

    /// <summary>Gets the native window identity while running.</summary>
    /// <returns>Zero for the active root window; minus one while detached.</returns>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public int GetWindowID() { ThrowIfDisposed(); return _display is null ? DisplayServer.InvalidWindowId : DisplayServer.MainWindowId; }

    /// <summary>Reports whether the active native window has keyboard focus.</summary>
    /// <returns>The platform's current focus observation.</returns>
    /// <exception cref="InvalidOperationException">The window is inactive or accessed off-thread.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public bool HasFocus() => GetDisplay().WindowIsFocused();

    /// <summary>Requests keyboard focus and foreground placement from the native system.</summary>
    /// <remarks>The operating system may deny focus. Wayland submits no foreground activation request through this
    /// operation. Inspect HasFocus for the observed result.</remarks>
    /// <exception cref="InvalidOperationException">The window is inactive, accessed off-thread, or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void GrabFocus() => GetDisplay().WindowMoveToForeground();

    /// <summary>Requests a platform attention indication until this window is focused.</summary>
    /// <exception cref="InvalidOperationException">The window is inactive, accessed off-thread, or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public void RequestAttention() => GetDisplay().WindowRequestAttention();

    /// <inheritdoc />
    public override Rect GetVisibleRect()
    {
        var size = Size;
        return new Rect(Vector2.Zero, new Vector2(size.X, size.Y));
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(WindowProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Window)
        ? CreateDefaultWindow : base.CreateSceneInstanceFactory();

    private static Node CreateDefaultWindow() => new Window();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CloseRequested = null;
            VisibilityChanged = null;
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

    internal void OpenNative()
    {
        _display = DisplayServer.OpenForRendering(_title, _size, hidden: !Visible);
        _display.WindowSetMinSize(_minSize);
        _display.WindowSetMaxSize(_maxSize);
        _display.WindowSetSize(_size);
        foreach (var flag in new[] { Flags.ResizeDisabled, Flags.Borderless, Flags.AlwaysOnTop, Flags.NoFocus })
            if (GetFlag(flag))
                _display.WindowSetFlag((DisplayServer.WindowFlag)flag, true);
        if (_currentScreen is { } screen)
            _display.WindowSetCurrentScreen(screen);
        if (_screenPosition is { } position)
            _display.WindowSetPosition(position);
        if (_mode != ModeEnum.Windowed)
            _display.WindowSetMode((DisplayServer.WindowMode)_mode);
        _size = _display.WindowGetSize();
        _display.CloseRequested += HandleClose;
        _display.QuitRequested += HandleClose;
        _display.WindowRectChanged += HandleRect;
        _display.WindowFocusChanged += HandleFocus;
        _display.WindowMouseEntered += HandleMouseEntered;
        _display.WindowMouseExited += HandleMouseExited;
        _display.WindowDpiChanged += HandleDPIChanged;
        _display.FilesDropped += HandleFilesDropped;
        _renderer = RenderingServer.Open(this, _display.AcquireRenderingWindow());
        _display.SetGraphicsHandleQuery(_renderer.GetNativeHandle);
    }

    internal Vector2 GetClientMousePosition() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); EnsureNativeOpen(); return _display!.GetClientMousePosition(); }
    internal void WarpClientMouse(Vector2I position) { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); EnsureNativeOpen(); _display!.WarpMouse(position); }

    internal void EnsureNativeOpen()
    {
        if (_display is null)
            throw new InvalidOperationException("Activate a root Window through Engine.Run.");
    }

    internal void CloseNative()
    {
        if (_display is not { } display)
            return;
        Exception? renderFailure = null;
        try { _renderer?.Close(); }
        catch (Exception error) { renderFailure = error; }
        finally { _renderer = null; display.ReleaseRenderingWindow(); }
        display.CloseRequested -= HandleClose;
        display.QuitRequested -= HandleClose;
        display.WindowRectChanged -= HandleRect;
        display.WindowFocusChanged -= HandleFocus;
        display.WindowMouseEntered -= HandleMouseEntered;
        display.WindowMouseExited -= HandleMouseExited;
        display.WindowDpiChanged -= HandleDPIChanged;
        display.FilesDropped -= HandleFilesDropped;
        try { display.Dispose(); _display = null; }
        catch (Exception error) when (renderFailure is not null) { throw new AggregateException(renderFailure, error); }
        if (renderFailure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(renderFailure).Throw();
    }

    internal void PumpEvents() => GetDisplay().ProcessEvents();

    internal void Render(SceneTree tree, double step) => tree.RenderCanvas(_renderer ?? throw new InvalidOperationException("Rendering has not started."), step);

    private DisplayServer GetDisplay()
    {
        ThrowIfDisposed();
        return _display ?? throw new InvalidOperationException("The window is not active.");
    }

    private void HandleClose()
    {
        try { CloseRequested?.Invoke(); }
        finally
        {
            Tree?.AcceptWindowClose();
        }
    }

    private void HandleRect(RectI rect) => CommitSize(rect.Size);
    private void HandleFocus(bool focused) { if (focused) FocusEntered?.Invoke(); else FocusExited?.Invoke(); }
    private void HandleMouseEntered()
    {
        List<Exception>? errors = null;
        try { Tree?.EnterGUIViewport(this); } catch (Exception error) { CollectException(ref errors, error); }
        try { MouseEntered?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Window pointer-enter callbacks failed.", errors);
    }

    private void HandleMouseExited()
    {
        List<Exception>? errors = null;
        try { Tree?.ClearGUIHover(); } catch (Exception error) { CollectException(ref errors, error); }
        try { MouseExited?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Window pointer-exit callbacks failed.", errors);
    }
    private void HandleDPIChanged() => DpiChanged?.Invoke();
    private void HandleFilesDropped(IReadOnlyList<string> paths) => FilesDropped?.Invoke(paths);

    private void CommitSize(Vector2I size)
    {
        if (_size == size)
            return;
        _size = size;
        NotifySizeChanged();
    }

    private static void ValidateLimits(Vector2I minimum, Vector2I maximum)
    {
        if (minimum.X < 0 || minimum.Y < 0 || maximum.X < 0 || maximum.Y < 0 ||
            maximum.X != 0 && maximum.X < minimum.X || maximum.Y != 0 && maximum.Y < minimum.Y)
            throw new ArgumentOutOfRangeException(nameof(maximum), "Window limits must be nonnegative and ordered on each axis.");
    }
}
