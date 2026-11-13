using System.ComponentModel;
using System.Runtime.InteropServices;
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
        if (windowId != MainWindowId)
        {
            EnsureOwner();
            return (_rootDisplay ?? this)._childWindows.TryGetValue(windowId, out var child) ? child.WindowGetCurrentScreenCore() : InvalidScreen;
        }
        EnsureOwner();
        if (windowId != MainWindowId)
            return InvalidScreen;
        var displayId = SDL.GetDisplayForWindow(GetWindow(windowId));
        return Array.IndexOf(GetDisplays(), displayId);
    }

    internal int GetKeyboardFocusScreenCore()
    {
        EnsureOwner();
        return SDL.GetCurrentVideoDriver() != "wayland" && FocusedWindow() is { } focused
            ? focused.WindowGetCurrentScreenCore() : GetPrimaryScreenCore();
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
        var root = _rootDisplay ?? this;
        var ids = new int[root._childWindows.Count + 1];
        var index = 1;
        foreach (var id in root._childWindows.Keys) ids[index++] = id;
        return ids;
    }

    internal nint WindowGetNativeHandleCore(HandleType handleType, int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) return ResolveWindow(windowId).WindowGetNativeHandleCore(handleType, MainWindowId);
        EnsureOwner();
        var window = GetWindow(windowId);
        if (handleType is not (HandleType.DisplayHandle or HandleType.WindowHandle or HandleType.OpenGLContext or
            HandleType.EGLDisplay or HandleType.EGLConfig or HandleType.GLXVisualID or HandleType.GLXFBConfig))
            throw new ArgumentOutOfRangeException(nameof(handleType), handleType, "Unknown native handle type.");
        if (handleType is not (HandleType.DisplayHandle or HandleType.WindowHandle))
            return _graphicsHandleQuery?.Invoke(handleType) ??
                throw new NotSupportedException("This window has no active graphics-context provider.");

        var driver = _videoDriver;
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

    internal bool CanDrawWindow()
    {
        EnsureOwner();
        return _outputBackend is not null && _videoDriver is not ("dummy" or "offscreen") &&
            RenderingServer.Service?.RenderLoopEnabledCore == true && IsNativePresentationVisible;
    }

    internal bool IsNativePresentationVisible =>
        (SDL.GetWindowFlags(_window.DangerousGetHandle()) & (SDL.WindowFlags.Hidden | SDL.WindowFlags.Minimized)) == 0;

    internal int GetWindowAtScreenPositionCore(Vector2i position)
    {
        EnsureOwner(); EnsureGlobalWindowCoordinatesAvailable();
        var root = _rootDisplay ?? this;
        if (root.NativeContext(SDL.GetMouseFocus()) is { } focused && focused.ContainsScreenPosition(position)) return focused.WindowID;
        foreach (var child in root._childWindows.Values) if (child.ContainsScreenPosition(position)) return child.WindowID;
        return root.ContainsScreenPosition(position) ? MainWindowId : InvalidWindowId;
    }

    private bool ContainsScreenPosition(Vector2i position)
    {
        var flags = SDL.GetWindowFlags(_window.DangerousGetHandle());
        if ((flags & (SDL.WindowFlags.Hidden | SDL.WindowFlags.Minimized)) != 0) return false;
        var x11 = SDL.GetCurrentVideoDriver() == "x11";
        var origin = x11 ? WindowGetPositionCore() : WindowGetPositionWithDecorationsCore();
        var size = x11 ? WindowGetSizeCore() : WindowGetSizeWithDecorationsCore();
        return (long)position.X >= origin.X && (long)position.X < (long)origin.X + size.X &&
               (long)position.Y >= origin.Y && (long)position.Y < (long)origin.Y + size.Y;
    }

    internal void WindowSetCurrentScreenCore(int screen, int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) { ResolveWindow(windowId).WindowSetCurrentScreenCore(screen, MainWindowId); return; }
        EnsureOwner();
        var window = GetWindow(windowId);
        var display = GetDisplayID(screen);
        var currentDisplay = SDL.GetDisplayForWindow(window);
        if (currentDisplay == 0)
            throw SDLFailure("find the window's current display");
        if (display == currentDisplay)
            return;
        if (IsNativePopup) throw new NotSupportedException("A native popup follows its parent's display; move the parent window instead.");
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
        if (windowId != MainWindowId) return ResolveWindow(windowId).WindowGetMinSizeCore();
        EnsureOwner();
        return _requestedMinimumSize ?? ReadNativeWindowLimit(minimum: true);
    }

    internal void WindowSetMinSizeCore(Vector2i size, int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) { ResolveWindow(windowId).WindowSetMinSizeCore(size); return; }
        ApplyWindowLimits(size, WindowGetMaxSizeCore(), _contentMinimumSize);
    }

    internal Vector2i WindowGetMaxSizeCore(int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) return ResolveWindow(windowId).WindowGetMaxSizeCore();
        EnsureOwner();
        return _requestedMaximumSize ?? ReadNativeWindowLimit(minimum: false);
    }

    internal void WindowSetMaxSizeCore(Vector2i size, int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) { ResolveWindow(windowId).WindowSetMaxSizeCore(size); return; }
        ApplyWindowLimits(WindowGetMinSizeCore(), size, _contentMinimumSize);
    }

    internal void SetWindowContentMinimum(Vector2i minimum, Vector2i? resize)
    {
        EnsureOwner();
        if (_contentMinimumSize != minimum)
            ApplyWindowLimits(WindowGetMinSizeCore(), WindowGetMaxSizeCore(), minimum, resize);
        else if (resize is { } size)
            WindowSetSizeCore(size);
    }

    private void ReapplyWaylandWindowLimits() =>
        ApplyWindowLimits(WindowGetMinSizeCore(), WindowGetMaxSizeCore(), _contentMinimumSize);

    private static bool LimitsConflict(Vector2i minimum, Vector2i maximum) =>
        maximum.X > 0 && minimum.X > maximum.X || maximum.Y > 0 && minimum.Y > maximum.Y;

    private static Vector2i CapWindowMinimum(Vector2i minimum, Vector2i maximum) =>
        new(maximum.X > 0 ? Math.Min(minimum.X, maximum.X) : minimum.X,
            maximum.Y > 0 ? Math.Min(minimum.Y, maximum.Y) : minimum.Y);

    private Vector2i ReadNativeWindowLimit(bool minimum)
    {
        var window = _window.DangerousGetHandle();
        var success = minimum ? SDL.GetWindowMinimumSize(window, out var width, out var height)
            : SDL.GetWindowMaximumSize(window, out width, out height);
        return success ? new(width, height) : throw SDLFailure("read native window size limits");
    }

    private void ApplyWindowLimits(Vector2i minimum, Vector2i maximum, Vector2i content, Vector2i? resize = null)
    {
        EnsureOwner();
        if (minimum.X < 0 || minimum.Y < 0 || maximum.X < 0 || maximum.Y < 0 || LimitsConflict(minimum, maximum))
            throw new ArgumentOutOfRangeException(nameof(minimum), "Window limits must be nonnegative and ordered on each axis.");

        var window = _window.DangerousGetHandle();
        var nativeMinimum = _waylandWindowPosition ? WaylandLogicalWindowLimit(minimum, window, minimum: true) : minimum;
        var nativeMaximum = _waylandWindowPosition ? WaylandLogicalWindowLimit(maximum, window, minimum: false) : maximum;
        if (LimitsConflict(nativeMinimum, nativeMaximum))
            throw new ArgumentOutOfRangeException(nameof(minimum), "The limits cannot both be represented at the current pixel density.");
        var nativeContent = _waylandWindowPosition ? WaylandLogicalWindowLimit(content, window, minimum: true) : content;
        nativeMinimum = CapWindowMinimum(nativeMinimum.Max(nativeContent), nativeMaximum);

        var previousMinimum = ReadNativeWindowLimit(minimum: true);
        var previousMaximum = ReadNativeWindowLimit(minimum: false);
        if (previousMinimum == nativeMinimum && previousMaximum == nativeMaximum)
        {
            if (resize is { } size) WindowSetSizeCore(size);
        }
        else
        {
            if (!SDL.GetWindowSize(window, out var width, out var height))
                throw SDLFailure("read window size before changing limits");
            try
            {
                SetNativeWindowLimits(window, previousMinimum, previousMaximum, nativeMinimum, nativeMaximum);
                if (resize is { } size) WindowSetSizeCore(size);
            }
            catch (Exception error)
            {
                try
                {
                    SetNativeWindowLimits(window, ReadNativeWindowLimit(minimum: true), ReadNativeWindowLimit(minimum: false), previousMinimum, previousMaximum);
                    if (!SDL.SetWindowSize(window, width, height)) throw SDLFailure("restore window size after changing limits");
                }
                catch (Exception rollback) { throw new AggregateException("Window size limits and rollback both failed.", error, rollback); }
                throw;
            }
        }
        _requestedMinimumSize = minimum;
        _requestedMaximumSize = maximum;
        _contentMinimumSize = content;
    }

    private static void SetNativeWindowLimits(nint window, Vector2i oldMinimum, Vector2i oldMaximum, Vector2i minimum, Vector2i maximum)
    {
        // SDL checks each setter against the opposite limit and may resize while applying it.
        if (LimitsConflict(oldMinimum, maximum))
        {
            oldMinimum = oldMinimum.Min(minimum);
            if (!SDL.SetWindowMinimumSize(window, oldMinimum.X, oldMinimum.Y))
                throw SDLFailure("lower minimum before changing maximum window size");
        }
        if (oldMaximum != maximum && !SDL.SetWindowMaximumSize(window, maximum.X, maximum.Y))
            throw SDLFailure("set maximum window size");
        if (oldMinimum != minimum && !SDL.SetWindowMinimumSize(window, minimum.X, minimum.Y))
            throw SDLFailure("set minimum window size");
    }

    internal WindowMode WindowGetModeCore(int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) return ResolveWindow(windowId).WindowGetModeCore(MainWindowId);
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
        if (windowId != MainWindowId) { ResolveWindow(windowId).WindowSetModeCore(mode, MainWindowId); return; }
        EnsureOwner();
        if (IsNativePopup)
        {
            if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            if (mode != WindowMode.Windowed) throw new NotSupportedException("Native popup surfaces cannot change top-level presentation mode.");
            return;
        }
        if (_transientDisplay is not null && mode is WindowMode.Fullscreen or WindowMode.ExclusiveFullscreen)
            throw new InvalidOperationException("A transient window cannot enter fullscreen.");
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
        if (windowId != MainWindowId) return ResolveWindow(windowId).WindowIsFocusedCore(MainWindowId);
        EnsureOwner();
        var window = GetWindow(windowId);
        if (IsNativePopup) return SDL.GetKeyboardFocus() == window;
        return _waylandWindowPosition
            ? SDL.GetMouseFocus() == window
            : (SDL.GetWindowFlags(window) & SDL.WindowFlags.InputFocus) != 0;
    }

    internal bool WindowGetFlagCore(WindowFlag flag, int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) return ResolveWindow(windowId).WindowGetFlagCore(flag, MainWindowId);
        EnsureOwner();
        var window = GetWindow(windowId);
        if (flag == WindowFlag.MousePassthrough) return _mousePassthrough;
        if (flag == WindowFlag.SharpCorners) return GetSharpCorners();
        if (flag == WindowFlag.ExcludeFromCapture) return GetCaptureExclusion();
        if (SDL.GetCurrentVideoDriver() == "wayland" && (flag == WindowFlag.AlwaysOnTop || flag == WindowFlag.NoFocus && !IsNativePopup))
            throw new NotSupportedException($"Window flag {flag} is unavailable for a Wayland top-level window.");
        var native = SDL.GetWindowFlags(window);
        return flag switch
        {
            WindowFlag.Borderless => IsNativePopup || (native & SDL.WindowFlags.Borderless) != 0,
            WindowFlag.ResizeDisabled => (native & SDL.WindowFlags.Resizable) == 0,
            WindowFlag.AlwaysOnTop => (native & SDL.WindowFlags.AlwaysOnTop) != 0,
            WindowFlag.NoFocus => (native & SDL.WindowFlags.NotFocusable) != 0,
            WindowFlag.Popup when IsNativePopup => PopupInputPolicy,
            WindowFlag.PopupWmHint when IsNativePopup => true,
            WindowFlag.MinimizeDisabled or WindowFlag.MaximizeDisabled when IsNativePopup => true,
            WindowFlag.Transparent => (native & SDL.WindowFlags.Transparent) != 0,
            >= WindowFlag.Transparent and < WindowFlag.Max => throw new NotSupportedException($"Window flag {flag} requires a display capability that is not integrated."),
            _ => throw new ArgumentOutOfRangeException(nameof(flag), flag, "Unknown window flag."),
        };
    }

    internal void WindowSetFlagCore(WindowFlag flag, bool enabled, int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) { ResolveWindow(windowId).WindowSetFlagCore(flag, enabled, MainWindowId); return; }
        EnsureOwner();
        var window = GetWindow(windowId);
        if (flag == WindowFlag.MousePassthrough) { SetMousePassthrough(enabled); return; }
        if (flag == WindowFlag.SharpCorners) { SetSharpCorners(enabled); return; }
        if (flag == WindowFlag.ExcludeFromCapture) { SetCaptureExclusion(enabled); return; }
        if (IsNativePopup && flag is WindowFlag.PopupWmHint or WindowFlag.MinimizeDisabled or WindowFlag.MaximizeDisabled or WindowFlag.Borderless or WindowFlag.ResizeDisabled)
        {
            if (!enabled) throw new NotSupportedException("A native popup cannot enable top-level decoration or presentation policies.");
            return;
        }
        if (IsNativePopup && flag == WindowFlag.Popup) { SetPopupInputPolicy(enabled); return; }
        if (flag == WindowFlag.Transparent)
        {
            if (WindowGetFlagCore(flag) != enabled) throw new NotSupportedException("Configure native transparency before creating the window.");
            return;
        }
        if (SDL.GetCurrentVideoDriver() == "wayland" && (flag == WindowFlag.AlwaysOnTop || flag == WindowFlag.NoFocus && !IsNativePopup))
            throw new NotSupportedException($"Window flag {flag} is unavailable for a Wayland top-level window.");
        var previousBorderless = flag == WindowFlag.Borderless && (SDL.GetWindowFlags(window) & SDL.WindowFlags.Borderless) != 0;
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
        if (flag == WindowFlag.Borderless)
        {
            try { RefreshMouseInputRegion(); }
            catch (Exception error)
            {
                try
                {
                    if (!SDL.SetWindowBordered(window, !previousBorderless)) throw SDLFailure("restore window borders");
                    RefreshMouseInputRegion();
                }
                catch (Exception rollback) { throw new AggregateException(error, rollback); }
                throw;
            }
        }
    }

    internal bool WindowIsMaximizeAllowedCore(int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) return ResolveWindow(windowId).WindowIsMaximizeAllowedCore(MainWindowId);
        EnsureOwner();
        var flags = SDL.GetWindowFlags(GetWindow(windowId));
        return (flags & SDL.WindowFlags.Resizable) != 0;
    }

    private void EnableForStealingFocusCore(int pid)
    {
        EnsureOwner();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pid);
        if (!OperatingSystem.IsWindows() || _videoDriver != "windows")
            throw new NotSupportedException("Foreground permission requires a native Windows desktop.");

        if (AllowSetForegroundWindow((uint)pid) == 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows rejected the foreground permission grant.");
    }

    [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int AllowSetForegroundWindow(uint pid);

    internal void WindowMoveToForegroundCore(int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) { ResolveWindow(windowId).WindowMoveToForegroundCore(MainWindowId); return; }
        EnsureOwner();
        var window = GetWindow(windowId);
        if (IsNativePopup) return;
        if (SDL.GetCurrentVideoDriver() == "wayland")
            return;
        if (!SDL.RaiseWindow(window))
            throw SDLFailure("raise the window");
    }

    internal void WindowRequestAttentionCore(int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) { ResolveWindow(windowId).WindowRequestAttentionCore(MainWindowId); return; }
        EnsureOwner();
        if (!SDL.FlashWindow(GetWindow(windowId), SDL.FlashOperation.UntilFocused))
            throw SDLFailure("request window attention");
    }

    internal Vector2i WindowGetPositionWithDecorationsCore(int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) return ResolveWindow(windowId).WindowGetPositionWithDecorationsCore(MainWindowId);
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
        if (windowId != MainWindowId) return ResolveWindow(windowId).WindowGetSizeWithDecorationsCore(MainWindowId);
        EnsureOwner();
        var window = GetWindow(windowId);
        if (_waylandWindowPosition)
            return WindowGetSizeCore(windowId);
        if (!SDL.GetWindowSize(window, out var width, out var height) ||
            !SDL.GetWindowBordersSize(window, out var top, out var left, out var bottom, out var right))
            throw SDLFailure("read decorated window size");
        return new Vector2i(checked(width + left + right), checked(height + top + bottom));
    }

    internal void WindowSetTaskbarProgressStateCore(ProgressState state, int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) { ResolveWindow(windowId).WindowSetTaskbarProgressStateCore(state, MainWindowId); return; }
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
        if (windowId != MainWindowId) { ResolveWindow(windowId).WindowSetTaskbarProgressValueCore(value, MainWindowId); return; }
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
