using System.Diagnostics;
using System.Runtime.InteropServices;

internal static class PhysicsCPUClock
{
    internal static bool IsSupported => OperatingSystem.IsLinux();

    internal static (long Process, long Owner) Read() => (ReadClock(2), ReadClock(3));

    internal static void Verify()
    {
        if (!IsSupported) throw new PlatformNotSupportedException("The physical CPU clock probe requires Linux.");
        _ = Read();
        Thread.Sleep(1);
        var start = Read();
        var wall = Stopwatch.GetTimestamp();
        Thread.Sleep(20);
        var idle = Read();
        var elapsed = Stopwatch.GetElapsedTime(wall).TotalNanoseconds;
        if (idle.Process < start.Process || idle.Owner < start.Owner || idle.Owner - start.Owner >= elapsed / 2)
            throw new InvalidOperationException("The physical CPU clocks must be monotonic and exclude owner suspension.");
        Thread.SpinWait(100_000);
        var active = Read();
        if (active.Process <= idle.Process || active.Owner <= idle.Owner)
            throw new InvalidOperationException("Both physical CPU clocks must observe actual owner work.");
    }

    private static long ReadClock(int id)
    {
        if (ClockGetTime(id, out var value) != 0)
        {
            var error = Marshal.GetLastPInvokeError();
            throw new InvalidOperationException($"Physical CPU clock {id} failed with errno {error}.");
        }
        return checked((long)value.Seconds * 1_000_000_000 + (long)value.Nanoseconds);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Timespec
    {
        internal nint Seconds, Nanoseconds;
    }

    [DllImport("libc", EntryPoint = "clock_gettime", SetLastError = true)]
    private static extern int ClockGetTime(int clock, out Timespec value);
}
