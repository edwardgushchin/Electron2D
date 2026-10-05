using Electron2D;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Certificate = Org.BouncyCastle.Tls.Certificate;

internal static class DTLSManagedOracle
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Wait(Task oracle, Action poll, Func<bool> ready)
    {
        var end = Environment.TickCount64 + 10000;
        while (!ready()) { if (oracle.IsFaulted) oracle.GetAwaiter().GetResult(); poll(); if (Environment.TickCount64 > end) throw new TimeoutException("Independent DTLS oracle deadline."); Thread.Yield(); }
    }
    private static void Finish(Socket socket, Task oracle, bool success)
    {
        socket.Dispose();
        try { oracle.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult(); }
        catch (Exception) when (!success && oracle.IsCompleted) { }
    }
    internal static void Run(CryptoKey key, Electron2D.X509Certificate cert, TLSOptions serverOptions, TLSOptions clientOptions)
    {
        using var identity = X509Certificate2.CreateFromPem(cert.SaveToString()); var certificate = identity.RawData;
        using var rsa = System.Security.Cryptography.RSA.Create(); rsa.ImportFromPem(key.SaveToString());
        var privateKey = PrivateKeyFactory.CreateKey(rsa.ExportPkcs8PrivateKey());
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)); var port = ((IPEndPoint)socket.LocalEndPoint!).Port;
            var oracle = Task.Run(() =>
            {
                var transport = new Wire(socket); var server = new Server(certificate, privateKey); var verifier = new DtlsVerifier(server.Crypto); var bytes = new byte[1200]; DtlsRequest? request = null; var challenged = false;
                while (request is null)
                {
                    var count = transport.Receive(bytes, 0, bytes.Length, 100); if (count <= 0) continue;
                    request = verifier.VerifyRequest(System.Text.Encoding.UTF8.GetBytes(socket.RemoteEndPoint!.ToString()!), bytes, 0, count, transport); if (request is null) challenged = true;
                }
                Check(challenged, "Independent DTLS server requires its cookie challenge."); var tls = new DtlsServerProtocol().Accept(server, transport, request);
                try { Receive(tls, "oracle"u8); tls.Send("answer"u8); Receive(tls, "ack"u8); }
                finally { tls.Close(); }
            });
            var success = false;
            try
            {
                using var udp = new PacketPeerUDP(); udp.ConnectToHost("127.0.0.1", port); using var peer = new PacketPeerDTLS(); peer.ConnectToPeer(udp, "localhost", clientOptions);
                Wait(oracle, peer.Poll, () => peer.GetStatus() == TLSStatus.Connected); peer.PutPacket("oracle"u8);
                Wait(oracle, peer.Poll, () => peer.GetAvailablePacketCount() > 0); Check(peer.GetPacket().AsSpan().SequenceEqual("answer"u8), "Independent managed DTLS server reply bytes.");
                peer.PutPacket("ack"u8);
                Wait(oracle, peer.Poll, () => oracle.IsCompleted); oracle.GetAwaiter().GetResult();
                Wait(oracle, peer.Poll, () => peer.GetStatus() == TLSStatus.Disconnected); Check(udp.IsSocketConnected(), "Independent DTLS server close preserves borrowed UDP."); success = true;
            }
            finally { Finish(socket, oracle, success); }
        }
        using var host = new UDPServer(); host.Listen(0, "127.0.0.1"); using var helper = new DTLSServer(); helper.Setup(serverOptions);
        using var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); clientSocket.Connect(IPAddress.Loopback, host.GetLocalPort());
        var client = Task.Run(() =>
        {
            var tls = new DtlsClientProtocol().Connect(new Client(certificate), new Wire(clientSocket));
            try { tls.Send("oracle"u8); Receive(tls, "answer"u8); }
            finally { tls.Close(); }
        });
        PacketPeerUDP? raw = null; PacketPeerDTLS? peerServer = null; var completed = false;
        void Poll() { host.Poll(); if (raw is null && host.IsConnectionAvailable()) { raw = host.TakeConnection(); peerServer = helper.TakeConnection(raw!); } peerServer?.Poll(); }
        try
        {
            Wait(client, Poll, () => peerServer?.GetStatus() == TLSStatus.Connected); Wait(client, Poll, () => peerServer!.GetAvailablePacketCount() > 0);
            Check(peerServer!.GetPacket().AsSpan().SequenceEqual("oracle"u8), "Independent managed DTLS client request bytes."); peerServer.PutPacket("answer"u8);
            Wait(client, Poll, () => client.IsCompleted); client.GetAwaiter().GetResult();
            Wait(client, Poll, () => peerServer.GetStatus() == TLSStatus.Disconnected); Check(raw!.IsSocketConnected(), "Independent DTLS client close preserves borrowed UDP."); completed = true;
        }
        finally { try { Finish(clientSocket, client, completed); } finally { peerServer?.Dispose(); raw?.Dispose(); } }
        Console.WriteLine("Independent Bouncy Castle DTLS 1.2 client/server, identity and exact packet checks passed.");
    }
    private static void Receive(DtlsTransport transport, ReadOnlySpan<byte> expected)
    {
        Span<byte> bytes = stackalloc byte[488]; var count = transport.Receive(bytes, 10000);
        Check(count == expected.Length && bytes[..count].SequenceEqual(expected), "Independent managed DTLS decrypts exact datagram bytes.");
    }
    private sealed class Wire(Socket socket) : DatagramTransport
    {
        private readonly long _end = Environment.TickCount64 + 10000;
        public int GetReceiveLimit() => 1200;
        public int GetSendLimit() => 1200;
        public int Receive(byte[] buf, int off, int len, int waitMillis) => Receive(buf.AsSpan(off, len), waitMillis);
        public int Receive(Span<byte> buffer, int waitMillis)
        {
            var left = _end - Environment.TickCount64; if (left <= 0) throw new TimeoutException("Independent DTLS UDP deadline.");
            if (!socket.Poll(TimeSpan.FromMilliseconds(waitMillis == 0 ? left : Math.Min(waitMillis, left)), SelectMode.SelectRead)) return -1;
            if (socket.Connected) return socket.Receive(buffer);
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0); var count = socket.ReceiveFrom(buffer, ref remote); socket.Connect(remote); return count;
        }
        public void Send(byte[] buf, int off, int len) => Send(buf.AsSpan(off, len));
        public void Send(ReadOnlySpan<byte> buffer) => Check(socket.Send(buffer) == buffer.Length, "Independent UDP sends complete datagrams.");
        public void Close() => socket.Dispose();
    }
    private sealed class Server(byte[] certificate, AsymmetricKeyParameter privateKey) : DefaultTlsServer(new BcTlsCrypto())
    {
        protected override ProtocolVersion[] GetSupportedVersions() => ProtocolVersion.DTLSv12.Only();
        protected override int[] GetSupportedCipherSuites() => [CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256];
        public override int GetHandshakeTimeoutMillis() => 10000;
        protected override TlsCredentialedSigner GetRsaSignerCredentials() => new BcDefaultTlsCredentialedSigner(new TlsCryptoParameters(m_context), (BcTlsCrypto)Crypto, privateKey, new Certificate([Crypto.CreateCertificate(certificate)]), new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.rsa));
    }
    private sealed class Client(byte[] certificate) : DefaultTlsClient(new BcTlsCrypto())
    {
        protected override ProtocolVersion[] GetSupportedVersions() => ProtocolVersion.DTLSv12.Only();
        protected override int[] GetSupportedCipherSuites() => [CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256];
        public override int GetHandshakeTimeoutMillis() => 10000;
        public override TlsAuthentication GetAuthentication() => new Authentication(m_context, certificate);
    }
    private sealed class Authentication(TlsContext context, byte[] certificate) : TlsAuthentication
    {
        public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
        {
            var chain = serverCertificate.Certificate.GetCertificateList();
            if (chain.Length != 1 || !chain[0].GetEncoded().AsSpan().SequenceEqual(certificate)) throw new TlsFatalAlert(AlertDescription.bad_certificate);
            using var identity = X509CertificateLoader.LoadCertificate(certificate);
            if (!identity.MatchesHostname("localhost")) throw new TlsFatalAlert(AlertDescription.bad_certificate);
            using var validation = new X509Chain(); validation.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust; validation.ChainPolicy.CustomTrustStore.Add(identity);
            validation.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; validation.ChainPolicy.DisableCertificateDownloads = true; validation.ChainPolicy.ApplicationPolicy.Add(new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.1"));
            if (!validation.Build(identity)) throw new TlsFatalAlert(AlertDescription.bad_certificate);
            TlsUtilities.CheckPeerSigAlgs(context, chain);
        }
        public TlsCredentials? GetClientCredentials(Org.BouncyCastle.Tls.CertificateRequest certificateRequest) => null;
    }
}
