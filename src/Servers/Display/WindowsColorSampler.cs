using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class WindowsColorSampler : IDesktopColorSampler
{
    private const string User32 = "user32.dll";
    private const int MouseHook = 14;
    private const uint LeftDown = 0x0201, LeftUp = 0x0202, RightDown = 0x0204, RightUp = 0x0205;
    private const uint AllInput = 0x04ff, InputAvailable = 4, RemoveMessage = 1;
    private static readonly uint ProcessID = (uint)Environment.ProcessId;
    private static WindowsColorSampler? _active;
    private readonly Thread _thread;
    private readonly ManualResetEvent _stop = new(false);
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Vector2i _point;
    private Exception? _failure;
    private int _result;
    private bool _leftDown, _rightDown, _disposed;

    private WindowsColorSampler()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "Electron2D color sampler" };
    }

    internal static bool HasForeground => GetWindowThreadProcessId(GetForegroundWindow(), out var pid) != 0 && pid == ProcessID;

    internal static WindowsColorSampler Open()
    {
        var sampler = new WindowsColorSampler();
        if (Interlocked.CompareExchange(ref _active, sampler, null) is not null)
        {
            sampler.Dispose();
            throw new InvalidOperationException("A Windows desktop color sampler is already active.");
        }
        try
        {
            sampler._thread.Start();
            sampler._ready.Task.GetAwaiter().GetResult();
            return sampler;
        }
        catch
        {
            sampler.Dispose();
            throw;
        }
    }

    public bool TryReadSelection(out Vector2i point, out bool accepted)
    {
        if (Volatile.Read(ref _failure) is { } error)
            throw new IOException("The Windows color sampler stopped.", error);
        var result = Volatile.Read(ref _result);
        point = result == 0 ? default : _point;
        accepted = result == 1;
        return result != 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stop.Set();
        if (_thread.IsAlive) _thread.Join();
        _stop.Dispose();
        Interlocked.CompareExchange(ref _active, null, this);
    }

    private void Run()
    {
        nint hook = 0;
        try
        {
            // A separate pump keeps native input delivery independent of frame submission.
            _ = PeekMessageW(out _, 0, 0, 0, 0);
            hook = SetWindowsHookExW(MouseHook, &OnMouse, GetModuleHandleW(0), 0);
            if (hook == 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not acquire the Windows pointer for color sampling.");
            _ready.SetResult();
            var stop = _stop.SafeWaitHandle.DangerousGetHandle();
            uint wait;
            while ((wait = MsgWaitForMultipleObjectsEx(1, &stop, uint.MaxValue, AllInput, InputAvailable)) == 1)
                while (PeekMessageW(out _, 0, 0, 0, RemoveMessage) != 0) { }
            if (wait != 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not wait for Windows color sampling input.");
        }
        catch (Exception error)
        {
            Volatile.Write(ref _failure, error);
            _ready.TrySetException(error);
        }
        finally
        {
            if (hook != 0) UnhookWindowsHookEx(hook);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint OnMouse(int code, nuint message, nint data)
    {
        if (code == 0 && message is LeftDown or LeftUp or RightDown or RightUp &&
            _active is { _result: 0 } sampler &&
            HasForeground)
        {
            ref var pressed = ref (message is LeftDown or LeftUp ? ref sampler._leftDown : ref sampler._rightDown);
            if (message is LeftDown or RightDown)
            {
                pressed = true;
                return 1;
            }
            if (pressed)
            {
                sampler._point = new(((int*)data)[0], ((int*)data)[1]);
                Volatile.Write(ref sampler._result, message == LeftUp ? 1 : 2);
                return 1;
            }
        }
        return CallNextHookEx(0, code, message, data);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        internal nint Window;
        internal uint Type;
        internal nuint WParam;
        internal nint LParam;
        internal uint Time;
        internal int X, Y;
        internal uint Private;
    }

    [LibraryImport(User32, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint SetWindowsHookExW(int type, delegate* unmanaged[Stdcall]<int, nuint, nint, nint> callback, nint module, uint thread);
    [LibraryImport(User32), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int UnhookWindowsHookEx(nint hook);
    [LibraryImport(User32), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint CallNextHookEx(nint hook, int code, nuint message, nint data);
    [LibraryImport(User32), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint GetForegroundWindow();
    [LibraryImport(User32), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport(User32), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int PeekMessageW(out Message message, nint window, uint minimum, uint maximum, uint flags);
    [LibraryImport(User32, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint MsgWaitForMultipleObjectsEx(uint count, nint* handles, uint timeout, uint mask, uint flags);
    [LibraryImport("kernel32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint GetModuleHandleW(nint name);
}
