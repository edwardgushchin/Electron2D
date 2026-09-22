using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Identifies a borrowed native display or window handle.</summary>
    public enum HandleType
    {
        /// <summary>The X11 or Wayland display connection.</summary>
        DisplayHandle = 0,
        /// <summary>The platform window: an X11 window ID, Wayland surface, Win32 HWND, or Cocoa NSWindow.</summary>
        WindowHandle = 1,
    }

    /// <summary>Selects a main-window policy by its stable display-server ID.</summary>
    public enum WindowFlag
    {
        /// <summary>Whether dragging the window border is prevented from resizing it.</summary>
        ResizeDisabled = 0,
        /// <summary>Whether the window has no native border and title bar.</summary>
        Borderless = 1,
        /// <summary>Whether the window stays above ordinary windows.</summary>
        /// <remarks>The ordinary Wayland top-level window cannot apply this policy.</remarks>
        AlwaysOnTop = 2,
        /// <summary>Whether the window background can be transparent; requires transparent window creation and a renderer.</summary>
        Transparent = 3,
        /// <summary>Whether the window is prevented from receiving keyboard focus.</summary>
        /// <remarks>SDL supports changing this policy only for Wayland popup-menu windows, which the main window is not.</remarks>
        NoFocus = 4,
        /// <summary>Whether the window is a transient menu popup; requires multiple-window ownership.</summary>
        Popup = 5,
        /// <summary>Whether content extends under the native title bar; requires platform title-bar integration.</summary>
        ExtendToTitle = 6,
        /// <summary>Whether mouse input passes to an underlying application window; requires native hit-test integration.</summary>
        MousePassthrough = 7,
        /// <summary>Whether native rounded window corners are suppressed; requires platform window-style integration.</summary>
        SharpCorners = 8,
        /// <summary>Whether ordinary screen capture excludes the window; requires platform capture-policy integration.</summary>
        ExcludeFromCapture = 9,
        /// <summary>Whether the window manager treats the window as a popup; requires multiple-window ownership.</summary>
        PopupWmHint = 10,
        /// <summary>Whether native minimization controls are disabled; requires platform window-style integration.</summary>
        MinimizeDisabled = 11,
        /// <summary>Whether native maximization controls are disabled; requires platform window-style integration.</summary>
        MaximizeDisabled = 12,
        /// <summary>The number of defined policy identifiers; not a window policy.</summary>
        Max = 13,
    }

    /// <summary>Identifies the current presentation state of a native window.</summary>
    public enum WindowMode
    {
        /// <summary>A decorated or undecorated floating window.</summary>
        Windowed = 0,
        /// <summary>A window hidden by the window manager and represented in its task list.</summary>
        Minimized = 1,
        /// <summary>A window expanded to the work area with its border retained.</summary>
        Maximized = 2,
        /// <summary>A borderless window covering its current display without a video-mode change.</summary>
        Fullscreen = 3,
        /// <summary>An exclusive fullscreen mode selected from the current display's available video modes where supported.</summary>
        /// <remarks>Wayland uses ordinary compositor fullscreen for this request and reports <see cref="Fullscreen"/>.</remarks>
        ExclusiveFullscreen = 4,
    }

    /// <summary>Identifies a native taskbar progress indication.</summary>
    public enum ProgressState
    {
        /// <summary>Removes taskbar progress.</summary>
        None = 0,
        /// <summary>Shows activity without a numerical fraction.</summary>
        Indeterminate = 1,
        /// <summary>Shows normal progress.</summary>
        Normal = 2,
        /// <summary>Shows an error state.</summary>
        Error = 3,
        /// <summary>Shows a paused state.</summary>
        Paused = 4,
    }

    /// <summary>Gets the current screen index containing the main window.</summary>
    /// <param name="windowId">The window ID; only zero identifies an owned window.</param>
    /// <returns>The zero-based screen index or <see cref="InvalidScreen"/> when the window has no display.</returns>
    public int WindowGetCurrentScreen(int windowId = MainWindowId)
    {
        EnsureOwner();
        if (windowId != MainWindowId)
            return InvalidScreen;
        var displayId = SDL.GetDisplayForWindow(GetWindow(windowId));
        return Array.IndexOf(GetDisplays(), displayId);
    }

    /// <summary>Gets the index of the display with keyboard focus.</summary>
    /// <returns>The main window's screen when it has focus on a backend with global focus information; otherwise the primary screen.</returns>
    /// <remarks>Wayland does not expose a process-wide keyboard-focus window, so its result is always the primary screen.</remarks>
    public int GetKeyboardFocusScreen()
    {
        EnsureOwner();
        return SDL.GetCurrentVideoDriver() != "wayland" && SDL.GetKeyboardFocus() == _window.DangerousGetHandle()
            ? WindowGetCurrentScreen() : GetPrimaryScreen();
    }

    /// <summary>Gets the display containing the largest portion of a desktop rectangle.</summary>
    /// <param name="rectangle">Desktop rectangle in platform-native coordinates.</param>
    /// <returns>The zero-based index of the screen with the greatest whole-pixel overlap area, or <see cref="InvalidScreen"/> when no overlap reaches one pixel.</returns>
    /// <remarks>On Wayland, SDL output origins may be in logical desktop coordinates while the public output sizes use physical pixels; cross-output selection can therefore be ambiguous at mixed scales.</remarks>
    public int GetScreenFromRect(Rect rectangle)
    {
        EnsureOwner();
        if (!rectangle.IsFinite() || !rectangle.HasArea())
            return InvalidScreen;
        var displays = GetDisplays();
        var selected = InvalidScreen;
        var greatestArea = 0L;
        for (var index = 0; index < displays.Length; index++)
        {
            if (!SDL.GetDisplayBounds(displays[index], out var bounds))
                continue;
            var size = SDL.GetCurrentVideoDriver() == "wayland"
                ? WaylandPhysicalScreenSize(displays[index], bounds)
                : new Vector2I(bounds.W, bounds.H);
            var left = Math.Max((double)rectangle.Position.X, bounds.X);
            var top = Math.Max((double)rectangle.Position.Y, bounds.Y);
            var right = Math.Min((double)rectangle.End.X, (double)bounds.X + size.X);
            var bottom = Math.Min((double)rectangle.End.Y, (double)bounds.Y + size.Y);
            var area = (long)Math.Truncate(Math.Max(0d, right - left) * Math.Max(0d, bottom - top));
            if (area <= greatestArea)
                continue;
            greatestArea = area;
            selected = index;
        }
        return selected;
    }

    /// <summary>Gets a snapshot of the engine-owned native window IDs.</summary>
    /// <returns>A one-element snapshot containing <see cref="MainWindowId"/>.</returns>
    public int[] GetWindowList()
    {
        EnsureOwner();
        return [MainWindowId];
    }

    /// <summary>Gets a borrowed operating-system handle for the main display or window.</summary>
    /// <param name="handleType">The native handle category to query.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>A nonzero pointer or platform window ID represented as a pointer-sized integer.</returns>
    /// <remarks>
    /// The caller does not own the returned handle and must never destroy or release it. Query it again after native
    /// window state changes; it is invalid after this server is disposed. Native interop with it must obey the
    /// platform's thread rules and this server's owner-thread boundary. Display handles are available on X11 and
    /// Wayland; window handles are available on X11, Wayland, Windows, and macOS.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="handleType"/> or <paramref name="windowId"/> is not defined or owned.</exception>
    /// <exception cref="NotSupportedException">The requested native handle is unavailable on the active video driver.</exception>
    /// <exception cref="InvalidOperationException">The call is made from another thread, or SDL cannot retrieve window properties or the native handle is currently absent.</exception>
    /// <exception cref="ObjectDisposedException">The display server has been disposed.</exception>
    public nint WindowGetNativeHandle(HandleType handleType, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        if (handleType is not (HandleType.DisplayHandle or HandleType.WindowHandle))
            throw new ArgumentOutOfRangeException(nameof(handleType), handleType, "Unknown native handle type.");

        var driver = SDL.GetCurrentVideoDriver();
        var property = (handleType, driver) switch
        {
            (HandleType.DisplayHandle, "x11") => SDL.Props.WindowX11DisplayPointer,
            (HandleType.DisplayHandle, "wayland") => SDL.Props.WindowWaylandDisplayPointer,
            (HandleType.WindowHandle, "windows") => SDL.Props.WindowWin32HWNDPointer,
            (HandleType.WindowHandle, "wayland") => SDL.Props.WindowWaylandSurfacePointer,
            (HandleType.WindowHandle, "cocoa") => SDL.Props.WindowCocoaWindowPointer,
            (HandleType.WindowHandle, "x11") => SDL.Props.WindowX11WindowNumber,
            _ => throw new NotSupportedException($"The {handleType} handle is unavailable on the {driver} video driver."),
        };
        var properties = SDL.GetWindowProperties(window);
        if (properties == 0)
            throw SdlFailure("query native window properties");
        var handle = driver == "x11" && handleType == HandleType.WindowHandle
            ? checked((nint)SDL.GetNumberProperty(properties, property, 0))
            : SDL.GetPointerProperty(properties, property, 0);
        return handle != 0 ? handle : throw new InvalidOperationException("The native handle is currently unavailable.");
    }

    /// <summary>Finds the engine-owned window at a desktop position.</summary>
    /// <param name="position">Global desktop point in platform-native coordinates.</param>
    /// <returns><see cref="MainWindowId"/> when the point is inside the visible main window; otherwise <see cref="InvalidWindowId"/>.</returns>
    /// <remarks>On X11, only the client area is counted; the native border and title bar are excluded. Other desktop drivers use decorated bounds.</remarks>
    /// <exception cref="NotSupportedException">The active Wayland compositor does not expose a reliable global window position.</exception>
    public int GetWindowAtScreenPosition(Vector2I position)
    {
        EnsureOwner();
        var flags = SDL.GetWindowFlags(GetWindow(MainWindowId));
        EnsureGlobalWindowCoordinatesAvailable();
        if ((flags & (SDL.WindowFlags.Hidden | SDL.WindowFlags.Minimized)) != 0)
            return InvalidWindowId;
        var x11 = SDL.GetCurrentVideoDriver() == "x11";
        var origin = x11 ? WindowGetPosition() : WindowGetPositionWithDecorations();
        var size = x11 ? WindowGetSize() : WindowGetSizeWithDecorations();
        return (long)position.X >= origin.X && (long)position.X < (long)origin.X + size.X &&
               (long)position.Y >= origin.Y && (long)position.Y < (long)origin.Y + size.Y
            ? MainWindowId : InvalidWindowId;
    }

    /// <summary>Requests that the main window move to another connected display.</summary>
    /// <param name="screen">A zero-based display index or a negative display selector.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>A request for the current screen does nothing. A floating window retains its offset from the source display, clamped so part of it remains in the target work area. Fullscreen and maximized windows keep their mode. The window manager may apply or deny a move asynchronously.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="screen"/> or <paramref name="windowId"/> does not identify an available display or the main window.</exception>
    /// <exception cref="NotSupportedException">Wayland does not allow this top-level window to choose another display.</exception>
    /// <exception cref="InvalidOperationException">A native display query or move fails, or the window state fails to synchronize.</exception>
    /// <exception cref="AggregateException">A transfer fails and restoring the previous window mode also fails.</exception>
    public void WindowSetCurrentScreen(int screen, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        var display = GetDisplayId(screen);
        var currentDisplay = SDL.GetDisplayForWindow(window);
        if (currentDisplay == 0)
            throw SdlFailure("find the window's current display");
        if (display == currentDisplay)
            return;
        EnsureGlobalWindowCoordinatesAvailable();

        var mode = WindowGetMode(windowId);
        if (mode == WindowMode.ExclusiveFullscreen)
        {
            MoveExclusiveFullscreenToDisplay(window, display);
            return;
        }

        if (!SDL.GetDisplayBounds(display, out var targetBounds))
            throw SdlFailure("read the target display bounds");
        if (mode == WindowMode.Fullscreen)
        {
            if (!SDL.SetWindowPosition(window, targetBounds.X, targetBounds.Y))
                throw SdlFailure("move the fullscreen window to a display");
            return;
        }
        if (mode == WindowMode.Maximized)
        {
            if (!SDL.GetWindowPosition(window, out var oldX, out var oldY))
                throw SdlFailure("read the maximized window position");
            MoveMaximizedToPosition(window, targetBounds.X, targetBounds.Y, oldX, oldY);
            return;
        }

        if (!SDL.GetDisplayBounds(currentDisplay, out var sourceBounds) ||
            !SDL.GetDisplayUsableBounds(display, out var usableBounds) ||
            !SDL.GetWindowPosition(window, out var x, out var y) ||
            !SDL.GetWindowSize(window, out var width, out var height))
            throw SdlFailure("read the window and display bounds");

        var targetX = Math.Clamp((long)x - sourceBounds.X + usableBounds.X,
            usableBounds.X, Math.Max((long)usableBounds.X, (long)usableBounds.X + usableBounds.W - width / 3));
        var targetY = Math.Clamp((long)y - sourceBounds.Y + usableBounds.Y,
            usableBounds.Y, Math.Max((long)usableBounds.Y, (long)usableBounds.Y + usableBounds.H - height / 3));
        var requestedX = (int)Math.Clamp(targetX, int.MinValue, int.MaxValue);
        var requestedY = (int)Math.Clamp(targetY, int.MinValue, int.MaxValue);
        if (!SDL.SetWindowPosition(window, requestedX, requestedY))
            throw SdlFailure("move the window to a display");
    }

    private static void MoveMaximizedToPosition(nint window, int x, int y, int oldX, int oldY)
    {
        if (!SDL.RestoreWindow(window))
            throw SdlFailure("restore the maximized window before moving it");
        try
        {
            if (!SDL.SyncWindow(window) || !SDL.SetWindowPosition(window, x, y) ||
                !SDL.SyncWindow(window) || !SDL.MaximizeWindow(window))
                throw SdlFailure("move and remaximize the window");
        }
        catch (Exception failure)
        {
            if (!SDL.RestoreWindow(window) || !SDL.SyncWindow(window) ||
                !SDL.SetWindowPosition(window, oldX, oldY) || !SDL.SyncWindow(window) ||
                !SDL.MaximizeWindow(window))
                throw new AggregateException(failure, SdlFailure("restore the previous maximized window"));
            throw;
        }
    }

    private static void MoveExclusiveFullscreenToDisplay(nint window, uint display)
    {
        var previousMode = SDL.GetWindowFullscreenMode(window) ??
            throw new InvalidOperationException("The exclusive fullscreen window has no selected display mode.");
        if (!SDL.GetClosestFullscreenDisplayMode(display, previousMode.W, previousMode.H,
            previousMode.RefreshRate, true, out var targetMode))
            throw SdlFailure("select a fullscreen mode for the target display");
        if (!SDL.SetWindowFullscreenMode(window, targetMode))
            throw SdlFailure("move the exclusive fullscreen window to a display");
    }

    /// <summary>Gets the current screen refresh rate.</summary>
    /// <param name="screen">Display index or one of the negative display selectors; defaults to the main window's display.</param>
    /// <returns>The current display-mode refresh rate in hertz, using the precise rational value when available, or minus one when unavailable.</returns>
    public float ScreenGetRefreshRate(int screen = ScreenOfMainWindow)
    {
        EnsureOwner();
        if (!TryGetDisplayId(screen, out var displayId))
            return -1f;
        var mode = SDL.GetCurrentDisplayMode(displayId);
        var rate = mode is { RefreshRateNumerator: > 0, RefreshRateDenominator: > 0 } precise
            ? (float)precise.RefreshRateNumerator / precise.RefreshRateDenominator
            : mode?.RefreshRate ?? 0f;
        return float.IsFinite(rate) && rate > 0f ? rate : -1f;
    }

    /// <summary>Gets the largest reported content scale among connected displays.</summary>
    /// <returns>The maximum positive logical-to-physical ratio, at least one even when no display is connected.</returns>
    public float ScreenGetMaxScale()
    {
        EnsureOwner();
        var count = GetScreenCount();
        var maximum = 1f;
        for (var screen = 0; screen < count; screen++)
            maximum = Math.Max(maximum, ScreenGetScale(screen));
        return maximum;
    }

    /// <summary>Gets the requested minimum size of the main window.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The requested pixel dimensions on Wayland or native window dimensions elsewhere; zero means no bound on that axis.</returns>
    public Vector2I WindowGetMinSize(int windowId = MainWindowId)
    {
        EnsureOwner();
        if (_waylandWindowPosition)
        {
            _ = GetWindow(windowId);
            return _waylandMinimumSize;
        }
        if (!SDL.GetWindowMinimumSize(GetWindow(windowId), out var width, out var height))
            throw SdlFailure("read minimum window size");
        return new Vector2I(width, height);
    }

    /// <summary>Requests minimum dimensions for the main window.</summary>
    /// <param name="size">Nonnegative lower bounds in client pixels on Wayland and native window coordinates elsewhere; zero leaves that axis unbounded.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>Wayland rounds each pixel lower bound upward to native window coordinates and reapplies it when the window pixel density changes.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A bound is negative, exceeds a nonzero maximum, or cannot coexist with it after native pixel-density conversion.</exception>
    public void WindowSetMinSize(Vector2I size, int windowId = MainWindowId)
    {
        EnsureOwner();
        if (size.X < 0 || size.Y < 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Minimum dimensions cannot be negative.");
        var maximum = WindowGetMaxSize(windowId);
        if ((maximum.X > 0 && size.X > maximum.X) || (maximum.Y > 0 && size.Y > maximum.Y))
            throw new ArgumentOutOfRangeException(nameof(size), size, "Minimum dimensions cannot exceed the maximum dimensions.");
        var window = GetWindow(windowId);
        var nativeSize = _waylandWindowPosition ? WaylandLogicalWindowLimit(size, window, minimum: true) : size;
        if (_waylandWindowPosition)
        {
            var nativeMaximum = WaylandLogicalWindowLimit(maximum, window, minimum: false);
            if ((nativeMaximum.X > 0 && nativeSize.X > nativeMaximum.X) ||
                (nativeMaximum.Y > 0 && nativeSize.Y > nativeMaximum.Y))
                throw new ArgumentOutOfRangeException(nameof(size), size, "The minimum and maximum cannot both be represented at the current pixel density.");
        }
        if (!SDL.SetWindowMinimumSize(window, nativeSize.X, nativeSize.Y))
            throw SdlFailure("set minimum window size");
        if (_waylandWindowPosition)
            _waylandMinimumSize = size;
    }

    /// <summary>Gets the requested maximum size of the main window.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The requested pixel dimensions on Wayland or native window dimensions elsewhere; zero means no bound on that axis.</returns>
    public Vector2I WindowGetMaxSize(int windowId = MainWindowId)
    {
        EnsureOwner();
        if (_waylandWindowPosition)
        {
            _ = GetWindow(windowId);
            return _waylandMaximumSize;
        }
        if (!SDL.GetWindowMaximumSize(GetWindow(windowId), out var width, out var height))
            throw SdlFailure("read maximum window size");
        return new Vector2I(width, height);
    }

    /// <summary>Requests maximum dimensions for the main window.</summary>
    /// <param name="size">Nonnegative upper bounds in client pixels on Wayland and native window coordinates elsewhere; zero leaves that axis unbounded.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>Wayland rounds each pixel upper bound downward to native window coordinates and reapplies it when the window pixel density changes.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A bound is negative, falls below a nonzero minimum, or cannot represent a finite maximum after native pixel-density conversion.</exception>
    public void WindowSetMaxSize(Vector2I size, int windowId = MainWindowId)
    {
        EnsureOwner();
        if (size.X < 0 || size.Y < 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Maximum dimensions cannot be negative.");
        var minimum = WindowGetMinSize(windowId);
        if ((size.X > 0 && size.X < minimum.X) || (size.Y > 0 && size.Y < minimum.Y))
            throw new ArgumentOutOfRangeException(nameof(size), size, "Maximum dimensions cannot be smaller than the minimum dimensions.");
        var window = GetWindow(windowId);
        var nativeSize = _waylandWindowPosition ? WaylandLogicalWindowLimit(size, window, minimum: false) : size;
        if (_waylandWindowPosition)
        {
            var nativeMinimum = WaylandLogicalWindowLimit(minimum, window, minimum: true);
            if ((nativeSize.X > 0 && nativeSize.X < nativeMinimum.X) ||
                (nativeSize.Y > 0 && nativeSize.Y < nativeMinimum.Y))
                throw new ArgumentOutOfRangeException(nameof(size), size, "The minimum and maximum cannot both be represented at the current pixel density.");
        }
        if (!SDL.SetWindowMaximumSize(window, nativeSize.X, nativeSize.Y))
            throw SdlFailure("set maximum window size");
        if (_waylandWindowPosition)
            _waylandMaximumSize = size;
    }

    private void ReapplyWaylandWindowLimits()
    {
        var window = GetWindow(MainWindowId);
        var minimum = WaylandLogicalWindowLimit(_waylandMinimumSize, window, minimum: true);
        var maximum = WaylandLogicalWindowLimit(_waylandMaximumSize, window, minimum: false);
        if (!SDL.SetWindowMinimumSize(window, minimum.X, minimum.Y) ||
            !SDL.SetWindowMaximumSize(window, maximum.X, maximum.Y))
            throw SdlFailure("update window size limits for the pixel density");
    }

    /// <summary>Gets the main window's current native mode.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The state reported by SDL; an exclusive display mode is distinguished from desktop fullscreen. Wayland can retain a requested minimized state until the window regains focus.</returns>
    public WindowMode WindowGetMode(int windowId = MainWindowId)
    {
        EnsureOwner();
        var flags = SDL.GetWindowFlags(GetWindow(windowId));
        if ((flags & SDL.WindowFlags.Fullscreen) != 0)
            return SDL.GetWindowFullscreenMode(GetWindow(windowId)) is null
                ? WindowMode.Fullscreen : WindowMode.ExclusiveFullscreen;
        if ((flags & SDL.WindowFlags.Minimized) != 0)
            return WindowMode.Minimized;
        if ((flags & SDL.WindowFlags.Maximized) != 0)
            return WindowMode.Maximized;
        return WindowMode.Windowed;
    }

    /// <summary>Requests a native main-window mode.</summary>
    /// <param name="mode">Windowed, minimized, maximized, borderless fullscreen, or exclusive fullscreen.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>On Wayland, exclusive fullscreen uses the compositor's ordinary fullscreen request and is observed as <see cref="WindowMode.Fullscreen"/> if accepted. Wayland has no reliable programmatic restoration from a minimized state; the minimized flag can remain set until the window regains focus. Elsewhere exclusive mode chooses the closest native mode to the current logical window size. Mode transitions may complete asynchronously; call <see cref="WindowGetMode"/> for the reported state.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is unknown.</exception>
    /// <exception cref="InvalidOperationException">No suitable exclusive mode exists or the native window manager rejects a transition.</exception>
    /// <exception cref="AggregateException">A fullscreen transition and restoration of the previous mode both fail.</exception>
    public void WindowSetMode(WindowMode mode, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        var success = mode switch
        {
            WindowMode.Windowed => SDL.SetWindowFullscreen(window, false) && SDL.RestoreWindow(window),
            WindowMode.Minimized => SDL.MinimizeWindow(window),
            WindowMode.Maximized => SDL.SetWindowFullscreen(window, false) && SDL.MaximizeWindow(window),
            WindowMode.Fullscreen => SetFullscreen(window, null),
            WindowMode.ExclusiveFullscreen => SDL.GetCurrentVideoDriver() == "wayland"
                ? SetFullscreen(window, null) : SetExclusiveFullscreen(window),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown window mode."),
        };
        if (!success)
            throw SdlFailure("set window mode");
    }

    private bool SetExclusiveFullscreen(nint window)
    {
        var display = SDL.GetDisplayForWindow(window);
        if (display == 0 || !SDL.GetWindowSize(window, out var width, out var height) ||
            !SDL.GetClosestFullscreenDisplayMode(display, width, height, 0f, true, out var mode))
            throw SdlFailure("select an exclusive fullscreen mode");
        return SetFullscreen(window, mode);
    }

    private static bool SetFullscreen(nint window, SDL.DisplayMode? mode)
    {
        var previousMode = SDL.GetWindowFullscreenMode(window);
        var selected = mode is { } value
            ? SDL.SetWindowFullscreenMode(window, value)
            : SDL.SetWindowFullscreenMode(window, 0);
        if (!selected)
            return false;
        if (SDL.SetWindowFullscreen(window, true))
            return true;
        var failure = SdlFailure("enter fullscreen");
        var restored = previousMode is { } previous
            ? SDL.SetWindowFullscreenMode(window, previous)
            : SDL.SetWindowFullscreenMode(window, 0);
        if (!restored)
            throw new AggregateException(failure, SdlFailure("restore the previous fullscreen mode"));
        throw failure;
    }

    /// <summary>Gets whether the main window currently has native focus.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns><see langword="true"/> when the window is focused.</returns>
    /// <remarks>On Wayland, focus follows SDL's mouse-focused window; tablet and touch focus are not merged into this query. Other video drivers use keyboard focus.</remarks>
    public bool WindowIsFocused(int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        return SDL.GetCurrentVideoDriver() == "wayland"
            ? SDL.GetMouseFocus() == window
            : (SDL.GetWindowFlags(window) & SDL.WindowFlags.InputFocus) != 0;
    }

    /// <summary>Reads a supported native window flag.</summary>
    /// <param name="flag">Policy to inspect.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>Whether the policy is enabled in the observed native flags.</returns>
    /// <remarks>On Wayland, <see cref="WindowFlag.AlwaysOnTop"/> and <see cref="WindowFlag.NoFocus"/> are unavailable for the main window.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="flag"/> is not a defined policy.</exception>
    /// <exception cref="NotSupportedException">The defined policy requires a display capability that is not integrated.</exception>
    public bool WindowGetFlag(WindowFlag flag, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        if (SDL.GetCurrentVideoDriver() == "wayland" && flag is (WindowFlag.AlwaysOnTop or WindowFlag.NoFocus))
            throw new NotSupportedException($"Window flag {flag} is unavailable for a Wayland top-level window.");
        var native = SDL.GetWindowFlags(window);
        return flag switch
        {
            WindowFlag.Borderless => (native & SDL.WindowFlags.Borderless) != 0,
            WindowFlag.ResizeDisabled => (native & SDL.WindowFlags.Resizable) == 0,
            WindowFlag.AlwaysOnTop => (native & SDL.WindowFlags.AlwaysOnTop) != 0,
            WindowFlag.NoFocus => (native & SDL.WindowFlags.NotFocusable) != 0,
            >= WindowFlag.Transparent and < WindowFlag.Max => throw new NotSupportedException($"Window flag {flag} requires a display capability that is not integrated."),
            _ => throw new ArgumentOutOfRangeException(nameof(flag), flag, "Unknown window flag."),
        };
    }

    /// <summary>Requests a supported native window policy.</summary>
    /// <param name="flag">Policy to change.</param>
    /// <param name="enabled">Whether to enable the policy.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>A platform may reject the policy or apply it asynchronously. Wayland cannot apply <see cref="WindowFlag.AlwaysOnTop"/> to an ordinary top-level window, and SDL can change its internal focusable flag before rejecting <see cref="WindowFlag.NoFocus"/> there; both requests are rejected before native mutation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="flag"/> is not a defined policy.</exception>
    /// <exception cref="NotSupportedException">The defined policy requires a display capability that is not integrated.</exception>
    public void WindowSetFlag(WindowFlag flag, bool enabled, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        if (SDL.GetCurrentVideoDriver() == "wayland" && flag is (WindowFlag.AlwaysOnTop or WindowFlag.NoFocus))
            throw new NotSupportedException($"Window flag {flag} is unavailable for a Wayland top-level window.");
        var success = flag switch
        {
            WindowFlag.Borderless => SDL.SetWindowBordered(window, !enabled),
            WindowFlag.ResizeDisabled => SDL.SetWindowResizable(window, !enabled),
            WindowFlag.AlwaysOnTop => SDL.SetWindowAlwaysOnTop(window, enabled),
            WindowFlag.NoFocus => SDL.SetWindowFocusable(window, !enabled),
            >= WindowFlag.Transparent and < WindowFlag.Max => throw new NotSupportedException($"Window flag {flag} requires a display capability that is not integrated."),
            _ => throw new ArgumentOutOfRangeException(nameof(flag), flag, "Unknown window flag."),
        };
        if (!success)
            throw SdlFailure("change window flag");
    }

    /// <summary>Gets whether the current SDL resize policy permits a maximize request.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns><see langword="true"/> when the window is resizable, regardless of its current mode.</returns>
    /// <remarks>The Wayland compositor may independently disable maximization through its window-manager capabilities; SDL does not expose that native policy here. A fullscreen window can leave fullscreen before requesting maximization.</remarks>
    public bool WindowIsMaximizeAllowed(int windowId = MainWindowId)
    {
        EnsureOwner();
        var flags = SDL.GetWindowFlags(GetWindow(windowId));
        return (flags & SDL.WindowFlags.Resizable) != 0;
    }

    /// <summary>Requests that the window manager bring the main window to the foreground.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>Wayland has no standard foreground request for an existing top-level window, so this is a no-op there. Other window managers may refuse a native raise request under their focus policy.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> does not identify the main window.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread or the native raise request fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server is disposing or disposed.</exception>
    public void WindowMoveToForeground(int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        if (SDL.GetCurrentVideoDriver() == "wayland")
            return;
        if (!SDL.RaiseWindow(window))
            throw SdlFailure("raise the window");
    }

    /// <summary>Requests user attention until the main window receives focus.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>On Wayland this submits an activation request without an input serial, allowing the compositor to mark the window urgent without switching focus. The platform chooses how, or whether, to display the request; a focused window may show no effect.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> does not identify the main window.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread or the native attention request fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server is disposing or disposed.</exception>
    public void WindowRequestAttention(int windowId = MainWindowId)
    {
        EnsureOwner();
        if (!SDL.FlashWindow(GetWindow(windowId), SDL.FlashOperation.UntilFocused))
            throw SdlFailure("request window attention");
    }

    /// <summary>Gets the main-window position including its left and top decorations.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The outer upper-left position in platform-native desktop coordinates.</returns>
    /// <exception cref="NotSupportedException">The active Wayland compositor does not expose a reliable global window position.</exception>
    public Vector2I WindowGetPositionWithDecorations(int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        EnsureGlobalWindowCoordinatesAvailable();
        if (!SDL.GetWindowPosition(window, out var x, out var y) ||
            !SDL.GetWindowBordersSize(window, out var top, out var left, out _, out _))
            throw SdlFailure("read decorated window position");
        return new Vector2I(checked(x - left), checked(y - top));
    }

    /// <summary>Gets the main-window size including native decorations.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>Outer window dimensions in platform-native window coordinates; on Wayland, the client size in pixels.</returns>
    /// <remarks>Wayland does not provide a reliable server-side decoration size for a top-level window.</remarks>
    public Vector2I WindowGetSizeWithDecorations(int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        if (SDL.GetCurrentVideoDriver() == "wayland")
            return WindowGetSize(windowId);
        if (!SDL.GetWindowSize(window, out var width, out var height) ||
            !SDL.GetWindowBordersSize(window, out var top, out var left, out var bottom, out var right))
            throw SdlFailure("read decorated window size");
        return new Vector2I(checked(width + left + right), checked(height + top + bottom));
    }

    /// <summary>Requests a taskbar progress state for the main window where the desktop supports it.</summary>
    /// <param name="state">The indication to show.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="state"/> is unknown.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> does not identify the main window.</exception>
    /// <exception cref="InvalidOperationException">The call is off the opening thread or a supported native backend rejects the request.</exception>
    /// <exception cref="NotSupportedException">Taskbar progress integration is unavailable on the active Wayland backend.</exception>
    /// <exception cref="ObjectDisposedException">The display server has been disposed.</exception>
    /// <remarks>Wayland needs a verified desktop entry and a desktop environment that accepts progress notifications. A successful native setter alone does not establish that the indication is visible.</remarks>
    public void WindowSetTaskbarProgressState(ProgressState state, int windowId = MainWindowId)
    {
        EnsureOwner();
        var native = state switch
        {
            ProgressState.None => SDL.ProgressState.None,
            ProgressState.Indeterminate => SDL.ProgressState.Indeterminate,
            ProgressState.Normal => SDL.ProgressState.Normal,
            ProgressState.Error => SDL.ProgressState.Error,
            ProgressState.Paused => SDL.ProgressState.Paused,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown progress state."),
        };
        var window = GetWindow(windowId);
        if (SDL.GetCurrentVideoDriver() == "wayland")
            throw new NotSupportedException("Taskbar progress is unavailable without verified Wayland desktop integration.");
        if (!SDL.SetWindowProgressState(window, native))
            throw SdlFailure("set taskbar progress state");
    }

    /// <summary>Requests a taskbar progress fraction for the main window where the desktop supports it.</summary>
    /// <param name="value">A finite fraction from zero through one.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is outside the supported range.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> does not identify the main window.</exception>
    /// <exception cref="InvalidOperationException">The call is off the opening thread or a supported native backend rejects the request.</exception>
    /// <exception cref="NotSupportedException">Taskbar progress integration is unavailable on the active Wayland backend.</exception>
    /// <exception cref="ObjectDisposedException">The display server has been disposed.</exception>
    /// <remarks>Wayland needs a verified desktop entry and a desktop environment that accepts progress notifications. A successful native setter alone does not establish that the value is visible.</remarks>
    public void WindowSetTaskbarProgressValue(float value, int windowId = MainWindowId)
    {
        EnsureOwner();
        if (!float.IsFinite(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Progress must be between zero and one.");
        var window = GetWindow(windowId);
        if (SDL.GetCurrentVideoDriver() == "wayland")
            throw new NotSupportedException("Taskbar progress is unavailable without verified Wayland desktop integration.");
        if (!SDL.SetWindowProgressValue(window, value))
            throw SdlFailure("set taskbar progress value");
    }
}
