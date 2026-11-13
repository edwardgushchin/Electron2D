using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Electron2D;

internal static partial class ProcessPlatform
{
    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsStartup
    {
        internal int Size;
        internal nint Reserved, Desktop, Title;
        internal int X, Y, Width, Height, Columns, Rows, Fill, Flags;
        internal short Show, ReservedSize;
        internal nint ReservedData, Input, Output, Error, Attributes;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsProcess { internal nint Process, Thread; internal int PID, ThreadID; }
    [LibraryImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool CreateWindows(nint application, char* command, nint processSecurity, nint threadSecurity, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint flags, nint environment, nint directory, ref WindowsStartup startup, out WindowsProcess process);
    [LibraryImport("kernel32.dll", EntryPoint = "GetStdHandle")]
    private static partial nint StandardWindows(int kind);
    [LibraryImport("kernel32.dll", EntryPoint = "DuplicateHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DuplicateWindows(nint sourceProcess, nint source, nint targetProcess, out SafeFileHandle target, uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint options);
    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial SafeFileHandle FileWindows(string path, uint access, uint share, nint security, uint creation, uint flags, nint template);
    [LibraryImport("kernel32.dll", EntryPoint = "InitializeProcThreadAttributeList", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool InitializeWindowsAttributes(nint attributes, int count, uint flags, ref nuint size);
    [LibraryImport("kernel32.dll", EntryPoint = "UpdateProcThreadAttribute", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool UpdateWindowsAttributes(nint attributes, uint flags, nuint attribute, nint* value, nuint size, nint previous, nint returnedSize);
    [LibraryImport("kernel32.dll", EntryPoint = "DeleteProcThreadAttributeList")]
    private static partial void DeleteWindowsAttributes(nint attributes);
    [LibraryImport("kernel32.dll", EntryPoint = "WaitForSingleObject", SetLastError = true)]
    private static partial uint WaitWindows(SafeProcessHandle process, uint milliseconds);
    [LibraryImport("kernel32.dll", EntryPoint = "GetExitCodeProcess", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ExitWindows(SafeProcessHandle process, out int code);

    internal static bool PollWindows(SafeProcessHandle process, out int code)
    {
        code = 0; var state = WaitWindows(process, 0);
        if (state == 258) return false;
        if (state != 0 || !ExitWindows(process, out code)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        return true;
    }
    internal static void KillWindows(SafeProcessHandle process)
    {
        if (!TerminateWindows(process.DangerousGetHandle(), 0)) throw new Win32Exception(Marshal.GetLastPInvokeError());
    }
    private static SafeFileHandle InheritWindows(nint handle, bool input)
    {
        using var fallback = handle is 0 or -1 ? FileWindows("NUL", input ? 0x80000000u : 0x40000000u, 3, 0, 3, 0, 0) : null;
        if (fallback is { IsInvalid: true }) throw new Win32Exception(Marshal.GetLastPInvokeError());
        if (!DuplicateWindows(-1, fallback?.DangerousGetHandle() ?? handle, -1, out var duplicate, 0, true, 2)) { duplicate.Dispose(); throw new Win32Exception(Marshal.GetLastPInvokeError()); }
        return duplicate;
    }
    private static void AppendWindowsArgument(StringBuilder command, string argument)
    {
        if (command.Length != 0) command.Append(' ');
        command.Append('"'); var slashes = 0;
        foreach (var value in argument)
        {
            if (value == '\\') { slashes++; continue; }
            command.Append('\\', value == '"' ? slashes * 2 + 1 : slashes).Append(value); slashes = 0;
        }
        command.Append('\\', slashes * 2).Append('"');
    }
    internal static unsafe OSProcess StartWindows(string path, ReadOnlySpan<string> arguments, bool console, bool capture, bool mergeError, out AnonymousPipeServerStream? output)
    {
        output = null;
        AnonymousPipeServerStream? pipe = null;
        var command = new StringBuilder(); AppendWindowsArgument(command, path);
        foreach (var argument in arguments) AppendWindowsArgument(command, argument);
        var text = (command.ToString() + '\0').ToCharArray();
        var startup = new WindowsStartup { Size = Marshal.SizeOf<WindowsStartup>() };
        var attributes = (nint)0; var initialized = false;
        SafeFileHandle? input = null, stdout = null, stderr = null;
        try
        {
            // New consoles get their own standard devices unless capture was requested.
            if (capture || !console)
            {
                startup.Flags = 0x100;
                input = InheritWindows(StandardWindows(-10), true);
                if (capture) output = pipe = new(PipeDirection.In, HandleInheritability.Inheritable);
                else stdout = InheritWindows(StandardWindows(-11), false);
                stderr = capture ? mergeError ? null : InheritWindows(0, false) : InheritWindows(StandardWindows(-12), false);
                startup.Input = input.DangerousGetHandle();
                startup.Output = output?.ClientSafePipeHandle.DangerousGetHandle() ?? stdout!.DangerousGetHandle();
                startup.Error = capture && mergeError ? startup.Output : stderr!.DangerousGetHandle();
                var handles = new[] { startup.Input, startup.Output, startup.Error }.Distinct().ToArray();
                nuint size = 0; InitializeWindowsAttributes(0, 1, 0, ref size);
                attributes = (nint)NativeMemory.Alloc(size);
                if (!InitializeWindowsAttributes(attributes, 1, 0, ref size)) throw new Win32Exception(Marshal.GetLastPInvokeError());
                initialized = true;
                fixed (nint* values = handles)
                {
                    if (!UpdateWindowsAttributes(attributes, 0, 0x20002, values, (nuint)(handles.Length * IntPtr.Size), 0, 0)) throw new Win32Exception(Marshal.GetLastPInvokeError());
                    startup.Attributes = attributes;
                    return Launch();
                }
            }
            return Launch();
        }
        catch { output?.Dispose(); output = null; throw; }
        finally
        {
            input?.Dispose(); stdout?.Dispose(); stderr?.Dispose();
            if (initialized) DeleteWindowsAttributes(attributes);
            NativeMemory.Free((void*)attributes);
        }

        OSProcess Launch()
        {
            fixed (char* line = text)
            {
                var flags = (console ? 0x10u : 0x08000000u) | (attributes != 0 ? 0x80000u : 0);
                if (!CreateWindows(0, line, 0, 0, attributes != 0, flags, 0, 0, ref startup, out var process)) throw new Win32Exception(Marshal.GetLastPInvokeError());
                using var thread = new SafeWaitHandle(process.Thread, true);
                pipe?.DisposeLocalCopyOfClientHandle();
                return new OSProcess(new SafeProcessHandle(process.Process, true), process.PID);
            }
        }
    }
}
