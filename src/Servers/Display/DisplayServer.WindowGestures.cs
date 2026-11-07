using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Identifies the edge or corner used for interactive resizing.</summary>
    public enum WindowResizeEdge
    {
        /// <summary>The top-left corner.</summary>
        TopLeft = 0,
        /// <summary>The top edge.</summary>
        Top = 1,
        /// <summary>The top-right corner.</summary>
        TopRight = 2,
        /// <summary>The left edge.</summary>
        Left = 3,
        /// <summary>The right edge.</summary>
        Right = 4,
        /// <summary>The bottom-left corner.</summary>
        BottomLeft = 5,
        /// <summary>The bottom edge.</summary>
        Bottom = 6,
        /// <summary>The bottom-right corner.</summary>
        BottomRight = 7,
        /// <summary>The number of edges and corners. This is not a selectable edge.</summary>
        Max = 8,
    }

    /// <summary>Requests that the window manager move a window with the pointer.</summary>
    /// <param name="windowID">The live native window identity.</param>
    /// <remarks>Call on the display owner thread during a physical left-button press in a visible
    /// window with no other mouse button held. The window manager owns the interaction. On X11 it must support EWMH move/resize.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The window identity is absent or retired.</exception>
    /// <exception cref="InvalidOperationException">There is no owned display session or the caller is not its owner.</exception>
    /// <exception cref="NotSupportedException">The active display backend or window manager has no gesture adapter.</exception>
    public static void WindowStartDrag(int windowID = MainWindowId) =>
        RequireService().ResolveWindow(windowID).WindowStartGestureCore(null);

    /// <summary>Requests that the window manager resize a window from the specified edge with the pointer.</summary>
    /// <param name="edge">The edge or corner to move.</param>
    /// <param name="windowID">The live native window identity.</param>
    /// <remarks>Uses the same pointer and backend requirements as <see cref="WindowStartDrag"/>.
    /// A nonresizable window ignores the request.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The edge is invalid or the window identity is absent or retired.</exception>
    /// <exception cref="InvalidOperationException">There is no owned display session or the caller is not its owner.</exception>
    /// <exception cref="NotSupportedException">The active display backend or window manager has no gesture adapter.</exception>
    public static void WindowStartResize(WindowResizeEdge edge, int windowID = MainWindowId)
    {
        ValidateWindowResizeEdge(edge);
        RequireService().ResolveWindow(windowID).WindowStartGestureCore(edge);
    }

    internal static void ValidateWindowResizeEdge(WindowResizeEdge edge)
    {
        if ((uint)edge >= (uint)WindowResizeEdge.Max)
            throw new ArgumentOutOfRangeException(nameof(edge));
    }

    private (DisplayServer Owner, X11WindowGesture Native)? _windowGesture;
    private DisplayServer? _windowGestureRelease;
    private bool _discardWindowGestureRelease;

    internal void WindowStartGestureCore(WindowResizeEdge? edge)
    {
        EnsureOwner();
        var root = _rootDisplay ?? this;
        var flags = SDL.GetWindowFlags(_window.DangerousGetHandle());
        if (IsNativePopup || (flags & (SDL.WindowFlags.Hidden | SDL.WindowFlags.Minimized)) != 0 ||
            edge is not null && (flags & SDL.WindowFlags.Resizable) == 0)
            return;
        if (SDL.GetCurrentVideoDriver() != "x11")
            throw new NotSupportedException("Native interactive window gestures require the X11 display backend.");
        if (root._mouseMode is not (MouseMode.Visible or MouseMode.Hidden) || root._windowGesture is not null ||
            _heldMouseButtons != MouseButtonMask.Left)
            return;

        var display = WindowGetNativeHandleCore(HandleType.DisplayHandle);
        var window = (nuint)WindowGetNativeHandleCore(HandleType.WindowHandle);
        if (X11WindowGesture.Start(display, window, edge) is { } gesture)
        {
            root._windowGesture = (this, gesture);
            root._windowGestureRelease = this;
        }
    }

    private void DropWindowGesture()
    {
        if (_windowGesture is not { } active || _discardWindowGestureRelease)
            return;
        active.Native.Cancel();
        // Keep the release associated with this press until the physical button is up.
        _discardWindowGestureRelease = true;
    }

    private bool EndWindowGesture()
    {
        var missingRelease = _windowGestureRelease is not null;
        if (_windowGesture is not { } active || !active.Native.TryFinish(missingRelease, cancel: !_discardWindowGestureRelease))
            return false;
        _windowGesture = null;
        if (!missingRelease)
            _discardWindowGestureRelease = false;
        return missingRelease;
    }

    private void RetireWindowGesture()
    {
        var root = _rootDisplay ?? this;
        if (root._windowGesture is { } active && active.Owner == this)
        {
            root._windowGesture = null;
            if (!root._discardWindowGestureRelease)
                active.Native.Cancel();
            root._discardWindowGestureRelease = false;
        }
        if (root._windowGestureRelease == this)
        {
            root._windowGestureRelease = null;
            root._discardWindowGestureRelease = false;
        }
    }
}
