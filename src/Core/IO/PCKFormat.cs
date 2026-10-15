using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Electron2D;

internal static class PCKFormat
{
    internal const uint Magic = 0x43504447;
    internal const int DirectoryLimit = 64 * 1024 * 1024;
    internal static readonly UTF8Encoding UTF8 = new(false, true);

    internal static string Normalize(string path, bool directory = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        path = path.Replace('\\', '/');
        if (path.StartsWith("res://", StringComparison.Ordinal)) path = path[6..];
        if (path.StartsWith('/') || path.Contains(':') || path.Any(char.IsControl))
            throw new ArgumentException("A pack path must be relative to res://.", nameof(path));
        var parts = new List<string>();
        foreach (var part in path.Split('/'))
        {
            if (part is "" or ".") continue;
            if (part == "..")
            {
                if (parts.Count == 0) throw new UnauthorizedAccessException("A pack path escapes res://.");
                parts.RemoveAt(parts.Count - 1);
            }
            else parts.Add(part);
        }
        var result = string.Join('/', parts);
        if (!directory && result.Length == 0) throw new ArgumentException("A pack file needs a name.", nameof(path));
        if (UTF8.GetByteCount(result) > 1024 * 1024) throw new ArgumentException("A pack path is too long.", nameof(path));
        return result;
    }

    internal static void Align(Stream stream, int alignment)
    {
        var padding = (alignment - stream.Position % alignment) % alignment;
        Span<byte> zeros = stackalloc byte[256]; zeros.Clear();
        while (padding > 0) { var n = (int)Math.Min(zeros.Length, padding); stream.Write(zeros[..n]); padding -= n; }
    }

    internal static long ReadOffset(BinaryReader reader) => checked((long)reader.ReadUInt64());

    internal static byte[] ReadExactly(Stream stream, int count)
    {
        if (count < 0 || count > stream.Length - stream.Position) throw new InvalidDataException("Truncated resource pack.");
        var result = new byte[count]; stream.ReadExactly(result); return result;
    }

    internal static void Encrypt(Stream destination, ReadOnlySpan<byte> data, byte[] key)
    {
        using var aes = Aes.Create(); aes.Key = key;
        var iv = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        destination.Write(MD5.HashData(data));
        Span<byte> length = stackalloc byte[8]; BinaryPrimitives.WriteUInt64LittleEndian(length, (ulong)data.Length); destination.Write(length);
        destination.Write(iv);
        var padded = new byte[checked((data.Length + 15) / 16 * 16)]; data.CopyTo(padded);
        try { destination.Write(aes.EncryptCfb(padded, iv, PaddingMode.None, 128)); }
        finally { CryptographicOperations.ZeroMemory(padded); }
    }

    internal static byte[] Decrypt(Stream source, byte[] key, long expectedLength = -1, int limit = int.MaxValue - 16)
    {
        using var reader = new BinaryReader(source, UTF8, leaveOpen: true);
        var hash = ReadExactly(source, 16); var length = ReadOffset(reader); var iv = ReadExactly(source, 16);
        if (length > limit || expectedLength >= 0 && length != expectedLength) throw new InvalidDataException("Invalid encrypted pack length.");
        var bytes = ReadExactly(source, checked((int)((length + 15) / 16 * 16)));
        using var aes = Aes.Create(); aes.Key = key;
        var result = aes.DecryptCfb(bytes, iv, PaddingMode.None, 128);
        if (!CryptographicOperations.FixedTimeEquals(MD5.HashData(result.AsSpan(0, (int)length)), hash))
        {
            CryptographicOperations.ZeroMemory(result); throw new InvalidDataException("Pack checksum or decryption key is invalid.");
        }
        Array.Resize(ref result, (int)length); return result;
    }
}

internal sealed class PackSliceStream(Stream source, long start, long length) : Stream
{
    private long _position;
    private bool _disposed;
    public override bool CanRead => !_disposed;
    public override bool CanSeek => !_disposed;
    public override bool CanWrite => false;
    public override long Length { get { ObjectDisposedException.ThrowIf(_disposed, this); return length; } }
    public override long Position { get { ObjectDisposedException.ThrowIf(_disposed, this); return _position; } set => Seek(value, SeekOrigin.Begin); }
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_position >= length) return 0;
        source.Position = checked(start + _position);
        var read = source.Read(buffer[..(int)Math.Min(buffer.Length, length - _position)]);
        if (read == 0) throw new EndOfStreamException("The resource pack was truncated.");
        _position += read; return read;
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var position = origin switch { SeekOrigin.Begin => offset, SeekOrigin.Current => checked(_position + offset), SeekOrigin.End => checked(length + offset), _ => throw new ArgumentOutOfRangeException(nameof(origin)) };
        if (position < 0) throw new IOException("Cannot seek before the packed file.");
        return _position = position;
    }
    public override void Flush() => ObjectDisposedException.ThrowIf(_disposed, this);
    public override void SetLength(long value) => throw new NotSupportedException("Packed files are read-only.");
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Packed files are read-only.");
    protected override void Dispose(bool disposing) { if (!_disposed) { _disposed = true; if (disposing) source.Dispose(); } base.Dispose(disposing); }
}
