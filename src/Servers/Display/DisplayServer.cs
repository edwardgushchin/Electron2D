using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

/// <summary>Owns the process's native display connection and main window.</summary>
/// <remarks>
/// Open one server on the SDL main thread. Window and display calls are confined to the opening thread; dispose the
/// server on that thread after the host stops its main loop. SDL window changes can be asynchronous under a window
/// manager, so getters report observed state rather than the last requested value.
/// </remarks>
public sealed partial class DisplayServer : ElectronObject
{
    /// <summary>Identifies the main window.</summary>
    public const int MainWindowId = 0;
    /// <summary>Identifies a window that does not exist.</summary>
    public const int InvalidWindowId = -1;
    /// <summary>Identifies a display that does not exist.</summary>
    public const int InvalidScreen = -1;
    /// <summary>Selects the display containing the mouse pointer; on Wayland this is index zero.</summary>
    public const int ScreenWithMouseFocus = -4;
    /// <summary>Selects the display containing the keyboard-focused window.</summary>
    public const int ScreenWithKeyboardFocus = -3;
    /// <summary>Selects the primary display.</summary>
    public const int ScreenPrimary = -2;
    /// <summary>Selects the display containing the main window.</summary>
    public const int ScreenOfMainWindow = -1;

    private static readonly object InstanceGate = new();
    private static DisplayServer? _instance;

