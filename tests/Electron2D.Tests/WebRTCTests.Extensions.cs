using Electron2D;
internal static partial class WebRTCTests
{
    private sealed class ExtensionChannel : WebRTCDataChannelExtension
    {
        internal bool FailWrite, FailDispose, FailPoll;
        internal int DisposalCount;
        internal Action? DuringPoll;
        private byte[]? _packet;
        private bool _closed;
        private PacketWriteMode _write = PacketWriteMode.Binary;
        public override PacketWriteMode WriteMode { get { CheckPacketPeer(); return _write; } set { CheckPacketPeer(); if (FailWrite) throw new InvalidOperationException("write policy"); _write = value; } }
        public override void Close() { CheckPacketPeer(); _closed = true; }
        public override int GetAvailablePacketCount() { CheckPacketPeer(); return _packet is null ? 0 : 1; }
        public override int GetBufferedAmount() { CheckPacketPeer(); return 0; }
        public override int GetID() { CheckPacketPeer(); return 4; }
        public override string GetLabel() { CheckPacketPeer(); return "extension"; }
        public override int GetMaxPacketLifeTime() { CheckPacketPeer(); return 65535; }
        public override int GetMaxPacketSize() { CheckPacketPeer(); return 1200; }
        public override int GetMaxRetransmits() { CheckPacketPeer(); return 65535; }
        public override string GetProtocol() { CheckPacketPeer(); return "test"; }
        public override ChannelState GetReadyState() { CheckPacketPeer(); return _closed ? ChannelState.Closed : ChannelState.Open; }
        public override bool IsNegotiated() { CheckPacketPeer(); return true; }
        public override bool IsOrdered() { CheckPacketPeer(); return true; }
        public override void Poll() { CheckPacketPeer(); DuringPoll?.Invoke(); if (FailPoll) throw new InvalidDataException("receive overflow"); }
        public override void PutPacket(ReadOnlySpan<byte> data) { CheckPacketPeer(); _packet = data.ToArray(); }
        public override bool WasStringPacket() { CheckPacketPeer(); return false; }
        protected override int NextPacketSize() => _packet?.Length ?? -1;
        protected override void ReadPacketCore(Span<byte> destination) { _packet!.CopyTo(destination); _packet = null; }
        protected override void Dispose(bool disposing) { if (disposing) { DisposalCount++; Close(); } base.Dispose(disposing); if (disposing && FailDispose) throw new IOException("dispose channel"); }
    }
    private sealed class ExtensionPeer : WebRTCPeerConnectionExtension
    {
        internal readonly List<ExtensionChannel> Channels = [];
        internal int FailWriteAt = -1;
        internal bool FailCleanup;
        private ConnectionState _state;
        public override void Initialize(WebRTCConfiguration? configuration = null) { CheckPeerAccess(); _state = ConnectionState.New; }
        public override void AddICECandidate(string media, int index, string name) { CheckPeerAccess(); EmitICECandidateCreated(media, index, name); }
        public override void Close() { CheckPeerAccess(); _state = ConnectionState.Closed; }
        public override WebRTCDataChannel CreateDataChannel(string label, WebRTCDataChannelOptions? options = null)
        { CheckPeerAccess(); var channel = new ExtensionChannel { FailWrite = Channels.Count == FailWriteAt, FailDispose = FailCleanup }; Channels.Add(channel); return channel; }
        public override void CreateOffer() { CheckPeerAccess(); EmitSessionDescriptionCreated("offer", "extension-sdp"); }
        public override ConnectionState GetConnectionState() { CheckPeerAccess(); return _state; }
        public override GatheringState GetGatheringState() { CheckPeerAccess(); return GatheringState.Complete; }
        public override SignalingState GetSignalingState() { CheckPeerAccess(); return SignalingState.Stable; }
        public override void Poll() { CheckPeerAccess(); }
        public override void SetLocalDescription(string type, string sdp) { CheckPeerAccess(); _state = ConnectionState.Connecting; }
        public override void SetRemoteDescription(string type, string sdp) { CheckPeerAccess(); _state = ConnectionState.Connected; }
        internal void Incoming(WebRTCDataChannel channel) => EmitDataChannelReceived(channel);
    }
    private static void Extensions()
    {
        WebRTCPeerConnection.SetDefaultExtension(static () => new ExtensionPeer());
        try
        {
            using var created = WebRTCPeerConnection.Create(); Check(created is ExtensionPeer, "Typed default extension factory.");
            var peer = (ExtensionPeer)created; var delivered = 0;
            peer.SessionDescriptionCreated += (_, _) => throw new InvalidDataException("subscriber");
            peer.SessionDescriptionCreated += (type, sdp) => { Check(type == "offer" && sdp == "extension-sdp", "Typed SDP event."); delivered++; };
            Reject<AggregateException>(peer.CreateOffer); Check(delivered == 1, "A failed subscriber preserves later delivery.");
            var candidates = 0; peer.ICECandidateCreated += (mid, index, name) => { Check(mid == "app" && index == 2 && name == "candidate", "Typed candidate payload."); candidates++; };
            peer.AddICECandidate("app", 2, "candidate"); Check(candidates == 1, "Candidate event hook.");
            using var channel = peer.CreateDataChannel("payload"); WebRTCDataChannel? received = null; peer.DataChannelReceived += value => received = value; peer.Incoming(channel); Check(ReferenceEquals(received, channel), "Borrowed extension channel identity.");
            channel.PutPacket("span"u8); Reject<ArgumentException>(() => channel.GetPacket(new byte[1])); Span<byte> bytes = stackalloc byte[4]; Check(channel.GetPacket(bytes) == 4 && bytes.SequenceEqual("span"u8), "Extension packet hooks preserve small-buffer retry.");
            Task.Run(() => { Reject<InvalidOperationException>(peer.Poll); Reject<InvalidOperationException>(channel.Poll); Reject<InvalidOperationException>(peer.Dispose); }).GetAwaiter().GetResult(); Check(!peer.IsDisposed, "Foreign disposal rejects before state change.");
        }
        finally { WebRTCPeerConnection.SetDefaultExtension(null); }
        using (var broken = new ExtensionPeer { FailWriteAt = 2, FailCleanup = true })
        using (var multiplayer = new WebRTCMultiplayerPeer())
        {
            multiplayer.CreateServer(); AggregateException? failure = null; try { multiplayer.AddPeer(broken, 2); } catch (AggregateException e) { failure = e; }
            Check(failure?.Flatten().InnerExceptions.Count == 4 && !multiplayer.HasPeer(2) && broken.Channels.Count == 3 && broken.Channels.All(c => c.DisposalCount == 1), "Setup rollback attempts all channels and retains setup plus cleanup failures.");
        }
        using (var peer = new ExtensionPeer())
        using (var multiplayer = new WebRTCMultiplayerPeer())
        {
            multiplayer.CreateMesh(5); multiplayer.AddPeer(peer, 7); var connected = 0; var disconnected = 0; multiplayer.PeerConnected += _ => connected++; multiplayer.PeerDisconnected += _ => disconnected++;
            multiplayer.Poll(); Check(connected == 1, "Extension channels admit real multiplayer membership."); peer.Channels[0].FailPoll = true;
            Reject<AggregateException>(multiplayer.Poll); Check(disconnected == 1 && !multiplayer.HasPeer(7) && peer.Channels.All(c => c.IsDisposed), "Failed channel polling retires membership once and releases all channels.");
        }
        using (var peer = new ExtensionPeer())
        using (var multiplayer = new WebRTCMultiplayerPeer())
        {
            multiplayer.CreateMesh(5); multiplayer.AddPeer(peer, 7); multiplayer.PeerConnected += id => multiplayer.RemovePeer(id); multiplayer.Poll(); Check(!multiplayer.HasPeer(7), "Committed callbacks may remove their own membership during polling.");
        }
        using (var first = new ExtensionPeer())
        using (var second = new ExtensionPeer())
        using (var multiplayer = new WebRTCMultiplayerPeer())
        {
            multiplayer.CreateMesh(5); multiplayer.AddPeer(first, 7); multiplayer.AddPeer(second, 9); multiplayer.Poll();
            multiplayer.SetTargetPeer(-7); multiplayer.PutPacket("excluded"u8); Check(first.Channels[0].GetAvailablePacketCount() == 0 && second.Channels[0].GetAvailablePacketCount() == 1, "Negative target excludes one identity.");
            Check(multiplayer.GetPacketPeer() == 9 && multiplayer.GetPacket().AsSpan().SequenceEqual("excluded"u8), "Selected source follows the actual queued channel.");
            first.Channels[0].DuringPoll = () => multiplayer.RemovePeer(7); multiplayer.Poll(); Check(!multiplayer.HasPeer(7) && multiplayer.HasPeer(9), "Provider callback removal suppresses the retired cohort during channel polling.");
        }
        Console.WriteLine("WebRTC extension factory, events, span hooks, owner access, rollback aggregation and failed-channel retirement passed.");
    }
}
