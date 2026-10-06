using Electron2D;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using static OpenSSLOracle;
using Path = System.IO.Path;
using RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;
using Certificate = Electron2D.X509Certificate;

internal static class TLSTests
{
    internal static void Run()
    {
        TLSSystemTrustTests.Run();
        using var authorityKey = RSA.Create(2048);
        var authorityRequest = new CertificateRequest("CN=Electron2D test root", authorityKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        authorityRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        authorityRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        using var authority = authorityRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(3));
        using var leafKey = RSA.Create(2048); using var leaf = Leaf(leafKey, authority, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddDays(1));
        using var key = new CryptoKey(); key.LoadFromString(leafKey.ExportRSAPrivateKeyPem());
        using var chain = new Certificate(); chain.LoadFromString(leaf.ExportCertificatePem() + "\n" + authority.ExportCertificatePem());
        using var trust = new Certificate(); trust.LoadFromString(authority.ExportCertificatePem());
        Resources(key, chain, trust, leafKey, leaf); Fragments(key, chain, trust); Abandoned(key, chain, trust); TCP(key, chain, trust); Failures(key, chain, trust, leafKey, authority);
        Interop(leaf, leafKey, authority, trust, key, chain, SslProtocols.Tls12);
        if (!OperatingSystem.IsMacOS()) Interop(leaf, leafKey, authority, trust, key, chain, SslProtocols.Tls13);
        if (!OperatingSystem.IsWindows()) OpenSSLInterop(trust, key, chain);
        Console.WriteLine("TLS native handshake/trust/name/records, PEM/DER resources, fragmented streams, socket/oracle interoperability and lifetime checks passed.");
    }
    private static X509Certificate2 Leaf(RSA key, X509Certificate2 authority, DateTimeOffset start, DateTimeOffset end)
    {
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        var names = new SubjectAlternativeNameBuilder(); names.AddDnsName("localhost"); names.AddIpAddress(IPAddress.Loopback); request.CertificateExtensions.Add(names.Build());
        return request.Create(authority, start, end, RandomNumberGenerator.GetBytes(16));
    }
    private static void Resources(CryptoKey key, Certificate chain, Certificate trust, RSA source, X509Certificate2 leaf)
    {
        using (var identity = TLSOracleIdentity.ForServer(leaf.CopyWithPrivateKey(source)))
        {
            Check(identity.HasPrivateKey && identity.RawData.AsSpan().SequenceEqual(leaf.RawData), "Oracle identity preserves the leaf certificate and private key.");
            using var privateKey = identity.GetRSAPrivateKey()!; using var verifier = leaf.GetRSAPublicKey()!;
            var signature = privateKey.SignData("TLS oracle identity"u8, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Check(verifier.VerifyData("TLS oracle identity"u8, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1), "Oracle provider key matches the original certificate.");
        }
        Check(!key.IsPublicOnly() && chain.SaveToString().Split("BEGIN CERTIFICATE").Length == 3, "Private key and chain identity.");
        using var publicKey = new CryptoKey(); publicKey.LoadFromString(key.SaveToString(true), true); Check(publicKey.IsPublicOnly(), "Public-only key."); Reject<CryptographicException>(() => publicKey.SaveToString()); Reject<CryptographicException>(() => publicKey.LoadFromString(key.SaveToString(), true));
        using var copy = (CryptoKey)key.Duplicate(); Check(copy.SaveToString(true) == key.SaveToString(true), "Key resource copy.");
        using var copiedChain = (Certificate)chain.Duplicate(); Check(copiedChain.SaveToString() == chain.SaveToString(), "Certificate resource copy retains full chain.");
        var original = key.SaveToString(); Reject<CryptographicException>(() => key.LoadFromString("invalid")); Check(key.SaveToString() == original, "Malformed key rollback.");
        var originalChain = chain.SaveToString(); Reject<CryptographicException>(() => chain.LoadFromString("invalid")); Check(chain.SaveToString() == originalChain, "Malformed chain rollback.");
        using var options = TLSOptions.Server(key, chain); Check(options.IsServer() && options.GetPrivateKey() == key && options.GetOwnCertificate() == chain, "Options borrow resource identity.");
        using var client = TLSOptions.Client(trust, "localhost"); Check(client.GetTrustedCAChain() == trust && client.GetCommonNameOverride() == "localhost" && !client.IsUnsafeClient(), "Client options.");
        using var empty = new Certificate(); Check(empty.SaveToString() == "", "Empty certificate export.");
        var folder = Path.Combine(Path.GetTempPath(), "e2d-tls-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var keyPath = Path.Combine(folder, "identity.key"); var chainPath = Path.Combine(folder, "identity.crt"); key.Save(keyPath); chain.Save(chainPath);
            using var loadedKey = new CryptoKey(); loadedKey.Load(keyPath); using var loadedChain = new Certificate(); loadedChain.Load(chainPath); Check(loadedKey.SaveToString(true) == originalPublic() && loadedChain.SaveToString() == chain.SaveToString(), "Filesystem key/chain roundtrip.");
            using var cachedKey = ResourceLoader.Load<CryptoKey>(keyPath); using var cachedCertificate = ResourceLoader.Load<Certificate>(chainPath); Check(ReferenceEquals(cachedKey, ResourceLoader.Load<CryptoKey>(keyPath)) && ResourceLoader.Exists<Certificate>(chainPath), "Security resources share typed weak cache.");
            Check(ResourceLoader.GetRecognizedExtensionsForType<Resource>().Contains("crt") && ResourceLoader.GetRecognizedExtensionsForType<CryptoKey>().SequenceEqual(new[] { "key" }), "Security loader discovery.");
            using (var independent = ResourceLoader.Load<CryptoKey>(keyPath, ResourceLoader.CacheMode.Ignore)) Check(!ReferenceEquals(independent, cachedKey), "Independent private-key load.");
            cachedCertificate.ResourceName = "identity"; Check(ReferenceEquals(cachedCertificate, ResourceLoader.Load<Certificate>(chainPath, ResourceLoader.CacheMode.Replace)) && cachedCertificate.ResourceName == "identity", "Certificate replacement preserves identity.");
            File.WriteAllText(keyPath, "invalid"); Reject<CryptographicException>(() => ResourceLoader.Load<CryptoKey>(keyPath, ResourceLoader.CacheMode.Replace)); Check(cachedKey.SaveToString(true) == key.SaveToString(true), "Malformed loader replacement preserves key.");
            File.WriteAllBytes(keyPath, source.ExportPkcs8PrivateKey()); cachedKey.ResourceName = "key identity"; Check(ReferenceEquals(cachedKey, ResourceLoader.Load<CryptoKey>(keyPath, ResourceLoader.CacheMode.Replace)) && cachedKey.ResourceName == "key identity", "Key replacement preserves identity metadata."); using var derKey = new CryptoKey(); derKey.Load(keyPath); File.WriteAllBytes(chainPath, leaf.RawData); using var derChain = new Certificate(); derChain.Load(chainPath); Check(derKey.SaveToString(true) == key.SaveToString(true) && derChain.SaveToString() == leaf.ExportCertificatePem(), "DER filesystem input.");
        }
        finally { Directory.Delete(folder, true); }
        string originalPublic() => key.SaveToString(true);
        var deliveries = 0; Action<Resource> failedObserver = _ => { deliveries++; throw new CryptographicException("Observer failure."); }; copy.Changed += failedObserver; Reject<CryptographicException>(() => copy.LoadFromString(key.SaveToString())); copy.Changed -= failedObserver; Check(deliveries == 1 && copy.SaveToString(true) == key.SaveToString(true), "Key observer failure occurs once after commitment.");
        using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256); using var ecKey = new CryptoKey(); ecKey.LoadFromString(ec.ExportECPrivateKeyPem()); using var ecPublic = new CryptoKey(); ecPublic.LoadFromString(ecKey.SaveToString(true), true); Check(ecPublic.IsPublicOnly(), "Elliptic key import/export.");
        var ecRequest = new CertificateRequest("CN=localhost", ec, HashAlgorithmName.SHA256); using var ecIdentity = ecRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1)); using var ecChain = new Certificate(); ecChain.LoadFromString(ecIdentity.ExportCertificatePem()); Attempt(TLSOptions.Client(ecChain), "localhost", ecChain, ecKey, TLSStatus.Connected);
    }
    private sealed class Duplex : StreamPeer
    {
        private readonly byte[] _bytes = new byte[131072]; private int _head, _count;
        internal Duplex Other = null!;
        internal int Fragment = 257;
        public override int GetAvailableBytes() { CheckStream(); return _count; }
        protected override int ReadCore(Span<byte> destination, bool block)
        {
            var count = Math.Min(Math.Min(destination.Length, _count), Fragment);
            for (var i = 0; i < count; i++) destination[i] = _bytes[(_head + i) % _bytes.Length]; _head = (_head + count) % _bytes.Length; _count -= count; return count;
        }
        protected override int WriteCore(ReadOnlySpan<byte> data, bool block)
        {
            var count = Math.Min(Math.Min(data.Length, Other._bytes.Length - Other._count), Fragment);
            for (var i = 0; i < count; i++) Other._bytes[(Other._head + Other._count + i) % Other._bytes.Length] = data[i]; Other._count += count; return count;
        }
    }
    private static void Fragments(CryptoKey key, Certificate chain, Certificate trust)
    {
        using var a = new Duplex(); using var b = new Duplex(); a.Other = b; b.Other = a;
        using var clientOptions = TLSOptions.Client(trust); using var serverOptions = TLSOptions.Server(key, chain);
        using var client = new StreamPeerTLS(); using var server = new StreamPeerTLS(); Check(client.GetStatus() == TLSStatus.Disconnected && client.GetStream() is null, "TLS initial state.");
        server.AcceptStream(b, serverOptions); client.ConnectToStream(a, "localhost", clientOptions); Handshake(client, server);
        Reject<InvalidOperationException>(() => key.LoadFromString(key.SaveToString())); Reject<InvalidOperationException>(key.Dispose); Reject<InvalidOperationException>(chain.Dispose); using (var duplicate = (CryptoKey)key.Duplicate()) Reject<InvalidOperationException>(() => key.CopyFromResource(duplicate)); Reject<InvalidOperationException>(() => trust.LoadFromString(trust.SaveToString())); Check(!key.IsDisposed && !chain.IsDisposed, "Retained resource disposal rejected before mutation.");
        Reject<InvalidOperationException>(() => client.ConnectToStream(a, "localhost", clientOptions)); Reject<InvalidOperationException>(() => Task.Run(client.Poll).GetAwaiter().GetResult()); Reject<InvalidOperationException>(() => Task.Run(client.Dispose).GetAwaiter().GetResult());
        var send = new byte[131072]; Random.Shared.NextBytes(send); var received = new byte[send.Length]; var offset = 0; var read = 0;
        Wait(() => { if (offset < send.Length) offset += client.PutPartialData(send.AsSpan(offset)); client.Poll(); server.Poll(); if (read < received.Length) read += server.GetPartialData(received.AsSpan(read)); return offset == send.Length && read == received.Length; }); Check(received.SequenceEqual(send), "Fragmented TLS record/large payload.");
        a.Fragment = b.Fragment = 65536;
        for (var i = 0; i < 64; i++) RoundTrip(client, server);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) RoundTrip(client, server); var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, "TLS warm transport allocated " + allocated); Console.WriteLine("64 TLS active number/poll cycles: " + allocated + " managed bytes.");
        before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { client.Poll(); server.Poll(); _ = client.GetStatus(); _ = client.GetAvailableBytes(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "TLS idle allocation.");
        client.DisconnectFromStream(); Wait(() => { server.Poll(); return server.GetStatus() == TLSStatus.Disconnected; }); Check(!a.IsDisposed && !b.IsDisposed && client.GetStream() is null, "TLS close-notify and borrowed stream lifetime.");
        key.LoadFromString(key.SaveToString()); trust.LoadFromString(trust.SaveToString());
    }
    private static void Abandoned(CryptoKey key, Certificate chain, Certificate trust)
    {
        var peers = AbandonSessions(key, chain, trust);
        for (var i = 0; i < 5 && peers.Any(p => p.IsAlive); i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Check(peers.All(p => !p.IsAlive), "Abandoned TLS sessions finalize."); key.LoadFromString(key.SaveToString()); trust.LoadFromString(trust.SaveToString());
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference[] AbandonSessions(CryptoKey key, Certificate chain, Certificate trust)
    {
        var a = new Duplex(); var b = new Duplex(); a.Other = b; b.Other = a;
        using var serverOptions = TLSOptions.Server(key, chain); using var clientOptions = TLSOptions.Client(trust);
        var server = new StreamPeerTLS(); var client = new StreamPeerTLS(); server.AcceptStream(b, serverOptions); client.ConnectToStream(a, "localhost", clientOptions); Handshake(client, server);
        return [new WeakReference(client), new WeakReference(server)];
    }
    private static void TCP(CryptoKey key, Certificate chain, Certificate trust)
    {
        using var listener = new TCPServer(); listener.Listen(0, "127.0.0.1"); using var socket = new StreamPeerTCP(); socket.ConnectToHost("127.0.0.1", listener.GetLocalPort()); Wait(() => { socket.Poll(); return socket.GetStatus() == StreamSocketStatus.Connected && listener.IsConnectionAvailable(); }); using var accepted = listener.TakeConnection()!;
        using var options = TLSOptions.Server(key, chain); using var validation = TLSOptions.Client(trust); using var server = new StreamPeerTLS(); using var client = new StreamPeerTLS(); server.AcceptStream(accepted, options); client.ConnectToStream(socket, "127.0.0.1", validation); Handshake(client, server); RoundTrip(client, server);
        client.DisconnectFromStream(); Wait(() => { server.Poll(); return server.GetStatus() == TLSStatus.Disconnected; }); Check(socket.GetStatus() == StreamSocketStatus.Connected, "TLS disconnect preserves raw TCP socket.");
        server.AcceptStream(accepted, options); client.ConnectToStream(socket, "localhost", validation); Handshake(client, server); RoundTrip(client, server);
    }
    private static void Failures(CryptoKey key, Certificate chain, Certificate trust, RSA leafKey, X509Certificate2 authority)
    {
        Attempt(TLSOptions.Client(trust), "wrong.invalid", chain, key, TLSStatus.ErrorHostnameMismatch); Attempt(TLSOptions.Client(), "localhost", chain, key, TLSStatus.Error);
        using var otherKey = RSA.Create(2048); using var otherRoot = new Certificate(); var request = new CertificateRequest("CN=Other", otherKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1); using var other = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)); otherRoot.LoadFromString(other.ExportCertificatePem());
        Attempt(TLSOptions.Client(otherRoot), "localhost", chain, key, TLSStatus.Error); Attempt(TLSOptions.ClientUnsafe(otherRoot), "wrong.invalid", chain, key, TLSStatus.Error); Attempt(TLSOptions.ClientUnsafe(), "wrong.invalid", chain, key, TLSStatus.Connected); Attempt(TLSOptions.ClientUnsafe(trust), "wrong.invalid", chain, key, TLSStatus.Connected); Attempt(TLSOptions.Client(trust, "localhost"), "wrong.invalid", chain, key, TLSStatus.Connected);
        using var expired = Leaf(leafKey, authority, DateTimeOffset.UtcNow.AddHours(-5), DateTimeOffset.UtcNow.AddHours(-1)); using var expiredChain = new Certificate(); expiredChain.LoadFromString(expired.ExportCertificatePem()); Attempt(TLSOptions.Client(trust), "localhost", expiredChain, key, TLSStatus.Error);
        using var wrongKey = new CryptoKey(); wrongKey.LoadFromString(otherKey.ExportPkcs8PrivateKeyPem()); using var options = TLSOptions.Server(wrongKey, chain); using var stream = new StreamPeerBuffer(); using var peer = new StreamPeerTLS(); Reject<AuthenticationException>(() => peer.AcceptStream(stream, options)); Check(peer.GetStatus() == TLSStatus.Error && peer.GetStream() is null, "Mismatched server identity cleanup."); wrongKey.LoadFromString(wrongKey.SaveToString());
        using var clientOptions = TLSOptions.Client(); Reject<ArgumentException>(() => peer.AcceptStream(stream, clientOptions)); Reject<ArgumentException>(() => peer.ConnectToStream(peer, "localhost"));
    }
    private static void Attempt(TLSOptions options, string name, Certificate chain, CryptoKey key, TLSStatus expected)
    {
        using (options)
        {
            using var a = new Duplex(); using var b = new Duplex(); a.Other = b; b.Other = a; using var serverOptions = TLSOptions.Server(key, chain); using var server = new StreamPeerTLS(); using var client = new StreamPeerTLS(); server.AcceptStream(b, serverOptions); client.ConnectToStream(a, name, options);
            try { Handshake(client, server); } catch (AuthenticationException) { }
            Check(client.GetStatus() == expected, "TLS validation outcome: " + expected + ", actual " + client.GetStatus()); if (expected != TLSStatus.Connected) Check(client.GetStream() is null && !a.IsDisposed, "Failed TLS preserves borrowed transport.");
        }
    }
    private static void Interop(X509Certificate2 leaf, RSA leafKey, X509Certificate2 authority, Certificate trust, CryptoKey key, Certificate chain, SslProtocols protocol)
    {
        using var identity = TLSOracleIdentity.ForServer(leaf.CopyWithPrivateKey(leafKey)); using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Console.WriteLine("Independent SslStream " + protocol + " server handshake.");
        var oracle = Task.Run(async () =>
        {
            using var accepted = await listener.AcceptTcpClientAsync(); using var ssl = new SslStream(accepted.GetStream(), false);
            try
            {
                await ssl.AuthenticateAsServerAsync(identity, false, protocol, false); Check(ssl.SslProtocol == protocol, "Independent forced " + protocol + " negotiation.");
                var bytes = new byte[8]; await ssl.ReadExactlyAsync(bytes); Check(System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes) == 42, "Independent TLS server reads wire value.");
                System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(bytes, 99); await ssl.WriteAsync(bytes); await ssl.ShutdownAsync();
            }
            catch (Exception error) { Console.Error.WriteLine("Independent SslStream " + protocol + " server failed: " + error); throw; }
        });
        using var raw = new StreamPeerTCP(); raw.ConnectToHost("127.0.0.1", port); Wait(() => { raw.Poll(); return raw.GetStatus() == StreamSocketStatus.Connected; }); using var options = TLSOptions.Client(trust); using var peer = new StreamPeerTLS(); peer.ConnectToStream(raw, "localhost", options); Wait(() => { peer.Poll(); return peer.GetStatus() == TLSStatus.Connected; }); peer.PutU64(42); Wait(() => { peer.Poll(); return peer.GetAvailableBytes() >= 8; }); Check(peer.GetU64() == 99, "Independent SslStream server reply."); Wait(() => oracle.IsCompleted); oracle.GetAwaiter().GetResult();
        Wait(() => { peer.Poll(); return peer.GetStatus() == TLSStatus.Disconnected; });
        Check(!raw.IsDisposed && peer.GetStream() is null, "Independent server close notification preserves borrowed transport ownership.");
        foreach (var graceful in new[] { true, false })
        {
            using var engineListener = new TCPServer(); engineListener.Listen(0, "127.0.0.1"); var enginePort = engineListener.GetLocalPort();
            var reverse = Task.Run(async () =>
            {
                using var socket = new TcpClient(); await socket.ConnectAsync(IPAddress.Loopback, enginePort); using var ssl = new SslStream(socket.GetStream(), false);
                var policy = new X509ChainPolicy { TrustMode = X509ChainTrustMode.CustomRootTrust, RevocationMode = X509RevocationMode.NoCheck }; policy.CustomTrustStore.Add(authority);
                await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = "localhost", CertificateChainPolicy = policy, EnabledSslProtocols = protocol });
                Check(ssl.SslProtocol == protocol, "Independent forced " + protocol + " client negotiation.");
                var bytes = new byte[8]; System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(bytes, 123); await ssl.WriteAsync(bytes); await ssl.ReadExactlyAsync(bytes);
                Check(System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes) == 456, "Independent TLS client reply.");
                if (graceful) await ssl.ShutdownAsync();
                else socket.Client.Shutdown(SocketShutdown.Both);
            });
            Wait(engineListener.IsConnectionAvailable); using var engineSocket = engineListener.TakeConnection()!; using var serverOptions = TLSOptions.Server(key, chain); using var server = new StreamPeerTLS();
            server.AcceptStream(engineSocket, serverOptions);
            void Poll() { if (reverse.IsCompleted) reverse.GetAwaiter().GetResult(); server.Poll(); }
            Wait(() => { Poll(); return server.GetStatus() == TLSStatus.Connected; }); Wait(() => { Poll(); return server.GetAvailableBytes() >= 8; });
            Check(server.GetU64() == 123, "Independent SslStream client request."); server.PutU64(456); Wait(() => reverse.IsCompleted); reverse.GetAwaiter().GetResult();
            if (graceful) Wait(() => { server.Poll(); return server.GetStatus() == TLSStatus.Disconnected; });
            else
            {
                Reject<AuthenticationException>(() => Wait(() => { server.Poll(); return server.GetStatus() == TLSStatus.Disconnected; }));
                Check(server.GetStatus() == TLSStatus.Error, "An abrupt TLS EOF remains an authentication error.");
            }
            Check(!engineSocket.IsDisposed && server.GetStream() is null, "Both TLS closure paths preserve borrowed transport ownership.");
        }
        Console.WriteLine("Independent SslStream " + protocol + " client/server records, trust and graceful/abrupt close checks passed.");
    }
    private static void OpenSSLInterop(Certificate trust, CryptoKey key, Certificate chain)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-tls-oracle-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var keyPath = Path.Combine(folder, "key.pem"); var certPath = Path.Combine(folder, "cert.pem"); var trustPath = Path.Combine(folder, "trust.pem");
        try
        {
            key.Save(keyPath); chain.Save(certPath); trust.Save(trustPath);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            int port; using (var reserve = new TCPServer()) { reserve.Listen(0, "127.0.0.1"); port = reserve.GetLocalPort(); }
            using (var oracle = StartOracle(["s_server", "-quiet", "-no_ign_eof", "-tls1_3", "-naccept", "1", "-accept", "127.0.0.1:" + port, "-cert", certPath, "-key", keyPath], out var error))
            {
                try
                {
                    using var raw = new StreamPeerTCP();
                    Wait(() =>
                    {
                        CheckOracle(oracle, error);
                        try { if (raw.GetStatus() == StreamSocketStatus.None) raw.ConnectToHost("127.0.0.1", port); raw.Poll(); return raw.GetStatus() == StreamSocketStatus.Connected; }
                        catch (SocketException failure) when (failure.SocketErrorCode == SocketError.ConnectionRefused) { raw.DisconnectFromHost(); Thread.Sleep(10); return false; }
                    });
                    using var options = TLSOptions.Client(trust); using var peer = new StreamPeerTLS(); peer.ConnectToStream(raw, "localhost", options);
                    Wait(() => { CheckOracle(oracle, error); peer.Poll(); return peer.GetStatus() == TLSStatus.Connected; });
                    var bytes = new byte[8]; var read = oracle.StandardOutput.BaseStream.ReadExactlyAsync(bytes).AsTask(); peer.PutU64(42);
                    Wait(() => { CheckOracle(oracle, error); peer.Poll(); return read.IsCompleted; }); read.GetAwaiter().GetResult();
                    Check(System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes) == 42, "Forced TLS 1.3 OpenSSL server decrypts wire value.");
                    System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(bytes, 99); oracle.StandardInput.BaseStream.Write(bytes); oracle.StandardInput.BaseStream.Flush();
                    Wait(() => { CheckOracle(oracle, error); peer.Poll(); return peer.GetAvailableBytes() >= bytes.Length; }); Check(peer.GetU64() == 99, "Forced TLS 1.3 OpenSSL server reply.");
                    oracle.StandardInput.Close(); Wait(() => { peer.Poll(); return peer.GetStatus() == TLSStatus.Disconnected && oracle.HasExited; }); CheckOracle(oracle, error);
                }
                finally { StopOracle(oracle); }
            }
            using var listener = new TCPServer(); listener.Listen(0, "127.0.0.1");
            using var client = StartOracle(["s_client", "-quiet", "-no_ign_eof", "-tls1_3", "-connect", "127.0.0.1:" + listener.GetLocalPort(), "-CAfile", trustPath, "-verify_return_error", "-verify_hostname", "localhost"], out var clientError);
            try
            {
                Wait(() => { CheckOracle(client, clientError); return listener.IsConnectionAvailable(); });
                using var raw = listener.TakeConnection()!; using var options = TLSOptions.Server(key, chain); using var peer = new StreamPeerTLS(); peer.AcceptStream(raw, options);
                Wait(() => { CheckOracle(client, clientError); peer.Poll(); return peer.GetStatus() == TLSStatus.Connected; });
                var bytes = new byte[8]; System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(bytes, 123); client.StandardInput.BaseStream.Write(bytes); client.StandardInput.BaseStream.Flush();
                Wait(() => { CheckOracle(client, clientError); peer.Poll(); return peer.GetAvailableBytes() >= bytes.Length; }); Check(peer.GetU64() == 123, "Forced TLS 1.3 OpenSSL client request.");
                var read = client.StandardOutput.BaseStream.ReadExactlyAsync(bytes).AsTask(); peer.PutU64(456);
                Wait(() => { CheckOracle(client, clientError); peer.Poll(); return read.IsCompleted; }); read.GetAwaiter().GetResult();
                Check(System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes) == 456, "Forced TLS 1.3 OpenSSL client authenticates and decrypts reply.");
                client.StandardInput.Close(); Wait(() => { peer.Poll(); return peer.GetStatus() == TLSStatus.Disconnected && client.HasExited; }); CheckOracle(client, clientError);
            }
            finally { StopOracle(client); }
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine("Independent OpenSSL TLS 1.3 client/server records, trust/name validation and close notifications passed.");
    }
    private static void Handshake(StreamPeerTLS a, StreamPeerTLS b) => Wait(() => { a.Poll(); b.Poll(); return a.GetStatus() == TLSStatus.Connected && b.GetStatus() == TLSStatus.Connected; });
    private static void RoundTrip(StreamPeerTLS a, StreamPeerTLS b)
    {
        a.PutU64(123); var start = Environment.TickCount64; while (b.GetAvailableBytes() < 8) { a.Poll(); b.Poll(); if (Environment.TickCount64 - start > 5000) throw new TimeoutException(); }
        Check(b.GetU64() == 123, "TLS request value."); b.PutU64(456); while (a.GetAvailableBytes() < 8) { a.Poll(); b.Poll(); if (Environment.TickCount64 - start > 5000) throw new TimeoutException(); }
        Check(a.GetU64() == 456, "TLS response value.");
    }
    private static void Wait(Func<bool> condition) { var start = Environment.TickCount64; while (!condition()) { if (Environment.TickCount64 - start > 10000) throw new TimeoutException("TLS test deadline."); Thread.Yield(); } }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
