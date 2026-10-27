using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RenderMemorySnapshot(ulong textures, ulong buffers, ulong total)
{
    internal readonly ulong Textures = textures, Buffers = buffers, Total = total;
    internal long Select(RenderingDevice.MemoryType type) => checked((long)(type switch
    {
        RenderingDevice.MemoryType.Textures => Textures,
        RenderingDevice.MemoryType.Buffers => Buffers,
        RenderingDevice.MemoryType.Total => Total,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    }));
}

internal static partial class RenderMemory
{
    internal static RenderMemorySnapshot Read(nint device)
    {
        var result = ReadNative(device, 1, out var memory);
        if (result == 0) throw new NotSupportedException("Native render memory queries require the pinned SDL 3.4.18 Vulkan adapter.");
        if (result != 1) throw new InvalidOperationException("The native render allocator returned an inconsistent memory snapshot.");
        return memory;
    }

    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_memory")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int ReadNative(nint device, uint abi, out RenderMemorySnapshot memory);
}

internal sealed class RenderTextureMemory
{
    private long _bytes;
    internal void Add(long bytes) => Interlocked.Add(ref _bytes, bytes);
    internal RenderMemorySnapshot Read()
    {
        var bytes = checked((ulong)Interlocked.Read(ref _bytes));
        return new(bytes, 0, bytes);
    }
}
