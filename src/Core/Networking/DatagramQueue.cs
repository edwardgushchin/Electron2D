using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
namespace Electron2D;

internal readonly record struct DatagramAddress(ulong High, ulong Low, long Scope, int Port)
{
    internal static DatagramAddress Capture(SocketAddress address)
    {
        var span = address.Buffer.Span; var port = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2));
        if (address.Family == AddressFamily.InterNetwork) return new(0, 0xffff00000000UL | BinaryPrimitives.ReadUInt32BigEndian(span.Slice(4, 4)), 0, port);
        return new(BinaryPrimitives.ReadUInt64BigEndian(span.Slice(8, 8)), BinaryPrimitives.ReadUInt64BigEndian(span.Slice(16, 8)), BitConverter.ToUInt32(span.Slice(24, 4)), port);
    }
    internal IPAddress Address()
    {
        Span<byte> data = stackalloc byte[16]; BinaryPrimitives.WriteUInt64BigEndian(data, High); BinaryPrimitives.WriteUInt64BigEndian(data[8..], Low);
        var address = new IPAddress(data, Scope); return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }
}

internal sealed class DatagramQueue
{
    // Metadata preserves the 24-byte queue budget while retaining IPv6 scope in a separate value array.
    private byte[] _bytes = [];
    private (DatagramAddress Address, int Size)[] _records = [];
    private int _read, _write, _first, _next, _used;
    internal int Count { get; private set; }
    internal int NextSize => Count == 0 ? -1 : _records[_first].Size;
    internal void Prepare(int requested)
    {
        if (requested < 0 || requested > 64 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(requested));
        var size = 1; while (size < requested) size = checked(size * 2);
        if (_bytes.Length != size) { _bytes = new byte[size]; _records = new (DatagramAddress, int)[Math.Max(1, size / 24)]; }
        Clear();
    }
    internal void Clear() { _read = _write = _first = _next = _used = Count = 0; }
    internal bool Store(DatagramAddress address, ReadOnlySpan<byte> packet)
    {
        if (packet.Length + 24 > _bytes.Length - _used || Count == _records.Length) return false;
        _records[_next] = (address, packet.Length); _next = (_next + 1) % _records.Length;
        var first = Math.Min(packet.Length, _bytes.Length - _write); packet[..first].CopyTo(_bytes.AsSpan(_write)); packet[first..].CopyTo(_bytes);
        _write = (_write + packet.Length) % _bytes.Length; _used += packet.Length + 24; Count++; return true;
    }
    internal DatagramAddress Take(Span<byte> destination)
    {
        var record = _records[_first]; var first = Math.Min(record.Size, _bytes.Length - _read);
        _bytes.AsSpan(_read, first).CopyTo(destination); _bytes.AsSpan(0, record.Size - first).CopyTo(destination[first..]);
        _read = (_read + record.Size) % _bytes.Length; _first = (_first + 1) % _records.Length; _used -= record.Size + 24; Count--; return record.Address;
    }
}
