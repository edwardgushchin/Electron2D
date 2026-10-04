using System.Runtime.InteropServices;
namespace Electron2D;

/// <summary>Incrementally compresses or decompresses gzip and zlib-wrapped deflate bytes through bounded output storage.</summary>
/// <remarks>Input and output spans are borrowed only during a call. Drain output when partial writes stop progressing.
/// Native codec state and output capacity are prepared on Start. Calls and disposal require the constructing thread.</remarks>
public class StreamPeerGZIP : StreamPeer
{
    private GZIPContext? _context;
    private byte[] _bytes = [];
    private int _head, _count;
    /// <summary>Creates an unconfigured compression stream.</summary>
    public StreamPeerGZIP() { }
    /// <summary>Starts compression with a fresh bounded output buffer.</summary><param name="useDeflate">True selects zlib-wrapped deflate; false selects gzip.</param><param name="bufferSize">Positive prepared capacity up to 64 MiB, rounded to a power of two with one reserved byte.</param>
    public void StartCompression(bool useDeflate = false, int bufferSize = 65535) => Start(true, useDeflate, bufferSize);
    /// <summary>Starts decompression with a fresh bounded output buffer.</summary><param name="useDeflate">True selects zlib-wrapped deflate; false selects gzip, including concatenated members.</param><param name="bufferSize">Positive prepared capacity up to 64 MiB.</param>
    public void StartDecompression(bool useDeflate = false, int bufferSize = 65535) => Start(false, useDeflate, bufferSize);
    private void Start(bool compress, bool deflate, int size)
    {
        CheckStream(); if (_context is not null) throw new InvalidOperationException("Clear the active codec before restarting.");
        if (size <= 0 || size > 64 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(size));
        var capacity = 2; while (capacity < size) capacity *= 2; var bytes = new byte[capacity]; var context = new GZIPContext(compress, deflate); _bytes = bytes; _context = context; _head = _count = 0;
    }
    /// <summary>Reports whether the compression finish or decompression member boundary was reached.</summary><returns>False before configuration or while a member remains incomplete.</returns>
    public bool IsFinished() { CheckStream(); return _context?.Finished ?? false; }
    /// <summary>Finishes a compression stream, retaining output for reading.</summary>
    /// <exception cref="InvalidOperationException">The stream is not compressing, or output must be drained before retrying Finish.</exception>
    public void Finish()
    {
        CheckStream(); var context = _context ?? throw new InvalidOperationException("No codec is active."); if (!context.Compress) throw new InvalidOperationException("Finish requires compression.");
        while (!context.Finished) { if (OutputSpace().IsEmpty) throw new InvalidOperationException("Drain compressed output and retry Finish."); var before = _count; _ = Process(ReadOnlySpan<byte>.Empty, true); if (!_context!.Finished && before == _count) throw new InvalidOperationException("Drain compressed output and retry Finish."); }
    }
    /// <summary>Releases codec state and discards queued output.</summary>
    public void Clear() { CheckStream(); _context?.Dispose(); _context = null; _bytes = []; _head = _count = 0; }
    /// <inheritdoc />
    public override int GetAvailableBytes() { CheckStream(); return _count; }
    private Span<byte> OutputSpace() { if (_bytes.Length == 0 || _count == _bytes.Length - 1) return Span<byte>.Empty; var tail = (_head + _count) % _bytes.Length; return _bytes.AsSpan(tail, Math.Min(_bytes.Length - 1 - _count, _bytes.Length - tail)); }
    private int Process(ReadOnlySpan<byte> input, bool finish)
    {
        var context = _context!; var output = OutputSpace(); var consumed = context.Process(input, output, finish, out var written); _count += written; return consumed;
    }
    /// <inheritdoc />
    protected override int WriteCore(ReadOnlySpan<byte> data, bool block)
    {
        var context = _context ?? throw new InvalidOperationException("No codec is active."); if (context.Compress && context.Finished) throw new InvalidOperationException("Compression is finished.");
        var total = 0; while (!data.IsEmpty && !OutputSpace().IsEmpty) { var before = _count; var consumed = Process(data, false); total += consumed; data = data[consumed..]; if (consumed == 0 && before == _count) break; }
        return total;
    }
    /// <inheritdoc />
    protected override int ReadCore(Span<byte> destination, bool block)
    {
        if (_context is not null && !_context.Compress && !_context.Finished && !OutputSpace().IsEmpty) _ = Process(ReadOnlySpan<byte>.Empty, false);
        var count = Math.Min(destination.Length, _count); var first = Math.Min(count, _bytes.Length - _head); _bytes.AsSpan(_head, first).CopyTo(destination); if (count > first) _bytes.AsSpan(0, count - first).CopyTo(destination[first..]);
        if (_bytes.Length != 0) _head = (_head + count) % _bytes.Length; _count -= count; return count;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) Clear(); base.Dispose(disposing); }
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GZIPState
{
    internal byte* Input;
    internal byte* Output;
    internal nint Message, State;
    internal uint InputLeft, OutputLeft;
}

