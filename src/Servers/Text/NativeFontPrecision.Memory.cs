using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class NativeFontPrecision
{
    private FTMemory* _ftMemory;

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
