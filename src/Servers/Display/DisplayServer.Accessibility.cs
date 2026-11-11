using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private enum AccessibilityPreference { ScreenReader, Contrast, Animation, Transparency }

    private int ReadAccessibilityPreference(AccessibilityPreference preference)
    {
        EnsureOwner();
        if (_videoDriver == "windows" && OperatingSystem.IsWindows())
        {
            return preference switch
            {
                AccessibilityPreference.ScreenReader => ReadWindowsScreenReader(),
                AccessibilityPreference.Contrast => ReadWindowsHighContrast(),
                AccessibilityPreference.Animation => ReadWindowsAccessibilityFlag(0x1042, invert: true), // SPI_GETCLIENTAREAANIMATION
                AccessibilityPreference.Transparency => ReadWindowsAccessibilityFlag(0x1040), // SPI_GETDISABLEOVERLAPPEDCONTENT
                _ => -1,
            };
        }

        if (OperatingSystem.IsLinux() && _videoDriver is "x11" or "wayland")
        {
            return preference switch
            {
                AccessibilityPreference.ScreenReader => LinuxPortalDBus.ReadScreenReader(),
                AccessibilityPreference.Contrast => LinuxPortalDBus.ReadAppearancePreference("contrast"),
                AccessibilityPreference.Animation => LinuxPortalDBus.ReadAppearancePreference("reduced-motion"),
                _ => -1,
            };
        }

        return -1;
    }

    private static int ReadWindowsScreenReader()
    {
        var flag = ReadWindowsAccessibilityFlag(0x0046); // SPI_GETSCREENREADER
        if (flag == 1)
            return 1;

        // Narrator does not set SPI_SETSCREENREADER.
        var narrator = OpenAccessibilityMutex(0x00100000, 0, "NarratorRunning"); // SYNCHRONIZE
        if (narrator == 0)
            return Marshal.GetLastPInvokeError() == 2 ? flag : -1; // ERROR_FILE_NOT_FOUND

        _ = CloseAccessibilityHandle(narrator);
        return 1;
    }

    private static int ReadWindowsHighContrast()
    {
        const uint highContrastOn = 1;
        var state = new WindowsHighContrast { Size = (uint)Marshal.SizeOf<WindowsHighContrast>() };
        if (GetWindowsHighContrast(0x0042, state.Size, ref state, 0) == 0) // SPI_GETHIGHCONTRAST
            return -1;

        // User32 can return the same scheme pointer on successive reads. Only the flags are consumed.
        return (state.Flags & highContrastOn) != 0 ? 1 : 0;
    }

    private static int ReadWindowsAccessibilityFlag(uint action, bool invert = false) =>
        GetWindowsAccessibilityFlag(action, 0, out var enabled, 0) == 0 ? -1 : ((enabled != 0) != invert ? 1 : 0);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsHighContrast
    {
        internal uint Size, Flags;
        internal nint Scheme;
    }

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    private static partial int GetWindowsAccessibilityFlag(uint action, uint parameter, out int value, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    private static partial int GetWindowsHighContrast(uint action, uint parameter, ref WindowsHighContrast value, uint flags);

    [LibraryImport("kernel32.dll", EntryPoint = "OpenMutexW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint OpenAccessibilityMutex(uint access, int inherit, string name);

    [LibraryImport("kernel32.dll", EntryPoint = "CloseHandle")]
    private static partial int CloseAccessibilityHandle(nint handle);
}
