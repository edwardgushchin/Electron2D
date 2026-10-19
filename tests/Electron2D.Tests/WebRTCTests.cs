using System.Diagnostics;
using System.Text;
using Electron2D;
internal static partial class WebRTCTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Pump(WebRTCPeerConnection a, WebRTCPeerConnection b, Func<bool> done)
    { var watch = Stopwatch.StartNew(); while (!done()) { if (watch.Elapsed.TotalSeconds > 15) throw new TimeoutException("WebRTC peers " + a.GetConnectionState() + "/" + b.GetConnectionState()); a.Poll(); b.Poll(); Thread.Sleep(1); } }
    internal static void Link(WebRTCPeerConnection a, WebRTCPeerConnection b)
    {
        a.SessionDescriptionCreated += (type, sdp) => { a.SetLocalDescription(type, sdp.Replace("\r\n", "\n", StringComparison.Ordinal)); b.SetRemoteDescription(type, sdp); };
        b.SessionDescriptionCreated += (type, sdp) => { b.SetLocalDescription(type, sdp); a.SetRemoteDescription(type, sdp); };
        a.ICECandidateCreated += (mid, index, candidate) => b.AddICECandidate(mid, index, candidate);
        b.ICECandidateCreated += (mid, index, candidate) => a.AddICECandidate(mid, index, candidate);
    }
    internal static void Run()
    {
        using var a = WebRTCPeerConnection.Create(); using var b = WebRTCPeerConnection.Create(); a.Initialize(); b.Initialize(); Link(a, b);
        using var send = a.CreateDataChannel("payload\u2603", new() { Protocol = "test" }); WebRTCDataChannel? received = null; b.DataChannelReceived += channel => received = channel;
        a.CreateOffer(); Check(a.GetSignalingState() == WebRTCPeerConnection.SignalingState.Stable, "Generating an offer does not commit local signaling.");
        Pump(a, b, () => received is not null && send.GetReadyState() == WebRTCDataChannel.ChannelState.Open && received.GetReadyState() == WebRTCDataChannel.ChannelState.Open);
        using var receiver = received!; Check(a.GetConnectionState() == WebRTCPeerConnection.ConnectionState.Connected && b.GetConnectionState() == WebRTCPeerConnection.ConnectionState.Connected && receiver.GetLabel() == "payload\u2603" && receiver.GetProtocol() == "test" && !receiver.IsNegotiated() && receiver.IsOrdered(), "Native in-band data channel and metadata.");
        Reject<InvalidOperationException>(() => a.CreateDataChannel("late"));
        var text = Encoding.UTF8.GetBytes("before\0after\u2603"); send.WriteMode = PacketWriteMode.Text; send.PutPacket(text); Pump(a, b, () => receiver.GetAvailablePacketCount() != 0);
        Reject<ArgumentException>(() => receiver.GetPacket(new byte[1])); Check(receiver.GetAvailablePacketCount() == 1, "A small receive destination preserves the complete packet."); Check(receiver.GetPacket().AsSpan().SequenceEqual(text) && receiver.WasStringPacket(), "UTF-8 text preserves embedded NUL and non-ASCII bytes.");
        send.WriteMode = PacketWriteMode.Binary; send.PutPacket([]); Pump(a, b, () => receiver.GetAvailablePacketCount() != 0); Check(receiver.GetPacket().Length == 0 && !receiver.WasStringPacket(), "Empty binary message preserves its type and boundary.");
        Span<byte> payload = stackalloc byte[64]; Span<byte> target = stackalloc byte[64]; payload.Fill(71);
        for (var i = 0; i < 32; i++) { send.PutPacket(payload); Pump(a, b, () => receiver.GetAvailablePacketCount() != 0); receiver.GetPacket(target); }
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) { send.PutPacket(payload); var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 5; while (receiver.GetAvailablePacketCount() == 0) { a.Poll(); b.Poll(); if (Stopwatch.GetTimestamp() > deadline) throw new TimeoutException("Prepared packet"); Thread.Sleep(1); } receiver.GetPacket(target); }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated; Check(bytes == 0 && target.SequenceEqual(payload), "Prepared native send/poll/read must allocate zero owner bytes: " + bytes); Console.WriteLine("Native packet interval: 32 frames, " + bytes + " managed owner bytes.");
        a.Close(); Pump(a, b, () => receiver.GetReadyState() == WebRTCDataChannel.ChannelState.Closed); Check(!send.IsDisposed && send.GetReadyState() == WebRTCDataChannel.ChannelState.Closed, "Peer close retires transport without disposing the caller's channel identity.");
        a.Initialize(); Check(a.GetConnectionState() == WebRTCPeerConnection.ConnectionState.New && send.GetReadyState() == WebRTCDataChannel.ChannelState.Closed, "Reinitialization creates a new cohort without rebinding old channels.");
        Console.WriteLine("WebRTC native negotiation, exact UTF-8/binary packets, metadata, lifecycle and prepared allocation passed.");
        Multiplayer();
        Extensions();
        Configuration();
        Scene();
    }
}
