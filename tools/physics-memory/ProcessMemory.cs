using System.Runtime.InteropServices;

internal static class ProcessMemory
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MallInfo
    {
        internal nuint Arena, Ordblks, Smblks, Hblks, Hblkhd, Usmblks, Fsmblks, Uordblks, Fordblks, Keepcost;
        internal readonly ulong TrackedBytes => checked((ulong)Uordblks + (ulong)Hblkhd);
    }
    [DllImport("libc", EntryPoint = "mallinfo2", CallingConvention = CallingConvention.Cdecl)]
    private static extern MallInfo Read();
    [DllImport("libc", EntryPoint = "gnu_get_libc_version", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint Version();
    [DllImport("libc", EntryPoint = "malloc", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint Allocate(nuint size);
    [DllImport("libc", EntryPoint = "free", CallingConvention = CallingConvention.Cdecl)]
    private static extern void Free(nint pointer);

    private static void RequireHost()
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64 || Marshal.SizeOf<MallInfo>() != 80)
            throw new PlatformNotSupportedException("This process-memory fixture requires Linux x64 glibc mallinfo2.");
    }

    internal static object Capture()
    {
        RequireHost();
        var gc = GC.GetGCMemoryInfo(GCKind.FullBlocking); var native = Read();
        if (gc.Index == 0 || gc.Generation != GC.MaxGeneration) throw new InvalidOperationException("Collect before the memory observation.");
        return new
        {
            GC = new { gc.Index, gc.Generation, gc.HeapSizeBytes, gc.FragmentedBytes, gc.TotalCommittedBytes },
            LibcVersion = Marshal.PtrToStringAnsi(Version()),
            MallInfo2 = new
            {
                Arena = (ulong)native.Arena,
                Ordblks = (ulong)native.Ordblks,
                Smblks = (ulong)native.Smblks,
                Hblks = (ulong)native.Hblks,
                Hblkhd = (ulong)native.Hblkhd,
                Usmblks = (ulong)native.Usmblks,
                Fsmblks = (ulong)native.Fsmblks,
                Uordblks = (ulong)native.Uordblks,
                Fordblks = (ulong)native.Fordblks,
                Keepcost = (ulong)native.Keepcost
            }
        };
    }

    internal static object Calibrate()
    {
        RequireHost();
        var version = Marshal.PtrToStringAnsi(Version());
        var warm = Allocate(1); if (warm == 0) throw new OutOfMemoryException();
        Marshal.WriteByte(warm, 1); Free(warm); _ = Read();
        const int bytes = 8 * 1024 * 1024;
        var before = Read(); var pointer = Allocate(bytes);
        if (pointer == 0) throw new OutOfMemoryException();
        MallInfo during;
        try
        {
            for (var offset = 0; offset < bytes; offset += Environment.SystemPageSize) Marshal.WriteByte(pointer, offset, 1);
            Marshal.WriteByte(pointer, bytes - 1, 1); during = Read();
        }
        finally { Free(pointer); }
        var after = Read();
        if (during.TrackedBytes < before.TrackedBytes + bytes || during.TrackedBytes < after.TrackedBytes + bytes)
            throw new InvalidOperationException("mallinfo2 did not observe the known live allocation and release.");
        return new
        {
            ABIBytes = Marshal.SizeOf<MallInfo>(),
            PointerBytes = IntPtr.Size,
            UordblksOffset = Marshal.OffsetOf<MallInfo>(nameof(MallInfo.Uordblks)).ToInt64(),
            HblkhdOffset = Marshal.OffsetOf<MallInfo>(nameof(MallInfo.Hblkhd)).ToInt64(),
            LibcVersion = version,
            AllocationBytes = bytes,
            ObservedIncreaseBytes = during.TrackedBytes - before.TrackedBytes,
            ObservedDecreaseBytes = during.TrackedBytes - after.TrackedBytes
        };
    }
}
