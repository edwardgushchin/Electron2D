using System.Collections;
using System.Diagnostics;
using Electron2D;
internal static partial class WebRTCTests
{
    private sealed class ReentrantServers(WebRTCPeerConnection peer) : IReadOnlyList<WebRTCICEServer>
    {
        public int Count => 1;
        public WebRTCICEServer this[int index] => new() { URLs = ["stun:127.0.0.1:9"] };
        public IEnumerator<WebRTCICEServer> GetEnumerator() { peer.Close(); yield return this[0]; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private static void Configuration()
    {
        using var peer = WebRTCPeerConnection.Create(); peer.Initialize(); using var live = peer.CreateDataChannel("preserved");
        Reject<ArgumentException>(() => peer.Initialize(new() { ICEServers = [new() { URLs = ["https://invalid"] }] }));
        Reject<ArgumentNullException>(() => peer.Initialize(new() { ICEServers = [new() { URLs = null! }] }));
        Reject<InvalidOperationException>(() => peer.Initialize(new() { ICEServers = new ReentrantServers(peer) }));
        Check(peer.GetConnectionState() == WebRTCPeerConnection.ConnectionState.New && live.GetReadyState() == WebRTCDataChannel.ChannelState.Connecting, "Failed configuration/reentrant enumeration preserves native cohort.");
        foreach (var options in new WebRTCDataChannelOptions[] { new() { Negotiated = true }, new() { ID = -1 }, new() { ID = 65535 }, new() { MaxPacketLifeTime = -1 }, new() { MaxRetransmits = 65536 }, new() { MaxPacketLifeTime = 0, MaxRetransmits = 0 } })
            Reject<ArgumentException>(() => peer.CreateDataChannel("invalid", options));
        Reject<ArgumentException>(() => peer.CreateDataChannel("bad\0label")); Reject<ArgumentException>(() => peer.CreateDataChannel("bad", new() { Protocol = "\ud800" }));
        using var lifetime = peer.CreateDataChannel("lifetime", new() { Negotiated = true, ID = 20, Ordered = false, MaxPacketLifeTime = 0 });
        using var retransmit = peer.CreateDataChannel("retransmit", new() { Negotiated = true, ID = 22, MaxRetransmits = 0 });
        Check(lifetime.GetID() == 20 && lifetime.IsNegotiated() && !lifetime.IsOrdered() && lifetime.GetMaxPacketLifeTime() == 0 && lifetime.GetMaxRetransmits() == 65535 && retransmit.GetMaxRetransmits() == 0 && retransmit.GetMaxPacketLifeTime() == 65535, "Zero reliability limits and absent sentinels preserve distinct semantics.");
        Reject<InvalidOperationException>(() => live.PutPacket([1])); Reject<ArgumentOutOfRangeException>(() => live.WriteMode = (PacketWriteMode)99);
        var seen = 0; peer.SessionDescriptionCreated += (type, sdp) => { seen++; Reject<IOException>(() => peer.SetLocalDescription(type, sdp.Replace("a=ice-ufrag:", "a=ice-ufrag:foreign", StringComparison.Ordinal))); };
        peer.CreateOffer(); peer.Poll(); Check(seen == 1 && peer.GetSignalingState() == WebRTCPeerConnection.SignalingState.Stable, "Foreign local transport identity rejects without committing.");
        peer.Close(); Check(live.GetReadyState() == WebRTCDataChannel.ChannelState.Closed && !live.IsDisposed, "Closing pending channels preserves caller wrappers.");
        Console.WriteLine("WebRTC transactional configuration, reentry, option domains, zero reliability limits and foreign SDP rejection passed.");
        Overflow();
    }
    private static void Overflow()
    {
        using var a = WebRTCPeerConnection.Create(); using var b = WebRTCPeerConnection.Create(); Link(a, b); using var channel = a.CreateDataChannel("overflow"); WebRTCDataChannel? incoming = null; b.DataChannelReceived += value => incoming = value; a.CreateOffer(); Pump(a, b, () => incoming is not null && channel.GetReadyState() == WebRTCDataChannel.ChannelState.Open);
        using var receiver = incoming!; for (var i = 0; i < 1100; i++) channel.PutPacket([]);
        var failed = false; var timer = Stopwatch.StartNew(); while (!failed && timer.Elapsed.TotalSeconds < 10) { a.Poll(); b.Poll(); try { receiver.Poll(); } catch (IOException) { failed = true; } Thread.Sleep(1); }
        Check(failed, "Bounded native receive queue reports overflow rather than silently losing packets."); Reject<IOException>(receiver.Poll);
        Console.WriteLine("WebRTC 1024-packet receive budget and sticky overflow passed.");
    }
}
