using Electron2D;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Status = Electron2D.MultiplayerConnectionStatus;
using Mode = Electron2D.TransferMode;
using Certificate = Electron2D.X509Certificate;

internal static class MultiplayerTests
{
    internal static void Run() { OfflineAndExtension(); Native(false); Native(true); CallbacksAndLifecycle(); IndependentClient(); IdentityDeadline(); Console.WriteLine("Multiplayer identities/routing, WS/WSS server/client, independent binary ID wire, extension hooks, callbacks/lifetime and prepared packet checks passed."); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Wait(Func<bool> ready) { var deadline = Environment.TickCount64 + 10000; while (!ready()) { if (Environment.TickCount64 > deadline) throw new TimeoutException("Multiplayer check timed out."); Thread.Sleep(1); } }
    private static int Port() { var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start(); var port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop(); return port; }
    private sealed class CustomPeer : MultiplayerPeer
    {
        private readonly byte[] _data = new byte[64];
        private int _size = -1;
        internal int VirtualWrites;
        public override int TransferChannel { get => base.TransferChannel; set { base.TransferChannel = value; VirtualWrites++; } }
        public override Mode TransferMode { get => base.TransferMode; set { base.TransferMode = value; VirtualWrites++; } }
        public override bool RefuseNewConnections { get => base.RefuseNewConnections; set { base.RefuseNewConnections = value; VirtualWrites++; } }
        public override int GetAvailablePacketCount() { CheckPacketPeer(); return _size < 0 ? 0 : 1; }
        public override int GetMaxPacketSize() { CheckPacketPeer(); return _data.Length; }
        public override void PutPacket(ReadOnlySpan<byte> data) { CheckPacketPeer(); if (data.Length > _data.Length) throw new ArgumentException(); data.CopyTo(_data); _size = data.Length; }
        protected override int NextPacketSize() => _size;
        protected override void ReadPacketCore(Span<byte> destination) { _data.AsSpan(0, _size).CopyTo(destination); _size = -1; }
        public override void SetTargetPeer(int id) { CheckPacketPeer(); }
        public override int GetPacketPeer() { CheckPacketPeer(); return 42; }
        public override int GetPacketChannel() => TransferChannel;
        public override Mode GetPacketMode() => TransferMode;
        public override int GetUniqueID() { CheckPacketPeer(); return 2; }
        public override Status GetConnectionStatus() { CheckPacketPeer(); return Status.Connected; }
        public override bool IsServer() { CheckPacketPeer(); return false; }
        public override bool IsServerRelaySupported() { CheckPacketPeer(); return true; }
        public override void Poll() { CheckPacketPeer(); }
        public override void Close() { CheckPacketPeer(); _size = -1; }
        public override void DisconnectPeer(int peer, bool force = false) { CheckPacketPeer(); if (!force) EmitPeerDisconnected(peer); }
        internal void SignalConnected(int peer) => EmitPeerConnected(peer);
    }
    private static void Ignore(int _) { }
    private static void OfflineAndExtension()
    {
        using var offline = new OfflineMultiplayerPeer(); Check(offline.IsServer() && offline.GetUniqueID() == 1 && offline.GetConnectionStatus() == Status.Connected && !offline.IsServerRelaySupported(), "Offline local authority."); offline.PutPacket("discarded"u8); Check(offline.GetAvailablePacketCount() == 0 && offline.GetPacket().Length == 0 && offline.GetPacketError() == PacketReadStatus.OK && offline.GetMaxPacketSize() == 0, "Offline sink and empty read."); offline.Close(); offline.DisconnectPeer(42); offline.Poll(); Check(offline.GetConnectionStatus() == Status.Connected && offline.GetPacketPeer() == 0 && offline.GetPacketMode() == Mode.Reliable, "Offline close preserves server identity.");
        using var peer = new CustomPeer(); MultiplayerPeer facade = peer; facade.TransferChannel = 7; facade.TransferMode = Mode.UnreliableOrdered; facade.RefuseNewConnections = true; Check(peer.VirtualWrites == 3 && facade.GetPacketChannel() == 7 && facade.GetPacketMode() == Mode.UnreliableOrdered && facade.RefuseNewConnections, "Consumer overrides all typed settings."); facade.PutPacket("wire"u8); Reject<ArgumentException>(() => facade.GetPacket(new byte[3])); Span<byte> bytes = stackalloc byte[64]; Check(facade.GetPacket(bytes) == 4 && bytes[..4].SequenceEqual("wire"u8) && facade.GetPacketPeer() == 42 && facade.IsServerRelaySupported(), "Consumer span packet hooks.");
        var notified = 0; Action<int> throwing = _ => throw new InvalidOperationException("subscriber"); Action<int> later = _ => notified++; peer.PeerConnected += throwing; peer.PeerConnected += later; Reject<AggregateException>(() => peer.SignalConnected(42)); Check(notified == 1, "Every event subscriber runs despite earlier failure."); peer.PeerConnected -= throwing; peer.PeerConnected -= later; peer.PeerConnected += Ignore;
        var ids = new HashSet<int>(); for (var i = 0; i < 64; i++) { var id = facade.GenerateUniqueID(); Check(id > 1 && ids.Add(id), "Generated peer IDs are positive and distinct in this sample."); }
        Reject<ArgumentOutOfRangeException>(() => facade.TransferChannel = -1); Reject<ArgumentOutOfRangeException>(() => facade.TransferMode = (Mode)3); Reject<InvalidOperationException>(() => Task.Run(facade.Poll).GetAwaiter().GetResult()); Reject<InvalidOperationException>(() => Task.Run(facade.Dispose).GetAwaiter().GetResult()); Check(!facade.IsDisposed, "Owner validation precedes disposed state.");
        for (var i = 0; i < 32; i++) { peer.PutPacket("warm"u8); peer.GetPacket(bytes); peer.SignalConnected(42); peer.Poll(); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { peer.PutPacket("warm"u8); peer.GetPacket(bytes); peer.SignalConnected(42); peer.Poll(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Custom multiplayer packets and event dispatch allocate zero bytes."); Console.WriteLine("64 custom MultiplayerPeer packet/event cycles: 0 managed bytes.");
    }
    private sealed class Poller : Node
    {
        private readonly WebSocketMultiplayerPeer[] _peers;
        internal Poller(params WebSocketMultiplayerPeer[] peers) { _peers = peers; ProcessEnabled = true; }
        protected override void OnProcess(double delta) { foreach (var peer in _peers) peer.Poll(); }
    }
    private static WebSocketMultiplayerPeer Peer(int bytes = 1024, int packets = 8) => new() { InboundBufferSize = bytes, OutboundBufferSize = bytes, MaxQueuedPackets = packets, SupportedProtocols = ["game"], HandshakeHeaders = ["X-Game: multiplayer"] };
    private static void Native(bool secure)
    {
        using var key = RSA.Create(2048); var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1); request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true)); var names = new SubjectAlternativeNameBuilder(); names.AddDnsName("localhost"); request.CertificateExtensions.Add(names.Build()); using var identity = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1)); using var chain = new Certificate(); chain.LoadFromString(identity.ExportCertificatePem()); using var privateKey = new CryptoKey(); privateKey.LoadFromString(key.ExportPkcs8PrivateKeyPem()); using var serverTLS = TLSOptions.Server(privateKey, chain); using var clientTLS = TLSOptions.Client(chain);
        using var server = Peer(); using var a = Peer(); using var b = Peer(); var port = Port(); server.CreateServer(port, "127.0.0.1", secure ? serverTLS : null); var url = (secure ? "wss" : "ws") + "://localhost:" + port + "/game"; var owner = Environment.CurrentManagedThreadId; var connected = new List<int>(); server.PeerConnected += id => { Check(Environment.CurrentManagedThreadId == owner && server.GetPeer(id) is not null, "Committed server event on owner."); connected.Add(id); }; var serverSeen = 0; a.PeerConnected += id => { Check(a.GetConnectionStatus() == Status.Connected && a.GetUniqueID() > 1 && id == 1, "Committed client identity."); serverSeen++; };
        a.CreateClient(url, secure ? clientTLS : null); b.CreateClient(url, secure ? clientTLS : null); using var root = new Node(); using var tree = new SceneTree(root); root.AddChild(new Poller(server, a, b)); Wait(() => { tree.ProcessFrame(.001); return a.GetConnectionStatus() == Status.Connected && b.GetConnectionStatus() == Status.Connected; }); var aID = a.GetUniqueID(); var bID = b.GetUniqueID(); Check(aID != bID && connected.Contains(aID) && connected.Contains(bID) && serverSeen == 1 && server.IsServer() && server.GetUniqueID() == 1 && server.IsServerRelaySupported(), "Server and two assigned clients."); Check(server.GetPeerAddress(aID) == "127.0.0.1" && server.GetPeerPort(aID) > 0 && a.GetPeerPort(1) == port && server.GetPeer(999) is null, "Borrowed endpoint queries.");
        a.TransferChannel = 19; a.TransferMode = Mode.Unreliable; a.PutPacket("first"u8); Wait(() => { tree.ProcessFrame(.001); return server.GetAvailablePacketCount() == 1; }); Check(server.GetPacketPeer() == aID && server.GetPacketChannel() == 0 && server.GetPacketMode() == Mode.Reliable, "Next packet metadata uses actual reliable single channel."); Reject<ArgumentException>(() => server.GetPacket(new byte[4])); Check(server.GetPacketPeer() == aID && Encoding.UTF8.GetString(server.GetPacket()) == "first", "Too-small reads preserve next source and payload.");
        server.SetTargetPeer(0); server.PutPacket("all"u8); Wait(() => { tree.ProcessFrame(.001); return a.GetAvailablePacketCount() == 1 && b.GetAvailablePacketCount() == 1; }); Check(Encoding.UTF8.GetString(a.GetPacket()) == "all" && Encoding.UTF8.GetString(b.GetPacket()) == "all", "Broadcast reaches both peers."); server.SetTargetPeer(-aID); server.PutPacket("except"u8); Wait(() => { tree.ProcessFrame(.001); return b.GetAvailablePacketCount() == 1; }); Check(a.GetAvailablePacketCount() == 0 && Encoding.UTF8.GetString(b.GetPacket()) == "except", "Negative target excludes one peer."); server.SetTargetPeer(aID); server.PutPacket("target"u8); Wait(() => { tree.ProcessFrame(.001); return a.GetAvailablePacketCount() == 1; }); Check(b.GetAvailablePacketCount() == 0 && Encoding.UTF8.GetString(a.GetPacket()) == "target", "Positive target selects one peer."); a.SetTargetPeer(bID); a.PutPacket("to server"u8); Wait(() => { tree.ProcessFrame(.001); return server.GetAvailablePacketCount() > 0; }); Check(server.GetPacketPeer() == aID && Encoding.UTF8.GetString(server.GetPacket()) == "to server" && b.GetAvailablePacketCount() == 0, "Client target never bypasses the relay consumer.");
        server.SetTargetPeer(999); Reject<ArgumentException>(() => server.PutPacket("absent"u8)); Reject<ArgumentOutOfRangeException>(() => server.SetTargetPeer(int.MinValue)); Reject<ArgumentException>(() => a.PutPacket(new byte[a.GetMaxPacketSize() + 1])); server.SetTargetPeer(aID);
        Span<byte> warm = stackalloc byte[4]; for (var i = 0; i < 32; i++) RoundTrip(server, a, warm); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) RoundTrip(server, a, warm); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared native multiplayer routing/read/poll allocates zero bytes."); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { server.Poll(); a.Poll(); b.Poll(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Idle native multiplayer polling allocates zero bytes."); Console.WriteLine("64 " + (secure ? "WSS" : "WS") + " MultiplayerPeer active and idle cycles: 0 managed bytes.");
        using var newcomer = Peer(); newcomer.HandshakeTimeout = .05; server.RefuseNewConnections = true; newcomer.CreateClient(url, secure ? clientTLS : null); Wait(() => { tree.ProcessFrame(.001); newcomer.Poll(); return newcomer.GetConnectionStatus() == Status.Disconnected; }); Check(connected.Count == 2 && server.GetConnectionStatus() == Status.Connected, "Refusal preserves existing peers and prevents admission."); server.RefuseNewConnections = false; newcomer.HandshakeTimeout = 3; newcomer.CreateClient(url, secure ? clientTLS : null); Wait(() => { tree.ProcessFrame(.001); newcomer.Poll(); return newcomer.GetConnectionStatus() == Status.Connected; }); server.GetPeer(newcomer.GetUniqueID())!.Dispose(); server.SetTargetPeer(0); Reject<AggregateException>(() => server.PutPacket("survives"u8)); Wait(() => { tree.ProcessFrame(.001); newcomer.Poll(); return newcomer.GetConnectionStatus() == Status.Disconnected && a.GetAvailablePacketCount() == 1 && b.GetAvailablePacketCount() == 1; }); Check(Encoding.UTF8.GetString(a.GetPacket()) == "survives" && Encoding.UTF8.GetString(b.GetPacket()) == "survives", "Failed broadcast recipient does not suppress later recipients.");
        var disconnected = new List<int>(); server.PeerDisconnected += disconnected.Add; server.DisconnectPeer(bID, true); Wait(() => { tree.ProcessFrame(.001); return b.GetConnectionStatus() == Status.Disconnected; }); Check(!disconnected.Contains(bID) && server.GetPeer(bID) is null, "Forced local disconnect suppresses its event."); server.DisconnectPeer(aID); Wait(() => { tree.ProcessFrame(.001); return a.GetConnectionStatus() == Status.Disconnected && server.GetPeer(aID) is null; }); Check(disconnected.Contains(aID), "Graceful disconnect completes and emits after detach."); server.Close(); Check(server.GetConnectionStatus() == Status.Disconnected && server.GetUniqueID() == 0, "Close clears identity/listener without more events."); server.CreateServer(port, "127.0.0.1", secure ? serverTLS : null); Check(server.GetConnectionStatus() == Status.Connected, "Listener restarts after owned close.");
    }
    private static void RoundTrip(WebSocketMultiplayerPeer server, WebSocketMultiplayerPeer client, Span<byte> bytes)
    {
        client.PutPacket("warm"u8); var deadline = Environment.TickCount64 + 10000; while (server.GetAvailablePacketCount() == 0) { server.Poll(); client.Poll(); if (Environment.TickCount64 > deadline) throw new TimeoutException(); Thread.Yield(); }
        Check(server.GetPacket(bytes) == 4 && bytes.SequenceEqual("warm"u8), "Server span packet."); server.PutPacket(bytes); while (client.GetAvailablePacketCount() == 0) { server.Poll(); client.Poll(); if (Environment.TickCount64 > deadline) throw new TimeoutException(); Thread.Yield(); }
        Check(client.GetPacket(bytes) == 4 && bytes.SequenceEqual("warm"u8), "Client span packet.");
    }
    private static void CallbacksAndLifecycle()
    {
        var port = Port(); using var server = Peer(64, 1); using var client = Peer(64, 1); server.CreateServer(port, "127.0.0.1"); client.CreateClient("ws://127.0.0.1:" + port); var called = 0; Action<int> throwing = _ => server.Poll(); server.PeerConnected += throwing; server.PeerConnected += _ => called++; var sawError = false;
        Wait(() => { try { server.Poll(); } catch (AggregateException) { sawError = true; } client.Poll(); return client.GetConnectionStatus() == Status.Connected; }); Check(called == 1 && sawError && server.GetPeer(client.GetUniqueID()) is not null, "Callback failure preserves committed connection and later subscriber."); server.PeerConnected -= throwing;
        client.PutPacket("one"u8); client.Poll(); Wait(() => { server.Poll(); client.Poll(); return server.GetAvailablePacketCount() == 1; }); client.PutPacket("two"u8); Wait(() => { server.Poll(); client.Poll(); return server.GetPeer(client.GetUniqueID())!.GetAvailablePacketCount() > 0; }); Check(Encoding.UTF8.GetString(server.GetPacket()) == "one", "Aggregate inbox pressure retains first."); server.Poll(); Check(Encoding.UTF8.GetString(server.GetPacket()) == "two", "Aggregate inbox resumes without packet loss.");
        var raw = server.GetPeer(client.GetUniqueID())!; raw.Disposed += _ => throw new InvalidOperationException("dispose subscriber"); Reject<AggregateException>(server.Close); Check(server.GetConnectionStatus() == Status.Disconnected && server.GetPeer(client.GetUniqueID()) is null && raw.IsDisposed, "Cleanup callback failures do not retain maps or sockets."); server.CreateServer(port, "127.0.0.1"); client.Close();
        Action<int> reset = _ => server.Close(); server.PeerConnected += reset; client.CreateClient("ws://127.0.0.1:" + port); Wait(() => { server.Poll(); client.Poll(); return server.GetConnectionStatus() == Status.Disconnected; }); Check(server.GetUniqueID() == 0, "Connection callback close invalidates old iteration."); server.PeerConnected -= reset;
        using var invalid = Peer(); invalid.InboundBufferSize = 3; Reject<InvalidOperationException>(() => invalid.CreateServer(Port(), "127.0.0.1")); invalid.InboundBufferSize = 64; invalid.MaxQueuedPackets = 0; Reject<InvalidOperationException>(() => invalid.CreateClient("ws://localhost:1")); Reject<ArgumentOutOfRangeException>(() => invalid.HandshakeTimeout = double.NaN); Check(invalid.GetConnectionStatus() == Status.Disconnected, "Impossible identity budgets reject before connecting.");
    }
    private static void IndependentClient()
    {
        var port = Port(); using var host = Peer(); host.SupportedProtocols = []; host.CreateServer(port, "127.0.0.1"); using var stop = new CancellationTokenSource(10000); var assigned = 0;
        var oracle = Task.Run(async () => { using var client = new ClientWebSocket(); await client.ConnectAsync(new Uri("ws://localhost:" + port + "/oracle"), stop.Token); var bytes = new byte[128]; var identity = await client.ReceiveAsync(bytes, stop.Token); Check(identity.Count == 4 && identity.EndOfMessage && identity.MessageType == WebSocketMessageType.Binary, "Independent peer receives a raw four-byte identity message."); assigned = BinaryPrimitives.ReadInt32LittleEndian(bytes); Check(assigned > 1, "Independent little-endian identity."); await client.SendAsync("external"u8.ToArray(), WebSocketMessageType.Binary, true, stop.Token); var reply = await client.ReceiveAsync(bytes, stop.Token); Check(bytes.AsSpan(0, reply.Count).SequenceEqual("reply"u8), "Application message has no invented routing envelope."); await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", stop.Token); });
        Wait(() => { host.Poll(); return host.GetAvailablePacketCount() > 0; }); Check(host.GetPacketPeer() == assigned && Encoding.UTF8.GetString(host.GetPacket()) == "external", "Independent message/source metadata."); host.SetTargetPeer(assigned); host.PutPacket("reply"u8); Wait(() => { host.Poll(); return oracle.IsCompleted; }); oracle.GetAwaiter().GetResult();
    }
    private static void IdentityDeadline()
    {
        foreach (var kind in new[] { "timeout", "small", "reserved", "text" })
        {
            using var stop = new CancellationTokenSource(10000); var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var oracle = Task.Run(async () => { using var socket = await listener.AcceptTcpClientAsync(stop.Token); using var stream = socket.GetStream(); var buffer = new byte[4096]; var count = 0; while (count < buffer.Length) { await stream.ReadExactlyAsync(buffer.AsMemory(count, 1), stop.Token); if (++count >= 4 && buffer.AsSpan(count - 4, 4).SequenceEqual("\r\n\r\n"u8)) break; } var headers = Encoding.ASCII.GetString(buffer, 0, count); var nonce = headers.Split("\r\n").Single(l => l.StartsWith("Sec-WebSocket-Key: "))[19..].Trim(); var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(nonce + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))); await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + accept + "\r\n\r\n"), stop.Token); if (kind != "timeout") { byte[] payload = kind == "small" ? [2, 0, 0] : kind == "reserved" ? [1, 0, 0, 0] : "aaaa"u8.ToArray(); byte[] frame = [(byte)(kind == "text" ? 0x81 : 0x82), (byte)payload.Length, .. payload]; await stream.WriteAsync(frame, stop.Token); } var read = await stream.ReadAsync(buffer, stop.Token); Check(read == 0, "Timed-out identity transport closes cleanly."); });
            using var client = Peer(); client.SupportedProtocols = []; client.HandshakeTimeout = .1; var events = 0; client.PeerConnected += _ => events++; client.CreateClient("ws://127.0.0.1:" + port); Wait(() => { client.Poll(); return client.GetConnectionStatus() == Status.Disconnected; }); Check(events == 0 && client.GetUniqueID() == 0 && client.GetPeer(1) is null, "Invalid/missing identity is rejected before client connection event: " + kind); oracle.GetAwaiter().GetResult(); listener.Stop();
        }
    }
}
