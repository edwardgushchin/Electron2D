namespace Electron2D;

public partial class Window
{
    private Rect2i _nonclientArea;
    private static readonly PropertyDescriptor[] InputProperties =
    [new PropertyDescriptor<Window, Rect2i>(nameof(NonclientArea), w => w.NonclientArea, (w, v) => w.NonclientArea = v, _ => default, stored: true)];

    /// <summary>Gets or sets the custom decoration rectangle that receives mouse events while an exclusive child blocks normal input.</summary>
    /// <value>A window-client rectangle before the final viewport transform; initially empty.</value>
    /// <remarks>Only positive width and height enable the rectangle. Integer hit testing includes its top/left edges
    /// and excludes its bottom/right edges. Changing this stored authoring value creates no native decoration.</remarks>
    /// <exception cref="InvalidOperationException">Access violates the scene owner, mutation or capture policy.</exception>
    /// <exception cref="ObjectDisposedException">This window is disposed.</exception>
    public Rect2i NonclientArea
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _nonclientArea; }
        set { EnsureMutable(); _nonclientArea = value; }
    }

    /// <summary>Occurs when a mouse event hits the custom decoration rectangle while an exclusive child blocks normal input.</summary>
    /// <remarks>The event is borrowed for synchronous delivery before ordinary WindowInput. Coordinates are window-client
    /// coordinates before final viewport localization; embedded delivery has already removed the receiving window position.
    /// Native blocked reception stops after this signal; embedded hosts can still forward to their child windows.
    /// Internal mouse events are included. Direct PushInput bypasses this reception signal. Pause and input disabling
    /// do not suppress a window reception that has already reached this window. Signal blocking and lifetime rules apply.</remarks>
    public event Action<InputEvent>? NonclientWindowInput { add => _publisherNonclientWindowInput.Add(value); remove => _publisherNonclientWindowInput.Remove(value); }
    private EventPublisher<Action<InputEvent>> _publisherNonclientWindowInput;

    internal bool HasExclusiveInputChild => ExclusiveChild is { IsDisposed: false, Visible: true } child && ReferenceEquals(child.Tree, Tree);
    internal void EmitNonclientWindowInput(InputEvent input)
    {
        if (!IsInsideTree || !_nonclientArea.HasArea() || input is not InputEventMouse mouse ||
            !_nonclientArea.HasPoint((Vector2i)mouse.Position) || BeginEventEmission(_publisherNonclientWindowInput.Handlers) is not { } handlers) return;
        handlers(input);
    }

    /// <summary>Occurs when this window receives a non-internal input event, before viewport input dispatch.</summary>
    /// <remarks>The event is borrowed for synchronous delivery. Positional coordinates are window-client coordinates,
    /// before this window's final viewport transform. Embedded delivery removes the window position from its embedder's
    /// coordinates. Direct PushInput on this window bypasses this event; forwarding to an embedded child does not.
    /// The signal runs independently of process pause and GUI input disabling. Use Node input callbacks or Control GUI
    /// input to consume events. Handler failures are collected while ordinary input delivery continues.</remarks>
    public event Action<InputEvent>? WindowInput { add => _publisherWindowInput.Add(value); remove => _publisherWindowInput.Remove(value); }
    private EventPublisher<Action<InputEvent>> _publisherWindowInput;

    internal void EmitWindowInput(InputEvent input)
    {
        if (input.Device == InputEvent.DeviceIdInternal || !IsInsideTree || BeginEventEmission(_publisherWindowInput.Handlers) is not { } handlers) return;
        handlers(input);
    }
}
