using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private X11ScreenCapture? _x11ScreenCapture;
    private bool _screenCaptureChecked;
    internal bool UsesWindowsScreenCapture => _videoDriver == "windows" && OperatingSystem.IsWindows();
    private bool HasScreenCapture => CanSampleDesktopColor || _waylandInput?.CanCaptureOutput == true;
    internal bool CanSampleDesktopColor => UsesWindowsScreenCapture || GetX11ScreenCapture() is not null;
    internal bool CanMagnifyDesktopColor => UsesWindowsScreenCapture && HasCaptureExclusion();

    internal IDesktopColorSampler OpenDesktopColorSampler()
    {
        EnsureOwner();
        if (UsesWindowsScreenCapture) return WindowsColorSampler.Open();
        return GetX11ScreenCapture()?.OpenColorSampler() ?? throw new NotSupportedException("Live desktop color sampling requires Windows or native X11.");
    }

    internal Rect2i DesktopColorSampleBounds(Vector2i point)
    {
        EnsureOwner();
        var at = new SDL.Point { X = point.X, Y = point.Y };
        var id = SDL.GetDisplayForPoint(in at);
        if (id == 0 || !SDL.GetDisplayBounds(id, out var bounds))
            throw new IOException("The screen under the pointer is unavailable.");
        return new(bounds.X, bounds.Y, bounds.W, bounds.H);
    }

    /// <summary>Captures an entire screen from the native desktop.</summary>
    /// <param name="screen">A zero-based screen index or a screen selector.</param>
    /// <returns>An independent opaque RGBA8 image owned by the caller.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The screen selector does not resolve to an active screen or the dimensions exceed the image limits.</exception>
    /// <exception cref="InvalidOperationException">The display is inactive or the caller is off its owner thread.</exception>
    /// <exception cref="NotSupportedException">Desktop capture is unavailable on the current display.</exception>
    /// <exception cref="IOException">The native display failed to return pixels.</exception>
    public static Image ScreenGetImage(int screen = ScreenOfMainWindow)
    {
        var display = RequireService();
        display.RequireScreenCapture();
        if (!display.TryGetDisplayID(screen, out var id))
            throw new ArgumentOutOfRangeException(nameof(screen));
        if (!SDL.GetDisplayBounds(id, out var bounds))
            throw SDLFailure("query the screen bounds");
        return display._waylandInput is { } wayland
            ? CaptureWaylandScreen(wayland, GetWaylandCaptureOutput(id, bounds))
            : display.CaptureScreenRect(new Rect2i(bounds.X, bounds.Y, bounds.W, bounds.H));
    }

    /// <summary>Captures a rectangle from the native desktop in screen coordinates.</summary>
    /// <remarks>On Wayland, gaps between screens are black and the first screen index wins where screen rectangles overlap.</remarks>
    /// <param name="rectangle">A positive rectangle inside the desktop bounds. On Wayland, use the published screen positions and pixel sizes.</param>
    /// <returns>An independent opaque RGBA8 image owned by the caller.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The rectangle lies outside the desktop or exceeds the image limits.</exception>
    /// <exception cref="InvalidOperationException">The display is inactive or the caller is off its owner thread.</exception>
    /// <exception cref="NotSupportedException">Desktop capture is unavailable on the current display.</exception>
    /// <exception cref="IOException">The native display failed to return pixels.</exception>
    public static Image ScreenGetImageRect(Rect2i rectangle)
    {
        var display = RequireService();
        display.RequireScreenCapture();
        return display.CaptureScreenRect(rectangle);
    }

    /// <summary>Reads one opaque color from the native desktop.</summary>
    /// <remarks>On Wayland, gaps between screens are black and the first screen index wins where screen rectangles overlap.</remarks>
    /// <param name="position">A desktop pixel in screen coordinates. On Wayland, use the published screen positions and pixel sizes.</param>
    /// <exception cref="ArgumentOutOfRangeException">The pixel lies outside the desktop.</exception>
    /// <exception cref="InvalidOperationException">The display is inactive or the caller is off its owner thread.</exception>
    /// <exception cref="NotSupportedException">Desktop capture is unavailable on the current display.</exception>
    /// <exception cref="IOException">The native display failed to return the pixel.</exception>
    public static Color ScreenGetPixel(Vector2i position)
    {
        var display = RequireService();
        display.RequireScreenCapture();
        if (display._waylandInput is { } wayland) return display.CaptureWaylandPixel(wayland, position);
        return display.UsesWindowsScreenCapture ? WindowsScreenCapture.Pixel(position) : display._x11ScreenCapture!.Pixel(position);
    }

    private Image CaptureScreenRect(Rect2i rectangle)
    {
        if (_waylandInput is { } wayland) return CaptureWaylandRectangle(wayland, rectangle);
        return UsesWindowsScreenCapture ? WindowsScreenCapture.Capture(rectangle) : _x11ScreenCapture!.Capture(rectangle);
    }

    private void RequireScreenCapture()
    {
        EnsureOwner();
        if (!HasScreenCapture)
            throw new NotSupportedException("Desktop capture requires Windows, a native X11 display with a single TrueColor root, or a Wayland compositor that exposes output image capture.");
    }

    private X11ScreenCapture? GetX11ScreenCapture()
    {
        if (_videoDriver != "x11" || _screenCaptureChecked)
            return _x11ScreenCapture;

        // Older XWayland servers do not advertise the XWAYLAND extension.
        foreach (var output in GetDisplays())
        {
            if (SDL.GetDisplayName(output)?.StartsWith("XWAYLAND", StringComparison.OrdinalIgnoreCase) == true)
            {
                _screenCaptureChecked = true;
                return null;
            }
        }

        var properties = SDL.GetWindowProperties(_window.DangerousGetHandle());
        var display = SDL.GetPointerProperty(properties, SDL.Props.WindowX11DisplayPointer, 0);
        if (display != 0)
            _x11ScreenCapture = X11ScreenCapture.Open(display);
        _screenCaptureChecked = true;
        return _x11ScreenCapture;
    }
}