    [LibraryImport("libc", EntryPoint = "setenv", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int SetNativeEnvironmentVariable(string name, string value, int overwrite);

    private readonly int _ownerThreadId;
    private readonly SdlWindowHandle _window;
    private readonly uint _sdlWindowId;
    private readonly bool _waylandWindowPosition;
    private readonly bool _linuxPortalThemeDriver;
    private readonly bool _linuxPortalThemeSupported;
    private readonly nint _gtkScreen;
    private readonly nint _gtkTitlebarProvider;
    private RectI _windowRect;
    private Vector2I _waylandMinimumSize = new(64, 64);
    private Vector2I _waylandMaximumSize;

    private DisplayServer(nint window, nint gtkScreen, nint gtkTitlebarProvider)
    {
        _gtkScreen = gtkScreen;
        _gtkTitlebarProvider = gtkTitlebarProvider;
        _ownerThreadId = Environment.CurrentManagedThreadId;
        _sdlWindowId = SDL.GetWindowID(window);
        if (_sdlWindowId == 0)
            throw SdlFailure("identify the main window");
        var videoDriver = SDL.GetCurrentVideoDriver();
        _waylandWindowPosition = videoDriver == "wayland";
        _linuxPortalThemeDriver = videoDriver is "wayland" or "x11";
        _linuxPortalThemeSupported = _linuxPortalThemeDriver && LinuxPortalThemeSupport.Query();
        int width;
        int height;
        var sizeRead = _waylandWindowPosition
            ? SDL.GetWindowSizeInPixels(window, out width, out height)
            : SDL.GetWindowSize(window, out width, out height);
        if (!sizeRead)
            throw SdlFailure("read the main window's initial size");
        var position = Vector2I.Zero;
        if (!_waylandWindowPosition)
        {
            if (!SDL.GetWindowPosition(window, out var x, out var y))
                throw SdlFailure("read the main window's initial position");
            position = new Vector2I(x, y);
        }
        _windowRect = new RectI(position, width, height);
        _window = new SdlWindowHandle(window);
        GC.SuppressFinalize(_window);
    }

    /// <summary>Gets the active display server, if one has been opened.</summary>
    /// <value>The active instance or <see langword="null"/> after disposal.</value>
    public static DisplayServer? Instance
    {
        get { lock (InstanceGate) return _instance; }
    }

    /// <summary>Opens the native video subsystem and creates the main window.</summary>
    /// <param name="title">Initial UTF-8 window title.</param>
    /// <param name="size">Positive initial dimensions in native window coordinates, which are logical on Wayland.</param>
    /// <param name="hidden">Whether the window starts hidden.</param>
    /// <returns>The process's active display server.</returns>
    /// <remarks>The caller owns and must dispose the returned server on the opening thread. The main window starts with a 64-by-64 minimum in client pixels on Wayland and native window coordinates elsewhere, so smaller requested dimensions may be constrained by the native window manager. A visible Wayland window presents a blank surface so the compositor can show it before rendering is available; size and scale events refresh that surface. If a Wayland session inherits an X11-only GTK backend setting, this method selects the matching GTK backend before initializing video. An available GTK decoration plugin keeps its desktop theme while filling the border below its title bar.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="title"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> has a nonpositive component.</exception>
    /// <exception cref="InvalidOperationException">Another server is active, the call is off SDL's main thread, or SDL fails to open video or create the window.</exception>
    public static DisplayServer Open(string title, Vector2I size, bool hidden = false)
    {
        ArgumentNullException.ThrowIfNull(title);
        if (size.X <= 0 || size.Y <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Both window dimensions must be positive.");

        lock (InstanceGate)
        {
            if (_instance is not null)
                throw new InvalidOperationException("A display server is already open.");
            var previousGdkBackend = Environment.GetEnvironmentVariable("GDK_BACKEND");
            var videoDriver = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER");
            var correctedGdkBackend = OperatingSystem.IsLinux() && previousGdkBackend == "x11" &&
                (videoDriver == "wayland" || videoDriver is null &&
                    Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland" &&
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")));
            if (correctedGdkBackend)
                SetGdkBackend("wayland");
            try
            {
                if (!SDL.IsMainThread())
                    throw new InvalidOperationException("The display server must be opened on SDL's main thread.");
                if (!SDL.InitSubSystem(SDL.InitFlags.Video))
                    throw SdlFailure("initialize the video subsystem");
            }
            catch
            {
                if (correctedGdkBackend)
                    SetGdkBackend(previousGdkBackend!);
                throw;
            }

            (nint GtkScreen, nint GtkProvider) gtkStyle = default;
            try
            {
                if (correctedGdkBackend && SDL.GetCurrentVideoDriver() != "wayland")
                {
                    SetGdkBackend(previousGdkBackend!);
                    correctedGdkBackend = false;
                }
                if (SDL.GetCurrentVideoDriver() == "wayland")
                    gtkStyle = InstallGtkTitlebarStyle();
                var flags = SDL.WindowFlags.Resizable | SDL.WindowFlags.HighPixelDensity;
                if (hidden)
                    flags |= SDL.WindowFlags.Hidden;
                var window = SDL.CreateWindow(title, size.X, size.Y, flags);
                if (window == 0)
                    throw SdlFailure("create the main window");

                try
                {
                    var minimumSize = SDL.GetCurrentVideoDriver() == "wayland"
                        ? WaylandLogicalWindowLimit(new Vector2I(64, 64), window, minimum: true)
                        : new Vector2I(64, 64);
                    if (!SDL.SetWindowMinimumSize(window, minimumSize.X, minimumSize.Y))
                        throw SdlFailure("set the main window's minimum size");
                    if (!hidden && SDL.GetCurrentVideoDriver() == "wayland")
                        PresentBlankWindowSurface(window);
                    _instance = new DisplayServer(window, gtkStyle.GtkScreen, gtkStyle.GtkProvider);
                    Input.Instance.SetNativeFlush(_instance.FlushBufferedInput);
                    return _instance;
                }
                catch
                {
                    SDL.DestroyWindow(window);
                    throw;
                }
            }
            catch
            {
                RemoveGtkTitlebarStyle(gtkStyle.GtkScreen, gtkStyle.GtkProvider);
                if (correctedGdkBackend)
                    SetGdkBackend(previousGdkBackend!);
                SDL.QuitSubSystem(SDL.InitFlags.Video);
                throw;
            }
        }
    }

    private static void SetGdkBackend(string value)
    {
        if (SetNativeEnvironmentVariable("GDK_BACKEND", value, 1) != 0)
            throw new InvalidOperationException("Failed to select the GTK display backend.");
        Environment.SetEnvironmentVariable("GDK_BACKEND", value);
    }

    private static void PresentBlankWindowSurface(nint window)
    {
        var surface = SDL.GetWindowSurface(window);
        if (surface == 0)
            throw SdlFailure("create the main window's initial surface");
        if (!SDL.FillSurfaceRect(surface, 0, SDL.MapSurfaceRGB(surface, 32, 32, 32)) ||
            !SDL.UpdateWindowSurface(window))
            throw SdlFailure("present the main window's initial surface");
    }

    private void RefreshBlankWindowSurface()
    {
        var window = _window.DangerousGetHandle();
        if (_waylandWindowPosition && SDL.WindowHasSurface(window))
            PresentBlankWindowSurface(window);
    }

    /// <summary>Gets the display backend name.</summary>
    /// <returns>The active backend's public name, such as <c>Wayland</c> or <c>X11</c>.</returns>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the video driver is unavailable.</exception>
    public string GetName()
    {
        EnsureOwner();
        return (SDL.GetCurrentVideoDriver() ?? throw SdlFailure("read the video driver name")) switch
        {
            "wayland" => "Wayland",
            "x11" => "X11",
            "windows" => "Windows",
            "cocoa" => "macOS",
            "android" => "Android",
            "uikit" => "iOS",
            "emscripten" => "web",
            "dummy" => "headless",
            var driver => driver,
        };
    }

    /// <summary>Reports whether the operating system currently requests a dark appearance.</summary>
    /// <returns><see langword="true"/> only when the native system theme is dark.</returns>
    /// <remarks>On Linux Wayland and X11, the system Settings portal must be available. An unknown or unset preference returns <see langword="false"/> without classifying the theme as light. Query on the opening thread after processing native events to observe changes.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    public bool IsDarkMode()
    {
        EnsureOwner();
        return (!_linuxPortalThemeDriver || _linuxPortalThemeSupported) &&
               SDL.GetSystemTheme() == SDL.SystemTheme.Dark;
    }

    /// <summary>Reports whether the system can provide a light or dark appearance preference.</summary>
    /// <returns><see langword="true"/> on Linux Wayland or X11 when the desktop Settings portal is available, even if no appearance is preferred; on other drivers, when the native theme is known.</returns>
    /// <remarks>The Linux check queries the Settings portal when the server opens and requires interface version one or newer. A missing portal or native library returns <see langword="false"/>. On other drivers, an unknown theme returns <see langword="false"/>.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    public bool IsDarkModeSupported()
    {
        EnsureOwner();
        return _linuxPortalThemeDriver
            ? _linuxPortalThemeSupported
            : SDL.GetSystemTheme() != SDL.SystemTheme.Unknown;
    }

    /// <summary>Gets the current number of connected displays.</summary>
    /// <returns>The display count, which may change after hotplug events.</returns>
    public int GetScreenCount()
    {
        EnsureOwner();
        return GetDisplays().Length;
    }

    /// <summary>Reports whether hardware keyboard input is available.</summary>
    /// <returns>On Android and iOS, whether SDL detects a connected keyboard; on other targets, <see langword="true"/>.</returns>
    /// <remarks>The mobile result can change when devices are attached or removed. Query it on the opening thread.</remarks>
    public bool HasHardwareKeyboard()
    {
        EnsureOwner();
        return !OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS() || SDL.HasKeyboard();
    }

    /// <summary>Gets whether the display is being kept awake by this process.</summary>
    /// <returns><see langword="true"/> while native screen blanking is disabled.</returns>
    /// <remarks>This reports SDL's process-wide screensaver setting, not an operating-system promise that the display stays on.</remarks>
    public bool ScreenIsKeptOn()
    {
        EnsureOwner();
        return !SDL.ScreenSaverEnabled();
    }

    /// <summary>Requests that the display remain awake or resume normal power-saving behavior.</summary>
    /// <param name="enable">Whether to inhibit native screen blanking.</param>
    /// <remarks>The request applies to this process and is automatically released when SDL video quits.</remarks>
    /// <exception cref="InvalidOperationException">The native video backend rejects the request.</exception>
    /// <exception cref="AggregateException">The native request fails and its previous state cannot be restored.</exception>
    public void ScreenSetKeepOn(bool enable)
    {
        EnsureOwner();
        var wasEnabled = SDL.ScreenSaverEnabled();
        if (wasEnabled == !enable)
            return;
        if (enable ? SDL.DisableScreenSaver() : SDL.EnableScreenSaver())
            return;
        var failure = SdlFailure("change screen blanking policy");
        _ = wasEnabled ? SDL.EnableScreenSaver() : SDL.DisableScreenSaver();
        if (SDL.ScreenSaverEnabled() != wasEnabled)
            throw new AggregateException(failure, SdlFailure("restore screen blanking policy"));
        throw failure;
    }

    /// <summary>Gets the index of the current primary display.</summary>
    /// <returns>The zero-based index or <see cref="InvalidScreen"/> when no display matches.</returns>
    /// <remarks>Wayland does not expose a primary display and uses index zero.</remarks>
    public int GetPrimaryScreen()
    {
        EnsureOwner();
        if (SDL.GetCurrentVideoDriver() == "wayland")
            return 0;
        var primary = SDL.GetPrimaryDisplay();
        var displays = GetDisplays();
        return Array.IndexOf(displays, primary);
    }

    /// <summary>Gets the global desktop position of a display.</summary>
    /// <param name="screen">Display index or one of the negative display selectors; defaults to the main window's display.</param>
    /// <returns>The upper-left position in platform-native desktop coordinates, or zero if the display is invalid.</returns>
    public Vector2I ScreenGetPosition(int screen = ScreenOfMainWindow)
    {
        EnsureOwner();
        if (!TryGetDisplayId(screen, out var displayId) || !SDL.GetDisplayBounds(displayId, out var bounds))
            return Vector2I.Zero;
        return new Vector2I(bounds.X, bounds.Y);
    }

    /// <summary>Gets the full size of a display.</summary>
    /// <param name="screen">Display index or one of the negative display selectors; defaults to the main window's display.</param>
    /// <returns>The display size in pixels on Wayland or platform-native desktop units elsewhere; zero if the display is invalid.</returns>
    public Vector2I ScreenGetSize(int screen = ScreenOfMainWindow)
    {
        EnsureOwner();
        if (!TryGetDisplayId(screen, out var displayId) || !SDL.GetDisplayBounds(displayId, out var bounds))
            return Vector2I.Zero;
        return SDL.GetCurrentVideoDriver() == "wayland"
            ? WaylandPhysicalScreenSize(displayId, bounds)
            : new Vector2I(bounds.W, bounds.H);
    }

    /// <summary>Gets the usable desktop rectangle of a display.</summary>
    /// <param name="screen">Display index or one of the negative display selectors; defaults to the main window's display.</param>
    /// <returns>The work area after platform-reserved bars are excluded; empty if the display is invalid. On Wayland, the full display position and physical pixel size are returned.</returns>
    public RectI ScreenGetUsableRect(int screen = ScreenOfMainWindow)
    {
        EnsureOwner();
        if (!TryGetDisplayId(screen, out var displayId))
            return default;
        var found = SDL.GetCurrentVideoDriver() == "wayland"
            ? SDL.GetDisplayBounds(displayId, out var bounds)
            : SDL.GetDisplayUsableBounds(displayId, out bounds);
        if (!found)
            return default;
        var size = SDL.GetCurrentVideoDriver() == "wayland"
            ? WaylandPhysicalScreenSize(displayId, bounds)
            : new Vector2I(bounds.W, bounds.H);
        return new RectI(new Vector2I(bounds.X, bounds.Y), size);
    }

    private static Vector2I WaylandPhysicalScreenSize(uint displayId, SDL.Rect bounds)
    {
        var density = SDL.GetCurrentDisplayMode(displayId)?.PixelDensity ?? 0f;
        if (!float.IsFinite(density) || density <= 0f)
            return new Vector2I(bounds.W, bounds.H);
        return new Vector2I(checked((int)Math.Round(bounds.W * (double)density)),
            checked((int)Math.Round(bounds.H * (double)density)));
    }

    /// <summary>Gets the content scale of a display or, on Wayland, the main window.</summary>
    /// <param name="screen">Display index or one of the negative display selectors; defaults to the main window's display.</param>
    /// <returns>A positive content scale, or one if the display is invalid. A Wayland display index reports an integer scale; the main-window selector can report a fractional scale.</returns>
    /// <remarks>On Wayland, indexed scales round the current display mode's pixel density upward. The main-window selector reads its current window scale. Before the compositor assigns a preferred fractional scale to a newly created or hidden window, that value can be provisional; it can change after the first buffer is presented or the window moves to another output. Call this method on the opening thread.</remarks>
    public float ScreenGetScale(int screen = ScreenOfMainWindow)
    {
        EnsureOwner();
        if (!TryGetDisplayId(screen, out var displayId))
            return 1f;
        var driver = SDL.GetCurrentVideoDriver();
        if (driver == "x11")
            return 1f;
        var scale = driver == "wayland"
            ? screen == ScreenOfMainWindow
                ? SDL.GetWindowDisplayScale(_window.DangerousGetHandle())
                : SDL.GetCurrentDisplayMode(displayId)?.PixelDensity ?? 0f
            : SDL.GetDisplayContentScale(displayId);
        if (!float.IsFinite(scale) || scale <= 0f)
            return 1f;
        return driver == "wayland" && screen != ScreenOfMainWindow ? MathF.Ceiling(scale) : scale;
    }

    /// <summary>Gets the title of the main window.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The title observed by SDL.</returns>
    internal string WindowGetTitle(int windowId = MainWindowId)
    {
        EnsureOwner();
        return SDL.GetWindowTitle(GetWindow(windowId));
    }

    /// <summary>Requests a new main-window title.</summary>
    /// <param name="title">New UTF-8 title.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <exception cref="ArgumentNullException"><paramref name="title"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> does not identify the main window.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the native title request fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server has been disposed.</exception>
    public void WindowSetTitle(string title, int windowId = MainWindowId)
    {
        EnsureOwner();
        ArgumentNullException.ThrowIfNull(title);
        if (!SDL.SetWindowTitle(GetWindow(windowId), title))
            throw SdlFailure("set window title");
    }

    /// <summary>Gets the main window's client size.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>Pixel dimensions on Wayland and platform-native window dimensions elsewhere.</returns>
    public Vector2I WindowGetSize(int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        int width;
        int height;
        var sizeRead = _waylandWindowPosition
            ? SDL.GetWindowSizeInPixels(window, out width, out height)
            : SDL.GetWindowSize(window, out width, out height);
        if (!sizeRead)
            throw SdlFailure("read window size");
        return new Vector2I(width, height);
    }

    /// <summary>Requests a new main-window client size.</summary>
    /// <param name="size">Requested pixel dimensions on Wayland or platform-native window dimensions elsewhere.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>Wayland clamps each requested component below one to one before applying native minimum-size constraints. Other video drivers require positive dimensions.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is not positive on a non-Wayland video driver.</exception>
    public void WindowSetSize(Vector2I size, int windowId = MainWindowId)
    {
        EnsureOwner();
        var requestedSize = SDL.GetCurrentVideoDriver() == "wayland"
            ? new Vector2I(Math.Max(1, size.X), Math.Max(1, size.Y))
            : size;
        if (requestedSize.X <= 0 || requestedSize.Y <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Both window dimensions must be positive.");
        var window = GetWindow(windowId);
        if (_waylandWindowPosition)
        {
            requestedSize = WaylandLogicalWindowSize(requestedSize, window);
            requestedSize = new Vector2I(Math.Max(1, requestedSize.X), Math.Max(1, requestedSize.Y));
        }
        if (!SDL.SetWindowSize(window, requestedSize.X, requestedSize.Y))
            throw SdlFailure("set window size");
    }

    private static Vector2I WaylandLogicalWindowSize(Vector2I pixelSize, nint window)
    {
        var density = WaylandWindowPixelDensity(window);
        return new Vector2I(
            checked((int)Math.Round(pixelSize.X / (double)density, MidpointRounding.AwayFromZero)),
            checked((int)Math.Round(pixelSize.Y / (double)density, MidpointRounding.AwayFromZero)));
    }

    private static Vector2I WaylandLogicalWindowLimit(Vector2I pixelSize, nint window, bool minimum)
    {
        var density = WaylandWindowPixelDensity(window);
        var logicalSize = new Vector2I(
            checked((int)(minimum ? Math.Ceiling(pixelSize.X / (double)density) : Math.Floor(pixelSize.X / (double)density))),
            checked((int)(minimum ? Math.Ceiling(pixelSize.Y / (double)density) : Math.Floor(pixelSize.Y / (double)density))));
        if (!minimum && ((pixelSize.X > 0 && logicalSize.X == 0) || (pixelSize.Y > 0 && logicalSize.Y == 0)))
            throw new ArgumentOutOfRangeException(nameof(pixelSize), pixelSize, "The maximum cannot be represented in window coordinates at the current pixel density.");
        return logicalSize;
    }

    private static float WaylandWindowPixelDensity(nint window)
    {
        var density = SDL.GetWindowPixelDensity(window);
        if (!float.IsFinite(density) || density <= 0f)
            throw SdlFailure("read window pixel density");
        return density;
    }

    /// <summary>Gets the main window's global desktop position.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The upper-left position in platform-native desktop coordinates.</returns>
    /// <exception cref="NotSupportedException">The active Wayland compositor does not expose a reliable global window position.</exception>
    public Vector2I WindowGetPosition(int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        EnsureGlobalWindowCoordinatesAvailable();
        if (!SDL.GetWindowPosition(window, out var x, out var y))
            throw SdlFailure("read window position");
        return new Vector2I(x, y);
    }

    /// <summary>Requests a global desktop position for the main window.</summary>
    /// <param name="position">Upper-left position in platform-native desktop coordinates.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <exception cref="NotSupportedException">Wayland does not allow this top-level window to choose its desktop position.</exception>
    public void WindowSetPosition(Vector2I position, int windowId = MainWindowId)
    {
        EnsureOwner();
        var window = GetWindow(windowId);
        EnsureGlobalWindowCoordinatesAvailable();
        if (!SDL.SetWindowPosition(window, position.X, position.Y))
            throw SdlFailure("set window position");
    }

    private static void EnsureGlobalWindowCoordinatesAvailable()
    {
        if (SDL.GetCurrentVideoDriver() == "wayland")
            throw new NotSupportedException("Wayland does not expose global top-level window coordinates.");
    }

    /// <summary>Gets whether the system clipboard currently contains nonempty text.</summary>
    /// <returns><see langword="true"/> when <see cref="ClipboardGet"/> returns nonempty text.</returns>
    /// <remarks>Reads the clipboard on the opening thread. An unavailable text selection returns <see langword="false"/>.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    public bool ClipboardHas()
    {
        return ClipboardGet().Length != 0;
    }

    /// <summary>Gets text from the system clipboard.</summary>
    /// <returns>The current text or an empty string when no text is available.</returns>
    /// <remarks>Reads the system clipboard on the opening thread. Clipboard and primary-selection text are separate. On Wayland, the compositor sends a clipboard offer to a client with keyboard focus; a hidden or unfocused client may receive no text.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    public string ClipboardGet()
    {
        EnsureOwner();
        return SDL.GetClipboardText();
    }

    /// <summary>Replaces system clipboard text.</summary>
    /// <param name="text">Text to place on the clipboard.</param>
    /// <remarks>Requests replacement of the system clipboard on the opening thread. This does not change the primary selection. On Wayland, the compositor may require a recent input event before it accepts clipboard ownership; a successful native call alone does not prove another process can read the new text.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread or the native backend rejects the replacement.</exception>
    public void ClipboardSet(string text)
    {
        EnsureOwner();
        ArgumentNullException.ThrowIfNull(text);
        if (!SDL.SetClipboardText(text))
            throw SdlFailure("set clipboard text");
    }

    /// <summary>Gets text from the platform's primary selection.</summary>
    /// <returns>The selected text, or an empty string when the backend has no primary selection.</returns>
    /// <remarks>Primary selection is separate from the system clipboard and is available on Linux/Wayland. Read it on the opening thread. Wayland sends the selection offer only to a client with keyboard focus; a hidden or unfocused client may receive no text. Platforms without it may report an empty value.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    public string ClipboardGetPrimary()
    {
        EnsureOwner();
        return SDL.GetPrimarySelectionText();
    }

    /// <summary>Replaces text in the platform's primary selection.</summary>
    /// <param name="text">The selection text.</param>
    /// <remarks>Requests replacement of the separate primary selection on the opening thread. The system clipboard is unchanged. On Wayland, the compositor may require a recent input event before it accepts selection ownership; a successful native call alone does not prove another process can read the new text.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread or the native backend rejects the selection.</exception>
    public void ClipboardSetPrimary(string text)
    {
        EnsureOwner();
        ArgumentNullException.ThrowIfNull(text);
        if (!SDL.SetPrimarySelectionText(text))
            throw SdlFailure("set primary selection text");
    }

    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        EnsureOwner();
        if (_processingEvents)
            throw new InvalidOperationException("The display server cannot be disposed during event delivery.");
        ValidateDialogDisposal();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;
        lock (InstanceGate)
        {
            _pendingDroppedFiles = null;
            DisposeDialogs();
            ReleasePointer();
            Input.Instance.SetNativeFlush(null);
            Input.Instance.ReleasePressedEvents();
            _window.Dispose();
            RemoveGtkTitlebarStyle(_gtkScreen, _gtkTitlebarProvider);
            SDL.QuitSubSystem(SDL.InitFlags.Video);
            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
        base.Dispose(disposing);
    }

    internal void SetWindowVisible(bool visible)
    {
        EnsureOwner();
        var window = GetWindow(MainWindowId);
        if (!(visible ? SDL.ShowWindow(window) : SDL.HideWindow(window)))
            throw SdlFailure("change window visibility");
        if (visible && _waylandWindowPosition)
            PresentBlankWindowSurface(window);
    }

    private void EnsureOwner()
    {
        ThrowIfDisposed();
        if (Environment.CurrentManagedThreadId != _ownerThreadId)
            throw new InvalidOperationException("DisplayServer calls must run on the opening thread.");
    }

    private nint GetWindow(int windowId)
    {
        if (windowId != MainWindowId)
            throw new ArgumentOutOfRangeException(nameof(windowId), windowId, "Only the main window exists.");
        return _window.DangerousGetHandle();
    }

    private uint[] GetDisplays()
    {
        var displays = SDL.GetDisplays(out _);
        return displays ?? throw SdlFailure("enumerate displays");
    }

    private bool TryGetDisplayId(int screen, out uint displayId)
    {
        var displays = GetDisplays();
        var index = screen switch
        {
            ScreenOfMainWindow => WindowGetCurrentScreen(),
            ScreenPrimary => GetPrimaryScreen(),
            ScreenWithKeyboardFocus => GetKeyboardFocusScreen(),
            ScreenWithMouseFocus => GetMouseFocusScreen(displays),
            _ => screen,
        };
        if ((uint)index >= (uint)displays.Length)
        {
            displayId = 0;
            return false;
        }
        displayId = displays[index];
        return true;
    }

    private uint GetDisplayId(int screen) => TryGetDisplayId(screen, out var displayId)
        ? displayId : throw new ArgumentOutOfRangeException(nameof(screen), screen, "No display has this index.");

    private int GetMouseFocusScreen(uint[] displays)
    {
        if (SDL.GetCurrentVideoDriver() == "wayland")
            return 0;
        var position = MouseGetPosition();
        var point = new SDL.Point { X = position.X, Y = position.Y };
        var index = Array.IndexOf(displays, SDL.GetDisplayForPoint(in point));
        return index >= 0 ? index : GetPrimaryScreen();
    }

    private static InvalidOperationException SdlFailure(string action) =>
        new($"Unable to {action}: {SDL.GetError()}");

    private sealed class SdlWindowHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SdlWindowHandle(nint handle) : base(ownsHandle: true) => SetHandle(handle);

        protected override bool ReleaseHandle()
        {
            SDL.DestroyWindow(handle);
            return true;
        }
    }
}
