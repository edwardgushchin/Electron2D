namespace Electron2D;

public static partial class ByteBuffer
{
    /// <summary>Compresses bytes into the versioned FileAccess compressed container.</summary>
    /// <param name="data">Borrowed source bytes; empty input returns an empty array.</param>
    /// <param name="compressionMode">Codec identity stored in the container.</param>
    /// <returns>A caller-owned compressed snapshot, readable by FileAccess.OpenCompressed.</returns>
    /// <remarks>All five codecs share the existing file envelope; native codecs retain checksummed 64 KiB blocks.
    /// This is asset preparation, with allocations and no external raw-codec format promise.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The codec is invalid.</exception>
    /// <exception cref="PlatformNotSupportedException">The native codec bundle is unavailable.</exception>
    public static byte[] Compress(ReadOnlySpan<byte> data, FileCompressionMode compressionMode = FileCompressionMode.FastLz)
    {
        ValidateCodec(compressionMode);
        return data.IsEmpty ? [] : FileAccess.EncodeCompressed(data, compressionMode);
    }

    /// <summary>Decompresses a compressed snapshot subject to a caller-supplied output capacity.</summary>
    /// <param name="data">Borrowed versioned compressed container.</param><param name="bufferSize">Positive maximum decoded byte count.</param>
    /// <param name="compressionMode">Required codec identity.</param><returns>A new array containing the actual decoded bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The capacity or codec is invalid.</exception>
    /// <exception cref="InvalidDataException">The container is invalid, corrupt, mismatched or exceeds the capacity.</exception>
    /// <exception cref="PlatformNotSupportedException">The native codec bundle is unavailable.</exception>
    public static byte[] Decompress(ReadOnlySpan<byte> data, int bufferSize, FileCompressionMode compressionMode = FileCompressionMode.FastLz)
    {
        ValidateCodec(compressionMode);
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
        return FileAccess.DecodeCompressed(data, compressionMode, bufferSize);
    }

    /// <summary>Decompresses DEFLATE, GZip or Brotli without requiring the exact decoded size.</summary>
    /// <param name="data">Borrowed versioned compressed container.</param>
    /// <param name="maxOutputSize">Positive output limit; -1 permits the managed array limit. Zero and smaller negatives reject.</param>
    /// <param name="compressionMode">DEFLATE, GZip or Brotli. Native block codecs require Decompress instead.</param>
    /// <returns>A new decoded snapshot; the stored length is checked before output allocation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The limit or codec is unsupported.</exception>
    /// <exception cref="InvalidDataException">The container is invalid, corrupt, mismatched or exceeds the limit.</exception>
    public static byte[] DecompressDynamic(ReadOnlySpan<byte> data, int maxOutputSize, FileCompressionMode compressionMode = FileCompressionMode.FastLz)
    {
        if (compressionMode is not (FileCompressionMode.Deflate or FileCompressionMode.Gzip or FileCompressionMode.Brotli)) throw new ArgumentOutOfRangeException(nameof(compressionMode));
        if (maxOutputSize != -1 && maxOutputSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxOutputSize));
        return FileAccess.DecodeCompressed(data, compressionMode, maxOutputSize == -1 ? Array.MaxLength : maxOutputSize);
    }

    private static void ValidateCodec(FileCompressionMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
    }
}