internal sealed unsafe class GZIPContext : SafeHandle
{
    internal readonly bool Compress;
    private readonly int _window;
    internal bool Finished { get; private set; }
    internal GZIPContext(bool compress, bool deflate) : base(0, true)
    {
        Compress = compress; _window = deflate ? 15 : 31;
        var pointer = (GZIPState*)NativeMemory.AllocZeroed((nuint)sizeof(GZIPState));
        try { var result = compress ? GZIPNative.StartCompression(pointer, -1, 8, _window, 8, 0) : GZIPNative.StartDecompression(pointer, _window); if (result != 0) throw new InvalidDataException("Native compression initialization failed: " + result); SetHandle((nint)pointer); }
        catch { NativeMemory.Free(pointer); throw; }
    }
    public override bool IsInvalid => handle == 0;
    internal int Process(ReadOnlySpan<byte> input, Span<byte> output, bool finish, out int written)
    {
        var state = (GZIPState*)handle;
        if (!Compress && Finished && !input.IsEmpty) { if (_window != 31) throw new InvalidDataException("Trailing zlib data."); if (GZIPNative.Reset(state, _window) != 0) throw new InvalidDataException("Gzip member reset failed."); Finished = false; }
        fixed (byte* source = input) fixed (byte* destination = output)
        {
            state->Input = source; state->InputLeft = (uint)input.Length; state->Output = destination; state->OutputLeft = (uint)output.Length;
            var result = Compress ? GZIPNative.Compress(state, finish ? 4 : 0) : GZIPNative.Decompress(state, 0);
            written = output.Length - (int)state->OutputLeft; var consumed = input.Length - (int)state->InputLeft;
            state->Input = state->Output = null; state->InputLeft = state->OutputLeft = 0;
            if (result is not (0 or 1 or -5)) throw new InvalidDataException("Compressed stream validation failed: " + result); Finished = result == 1; return consumed;
        }
    }
    protected override bool ReleaseHandle() { var state = (GZIPState*)handle; if (Compress) GZIPNative.EndCompression(state); else GZIPNative.EndDecompression(state); NativeMemory.Free(state); return true; }
}

internal static unsafe partial class GZIPNative
{
    private const string Library = "System.IO.Compression.Native";
    [LibraryImport(Library, EntryPoint = "CompressionNative_DeflateInit2_")] internal static partial int StartCompression(GZIPState* stream, int level, int method, int windowBits, int memoryLevel, int strategy);
    [LibraryImport(Library, EntryPoint = "CompressionNative_InflateInit2_")] internal static partial int StartDecompression(GZIPState* stream, int windowBits);
    [LibraryImport(Library, EntryPoint = "CompressionNative_Deflate")] internal static partial int Compress(GZIPState* stream, int flush);
    [LibraryImport(Library, EntryPoint = "CompressionNative_Inflate")] internal static partial int Decompress(GZIPState* stream, int flush);
    [LibraryImport(Library, EntryPoint = "CompressionNative_DeflateEnd")] internal static partial int EndCompression(GZIPState* stream);
    [LibraryImport(Library, EntryPoint = "CompressionNative_InflateEnd")] internal static partial int EndDecompression(GZIPState* stream);
    [LibraryImport(Library, EntryPoint = "CompressionNative_InflateReset2_")] internal static partial int Reset(GZIPState* stream, int windowBits);
}
