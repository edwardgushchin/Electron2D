using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private string _imeText = string.Empty;
    private Vector2i _imeSelection;

    internal string IMEGetTextCore()
    {
        EnsureOwner();
        return _imeText;
    }

    internal Vector2i IMEGetSelectionCore()
    {
        EnsureOwner();
        return _imeSelection;
    }

    internal void WindowSetIMEActiveCore(bool active, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        var success = active ? SDL.StartTextInput(window) : SDL.StopTextInput(window);
        if (!success)
            throw SDLFailure("change text input state");
        if (!active)
        {
            _imeText = string.Empty;
            _imeSelection = Vector2i.Zero;
        }
    }

    internal void WindowSetIMEPositionCore(Vector2i position, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        var nativePosition = _waylandWindowPosition ? WaylandLogicalWindowSize(position, window) : position;
        var area = new SDL.Rect { X = nativePosition.X, Y = nativePosition.Y, W = 1, H = 10 };
        if (!SDL.SetTextInputArea(window, in area, 0))
            throw SDLFailure("set the text input area");
    }

    internal bool IsTouchscreenAvailableCore()
    {
        EnsureOwner();
        if (Input.EmulateTouchFromMouse)
            return true;
        var devices = SDL.GetTouchDevices(out var count);
        return devices is not null && count > 0;
    }
}
