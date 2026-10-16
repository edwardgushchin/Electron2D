namespace Electron2D;

public partial class Window
{
    /// <summary>Occurs when this window receives a non-internal input event, before viewport input dispatch.</summary>
    /// <remarks>The event is borrowed for synchronous delivery. Positional coordinates are window-client coordinates,
    /// before this window's final viewport transform. Embedded delivery removes the window position from its embedder's
    /// coordinates. Direct PushInput on this window bypasses this event; forwarding to an embedded child does not.
    /// The signal runs independently of process pause and GUI input disabling. Use Node input callbacks or Control GUI
    /// input to consume events. Handler failures are collected while ordinary input delivery continues.</remarks>
    public event Action<InputEvent>? WindowInput;

    internal void EmitWindowInput(InputEvent input)
    {
        if (input.Device == InputEvent.DeviceIdInternal || !IsInsideTree || BeginEventEmission(WindowInput) is not { } handlers) return;
        handlers(input);
    }
}
