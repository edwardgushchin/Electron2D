using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private string _imeText = string.Empty;
    private Vector2I _imeSelection;

    /// <summary>Gets the most recently received native IME composition text.</summary>
    /// <returns>The active composition, or an empty string before composition begins or after it commits.</returns>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    public string ImeGetText()
    {
        EnsureOwner();
        return _imeText;
    }

    /// <summary>Gets the current composition selection.</summary>
    /// <returns>The zero-based Unicode-codepoint start and length reported by the input method. Unknown negative offsets become zero.</returns>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    public Vector2I ImeGetSelection()
    {
        EnsureOwner();
        return _imeSelection;
    }

    /// <summary>Enables or disables native text input for the main window.</summary>
    /// <param name="active">Whether to accept committed text and composition updates.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>The host should enable text input only while a text field owns focus.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The window ID is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is off the opening thread or native text input could not change state.</exception>
    public void WindowSetImeActive(bool active, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        var success = active ? SDL.StartTextInput(window) : SDL.StopTextInput(window);
        if (!success)
            throw SdlFailure("change text input state");
        if (!active)
        {
            _imeText = string.Empty;
            _imeSelection = Vector2I.Zero;
        }
    }

    /// <summary>Moves the native IME candidate area to a window-local text caret.</summary>
    /// <param name="position">Caret position in client pixels on Wayland, or platform-native window coordinates elsewhere.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>On Wayland, the requested client-pixel position is converted to SDL logical window coordinates using the current pixel density. The native candidate area is one by ten window-coordinate units with a zero cursor offset. Text input must be active for a candidate popup to be shown.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The window ID is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="OverflowException">The pixel position cannot be represented in native window coordinates.</exception>
    /// <exception cref="InvalidOperationException">The caller is off the opening thread or the native density or area request fails.</exception>
    public void WindowSetImePosition(Vector2I position, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        var nativePosition = _waylandWindowPosition ? WaylandLogicalWindowSize(position, window) : position;
        var area = new SDL.Rect { X = nativePosition.X, Y = nativePosition.Y, W = 1, H = 10 };
        if (!SDL.SetTextInputArea(window, in area, 0))
            throw SdlFailure("set the text input area");
    }

    /// <summary>Gets whether touch input is available from a device or mouse emulation.</summary>
    /// <returns><see langword="true"/> when a touch device is connected or mouse-to-touch emulation is enabled.</returns>
    /// <remarks>Reads the current <see cref="Input.EmulateTouchFromMouse"/> setting and native device list on the opening thread.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    public bool IsTouchscreenAvailable()
    {
        EnsureOwner();
        if (Input.Instance.EmulateTouchFromMouse)
            return true;
        var devices = SDL.GetTouchDevices(out var count);
        return devices is not null && count > 0;
    }
}
