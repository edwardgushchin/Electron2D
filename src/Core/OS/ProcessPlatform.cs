using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static partial class ProcessPlatform
{
    [LibraryImport("libc", EntryPoint = "waitid", SetLastError = true)]
    private static partial int WaitLinux(int type, uint pid, nint information, int options);
    [LibraryImport("libSystem.B.dylib", EntryPoint = "waitid", SetLastError = true)]
    private static partial int WaitDarwin(int type, uint pid, nint information, int options);
    [LibraryImport("kernel32.dll", EntryPoint = "TerminateProcess", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TerminateWindows(nint process, uint exitCode);
    internal static unsafe bool ObserveExit(int pid, out int code)
    {
        Span<byte> information = stackalloc byte[128]; information.Clear(); int result;
        fixed (byte* data = information)
        {
            do { information.Clear(); result = OperatingSystem.IsMacOS() ? WaitDarwin(1, (uint)pid, (nint)data, 1 | 4 | 0x20) : WaitLinux(1, (uint)pid, (nint)data, 1 | 4 | 0x1000000); } while (result != 0 && Marshal.GetLastPInvokeError() == 4);
            if (result != 0) throw new IOException("Cannot observe owned child exit status.", new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError()));
            var offset = OperatingSystem.IsMacOS() || IntPtr.Size == 4 ? 12 : 16;
            if (*(int*)(data + offset) == 0) { code = 0; return false; }
            var reason = *(int*)(data + 8); var status = *(int*)(data + offset + 8);
            code = reason == 1 ? status : status | (reason == 3 ? 128 : 0); return true;
        }
    }
    internal static void Kill(Process process)
    {
        if (OperatingSystem.IsWindows()) { if (!TerminateWindows(process.Handle, 0)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError()); }
        else process.Kill();
    }
}
