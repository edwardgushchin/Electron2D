namespace Electron2D;

/// <summary>Reports the outcome of the most recent packet-read attempt.</summary>
public enum PacketReadStatus
{
    /// <summary>The read succeeded, or no read has been attempted.</summary>
    OK = 0,
    /// <summary>No complete packet was available.</summary>
    Unavailable = 1,
    /// <summary>A read failed for another reason; LastReadException carries details.</summary>
    Error = 2
}

/// <summary>Transfers complete byte packets with caller-owned receive buffers.</summary>
/// <remarks>Packet boundaries are preserved. Calls require the constructing thread. Dynamic value serialization
/// is absent; callers choose their own typed packet encoding. Snapshot reads allocate; span reads reuse caller storage.</remarks>
public abstract class PacketPeer : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private PacketReadStatus _readStatus;
    private Exception? _lastReadException;
    /// <summary>Creates a packet transport owned by the current thread.</summary>
    protected PacketPeer() { }
    /// <summary>Checks transport lifetime and constructing-thread ownership.</summary>
    protected void CheckPacketPeer() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("Packet access requires its constructing thread."); }
    /// <summary>Returns the number of complete immediately available packets.</summary><returns>Nonnegative packet count.</returns>
    public abstract int GetAvailablePacketCount();
    /// <summary>Reports the maximum outgoing raw payload supported by this transport.</summary><returns>A nonnegative byte count; operating-system limits may be narrower.</returns>
    public abstract int GetMaxPacketSize();
    /// <summary>Returns one complete packet in a caller-owned array.</summary><returns>The packet, including an empty datagram.</returns>
    /// <exception cref="InvalidOperationException">No packet is available.</exception>
    public byte[] GetPacket()
    {
        CheckPacketPeer();
        try { var size = RequirePacket(); var result = size == 0 ? Array.Empty<byte>() : new byte[size]; ReadPacketCore(result); ReadSucceeded(); return result; }
        catch (Exception error) { ReadFailed(error); throw; }
    }
    /// <summary>Copies one complete packet without allocating a receive array.</summary><param name="destination">Caller-owned storage large enough for the packet.</param><returns>The copied length; zero is a valid datagram.</returns>
    /// <exception cref="ArgumentException">Storage is too small; the packet is not consumed.</exception>
    public int GetPacket(Span<byte> destination)
    {
        CheckPacketPeer();
        try { var size = RequirePacket(); if (size > destination.Length) throw new ArgumentException("The packet destination is too small.", nameof(destination)); ReadPacketCore(destination[..size]); ReadSucceeded(); return size; }
        catch (Exception error) { ReadFailed(error); throw; }
    }
    private bool _unavailable;
    private int RequirePacket() { _unavailable = false; var size = NextPacketSize(); if (size >= 0) return size; _unavailable = true; throw new InvalidOperationException("No packet is available."); }
    private void ReadSucceeded() { _readStatus = PacketReadStatus.OK; _lastReadException = null; }
    private void ReadFailed(Exception error) { _readStatus = _unavailable ? PacketReadStatus.Unavailable : PacketReadStatus.Error; _lastReadException = error; _unavailable = false; }
    /// <summary>Returns the last packet-read status without polling.</summary><returns>The last read outcome.</returns>
    public PacketReadStatus GetPacketError() { CheckPacketPeer(); return _readStatus; }
    /// <summary>Gets the original exception from the last failed packet read.</summary><value>Null after a successful read or before any read.</value>
    public Exception? LastReadException { get { CheckPacketPeer(); return _lastReadException; } }
    /// <summary>Sends exactly one packet.</summary><param name="data">Packet bytes, borrowed only for this call.</param>
    public abstract void PutPacket(ReadOnlySpan<byte> data);
    /// <summary>Reports the next packet size without consuming it.</summary><returns>Length, or minus one when absent.</returns>
    protected abstract int NextPacketSize();
    /// <summary>Consumes the next complete packet into a validated destination.</summary><param name="destination">An exact-sized destination.</param>
    protected abstract void ReadPacketCore(Span<byte> destination);    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); CheckPacketPeer(); }

}
