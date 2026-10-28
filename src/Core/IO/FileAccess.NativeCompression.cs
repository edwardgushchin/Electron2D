using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Electron2D;

public sealed partial class FileAccess
{
    private const int NativeBlockSize = 65536;
    private static bool IsNativeCodec(FileCompressionMode mode) => mode is FileCompressionMode.FastLz or FileCompressionMode.Zstandard;

    private static unsafe int NativeEncodeBound(FileCompressionMode mode, int length)
    {
        try
        {
            var bound = FileCodecNative.Encode((int)mode, null, length, null, 0);
            if (bound <= 0 || bound > NativeBlockSize * 2 + 64) throw new InvalidDataException("The native codec returned an invalid block bound.");
            return bound;
        }
        catch (EntryPointNotFoundException error) { throw new PlatformNotSupportedException("The packaged native bridge does not provide file codecs; use the matching source-built native bundle.", error); }
        catch (DllNotFoundException error) { throw new PlatformNotSupportedException("File codecs require a packaged native bridge for this target.", error); }
    }
    private static unsafe byte[] EncodeNativeBlocks(ReadOnlySpan<byte> data, FileCompressionMode mode)
    {
        if (data.IsEmpty) return [];
        var encoded = new byte[NativeEncodeBound(mode, NativeBlockSize)];
        using var output = new MemoryStream();
        Span<byte> header = stackalloc byte[4]; Span<byte> checksum = stackalloc byte[32];
        for (var offset = 0; offset < data.Length; offset += NativeBlockSize)
        {
            var block = data.Slice(offset, Math.Min(NativeBlockSize, data.Length - offset));
            int count;
            fixed (byte* inputPointer = block) fixed (byte* outputPointer = encoded)
                count = FileCodecNative.Encode((int)mode, inputPointer, block.Length, outputPointer, encoded.Length);
            if (count <= 0 || count > encoded.Length) throw new IOException("The native file codec could not encode the block.");
            BinaryPrimitives.WriteInt32LittleEndian(header, count); output.Write(header);
            SHA256.HashData(block, checksum); output.Write(checksum); output.Write(encoded, 0, count);
        }
        return output.ToArray();
    }
    private static unsafe byte[] DecodeNativeBlocks(ReadOnlySpan<byte> payload, int expectedLength, FileCompressionMode mode)
    {
        var offset = 0; var remaining = expectedLength;
        while (remaining > 0)
        {
            if (payload.Length - offset < 36) throw new InvalidDataException("The compressed block header is truncated.");
            var count = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(offset, 4));
            if (count <= 0 || count > NativeEncodeBound(mode, Math.Min(remaining, NativeBlockSize)) || count > payload.Length - offset - 36)
                throw new InvalidDataException("The compressed block length is invalid.");
            offset += count + 36; remaining -= Math.Min(remaining, NativeBlockSize);
        }
        if (offset != payload.Length) throw new InvalidDataException("The compressed file contains trailing blocks.");
        var result = new byte[expectedLength]; var padded = new byte[16];
        Span<byte> checksum = stackalloc byte[32]; offset = 0;
        for (var destination = 0; destination < result.Length; destination += NativeBlockSize)
        {
            var size = Math.Min(NativeBlockSize, result.Length - destination);
            var count = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(offset, 4));
            var expectedChecksum = payload.Slice(offset + 4, 32); var encoded = payload.Slice(offset + 36, count);
            var block = result.AsSpan(destination, size);
            var temporary = mode == FileCompressionMode.FastLz && size < 16;
            var output = temporary ? padded.AsSpan() : block;
            int decoded;
            fixed (byte* inputPointer = encoded) fixed (byte* outputPointer = output)
                decoded = FileCodecNative.Decode((int)mode, inputPointer, encoded.Length, outputPointer, output.Length);
            if (decoded != output.Length) throw new InvalidDataException("The native decoded block length is invalid.");
            if (temporary)
            {
                if (padded.AsSpan(size).IndexOfAnyExcept((byte)0) >= 0) throw new InvalidDataException("The native block padding is invalid.");
                padded.AsSpan(0, size).CopyTo(block);
            }
            SHA256.HashData(block, checksum);
            if (!CryptographicOperations.FixedTimeEquals(checksum, expectedChecksum)) throw new InvalidDataException("The compressed block checksum is invalid.");
            offset += count + 36;
        }
        return result;
    }
}
internal static unsafe partial class FileCodecNative
{
    [LibraryImport(NativeLibraries.ENetLibrary, EntryPoint = "e2d_file_encode"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int EncodeCore(int mode, byte* input, int length, byte* output, int capacity);
    internal static int Encode(int mode, byte* input, int length, byte* output, int capacity)
    { ENetNative.PrepareMemory(); return EncodeCore(mode, input, length, output, capacity); }
    [LibraryImport(NativeLibraries.ENetLibrary, EntryPoint = "e2d_file_decode"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int DecodeCore(int mode, byte* input, int length, byte* output, int capacity);
    internal static int Decode(int mode, byte* input, int length, byte* output, int capacity)
    { ENetNative.PrepareMemory(); return DecodeCore(mode, input, length, output, capacity); }
}
