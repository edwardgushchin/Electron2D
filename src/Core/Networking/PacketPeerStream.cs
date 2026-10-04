using System.Buffers.Binary;
namespace Electron2D;

/// <summary>Preserves packet boundaries over a borrowed ordered byte stream.</summary>
/// <remarks>Wire packets use a four-byte little-endian length independent of StreamPeer.BigEndian.
/// Zero-byte sends are ignored; a received zero length remains a valid packet. Input resizing rejects queued
/// or partial data. Oversized headers fail explicitly rather than stalling a full buffer indefinitely.</remarks>
public class PacketPeerStream : PacketPeer
{
    private StreamPeer? _peer;
    private byte[] _input = new byte[65536], _output = new byte[65536];
    private int _count;
    /// <summary>Creates a detached packet wrapper with 65532-byte payload limits.</summary>
    public PacketPeerStream() { }
    /// <summary>Gets or replaces the borrowed stream. Different identities discard queued input.</summary><value>Null initially. The wrapper never disposes its stream.</value>
    public StreamPeer? StreamPeer
    {
        get { CheckPacketPeer(); return _peer; }
        set { CheckPacketPeer(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(value, _peer)) return; _peer = value; _count = 0; }
    }
    /// <summary>Gets or sets the rounded input payload limit.</summary><value>65532 initially; allocation is the next power of two of value plus four, at most 64 MiB.</value>
    /// <exception cref="InvalidOperationException">Input bytes are queued and would be lost.</exception>
    public int InputBufferMaxSize { get { CheckPacketPeer(); return _input.Length - 4; } set { CheckPacketPeer(); if (_count != 0) throw new InvalidOperationException("Input buffer contains data."); _input = new byte[Capacity(value)]; } }
    /// <summary>Gets or sets the rounded output payload limit.</summary><value>65532 initially; allocation is the next power of two of value plus four, at most 64 MiB.</value>
    public int OutputBufferMaxSize { get { CheckPacketPeer(); return _output.Length - 4; } set { CheckPacketPeer(); _output = new byte[Capacity(value)]; } }
    private static int Capacity(int value) { if (value < 0 || value > 64 * 1024 * 1024 - 4) throw new ArgumentOutOfRangeException(nameof(value)); var size = 4; while (size < value + 4) size *= 2; return size; }
    private StreamPeer RequireStream() => _peer ?? throw new InvalidOperationException("A byte stream is required.");
    private void PollBuffer()
    {
        var peer = RequireStream(); if (_count < _input.Length) _count += peer.GetPartialData(_input.AsSpan(_count));
    }
    private int LengthAt(int offset)
    {
        var length = BinaryPrimitives.ReadUInt32LittleEndian(_input.AsSpan(offset, 4));
        if (length > (uint)InputBufferMaxSize) { _count = 0; throw new InvalidDataException("Packet header exceeds the configured input payload limit."); }
        return (int)length;
    }
    /// <inheritdoc />
    public override int GetAvailablePacketCount()
    {
        CheckPacketPeer(); PollBuffer(); var offset = 0; var count = 0;
        while (_count - offset >= 4) { var length = LengthAt(offset); if (length > _count - offset - 4) break; offset += length + 4; count++; }
        return count;
    }
    /// <inheritdoc />
    protected override int NextPacketSize() { PollBuffer(); if (_count < 4) return -1; var length = LengthAt(0); return length > _count - 4 ? -1 : length; }
    /// <inheritdoc />
    protected override void ReadPacketCore(Span<byte> destination)
    {
        var consumed = destination.Length + 4; _input.AsSpan(4, destination.Length).CopyTo(destination); _count -= consumed; _input.AsSpan(consumed, _count).CopyTo(_input);
    }
    /// <inheritdoc />
    public override int GetMaxPacketSize() => OutputBufferMaxSize;
    /// <inheritdoc />
    public override void PutPacket(ReadOnlySpan<byte> data)
    {
        CheckPacketPeer(); var peer = RequireStream(); PollBuffer(); if (data.IsEmpty) return;
        if (data.Length > OutputBufferMaxSize) throw new ArgumentException("Packet exceeds the configured output payload limit.", nameof(data));
        BinaryPrimitives.WriteUInt32LittleEndian(_output, (uint)data.Length); data.CopyTo(_output.AsSpan(4)); peer.PutData(_output.AsSpan(0, data.Length + 4));
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<PacketPeerStream, int>(nameof(InputBufferMaxSize), s => s.InputBufferMaxSize, (s, v) => s.InputBufferMaxSize = v, _ => 65532, stored: true);
        yield return new PropertyDescriptor<PacketPeerStream, int>(nameof(OutputBufferMaxSize), s => s.OutputBufferMaxSize, (s, v) => s.OutputBufferMaxSize = v, _ => 65532, stored: true);
        yield return new PropertyDescriptor<PacketPeerStream, StreamPeer?>(nameof(StreamPeer), s => s.StreamPeer, (s, v) => s.StreamPeer = v, _ => null);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _peer = null; _input = _output = []; _count = 0; } base.Dispose(disposing); }
}
