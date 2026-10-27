using System.Buffers.Binary;

namespace Electron2D;

/// <summary>Encodes and decodes caller-owned binary spans without introducing a collection type.</summary>
/// <remarks>Numeric values use little-endian byte order and IEEE floating-point storage. Reads and writes
/// accept unaligned offsets. Invalid ranges throw before mutation. Scalar and byte-swap operations allocate
/// no managed storage; strings, arrays and compressed snapshots are allocating preparation operations.</remarks>
public static partial class ByteBuffer
{
    /// <summary>Reads a 8-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static sbyte DecodeS8(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 1); return unchecked((sbyte)data[byteOffset]); }

    /// <summary>Writes a 8-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeS8(Span<byte> data, int byteOffset, sbyte value) { Range(data.Length, byteOffset, 1); data[byteOffset] = unchecked((byte)value); }

    /// <summary>Reads a 8-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static byte DecodeU8(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 1); return data[byteOffset]; }

    /// <summary>Writes a 8-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeU8(Span<byte> data, int byteOffset, byte value) { Range(data.Length, byteOffset, 1); data[byteOffset] = value; }

    /// <summary>Reads a 16-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static short DecodeS16(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 2); return BinaryPrimitives.ReadInt16LittleEndian(data.Slice(byteOffset, 2)); }

    /// <summary>Writes a 16-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeS16(Span<byte> data, int byteOffset, short value) { Range(data.Length, byteOffset, 2); BinaryPrimitives.WriteInt16LittleEndian(data.Slice(byteOffset, 2), value); }

    /// <summary>Reads a 16-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static ushort DecodeU16(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 2); return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(byteOffset, 2)); }

    /// <summary>Writes a 16-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeU16(Span<byte> data, int byteOffset, ushort value) { Range(data.Length, byteOffset, 2); BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(byteOffset, 2), value); }

    /// <summary>Reads a 32-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static int DecodeS32(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 4); return BinaryPrimitives.ReadInt32LittleEndian(data.Slice(byteOffset, 4)); }

    /// <summary>Writes a 32-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeS32(Span<byte> data, int byteOffset, int value) { Range(data.Length, byteOffset, 4); BinaryPrimitives.WriteInt32LittleEndian(data.Slice(byteOffset, 4), value); }

    /// <summary>Reads a 32-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static uint DecodeU32(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 4); return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(byteOffset, 4)); }

    /// <summary>Writes a 32-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeU32(Span<byte> data, int byteOffset, uint value) { Range(data.Length, byteOffset, 4); BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(byteOffset, 4), value); }

    /// <summary>Reads a 64-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static long DecodeS64(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 8); return BinaryPrimitives.ReadInt64LittleEndian(data.Slice(byteOffset, 8)); }

    /// <summary>Writes a 64-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeS64(Span<byte> data, int byteOffset, long value) { Range(data.Length, byteOffset, 8); BinaryPrimitives.WriteInt64LittleEndian(data.Slice(byteOffset, 8), value); }

    /// <summary>Reads a 64-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static ulong DecodeU64(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 8); return BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(byteOffset, 8)); }

    /// <summary>Writes a 64-bit integer at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeU64(Span<byte> data, int byteOffset, ulong value) { Range(data.Length, byteOffset, 8); BinaryPrimitives.WriteUInt64LittleEndian(data.Slice(byteOffset, 8), value); }

    /// <summary>Reads a 16-bit IEEE floating-point value at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static float DecodeHalf(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 2); return (float)BinaryPrimitives.ReadHalfLittleEndian(data.Slice(byteOffset, 2)); }

    /// <summary>Writes a 16-bit IEEE floating-point value at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode; rounded to binary16.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeHalf(Span<byte> data, int byteOffset, float value) { Range(data.Length, byteOffset, 2); BinaryPrimitives.WriteHalfLittleEndian(data.Slice(byteOffset, 2), (Half)value); }

    /// <summary>Reads a 32-bit IEEE floating-point value at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static float DecodeFloat(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 4); return BinaryPrimitives.ReadSingleLittleEndian(data.Slice(byteOffset, 4)); }

    /// <summary>Writes a 32-bit IEEE floating-point value at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeFloat(Span<byte> data, int byteOffset, float value) { Range(data.Length, byteOffset, 4); BinaryPrimitives.WriteSingleLittleEndian(data.Slice(byteOffset, 4), value); }

    /// <summary>Reads a 64-bit IEEE floating-point value at a byte offset.</summary>
    /// <param name="data">Borrowed source bytes.</param><param name="byteOffset">First byte of the value.</param>
    /// <returns>The decoded value, including floating-point infinities and NaNs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span.</exception>
    public static double DecodeDouble(ReadOnlySpan<byte> data, int byteOffset) { Range(data.Length, byteOffset, 8); return BinaryPrimitives.ReadDoubleLittleEndian(data.Slice(byteOffset, 8)); }

    /// <summary>Writes a 64-bit IEEE floating-point value at a byte offset.</summary>
    /// <param name="data">Borrowed mutable destination.</param><param name="byteOffset">First byte of the value.</param><param name="value">Value to encode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The complete value lies outside the span; no bytes change.</exception>
    public static void EncodeDouble(Span<byte> data, int byteOffset, double value) { Range(data.Length, byteOffset, 8); BinaryPrimitives.WriteDoubleLittleEndian(data.Slice(byteOffset, 8), value); }

    /// <summary>Reverses bytes within complete 16-bit segments in place.</summary>
    /// <param name="data">Borrowed mutable bytes.</param><param name="offset">First segment byte, possibly unaligned.</param>
    /// <param name="count">Segment count; any negative value processes all complete remaining segments, preserving trailing bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">The selected range is invalid; no bytes change.</exception>
    public static void BSwap16(Span<byte> data, int offset = 0, int count = -1) => Swap(data, offset, count, 2);

    /// <summary>Reverses bytes within complete 32-bit segments in place.</summary>
    /// <param name="data">Borrowed mutable bytes.</param><param name="offset">First segment byte, possibly unaligned.</param>
    /// <param name="count">Segment count; any negative value processes all complete remaining segments, preserving trailing bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">The selected range is invalid; no bytes change.</exception>
    public static void BSwap32(Span<byte> data, int offset = 0, int count = -1) => Swap(data, offset, count, 4);

    /// <summary>Reverses bytes within complete 64-bit segments in place.</summary>
    /// <param name="data">Borrowed mutable bytes.</param><param name="offset">First segment byte, possibly unaligned.</param>
    /// <param name="count">Segment count; any negative value processes all complete remaining segments, preserving trailing bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">The selected range is invalid; no bytes change.</exception>
    public static void BSwap64(Span<byte> data, int offset = 0, int count = -1) => Swap(data, offset, count, 8);

    private static void Range(int length, int offset, int size)
    {
        if ((uint)offset > (uint)length || size > length - offset) throw new ArgumentOutOfRangeException(nameof(offset));
    }
    private static void Swap(Span<byte> data, int offset, int count, int size)
    {
        Range(data.Length, offset, 0);
        var available = (data.Length - offset) / size;
        if (count < 0) count = available;
        if (count > available) throw new ArgumentOutOfRangeException(nameof(count));
        var selected = data.Slice(offset, count * size);
        for (var i = 0; i < selected.Length; i += size) selected.Slice(i, size).Reverse();
    }
}
