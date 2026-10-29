using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

// Native ownership and runtime producers share the private bundle's allocation gate.
internal static unsafe partial class NativeAllocation
{
    private static bool _ready;
    [StructLayout(LayoutKind.Sequential)]
    private struct State
    {
        public ulong ManagedCurrent, ManagedPeak, NativeCurrent, NativePeak, Current, Peak,
            Allocated, Retired, Allocations, Resizes, Frees, Failures;
    }
    internal static void Prepare()
    {
        if (Volatile.Read(ref _ready)) return;
        try
        {
            if (MemoryABI() != 1) throw new PlatformNotSupportedException("The native allocation ABI does not match this runtime.");
        }
        catch (EntryPointNotFoundException error)
        { throw new PlatformNotSupportedException("Native allocation accounting requires the matching source-built native bundle.", error); }
        Volatile.Write(ref _ready, true);
    }
    internal static (long Current, long Peak) Snapshot()
    {
        Prepare(); ReadState(out var state);
        return ((long)state.NativeCurrent, (long)state.NativePeak);
    }
    // A native provider can return a fallback value after refusing its required storage.
    internal static ulong GetFailureEpoch()
    {
        Prepare();
        try { return FailureEpoch(); }
        catch (EntryPointNotFoundException error)
        { throw new PlatformNotSupportedException("Native allocation error tracking requires the matching source-built native bundle.", error); }
    }
    internal static void ThrowIfFailedSince(ulong epoch)
    {
        if (GetFailureEpoch() != epoch) throw new OutOfMemoryException();
    }
    internal static void* Alloc(nuint size) => Allocate(size, 0, 0);
    internal static void* AllocZeroed(nuint size) => Allocate(size, 0, 1);
    internal static void* AlignedAlloc(nuint size, nuint alignment)
    {
        if (alignment == 0 || (alignment & (alignment - 1)) != 0) throw new ArgumentException("Alignment must be a power of two.", nameof(alignment));
        return Allocate(size, alignment, 0);
    }
    private static void* Allocate(nuint size, nuint alignment, uint zero)
    {
        Prepare();
        var result = AllocateNative(size, alignment, zero, out var error);
        if (error != 0) throw new OutOfMemoryException();
        return result;
    }
    internal static void* Realloc(void* value, nuint size)
    {
        Prepare();
        var result = ReallocateNative(value, size, out var error);
        if (error == 1) throw new ArgumentException("An aligned block cannot use ordinary reallocation.", nameof(value));
        if (error != 0) throw new OutOfMemoryException();
        return result;
    }
    internal static void Free(void* value) { if (value is not null) { Prepare(); ReleaseNative(value); } }
    internal static void AlignedFree(void* value) => Free(value);
    [LibraryImport(NativeLibraries.ENetLibrary, EntryPoint = "e2d_memory_abi"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint MemoryABI();
    [LibraryImport(NativeLibraries.ENetLibrary, EntryPoint = "e2d_memory_allocate"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void* AllocateNative(nuint size, nuint alignment, uint zero, out int error);
    [LibraryImport(NativeLibraries.ENetLibrary, EntryPoint = "e2d_memory_reallocate"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void* ReallocateNative(void* value, nuint size, out int error);
    [LibraryImport(NativeLibraries.ENetLibrary, EntryPoint = "e2d_memory_release"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void ReleaseNative(void* value);
    [LibraryImport(NativeLibraries.ENetLibrary, EntryPoint = "e2d_memory_snapshot"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void ReadState(out State state);
    [LibraryImport(NativeLibraries.ENetLibrary, EntryPoint = "e2d_memory_failure_epoch"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ulong FailureEpoch();
}
