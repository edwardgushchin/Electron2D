using Electron2D;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

internal static class SceneMultiplayerTests
{
    private static readonly RPCMethod<Player, int> Score = new(1, static (n, value) => { n.Score += value; n.Sender = n.Multiplayer.GetRemoteSenderID(); }, static _ => 4, static (v, b) => { BinaryPrimitives.WriteInt32LittleEndian(b, v); return 4; }, static b => b.Length == 4 ? BinaryPrimitives.ReadInt32LittleEndian(b) : throw new InvalidDataException());
    private sealed class Player : Node { internal int Score, Sender; internal Player() { RPCConfig(SceneMultiplayerTests.Score, new RPCOptions(RPCMode.AnyPeer, true)); } }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static int Port() { using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); listener.Bind(new IPEndPoint(IPAddress.Loopback, 0)); return ((IPEndPoint)listener.LocalEndPoint!).Port; }
    private static void Wait(Action poll, Func<bool> ready) { var deadline = Environment.TickCount64 + 10000; while (!ready()) { poll(); if (Environment.TickCount64 > deadline) throw new TimeoutException(); Thread.Yield(); } }
    private static WebSocketMultiplayerPeer Peer() => new() { InboundBufferSize = 4096, OutboundBufferSize = 4096, MaxQueuedPackets = 64 };
    internal static void Run() { Native(false); Native(true); Authentication(); LocalAndBranches(); MalformedAndCallbacks(); Console.WriteLine("SceneMultiplayer typed RPC/authentication/relay, scene integration, lifetime and prepared allocation checks passed."); }
    private static void Native(bool secure)
    {
        using var crypto = new Crypto(); using var key = secure ? crypto.GenerateRSA(2048) : null; using var certificate = secure ? crypto.GenerateSelfSignedCertificate(key!, "CN=localhost,O=Electron2D,C=RU") : null; using var serverTLS = secure ? TLSOptions.Server(key!, certificate!) : null; using var clientTLS = secure ? TLSOptions.Client(certificate!, "localhost") : null;
        using var host = Peer(); using var a = Peer(); using var b = Peer(); var port = Port(); host.CreateServer(port, "127.0.0.1", serverTLS); a.CreateClient((secure ? "wss" : "ws") + "://127.0.0.1:" + port, clientTLS); b.CreateClient((secure ? "wss" : "ws") + "://127.0.0.1:" + port, clientTLS);
        using var hs = new SceneMultiplayer { MultiplayerPeer = host }; using var ca = new SceneMultiplayer { MultiplayerPeer = a }; using var cb = new SceneMultiplayer { MultiplayerPeer = b };
        var root = new Node { Name = "Root" }; var hn = new Player { Name = "Player" }; root.AddChild(hn); var ar = new Node { Name = "ClientA" }; var an = new Player { Name = "Player" }; ar.AddChild(an); root.AddChild(ar); var br = new Node { Name = "ClientB" }; var bn = new Player { Name = "Player" }; br.AddChild(bn); root.AddChild(br);
        using var tree = new SceneTree(root); tree.SetMultiplayer(hs); tree.SetMultiplayer(ca, ar.GetPath()); tree.SetMultiplayer(cb, br.GetPath());
        void Poll() => tree.ProcessFrame(0);
        Wait(Poll, () => ca.GetPeers().Length == 2 && cb.GetPeers().Length == 2 && hs.GetPeers().Length == 2); var aid = a.GetUniqueID(); var bid = b.GetUniqueID(); Check(an.Multiplayer == ca && bn.Multiplayer == cb && hn.Multiplayer == hs, "Closest branch override selection.");
        an.RPC(Score, 3); Wait(Poll, () => hn.Score == 3 && bn.Score == 3); Check(an.Score == 3 && hn.Sender == aid && bn.Sender == aid && ca.GetRemoteSenderID() == 0, "RPC broadcast/local relay preserves original sender."); bn.RPCID(aid, Score, 2); Wait(Poll, () => an.Score == 5); Check(hn.Score == 3 && bn.Score == 3, "Client targets another client through server.");
        hn.RPCConfig(Score, new RPCOptions(RPCMode.Authority)); an.RPCID(1, Score, 1); var rejected = false; Wait(() => { try { Poll(); } catch (AggregateException) { rejected = true; } }, () => rejected); Check(hn.Score == 3 && hs.GetRemoteSenderID() == 0, "Receiver authority guard and scope restoration."); hn.SetMultiplayerAuthority(aid); an.RPCID(1, Score, 1); Wait(Poll, () => hn.Score == 4);
        var received = 0; MultiplayerPacketHandler raw = (id, bytes) => { Check(id == aid && bytes.SequenceEqual("raw"u8), "Raw relayed identity/payload."); received++; }; cb.PeerPacket += raw; ca.SendBytes("raw"u8, bid); Wait(Poll, () => received == 1); cb.PeerPacket -= raw;
        for (var i = 0; i < 32; i++) { hn.RPCID(aid, Score, 1); var expected = an.Score + 1; Wait(Poll, () => an.Score == expected); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { var expected = an.Score + 1; hn.RPCID(aid, Score, 1); var deadline = Environment.TickCount64 + 10000; while (an.Score != expected) { Poll(); if (Environment.TickCount64 > deadline) throw new TimeoutException(); } }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared native RPC/scene frame flow allocates zero managed bytes."); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Poll(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared idle scene multiplayer allocates zero managed bytes."); Console.WriteLine("64 " + (secure ? "WSS" : "WS") + " typed RPC/process-frame and idle cycles: 0 managed bytes.");
        tree.MultiplayerPoll = false; var prior = an.Score; hn.RPCID(aid, Score, 1); for (var i = 0; i < 8; i++) Poll(); Check(an.Score == prior, "Automatic polling toggle."); tree.MultiplayerPoll = true; Wait(Poll, () => an.Score == prior + 1); an.Name = "Changed"; hn.Name = "Changed"; hn.RPCID(aid, Score, 1); Wait(Poll, () => an.Score == prior + 2); Check(hs.RootPath == root.GetPath(), "Paths invalidate across hierarchy rename.");
        Reject<InvalidOperationException>(hs.Dispose); Reject<ArgumentException>(() => ca.SendBytes([])); Reject<ArgumentOutOfRangeException>(() => ca.SendBytes("x"u8, int.MinValue)); Reject<InvalidOperationException>(() => ca.MaxPacketBytes = 100); Reject<InvalidOperationException>(() => Task.Run(hs.Poll).GetAwaiter().GetResult());
        tree.SetMultiplayer(null, ar.GetPath()); tree.SetMultiplayer(null, br.GetPath()); tree.SetMultiplayer(null); Check(!hs.IsDisposed && host.GetConnectionStatus() == MultiplayerConnectionStatus.Connected, "Assigned APIs/transports stay borrowed on replacement.");
    }
    private static void Authentication()
    {
        using var host = Peer(); using var client = Peer(); var port = Port(); host.CreateServer(port, "127.0.0.1"); client.CreateClient("ws://127.0.0.1:" + port); using var server = new SceneMultiplayer { MultiplayerPeer = host }; using var api = new SceneMultiplayer { MultiplayerPeer = client }; var token = 0;
        server.AuthCallback = (id, data) => { Check(data.SequenceEqual("secret"u8), "Auth challenge data."); token++; server.CompleteAuth(id); }; api.AuthCallback = (id, data) => { Check(id == 1 && data.SequenceEqual("hello"u8), "Auth response data."); api.SendAuth(id, "secret"u8); api.CompleteAuth(id); };
        server.PeerAuthenticating += id => server.SendAuth(id, "hello"u8); void Poll() { server.Poll(); api.Poll(); }
        Wait(Poll, () => server.GetPeers().Length == 1 && api.GetPeers().Length == 1); Check(token == 1 && server.GetAuthenticatingPeers().Length == 0, "Bilateral authentication completes before admission."); Reject<InvalidOperationException>(() => api.SendAuth(1, "late"u8));
        var later = 0; MultiplayerPacketHandler fail = (_, _) => throw new InvalidOperationException("callback"); server.PeerPacket += fail; server.PeerPacket += (_, _) => later++; api.SendBytes("first"u8, 1); api.SendBytes("second"u8, 1); var saw = false; Wait(() => { try { Poll(); } catch (AggregateException) { saw = true; } }, () => later == 2); Check(saw && server.GetRemoteSenderID() == 0, "Callbacks aggregate while later messages/subscribers run."); server.PeerPacket -= fail;
        using var blocked = Peer(); using var pending = new SceneMultiplayer { AuthCallback = static (_, _) => { }, AuthTimeout = .01 }; pending.PeerAuthenticationFailed += _ => token++; blocked.CreateClient("ws://127.0.0.1:" + port); pending.MultiplayerPeer = blocked; Wait(() => { server.Poll(); pending.Poll(); }, () => token == 2); Check(pending.GetAuthenticatingPeers().Length == 0, "Authentication timeout closes pending peer.");
    }
    private static void MalformedAndCallbacks()
    {
        using var host = Peer(); using var client = Peer(); var port = Port(); host.CreateServer(port, "127.0.0.1"); client.CreateClient("ws://127.0.0.1:" + port); using var server = new SceneMultiplayer { MaxPacketBytes = 64, MultiplayerPeer = host }; using var api = new SceneMultiplayer { MultiplayerPeer = client }; var good = 0; server.PeerPacket += (_, bytes) => { Check(bytes.SequenceEqual("ok"u8), "Good packet after invalid frames."); good++; }; void Poll() { server.Poll(); api.Poll(); }
        Wait(Poll, () => server.GetPeers().Length == 1 && api.GetPeers().Length == 1); client.PutPacket(new byte[128]); client.PutPacket(new byte[] { 7, 1, 99, 0, 0, 0 }); client.PutPacket(new byte[] { 0, 1 }); api.SendBytes("ok"u8, 1); var failures = 0; Wait(() => { try { Poll(); } catch (AggregateException) { failures++; } }, () => good == 1); Check(failures > 0 && server.GetRemoteSenderID() == 0, "Oversize packet drains and malformed/forged commands do not prevent later messages.");
        var recursive = false; server.PeerPacket += (_, _) => { try { server.Poll(); } catch (InvalidOperationException) { recursive = true; } server.Clear(); }; api.SendBytes("ok"u8, 1); Wait(Poll, () => recursive); Check(server.GetPeers().Length == 0, "Callback reset invalidates old dispatch; recursive polling rejects.");
    }
    private sealed class ChangingPeer : MultiplayerPeer
    {
        internal int Target, Sent;
        internal bool Change, Fail;
        internal void Admit(int id) => EmitPeerConnected(id);
        public override int GetAvailablePacketCount() { CheckPacketPeer(); return 0; }
        protected override int NextPacketSize() => 0;
        protected override void ReadPacketCore(Span<byte> destination) => throw new InvalidOperationException();
        public override int GetMaxPacketSize() => 1024;
        public override void PutPacket(ReadOnlySpan<byte> packet) { CheckPacketPeer(); Sent++; if (Fail) throw new IOException("managed send failure"); if (Change) { Change = false; EmitPeerConnected(4); } }
        public override void SetTargetPeer(int id) => Target = id;
        public override int GetPacketPeer() => 0;
        public override int GetPacketChannel() => 0;
        public override TransferMode GetPacketMode() => TransferMode.Reliable;
        public override int GetUniqueID() => 1;
        public override MultiplayerConnectionStatus GetConnectionStatus() => MultiplayerConnectionStatus.Connected;
        public override bool IsServer() => true;
        public override void Poll() { CheckPacketPeer(); }
        public override void Close() { CheckPacketPeer(); }
        public override void DisconnectPeer(int id, bool force = false) { CheckPacketPeer(); }
    }
    private sealed class CustomAPI : MultiplayerAPI
    {
        private MultiplayerPeer? _peer;
        internal int Count;
        internal string Root = "";
        public override MultiplayerPeer? MultiplayerPeer { get { CheckMultiplayer(); return _peer; } set { CheckMultiplayer(); _peer = value; } }
        public override void Poll() { CheckMultiplayer(); Count++; }
        public override int GetUniqueID() { CheckMultiplayer(); return _peer?.GetUniqueID() ?? 1; }
        public override int[] GetPeers() { CheckMultiplayer(); return []; }
        public override int GetRemoteSenderID() { CheckMultiplayer(); return 0; }
        public override void ObjectConfigurationAdd(string path) { CheckMultiplayer(); Root = path; }
        public override void ObjectConfigurationRemove(string path) { CheckMultiplayer(); Check(path == Root, "Custom root configuration."); Root = ""; }
        public override void RPC<TNode, T>(int peer, TNode node, RPCMethod<TNode, T> method, T arguments)
        { CheckMultiplayer(); var storage = new byte[method.GetEncodedSize(arguments)]; method.Encode(arguments, storage); method.InvokeEncoded(node, storage); Count++; }
    }
    private static void LocalAndBranches()
    {
        using (var peer = new ChangingPeer()) using (var scene = new SceneMultiplayer { MultiplayerPeer = peer }) { peer.Admit(2); peer.Admit(3); peer.Change = true; scene.SendBytes("snapshot"u8); Check(peer.Sent == 2 && scene.GetPeers().Length == 3, "Managed transport admission during send preserves captured recipients."); }

        using (var peer = new ChangingPeer()) using (var scene = new SceneMultiplayer { MultiplayerPeer = peer }) using (var localTree = new SceneTree(new Player { Name = "Root" })) { localTree.SetMultiplayer(scene); peer.Admit(2); peer.Fail = true; Reject<AggregateException>(() => localTree.Root.RPC(Score, 1)); Check(((Player)localTree.Root).Score == 1 && scene.GetRemoteSenderID() == 0, "Remote failure still attempts configured local invocation and restores scope."); }
        using var tree = new SceneTree(new Player { Name = "Root" }); var player = (Player)tree.Root; Check(new RPCOptions().Mode == RPCMode.Authority && new RPCOptions().TransferMode == TransferMode.Reliable && default(RPCOptions).Mode == RPCMode.Disabled, "Constructed versus zero-initialized RPC policy."); Check(player.IsMultiplayerAuthority() && tree.MultiplayerPoll && tree.GetMultiplayer() is SceneMultiplayer, "Default offline local authority."); player.RPC(Score, 7); Check(player.Score == 7 && player.Sender == 1 && player.Multiplayer.GetRemoteSenderID() == 0, "Offline call-local typed dispatch."); player.RPCConfig(Score); Reject<InvalidOperationException>(() => player.RPC(Score, 1)); player.SetMultiplayerAuthority(2); Check(!player.IsMultiplayerAuthority(), "Authority local setting."); Reject<ArgumentOutOfRangeException>(() => player.SetMultiplayerAuthority(0)); using var custom = new CustomAPI(); tree.SetMultiplayer(custom); player.RPCConfig(Score, new RPCOptions()); player.RPC(Score, 1); tree.ProcessFrame(0); Check(custom.Count == 2 && custom.Root == "/Root", "Managed provider executes typed codec/configuration and scene hooks."); tree.SetMultiplayer(null); Check(custom.Root == "" && !custom.IsDisposed, "Managed provider detaches without disposal."); var saved = MultiplayerAPI.GetDefaultInterface(); MultiplayerAPI.SetDefaultInterface(static () => new CustomAPI()); try { using var created = MultiplayerAPI.CreateDefaultInterface(); Check(created is CustomAPI, "Typed default factory."); } finally { MultiplayerAPI.SetDefaultInterface(saved); }
        using var api = new SceneMultiplayer(); Reject<ArgumentException>(() => api.RootPath = "relative"); Reject<ArgumentOutOfRangeException>(() => api.AuthTimeout = double.NaN); Reject<InvalidOperationException>(() => tree.SetMultiplayer(tree.GetMultiplayer(), "/Root"));
    }
}
