using System.Buffers.Binary;
using System.Text;

namespace Electron2D;

internal readonly record struct ZIPDirectoryInfo(long Start, long Length, long Prefix, long Count, byte[]? Comment);

internal static class ZIPDirectory
{
    internal static readonly UTF8Encoding UTF8 = new(false, true);
    private static readonly uint[] CRCTable = CreateCRCTable();
    private static uint[] CreateCRCTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++) { var value = i; for (var bit = 0; bit < 8; bit++) value = (value >> 1) ^ ((value & 1) == 0 ? 0 : 0xedb88320); table[i] = value; }
        return table;
    }
    internal static uint UpdateCRC(uint crc, ReadOnlySpan<byte> data)
    { foreach (var value in data) crc = CRCTable[(crc ^ value) & 255] ^ (crc >> 8); return crc; }
    internal static string ResolvePath(string path)
    { ArgumentException.ThrowIfNullOrEmpty(path); return ProjectSettings.GlobalizePath(path); }
    internal static byte[] Name(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (name.Contains('\0')) throw new ArgumentException("An archive name cannot contain a null character.", nameof(name));
        var bytes = UTF8.GetBytes(name);
        if (bytes.Length > ushort.MaxValue) throw new ArgumentException("The UTF-8 entry name exceeds 65535 bytes.", nameof(name));
        return bytes;
    }
    internal static ushort U16(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
    internal static uint U32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
    internal static ulong U64(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
    internal static void W16(Span<byte> data, int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(data[offset..], value);
    internal static void W32(Span<byte> data, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(data[offset..], value);
    internal static void W64(Span<byte> data, int offset, ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(data[offset..], value);
    private static long Offset(ulong value) => value <= long.MaxValue ? (long)value : throw new InvalidDataException("ZIP offset exceeds the stream range.");
    private static void ReadAt(Stream stream, long position, Span<byte> data)
    { if (position < 0 || position > stream.Length - data.Length) throw new InvalidDataException("Truncated ZIP directory."); stream.Position = position; stream.ReadExactly(data); }
    internal static ZIPDirectoryInfo Read(Stream stream)
    {
        var saved = stream.Position;
        try
        {
            var tail = new byte[(int)Math.Min(stream.Length, ushort.MaxValue + 22L)]; ReadAt(stream, stream.Length - tail.Length, tail);
            for (var i = tail.Length - 22; i >= 0; i--)
            {
                if (U32(tail, i) != 0x06054b50 || i + 22 + U16(tail, i + 20) != tail.Length) continue;
                if (U16(tail, i + 4) != 0 || U16(tail, i + 6) != 0 || U16(tail, i + 8) != U16(tail, i + 10)) throw new InvalidDataException("Split ZIP archives are unsupported.");
                var end = stream.Length - tail.Length + i; long count = U16(tail, i + 10), length = U32(tail, i + 12), logical = U32(tail, i + 16); var centralEnd = end;
                if (count == ushort.MaxValue || length == uint.MaxValue || logical == uint.MaxValue)
                {
                    Span<byte> locator = stackalloc byte[20]; ReadAt(stream, end - 20, locator);
                    if (U32(locator, 0) != 0x07064b50 || U32(locator, 4) != 0 || U32(locator, 16) != 1) throw new InvalidDataException("Invalid ZIP64 locator.");
                    var record = FindZIP64End(stream, end - 20, Offset(U64(locator, 8))); Span<byte> header = stackalloc byte[56]; ReadAt(stream, record, header);
                    if (U32(header, 16) != 0 || U32(header, 20) != 0 || U64(header, 24) != U64(header, 32)) throw new InvalidDataException("Split ZIP64 archives are unsupported.");
                    count = Offset(U64(header, 32)); length = Offset(U64(header, 40)); logical = Offset(U64(header, 48)); centralEnd = record;
                }
                var start = centralEnd - length; var prefix = start - logical;
                if (start < 0 || prefix < 0 || count > int.MaxValue || count > length / 46) throw new InvalidDataException("Invalid ZIP directory extent.");
                return new(start, length, prefix, count, tail.AsSpan(i + 22).ToArray());
            }
            throw new InvalidDataException("The ZIP end record is missing.");
        }
        finally { stream.Position = saved; }
    }
    private static long FindZIP64End(Stream stream, long end, long logical)
    {
        bool Matches(long position)
        {
            Span<byte> candidate = stackalloc byte[12];
            if (position < 0 || position > end - 56) return false;
            ReadAt(stream, position, candidate);
            return U32(candidate, 0) == 0x06064b50 && U64(candidate, 4) >= 44 && U64(candidate, 4) == (ulong)(end - position - 12);
        }
        if (Matches(logical)) return logical;
        if (Matches(end - 56)) return end - 56;
        var buffer = new byte[65536]; var upper = end - 56;
        while (upper >= 0)
        {
            var lower = Math.Max(0, upper - buffer.Length + 4); var size = (int)Math.Min(buffer.Length, end - lower);
            ReadAt(stream, lower, buffer.AsSpan(0, size));
            for (var i = Math.Min(size - 4, (int)(upper - lower)); i >= 0; i--) if (U32(buffer, i) == 0x06064b50 && Matches(lower + i)) return lower + i;
            upper = lower - 1;
        }
        throw new InvalidDataException("The ZIP64 end record is missing.");
    }
    internal static ushort[] Flags(Stream stream, ZIPDirectoryInfo directory)
    {
        var saved = stream.Position; var result = new ushort[(int)directory.Count]; Span<byte> header = stackalloc byte[46];
        try
        {
            var cursor = directory.Start; var end = checked(cursor + directory.Length);
            for (var i = 0; i < result.Length; i++)
            {
                ReadAt(stream, cursor, header);
                if (U32(header, 0) != 0x02014b50) throw new InvalidDataException("Invalid ZIP central entry.");
                result[i] = U16(header, 8); cursor = checked(cursor + 46 + U16(header, 28) + U16(header, 30) + U16(header, 32));
                if (cursor > end) throw new InvalidDataException("ZIP entry exceeds its central directory.");
            }
            if (cursor != end) throw new InvalidDataException("Unexpected data in the ZIP central directory.");
            return result;
        }
        finally { stream.Position = saved; }
    }
    internal static void CopyDirectory(Stream stream, ZIPDirectoryInfo directory)
    {
        var destination = stream.Position; Span<byte> buffer = stackalloc byte[65536]; long copied = 0;
        while (copied < directory.Length)
        {
            var count = (int)Math.Min(buffer.Length, directory.Length - copied); ReadAt(stream, directory.Start + copied, buffer[..count]);
            stream.Position = destination + copied; stream.Write(buffer[..count]); copied += count;
        }
        stream.Position = destination + copied;
    }
}

internal sealed class ZIPViewStream(Stream stream, long origin) : Stream
{
    public override bool CanRead => stream.CanRead;
    public override bool CanSeek => stream.CanSeek;
    public override bool CanWrite => false;
    public override long Length => stream.Length - origin;
    public override long Position { get => stream.Position - origin; set { ArgumentOutOfRangeException.ThrowIfNegative(value); stream.Position = checked(origin + value); } }
    public override int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => stream.Read(buffer);
    public override long Seek(long offset, SeekOrigin seekOrigin) { Position = checked((seekOrigin switch { SeekOrigin.Begin => 0, SeekOrigin.Current => Position, SeekOrigin.End => Length, _ => throw new ArgumentOutOfRangeException(nameof(seekOrigin)) }) + offset); return Position; }
    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
