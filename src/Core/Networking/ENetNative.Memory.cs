using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe partial class ENetNative
{
    private static bool _memoryReady;
    internal static void PrepareMemory()
    {
        lock (Gate)
        {
            if (_memoryReady) return;
            try
            {
                if (MemoryCallbacks(&AllocateMemory, &FreeMemory) != 0)
                    throw new InvalidOperationException("The native ENet allocator has already been assigned to another owner.");
            }
            catch (EntryPointNotFoundException error)
            { throw new PlatformNotSupportedException("ENet allocation accounting requires the matching source-built native bundle.", error); }
            _memoryReady = true;
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void* AllocateMemory(nuint size)
    {
        try { return NativeAllocation.Alloc(size); }
        catch (OutOfMemoryException) { return null; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void FreeMemory(void* value) => NativeAllocation.Free(value);
    [LibraryImport(Library, EntryPoint = "e2d_enet_memory"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int MemoryCallbacks(delegate* unmanaged[Cdecl]<nuint, void*> allocate, delegate* unmanaged[Cdecl]<void*, void> release);
}
