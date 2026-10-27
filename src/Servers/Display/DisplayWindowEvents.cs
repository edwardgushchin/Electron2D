namespace Electron2D;

/// <summary>Publishes typed input and notifications for one native window.</summary>
/// <remarks>Obtain a borrowed publisher through DisplayServer.GetWindowEvents. Subscription and disposal
/// use the display owner thread. Native retirement clears and disposes the publisher. Explicit disposal
/// clears subscriptions without closing the window; a subsequent query creates a new publisher.
/// Adding a handler to a retired publisher throws ObjectDisposedException; removing one is harmless.
/// Live add/remove and disposal require the display owner thread. Notifications use standard synchronous
/// C# multicast delivery: a failing handler stops later handlers on this publisher. Independent window
/// recipients and the scene input phase still run; failures propagate through the parser or native pump.
/// Native notifications follow the existing window/scene callback; parsed Input precedes scene input.
/// Signal blocking suppresses this publisher's notifications without blocking the scene.</remarks>
public sealed class DisplayWindowEvents : ElectronObject
{
    private DisplayServer? _owner;
    private EventPublisher<Action<InputEvent>> _input;
    private EventPublisher<Action<string>> _textInput;
    private EventPublisher<Action<string, Vector2i>> _textEditing;
    private EventPublisher<Action> _closeRequested, _forceCloseRequested, _goBackRequested, _mouseEntered, _mouseExited, _dpiChanged;
    private EventPublisher<Action<bool>> _focusChanged;
    private EventPublisher<Action<float>> _outputChanged;
    private EventPublisher<Action<Rect2i>> _rectChanged;
    private EventPublisher<Action<IReadOnlyList<string>>> _filesDropped;
    internal DisplayWindowEvents(DisplayServer owner, int windowID) { _owner = owner; WindowID = windowID; }
    /// <summary>Gets the native session's logical window ID, retained after publisher disposal.</summary>
    /// <value>Zero identifies the main window; child IDs are not reused within the session.</value>
    public int WindowID { get; }
    internal bool Available => _owner is { WindowEventsAvailable: true };
    internal bool HasInput => _input.Handlers is not null && Available;

    /// <summary>Occurs after raw/action state commits and before ordinary scene input delivery.</summary>
    /// <remarks>The event is borrowed for this synchronous callback. Window-tagged events select this ID;
    /// untagged events broadcast to requested native windows. Native, caller-parsed and emulated events
    /// share this phase. With active subscriptions, parsing requires the display owner thread.</remarks>
    /// <exception cref="ObjectDisposedException">The publisher or its native window has retired.</exception>
    /// <exception cref="InvalidOperationException">Subscription is off the display owner thread.</exception>
    public event Action<InputEvent>? Input
    {
        add { Add(ref _input, value); _owner!.RefreshWindowInputSources(); }
        remove { if (Remove(ref _input, value)) _owner!.RefreshWindowInputSources(); }
    }
    /// <summary>Occurs when the platform commits a complete text string for this window.</summary>
    public event Action<string>? TextInput { add => Add(ref _textInput, value); remove => Remove(ref _textInput, value); }
    /// <summary>Occurs after this window's composition text and codepoint selection commit.</summary>
    public event Action<string, Vector2i>? TextEditing { add => Add(ref _textEditing, value); remove => Remove(ref _textEditing, value); }
    /// <summary>Occurs on an ordinary native close request; it does not close the window automatically.</summary>
    public event Action? CloseRequested { add => Add(ref _closeRequested, value); remove => Remove(ref _closeRequested, value); }
    /// <summary>Occurs before native popup dismissal or parent retirement forcibly hides this popup.</summary>
    /// <remarks>Callback failure cannot cancel native retirement. Ordinary hide/disposal is not a forced request.</remarks>
    public event Action? ForceCloseRequested { add => Add(ref _forceCloseRequested, value); remove => Remove(ref _forceCloseRequested, value); }
    /// <summary>Occurs on a native system-back request for this window.</summary>
    public event Action? GoBackRequested { add => Add(ref _goBackRequested, value); remove => Remove(ref _goBackRequested, value); }
    /// <summary>Occurs when the native pointer enters this window.</summary>
    public event Action? MouseEntered { add => Add(ref _mouseEntered, value); remove => Remove(ref _mouseEntered, value); }
    /// <summary>Occurs when the native pointer leaves this window.</summary>
    public event Action? MouseExited { add => Add(ref _mouseExited, value); remove => Remove(ref _mouseExited, value); }
    /// <summary>Occurs when this window's native display content scale changes.</summary>
    /// <remarks>This reports content scale, not a physical DPI measurement.</remarks>
    public event Action? DPIChanged { add => Add(ref _dpiChanged, value); remove => Remove(ref _dpiChanged, value); }

