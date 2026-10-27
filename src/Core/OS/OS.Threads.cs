using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class OS
{
    private const ulong MainThreadIdentity = 1;
    [ThreadStatic] private static ulong _callerIdentity;
    private long _nextThreadIdentity = 1;
    private int _mainThreadOwner;

    /// <summary>Gets the reserved identity of the Engine lifecycle's main thread.</summary>
    /// <returns>The engine main-thread identity, including when no lifecycle is active.</returns>
    /// <remarks>Engine.Start and the owned window hosts assign this identity to their actual owner.
    /// Identities are process-local engine values, not operating-system thread IDs or access permissions.</remarks>
    public static ulong GetMainThreadID() => MainThreadIdentity;

    /// <summary>Gets the calling thread's engine identity for diagnostic correlation.</summary>
    /// <returns>A nonzero identity, equal to GetMainThreadID on the current Engine lifecycle owner.</returns>
    /// <remarks>Ordinary threads retain their own identity. Becoming or ceasing to be the main owner changes
    /// the returned role identity. Values are not persistent across processes and confer no scene access.</remarks>
    public static ulong GetThreadCallerID()
    {
        var service = Service;
        if (Volatile.Read(ref service._mainThreadOwner) == Environment.CurrentManagedThreadId) return MainThreadIdentity;
        if (_callerIdentity == 0) _callerIdentity = unchecked((ulong)Interlocked.Increment(ref service._nextThreadIdentity));
        return _callerIdentity;
    }

    internal static void SetMainThreadOwner(int managedThreadID) => Volatile.Write(ref Service._mainThreadOwner, managedThreadID);

    /// <summary>Assigns an operating-system name to the calling thread.</summary>
    /// <param name="name">The native thread name; embedded null characters are rejected.</param>
    /// <remarks>This cold operation names only the caller. Native byte-length limits apply without truncation;
    /// for example, Linux accepts at most 15 UTF-8 bytes. It does not change the managed Thread.Name property.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The name contains a null character or the native POSIX request rejects it.</exception>
    /// <exception cref="NotSupportedException">This platform has no integrated native thread-naming operation.</exception>
    /// <exception cref="InvalidOperationException">The native Windows request fails.</exception>
    public static void SetThreadName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Contains('\0')) throw new ArgumentException("A thread name cannot contain a null character.", nameof(name));
        if (OperatingSystem.IsWindows())
        {
            var result = SetThreadDescription(GetCurrentThread(), name);
            if (result < 0) throw new InvalidOperationException($"The native thread name request failed (HRESULT 0x{result:X8}).");
            return;
        }
        int error;
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS()) error = SetAppleThreadName(name);
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) error = SetPOSIXThreadName(GetPOSIXThread(), name);
        else throw new NotSupportedException("This platform has no native thread-naming adapter.");
        if (error != 0) throw new ArgumentException("The native thread name request was rejected.", nameof(name), new Win32Exception(error));
    }

    [LibraryImport("kernel32.dll")] private static partial nint GetCurrentThread();
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial int SetThreadDescription(nint thread, string description);
    [LibraryImport("libc", EntryPoint = "pthread_self")] private static partial nuint GetPOSIXThread();
    [LibraryImport("libc", EntryPoint = "pthread_setname_np", StringMarshalling = StringMarshalling.Utf8)] private static partial int SetPOSIXThreadName(nuint thread, string name);
    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "pthread_setname_np", StringMarshalling = StringMarshalling.Utf8)] private static partial int SetAppleThreadName(string name);
}
