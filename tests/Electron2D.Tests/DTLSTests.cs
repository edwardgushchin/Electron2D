using Electron2D;
using System.Net;
using System.Diagnostics;
using System.Text;
using System.Security.Authentication;

internal static class DTLSTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Wait(Action poll, Func<bool> ready) { var end = Environment.TickCount64 + 10000; while (!ready()) { poll(); if (Environment.TickCount64 > end) throw new TimeoutException(); Thread.Yield(); } }
    internal static void Run()
    {
        using var crypto = new Crypto(); using var key = crypto.GenerateRSA(2048); using var cert = crypto.GenerateSelfSignedCertificate(key, "CN=localhost,O=Electron2D,C=RU"); using var serverOptions = TLSOptions.Server(key, cert); using var clientOptions = TLSOptions.Client(cert, "localhost");
        Native(key, cert, serverOptions, clientOptions, false); Native(key, cert, serverOptions, clientOptions, true); Rejection(serverOptions, clientOptions, cert); Policies(serverOptions); Oracle(key, cert, serverOptions, clientOptions); Console.WriteLine("Native DTLS packet/cookie/handshake, retransmission, trust, ownership and allocation checks passed.");
    }
    private sealed class LossyUDP : PacketPeerUDP
    {
        internal bool DropFirst = true;
        internal int Dropped, Challenges, Guards;
        internal bool Capture, Tamper;
        internal byte[]? Captured;
        internal Action? Reenter;
        public override void PutPacket(ReadOnlySpan<byte> data) { if (DropFirst) { DropFirst = false; Dropped++; return; } if (Capture && data.Length > 0 && data[0] == 23) Captured = data.ToArray(); if (Tamper && data.Length > 0 && data[0] == 23) { Tamper = false; var bytes = data.ToArray(); bytes[^1] ^= 1; base.PutPacket(bytes); return; } base.PutPacket(data); }
        protected override void ReadPacketCore(Span<byte> data) { base.ReadPacketCore(data); if (data.Length > 13 && data[0] == 22 && data[13] == 3) Challenges++; if (Reenter is not null) { Reenter(); Guards++; } }
        internal void Replay() => base.PutPacket(Captured!);
    }
    private static void Native(CryptoKey key, X509Certificate cert, TLSOptions serverOptions, TLSOptions clientOptions, bool drop)
    {
        using var host = new UDPServer(); host.Listen(0, "127.0.0.1"); using var helper = new DTLSServer(); helper.Setup(serverOptions); Reject<InvalidOperationException>(() => helper.Setup(serverOptions)); Reject<InvalidOperationException>(key.Dispose); Reject<InvalidOperationException>(cert.Dispose);
        using var wire = new LossyUDP { DropFirst = drop }; wire.ConnectToHost("127.0.0.1", host.GetLocalPort()); using var client = new PacketPeerDTLS(); client.ConnectToPeer(wire, "ignored", clientOptions); wire.Reenter = () => Reject<InvalidOperationException>(client.Dispose); PacketPeerUDP? accepted = null; PacketPeerDTLS? server = null;
        void Poll() { client.Poll(); host.Poll(); if (accepted is null && host.IsConnectionAvailable()) { accepted = host.TakeConnection(); server = helper.TakeConnection(accepted!); } server?.Poll(); }
        try
        {
            Wait(Poll, () => client.GetStatus() == TLSStatus.Connected && server?.GetStatus() == TLSStatus.Connected); Check(wire.Dropped == (drop ? 1 : 0) && wire.Challenges > 0 && wire.Guards > 0, "Cookie challenge, disposal reentry guard and retransmitted initial ClientHello."); wire.Reenter = null; helper.Dispose(); Reject<InvalidOperationException>(key.Dispose); Check(server!.GetMaxPacketSize() == 488, "Preserved advertised packet capacity.");
            Span<byte> bytes = stackalloc byte[488]; for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)i; client.PutPacket(bytes); Wait(Poll, () => server.GetAvailablePacketCount() == 1); Reject<ArgumentException>(() => server.GetPacket(new byte[1])); Check(server.GetAvailablePacketCount() == 1 && server.GetPacket(bytes) == 488 && bytes[487] == unchecked((byte)487), "Datagram length/contents and undersized read preservation."); Reject<ArgumentException>(() => client.PutPacket(new byte[489])); client.PutPacket([]); Poll(); Check(server.GetAvailablePacketCount() == 0, "Empty sends are no-op.");
            wire.Capture = true; client.PutPacket("once"u8); Wait(Poll, () => server.GetAvailablePacketCount() == 1); Check(server.GetPacket(bytes) == 4, "Captured record delivers once."); wire.Capture = false; wire.Replay(); for (var i = 0; i < 32; i++) Poll(); Check(server.GetAvailablePacketCount() == 0, "DTLS anti-replay rejects repeated ciphertext."); wire.Tamper = true; client.PutPacket("bad"u8); for (var i = 0; i < 32; i++) Poll(); Check(server.GetAvailablePacketCount() == 0, "Invalid record MAC exposes no plaintext."); client.PutPacket("fresh"u8); Wait(Poll, () => server.GetAvailablePacketCount() == 1); Check(server.GetPacket(bytes) == 5, "Valid records resume after rejected ciphertext.");
            var root = new Probe(client, server); using (var tree = new SceneTree(root)) { tree.ProcessFrame(0); Wait(Poll, () => server.GetAvailablePacketCount() == 1); Check(server.GetPacket(bytes) == 4 && bytes[..4].SequenceEqual("node"u8), "Public Node/SceneTree DTLS use."); }
            for (var i = 0; i < 32; i++) Exchange(client, server, bytes); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Exchange(client, server, bytes); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared DTLS active cycles allocate zero managed bytes."); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { client.Poll(); server.Poll(); }
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared DTLS idle cycles allocate zero managed bytes."); Console.WriteLine("64 DTLS active/idle packet cycles: 0 managed bytes.");
            Reject<InvalidOperationException>(() => Task.Run(client.Poll).GetAwaiter().GetResult()); client.DisconnectFromPeer(); Wait(() => server.Poll(), () => server.GetStatus() == TLSStatus.Disconnected); Check(wire.IsSocketConnected() && accepted!.IsSocketConnected(), "DTLS close preserves UDP ownership.");
        }
        finally { server?.Dispose(); accepted?.Dispose(); }
    }
    private static void Exchange(PacketPeerDTLS a, PacketPeerDTLS b, Span<byte> scratch)
    {
        a.PutPacket("ping"u8); var end = Environment.TickCount64 + 10000; while (b.GetAvailablePacketCount() == 0) { a.Poll(); b.Poll(); if (Environment.TickCount64 > end) throw new TimeoutException(); }
        Check(b.GetPacket(scratch) == 4 && scratch[..4].SequenceEqual("ping"u8), "DTLS ping."); b.PutPacket("pong"u8); while (a.GetAvailablePacketCount() == 0) { a.Poll(); b.Poll(); if (Environment.TickCount64 > end) throw new TimeoutException(); }
        Check(a.GetPacket(scratch) == 4 && scratch[..4].SequenceEqual("pong"u8), "DTLS pong.");
    }
    private sealed class Probe(PacketPeerDTLS client, PacketPeerDTLS server) : Node { protected override void OnEnterTree() => ProcessEnabled = true; protected override void OnProcess(double delta) { client.Poll(); server.Poll(); client.PutPacket("node"u8); ProcessEnabled = false; } }
    private static void Policies(TLSOptions serverOptions)
    {
        using var otherCrypto = new Crypto(); using var otherKey = otherCrypto.GenerateRSA(2048); using var otherCA = otherCrypto.GenerateSelfSignedCertificate(otherKey); using var unsafeOptions = TLSOptions.ClientUnsafe(); using var wrongCA = TLSOptions.Client(otherCA, "localhost"); using var unsafeWrongCA = TLSOptions.ClientUnsafe(otherCA); using var system = TLSOptions.Client();
        foreach (var options in new[] { unsafeOptions, wrongCA, unsafeWrongCA, system })
        {
            using var host = new UDPServer(); host.Listen(0, "127.0.0.1"); using var helper = new DTLSServer(); helper.Setup(serverOptions); using var wire = new PacketPeerUDP(); wire.ConnectToHost("127.0.0.1", host.GetLocalPort()); using var client = new PacketPeerDTLS(); client.ConnectToPeer(wire, "localhost", options); PacketPeerUDP? raw = null; PacketPeerDTLS? server = null; var failed = false;
            try
            {
                Wait(() => { try { client.Poll(); } catch (AuthenticationException) { failed = true; } host.Poll(); if (raw is null && host.IsConnectionAvailable()) { raw = host.TakeConnection(); server = helper.TakeConnection(raw!); } try { server?.Poll(); } catch (AuthenticationException) { } }, () => failed || client.GetStatus() == TLSStatus.Connected);
                Check(ReferenceEquals(options, unsafeOptions) ? !failed : failed && client.GetStatus() == TLSStatus.Error, "System/custom/unsafe chain policy.");
                if (!failed) { wire.Close(); wire.ConnectToHost("127.0.0.1", host.GetLocalPort()); Reject<InvalidOperationException>(client.Poll); Check(client.GetStatus() == TLSStatus.Error, "A reopened UDP generation cannot retain DTLS state."); }
            }
            finally { server?.Dispose(); raw?.Dispose(); }
        }
        using var emptyKey = new CryptoKey(); using var emptyCert = new X509Certificate(); using var invalid = TLSOptions.Server(emptyKey, emptyCert); using var retry = new DTLSServer(); Reject<System.Security.Cryptography.CryptographicException>(() => retry.Setup(invalid)); retry.Setup(serverOptions); Reject<InvalidOperationException>(() => Task.Run(retry.Dispose).GetAwaiter().GetResult()); Check(!retry.IsDisposed, "Failed server setup rolls back; wrong-owner disposal preserves helper.");
    }
    private static void Oracle(CryptoKey key, X509Certificate cert, TLSOptions serverOptions, TLSOptions clientOptions)
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-dtls-oracle-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); if (!OperatingSystem.IsWindows()) System.IO.File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); var keyPath = System.IO.Path.Combine(folder, "key.pem"); var certPath = System.IO.Path.Combine(folder, "cert.pem");
        try
        {
            key.Save(keyPath); cert.Save(certPath); if (!OperatingSystem.IsWindows()) System.IO.File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            int port; using (var reserve = new UDPServer()) { reserve.Listen(0, "127.0.0.1"); port = reserve.GetLocalPort(); }
            using (var oracle = StartOracle(["s_server", "-dtls1_2", "-accept", "127.0.0.1:" + port, "-cert", certPath, "-key", keyPath]))
            {
                try { var bound = OracleLine(oracle, "ACCEPT"); Wait(() => { }, () => bound.IsCompleted); bound.GetAwaiter().GetResult(); using var udp = new PacketPeerUDP(); udp.ConnectToHost("127.0.0.1", port); using var peer = new PacketPeerDTLS(); peer.ConnectToPeer(udp, "localhost", clientOptions); Wait(peer.Poll, () => peer.GetStatus() == TLSStatus.Connected); var read = OracleLine(oracle, "oracle"); peer.PutPacket("oracle\n"u8); Wait(peer.Poll, () => read.IsCompleted); Check(read.GetAwaiter().GetResult() == "oracle", "Independent native OpenSSL server decrypts packet."); oracle.StandardInput.WriteLine("answer"); oracle.StandardInput.Flush(); Wait(peer.Poll, () => peer.GetAvailablePacketCount() > 0); Check(Encoding.UTF8.GetString(peer.GetPacket()) == "answer\n", "Independent native server application reply."); }
                finally { StopOracle(oracle); }
            }
            using var host = new UDPServer(); host.Listen(0, "127.0.0.1"); using var helper = new DTLSServer(); helper.Setup(serverOptions); using var client = StartOracle(["s_client", "-quiet", "-dtls1_2", "-connect", "127.0.0.1:" + host.GetLocalPort(), "-CAfile", certPath, "-verify_return_error", "-verify_hostname", "localhost"]); PacketPeerUDP? raw = null; PacketPeerDTLS? server = null;
            void Poll() { host.Poll(); if (raw is null && host.IsConnectionAvailable()) { raw = host.TakeConnection(); server = helper.TakeConnection(raw!); } server?.Poll(); }
            try { Wait(Poll, () => server?.GetStatus() == TLSStatus.Connected); client.StandardInput.WriteLine("oracle"); client.StandardInput.Flush(); Wait(Poll, () => server!.GetAvailablePacketCount() > 0); Check(Encoding.UTF8.GetString(server!.GetPacket()) == "oracle\n", "Independent native client encrypts packet."); var read = client.StandardOutput.ReadLineAsync(); server.PutPacket("answer\n"u8); Wait(Poll, () => read.IsCompleted); Check(read.GetAwaiter().GetResult() == "answer", "Independent native client authenticates server/cookies and decrypts reply."); }
            finally { StopOracle(client); server?.Dispose(); raw?.Dispose(); }
        }
        finally { Directory.Delete(folder, true); }
    }
    private static async Task<string> OracleLine(Process process, string expected) { while (true) { var line = await process.StandardOutput.ReadLineAsync(); if (line is null) throw new IOException("Independent DTLS process ended before the expected output."); if (line == expected) return line; } }
    private static Process StartOracle(string[] args)
    {
        var start = new ProcessStartInfo("openssl") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true }; foreach (var argument in args) start.ArgumentList.Add(argument); var result = Process.Start(start)!; _ = result.StandardError.ReadToEndAsync(); return result;
    }
    private static void StopOracle(Process process) { if (!process.HasExited) process.Kill(true); Check(process.WaitForExit(3000), "Independent oracle exits."); }
    private static void Rejection(TLSOptions serverOptions, TLSOptions clientOptions, X509Certificate cert)
    {
        using var empty = new PacketPeerUDP(); using var unconfigured = new DTLSServer(); Reject<InvalidOperationException>(() => unconfigured.TakeConnection(empty)); Reject<ArgumentException>(() => unconfigured.Setup(clientOptions)); using var inactive = new PacketPeerDTLS(); Check(inactive.GetStatus() == TLSStatus.Disconnected && inactive.GetAvailablePacketCount() == 0, "DTLS defaults."); Reject<InvalidOperationException>(() => inactive.ConnectToPeer(empty, "localhost")); inactive.DisconnectFromPeer();
        using var host = new UDPServer(); host.Listen(0, "127.0.0.1"); using var helper = new DTLSServer(); helper.Setup(serverOptions); using var wire = new PacketPeerUDP(); wire.ConnectToHost("127.0.0.1", host.GetLocalPort()); using var wrong = TLSOptions.Client(cert, "wrong.test"); using var client = new PacketPeerDTLS(); client.ConnectToPeer(wire, "localhost", wrong); PacketPeerUDP? accepted = null; PacketPeerDTLS? server = null; var mismatch = false;
        try { Wait(() => { try { client.Poll(); } catch (AuthenticationException) { mismatch = true; } host.Poll(); if (accepted is null && host.IsConnectionAvailable()) { accepted = host.TakeConnection(); server = helper.TakeConnection(accepted!); } server?.Poll(); }, () => mismatch); Check(client.GetStatus() == TLSStatus.ErrorHostnameMismatch && wire.IsSocketConnected(), "Name mismatch has exact status and preserves UDP."); client.DisconnectFromPeer(); Check(client.GetStatus() == TLSStatus.Disconnected, "Error state resets explicitly."); }
        finally { server?.Dispose(); accepted?.Dispose(); }
    }
}
