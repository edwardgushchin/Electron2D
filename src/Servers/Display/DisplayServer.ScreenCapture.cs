using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private X11ScreenCapture? _x11ScreenCapture;
    private bool _screenCaptureChecked;
    private bool UsesWindowsScreenCapture => _videoDriver == "windows" && OperatingSystem.IsWindows();
    private bool HasScreenCapture => UsesWindowsScreenCapture || GetX11ScreenCapture() is not null;

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
        return display.CaptureScreenRect(new Rect2i(bounds.X, bounds.Y, bounds.W, bounds.H));
    }

    /// <summary>Captures a rectangle from the native desktop in screen coordinates.</summary>
    /// <param name="rectangle">A positive rectangle fully contained within the desktop, in pixels.</param>
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
    /// <param name="position">A desktop pixel in screen coordinates.</param>
    /// <exception cref="ArgumentOutOfRangeException">The pixel lies outside the desktop.</exception>
    /// <exception cref="InvalidOperationException">The display is inactive or the caller is off its owner thread.</exception>
    /// <exception cref="NotSupportedException">Desktop capture is unavailable on the current display.</exception>
    /// <exception cref="IOException">The native display failed to return the pixel.</exception>
    public static Color ScreenGetPixel(Vector2i position)
    {
        var display = RequireService();
        display.RequireScreenCapture();
        return display.UsesWindowsScreenCapture ? WindowsScreenCapture.Pixel(position) : display._x11ScreenCapture!.Pixel(position);
    }

    private Image CaptureScreenRect(Rect2i rectangle) => UsesWindowsScreenCapture
        ? WindowsScreenCapture.Capture(rectangle)
        : _x11ScreenCapture!.Capture(rectangle);

    private void RequireScreenCapture()
    {
        EnsureOwner();
        if (!HasScreenCapture)
            throw new NotSupportedException("Desktop capture requires Windows or a native X11 display with a single TrueColor root.");
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
