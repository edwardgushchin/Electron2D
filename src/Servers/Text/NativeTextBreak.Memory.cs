using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe partial class NativeTextBreak
{
    internal static void PrepareMemory()
    {
        NativeAllocation.Prepare();
        try { Check(InstallMemory(&AllocateMemory, &ReallocateMemory, &FreeMemory)); }
        catch (EntryPointNotFoundException error)
        { throw new PlatformNotSupportedException("Text allocation accounting requires the matching source-built native bundle.", error); }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* AllocateMemory(void* context, nuint size)
    {
        try { return NativeAllocation.Alloc(size); } catch (OutOfMemoryException) { return null; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* ReallocateMemory(void* context, void* value, nuint size)
    {
        try { return NativeAllocation.Realloc(value, size); } catch (OutOfMemoryException) { return null; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void FreeMemory(void* context, void* value) => NativeAllocation.Free(value);
    [LibraryImport(Library, EntryPoint = "e2d_text_memory"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int InstallMemory(delegate* unmanaged[Cdecl]<void*, nuint, void*> allocate,
        delegate* unmanaged[Cdecl]<void*, void*, nuint, void*> resize, delegate* unmanaged[Cdecl]<void*, void*, void> release);
}
