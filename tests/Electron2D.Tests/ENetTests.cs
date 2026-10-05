using Electron2D;
internal static class ENetTests
{
    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private static void Wait(Action poll, Func<bool> ready) { var end = Environment.TickCount64 + 10000; while (!ready()) { poll(); if (Environment.TickCount64 > end) throw new TimeoutException("ENet progress timed out."); } }
    public static void Run()
    {
        foreach (var address in new[] { "127.0.0.1", "::1" })
            foreach (var codec in Enum.GetValues<ENetCompressionMode>()) Exchange(address, codec);
        Secure(); Multiplayer(); Mesh(); Boundaries(); Scene(); Oracle(); RefusalAndLoss(); Callbacks(); SceneReplicationTests.RunENet();
        Console.WriteLine("ENet IPv4/IPv6, all five codecs, channel flags and fragmentation passed.");
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Callbacks()
    {
        using var server = new ENetMultiplayerPeer(); using var client = new ENetMultiplayerPeer(); server.SetBindIP("127.0.0.1"); server.CreateServer(0, 2, 2); client.CreateClient("127.0.0.1", server.Host!.GetLocalPort(), 2); var fanout = 0; var failed = false; ENetPacketPeer? borrowed = null;
        server.PeerConnected += id => { Check(server.GetPeer(id) is not null, "Connection event sees committed peer."); Reject<InvalidOperationException>(server.Poll); throw new IOException("Fixture observer failure."); };
        server.PeerConnected += id => { borrowed = server.GetPeer(id); server.Close(); server.CreateServer(0, 2, 2); fanout++; };
        server.PeerConnected += _ => fanout++;
        Wait(() => { try { server.Poll(); } catch (AggregateException) { failed = true; } client.Poll(); }, () => failed);
        Check(fanout == 2 && server.GetConnectionStatus() == MultiplayerConnectionStatus.Connected && borrowed is { IsDisposed: false } && !borrowed.IsActive(), "Callback failures/close/recreation preserve complete committed lifecycle.");
    }
    private static void RefusalAndLoss()
    {
        using var server = new ENetConnection(); using var client = new ENetConnection(); server.CreateHostBound("127.0.0.1", 0, 2, 2); client.CreateHost(1, 2); Check(client.GetLocalPort() > 0, "CreateHost binds an automatic port immediately.");
        using var proxy = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp); proxy.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0)); proxy.Blocking = false;
        var destination = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, server.GetLocalPort()); var clientAddress = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, client.GetLocalPort()); var port = ((System.Net.IPEndPoint)proxy.LocalEndPoint!).Port; var peer = client.ConnectToHost("127.0.0.1", port, 2); ENetPacketPeer? remote = null; var drop = false; var dropped = 0; var buffer = new byte[65536];
        void Poll() { while (proxy.Poll(0, System.Net.Sockets.SelectMode.SelectRead)) { System.Net.EndPoint source = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0); var size = proxy.ReceiveFrom(buffer, ref source); if (((System.Net.IPEndPoint)source).Port == clientAddress.Port) { if (drop) { drop = false; dropped++; } else proxy.SendTo(buffer.AsSpan(0, size), destination); } else proxy.SendTo(buffer.AsSpan(0, size), clientAddress); } var e = server.Service(); if (e.Type == ENetEventType.Connect) remote = e.Peer; client.Service(); }
        Wait(Poll, () => remote is not null && peer.GetState() == ENetPeerState.Connected); peer.PingInterval(0); peer.ThrottleConfigure(4000, 3, 4); peer.SetTimeout(32, 5000, 30000); Check(peer.GetStatistic(ENetPeerStatistic.PacketThrottleAcceleration) == 3 && peer.GetStatistic(ENetPeerStatistic.PacketThrottleDeceleration) == 4 && peer.GetStatistic(ENetPeerStatistic.PacketThrottleInterval) == 4000, "Native tuning/statistics."); Reject<ArgumentException>(() => peer.SetTimeout(32, 30001, 30000)); Reject<ArgumentOutOfRangeException>(() => peer.Send(2, [], ENetPacketFlags.Reliable)); Reject<ArgumentOutOfRangeException>(() => peer.Send(0, [], (ENetPacketFlags)4));
        peer.Ping(); for (var i = 0; i < 16; i++) Poll(); drop = true; peer.PutPacket("retry"u8); Wait(Poll, () => remote!.GetAvailablePacketCount() > 0); Check(dropped == 1 && remote!.GetPacket().AsSpan().SequenceEqual("retry"u8), "Reliable packet retries after datagram loss.");
        server.RefuseNewConnections(true); using var refused = new ENetConnection(); refused.CreateHost(1, 2); var pending = refused.ConnectToHost("127.0.0.1", server.GetLocalPort(), 2); pending.SetTimeout(1, 10, 60); Wait(() => { Poll(); refused.Service(); }, () => !pending.IsActive()); Check(server.GetPeers().Length == 1, "Refusal rejects a new endpoint and preserves existing connection.");
        server.Broadcast(0, "broadcast"u8, ENetPacketFlags.Reliable); server.Flush(); Wait(Poll, () => peer.GetAvailablePacketCount() > 0); Check(peer.GetPacket().AsSpan().SequenceEqual("broadcast"u8), "Host broadcast/flush.");
        for (var i = 0; i < 65536; i++) peer.Send(0, [], ENetPacketFlags.Reliable); Reject<IOException>(() => peer.Send(0, [], ENetPacketFlags.Reliable)); peer.Reset(); Check(!peer.IsActive() && peer.GetState() == ENetPeerState.Disconnected, "Reset detaches and releases native send budget.");
    }
    private static void Scene()
    {
        using var server = new ENetMultiplayerPeer(); using var client = new ENetMultiplayerPeer(); server.SetBindIP("127.0.0.1"); server.CreateServer(0, 2, 2); client.CreateClient("127.0.0.1", server.Host!.GetLocalPort(), 2); using var hs = new SceneMultiplayer { MultiplayerPeer = server }; using var cs = new SceneMultiplayer { MultiplayerPeer = client }; var root = new Node { Name = "Root" }; var branch = new Node { Name = "Client" }; root.AddChild(branch); using var tree = new SceneTree(root); tree.SetMultiplayer(hs); tree.SetMultiplayer(cs, branch.GetPath());
        void Poll() => tree.ProcessFrame(0); Wait(Poll, () => hs.GetPeers().Length == 1 && cs.GetPeers().Length == 1); var received = 0; hs.PeerPacket += (id, data) => { Check(id == client.GetUniqueID() && data.SequenceEqual("scene"u8), "Scene bytes preserve source and payload."); received++; };
        foreach (var mode in Enum.GetValues<TransferMode>()) { cs.SendBytes("scene"u8, 1, mode, 2); var target = received + 1; Wait(Poll, () => received == target); }
        tree.SetMultiplayer(null, branch.GetPath()); tree.SetMultiplayer(null);
    }
    private static void Oracle()
    {
        var path = Environment.GetEnvironmentVariable("ELECTRON2D_ENET_ORACLE"); if (path is null) return;
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { RedirectStandardOutput = true, RedirectStandardError = true })!;
        try { var ready = process.StandardOutput.ReadLineAsync(); Wait(() => { }, () => ready.IsCompleted); var port = int.Parse(ready.GetAwaiter().GetResult()!); using var host = new ENetConnection(); host.CreateHost(1, 3); var peer = host.ConnectToHost("127.0.0.1", port, 3); Wait(() => host.Service(), () => peer.GetState() == ENetPeerState.Connected); peer.Send(2, "stock native ENet"u8, ENetPacketFlags.Reliable); Wait(() => host.Service(), () => peer.GetAvailablePacketCount() > 0); Check(peer.GetPacket().AsSpan().SequenceEqual("stock native ENet"u8), "Stock ENet socket backend interoperates with routing-token adapter."); peer.PeerDisconnect(); Wait(() => host.Service(), () => process.HasExited); Check(process.ExitCode == 0, "Native oracle exits cleanly."); }
        finally { if (!process.HasExited) { process.Kill(true); process.WaitForExit(); } }
    }
    private static void Secure()
    {
        using var crypto = new Crypto(); using var key = crypto.GenerateRSA(2048); using var cert = crypto.GenerateSelfSignedCertificate(key, "CN=localhost,O=Electron2D,C=RU"); using var serverOptions = TLSOptions.Server(key, cert); using var clientOptions = TLSOptions.Client(cert, "localhost");
        using var server = new ENetConnection(); using var client = new ENetConnection(); server.CreateHostBound("127.0.0.1", 0, 4, 2); client.CreateHost(1, 2); server.DTLSServerSetup(serverOptions); client.DTLSClientSetup("localhost", clientOptions); var peer = client.ConnectToHost("127.0.0.1", server.GetLocalPort(), 2); ENetPacketPeer? remote = null;
        void Poll() { var a = server.Service(); if (a.Type == ENetEventType.Connect) remote = a.Peer; client.Service(); }
        Wait(Poll, () => remote is not null && peer.GetState() == ENetPeerState.Connected); var data = new byte[12000]; Array.Fill(data, (byte)23); peer.Send(1, data, ENetPacketFlags.Reliable); Wait(Poll, () => remote!.GetAvailablePacketCount() > 0); Check(remote!.GetPacket().AsSpan().SequenceEqual(data), "DTLS ENet fragment exchange.");
    }
    private static void Multiplayer()
    {
        using var host = new ENetMultiplayerPeer(); using var a = new ENetMultiplayerPeer(); using var b = new ENetMultiplayerPeer(); host.SetBindIP("127.0.0.1"); host.CreateServer(0, 4, 3, 1000000, 1000000); var port = host.Host!.GetLocalPort(); Check(host.Host.GetMaxChannels() == 5, "Server channel argument reaches the channel limit."); a.CreateClient("127.0.0.1", port, 3); b.CreateClient("127.0.0.1", port, 3);
        void Poll() { host.Poll(); a.Poll(); b.Poll(); }
        Wait(Poll, () => a.GetConnectionStatus() == MultiplayerConnectionStatus.Connected && b.GetConnectionStatus() == MultiplayerConnectionStatus.Connected && host.GetPeer(a.GetUniqueID()) is not null && host.GetPeer(b.GetUniqueID()) is not null);
        Span<byte> scratch = stackalloc byte[128];
        foreach (var mode in Enum.GetValues<TransferMode>()) foreach (var channel in new[] { 0, 3 }) { a.TransferMode = mode; a.TransferChannel = channel; a.PutPacket("packet"u8); Wait(Poll, () => host.GetAvailablePacketCount() > 0); Check(host.GetPacketPeer() == a.GetUniqueID() && host.GetPacketChannel() == channel && host.GetPacketMode() == mode, "Metadata maps actual native flags/channel."); Check(host.GetPacket(scratch) == 6, "Span packet."); }
        host.SetTargetPeer(-a.GetUniqueID()); host.PutPacket("except"u8); Wait(Poll, () => b.GetAvailablePacketCount() > 0); Check(a.GetAvailablePacketCount() == 0 && b.GetPacket(scratch) == 6, "Negative exclusion."); host.SetTargetPeer(a.GetUniqueID());
        for (var i = 0; i < 32; i++) Cycle(host, a, scratch); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Cycle(host, a, scratch); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Ready ENet active cycles allocate no managed bytes."); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Poll(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Ready ENet idle cycles allocate no managed bytes.");
        host.RefuseNewConnections = true; a.TransferChannel = 0; a.PutPacket("existing"u8); Wait(Poll, () => host.GetAvailablePacketCount() > 0); Check(host.GetPacket(scratch) == 8, "Refusal preserves current peers.");
        var events = 0; host.PeerDisconnected += id => { Check(host.GetPeer(id) is null, "Event sees committed removal."); events++; }; host.DisconnectPeer(b.GetUniqueID()); Wait(Poll, () => events == 1 && b.GetConnectionStatus() == MultiplayerConnectionStatus.Disconnected); host.DisconnectPeer(a.GetUniqueID(), true); Check(events == 1, "Forced local disconnect suppresses signal.");
    }
    private static void Cycle(ENetMultiplayerPeer host, ENetMultiplayerPeer client, Span<byte> scratch)
    {
        host.PutPacket("ping"u8); var end = Environment.TickCount64 + 10000; while (client.GetAvailablePacketCount() == 0) { host.Poll(); client.Poll(); if (Environment.TickCount64 > end) throw new TimeoutException(); }
        Check(client.GetPacket(scratch) == 4, "Prepared packet.");
    }
    private static void Mesh()
    {
        using var server = new ENetConnection(); using var client = new ENetConnection(); server.CreateHostBound("127.0.0.1", 0, 1, 4); client.CreateHost(1, 4); var peer = client.ConnectToHost("127.0.0.1", server.GetLocalPort(), 4); ENetPacketPeer? incoming = null; Wait(() => { var e = server.Service(); if (e.Type == ENetEventType.Connect) incoming = e.Peer; client.Service(); }, () => incoming is not null && peer.GetState() == ENetPeerState.Connected);
        using var a = new ENetMultiplayerPeer(); using var b = new ENetMultiplayerPeer(); a.CreateMesh(20); b.CreateMesh(30); a.AddMeshPeer(30, server); b.AddMeshPeer(20, client); Check(a.Host is null && !a.IsServer() && !a.IsServerRelaySupported(), "Mesh role."); a.SetTargetPeer(30); a.PutPacket("mesh"u8); Wait(() => { a.Poll(); b.Poll(); }, () => b.GetAvailablePacketCount() > 0); Check(b.GetPacketPeer() == 20 && b.GetPacket().AsSpan().SequenceEqual("mesh"u8), "Mesh source."); a.Close(); Check(!server.IsDisposed, "Borrowed mesh object stays undisposed."); Reject<InvalidOperationException>(() => server.Service());
    }
    private static void Boundaries()
    {
        using var host = new ENetConnection(); Reject<ArgumentOutOfRangeException>(() => host.CreateHost(0)); host.CreateHostBound("127.0.0.1", 0, 1, 2); Reject<ArgumentOutOfRangeException>(() => host.ChannelLimit(256)); Reject<ArgumentOutOfRangeException>(() => host.Compress((ENetCompressionMode)99)); Reject<InvalidOperationException>(() => Task.Run(host.Dispose).GetAwaiter().GetResult()); Check(!host.IsDisposed, "Wrong owner preserves host."); using var raw = new PacketPeerUDP(); raw.Bind(0, "127.0.0.1"); host.SocketSend("127.0.0.1", raw.GetLocalPort(), "raw"u8); Wait(() => { }, () => raw.GetAvailablePacketCount() > 0); Check(raw.GetPacket().AsSpan().SequenceEqual("raw"u8), "Raw transport datagram."); host.Destroy(); host.CreateHost(1, 2); host.Destroy();
    }
    private static void Exchange(string address, ENetCompressionMode codec)
    {
        Console.WriteLine($"ENet {address}, {codec}: connect.");
        using var server = new ENetConnection(); using var client = new ENetConnection();
        server.CreateHostBound(address, 0, 4, 3); client.CreateHost(1, 3);
        server.Compress(codec); client.Compress(codec); var outgoing = client.ConnectToHost(address, server.GetLocalPort(), 3, 123);
        ENetPacketPeer? incoming = null; var connected = false;
        void Poll() { var a = server.Service(); if (a.Type == ENetEventType.Connect) { Check(a.Data == 123, "Connection data."); incoming = a.Peer; } var b = client.Service(); if (b.Type == ENetEventType.Connect) connected = true; }
        Wait(Poll, () => connected && incoming is not null);
        Check(outgoing.GetChannels() == 3 && incoming!.GetChannels() == 3, "Negotiated channels.");
        Check(outgoing.GetRemotePort() == server.GetLocalPort() && incoming!.GetRemotePort() == client.GetLocalPort(), "Ports.");
        var data = new byte[20000]; for (var i = 0; i < data.Length; i++) data[i] = (byte)(i % 16);
        foreach (var flags in new[] { ENetPacketFlags.Reliable, ENetPacketFlags.None, ENetPacketFlags.Unsequenced | ENetPacketFlags.UnreliableFragment })
        {
            Console.WriteLine($"ENet {address}, {codec}: 20000-byte {flags} fragments.");
            outgoing.Send(2, data, flags); Wait(Poll, () => incoming!.GetAvailablePacketCount() > 0); Check((incoming!.GetPacketFlags() & ENetPacketFlags.Reliable) != 0 == (flags == ENetPacketFlags.Reliable || flags == ENetPacketFlags.None), "Next packet reliability flags."); Reject<ArgumentException>(() => incoming.GetPacket(new byte[1])); Check(incoming.GetAvailablePacketCount() == 1, "Undersized read preserves packet."); Check(incoming.GetPacket().AsSpan().SequenceEqual(data), "Fragmented channel packet.");
        }
        Console.WriteLine($"ENet {address}, {codec}: reply.");
        incoming!.PutPacket("answer"u8); Wait(Poll, () => outgoing.GetAvailablePacketCount() > 0); Check(outgoing.GetPacket().AsSpan().SequenceEqual("answer"u8), "Reply.");
        Check(server.PopStatistic(ENetHostStatistic.TotalReceivedData) > 0 && server.PopStatistic(ENetHostStatistic.TotalReceivedData) == 0, "Statistics reset.");
        Console.WriteLine($"ENet {address}, {codec}: disconnect.");
        outgoing.PeerDisconnect(42); var disconnected = false; Wait(() => { var e = server.Service(); if (e.Type == ENetEventType.Disconnect) { Check(e.Data == 42, "Disconnect data."); disconnected = true; } client.Service(); }, () => disconnected && !outgoing.IsActive());
        Check(!incoming!.IsActive(), "Disconnected peer detaches."); server.Destroy(); Check(!incoming!.IsDisposed && incoming.GetState() == ENetPeerState.Disconnected, "Destroy preserves borrowed peer object identity.");
    }
}
