using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Identifies a borrowed native display, window or graphics-context handle.</summary>
    public enum HandleType
    {
        /// <summary>The X11 or Wayland display connection.</summary>
        DisplayHandle = 0,
        /// <summary>The platform window: an X11 window ID, Wayland surface, Win32 HWND, or Cocoa NSWindow.</summary>
        WindowHandle = 1,
        /// <summary>The compatibility renderer's GL context on Linux Wayland or X11.</summary>
        OpenGLContext = 3,
        /// <summary>The EGL display associated with the Linux compatibility renderer's context.</summary>
        EGLDisplay = 4,
        /// <summary>The EGL configuration associated with the Linux compatibility renderer's context.</summary>
        EGLConfig = 5,
        /// <summary>The visual ID associated with the X11 compatibility renderer's GLX context.</summary>
        GLXVisualID = 6,
        /// <summary>The framebuffer configuration associated with the X11 compatibility renderer's GLX context.</summary>
        GLXFBConfig = 7,
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

    internal int WindowGetCurrentScreenCore(int windowId = MainWindowId)
    {
        EnsureOwner();
        if (windowId != MainWindowId)
            return InvalidScreen;
        var displayId = SDL.GetDisplayForWindow(GetWindow(windowId));
        return Array.IndexOf(GetDisplays(), displayId);
    }

    internal int GetKeyboardFocusScreenCore()
    {
        EnsureOwner();
        return SDL.GetCurrentVideoDriver() != "wayland" && SDL.GetKeyboardFocus() == _window.DangerousGetHandle()
            ? WindowGetCurrentScreenCore() : GetPrimaryScreenCore();
    }

    internal int GetScreenFromRectCore(Rect2 rectangle)
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
                : new Vector2i(bounds.W, bounds.H);
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

    internal int[] GetWindowListCore()
    {
        EnsureOwner();
        return [MainWindowId];
    }

    internal nint WindowGetNativeHandleCore(HandleType handleType, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        if (handleType is not (HandleType.DisplayHandle or HandleType.WindowHandle or HandleType.OpenGLContext or
            HandleType.EGLDisplay or HandleType.EGLConfig or HandleType.GLXVisualID or HandleType.GLXFBConfig))
            throw new ArgumentOutOfRangeException(nameof(handleType), handleType, "Unknown native handle type.");
        if (handleType is not (HandleType.DisplayHandle or HandleType.WindowHandle))
            return _graphicsHandleQuery?.Invoke(handleType) ??
                throw new NotSupportedException("This window has no active graphics-context provider.");

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
            throw SDLFailure("query native window properties");
        var handle = driver == "x11" && handleType == HandleType.WindowHandle
            ? checked((nint)SDL.GetNumberProperty(properties, property, 0))
            : SDL.GetPointerProperty(properties, property, 0);
        return handle != 0 ? handle : throw new InvalidOperationException("The native handle is currently unavailable.");
    }

    internal int GetWindowAtScreenPositionCore(Vector2i position)
    {
        EnsureOwner();
        var flags = SDL.GetWindowFlags(GetWindow(MainWindowId));
        EnsureGlobalWindowCoordinatesAvailable();
        if ((flags & (SDL.WindowFlags.Hidden | SDL.WindowFlags.Minimized)) != 0)
            return InvalidWindowId;
        var x11 = SDL.GetCurrentVideoDriver() == "x11";
        var origin = x11 ? WindowGetPositionCore() : WindowGetPositionWithDecorationsCore();
        var size = x11 ? WindowGetSizeCore() : WindowGetSizeWithDecorationsCore();
        return (long)position.X >= origin.X && (long)position.X < (long)origin.X + size.X &&
               (long)position.Y >= origin.Y && (long)position.Y < (long)origin.Y + size.Y
            ? MainWindowId : InvalidWindowId;
    }

    internal void WindowSetCurrentScreenCore(int screen, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        var display = GetDisplayID(screen);
        var currentDisplay = SDL.GetDisplayForWindow(window);
        if (currentDisplay == 0)
            throw SDLFailure("find the window's current display");
        if (display == currentDisplay)
            return;
        EnsureGlobalWindowCoordinatesAvailable();

        var mode = WindowGetModeCore(windowId);
        if (mode == WindowMode.ExclusiveFullscreen)
        {
            MoveExclusiveFullscreenToDisplay(window, display);
            return;
        }

        if (!SDL.GetDisplayBounds(display, out var targetBounds))
            throw SDLFailure("read the target display bounds");
        if (mode == WindowMode.Fullscreen)
        {
            if (!SDL.SetWindowPosition(window, targetBounds.X, targetBounds.Y))
                throw SDLFailure("move the fullscreen window to a display");
            return;
        }
        if (mode == WindowMode.Maximized)
        {
            if (!SDL.GetWindowPosition(window, out var oldX, out var oldY))
                throw SDLFailure("read the maximized window position");
            MoveMaximizedToPosition(window, targetBounds.X, targetBounds.Y, oldX, oldY);
            return;
        }

        if (!SDL.GetDisplayBounds(currentDisplay, out var sourceBounds) ||
            !SDL.GetDisplayUsableBounds(display, out var usableBounds) ||
            !SDL.GetWindowPosition(window, out var x, out var y) ||
            !SDL.GetWindowSize(window, out var width, out var height))
            throw SDLFailure("read the window and display bounds");

        var targetX = Math.Clamp((long)x - sourceBounds.X + usableBounds.X,
            usableBounds.X, Math.Max((long)usableBounds.X, (long)usableBounds.X + usableBounds.W - width / 3));
        var targetY = Math.Clamp((long)y - sourceBounds.Y + usableBounds.Y,
            usableBounds.Y, Math.Max((long)usableBounds.Y, (long)usableBounds.Y + usableBounds.H - height / 3));
        var requestedX = (int)Math.Clamp(targetX, int.MinValue, int.MaxValue);
        var requestedY = (int)Math.Clamp(targetY, int.MinValue, int.MaxValue);
        if (!SDL.SetWindowPosition(window, requestedX, requestedY))
            throw SDLFailure("move the window to a display");
    }

    private static void MoveMaximizedToPosition(nint window, int x, int y, int oldX, int oldY)
    {
        if (!SDL.RestoreWindow(window))
            throw SDLFailure("restore the maximized window before moving it");
        try
        {
            if (!SDL.SyncWindow(window) || !SDL.SetWindowPosition(window, x, y) ||
                !SDL.SyncWindow(window) || !SDL.MaximizeWindow(window))
                throw SDLFailure("move and remaximize the window");
        }
        catch (Exception failure)
        {
            if (!SDL.RestoreWindow(window) || !SDL.SyncWindow(window) ||
                !SDL.SetWindowPosition(window, oldX, oldY) || !SDL.SyncWindow(window) ||
                !SDL.MaximizeWindow(window))
                throw new AggregateException(failure, SDLFailure("restore the previous maximized window"));
            throw;
        }
    }

    private static void MoveExclusiveFullscreenToDisplay(nint window, uint display)
    {
        var previousMode = SDL.GetWindowFullscreenMode(window) ??
            throw new InvalidOperationException("The exclusive fullscreen window has no selected display mode.");
        if (!SDL.GetClosestFullscreenDisplayMode(display, previousMode.W, previousMode.H,
            previousMode.RefreshRate, true, out var targetMode))
            throw SDLFailure("select a fullscreen mode for the target display");
        if (!SDL.SetWindowFullscreenMode(window, targetMode))
            throw SDLFailure("move the exclusive fullscreen window to a display");
    }

    internal float ScreenGetRefreshRateCore(int screen = ScreenOfMainWindow)
    {
        EnsureOwner();
        if (!TryGetDisplayID(screen, out var displayId))
            return -1f;
        var mode = SDL.GetCurrentDisplayMode(displayId);
        var rate = mode is { RefreshRateNumerator: > 0, RefreshRateDenominator: > 0 } precise
            ? (float)precise.RefreshRateNumerator / precise.RefreshRateDenominator
            : mode?.RefreshRate ?? 0f;
        return float.IsFinite(rate) && rate > 0f ? rate : -1f;
    }

    internal float ScreenGetMaxScaleCore()
    {
        EnsureOwner();
        var count = GetScreenCountCore();
        var maximum = 1f;
        for (var screen = 0; screen < count; screen++)
            maximum = Math.Max(maximum, ScreenGetScaleCore(screen));
        return maximum;
    }

    internal Vector2i WindowGetMinSizeCore(int windowId = MainWindowId)
    {
        EnsureOwner();
        if (_waylandWindowPosition)
        {
            _ = GetWindow(windowId);
            return _waylandMinimumSize;
        }
        if (!SDL.GetWindowMinimumSize(GetWindow(windowId), out var width, out var height))
            throw SDLFailure("read minimum window size");
        return new Vector2i(width, height);
    }

    internal void WindowSetMinSizeCore(Vector2i size, int windowId = MainWindowId)
    {
        EnsureOwner();
        if (size.X < 0 || size.Y < 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Minimum dimensions cannot be negative.");
        var maximum = WindowGetMaxSizeCore(windowId);
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
            throw SDLFailure("set minimum window size");
        if (_waylandWindowPosition)
            _waylandMinimumSize = size;
    }

    internal Vector2i WindowGetMaxSizeCore(int windowId = MainWindowId)
    {
        EnsureOwner();
        if (_waylandWindowPosition)
        {
            _ = GetWindow(windowId);
            return _waylandMaximumSize;
        }
        if (!SDL.GetWindowMaximumSize(GetWindow(windowId), out var width, out var height))
            throw SDLFailure("read maximum window size");
        return new Vector2i(width, height);
    }

    internal void WindowSetMaxSizeCore(Vector2i size, int windowId = MainWindowId)
    {
        EnsureOwner();
        if (size.X < 0 || size.Y < 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Maximum dimensions cannot be negative.");
        var minimum = WindowGetMinSizeCore(windowId);
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
            throw SDLFailure("set maximum window size");
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
            throw SDLFailure("update window size limits for the pixel density");
    }

    internal WindowMode WindowGetModeCore(int windowId = MainWindowId)
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

    internal void WindowSetModeCore(WindowMode mode, int windowId = MainWindowId)
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
            throw SDLFailure("set window mode");
    }

    private bool SetExclusiveFullscreen(nint window)
    {
        var display = SDL.GetDisplayForWindow(window);
        if (display == 0 || !SDL.GetWindowSize(window, out var width, out var height) ||
            !SDL.GetClosestFullscreenDisplayMode(display, width, height, 0f, true, out var mode))
            throw SDLFailure("select an exclusive fullscreen mode");
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
        var failure = SDLFailure("enter fullscreen");
        var restored = previousMode is { } previous
            ? SDL.SetWindowFullscreenMode(window, previous)
            : SDL.SetWindowFullscreenMode(window, 0);
        if (!restored)
            throw new AggregateException(failure, SDLFailure("restore the previous fullscreen mode"));
        throw failure;
    }

    internal bool WindowIsFocusedCore(int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        return SDL.GetCurrentVideoDriver() == "wayland"
            ? SDL.GetMouseFocus() == window
            : (SDL.GetWindowFlags(window) & SDL.WindowFlags.InputFocus) != 0;
    }

    internal bool WindowGetFlagCore(WindowFlag flag, int windowId = MainWindowId)
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

    internal void WindowSetFlagCore(WindowFlag flag, bool enabled, int windowId = MainWindowId)
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
            throw SDLFailure("change window flag");
    }

    internal bool WindowIsMaximizeAllowedCore(int windowId = MainWindowId)
    {
        EnsureOwner();
        var flags = SDL.GetWindowFlags(GetWindow(windowId));
        return (flags & SDL.WindowFlags.Resizable) != 0;
    }

    internal void WindowMoveToForegroundCore(int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        if (SDL.GetCurrentVideoDriver() == "wayland")
            return;
        if (!SDL.RaiseWindow(window))
            throw SDLFailure("raise the window");
    }

    internal void WindowRequestAttentionCore(int windowId = MainWindowId)
    {
        EnsureOwner();
        if (!SDL.FlashWindow(GetWindow(windowId), SDL.FlashOperation.UntilFocused))
            throw SDLFailure("request window attention");
    }

    internal Vector2i WindowGetPositionWithDecorationsCore(int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        EnsureGlobalWindowCoordinatesAvailable();
        if (!SDL.GetWindowPosition(window, out var x, out var y) ||
            !SDL.GetWindowBordersSize(window, out var top, out var left, out _, out _))
            throw SDLFailure("read decorated window position");
        return new Vector2i(checked(x - left), checked(y - top));
    }

    internal Vector2i WindowGetSizeWithDecorationsCore(int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        if (SDL.GetCurrentVideoDriver() == "wayland")
            return WindowGetSizeCore(windowId);
        if (!SDL.GetWindowSize(window, out var width, out var height) ||
            !SDL.GetWindowBordersSize(window, out var top, out var left, out var bottom, out var right))
            throw SDLFailure("read decorated window size");
        return new Vector2i(checked(width + left + right), checked(height + top + bottom));
    }

    internal void WindowSetTaskbarProgressStateCore(ProgressState state, int windowId = MainWindowId)
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
            throw SDLFailure("set taskbar progress state");
    }

    internal void WindowSetTaskbarProgressValueCore(float value, int windowId = MainWindowId)
    {
        EnsureOwner();
        if (!float.IsFinite(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Progress must be between zero and one.");
        var window = GetWindow(windowId);
        if (SDL.GetCurrentVideoDriver() == "wayland")
            throw new NotSupportedException("Taskbar progress is unavailable without verified Wayland desktop integration.");
        if (!SDL.SetWindowProgressValue(window, value))
            throw SDLFailure("set taskbar progress value");
    }
}
