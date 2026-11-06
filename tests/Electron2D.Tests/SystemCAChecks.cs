using Electron2D;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Certificate = Electron2D.X509Certificate;
using IOPath = System.IO.Path;

internal static class SystemCAChecks
{
    private static int _checks;
    internal static void Run(string inspector, string output, string? liveHost = null)
    {
        Directory.CreateDirectory(output); var native = IOPath.Combine(output, "native-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(native);
        var start = new ProcessStartInfo(inspector) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(native);
        using (var oracle = Process.Start(start) ?? throw new Exception("The native certificate inspector did not start."))
        {
            if (!oracle.WaitForExit(10000)) { oracle.Kill(); throw new TimeoutException("The native certificate inspector did not finish."); }
            Console.Write(oracle.StandardOutput.ReadToEnd()); Check(oracle.ExitCode == 0, "Independent native store query: " + oracle.StandardError.ReadToEnd());
        }
        var expected = Directory.GetFiles(native, "*.der").Select(path => Hash(File.ReadAllBytes(path))).Order().ToArray();
        var pem = OS.GetSystemCACertificates(); File.WriteAllText(IOPath.Combine(output, "snapshot.pem"), pem);
        Check(!DisplayServer.IsAvailable, "System certificate export does not open a display.");
        var actual = Decode(pem);
        Check(expected.Length != 0 && actual.SequenceEqual(expected), "Actual trusted root DER set agrees with the independent CryptoAPI query.");
        Check(!pem.Contains('\r') && !pem.Contains("PRIVATE KEY", StringComparison.Ordinal), "PEM contains LF public certificate blocks only.");
        using var trust = new Certificate(); trust.LoadFromString(pem);
        Check(Decode(trust.SaveToString()).SequenceEqual(actual), "The entire OS snapshot loads and roundtrips through the public certificate resource.");
        Check(Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Decode(OS.GetSystemCACertificates())))).GetAwaiter().GetResult().All(value => value.SequenceEqual(actual)), "Concurrent independent snapshots preserve the native root set.");
        using var options = TLSOptions.Client(trust); Check(ReferenceEquals(options.GetTrustedCAChain(), trust) && !options.IsUnsafeClient(), "The exported snapshot becomes an explicitly selected validating TLS trust chain.");
        using var key = RSA.Create(2048); var request = new CertificateRequest("CN=Private untrusted control", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        Check(!actual.Contains(Hash(generated.RawData)), "Locally generated certificates are not implicitly trusted or exported.");
        if (liveHost is not null)
        {
            TLS(trust, liveHost, liveHost, null, output, "snapshot");
            TLS(null, liveHost, liveHost, null, output, "system");
            using var wrong = new Certificate(); wrong.LoadFromString(generated.ExportCertificatePem());
            TLS(wrong, liveHost, liveHost, TLSStatus.Error, output, "wrong-trust");
            LocalNameFailure(wrong, generated, output);
        }
        Console.WriteLine($"System CA: {_checks} public checks; {actual.Length} native roots; {(liveHost is null ? 0 : 4)} live TLS controls.");
    }
    private static void LocalNameFailure(Certificate trust, X509Certificate2 certificate, string output)
    {
        X509Certificate2? compatible = null;
        if (OperatingSystem.IsWindows())
        {
            // Schannel needs a temporary provider key rather than the constructor's ephemeral CNG handle.
            var encoded = certificate.Export(X509ContentType.Pfx);
            try { compatible = X509CertificateLoader.LoadPkcs12(encoded, null, X509KeyStorageFlags.UserKeySet); }
            finally { CryptographicOperations.ZeroMemory(encoded); }
        }
        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start();
            var server = Task.Run(async () =>
            {
                using var tcp = await listener.AcceptTcpClientAsync(); using var tls = new SslStream(tcp.GetStream());
                try { await tls.AuthenticateAsServerAsync(compatible ?? certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false); }
                catch (Exception error) when (error is AuthenticationException or IOException) { return error.ToString(); }
                return "Server handshake completed.";
            });
            try { TLS(trust, "localhost", "missing-certificate-name.invalid", TLSStatus.ErrorHostnameMismatch, output, "wrong-name", ((IPEndPoint)listener.LocalEndpoint).Port); }
            finally { File.WriteAllText(IOPath.Combine(output, "local-server.txt"), server.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult()); }
        }
        finally { listener.Stop(); compatible?.Dispose(); }
    }
    private static void TLS(Certificate? trust, string host, string expectedName, TLSStatus? failure, string output, string label, int port = 443)
    {
        var address = Dns.GetHostAddresses(host).First(value => value.AddressFamily == AddressFamily.InterNetwork);
        using var tcp = new StreamPeerTCP(); tcp.ConnectToHost(address.ToString(), port);
        Until(() => { tcp.Poll(); return tcp.GetStatus() == StreamSocketStatus.Connected; });
        using var options = TLSOptions.Client(trust); using var tls = new StreamPeerTLS();
        try
        {
            tls.ConnectToStream(tcp, expectedName, options); Until(() => { tls.Poll(); return tls.GetStatus() == TLSStatus.Connected; });
            Check(failure is null, "An untrusted or wrong-name peer must not connect.");
            if (trust is not null)
            {
                try { trust.Dispose(); throw new Exception("An active TLS snapshot must retain its resource."); }
                catch (InvalidOperationException) { _checks++; }
            }
            tls.PutData(Encoding.ASCII.GetBytes($"GET / HTTP/1.1\r\nHost: {host}\r\nConnection: close\r\n\r\n"));
            Until(() => { tls.Poll(); return tls.GetAvailableBytes() > 0; });
            var bytes = new byte[1024]; var count = tls.GetPartialData(bytes); var response = Encoding.ASCII.GetString(bytes, 0, count);
            Check(response.StartsWith("HTTP/1.", StringComparison.Ordinal), "Authenticated native TLS carries a real HTTP response.");
            File.WriteAllText(IOPath.Combine(output, label + ".txt"), response);
        }
        catch (AuthenticationException error)
        {
            if (!failure.HasValue) throw;
            Check(tls.GetStatus() == failure.Value, $"TLS rejects with the expected chain/name status: expected {failure}; actual {tls.GetStatus()}; {error.Message}");
            File.WriteAllText(IOPath.Combine(output, label + ".txt"), tls.GetStatus() + ": " + error.Message);
        }
        finally { tls.DisconnectFromStream(); }
        Check(tcp.GetStatus() == StreamSocketStatus.Connected, "TLS retirement leaves its borrowed TCP transport live.");
    }
    private static void Until(Func<bool> ready)
    {
        var clock = Stopwatch.StartNew();
        while (!ready()) { if (clock.Elapsed.TotalSeconds > 15) throw new TimeoutException("Native network progress timed out."); Thread.Sleep(2); }
    }
    private static string[] Decode(string pem)
    {
        var collection = new X509Certificate2Collection();
        try { collection.ImportFromPem(pem); return collection.Select(value => Hash(value.RawData)).Order().ToArray(); }
        finally { foreach (var certificate in collection) certificate.Dispose(); }
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static void Check(bool value, string message) { _checks++; if (!value) throw new Exception(message); }
}
