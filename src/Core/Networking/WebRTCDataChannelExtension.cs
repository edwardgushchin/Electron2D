namespace Electron2D;

/// <summary>Defines a typed channel implementation using the common packet span hooks.</summary>
/// <remarks>Implementors validate owner access with CheckPacketPeer. NextPacketSize observes without consuming;
/// ReadPacketCore consumes exactly one packet into a validated exact-sized span.</remarks>
public abstract class WebRTCDataChannelExtension : WebRTCDataChannel
{
    /// <summary>Creates an extension identity on the current owner thread.</summary>
    protected WebRTCDataChannelExtension() { }
    /// <inheritdoc />
    public abstract override void Close();
    /// <inheritdoc />
    public abstract override int GetAvailablePacketCount();
    /// <inheritdoc />
    public abstract override int GetBufferedAmount();
    /// <inheritdoc />
    public abstract override int GetID();
    /// <inheritdoc />
    public abstract override string GetLabel();
    /// <inheritdoc />
    public abstract override int GetMaxPacketLifeTime();
    /// <inheritdoc />
    public abstract override int GetMaxPacketSize();
    /// <inheritdoc />
    public abstract override int GetMaxRetransmits();
    /// <inheritdoc />
    public abstract override string GetProtocol();
    /// <inheritdoc />
    public abstract override ChannelState GetReadyState();
    /// <inheritdoc />
    public abstract override PacketWriteMode WriteMode { get; set; }
    /// <inheritdoc />
    public abstract override bool IsNegotiated();
    /// <inheritdoc />
    public abstract override bool IsOrdered();
    /// <inheritdoc />
    public abstract override void Poll();
    /// <inheritdoc />
    public abstract override void PutPacket(ReadOnlySpan<byte> data);
    /// <inheritdoc />
    public abstract override bool WasStringPacket();
    /// <inheritdoc />
    protected abstract override int NextPacketSize();
    /// <inheritdoc />
    protected abstract override void ReadPacketCore(Span<byte> destination);
}
