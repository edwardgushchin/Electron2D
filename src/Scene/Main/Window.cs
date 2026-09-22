namespace Electron2D;

/// <summary>A configurable native root window that owns scene children.</summary>
/// <remarks>Pass a detached window to <see cref="Engine.Run"/>. The runtime opens its native window before
/// scene entry and releases it after scene teardown. One root window is supported. The client size uses pixels
/// on Wayland and native window units elsewhere. Rendering and embedded windows are not implemented.</remarks>
public class Window : Viewport
{
    private static readonly PropertyDescriptor[] WindowProperties =
    [
        new PropertyDescriptor<Window, string>(nameof(Title), w => w.Title, (w, v) => w.Title = v, _ => "", stored: true),
        new PropertyDescriptor<Window, Vector2I>(nameof(Size), w => w.Size, (w, v) => w.Size = v, _ => new(100, 100), stored: true),
        new PropertyDescriptor<Window, Vector2I>(nameof(MinSize), w => w.MinSize, (w, v) => w.MinSize = v, _ => Vector2I.Zero, stored: true),
        new PropertyDescriptor<Window, Vector2I>(nameof(MaxSize), w => w.MaxSize, (w, v) => w.MaxSize = v, _ => Vector2I.Zero, stored: true),
    ];

    private DisplayServer? _display;
    private string _title = "";
    private Vector2I _size = new(100, 100);
    private Vector2I _minSize;
    private Vector2I _maxSize;
    private Vector2I? _screenPosition;

    /// <summary>Creates a detached visible window with an empty title and a 100 by 100 client area.</summary>
    /// <remarks>No native resources are acquired until <see cref="Engine.Run"/>.</remarks>
    public Window() { }

    /// <summary>Gets or sets the native window title.</summary>
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
            TitleChanged?.Invoke();
        }
    }

    /// <summary>Gets the observed client size or requests a positive client size.</summary>
    /// <value>100 by 100 before configuration or native activation.</value>
    /// <remarks>Native changes may be asynchronous or constrained by the compositor and size limits.
    /// SizeChanged follows committed size changes; desktop position and inherited node transforms do not affect size.</remarks>
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
    /// <remarks>This is independent of inherited <see cref="Node.Position"/>. Leaving it unset lets the system place
    /// the window. A preconfigured position is applied at startup and can fail on an unsupported platform.</remarks>
    /// <exception cref="NotSupportedException">The active compositor does not expose or accept global window positions, including Wayland.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner or the native request fails.</exception>
    /// <exception cref="ObjectDisposedException">The window is disposed.</exception>
    public Vector2I ScreenPosition
    {
        get { ThrowIfDisposed(); return _display?.WindowGetPosition() ?? _screenPosition ?? Vector2I.Zero; }
        set { EnsureMutable(); _display?.WindowSetPosition(value); _screenPosition = value; }
    }

    /// <inheritdoc />
    /// <remarks>Also shows or hides the native window when active. Calls through Node and inherited Show/Hide
    /// use this behavior. A native failure leaves managed visibility unchanged.</remarks>
    public override bool Visible
    {
        get => base.Visible;
        set { EnsureMutable(); _display?.SetWindowVisible(value); base.Visible = value; }
    }

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
    public int GetWindowId() { ThrowIfDisposed(); return _display is null ? DisplayServer.InvalidWindowId : DisplayServer.MainWindowId; }

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
            TitleChanged = null;
            FocusEntered = null;
            FocusExited = null;
        }
        base.Dispose(disposing);
    }

    internal void OpenNative()
    {
        _display = DisplayServer.Open(_title, _size, hidden: !Visible);
        _display.WindowSetMinSize(_minSize);
        _display.WindowSetMaxSize(_maxSize);
        _display.WindowSetSize(_size);
        if (_screenPosition is { } position)
            _display.WindowSetPosition(position);
        _size = _display.WindowGetSize();
        _display.CloseRequested += HandleClose;
        _display.QuitRequested += HandleClose;
        _display.WindowRectChanged += HandleRect;
        _display.WindowFocusChanged += HandleFocus;
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
        display.CloseRequested -= HandleClose;
        display.QuitRequested -= HandleClose;
        display.WindowRectChanged -= HandleRect;
        display.WindowFocusChanged -= HandleFocus;
        display.Dispose();
        _display = null;
    }

    internal void PumpEvents() => GetDisplay().ProcessEvents();

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
