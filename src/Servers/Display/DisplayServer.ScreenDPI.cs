using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private const int DefaultScreenDPI = 96;

    private int ScreenGetDPICore(int screen)
    {
        EnsureOwner();
        if (!TryGetDisplayID(screen, out var id))
            return DefaultScreenDPI;

        if (_videoDriver == "windows" && OperatingSystem.IsWindows())
        {
            var monitor = SDL.GetPointerProperty(SDL.GetDisplayProperties(id), SDL.Props.DisplayWindowsHMonitorPointer, 0);
            if (monitor != 0 && GetDpiForMonitor(monitor, 0, out var x, out var y) >= 0 && x > 0 && y > 0)
            {
                var dpi = ((ulong)x + y) / 2;
                return dpi <= int.MaxValue ? (int)dpi : DefaultScreenDPI;
            }
        }
        else if (_videoDriver == "x11" && OperatingSystem.IsLinux() && SDL.GetDisplayBounds(id, out var bounds))
        {
            var properties = SDL.GetWindowProperties(GetWindow(MainWindowId));
            var display = SDL.GetPointerProperty(properties, SDL.Props.WindowX11DisplayPointer, 0);
            if (display != 0)
            {
                var dpi = X11ScreenDPI.Read(display, bounds, GetDisplays().Length == 1);
                if (dpi > 0)
                    return dpi;
            }
        }
        else if (_videoDriver == "wayland" && _waylandInput is { } wayland)
        {
            var output = SDL.GetPointerProperty(SDL.GetDisplayProperties(id), SDL.Props.DisplayWaylandWLOutputPointer, 0);
            var dpi = wayland.ReadOutputDPI(output);
            if (dpi > 0)
                return dpi;
        }

        return DefaultScreenDPI;
    }

    internal static int PhysicalScreenDPI(int width, int height, int widthMM, int heightMM)
    {
        var x = widthMM > 0 && width > 0 ? width * 25.4 / widthMM : 0;
        var y = heightMM > 0 && height > 0 ? height * 25.4 / heightMM : 0;
        var dpi = x > 0 && y > 0 ? (x + y) / 2 : x + y;
        return dpi is >= 1 and <= int.MaxValue ? (int)dpi : 0;
    }

    [LibraryImport("shcore.dll")]
    private static partial int GetDpiForMonitor(nint monitor, int type, out uint dpiX, out uint dpiY);
}
