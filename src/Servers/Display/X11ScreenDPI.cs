using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal static unsafe partial class X11ScreenDPI
{
    private const string Xlib = "libX11.so.6", XRandR = "libXrandr.so.2";

    internal static int Read(nint display, SDL.Rect bounds, bool singleDisplay)
    {
        // Independent X screens can expose the same bounds with unrelated physical sizes.
        if (XScreenCount(display) != 1)
            return 0;

        var dpi = ReadMonitor(display, bounds);
        if (dpi > 0)
            return dpi;

        var screen = XDefaultScreen(display);
        if (!singleDisplay || bounds.X != 0 || bounds.Y != 0 ||
            bounds.W != XDisplayWidth(display, screen) || bounds.H != XDisplayHeight(display, screen))
            return 0;

        return Density(bounds.W, bounds.H, XDisplayWidthMM(display, screen), XDisplayHeightMM(display, screen));
    }

    private static int ReadMonitor(nint display, SDL.Rect bounds)
    {
        try
        {
            if (XRRQueryExtension(display, out _, out _) == 0 ||
                XRRQueryVersion(display, out var major, out var minor) == 0 ||
                major < 1 || major == 1 && minor < 5)
                return 0;

            var monitors = XRRGetMonitors(display, XDefaultRootWindow(display), 1, out var count);
            if (monitors == null)
                return 0;

            try
            {
                Monitor* match = null;
                for (var i = 0; i < count; i++)
                {
                    var monitor = monitors + i;
                    if (monitor->X != bounds.X || monitor->Y != bounds.Y ||
                        monitor->Width != bounds.W || monitor->Height != bounds.H)
                        continue;
                    if (match != null)
                        return 0;
                    match = monitor;
                }

                return match == null ? 0 : Density(match->Width, match->Height, match->WidthMM, match->HeightMM);
            }
            finally { XRRFreeMonitors(monitors); }
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException)
        {
            return 0;
        }
    }

    private static int Density(int width, int height, int widthMM, int heightMM)
    {
        var x = widthMM > 0 && width > 0 ? width * 25.4 / widthMM : 0;
        var y = heightMM > 0 && height > 0 ? height * 25.4 / heightMM : 0;
        var dpi = x > 0 && y > 0 ? (x + y) / 2 : x + y;
        return dpi is >= 1 and <= int.MaxValue ? (int)dpi : 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Monitor
    {
        internal nuint Name;
        internal int Primary, Automatic, OutputCount, X, Y, Width, Height, WidthMM, HeightMM;
        internal nint Outputs;
    }

    [LibraryImport(XRandR)] private static partial int XRRQueryExtension(nint display, out int eventBase, out int errorBase);
    [LibraryImport(XRandR)] private static partial int XRRQueryVersion(nint display, out int major, out int minor);
    [LibraryImport(XRandR)] private static partial Monitor* XRRGetMonitors(nint display, nuint window, int active, out int count);
    [LibraryImport(XRandR)] private static partial void XRRFreeMonitors(Monitor* monitors);
    [LibraryImport(Xlib)] private static partial nuint XDefaultRootWindow(nint display);
    [LibraryImport(Xlib)] private static partial int XScreenCount(nint display);
    [LibraryImport(Xlib)] private static partial int XDefaultScreen(nint display);
    [LibraryImport(Xlib)] private static partial int XDisplayWidth(nint display, int screen);
    [LibraryImport(Xlib)] private static partial int XDisplayHeight(nint display, int screen);
    [LibraryImport(Xlib)] private static partial int XDisplayWidthMM(nint display, int screen);
    [LibraryImport(Xlib)] private static partial int XDisplayHeightMM(nint display, int screen);
}
