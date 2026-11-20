using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Electron2D;

public sealed partial class OS
{
    private const uint TokenQuery = 0x0008;
    private const int TokenIsAppContainer = 29;

    private static bool IsWindowsAppContainer()
    {
        var opened = OpenProcessToken(GetCurrentProcess(), TokenQuery, out var token);
        var error = Marshal.GetLastPInvokeError();
        using (token)
        {
            if (opened == 0)
            {
                throw new Win32Exception(error, "Could not open the current process token.");
            }

            return ReadWindowsAppContainer(token);
        }
    }

    internal static bool ReadWindowsAppContainer(SafeAccessTokenHandle token)
    {
        if (GetTokenInformation(token, TokenIsAppContainer, out var value, sizeof(uint), out _) == 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not query AppContainer status.");
        }

        return value != 0;
    }

    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint GetCurrentProcess();

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int OpenProcessToken(nint process, uint desiredAccess, out SafeAccessTokenHandle token);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GetTokenInformation(
        SafeAccessTokenHandle token, int informationClass, out uint information, uint informationLength, out uint returnLength);
}
