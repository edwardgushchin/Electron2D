using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class NativeFontPrecision
{
    private FTMemory* _ftMemory;
    private static readonly object MemoryGate = new();
    private static bool _memoryReady;

    private static void PrepareHarfBuzzMemory()
    {
        lock (MemoryGate)
        {
            if (_memoryReady) return;
            try
            {
                if (HarfBuzzMemory(&AllocateHarfBuzzMemory, &ReallocateHarfBuzzMemory, &FreeHarfBuzzMemory) != 0)
                    throw new InvalidOperationException("The native font allocator has already been assigned to another owner.");
            }
            catch (EntryPointNotFoundException error)
            { throw new PlatformNotSupportedException("Font allocation accounting requires the matching source-built native bundle.", error); }
            _memoryReady = true;
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* AllocateHarfBuzzMemory(nuint size)
    {
        try { return NativeAllocation.Alloc(size); } catch (OutOfMemoryException) { return null; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* ReallocateHarfBuzzMemory(void* value, nuint size)
    {
        try { return NativeAllocation.Realloc(value, size); } catch (OutOfMemoryException) { return null; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void FreeHarfBuzzMemory(void* value) => NativeAllocation.Free(value);
    [LibraryImport(NativeLibraries.FreeTypeLibrary, EntryPoint = "e2d_ft_hb_memory"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int HarfBuzzMemory(delegate* unmanaged[Cdecl]<nuint, void*> allocate,
        delegate* unmanaged[Cdecl]<void*, nuint, void*> resize, delegate* unmanaged[Cdecl]<void*, void> release);

    [StructLayout(LayoutKind.Sequential)]
    internal struct FTMemory
    {
        internal void* User;
        internal delegate* unmanaged[Cdecl]<FTMemory*, CLong, void*> Allocate;
        internal delegate* unmanaged[Cdecl]<FTMemory*, void*, void> Free;
        internal delegate* unmanaged[Cdecl]<FTMemory*, CLong, CLong, void*, void*> Reallocate;
        internal static FTMemory Create() => new() { Allocate = &AllocateMemory, Free = &FreeMemory, Reallocate = &ReallocateMemory };
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* AllocateMemory(FTMemory* memory, CLong size)
    {
        if (size.Value < 0) return null;
        try { return NativeAllocation.Alloc((nuint)size.Value); } catch (OutOfMemoryException) { return null; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void FreeMemory(FTMemory* memory, void* block) => NativeAllocation.Free(block);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* ReallocateMemory(FTMemory* memory, CLong current, CLong size, void* block)
    {
        if (size.Value < 0) return null;
        try { return NativeAllocation.Realloc(block, (nuint)size.Value); } catch (OutOfMemoryException) { return null; }
    }
}
