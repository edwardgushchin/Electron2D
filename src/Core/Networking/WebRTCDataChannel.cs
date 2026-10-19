using System.Runtime.InteropServices;
using System.Text;
namespace Electron2D;

/// <summary>Exchanges complete WebRTC messages through the common packet span contract.</summary>
public abstract class WebRTCDataChannel : PacketPeer
{
    /// <summary>Describes the channel's ready state.</summary>
    public enum ChannelState
    {
        /// <summary>The channel is connecting.</summary>
        Connecting = 0,
        /// <summary>The channel accepts messages.</summary>
        Open = 1,
        /// <summary>The channel is closing.</summary>
        Closing = 2,
        /// <summary>The channel is closed.</summary>
        Closed = 3
    }
    /// <summary>Creates an owner-thread channel identity.</summary>
    protected WebRTCDataChannel() { }
    /// <summary>Gets or sets UTF-8 text versus binary transmission.</summary><value>Binary initially.</value>
    public abstract PacketWriteMode WriteMode { get; set; }
    /// <summary>Closes this channel without disposing its managed identity.</summary>
    public abstract void Close();
    /// <summary>Returns the number of buffered unsent application bytes.</summary><returns>A nonnegative byte count.</returns>
    public abstract int GetBufferedAmount();
    /// <summary>Returns the stream identity.</summary><returns>Zero through 65534, or 65535 before assignment.</returns>
    public abstract int GetID();
    /// <summary>Returns the immutable application label.</summary><returns>A UTF-8 label.</returns>
    public abstract string GetLabel();
    /// <summary>Returns the partial-reliability lifetime.</summary><returns>Milliseconds, or 65535 when unset.</returns>
    public abstract int GetMaxPacketLifeTime();
    /// <summary>Returns the partial-reliability retransmission count.</summary><returns>A count, or 65535 when unset.</returns>
    public abstract int GetMaxRetransmits();
    /// <summary>Returns the immutable application protocol.</summary><returns>A UTF-8 subprotocol label.</returns>
    public abstract string GetProtocol();
    /// <summary>Reports the current ready state.</summary><returns>The current channel state.</returns>
    public abstract ChannelState GetReadyState();
    /// <summary>Reports whether stream identity was negotiated out of band.</summary><returns>True for explicit negotiation.</returns>
    public abstract bool IsNegotiated();
    /// <summary>Reports whether delivered messages preserve ordering.</summary><returns>True for ordered delivery.</returns>
    public abstract bool IsOrdered();
    /// <summary>Observes native channel errors without consuming queued packets.</summary>
    public abstract void Poll();
    /// <summary>Reports the type of the last successfully received packet.</summary><returns>True for UTF-8 text, false for binary or before any read.</returns>
    public abstract bool WasStringPacket();
    internal int PeekPacketSize() { CheckPacketPeer(); return NextPacketSize(); }
}
internal sealed class NativeWebRTCDataChannel : WebRTCDataChannel
{
    private readonly WebRTCChannelHandle _handle; private readonly bool _negotiated; private readonly string _label, _protocol; private PacketWriteMode _mode = PacketWriteMode.Binary; private bool _text;
    private static readonly UTF8Encoding UTF8 = new(false, true);
    internal NativeWebRTCDataChannel(nint handle, bool negotiated)
    {
        _handle = new(handle); _negotiated = negotiated;
        try { _label = Marshal.PtrToStringUTF8(WebRTCNative.ChannelString(_handle, 0))!; _protocol = Marshal.PtrToStringUTF8(WebRTCNative.ChannelString(_handle, 1))!; }
        catch { _handle.Dispose(); throw; }
    }
    private int Info(int kind) { CheckPacketPeer(); var value = WebRTCNative.ChannelInfo(_handle, kind); WebRTCNative.Check(value); return value; }
    public override PacketWriteMode WriteMode { get { CheckPacketPeer(); return _mode; } set { CheckPacketPeer(); if (value is not PacketWriteMode.Text and not PacketWriteMode.Binary) throw new ArgumentOutOfRangeException(nameof(value)); _mode = value; } }
    public override void Close() { CheckPacketPeer(); WebRTCNative.Check(WebRTCNative.CloseChannel(_handle)); }
    public override int GetBufferedAmount() => Info(3);
    public override int GetID() => Info(1);
    public override string GetLabel() { CheckPacketPeer(); return _label; }
    public override int GetMaxPacketLifeTime() => Info(5);
    public override int GetMaxRetransmits() => Info(6);
    public override string GetProtocol() { CheckPacketPeer(); return _protocol; }
    public override ChannelState GetReadyState() => (ChannelState)Info(0);
    public override bool IsNegotiated() { CheckPacketPeer(); return _negotiated; }
    public override bool IsOrdered() => Info(4) != 0;
    public override void Poll() { CheckPacketPeer(); WebRTCNative.Check(WebRTCNative.PollChannel(_handle)); }
    public override bool WasStringPacket() { CheckPacketPeer(); return _text; }
    public override int GetAvailablePacketCount() { Poll(); return Info(7); }
    public override int GetMaxPacketSize() => Info(2);
    protected override int NextPacketSize() { Poll(); var size = WebRTCNative.ChannelSize(_handle); if (size < -1) WebRTCNative.Check(size); return size; }
    protected override unsafe void ReadPacketCore(Span<byte> destination) { fixed (byte* target = destination) { WebRTCNative.Check(WebRTCNative.Read(_handle, target, destination.Length, out var text)); _text = text != 0; } }
    public override unsafe void PutPacket(ReadOnlySpan<byte> data)
    {
        CheckPacketPeer(); if (GetReadyState() != ChannelState.Open) throw new InvalidOperationException("The channel is not open."); if (data.Length > GetMaxPacketSize()) throw new ArgumentException("Packet exceeds channel capacity.", nameof(data)); if (_mode == PacketWriteMode.Text) _ = UTF8.GetCharCount(data);
        fixed (byte* source = data) WebRTCNative.Check(WebRTCNative.Send(_handle, source, data.Length, _mode == PacketWriteMode.Text ? 1 : 0));
    }
    protected override void Dispose(bool disposing) { if (disposing) _handle.Dispose(); base.Dispose(disposing); }
}
