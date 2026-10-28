using System.Runtime.InteropServices;

namespace Electron2D;

// Counts owned payload bytes at allocation boundaries; borrowed library pointers never enter this allocator.
internal static unsafe class NativeAllocation
{
    private const nuint HeaderBytes = 32;
    private struct Header { internal void* Allocation; internal nuint Size, Alignment; }
    private static readonly object Gate = new();
    private static long _current, _peak;

    internal static (long Current, long Peak) Snapshot() { lock (Gate) return (_current, _peak); }
    internal static void* Alloc(nuint size) => Allocate(size, false, 0);
    internal static void* AllocZeroed(nuint size) => Allocate(size, true, 0);
    internal static void* AlignedAlloc(nuint size, nuint alignment)
    {
        if (alignment == 0 || (alignment & (alignment - 1)) != 0) throw new ArgumentException("Alignment must be a power of two.", nameof(alignment));
        return Allocate(size, false, alignment);
    }
    private static void* Allocate(nuint size, bool zeroed, nuint alignment)
    {
        lock (Gate)
        {
            var prefix = alignment > HeaderBytes ? alignment : HeaderBytes;
            var bytes = Request(size, prefix, alignment, 0);
            var raw = alignment != 0 ? NativeMemory.AlignedAlloc(bytes, alignment) : zeroed ? NativeMemory.AllocZeroed(bytes) : NativeMemory.Alloc(bytes);
            var value = (byte*)raw + prefix;
            var header = (Header*)(value - HeaderBytes);
            *header = new() { Allocation = raw, Size = size, Alignment = alignment };
            Publish((long)size);
            return value;
        }
    }
    internal static void* Realloc(void* value, nuint size)
    {
        if (value is null) return Alloc(size);
        lock (Gate)
        {
            var header = (Header*)((byte*)value - HeaderBytes);
            if (header->Alignment != 0) throw new ArgumentException("An aligned block cannot use ordinary reallocation.", nameof(value));
            var previous = header->Size;
            var bytes = Request(size, HeaderBytes, 0, previous);
            var raw = NativeMemory.Realloc(header->Allocation, bytes);
            var result = (byte*)raw + HeaderBytes;
            header = (Header*)(result - HeaderBytes);
            *header = new() { Allocation = raw, Size = size };
            Publish((long)size - (long)previous);
            return result;
        }
    }
    internal static void Free(void* value)
    {
        if (value is null) return;
        lock (Gate)
        {
            var header = (Header*)((byte*)value - HeaderBytes);
            var size = header->Size;
            if (header->Alignment == 0) NativeMemory.Free(header->Allocation); else NativeMemory.AlignedFree(header->Allocation);
            Publish(-(long)size);
        }
    }
    internal static void AlignedFree(void* value) => Free(value);
    private static nuint Request(nuint size, nuint prefix, nuint alignment, nuint previous)
    {
        if ((ulong)size > (ulong)(long.MaxValue - (_current - (long)previous)) || size > nuint.MaxValue - prefix) throw new OutOfMemoryException();
        var bytes = size + prefix;
        if (alignment > 1)
        {
            if (bytes > nuint.MaxValue - (alignment - 1)) throw new OutOfMemoryException();
            bytes = (bytes + alignment - 1) & ~(alignment - 1);
        }
        return bytes;
    }
    private static void Publish(long delta) { _current += delta; if (_current > _peak) _peak = _current; }
}
