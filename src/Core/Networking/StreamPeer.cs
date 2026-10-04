using System.Buffers;
using System.Buffers.Binary;
using System.Text;
namespace Electron2D;

/// <summary>Reads and writes ordered bytes, endian-aware numbers and length-prefixed strings.</summary>
/// <remarks>Instances belong to their constructing thread. Full operations consume the requested byte count
/// or throw; partial operations report progress without waiting. Returned arrays are caller-owned. Numeric
/// and caller-span operations do not allocate managed buffers. Dynamic values have no wire representation here.</remarks>
public abstract class StreamPeer : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private static readonly Encoding ASCII = Encoding.GetEncoding(20127, new EncoderReplacementFallback(" "), DecoderFallback.ReplacementFallback);
    private bool _bigEndian;
    private int _maxStringBytes = 16 * 1024 * 1024;
    /// <summary>Creates an ordered byte stream owned by the current thread.</summary>
    protected StreamPeer() { }
    /// <summary>Gets or sets number and string-length byte order.</summary><value>False uses little endian initially.</value>
    public bool BigEndian { get { CheckStream(); return _bigEndian; } set { CheckStream(); _bigEndian = value; } }
    /// <summary>Gets or sets the allocation limit for incoming string bytes.</summary><value>16 MiB initially; nonnegative.</value>
    /// <exception cref="ArgumentOutOfRangeException">The limit is negative.</exception>
    public int MaxStringBytes { get { CheckStream(); return _maxStringBytes; } set { CheckStream(); ArgumentOutOfRangeException.ThrowIfNegative(value); _maxStringBytes = value; } }
    /// <summary>Checks lifetime and constructing-thread ownership.</summary>
    /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the stream owner.</exception>
    protected void CheckStream() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("Stream access requires its constructing thread."); }
    /// <summary>Reports immediately readable bytes without consuming them.</summary><returns>A nonnegative count.</returns>
    public abstract int GetAvailableBytes();
    /// <summary>Reads exactly the requested bytes into a caller-owned span.</summary><param name="destination">The destination.</param>
    /// <exception cref="EndOfStreamException">The stream ends before filling it; a prefix may have been consumed.</exception>
    public void GetData(Span<byte> destination)
    {
        CheckStream(); if (destination.IsEmpty) { _ = ReadCore(destination, true); return; } while (!destination.IsEmpty) { var read = ReadCore(destination, true); if (read <= 0) throw new EndOfStreamException(); destination = destination[read..]; }
    }
    /// <summary>Reads exactly a requested number of bytes into a new array.</summary><param name="bytes">Nonnegative byte count.</param><returns>A caller-owned array.</returns>
    public byte[] GetData(int bytes) { CheckStream(); ArgumentOutOfRangeException.ThrowIfNegative(bytes); var data = new byte[bytes]; GetData(data.AsSpan()); return data; }
    /// <summary>Reads an available prefix without waiting.</summary><param name="destination">The caller-owned destination.</param><returns>Bytes read; zero when no progress is possible.</returns>
    public int GetPartialData(Span<byte> destination) { CheckStream(); return ReadCore(destination, false); }
    /// <summary>Reads up to a requested number of immediately available bytes.</summary><param name="bytes">Nonnegative maximum.</param><returns>A caller-owned array containing the received prefix.</returns>
    public byte[] GetPartialData(int bytes) { CheckStream(); ArgumentOutOfRangeException.ThrowIfNegative(bytes); var data = new byte[bytes]; var count = GetPartialData(data.AsSpan()); if (count != bytes) Array.Resize(ref data, count); return data; }
    /// <summary>Writes every supplied byte, waiting for progress when required.</summary><param name="data">The bytes to send; never retained.</param>
    public void PutData(ReadOnlySpan<byte> data) { CheckStream(); while (!data.IsEmpty) { var count = WriteCore(data, true); if (count <= 0) throw new IOException("Stream made no write progress."); data = data[count..]; } }
    /// <summary>Writes an available prefix without waiting.</summary><param name="data">The bytes to send; never retained.</param><returns>The byte count written.</returns>
    public int PutPartialData(ReadOnlySpan<byte> data) { CheckStream(); return data.IsEmpty ? 0 : WriteCore(data, false); }
    /// <summary>Reads a prefix for the concrete transport.</summary><param name="destination">The destination.</param><param name="block">Whether waiting is permitted.</param><returns>The received byte count.</returns>
    protected abstract int ReadCore(Span<byte> destination, bool block);
    /// <summary>Writes a prefix for the concrete transport.</summary><param name="data">The source.</param><param name="block">Whether waiting is permitted.</param><returns>The sent byte count.</returns>
    protected abstract int WriteCore(ReadOnlySpan<byte> data, bool block);
    /// <summary>Reads one 8-bit signed value in BigEndian order.</summary><returns>The decoded value.</returns>
    public sbyte Get8() { Span<byte> data = stackalloc byte[1]; GetData(data); return (sbyte)data[0]; }
    /// <summary>Writes one 8-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void Put8(sbyte value) { Span<byte> data = stackalloc byte[1]; data[0] = unchecked((byte)value); PutData(data); }
    /// <summary>Reads one 8-bit unsigned value in BigEndian order.</summary><returns>The decoded value.</returns>
    public byte GetU8() { Span<byte> data = stackalloc byte[1]; GetData(data); return (byte)data[0]; }
    /// <summary>Writes one 8-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void PutU8(byte value) { Span<byte> data = stackalloc byte[1]; data[0] = unchecked((byte)value); PutData(data); }
    /// <summary>Reads one 16-bit signed value in BigEndian order.</summary><returns>The decoded value.</returns>
    public short Get16() { Span<byte> data = stackalloc byte[2]; GetData(data); return (_bigEndian ? BinaryPrimitives.ReadInt16BigEndian(data) : BinaryPrimitives.ReadInt16LittleEndian(data)); }
    /// <summary>Writes one 16-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void Put16(short value) { Span<byte> data = stackalloc byte[2]; if (BigEndian) BinaryPrimitives.WriteInt16BigEndian(data, value); else BinaryPrimitives.WriteInt16LittleEndian(data, value); PutData(data); }
    /// <summary>Reads one 16-bit unsigned value in BigEndian order.</summary><returns>The decoded value.</returns>
    public ushort GetU16() { Span<byte> data = stackalloc byte[2]; GetData(data); return (_bigEndian ? BinaryPrimitives.ReadUInt16BigEndian(data) : BinaryPrimitives.ReadUInt16LittleEndian(data)); }
    /// <summary>Writes one 16-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void PutU16(ushort value) { Span<byte> data = stackalloc byte[2]; if (BigEndian) BinaryPrimitives.WriteUInt16BigEndian(data, value); else BinaryPrimitives.WriteUInt16LittleEndian(data, value); PutData(data); }
    /// <summary>Reads one 32-bit signed value in BigEndian order.</summary><returns>The decoded value.</returns>
    public int Get32() { Span<byte> data = stackalloc byte[4]; GetData(data); return (_bigEndian ? BinaryPrimitives.ReadInt32BigEndian(data) : BinaryPrimitives.ReadInt32LittleEndian(data)); }
    /// <summary>Writes one 32-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void Put32(int value) { Span<byte> data = stackalloc byte[4]; if (BigEndian) BinaryPrimitives.WriteInt32BigEndian(data, value); else BinaryPrimitives.WriteInt32LittleEndian(data, value); PutData(data); }
    /// <summary>Reads one 32-bit unsigned value in BigEndian order.</summary><returns>The decoded value.</returns>
    public uint GetU32() { Span<byte> data = stackalloc byte[4]; GetData(data); return (_bigEndian ? BinaryPrimitives.ReadUInt32BigEndian(data) : BinaryPrimitives.ReadUInt32LittleEndian(data)); }
    /// <summary>Writes one 32-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void PutU32(uint value) { Span<byte> data = stackalloc byte[4]; if (BigEndian) BinaryPrimitives.WriteUInt32BigEndian(data, value); else BinaryPrimitives.WriteUInt32LittleEndian(data, value); PutData(data); }
    /// <summary>Reads one 64-bit signed value in BigEndian order.</summary><returns>The decoded value.</returns>
    public long Get64() { Span<byte> data = stackalloc byte[8]; GetData(data); return (_bigEndian ? BinaryPrimitives.ReadInt64BigEndian(data) : BinaryPrimitives.ReadInt64LittleEndian(data)); }
    /// <summary>Writes one 64-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void Put64(long value) { Span<byte> data = stackalloc byte[8]; if (BigEndian) BinaryPrimitives.WriteInt64BigEndian(data, value); else BinaryPrimitives.WriteInt64LittleEndian(data, value); PutData(data); }
    /// <summary>Reads one 64-bit unsigned value in BigEndian order.</summary><returns>The decoded value.</returns>
    public ulong GetU64() { Span<byte> data = stackalloc byte[8]; GetData(data); return (_bigEndian ? BinaryPrimitives.ReadUInt64BigEndian(data) : BinaryPrimitives.ReadUInt64LittleEndian(data)); }
    /// <summary>Writes one 64-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void PutU64(ulong value) { Span<byte> data = stackalloc byte[8]; if (BigEndian) BinaryPrimitives.WriteUInt64BigEndian(data, value); else BinaryPrimitives.WriteUInt64LittleEndian(data, value); PutData(data); }
    /// <summary>Reads one 32-bit IEEE value in BigEndian order.</summary><returns>The decoded value.</returns>
    public float GetFloat() { Span<byte> data = stackalloc byte[4]; GetData(data); return (_bigEndian ? BinaryPrimitives.ReadSingleBigEndian(data) : BinaryPrimitives.ReadSingleLittleEndian(data)); }
    /// <summary>Writes one 32-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void PutFloat(float value) { Span<byte> data = stackalloc byte[4]; if (BigEndian) BinaryPrimitives.WriteSingleBigEndian(data, value); else BinaryPrimitives.WriteSingleLittleEndian(data, value); PutData(data); }
    /// <summary>Reads one 64-bit IEEE value in BigEndian order.</summary><returns>The decoded value.</returns>
    public double GetDouble() { Span<byte> data = stackalloc byte[8]; GetData(data); return (_bigEndian ? BinaryPrimitives.ReadDoubleBigEndian(data) : BinaryPrimitives.ReadDoubleLittleEndian(data)); }
    /// <summary>Writes one 64-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void PutDouble(double value) { Span<byte> data = stackalloc byte[8]; if (BigEndian) BinaryPrimitives.WriteDoubleBigEndian(data, value); else BinaryPrimitives.WriteDoubleLittleEndian(data, value); PutData(data); }
    /// <summary>Reads one 16-bit IEEE value in BigEndian order.</summary><returns>The decoded value.</returns>
    public float GetHalf() { Span<byte> data = stackalloc byte[2]; GetData(data); return (float)(_bigEndian ? BinaryPrimitives.ReadHalfBigEndian(data) : BinaryPrimitives.ReadHalfLittleEndian(data)); }
    /// <summary>Writes one 16-bit value in BigEndian order.</summary><param name="value">The value to encode.</param>
    public void PutHalf(float value) { Span<byte> data = stackalloc byte[2]; if (BigEndian) BinaryPrimitives.WriteHalfBigEndian(data, (Half)value); else BinaryPrimitives.WriteHalfLittleEndian(data, (Half)value); PutData(data); }
    /// <summary>Writes ASCII bytes prefixed by their unsigned 32-bit length.</summary><param name="value">Text; unrepresentable scalars become spaces.</param>
    public void PutString(string value) => PutEncoded(value, ASCII);
    /// <summary>Writes UTF-8 bytes prefixed by their unsigned 32-bit length.</summary><param name="value">Text to encode.</param>
    public void PutUTF8String(string value) => PutEncoded(value, Encoding.UTF8);
    private void PutEncoded(string value, Encoding encoding) { CheckStream(); ArgumentNullException.ThrowIfNull(value); var data = encoding.GetBytes(value); PutU32((uint)data.Length); PutData(data); }
    /// <summary>Reads Latin-1 text, terminating at the first NUL.</summary><param name="bytes">Explicit byte count; negative reads a signed 32-bit length prefix.</param><returns>The decoded string.</returns>
    /// <exception cref="InvalidDataException">The encoded length is negative or exceeds MaxStringBytes.</exception>
    public string GetString(int bytes = -1) { var data = GetStringBytes(bytes); var zero = Array.IndexOf(data, (byte)0); return Encoding.Latin1.GetString(data, 0, zero < 0 ? data.Length : zero); }
    /// <summary>Reads UTF-8 text, skipping a leading BOM, stopping at NUL and replacing malformed bytes.</summary><param name="bytes">Explicit byte count; negative reads a signed 32-bit length prefix.</param><returns>The decoded string.</returns>
    /// <exception cref="InvalidDataException">The encoded length is negative or exceeds MaxStringBytes.</exception>
    public string GetUTF8String(int bytes = -1)
    {
        var data = GetStringBytes(bytes).AsSpan(); if (data.Length >= 3 && data[0] == 0xef && data[1] == 0xbb && data[2] == 0xbf) data = data[3..];
        var zero = data.IndexOf((byte)0); if (zero >= 0) data = data[..zero];
        var result = new StringBuilder(data.Length); Span<char> chars = stackalloc char[2];
        while (!data.IsEmpty) { var status = Rune.DecodeFromUtf8(data, out var rune, out var consumed); if (status != OperationStatus.Done) { rune = Rune.ReplacementChar; consumed = 1; } var count = rune.EncodeToUtf16(chars); result.Append(chars[..count]); data = data[consumed..]; }
        return result.ToString();
    }
    private byte[] GetStringBytes(int bytes) { CheckStream(); if (bytes < 0) bytes = Get32(); if (bytes < 0 || bytes > _maxStringBytes) throw new InvalidDataException("String byte length exceeds the configured allocation limit."); return GetData(bytes); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); CheckStream(); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<StreamPeer, bool>(nameof(BigEndian), s => s.BigEndian, (s, v) => s.BigEndian = v, _ => false, stored: true);
        yield return new PropertyDescriptor<StreamPeer, int>(nameof(MaxStringBytes), s => s.MaxStringBytes, (s, v) => s.MaxStringBytes = v, _ => 16 * 1024 * 1024, stored: true);
    }
}
