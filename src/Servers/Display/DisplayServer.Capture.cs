using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private const uint CaptureExcluded = 0x11;

    private bool HasCaptureExclusion() => _videoDriver == "windows" &&
        OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) &&
        DwmIsCompositionEnabled(out var enabled) >= 0 && enabled != 0;

    private nint CaptureWindow()
    {
        if (!HasCaptureExclusion())
            throw new NotSupportedException("Window capture exclusion requires Windows 10 version 2004 or later with desktop composition enabled.");
        return WindowGetNativeHandleCore(HandleType.WindowHandle);
    }

    private bool GetCaptureExclusion()
    {
        if (GetWindowDisplayAffinity(CaptureWindow(), out var affinity) == 0)
            throw CaptureFailure("read window capture policy");
        return affinity == CaptureExcluded;
    }

    private void SetCaptureExclusion(bool enabled)
    {
        if (SetWindowDisplayAffinity(CaptureWindow(), enabled ? CaptureExcluded : 0) == 0)
            throw CaptureFailure("change window capture policy");
    }

    private static IOException CaptureFailure(string operation) =>
        new($"Cannot {operation}.", new Win32Exception(Marshal.GetLastPInvokeError()));

    [LibraryImport("dwmapi.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DwmIsCompositionEnabled(out int enabled);
    [LibraryImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GetWindowDisplayAffinity(nint window, out uint affinity);
    [LibraryImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int SetWindowDisplayAffinity(nint window, uint affinity);
}
