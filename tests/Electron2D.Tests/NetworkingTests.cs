using Electron2D;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

internal static class NetworkingTests
{
    internal static void Run()
    {
        Extensions(); Buffer(); TCP(); UDS(); UDP(); ReusableDatagramAddress(); DatagramPeerDeparture(); UDPConnections(); Multicast(); IPv6(); Scene(); FramedPackets();
        Console.WriteLine("Native TCP/UDP/UDS, binary wire encoding, polling, queue/lifetime boundaries and warm transport checks passed.");
    }
    private sealed class FragmentStream : StreamPeer
    {
        internal readonly StreamPeerBuffer Storage = new();
        internal int Reads, Writes;
        public override int GetAvailableBytes() { CheckStream(); return Storage.GetAvailableBytes(); }
        protected override int ReadCore(Span<byte> destination, bool block) { Reads++; return Storage.GetPartialData(destination[..Math.Min(1, destination.Length)]); }
        protected override int WriteCore(ReadOnlySpan<byte> data, bool block) { Writes++; return Storage.PutPartialData(data[..Math.Min(1, data.Length)]); }
        protected override void Dispose(bool disposing) { if (disposing) Storage.Dispose(); base.Dispose(disposing); }
    }
    private sealed class LocalPackets : PacketPeer
    {
        private byte[]? _packet;
        public override int GetAvailablePacketCount() { CheckPacketPeer(); return _packet is null ? 0 : 1; }
        public override int GetMaxPacketSize() { CheckPacketPeer(); return 4; }
        public override void PutPacket(ReadOnlySpan<byte> data) { CheckPacketPeer(); if (data.Length > GetMaxPacketSize()) throw new ArgumentException(); _packet = data.ToArray(); }
        protected override int NextPacketSize() => _packet?.Length ?? -1;
        protected override void ReadPacketCore(Span<byte> destination) { _packet!.CopyTo(destination); _packet = null; }
    }
    private static void Extensions()
    {
        using var stream = new FragmentStream(); stream.PutData(ReadOnlySpan<byte>.Empty); Check(stream.PutPartialData(ReadOnlySpan<byte>.Empty) == 0 && stream.Writes == 0, "Empty writes skip transport hooks.");
        stream.PutU32(0x12345678); stream.Storage.Seek(0); Check(stream.GetU32() == 0x12345678 && stream.Reads == 4 && stream.Writes == 4, "Consumer-defined fragment hooks fill full operations.");
        stream.Storage.Clear(); using var framed = new PacketPeerStream { StreamPeer = stream }; framed.PutPacket(new byte[] { 7, 8 }); stream.Storage.Seek(0);
        for (var i = 0; i < 5; i++) Check(framed.GetAvailablePacketCount() == 0, "Fragmented header/payload remains incomplete.");
        Check(framed.GetAvailablePacketCount() == 1 && framed.GetPacket().SequenceEqual(new byte[] { 7, 8 }), "Consumer-defined stream supports framed packet integration.");
        Check(framed.GetMaxPacketSize() == 65532, "Framed payload capacity."); using var udp = new PacketPeerUDP(); Check(udp.GetMaxPacketSize() == 65507, "Conservative UDP payload capacity.");
        using var packets = new LocalPackets(); packets.PutPacket(new byte[] { 1, 2 }); Reject<ArgumentException>(() => packets.GetPacket(new byte[1])); Check(packets.GetAvailablePacketCount() == 1 && packets.GetPacket().SequenceEqual(new byte[] { 1, 2 }), "Consumer packet hooks preserve nonconsuming bounds."); Reject<ArgumentException>(() => packets.PutPacket(new byte[5]));
    }
    private static void Buffer()
    {
        using var peer = new StreamPeerBuffer(); Check(peer.GetSize() == 0 && !peer.BigEndian, "Buffer defaults.");
        peer.Put16(-2); peer.PutU32(0x12345678); peer.PutHalf(1.5f); peer.PutFloat(-0f); peer.PutDouble(double.PositiveInfinity);
        Check(peer.DataArray.Take(8).SequenceEqual(new byte[] { 0xfe, 0xff, 0x78, 0x56, 0x34, 0x12, 0, 0x3e }), "Independent little-endian wire bytes.");
        peer.Seek(0); Check(peer.Get16() == -2 && peer.GetU32() == 0x12345678 && peer.GetHalf() == 1.5f && BitConverter.SingleToInt32Bits(peer.GetFloat()) == int.MinValue && double.IsPositiveInfinity(peer.GetDouble()), "Number decoding.");
        peer.Clear(); peer.BigEndian = true; peer.Put16(-2); peer.PutU32(0x12345678); peer.PutHalf(1.5f); Check(peer.DataArray.SequenceEqual(new byte[] { 0xff, 0xfe, 0x12, 0x34, 0x56, 0x78, 0x3e, 0 }), "Independent big-endian wire bytes.");
        peer.Clear(); peer.BigEndian = false; peer.PutString("Aé"); peer.PutUTF8String("é😀\0"); peer.Seek(0); Check(peer.GetString() == "A " && peer.GetUTF8String() == "é😀", "ASCII and UTF-8 framing.");
        peer.DataArray = [0xef, 0xbb, 0xbf, 0xe2, 0x82, 0, 65]; Check(peer.GetUTF8String(7) == "\ufffd\ufffd", "BOM/NUL/truncated UTF-8 wire semantics.");
        peer.DataArray = [1, 2, 3]; var snapshot = peer.DataArray; snapshot[0] = 9; Check(peer.GetU8() == 1, "Copied byte boundary.");
        using var duplicate = peer.Duplicate(); Check(duplicate.GetPosition() == 0 && !duplicate.BigEndian && duplicate.DataArray.SequenceEqual(new byte[] { 1, 2, 3 }), "Duplicate resets state.");
        peer.Seek(3); peer.Resize(1); Check(peer.GetPosition() == 1 && peer.GetAvailableBytes() == 0, "Shrink clamps cursor."); peer.Resize(4); Check(peer.DataArray.SequenceEqual(new byte[] { 1, 0, 0, 0 }), "Growth zero-fills.");
        peer.Seek(3); Reject<EndOfStreamException>(() => peer.Get32()); Check(peer.GetPosition() == 4, "Short full read consumes prefix.");
        Reject<ArgumentOutOfRangeException>(() => peer.Seek(5)); Reject<ArgumentOutOfRangeException>(() => peer.Resize(-1));
        peer.Clear(); peer.Put32(-1); peer.Seek(0); Reject<InvalidDataException>(() => peer.GetUTF8String());
        peer.Clear(); peer.Put32(20); peer.Seek(0); peer.MaxStringBytes = 10; Reject<InvalidDataException>(() => peer.GetString());
        Reject<InvalidOperationException>(() => Task.Run(() => peer.GetSize()).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(peer.Dispose).GetAwaiter().GetResult()); Check(!peer.IsDisposed, "Foreign-thread disposal is rejected before state changes.");
        peer.MaxStringBytes = 16 * 1024 * 1024;
        for (var i = 0; i < 32; i++) { peer.Clear(); peer.PutU64(123); peer.Seek(0); Check(peer.GetU64() == 123, "Buffer warmup."); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { peer.Clear(); peer.PutU64(123); peer.Seek(0); _ = peer.GetU64(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Buffer numeric warm allocation.");
    }
    private static void TCP()
    {
        using var server = new TCPServer(); server.Listen(0, "127.0.0.1"); Check(server.IsListening() && server.GetLocalPort() > 0 && server.TakeConnection() is null, "TCP listener.");
        using var client = new StreamPeerTCP(); client.Bind(0, "127.0.0.1"); var local = client.GetLocalPort(); client.ConnectToHost("127.0.0.1", server.GetLocalPort());
        Wait(() => { client.Poll(); return client.GetStatus() == StreamSocketStatus.Connected && server.IsConnectionAvailable(); });
        using var accepted = server.TakeConnection()!; Check(accepted.GetConnectedPort() == local && client.GetConnectedPort() == server.GetLocalPort() && client.GetConnectedHost() == "127.0.0.1", "TCP endpoints.");
        var originalPort = server.GetLocalPort(); client.SetNoDelay(true); accepted.SetNoDelay(true); server.Stop(); using (var restarted = new TCPServer()) restarted.Listen(originalPort, "127.0.0.1"); Check(!server.IsListening() && accepted.GetStatus() == StreamSocketStatus.Connected, "Accepted TCP survives listener stop.");
        client.PutUTF8String("wire 😀"); Wait(() => accepted.GetAvailableBytes() >= 13); Check(accepted.GetUTF8String() == "wire 😀", "TCP stream framing.");
        accepted.PutData(new byte[] { 1, 2, 3 }); Wait(() => client.GetAvailableBytes() == 3); Span<byte> data = stackalloc byte[4]; Check(client.GetPartialData(data) == 3 && data[..3].SequenceEqual(new byte[] { 1, 2, 3 }) && client.GetPartialData(data) == 0, "TCP partial read.");
        var payload = new byte[1024 * 1024]; Random.Shared.NextBytes(payload); var received = new byte[payload.Length]; var sent = 0; var read = 0;
        Wait(() => { if (sent < payload.Length) sent += client.PutPartialData(payload.AsSpan(sent)); if (read < received.Length) read += accepted.GetPartialData(received.AsSpan(read)); return sent == payload.Length && read == received.Length; }); Check(received.SequenceEqual(payload), "TCP large partial transfer.");
        for (var i = 0; i < 32; i++) RoundTrip(client, accepted);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { RoundTrip(client, accepted); client.Poll(); accepted.Poll(); _ = client.GetStatus(); _ = client.GetLocalPort(); _ = server.IsListening(); }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before; Check(bytes == 0, "64 TCP warm cycles allocate " + bytes); Console.WriteLine("64 TCP stream/number/poll cycles: " + bytes + " managed bytes.");
        client.PutU8(42); client.DisconnectFromHost(); Wait(() => accepted.GetAvailableBytes() == 1); accepted.Poll(); Check(accepted.GetStatus() == StreamSocketStatus.Connected && accepted.GetU8() == 42, "FIN preserves queued bytes."); Wait(() => { accepted.Poll(); return accepted.GetStatus() == StreamSocketStatus.None; });
        using (var resetListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        using (var resetClient = new StreamPeerTCP())
        {
            resetListener.Bind(new IPEndPoint(IPAddress.Loopback, 0)); resetListener.Listen(1);
            resetClient.ConnectToHost("127.0.0.1", ((IPEndPoint)resetListener.LocalEndPoint!).Port);
            using (var resetPeer = resetListener.Accept())
            {
                Wait(() => { resetClient.Poll(); return resetClient.GetStatus() == StreamSocketStatus.Connected; });
                resetPeer.LingerState = new LingerOption(true, 0);
            }
            var resetDetected = false;
            Wait(() =>
            {
                try { resetClient.Poll(); }
                catch (SocketException error) when (error.SocketErrorCode is SocketError.ConnectionReset or SocketError.ConnectionAborted) { resetDetected = true; }
                return resetDetected;
            });
            Check(resetClient.GetStatus() == StreamSocketStatus.Error && resetClient.GetLocalPort() == 0, "RST reports the actual socket error and closes native state.");
        }
        Reject<InvalidOperationException>(() => client.SetNoDelay(true)); Reject<ArgumentOutOfRangeException>(() => client.ConnectToHost("127.0.0.1", 0));
        using var refused = new StreamPeerTCP(); try { refused.ConnectToHost("127.0.0.1", server.GetLocalPort() == 0 ? local : server.GetLocalPort()); Wait(() => { refused.Poll(); return refused.GetStatus() != StreamSocketStatus.Connecting; }); } catch (SocketException) { }
        using var duplicateListener = new TCPServer(); using var occupied = new TCPServer(); occupied.Listen(0, "127.0.0.1"); Reject<SocketException>(() => duplicateListener.Listen(occupied.GetLocalPort(), "127.0.0.1")); Check(!duplicateListener.IsListening(), "Failed listen releases native state.");
    }
    private static void RoundTrip(StreamPeer a, StreamPeer b)
    {
        a.PutU64(0x12345678); SpinBytes(b, 8); if (b.GetU64() != 0x12345678) throw new InvalidOperationException("Wire mismatch."); b.PutU64(99); SpinBytes(a, 8); if (a.GetU64() != 99) throw new InvalidOperationException("Reply mismatch.");
    }
    private static void SpinBytes(StreamPeer peer, int count) { var start = Environment.TickCount64; while (peer.GetAvailableBytes() < count) { if (Environment.TickCount64 - start > 5000) throw new TimeoutException(); Thread.Yield(); } }
    private static void UDS()
    {
        if (!Socket.OSSupportsUnixDomainSockets) { Console.WriteLine("UDS unavailable on this runtime."); return; }
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-net-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); var path = System.IO.Path.Combine(folder, "socket");
        try
        {
            using var server = new UDSServer(); server.Listen(path); using var client = new StreamPeerUDS(); client.ConnectToHost(path); Wait(() => { client.Poll(); return client.GetStatus() == StreamSocketStatus.Connected && server.IsConnectionAvailable(); });
            using var peer = server.TakeConnection()!; Check(client.GetConnectedPath() == path, "UDS path."); RoundTrip(client, peer); server.Stop(); RoundTrip(client, peer);
            client.DisconnectFromHost(); Wait(() => { peer.Poll(); return peer.GetStatus() == StreamSocketStatus.None; });
            Check(!File.Exists(path), "Bound UDS endpoint is cleaned by the runtime.");
            File.WriteAllText(path, "sentinel"); using var failed = new UDSServer(); Reject<SocketException>(() => failed.Listen(path)); Check(File.ReadAllText(path) == "sentinel", "Existing namespace entries remain intact.");
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void UDP()
    {
        using (var native = NetworkSockets.Create(AddressFamily.InterNetwork, SocketType.Dgram))
            Check(native.SendBufferSize >= 65536 && native.ReceiveBufferSize >= 65536, "Native UDP buffers hold a maximum datagram independently of OS defaults.");
        using var receiver = new PacketPeerUDP(); receiver.Bind(0, "127.0.0.1"); using var sender = new PacketPeerUDP(); sender.SetDestAddress("localhost", receiver.GetLocalPort()); Check(!sender.IsBound(), "Destination assignment does not open UDP.");
        // Use the numeric loopback for this IPv4 receiver after exercising resolver configuration.
        sender.SetDestAddress("127.0.0.1", receiver.GetLocalPort()); sender.PutPacket(new byte[] { 1, 2, 3 }); Wait(() => receiver.GetAvailablePacketCount() == 1);
        Reject<ArgumentException>(() => receiver.GetPacket(new byte[2])); Check(receiver.GetAvailablePacketCount() == 1 && receiver.GetPacketError() == PacketReadStatus.Error, "Small destination preserves packet.");
        Span<byte> data = stackalloc byte[65536]; Check(receiver.GetPacket(data) == 3 && data[..3].SequenceEqual(new byte[] { 1, 2, 3 }) && receiver.GetPacketIP() == "127.0.0.1" && receiver.GetPacketPort() == sender.GetLocalPort(), "UDP boundaries and sender metadata.");
        Reject<InvalidOperationException>(() => receiver.GetPacket()); Check(receiver.GetPacketError() == PacketReadStatus.Unavailable && receiver.LastReadException is InvalidOperationException, "Unavailable packet read status.");
        sender.PutPacket(ReadOnlySpan<byte>.Empty); Wait(() => receiver.GetAvailablePacketCount() == 1); Check(receiver.GetPacket(data) == 0 && receiver.GetPacketError() == PacketReadStatus.OK, "Zero datagram.");
        var large = new byte[65507]; large[0] = 4; large[^1] = 5; sender.PutPacket(large); Wait(() => receiver.GetAvailablePacketCount() == 1); Check(receiver.GetPacket(data) == large.Length && data[0] == 4 && data[large.Length - 1] == 5, "Maximum IPv4 UDP payload.");
        using (var boundSender = new PacketPeerUDP()) { boundSender.SetDestAddress("127.0.0.1", receiver.GetLocalPort()); boundSender.Bind(0, "127.0.0.1"); boundSender.PutPacket(new byte[] { 11 }); Wait(() => receiver.GetAvailablePacketCount() > 0); Check(receiver.GetPacket(data) == 1 && data[0] == 11, "Destination survives explicit bind."); }
        sender.ConnectToHost("127.0.0.1", receiver.GetLocalPort()); Check(sender.IsSocketConnected(), "Connected UDP."); Reject<InvalidOperationException>(() => sender.SetDestAddress("127.0.0.1", 1));
        sender.PutPacket(new byte[] { 7 }); receiver.Wait(); Check(receiver.GetPacket(data) == 1, "Wait retains readable packet.");
        for (var i = 0; i < 32; i++) UDPCycle(sender, receiver, data);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) UDPCycle(sender, receiver, data); var bytes = GC.GetAllocatedBytesForCurrentThread() - before; Check(bytes == 0, "64 UDP warm cycles allocate " + bytes); Console.WriteLine("64 UDP packet/queue/metadata cycles: " + bytes + " managed bytes.");
        using (var other = new PacketPeerUDP()) { other.SetDestAddress("127.0.0.1", sender.GetLocalPort()); other.PutPacket(new byte[] { 9 }); receiver.SetDestAddress("127.0.0.1", sender.GetLocalPort()); receiver.PutPacket(new byte[] { 10 }); Wait(() => sender.GetAvailablePacketCount() > 0); Check(sender.GetPacket(data) == 1 && data[0] == 10 && sender.GetAvailablePacketCount() == 0, "Connected UDP filters other endpoints."); }
        receiver.Close(); Check(!receiver.IsBound() && !receiver.IsSocketConnected() && receiver.GetAvailablePacketCount() == 0, "UDP close resets native state.");
        using var tiny = new PacketPeerUDP(); tiny.Bind(0, "127.0.0.1", 32); sender.Close(); sender.SetDestAddress("127.0.0.1", tiny.GetLocalPort()); sender.PutPacket(new byte[16]); Wait(() => sender.IsBound()); Thread.Sleep(2); Check(tiny.GetAvailablePacketCount() == 0, "Queue overflow drops entire packets.");
        Reject<ArgumentOutOfRangeException>(() => receiver.Bind(-1)); Reject<ArgumentOutOfRangeException>(() => receiver.Bind(0, "127.0.0.1", -1));
    }
    private static void UDPCycle(PacketPeerUDP a, PacketPeerUDP b, Span<byte> receive)
    {
        Span<byte> send = stackalloc byte[8]; send[0] = 123; a.PutPacket(send); var start = Environment.TickCount64; while (b.GetAvailablePacketCount() == 0) { if (Environment.TickCount64 - start > 5000) throw new TimeoutException(); Thread.Yield(); }
        if (b.GetPacket(receive) != 8 || receive[0] != 123 || b.GetPacketPort() != a.GetLocalPort() || b.GetPacketIP() != "127.0.0.1") throw new InvalidOperationException("Packet mismatch.");
    }
    private static void UDPConnections()
    {
        using var server = new UDPServer { MaxPendingConnections = 2 }; server.Listen(0, "127.0.0.1"); using var a = new PacketPeerUDP(); using var b = new PacketPeerUDP(); using var c = new PacketPeerUDP();
        foreach (var peer in new[] { a, b, c }) peer.SetDestAddress("127.0.0.1", server.GetLocalPort()); a.PutPacket(new byte[] { 1 }); b.PutPacket(new byte[] { 2 }); c.PutPacket(new byte[] { 3 });
        Wait(() => { server.Poll(); return server.IsConnectionAvailable(); }); using var accepted = server.TakeConnection()!; Check(accepted.GetPacket().SequenceEqual(new byte[] { 1 }), "UDP accepted peer retains first packet.");
        server.MaxPendingConnections = 0; Check(!server.IsConnectionAvailable(), "Lower limit trims pending endpoints."); accepted.PutPacket(new byte[] { 4 }); Wait(() => a.GetAvailablePacketCount() > 0); Check(a.GetPacket().SequenceEqual(new byte[] { 4 }), "Shared peer replies through listener socket.");
        a.PutPacket(new byte[] { 5 }); Wait(() => accepted.GetAvailablePacketCount() > 0); Check(accepted.GetPacket().SequenceEqual(new byte[] { 5 }), "Accepted endpoints survive zero pending limit.");
        accepted.Close(); server.MaxPendingConnections = 1; a.PutPacket(new byte[] { 6 }); Wait(() => { server.Poll(); return server.IsConnectionAvailable(); }); using var again = server.TakeConnection()!; Check(again.GetPacket().SequenceEqual(new byte[] { 6 }), "Closed endpoint can be accepted again.");
        server.Stop(); Check(!server.IsListening() && !again.IsSocketConnected() && !again.IsBound(), "Stop detaches caller-owned UDP peer."); Reject<InvalidOperationException>(() => server.Poll());
    }
    private static void ReusableDatagramAddress()
    {
        Span<byte> received = stackalloc byte[8];
        foreach (var family in Socket.OSSupportsIPv6 ? new[] { AddressFamily.InterNetwork, AddressFamily.InterNetworkV6 } : new[] { AddressFamily.InterNetwork })
        {
            var loopback = family == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback;
            using var receiver = NetworkSockets.Create(family, SocketType.Dgram);
            using var sender = NetworkSockets.Create(family, SocketType.Dgram);
            receiver.Bind(new IPEndPoint(loopback, 0));
            var destination = receiver.LocalEndPoint!;
            var address = new SocketAddress(family);
            for (var cycle = 0; cycle < 2; cycle++)
            {
                sender.SendTo(new byte[] { 23 }, destination);
                Wait(() => receiver.Poll(0, SelectMode.SelectRead));
                address.Size = 2;
                Check(NetworkSockets.ReceiveDatagram(receiver, received, address) == 1 && received[0] == 23,
                    "Reusable UDP addresses restore capacity after a short native address result.");
                var endpoint = DatagramAddress.Capture(address);
                Check(endpoint.Address().Equals(loopback) && endpoint.Port == ((IPEndPoint)sender.LocalEndPoint!).Port,
                    "Reusable IPv4/IPv6 address preparation preserves sender metadata.");
            }
        }
    }
    private static void DatagramPeerDeparture()
    {
        Span<byte> received = stackalloc byte[8];
        foreach (var family in Socket.OSSupportsIPv6 ? new[] { AddressFamily.InterNetwork, AddressFamily.InterNetworkV6 } : new[] { AddressFamily.InterNetwork })
        {
            var loopback = family == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback;
            using var receiver = NetworkSockets.Create(family, SocketType.Dgram);
            using var healthy = NetworkSockets.Create(family, SocketType.Dgram);
            using var departed = NetworkSockets.Create(family, SocketType.Dgram);
            using var control = OperatingSystem.IsWindows() ? new Socket(family, SocketType.Dgram, ProtocolType.Udp) { Blocking = false } : null;
            receiver.Bind(new IPEndPoint(loopback, 0));
            healthy.Bind(new IPEndPoint(loopback, 0));
            departed.Bind(new IPEndPoint(loopback, 0));
            control?.Bind(new IPEndPoint(loopback, 0));
            var closedEndpoint = departed.LocalEndPoint!;
            departed.Close();
            receiver.SendTo(new byte[] { 27 }, closedEndpoint);
            if (OperatingSystem.IsWindows())
            {
                // Negative control proves that this host delivers an ICMP error for the closed port.
                control!.IOControl(unchecked((int)0x9800000C), new byte[] { 1, 0, 0, 0 }, null);
                control.SendTo(new byte[] { 28 }, closedEndpoint);
                Wait(() => control.Poll(0, SelectMode.SelectRead));
                var reset = false;
                try { NetworkSockets.ReceiveDatagram(control, received, new SocketAddress(family)); }
                catch (SocketException error) when (error.SocketErrorCode == SocketError.ConnectionReset) { reset = true; }
                Check(reset, "Windows reports port-unreachable as a reset when explicitly enabled.");
            }
            healthy.SendTo(new byte[] { 29 }, receiver.LocalEndPoint!);
            Wait(() => receiver.Poll(0, SelectMode.SelectRead));
            var address = new SocketAddress(family);
            Check(NetworkSockets.ReceiveDatagram(receiver, received, address) == 1 && received[0] == 29,
                "A departed UDP endpoint must not reset another peer's listener or fabricate an empty packet.");
            Check(DatagramAddress.Capture(address).Port == ((IPEndPoint)healthy.LocalEndPoint!).Port,
                "UDP listener retains the healthy sender after a peer departs.");
        }
    }
    private static void Multicast()
    {
        using var peer = new PacketPeerUDP(); peer.Bind(0, "0.0.0.0"); peer.SetBroadcastEnabled(true); peer.SetBroadcastEnabled(false);
        var interfaceName = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().First(n => n.SupportsMulticast && n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && n.GetIPProperties().UnicastAddresses.Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork)).Name;
        peer.JoinMulticastGroup("239.255.42.42", interfaceName); peer.LeaveMulticastGroup("239.255.42.42", interfaceName); Reject<ArgumentException>(() => peer.JoinMulticastGroup("127.0.0.1", interfaceName));
    }
    private static void IPv6()
    {
        if (!Socket.OSSupportsIPv6) return;
        using var server = new TCPServer(); server.Listen(0, "::1"); using var client = new StreamPeerTCP(); client.ConnectToHost("::1", server.GetLocalPort()); Wait(() => { client.Poll(); return client.GetStatus() == StreamSocketStatus.Connected && server.IsConnectionAvailable(); }); using var accepted = server.TakeConnection()!; RoundTrip(client, accepted); Check(client.GetLocalPort() > 0 && client.GetConnectedHost() == "::1", "IPv6 stream metadata.");
        using var receiver = new PacketPeerUDP(); receiver.Bind(0, "::1"); using var sender = new PacketPeerUDP(); sender.SetDestAddress("::1", receiver.GetLocalPort()); sender.PutPacket(new byte[] { 8 }); Wait(() => receiver.GetAvailablePacketCount() > 0); Check(receiver.GetPacket()[0] == 8 && receiver.GetPacketIP() == "::1", "IPv6 datagram metadata.");
    }
    private sealed class ExchangeNode : Node
    {
        private TCPServer? _server;
        private StreamPeerTCP? _client, _accepted;
        internal bool Complete;
        private int _phase;
        protected override void OnReady() { ProcessEnabled = true; _server = new(); _server.Listen(0, "127.0.0.1"); _client = new(); _client.ConnectToHost("127.0.0.1", _server.GetLocalPort()); }
        protected override void OnProcess(double delta)
        {
            _client!.Poll(); if (_accepted is null) _accepted = _server!.TakeConnection(); if (_accepted is null || _client.GetStatus() != StreamSocketStatus.Connected) return;
            if (_phase == 0) { _client.PutUTF8String("scene"); _phase++; }
            else if (_phase == 1 && _accepted.GetAvailableBytes() >= 9) { Check(_accepted.GetUTF8String() == "scene", "Scene request."); _accepted.PutUTF8String("reply"); _phase++; }
            else if (_phase == 2 && _client.GetAvailableBytes() >= 9) { Check(_client.GetUTF8String() == "reply", "Scene reply."); Complete = true; ProcessEnabled = false; }
        }
        protected override void Dispose(bool disposing) { if (disposing) { _accepted?.Dispose(); _client?.Dispose(); _server?.Dispose(); } base.Dispose(disposing); }
    }
    private static void Scene()
    {
        var root = new Node(); var node = new ExchangeNode(); root.AddChild(node); using (var tree = new SceneTree(root)) Wait(() => { tree.ProcessFrame(.01); return node.Complete; }); Check(node.IsDisposed, "Scene lifecycle disposes owned transports.");
    }
    private static void FramedPackets()
    {
        using var bytes = new StreamPeerBuffer { BigEndian = true }; using var packets = new PacketPeerStream { StreamPeer = bytes }; packets.PutPacket(new byte[] { 1, 2, 3 }); Check(bytes.DataArray.SequenceEqual(new byte[] { 3, 0, 0, 0, 1, 2, 3 }), "Packet framing is always little endian."); bytes.Seek(0); Check(packets.GetAvailablePacketCount() == 1, "Framed packet availability."); Reject<InvalidOperationException>(() => packets.InputBufferMaxSize = 100); Check(packets.GetPacket().SequenceEqual(new byte[] { 1, 2, 3 }), "Framed packet read.");
        bytes.Clear(); packets.PutPacket(ReadOnlySpan<byte>.Empty); Check(bytes.GetSize() == 0, "Empty framed send is ignored."); bytes.PutU32(0); bytes.Seek(0); Check(packets.GetPacket().Length == 0, "Empty inbound frame is valid.");
        bytes.Clear(); bytes.PutData(new byte[] { 0xff, 0xff, 0xff, 0xff }); bytes.Seek(0); Reject<InvalidDataException>(() => packets.GetPacket());
        packets.StreamPeer = null; Check(!bytes.IsDisposed, "Wrapper borrows underlying stream."); Reject<InvalidOperationException>(() => packets.GetAvailablePacketCount()); packets.StreamPeer = bytes;
        packets.InputBufferMaxSize = 5; packets.OutputBufferMaxSize = 5; Check(packets.InputBufferMaxSize == 12 && packets.OutputBufferMaxSize == 12, "Rounded frame limits.");
        bytes.Clear(); bytes.PutData(new byte[] { 2, 0 }); bytes.Seek(0); Check(packets.GetAvailablePacketCount() == 0, "Split header remains incomplete."); packets.StreamPeer = null; packets.StreamPeer = bytes; bytes.DataArray = [2, 0, 0, 0, 7, 8]; Check(packets.GetPacket().SequenceEqual(new byte[] { 7, 8 }), "Rebinding resets partial input.");
    }
    private static void Wait(Func<bool> condition) { var watch = Stopwatch.StartNew(); while (!condition()) { if (watch.ElapsedMilliseconds > 5000) throw new TimeoutException("Transport check deadline."); Thread.Yield(); } }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
