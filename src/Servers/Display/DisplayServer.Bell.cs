using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Requests the desktop system bell using the current operating system or compositor policy.</summary>
    /// <remarks>The request is independent of engine audio. A muted system bell or unavailable provider can produce no feedback. Call on the display opening thread.</remarks>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off its opening thread, or the Wayland connection fails.</exception>
    public static void Beep() => RequireService().BeepCore();

    private void BeepCore()
    {
        EnsureOwner();
        switch (_videoDriver)
        {
            case "windows" when OperatingSystem.IsWindows():
                _ = MessageBeep(0); // MB_OK is the user's default system sound.
                break;
            case "cocoa" when OperatingSystem.IsMacOS():
                NSBeep();
                break;
            case "x11" when OperatingSystem.IsLinux():
                var properties = SDL.GetWindowProperties(GetWindow(MainWindowId));
                var display = SDL.GetPointerProperty(properties, SDL.Props.WindowX11DisplayPointer, 0);
                if (display != 0)
                {
                    _ = XBell(display, 0);
                    _ = XFlush(display);
                }
                break;
            case "wayland" when OperatingSystem.IsLinux():
                _waylandInput?.RingSystemBell();
                break;
        }
    }

    [LibraryImport("user32.dll")] private static partial int MessageBeep(uint type);
    [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit")] private static partial void NSBeep();
    [LibraryImport("libX11.so.6")] private static partial int XBell(nint display, int percent);
    [LibraryImport("libX11.so.6")] private static partial int XFlush(nint display);
}
