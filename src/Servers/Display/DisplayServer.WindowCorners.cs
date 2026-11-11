using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private const int DWMWAWindowCornerPreference = 33;
    private const int DWMCornerDefault = 0;
    private const int DWMCornerDoNotRound = 1;

    private nint CornerWindow()
    {
        if (_videoDriver != "windows" || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            throw new NotSupportedException("Sharp native window corners require the Windows 11 display adapter.");
        return WindowGetNativeHandleCore(HandleType.WindowHandle);
    }

    private bool GetSharpCorners()
    {
        var result = DwmGetWindowAttribute(CornerWindow(), DWMWAWindowCornerPreference, out var preference, sizeof(int));
        if (result < 0)
            throw new IOException("Cannot read the native window corner preference.", Marshal.GetExceptionForHR(result, -1));
        return preference == DWMCornerDoNotRound;
    }

    private void SetSharpCorners(bool enabled)
    {
        var preference = enabled ? DWMCornerDoNotRound : DWMCornerDefault;
        var result = DwmSetWindowAttribute(CornerWindow(), DWMWAWindowCornerPreference, in preference, sizeof(int));
        if (result < 0)
            throw new IOException("Cannot change the native window corner preference.", Marshal.GetExceptionForHR(result, -1));
    }

    [LibraryImport("dwmapi.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DwmGetWindowAttribute(nint window, int attribute, out int value, uint size);

    [LibraryImport("dwmapi.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DwmSetWindowAttribute(nint window, int attribute, in int value, uint size);
}
