using Electron2D;
using System.Buffers.Binary;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BCLSocket = System.Net.WebSockets.WebSocket;
using EngineState = Electron2D.WebSocketState;
using Certificate = Electron2D.X509Certificate;

internal static class WebSocketTests
{
    internal static void Run() { Scripted(); InvalidHandshakes(); NativeClient(false); NativeClient(true); NativeServer(false); NativeServer(true); Console.WriteLine("WebSocket WS/WSS, independent peers, fragmented/control/close frames, bounded queues, scene polling and ownership checks passed."); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Wait(Func<bool> ready) { var deadline = Environment.TickCount64 + 10000; while (!ready()) { if (Environment.TickCount64 > deadline) throw new TimeoutException("WebSocket check timed out."); Thread.Sleep(1); } }
    private sealed class ScriptStream : StreamPeer
    {
        internal byte[] Input = [];
        internal readonly byte[] Output = new byte[2 * 1024 * 1024];
        internal int ReadOffset, Written, Fragment = 3;
        internal bool BlockWrites, EOF;
        public override int GetAvailableBytes() { CheckStream(); return Input.Length - ReadOffset; }
        protected override int ReadCore(Span<byte> destination, bool block) { if (EOF && ReadOffset == Input.Length) throw new EndOfStreamException(); var count = Math.Min(Math.Min(destination.Length, Fragment), Input.Length - ReadOffset); Input.AsSpan(ReadOffset, count).CopyTo(destination); ReadOffset += count; return count; }
        protected override int WriteCore(ReadOnlySpan<byte> data, bool block) { if (BlockWrites) return 0; var count = Math.Min(data.Length, Fragment); data[..count].CopyTo(Output.AsSpan(Written)); Written += count; return count; }
        internal void Feed(byte[] data) { Check(ReadOffset == Input.Length, "Script input drained."); Input = data; ReadOffset = 0; }
    }
    private const string Request = "GET /play?q=1 HTTP/1.1\r\nHost: localhost:8080\r\nUpgrade: websocket\r\nConnection: keep-alive, Upgrade\r\nSec-WebSocket-Version: 13\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n\r\n";
    private static WebSocketPeer Open(ScriptStream stream, int input = 65535, int output = 65535, int packets = 4096)
    {
        stream.Input = Encoding.ASCII.GetBytes(Request); var peer = new WebSocketPeer { InboundBufferSize = input, OutboundBufferSize = output, MaxQueuedPackets = packets }; peer.AcceptStream(stream); Wait(() => { peer.Poll(); return peer.GetReadyState() == EngineState.Open; }); Check(Encoding.ASCII.GetString(stream.Output, 0, stream.Written).Contains("Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n"), "Independent RFC handshake vector."); stream.Written = 0; return peer;
    }
    private static byte[] Frame(int opcode, ReadOnlySpan<byte> payload, bool final = true, bool masked = true)
    {
        var header = payload.Length < 126 ? 2 : payload.Length <= 65535 ? 4 : 10; var offset = header + (masked ? 4 : 0); var result = new byte[offset + payload.Length]; result[0] = (byte)((final ? 128 : 0) | opcode); result[1] = (byte)((masked ? 128 : 0) | (header == 2 ? payload.Length : header == 4 ? 126 : 127));
        if (header == 4) BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(2), (ushort)payload.Length); if (header == 10) BinaryPrimitives.WriteUInt64BigEndian(result.AsSpan(2), (ulong)payload.Length);
        if (masked) new byte[] { 1, 2, 3, 4 }.CopyTo(result, header); payload.CopyTo(result.AsSpan(offset)); if (masked) for (var i = 0; i < payload.Length; i++) result[offset + i] ^= result[header + (i & 3)]; return result;
    }
    private static void Scripted()
    {
        using var stream = new ScriptStream(); using var peer = Open(stream, packets: 2); Check(peer.GetRequestedURL() == "ws://localhost:8080/play?q=1", "Server request URL.");
        stream.Feed([.. Frame(1, new byte[] { 0xe2 }, false), .. Frame(9, "ping"u8), .. Frame(0, new byte[] { 0x82, 0xac })]); Wait(() => { peer.Poll(); return peer.GetAvailablePacketCount() == 1; }); Reject<ArgumentException>(() => peer.GetPacket(new byte[2])); Check(!peer.WasStringPacket() && peer.GetPacketError() == PacketReadStatus.Error, "Small destination preserves packet/text marker."); Span<byte> buffer = stackalloc byte[256]; Check(peer.GetPacket(buffer) == 3 && peer.WasStringPacket() && buffer[..3].SequenceEqual("€"u8), "Interleaved ping and fragmented UTF-8."); Check(stream.Output.AsSpan(0, stream.Written).SequenceEqual(new byte[] { 0x8a, 4, (byte)'p', (byte)'i', (byte)'n', (byte)'g' }), "Pong echoes exact payload.");
        stream.Feed([.. Frame(2, ReadOnlySpan<byte>.Empty), .. Frame(2, "one"u8), .. Frame(2, "two"u8)]); peer.Poll(); Check(peer.GetAvailablePacketCount() == 2, "Prepared inbound packet limit."); Check(peer.GetPacket(buffer) == 0 && !peer.WasStringPacket(), "Empty binary packet."); peer.Poll(); Check(peer.GetAvailablePacketCount() == 2 && peer.GetPacket(buffer) == 3 && buffer[..3].SequenceEqual("one"u8), "Backpressure retains third message."); Check(peer.GetPacket(buffer) == 3 && buffer[..3].SequenceEqual("two"u8), "Queued order.");
        Reject<ArgumentException>(() => peer.Send(new byte[] { 0xc0, 0xaf }, WebSocketWriteMode.Text)); Reject<ArgumentOutOfRangeException>(() => peer.Send([], (WebSocketWriteMode)2)); Reject<InvalidOperationException>(() => peer.InboundBufferSize = 4); Reject<InvalidOperationException>(() => Task.Run(peer.Poll).GetAwaiter().GetResult()); Reject<InvalidOperationException>(() => Task.Run(peer.Dispose).GetAwaiter().GetResult()); Check(!peer.IsDisposed, "Wrong-owner disposal does not poison peer."); Reject<NotSupportedException>(() => peer.SetNoDelay(false));
        var incoming = Frame(2, "warm"u8); stream.Fragment = 4096; for (var i = 0; i < 32; i++) { stream.Feed(incoming); peer.Poll(); peer.GetPacket(buffer); peer.SendText("warm"); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { stream.Feed(incoming); peer.Poll(); if (peer.GetPacket(buffer) != 4) throw new InvalidOperationException(); peer.SendText("warm"); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared server framing/text/span work allocates zero managed bytes."); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) peer.Poll(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Idle polling allocates zero managed bytes."); Console.WriteLine("64 WebSocket server active and idle cycles: 0 managed bytes.");
        Reject<ArgumentOutOfRangeException>(() => peer.Close(1006)); Reject<ArgumentException>(() => peer.Close(1000, new string('x', 124))); peer.Close(1000, "bye"); Check(peer.GetReadyState() == EngineState.Closing, "Close waits for confirmation."); stream.Feed(Frame(8, new byte[] { 3, 0xe8, (byte)'o', (byte)'k' })); peer.Poll(); Check(peer.GetReadyState() == EngineState.Closed && peer.GetCloseCode() == 1000 && peer.GetCloseReason() == "ok" && !stream.IsDisposed, "Clean close preserves borrowed stream.");
        using var limitedStream = new ScriptStream(); using var limited = Open(limitedStream, input: 8, output: 4, packets: 1); limitedStream.BlockWrites = true; limited.Send("1234"u8); Check(limited.GetCurrentOutboundBufferedAmount() == 4, "Blocked send retains payload count."); Reject<InvalidOperationException>(() => limited.Send([])); limited.Close(); limitedStream.BlockWrites = false; limited.Poll(); Check(limited.GetCurrentOutboundBufferedAmount() == 0 && limitedStream.Written > 0, "Reserved close capacity and partial send drain."); limited.Close(-1);
        foreach (var (frame, code) in new (byte[], int)[] { (Frame(2, "a"u8, masked: false), 1002), (new byte[] { 0xc2, 0x80, 1, 2, 3, 4 }, 1002), (Frame(0, []), 1002), (Frame(1, new byte[] { 0xc0, 0xaf }), 1007), (Frame(9, [], false), 1002), (Frame(8, new byte[] { 1 }), 1002), (Frame(8, new byte[] { 3, 0xed }), 1002), (Frame(2, new byte[9]), 1009), (new byte[] { 0x82, 0xfe, 0, 1, 1, 2, 3, 4, 0 }, 1002) })
        { using var badStream = new ScriptStream(); using var bad = Open(badStream, input: 8); badStream.Feed(frame); bad.Poll(); Check(bad.GetReadyState() == EngineState.Closed && bad.GetCloseCode() == code, "Malformed frame sends typed close " + code); }
        using var heartbeatStream = new ScriptStream(); using var heartbeat = Open(heartbeatStream); heartbeat.HeartbeatInterval = .003; Thread.Sleep(5); heartbeat.Poll(); Check(heartbeatStream.Written == 2 && heartbeatStream.Output[0] == 0x89, "Monotonic heartbeat ping."); heartbeatStream.Feed(Frame(10, [])); heartbeat.Poll(); Thread.Sleep(5); heartbeat.Poll(); Check(heartbeat.GetReadyState() == EngineState.Open, "Pong permits subsequent heartbeat."); Thread.Sleep(5); heartbeat.Poll(); Check(heartbeat.GetReadyState() == EngineState.Closed && heartbeat.GetCloseCode() == -1, "Unanswered heartbeat aborts.");
        using var eofStream = new ScriptStream(); using var eof = Open(eofStream); eofStream.EOF = true; eof.Poll(); Check(eof.GetReadyState() == EngineState.Closed && eof.GetCloseCode() == -1, "Transport EOF is unclean closure.");
        using var bigStream = new ScriptStream { Fragment = 4096 }; using var big = Open(bigStream, input: 131072, output: 131072); foreach (var size in new[] { 125, 126, 65535, 65536 }) { var payload = new byte[size]; Array.Fill(payload, (byte)42); bigStream.Feed(Frame(2, payload)); big.Poll(); Check(big.GetPacket().SequenceEqual(payload), "Canonical 7/16/64-bit payload length."); big.Send(payload); }
        big.Close(-1);
        using var emptyStream = new ScriptStream(); using var empty = Open(emptyStream, input: 0); emptyStream.Feed(Frame(2, [])); empty.Poll(); Check(empty.GetPacket().Length == 0, "Zero input budget admits empty message."); emptyStream.Feed(Frame(8, [])); empty.Poll(); Check(empty.GetCloseCode() == 1005, "Empty close preserves no-status marker.");
        using var configs = new WebSocketPeer(); Reject<ArgumentException>(() => configs.SupportedProtocols = ["bad token"]); Reject<ArgumentException>(() => configs.HandshakeHeaders = ["Connection: custom"]); Reject<ArgumentException>(() => configs.HandshakeHeaders = ["X: a\r\nb"]); Reject<ArgumentOutOfRangeException>(() => configs.HeartbeatInterval = double.NaN); configs.SupportedProtocols = [" game "]; var copy = configs.SupportedProtocols; copy[0] = "changed"; Check(configs.SupportedProtocols[0] == "game", "Protocol arrays copy and trim.");
        configs.SupportedProtocols = [];
        foreach (var request in new[] { Request.Replace("keep-alive, Upgrade", "keep-alive"), Request.Replace("GET /play?q=1 HTTP/1.1", "GET /play?q=1"), Request.Replace("dGhlIHNhbXBsZSBub25jZQ==", "short"), Request.Replace("Version: 13", "Version: 12"), Request.Replace("Host: localhost:8080", "Host: invalid/host"), Request.Replace("\r\n\r\n", "\r\nX-Padding: " + new string('x', 4096) + "\r\n\r\n") })
        { using var malformedStream = new ScriptStream { Input = Encoding.ASCII.GetBytes(request) }; configs.AcceptStream(malformedStream); Reject<System.IO.InvalidDataException>(configs.Poll); Check(configs.GetReadyState() == EngineState.Closed && !malformedStream.IsDisposed, "Invalid upgrade does not own custom stream."); }
        using var controlsStream = new ScriptStream(); using var controls = Open(controlsStream, packets: 0); controlsStream.BlockWrites = true; controlsStream.Feed([.. Frame(9, []), .. Frame(9, []), .. Frame(9, []), .. Frame(9, []), .. Frame(9, [])]); controls.Poll(); controlsStream.BlockWrites = false; controls.Poll(); Check(controlsStream.ReadOffset == controlsStream.Input.Length && controlsStream.Written == 10, "Reserved control queue backpressure resumes without growth or loss."); controls.Close(-1);

    }
    private static void InvalidHandshakes()
    {
        foreach (var kind in new[] { "version", "accept", "extension", "protocol" })
        {
            using var stop = new CancellationTokenSource(10000); var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var oracle = Task.Run(async () => { using var socket = await listener.AcceptTcpClientAsync(stop.Token); using var stream = socket.GetStream(); var headers = await ReadHeaders(stream, stop.Token); var nonce = headers.Split("\r\n").Single(l => l.StartsWith("Sec-WebSocket-Key: "))[19..].Trim(); var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(nonce + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))); var response = "HTTP/" + (kind == "version" ? "1.0" : "1.1") + " 101 Switching Protocols\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Accept: " + (kind == "accept" ? "wrong" : accept) + "\r\n"; if (kind == "extension") response += "Sec-WebSocket-Extensions: unknown\r\n"; if (kind == "protocol") response += "Sec-WebSocket-Protocol: unrequested\r\n"; await stream.WriteAsync(Encoding.ASCII.GetBytes(response + "\r\n"), stop.Token); });
            using var peer = new WebSocketPeer(); peer.ConnectToURL("localhost:" + port); Reject<System.IO.InvalidDataException>(() => Wait(() => { peer.Poll(); return peer.GetReadyState() == EngineState.Open; })); Check(peer.GetReadyState() == EngineState.Closed && peer.GetConnectedPort() == 0 && peer.GetCloseCode() == -1, "Client rejects invalid " + kind + " and releases owned TCP."); oracle.GetAwaiter().GetResult(); listener.Stop();
        }
    }
    private static async Task<string> ReadHeaders(Stream stream, CancellationToken stop)
    {
        var bytes = new byte[4096]; var count = 0; while (count < bytes.Length) { await stream.ReadExactlyAsync(bytes.AsMemory(count, 1), stop); count++; if (count >= 4 && bytes.AsSpan(count - 4, 4).SequenceEqual("\r\n\r\n"u8)) return Encoding.ASCII.GetString(bytes, 0, count); }
        throw new System.IO.InvalidDataException();
    }
    private static X509Certificate2 Identity(RSA key)
    {
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1); request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true)); var names = new SubjectAlternativeNameBuilder(); names.AddDnsName("localhost"); request.CertificateExtensions.Add(names.Build()); return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
    }
    private static void NativeClient(bool secure)
    {
        using var key = RSA.Create(2048); using var identity = Identity(key); using var trust = new Certificate(); trust.LoadFromString(identity.ExportCertificatePem()); using var options = TLSOptions.Client(trust);
        using var stop = new CancellationTokenSource(10000); var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var oracle = Task.Run(async () =>
        {
            using var socket = await listener.AcceptTcpClientAsync(stop.Token); using var raw = socket.GetStream(); using Stream stream = secure ? new SslStream(raw, true) : raw; if (stream is SslStream ssl) await ssl.AuthenticateAsServerAsync(identity, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
            var headers = await ReadHeaders(stream, stop.Token); var nonce = headers.Split("\r\n").Single(l => l.StartsWith("Sec-WebSocket-Key: "))[19..].Trim(); var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(nonce + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))); Check(headers.Contains("GET /game?q=1 HTTP/1.1\r\n") && headers.Contains("X-Game: check\r\n"), "Native client URL/custom headers.");
            await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: keep-alive, Upgrade\r\nSec-WebSocket-Accept: " + accept + "\r\nSec-WebSocket-Protocol: game\r\n\r\n"), stop.Token);
            using var ws = BCLSocket.CreateFromStream(stream, true, "game", System.Threading.Timeout.InfiniteTimeSpan); var bytes = new byte[1024]; var received = await ws.ReceiveAsync(bytes, stop.Token); Check(received.MessageType == WebSocketMessageType.Text && Encoding.UTF8.GetString(bytes, 0, received.Count) == "Привет", "Independent BCL server unmasks text.");
            for (var i = 0; i < 96; i++) { var echo = await ws.ReceiveAsync(bytes, stop.Token); Check(echo.Count == 4 && echo.MessageType == WebSocketMessageType.Text, "Independent masked warm message."); await ws.SendAsync(bytes.AsMemory(0, echo.Count), WebSocketMessageType.Binary, true, stop.Token); }
            await ws.SendAsync("fragment"u8.ToArray(), WebSocketMessageType.Binary, false, stop.Token); await ws.SendAsync("ed"u8.ToArray(), WebSocketMessageType.Binary, true, stop.Token); received = await ws.ReceiveAsync(bytes, stop.Token); Check(received.MessageType == WebSocketMessageType.Close, "Independent server receives close."); await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "oracle", stop.Token);
        });
        using var peer = new WebSocketPeer { SupportedProtocols = ["game"], HandshakeHeaders = ["X-Game: check"] }; using var root = new Node(); using var tree = new SceneTree(root); root.AddChild(new Poller(peer)); peer.ConnectToURL((secure ? "wss" : "ws") + "://localhost:" + port + "/game?q=1#ignored", secure ? options : null); Wait(() => { tree.ProcessFrame(.001); return peer.GetReadyState() == EngineState.Open; }); Check(peer.GetSelectedProtocol() == "game" && peer.GetConnectedPort() == port && peer.GetConnectedHost() == "127.0.0.1", "Native DNS/endpoint/subprotocol."); peer.SetNoDelay(false); peer.SendText("Привет"); Span<byte> warm = stackalloc byte[4]; for (var i = 0; i < 32; i++) Exchange(peer, warm); var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Exchange(peer, warm); Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Native masked client/TLS span exchange has zero managed allocation."); Console.WriteLine("64 WebSocket " + (secure ? "WSS" : "WS") + " client mask/text/poll/read cycles: 0 managed bytes."); Wait(() => { tree.ProcessFrame(.001); return peer.GetAvailablePacketCount() == 1; }); Check(Encoding.UTF8.GetString(peer.GetPacket()) == "fragmented" && !peer.WasStringPacket(), "Independent server fragmented binary."); peer.Close(); Wait(() => { tree.ProcessFrame(.001); return peer.GetReadyState() == EngineState.Closed; }); Check(peer.GetCloseCode() == 1000 && peer.GetCloseReason() == "oracle", "Independent native close result: secure=" + secure + ", code=" + peer.GetCloseCode() + ", reason=" + peer.GetCloseReason()); oracle.GetAwaiter().GetResult(); listener.Stop();
    }
    private static void Exchange(WebSocketPeer peer, Span<byte> bytes) { peer.SendText("warm"); var deadline = Environment.TickCount64 + 10000; while (peer.GetAvailablePacketCount() == 0) { peer.Poll(); if (Environment.TickCount64 > deadline) throw new TimeoutException(); Thread.Yield(); } Check(peer.GetPacket(bytes) == 4 && bytes.SequenceEqual("warm"u8), "Native span echo."); }
    private sealed class Poller : Node
    {
        private readonly WebSocketPeer _peer;
        internal Poller(WebSocketPeer peer) { _peer = peer; ProcessEnabled = true; }
        protected override void OnProcess(double delta) => _peer.Poll();
    }
    private static void NativeServer(bool secure)
    {
        using var key = RSA.Create(2048); using var identity = Identity(key); using var own = new Certificate(); own.LoadFromString(identity.ExportCertificatePem()); using var privateKey = new CryptoKey(); privateKey.LoadFromString(key.ExportPkcs8PrivateKeyPem()); using var options = TLSOptions.Server(privateKey, own);
        using var stop = new CancellationTokenSource(10000); using var listener = new TCPServer(); listener.Listen(0, "127.0.0.1"); var port = listener.GetLocalPort();
        var oracle = Task.Run(async () => { using var client = new ClientWebSocket(); client.Options.AddSubProtocol("game"); client.Options.RemoteCertificateValidationCallback = (_, certificate, _, _) => certificate is not null && certificate.GetCertHashString() == identity.GetCertHashString(); await client.ConnectAsync(new Uri((secure ? "wss" : "ws") + "://localhost:" + port + "/server"), stop.Token); var bytes = new byte[1024]; await client.SendAsync("from oracle"u8.ToArray(), WebSocketMessageType.Text, true, stop.Token); var response = await client.ReceiveAsync(bytes, stop.Token); Check(response.MessageType == WebSocketMessageType.Binary && bytes.AsSpan(0, response.Count).SequenceEqual("reply"u8), "Independent client receives native server binary."); await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", stop.Token); });
        Wait(listener.IsConnectionAvailable); using var tcp = listener.TakeConnection()!; using var tls = new StreamPeerTLS(); if (secure) tls.AcceptStream(tcp, options); using var peer = new WebSocketPeer { SupportedProtocols = ["game"], HandshakeHeaders = ["X-Server: engine"] }; peer.AcceptStream(secure ? tls : tcp); Wait(() => { peer.Poll(); return peer.GetReadyState() == EngineState.Open; }); Check(peer.GetRequestedURL().EndsWith("/server", StringComparison.Ordinal) && peer.GetConnectedPort() > 0, "Server metadata."); Wait(() => { peer.Poll(); return peer.GetAvailablePacketCount() > 0; }); Check(Encoding.UTF8.GetString(peer.GetPacket()) == "from oracle" && peer.WasStringPacket(), "Independent client masked text."); peer.PutPacket("reply"u8); Wait(() => { peer.Poll(); return peer.GetReadyState() == EngineState.Closed; }); Check(peer.GetCloseCode() == 1000 && peer.GetCloseReason() == "done" && !tcp.IsDisposed && !tls.IsDisposed, "Server close and borrowed ownership."); oracle.GetAwaiter().GetResult();
    }
}
