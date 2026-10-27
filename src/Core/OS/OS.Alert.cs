using SDL3;

namespace Electron2D;

public sealed partial class OS
{
    /// <summary>Displays a native modal message with an acknowledgement button.</summary>
    /// <param name="text">Message text. Empty text is allowed.</param>
    /// <param name="title">Window title; Alert! by default. Empty titles are allowed.</param>
    /// <remarks>Blocks the calling thread until acknowledged or closed. No renderer, scene or display server is required.
    /// With an active display, supplies its main window as a native parent hint and requires its owner thread.
    /// An active Engine lifecycle also requires its owner; outside these lifecycles the caller must use the host's main thread.
    /// This cold native operation allocates and can run before startup or after shutdown. Native presentation can fail on headless hosts.</remarks>
    /// <exception cref="ArgumentNullException">Text or title is null.</exception>
    /// <exception cref="ArgumentException">Text or title contains a null character.</exception>
    /// <exception cref="InvalidOperationException">The caller violates an active owner rule or native presentation fails.</exception>
    public static void Alert(string text, string title = "Alert!") => Service.AlertCore(text, title);

    private void AlertCore(string text, string title)
    {
        ThrowIfDisposed();
        ValidateNativeMessage(text, nameof(text)); ValidateNativeMessage(title, nameof(title));
        var owner = Volatile.Read(ref _mainThreadOwner);
        if (owner != 0 && owner != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("A native alert belongs to the active Engine lifecycle owner.");
        var parent = DisplayServer.Service?.GetAlertParentWindow() ?? 0;
        if (!SDL.ShowSimpleMessageBox(SDL.MessageBoxFlags.Information, title, text, parent))
            throw new InvalidOperationException("The platform could not show a native alert: " + SDL.GetError());
    }

    internal static void ValidateNativeMessage(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (value.Contains('\0')) throw new ArgumentException("Native message text cannot contain a null character.", parameterName);
    }
}
