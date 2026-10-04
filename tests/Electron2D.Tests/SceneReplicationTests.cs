using Electron2D;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

internal static class SceneReplicationTests
{
    private static readonly ReplicationProperty<Actor, int> Health = Property(1, static n => n.Health, static (n, v) => n.Health = v);
    private static readonly ReplicationProperty<Actor, int> Tag = Property(2, static n => n.Tag, static (n, v) => n.Tag = v);
    private static ReplicationProperty<Actor, int> Property(uint id, Func<Actor, int> get, Action<Actor, int> set) => new(id, get, set, static _ => 4, static (value, bytes) => { BinaryPrimitives.WriteInt32LittleEndian(bytes, value); return 4; }, static bytes => bytes.Length == 4 ? BinaryPrimitives.ReadInt32LittleEndian(bytes) : throw new InvalidDataException(), 4);
    private static readonly RPCMethod<Actor, int> Ping = new(80, static (n, v) => n.RPCHits += v, static _ => 4, static (v, b) => { BinaryPrimitives.WriteInt32LittleEndian(b, v); return 4; }, static b => b.Length == 4 ? BinaryPrimitives.ReadInt32LittleEndian(b) : throw new InvalidDataException());
    private static SceneReplicationConfig Config = null!;
    private sealed class Actor : Node
    {
        internal int Health = 1, Tag = 9, ReadyHealth, SyncCount, DeltaCount, RPCHits;
        internal MultiplayerSynchronizer Sync => GetNode<MultiplayerSynchronizer>("Sync");
        internal Actor(bool children = true) { Name = "Actor"; RPCConfig(Ping, new RPCOptions(RPCMode.AnyPeer)); if (children) AddChild(new MultiplayerSynchronizer { Name = "Sync", ReplicationConfig = Config }); }
        protected override void OnReady() { ReadyHealth = Health; Sync.Synchronized += () => SyncCount++; Sync.DeltaSynchronized += () => DeltaCount++; }
        protected override Func<Node> CreateSceneInstanceFactory() => CreateInstance;
        private static Node CreateInstance() => new Actor(false);
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() { foreach (var p in base.GetPropertyDescriptors()) yield return p; yield return new PropertyDescriptor<Actor, int>("Health", n => n.Health, (n, v) => n.Health = v, _ => 1, stored: true); yield return new PropertyDescriptor<Actor, int>("Tag", n => n.Tag, (n, v) => n.Tag = v, _ => 9, stored: true); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Wait(Action poll, Func<bool> ready) { var end = Environment.TickCount64 + 10000; while (!ready()) { poll(); if (Environment.TickCount64 > end) throw new TimeoutException(); Thread.Yield(); } }
    private static int Port() { using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)); return ((IPEndPoint)socket.LocalEndPoint!).Port; }
    private static WebSocketMultiplayerPeer Peer() => new() { InboundBufferSize = 8192, OutboundBufferSize = 8192, MaxQueuedPackets = 128 };
    private static Node Branch(string name, out MultiplayerSpawner spawner)
    { var root = new Node { Name = name }; var actors = new Node { Name = "Actors" }; root.AddChild(actors); spawner = new MultiplayerSpawner { Name = "Spawner", SpawnPath = "../Actors", SpawnFunction = new SpawnFactory<int>(44, static value => new Actor { Health = value }, static _ => 4, static (value, bytes) => { BinaryPrimitives.WriteInt32LittleEndian(bytes, value); return 4; }, static bytes => bytes.Length == 4 ? BinaryPrimitives.ReadInt32LittleEndian(bytes) : throw new InvalidDataException()) }; root.AddChild(spawner); return root; }
    internal static void Run() { Configuration(); BatchesAndValidation(); Native(false); Native(true); Console.WriteLine("Typed scene replication custom/automatic spawn, pre-ready state, sync/delta/visibility, lifecycle and allocation checks passed."); }
    private static void Configuration()
    {
        using var c = new SceneReplicationConfig(); c.AddProperty(Health); c.AddProperty(Tag, 0); Check(c.GetProperties().Length == 2 && c.GetProperties()[0] == Tag && c.PropertyGetSpawn(Health) && c.PropertyGetSync(Health), "Ordered config defaults."); Reject<ArgumentException>(() => c.AddProperty(Health)); c.PropertySetWatch(Health, true); c.PropertySetSync(Health, false); Check(c.PropertyGetWatch(Health), "Legacy sync false preserves OnChange."); c.PropertySetWatch(Health, false); Check(c.PropertyGetReplicationMode(Health) == ReplicationMode.Never, "Legacy watch disable."); c.PropertySetSpawn(Health, false); using var copy = (SceneReplicationConfig)c.Duplicate(); c.RemoveProperty(Health); Check(copy.HasProperty(Health) && !copy.PropertyGetSpawn(Health) && !c.HasProperty(Health), "Config duplicate copies policies and shares immutable tokens."); Reject<KeyNotFoundException>(() => c.PropertyGetSpawn(Health)); c.ResetState(); Check(c.GetProperties().Length == 0, "Config reset."); using var api = new SceneMultiplayer(); Check(api.MaxSyncPacketSize == 1350 && api.MaxDeltaPacketSize == 65535, "Batch budget defaults."); Reject<ArgumentOutOfRangeException>(() => api.MaxSyncPacketSize = 127); using var s = new MultiplayerSynchronizer(); Check(s.PublicVisibility && s.RootPath == ".." && s.VisibilityUpdateMode == VisibilityUpdateMode.Idle, "Synchronizer defaults."); s.PublicVisibility = false; s.SetVisibilityFor(2, true); Check(!s.GetVisibilityFor(0) && s.GetVisibilityFor(2), "Explicit visibility membership."); Reject<ArgumentOutOfRangeException>(() => s.DeltaInterval = double.NaN);
    }
    private sealed class WirePeer : MultiplayerPeer
    {
        internal readonly List<(byte[] Bytes, TransferMode Mode)> Sent = [];
        internal readonly Queue<byte[]> Incoming = [];
        internal void Admit(int id) => EmitPeerConnected(id);
        public override void Poll() { CheckPacketPeer(); }
        public override int GetUniqueID() => 1;
        public override MultiplayerConnectionStatus GetConnectionStatus() => MultiplayerConnectionStatus.Connected;
        public override bool IsServer() => true;
        public override int GetAvailablePacketCount() => Incoming.Count;
        protected override int NextPacketSize() => Incoming.Peek().Length;
        protected override void ReadPacketCore(Span<byte> bytes) => Incoming.Dequeue().CopyTo(bytes);
        public override int GetMaxPacketSize() => 8192;
        public override int GetPacketPeer() => 2;
        public override int GetPacketChannel() => 0;
        public override TransferMode GetPacketMode() => TransferMode.Reliable;
        public override void SetTargetPeer(int id) { }
        public override void PutPacket(ReadOnlySpan<byte> bytes) => Sent.Add((bytes.ToArray(), TransferMode));
        public override void Close() { }
        public override void DisconnectPeer(int id, bool force = false) { }
    }
    private static byte[] StatePacket(string path, byte command, uint sequence, params (uint ID, byte[] Data)[] values)
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); var name = System.Text.Encoding.UTF8.GetBytes(path); writer.Write(command); writer.Write(sequence); writer.Write((ushort)name.Length); writer.Write(name); writer.Write((ushort)values.Length); foreach (var value in values) { writer.Write(value.ID); writer.Write((uint)value.Data.Length); writer.Write(value.Data); }
        return stream.ToArray();
    }
    private static byte[] IntBytes(int value) { var bytes = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(bytes, value); return bytes; }
    private static void BatchesAndValidation()
    {
        using var config = new SceneReplicationConfig(); Config = config; config.AddProperty(Health); config.AddProperty(Tag); config.PropertySetReplicationMode(Tag, ReplicationMode.OnChange);
        using var peer = new WirePeer(); using var api = new SceneMultiplayer { MultiplayerPeer = peer, MaxSyncPacketSize = 128, MaxDeltaPacketSize = 128 }; var root = Branch("Root", out var spawner); using var tree = new SceneTree(root); tree.SetMultiplayer(api); peer.Admit(2); var actors = new Actor[4]; for (var i = 0; i < 4; i++) actors[i] = (Actor)spawner.Spawn(i + 1); peer.Sent.Clear(); tree.ProcessFrame(0);
        Check(peer.Sent.Count(p => p.Bytes[0] == 6) >= 2 && peer.Sent.Count(p => p.Bytes[0] == 22) >= 2 && peer.Sent.Where(p => p.Bytes[0] is 6 or 22).All(p => p.Bytes.Length <= 128), "Whole synchronizer groups batch at actual configured byte limits."); Check(peer.Sent.Where(p => p.Bytes[0] == 6).All(p => p.Mode == TransferMode.Unreliable) && peer.Sent.Where(p => p.Bytes[0] == 22).All(p => p.Mode == TransferMode.Reliable), "Always/delta request actual distinct delivery modes.");
        var actor = actors[0]; actor.Sync.SetMultiplayerAuthority(2, false); config.PropertySetReplicationMode(Health, ReplicationMode.OnChange); peer.Incoming.Enqueue(StatePacket("Actors/Actor/Sync", 22, 1, (1, IntBytes(999)), (2, new byte[3]))); Reject<AggregateException>(() => tree.ProcessFrame(0)); Check(actor.Health == 1, "Malformed second codec preserves all predecoded property state."); peer.Incoming.Enqueue(StatePacket("Actors/Actor/Sync", 22, 2, (2, IntBytes(500)))); tree.ProcessFrame(0); Check(actor.Tag == 500, "Independent little-endian schema payload applies after invalid input.");
        config.PropertySetReplicationMode(Health, ReplicationMode.Always); peer.Incoming.Enqueue(StatePacket("Actors/Actor/Sync", 6, 100, (1, IntBytes(7)))); peer.Incoming.Enqueue(StatePacket("Actors/Actor/Sync", 6, 99, (1, IntBytes(8)))); tree.ProcessFrame(0); Check(actor.Health == 7, "Old periodic sequence is ignored."); actor.Sync.SetMultiplayerAuthority(3, false); peer.Incoming.Enqueue(StatePacket("Actors/Actor/Sync", 22, 3, (2, IntBytes(501)))); Reject<AggregateException>(() => tree.ProcessFrame(0)); Check(actor.Tag == 500, "Wrong authority rejects property state."); tree.SetMultiplayer(null);
    }
    private static void Native(bool secure)
    {
        using var config = new SceneReplicationConfig(); Config = config; config.AddProperty(Health); config.AddProperty(Tag); config.PropertySetReplicationMode(Tag, ReplicationMode.OnChange);
        using var crypto = new Crypto(); using var key = secure ? crypto.GenerateRSA(2048) : null; using var cert = secure ? crypto.GenerateSelfSignedCertificate(key!, "CN=localhost,O=Electron2D,C=RU") : null; using var serverTLS = secure ? TLSOptions.Server(key!, cert!) : null; using var clientTLS = secure ? TLSOptions.Client(cert!, "localhost") : null;
        using var host = Peer(); using var a = Peer(); using var b = Peer(); var port = Port(); host.CreateServer(port, "127.0.0.1", serverTLS); var url = (secure ? "wss" : "ws") + "://127.0.0.1:" + port; a.CreateClient(url, clientTLS); b.CreateClient(url, clientTLS); using var server = new SceneMultiplayer { MultiplayerPeer = host }; using var ca = new SceneMultiplayer { MultiplayerPeer = a }; using var cb = new SceneMultiplayer { MultiplayerPeer = b };
        var root = Branch("Root", out var hs); var ar = Branch("A", out var asp); var br = Branch("B", out var bsp); root.AddChild(ar); root.AddChild(br); using var tree = new SceneTree(root); tree.SetMultiplayer(server); tree.SetMultiplayer(ca, ar.GetPath()); tree.SetMultiplayer(cb, br.GetPath()); void Poll() => tree.ProcessFrame(0);
        Wait(Poll, () => server.GetPeers().Length == 2 && ca.GetPeers().Length == 2 && cb.GetPeers().Length == 2); var spawned = 0; var despawned = 0; asp.Spawned += _ => spawned++; asp.Despawned += _ => despawned++;
        var local = (Actor)hs.Spawn(42); local.Health = 73; local.Tag = 11; Wait(Poll, () => ar.GetNode("Actors").Children.Count == 1 && br.GetNode("Actors").Children.Count == 1); var ra = ar.GetNode<Actor>("Actors/Actor"); var rb = br.GetNode<Actor>("Actors/Actor"); Check(ra.ReadyHealth == 42 && ra.Health == 73 && ra.Tag == 11 && rb.Health == 73 && spawned == 1, "Custom spawn applies initial state before ready and subsequent states.");
        var delta = ra.DeltaCount; for (var i = 0; i < 8; i++) Poll(); Check(ra.DeltaCount == delta, "Unchanged properties do not emit delta state."); local.Tag = 99; Wait(Poll, () => ra.Tag == 99 && rb.Tag == 99); Check(ra.DeltaCount > delta, "OnChange routes changed values.");
        local.Sync.PublicVisibility = false; local.Sync.SetVisibilityFor(b.GetUniqueID(), true); Wait(Poll, () => ar.GetNode("Actors").Children.Count == 0 && br.GetNode("Actors").Children.Count == 1); Check(despawned == 1, "Hidden peer despawns while explicitly visible peer remains."); rb = br.GetNode<Actor>("Actors/Actor"); Reject<InvalidOperationException>(() => local.RPCID(a.GetUniqueID(), Ping, 1)); local.RPC(Ping, 2); Wait(Poll, () => rb.RPCHits == 2); local.Health = 81; local.Tag = 101; local.Sync.SetVisibilityFor(a.GetUniqueID(), true); Wait(Poll, () => ar.GetNode("Actors").Children.Count == 1); ra = ar.GetNode<Actor>("Actors/Actor"); Check(ra.ReadyHealth == 81 && ra.Tag == 101, "Visibility replay uses current pre-ready spawn state.");
        for (var i = 0; i < 32; i++) { local.Health++; var expected = local.Health; Wait(Poll, () => ra.Health == expected && rb.Health == expected); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { local.Health++; local.Tag++; var expected = local.Health; var end = Environment.TickCount64 + 10000; while (ra.Health != expected || rb.Health != expected) { Poll(); if (Environment.TickCount64 > end) throw new TimeoutException(); } }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared active replication allocates zero managed bytes."); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Poll(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared replication idle allocates zero managed bytes."); Console.WriteLine("64 " + (secure ? "WSS" : "WS") + " active sync/delta and idle scene cycles: 0 managed bytes.");
        local.Sync.PublicVisibility = true; using var late = Peer(); using var lateAPI = new SceneMultiplayer(); var lr = Branch("Late", out var lsp); root.AddChild(lr); tree.SetMultiplayer(lateAPI, lr.GetPath()); late.CreateClient(url, clientTLS); lateAPI.MultiplayerPeer = late; Wait(Poll, () => lr.GetNodeOrNull("Actors/Actor") is not null); Check(lr.GetNode<Actor>("Actors/Actor").ReadyHealth == local.Health, "Late admission replays current spawn state."); late.Close(); Wait(Poll, () => server.GetPeers().Length == 2); tree.SetMultiplayer(null, lr.GetPath()); lr.Dispose();
        using var proto = new Actor { Health = 55 }; using var packed = new PackedScene(); proto.Sync.Owner = proto; packed.Pack(proto); hs.AddSpawnableScene(packed); asp.AddSpawnableScene(packed); bsp.AddSpawnableScene(packed); var automatic = (Actor)packed.Instantiate(); automatic.Name = "Automatic"; root.GetNode("Actors").AddChild(automatic); Wait(Poll, () => ar.GetNodeOrNull("Actors/Automatic") is not null); Check(ar.GetNode<Actor>("Actors/Automatic").ReadyHealth == 55, "PackedScene provenance drives automatic spawn."); hs.ClearSpawnableScenes(); Check(ar.GetNodeOrNull("Actors/Automatic") is not null, "Clearing templates preserves existing replicas."); automatic.Dispose(); Wait(Poll, () => ar.GetNodeOrNull("Actors/Automatic") is null);
        hs.SpawnLimit = 1; Reject<InvalidOperationException>(() => hs.Spawn(1)); Action<Node> failure = _ => throw new InvalidOperationException("despawn observer"); asp.Despawned += failure; var observed = false; local.Dispose(); Wait(() => { try { Poll(); } catch (AggregateException) { observed = true; } }, () => ar.GetNode("Actors").Children.Count == 0 && br.GetNode("Actors").Children.Count == 0); Check(despawned == 3 && observed, "Despawn callback failure still disposes all remote instances and later peers."); var again = hs.Spawn(777); Wait(Poll, () => ar.GetNode("Actors").Children.Count == 1); Reject<AggregateException>(() => ca.MultiplayerPeer = null); Check(ca.MultiplayerPeer is null && ca.GetPeers().Length == 0 && ar.GetNode("Actors").Children.Count == 0, "Replacement commits and cleans remote state despite observer failure."); asp.Despawned -= failure; a.Close(); Wait(Poll, () => server.GetPeers().Length == 1); again.Dispose(); Wait(Poll, () => br.GetNode("Actors").Children.Count == 0); tree.SetMultiplayer(null, ar.GetPath()); tree.SetMultiplayer(null, br.GetPath()); tree.SetMultiplayer(null);
    }
}
