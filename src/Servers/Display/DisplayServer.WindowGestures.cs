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
    /// window with no other mouse button held. The window manager owns the interaction. Windows enters
    /// its native move/size loop after the display pump drains queued input. X11 requires an EWMH-capable window manager.
    /// On Wayland, the compositor validates the originating press and decides whether to honor the request.</remarks>
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
    private (DisplayServer Owner, WindowResizeEdge? Edge)? _pendingWindowsGesture;
    private DisplayServer? _windowGestureRelease;
    private bool _discardWindowGestureRelease;

    internal void WindowStartGestureCore(WindowResizeEdge? edge)
    {
        EnsureOwner();
        var root = _rootDisplay ?? this;
        if (!CanMoveOrResize(edge))
            return;
        var driver = _videoDriver;
        if (driver is not ("windows" or "x11" or "wayland") || driver == "wayland" && root._waylandInput is null)
            throw new NotSupportedException("The active display backend has no native interactive window gesture adapter.");
        if (root._mouseMode is not (MouseMode.Visible or MouseMode.Hidden) || root._windowGesture is not null ||
            root._pendingWindowsGesture is not null || root._windowGestureRelease is not null ||
            _heldMouseButtons != MouseButtonMask.Left)
            return;

        if (driver == "windows")
        {
            root._pendingWindowsGesture = (this, edge);
            return;
        }

        if (root._waylandInput is { } wayland)
        {
            // A later queued button transition can supersede the press currently being delivered by SDL.
            if (SDL.HasEvents((uint)SDL.EventType.MouseButtonDown, (uint)SDL.EventType.MouseButtonUp))
                return;
            var properties = SDL.GetWindowProperties(_window.DangerousGetHandle());
            var surface = SDL.GetPointerProperty(properties, SDL.Props.WindowWaylandSurfacePointer, 0);
            var toplevel = SDL.GetPointerProperty(properties, SDL.Props.WindowWaylandXDGToplevelPointer, 0);
            if (wayland.TryStartGesture(surface, toplevel, edge))
                root._windowGestureRelease = this;
            return;
        }

        var display = WindowGetNativeHandleCore(HandleType.DisplayHandle);
        var window = (nuint)WindowGetNativeHandleCore(HandleType.WindowHandle);
        if (X11WindowGesture.Start(display, window, edge) is { } gesture)
        {
            root._windowGesture = (this, gesture);
            root._windowGestureRelease = this;
        }
    }

    private bool CanMoveOrResize(WindowResizeEdge? edge)
    {
        var flags = SDL.GetWindowFlags(_window.DangerousGetHandle());
        return !IsNativePopup && (flags & (SDL.WindowFlags.Hidden | SDL.WindowFlags.Minimized)) == 0 &&
            (edge is null || (flags & SDL.WindowFlags.Resizable) != 0);
    }

    private bool AdvanceNativeInput(ref List<Exception>? failures)
    {
        try { _waylandInput?.DispatchPending(); }
        catch (Exception error) { (failures ??= new List<Exception>()).Add(error); }
        finally { _waylandInput?.ClearKeys(); }
        if (_pendingWindowsGesture is null)
            return EndWindowGesture();

        try
        {
            FlushBufferedInput();
            if (_pendingWindowsGesture is not { } request)
                return false;
            _pendingWindowsGesture = null;
            var owner = request.Owner;
            if (owner.IsDisposed || owner._heldMouseButtons != MouseButtonMask.Left ||
                _mouseMode is not (MouseMode.Visible or MouseMode.Hidden) || !owner.CanMoveOrResize(request.Edge))
                return false;

            var window = owner.WindowGetNativeHandleCore(HandleType.WindowHandle);
            if (!WindowsWindowGesture.TryPrepare(window, request.Edge, owner._lastClientMousePosition, out var command, out var position))
                return false;

            // The Win32 modal loop can generate an SDL release while handing off capture.
            _windowGestureRelease = owner;
            WindowsWindowGesture.Run(window, command, position);
            return true;
        }
        catch (Exception error)
        {
            _pendingWindowsGesture = null;
            _windowGestureRelease = null;
            (failures ??= new List<Exception>()).Add(error);
            return false;
        }
    }

    private void DropWindowGesture()
    {
        _pendingWindowsGesture = null;
        if (_waylandInput is not null && _windowGestureRelease is not null)
            _discardWindowGestureRelease = true;
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
        if (root._waylandInput is { } wayland)
        {
            var properties = SDL.GetWindowProperties(_window.DangerousGetHandle());
            wayland.Forget(SDL.GetPointerProperty(properties, SDL.Props.WindowWaylandSurfacePointer, 0));
        }
        if (root._pendingWindowsGesture?.Owner == this)
            root._pendingWindowsGesture = null;
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
