using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class OS
{
    /// <summary>Queries physical, free, available and stack memory from the native operating system.</summary>
    /// <returns>An independent byte-valued snapshot; each unknown or unrepresentable value is -1.</returns>
    /// <remarks>May be called from any thread without an Engine lifecycle, window or renderer.
    /// This is advisory host information, not a managed-heap measure or a reservation. See OSMemoryInfo for platform semantics.</remarks>
    /// <exception cref="NotSupportedException">The platform has no native host-memory adapter.</exception>
    public static OSMemoryInfo GetMemoryInfo() => Service.GetMemoryInfoCore();

    private unsafe OSMemoryInfo GetMemoryInfoCore()
    {
        ThrowIfDisposed();
        if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
        {
            long physical = -1, free = -1, available = -1;
            if (GetLinuxMemory(out var info) == 0)
            {
                physical = MemoryBytes(info.TotalRAM, info.Unit);
                free = MemoryBytes(info.FreeRAM, info.Unit);
                available = AddMemoryBytes(free, MemoryBytes(info.FreeSwap, info.Unit));
            }
            return new(physical, free, available, GetLinuxResourceLimit(3, out var limit) == 0 ? MemoryBytes(limit.Current, 1) : -1);
        }
        if (OperatingSystem.IsWindows())
        {
            var info = new WindowsMemoryStatus { Size = (uint)sizeof(WindowsMemoryStatus) };
            long physical = -1, free = -1, available = -1;
            if (GetWindowsMemory(ref info, info.Size))
            {
                physical = MemoryBytes(info.PhysicalTotal, info.PageSize);
                free = MemoryBytes(info.PhysicalAvailable, info.PageSize);
                available = MemoryBytes(info.CommitLimit, info.PageSize);
            }
            GetCurrentThreadStackLimits(out var low, out var high);
            return new(physical, free, available, high >= low ? MemoryBytes(high - low, 1) : -1);
        }
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
        {
            ulong physicalValue = 0;
            nuint size = sizeof(ulong);
            long physical = GetAppleMemoryValue("hw.memsize", &physicalValue, ref size, 0, 0) == 0 && size == sizeof(ulong)
                ? MemoryBytes(physicalValue, 1) : -1;
            long free = -1, available = -1;
            var count = (uint)sizeof(AppleMemoryStatus) / sizeof(uint);
            if (AppleMemoryHost.Port != 0 && GetAppleMemory(AppleMemoryHost.Port, 4, out var info, ref count) == 0 && count >= 36)
            {
                var usedPages = (ulong)info.Active + info.Inactive + info.Speculative + info.Wired + info.Compressor;
                var reclaimablePages = (ulong)info.Purgeable + info.External;
                var used = usedPages >= reclaimablePages ? MemoryBytes(usedPages - reclaimablePages, (ulong)Environment.SystemPageSize) : -1;
                free = physical >= 0 && used >= 0 && used <= physical ? physical - used : -1;
                if (OperatingSystem.IsMacOS())
                {
                    AppleSwapStatus swap = default;
                    size = (nuint)sizeof(AppleSwapStatus);
                    if (GetAppleMemoryValue("vm.swapusage", &swap, ref size, 0, 0) == 0 && size == (nuint)sizeof(AppleSwapStatus))
                        available = AddMemoryBytes(free, MemoryBytes(swap.Available, 1));
                }
                else available = free;
            }
            return new(physical, free, available, GetAppleResourceLimit(3, out var limit) == 0 && limit.Current != long.MaxValue ? MemoryBytes(limit.Current, 1) : -1);
        }
        throw new NotSupportedException("This platform has no native host-memory adapter.");
    }

    internal static long MemoryBytes(ulong units, ulong unitSize) => unitSize == 0 || units > (ulong)long.MaxValue / unitSize ? -1 : (long)(units * unitSize);
    internal static long AddMemoryBytes(long first, long second) => first < 0 || second < 0 || first > long.MaxValue - second ? -1 : first + second;

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct LinuxMemoryStatus
    {
        internal nint Uptime;
        internal nuint Load1, Load5, Load15, TotalRAM, FreeRAM, SharedRAM, BufferRAM, TotalSwap, FreeSwap;
        internal ushort Processes, Padding;
        internal nuint TotalHigh, FreeHigh;
        internal uint Unit;
        // The 32-bit ABI writes eight trailing reserved bytes; extra output space is harmless on 64-bit hosts.
        internal fixed byte Reserved[8];
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ResourceLimit { internal ulong Current, Maximum; }
    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsMemoryStatus
    {
        internal uint Size;
        internal nuint CommitTotal, CommitLimit, CommitPeak, PhysicalTotal, PhysicalAvailable, SystemCache, KernelTotal, KernelPaged, KernelNonpaged, PageSize;
        internal uint HandleCount, ProcessCount, ThreadCount;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct AppleMemoryStatus
    {
        internal uint Free, Active, Inactive, Wired;
        internal ulong ZeroFill, Reactivations, PageIns, PageOuts, Faults, CopyOnWrite, Lookups, Hits, Purges;
        internal uint Purgeable, Speculative;
        internal ulong Decompressions, Compressions, SwapIns, SwapOuts;
        internal uint Compressor, Throttled, External, Internal;
        internal ulong TotalUncompressed, Swapped;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct AppleSwapStatus { internal ulong Total, Available, Used; internal uint PageSize, Encrypted; }
    private static class AppleMemoryHost
    {
        // Retain one process-owned send right instead of acquiring a new right on every snapshot.
        internal static readonly uint Port = GetAppleMemoryHost();
    }
    [LibraryImport("libc", EntryPoint = "sysinfo")] private static partial int GetLinuxMemory(out LinuxMemoryStatus info);
    [LibraryImport("libc", EntryPoint = "getrlimit64")] private static partial int GetLinuxResourceLimit(int resource, out ResourceLimit limit);
    [LibraryImport("psapi.dll", EntryPoint = "GetPerformanceInfo")]
    [return: MarshalAs(UnmanagedType.Bool)] private static partial bool GetWindowsMemory(ref WindowsMemoryStatus info, uint size);
    [LibraryImport("kernel32.dll")] private static partial void GetCurrentThreadStackLimits(out nuint low, out nuint high);
    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "getrlimit")] private static partial int GetAppleResourceLimit(int resource, out ResourceLimit limit);
    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "sysctlbyname", StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial int GetAppleMemoryValue(string name, void* value, ref nuint size, nint newValue, nuint newSize);
    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "mach_host_self")] private static partial uint GetAppleMemoryHost();
    [LibraryImport("/usr/lib/libSystem.B.dylib", EntryPoint = "host_statistics64")] private static partial int GetAppleMemory(uint host, int flavor, out AppleMemoryStatus info, ref uint count);
}