    /// <summary>Occurs after the active output's maximum linear color value changes and scene recipients run.</summary>
    public event Action<float>? OutputMaxLinearValueChanged { add => Add(ref _outputChanged, value); remove => Remove(ref _outputChanged, value); }
    /// <summary>Occurs when this window gains or loses keyboard focus, before the application notification.</summary>
    public event Action<bool>? FocusChanged { add => Add(ref _focusChanged, value); remove => Remove(ref _focusChanged, value); }
    /// <summary>Occurs after an effective native move or resize commits its complete client rectangle.</summary>
    /// <remarks>Wayland uses the zero-position convention because global desktop placement is unavailable.</remarks>
    public event Action<Rect2i>? RectChanged { add => Add(ref _rectChanged, value); remove => Remove(ref _rectChanged, value); }
    /// <summary>Occurs once per completed native file drop with an owned ordered path list.</summary>
    /// <remarks>The paths remain valid after delivery. Interrupted native drops are discarded.</remarks>
    public event Action<IReadOnlyList<string>>? FilesDropped { add => Add(ref _filesDropped, value); remove => Remove(ref _filesDropped, value); }

    private void Add<T>(ref EventPublisher<T> field, T? value) where T : Delegate
    { ThrowIfDisposed(); (_owner ?? throw new ObjectDisposedException(nameof(DisplayWindowEvents))).ValidateWindowEventAccess(); field.Add(value); }
    private bool Remove<T>(ref EventPublisher<T> field, T? value) where T : Delegate
    {
        if (_owner is null) return false;
        _owner.ValidateWindowEventOwner(); field.Remove(value); return true;
    }

    internal Action? CloseHandlers => Available ? BeginEventEmission(_closeRequested.Handlers) : null;
    internal Action? BackHandlers => Available ? BeginEventEmission(_goBackRequested.Handlers) : null;
    internal Action? EnterHandlers => Available ? BeginEventEmission(_mouseEntered.Handlers) : null;
    internal Action? ExitHandlers => Available ? BeginEventEmission(_mouseExited.Handlers) : null;
    internal Action? DPIHandlers => Available ? BeginEventEmission(_dpiChanged.Handlers) : null;
    internal Action<float>? OutputHandlers => Available ? BeginEventEmission(_outputChanged.Handlers) : null;
    internal Action<bool>? FocusHandlers => Available ? BeginEventEmission(_focusChanged.Handlers) : null;
    internal Action<Rect2i>? RectHandlers => Available ? BeginEventEmission(_rectChanged.Handlers) : null;
    internal Action<string>? TextHandlers => Available ? BeginEventEmission(_textInput.Handlers) : null;
    internal Action<string, Vector2i>? EditingHandlers => Available ? BeginEventEmission(_textEditing.Handlers) : null;
    internal Action<IReadOnlyList<string>>? DropHandlers => Available ? BeginEventEmission(_filesDropped.Handlers) : null;
    internal void EmitInput(InputEvent value) { if (Available) BeginEventEmission(_input.Handlers)?.Invoke(value); }
    internal void EmitForceClose() { if (_owner is not null) BeginEventEmission(_forceCloseRequested.Handlers)?.Invoke(); }

    /// <inheritdoc />
    protected override void ValidateDisposal() => _owner?.ValidateWindowEventOwner();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            var owner = _owner; _owner = null;
            _outputChanged.Handlers = null;
            _input.Handlers = null; _textInput.Handlers = null; _textEditing.Handlers = null; _focusChanged.Handlers = null; _rectChanged.Handlers = null; _filesDropped.Handlers = null;
            _closeRequested.Handlers = _forceCloseRequested.Handlers = _goBackRequested.Handlers = _mouseEntered.Handlers = _mouseExited.Handlers = _dpiChanged.Handlers = null;
            owner?.RefreshWindowInputSources();
        }
        base.Dispose(disposing);
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<DisplayWindowEvents, int>(nameof(WindowID), n => n.WindowID)]);
}
