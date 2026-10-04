using Electron2D;
using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Path = System.IO.Path;
using Certificate = Electron2D.X509Certificate;

internal static class HTTPTests
{
    internal static void Run() { Compression(); Scripted(); Native(); SceneRequests(); ProxiesAndTLS(); Console.WriteLine("HTTP/HTTPS wire framing, native/scene/worker requests, compression, proxies, redirects, limits and ownership checks passed."); }
    private static void Compression()
    {
        var raw = new byte[32768]; Random.Shared.NextBytes(raw);
        foreach (var deflate in new[] { false, true })
        {
            using var codec = new StreamPeerGZIP(); codec.StartCompression(deflate, 128); using var wire = new MemoryStream(); var offset = 0; var buffer = new byte[256];
            while (offset < raw.Length) { offset += codec.PutPartialData(raw.AsSpan(offset)); Drain(codec, wire, buffer); }
            while (!codec.IsFinished()) { try { codec.Finish(); } catch (InvalidOperationException) { } Drain(codec, wire, buffer); }
            Drain(codec, wire, buffer); var compressed = wire.ToArray(); using var input = new MemoryStream(compressed); using Stream oracle = deflate ? new ZLibStream(input, CompressionMode.Decompress) : new GZipStream(input, CompressionMode.Decompress); using var output = new MemoryStream(); oracle.CopyTo(output); Check(output.ToArray().SequenceEqual(raw), "Independent compression oracle.");
            codec.Clear(); codec.StartDecompression(deflate, 128); offset = 0; using var decoded = new MemoryStream(); while (offset < compressed.Length) { offset += codec.PutPartialData(compressed.AsSpan(offset, Math.Min(3, compressed.Length - offset))); Drain(codec, decoded, buffer); }
            Drain(codec, decoded, buffer); Check(codec.IsFinished() && decoded.ToArray().SequenceEqual(raw), "Fragmented codec, checksum and end marker.");
            Reject<InvalidOperationException>(codec.Finish); codec.Clear(); codec.StartDecompression(deflate); codec.PutData(compressed.AsSpan(0, compressed.Length - 1)); Check(!codec.IsFinished(), "Truncated compression is not completed.");
            Reject<InvalidOperationException>(() => Task.Run(codec.Clear).GetAwaiter().GetResult());
        }
        using var member = new MemoryStream(); using (var gzip = new GZipStream(member, CompressionLevel.Fastest, true)) gzip.Write("warm"u8); var fixture = member.ToArray(); using var reuse = new StreamPeerGZIP(); reuse.StartDecompression(); Span<byte> decodedBytes = stackalloc byte[4];
        for (var i = 0; i < 32; i++) { reuse.PutData(fixture); Check(reuse.GetPartialData(decodedBytes) == 4, "Codec warmup."); }
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { reuse.PutData(fixture); if (reuse.GetPartialData(decodedBytes) != 4 || !reuse.IsFinished()) throw new InvalidOperationException("Codec reuse."); }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocatedBefore, "Prepared gzip member decode/read/reset allocates zero managed bytes."); Console.WriteLine("64 gzip member/decode/span cycles: 0 managed bytes.");
        using var invalid = new StreamPeerGZIP(); Reject<ArgumentOutOfRangeException>(() => invalid.StartCompression(bufferSize: 0)); invalid.StartDecompression(); Reject<InvalidDataException>(() => invalid.PutData("garbage"u8));
    }
    private static void Drain(StreamPeerGZIP codec, MemoryStream output, byte[] buffer) { while (true) { var count = codec.GetPartialData(buffer); if (count == 0) break; output.Write(buffer, 0, count); } }
    private sealed class ScriptPeer : StreamPeer
    {
        internal byte[] Input = [];
        internal int Offset, Fragment = 3;
        internal bool EOF;
        internal readonly StreamPeerBuffer Sent = new();
        public override int GetAvailableBytes() { CheckStream(); return Input.Length - Offset; }
        protected override int ReadCore(Span<byte> destination, bool block) { var count = Math.Min(Math.Min(destination.Length, Fragment), Input.Length - Offset); if (count == 0 && EOF) throw new EndOfStreamException(); Input.AsSpan(Offset, count).CopyTo(destination); Offset += count; return count; }
        protected override int WriteCore(ReadOnlySpan<byte> data, bool block) { var count = Math.Min(Fragment, data.Length); Sent.PutData(data[..count]); return count; }
        protected override void Dispose(bool disposing) { if (disposing) Sent.Dispose(); base.Dispose(disposing); }
    }
    private static HTTPClient Script(string response, ScriptPeer stream)
    {
        stream.Input = System.Text.Encoding.ASCII.GetBytes(response); stream.Offset = 0; var client = new HTTPClient { Connection = stream }; client.Request(HTTPMethod.Get, "/resource", []); Wait(() => { client.Poll(); return client.HasResponse(); }); return client;
    }
    private static void Scripted()
    {
        using var stream = new ScriptPeer(); using var client = Script("HTTP/1.1 100 Continue\r\n\r\nHTTP/1.1 200 OK\r\nContent-Length: 5\r\nX-Test: one\r\nX-Test: two\r\n\r\nhello", stream);
        Check(client.GetStatus() == HTTPStatus.Body && client.GetResponseBodyLength() == 5 && client.GetResponseCode() == HTTPResponseCode.OK, "HTTP response state."); var headers = client.GetResponseHeadersAsDictionary(); Check(headers["X-Test"] == "two" && !client.HasResponse(), "Consumed typed duplicate header snapshot."); Check(ReadAll(client) == "hello" && client.GetStatus() == HTTPStatus.Connected, "Fragmented fixed body.");
        Check(System.Text.Encoding.UTF8.GetString(stream.Sent.DataArray).Contains("GET /resource HTTP/1.1\r\n") && !stream.IsDisposed, "Public HTTP wire and borrowed stream.");
        stream.Input = "HTTP/1.1 204 No Content\r\n\r\n"u8.ToArray(); stream.Offset = 0; client.Request(HTTPMethod.Head, "/", []); Wait(() => { client.Poll(); return client.HasResponse(); }); Check(client.GetStatus() == HTTPStatus.Connected && client.GetResponseBodyLength() == 0 && client.GetResponseHeaders().Length == 0, "Headerless/HEAD response is valid.");
        stream.Input = "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n3;extension=yes\r\nabc\r\n2\r\nde\r\n0\r\nX-Trailer: value\r\n\r\n"u8.ToArray(); stream.Offset = 0; client.Request(HTTPMethod.Get, "/", []); Wait(() => { client.Poll(); return client.HasResponse(); }); Check(client.IsResponseChunked() && ReadAll(client) == "abcde", "Chunk extensions/separators/trailers.");
        stream.Input = "HTTP/1.1 200 Tunnel\r\n\r\ntunnel"u8.ToArray(); stream.Offset = 0; client.Request(HTTPMethod.Connect, "host:443", []); Wait(() => { client.Poll(); return client.HasResponse(); }); Check(client.GetStatus() == HTTPStatus.Connected && client.GetResponseBodyLength() == 0 && System.Text.Encoding.ASCII.GetString(stream.GetData(6)) == "tunnel", "Successful CONNECT preserves protocol bytes.");
        client.Close(); Check(!stream.IsDisposed, "HTTP Close preserves borrowed stream.");
        foreach (var response in new[] { "HTTP/1.1 200 OK\r\nContent-Length: 1\r\nContent-Length: 2\r\n\r\n", "HTTP/1.1 200 OK\r\nContent-Length: -1\r\n\r\n", "HTTP/1.1 200 OK\r\nContent-Length: 1\r\nTransfer-Encoding: chunked\r\n\r\n" })
        { using var bad = new ScriptPeer { Input = System.Text.Encoding.ASCII.GetBytes(response), Fragment = 1024 }; using var reject = new HTTPClient { Connection = bad }; reject.Request(HTTPMethod.Get, "/", []); Reject<InvalidDataException>(reject.Poll); Check(reject.GetStatus() == HTTPStatus.ConnectionError, "Invalid framing marks transport error."); }
        using var until = new ScriptPeer { EOF = true }; using var eofClient = Script("HTTP/1.0 200 OK\r\n\r\nclose body", until); Check(ReadAll(eofClient) == "close body" && eofClient.GetStatus() == HTTPStatus.Disconnected, "Close-delimited body.");
        using var raw = new HTTPClient { Connection = stream }; Reject<ArgumentException>(() => raw.Request(HTTPMethod.Get, "/", ["X: ok\r\nInjected: bad"])); Reject<ArgumentException>(() => raw.Request(HTTPMethod.Get, "/has space", [])); Reject<ArgumentException>(() => raw.RequestRaw(HTTPMethod.Post, "/", ["Content-Length: 3"], new byte[2])); Reject<ArgumentOutOfRangeException>(() => raw.ReadChunkSize = 255);
        Check(HTTPClient.QueryStringFromDict(new Dictionary<string, string?> { ["a b"] = "é", ["flag"] = null }) == "a%20b=%C3%A9&flag", "Typed scalar query escaping."); Check(HTTPClient.QueryStringFromDict(new Dictionary<string, string?[]> { ["v"] = ["a", "b"] }) == "v=a&v=b", "Array query values repeat keys.");
        using var warm = new ScriptPeer { Fragment = 4096 }; var payload = new string('x', 65536); using var body = Script("HTTP/1.1 200 OK\r\nContent-Length: 65536\r\n\r\n" + payload, warm); Span<byte> bytes = stackalloc byte[256]; for (var i = 0; i < 32; i++) body.ReadResponseBodyChunk(bytes); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { body.Poll(); Check(body.ReadResponseBodyChunk(bytes) == bytes.Length, "Body span progress."); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "HTTP body span polling allocates zero managed bytes."); Console.WriteLine("64 HTTP body/span/poll cycles: 0 managed bytes.");
    }
    private static string ReadAll(HTTPClient client)
    {
        using var bytes = new MemoryStream(); var buffer = new byte[256]; Wait(() => { client.Poll(); var count = client.ReadResponseBodyChunk(buffer); bytes.Write(buffer, 0, count); return client.GetStatus() != HTTPStatus.Body; }); return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }
    private sealed class Server : IDisposable
    {
        internal readonly TcpListener Listener = new(IPAddress.Loopback, 0);
        private readonly Func<string, byte[], byte[]> _reply;
        private readonly X509Certificate2? _identity;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _task;
        internal int Port => ((IPEndPoint)Listener.LocalEndpoint).Port;
        internal Server(Func<string, byte[], byte[]> reply, X509Certificate2? identity = null) { _identity = identity; _reply = reply; Listener.Start(); _task = Task.Run(Loop); }
        private async Task Loop()
        {
            try { while (!_stop.IsCancellationRequested) { using var socket = await Listener.AcceptTcpClientAsync(_stop.Token); using var stream = socket.GetStream(); using Stream transport = _identity is null ? stream : new SslStream(stream, true); try { if (transport is SslStream ssl) await ssl.AuthenticateAsServerAsync(_identity!, false, SslProtocols.Tls12 | SslProtocols.Tls13, false); var (headers, body) = await ReadRequest(transport); var reply = _reply(headers, body); await transport.WriteAsync(reply, _stop.Token); } catch (IOException) { } catch (AuthenticationException) { } } }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }
        public void Dispose() { _stop.Cancel(); Listener.Stop(); _task.GetAwaiter().GetResult(); _stop.Dispose(); }
    }
    private static async Task<(string, byte[])> ReadRequest(Stream stream)
    {
        using var memory = new MemoryStream(); var one = new byte[1]; while (true) { await stream.ReadExactlyAsync(one); memory.WriteByte(one[0]); var data = memory.GetBuffer(); var length = (int)memory.Length; if (length >= 4 && data.AsSpan(length - 4, 4).SequenceEqual("\r\n\r\n"u8)) break; if (length > 65536) throw new InvalidDataException(); }
        var headers = System.Text.Encoding.ASCII.GetString(memory.ToArray()); var line = headers.Split("\r\n").FirstOrDefault(h => h.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)); var lengthBody = line is null ? 0 : int.Parse(line[(line.IndexOf(':') + 1)..]); var body = new byte[lengthBody]; if (lengthBody != 0) await stream.ReadExactlyAsync(body); return (headers, body);
    }
    private static byte[] Reply(byte[] body, string extras = "", int code = 200) => [.. System.Text.Encoding.ASCII.GetBytes($"HTTP/1.1 {code} Response\r\nContent-Length: {body.Length}\r\nConnection: close\r\n{extras}\r\n"), .. body];
    private static void Native()
    {
        using var server = new Server((headers, body) => Reply(body.Length == 0 ? "native"u8.ToArray() : body)); using var client = new HTTPClient(); client.ConnectToHost("localhost", server.Port); try { Wait(() => { client.Poll(); return client.GetStatus() == HTTPStatus.Connected; }); } catch { Console.WriteLine("Native connect phase: " + client.GetStatus()); throw; }
        client.RequestRaw(HTTPMethod.Post, "/echo", [], "echo"u8); try { Wait(() => { client.Poll(); return client.HasResponse(); }); } catch { Console.WriteLine("Native request phase: " + client.GetStatus()); throw; }
        Check(ReadAll(client) == "echo", "Native DNS fallback/TCP HTTP exchange.");
    }
    private static void SceneRequests()
    {
        var bytes = new byte[32768]; Array.Fill(bytes, (byte)42); using var compressed = new MemoryStream(); using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, true)) gzip.Write(bytes); var zipped = compressed.ToArray();
        using var server = new Server((headers, body) => headers.StartsWith("GET /redirect ") ? Reply([], "Location: /gzip\r\n", 302) : headers.StartsWith("GET /truncate ") ? Reply(zipped[..^2], "Content-Encoding: gzip\r\n") : Reply(zipped, "Content-Encoding: gzip\r\n"));
        foreach (var worker in new[] { false, true })
        {
            var root = new Node(); var request = new HTTPRequest { UseThreads = worker, DownloadChunkSize = 256 }; root.AddChild(request); using var tree = new SceneTree(root); byte[]? response = null; HTTPRequestResult result = default; var completions = 0; var owner = Environment.CurrentManagedThreadId;
            request.RequestCompleted += (r, code, h, data) => { Check(Environment.CurrentManagedThreadId == owner && request.GetHTTPClientStatus() == HTTPStatus.Disconnected, "Completion is owner-thread and idle."); result = r; response = data; completions++; };
            request.Request("http://localhost:" + server.Port + "/redirect"); Wait(() => { tree.ProcessFrame(.001); return response is not null; }); Check(result == HTTPRequestResult.Success && response!.SequenceEqual(bytes) && completions == 1, "Scene/worker redirect plus streaming gzip.");
            response = null; request.BodySizeLimit = 1024; request.Request("http://localhost:" + server.Port + "/gzip"); Wait(() => { tree.ProcessFrame(.001); return response is not null; }); Check(result == HTTPRequestResult.BodySizeLimitExceeded, "Decoded zip-bomb limit.");
            response = null; request.BodySizeLimit = -1; request.Request("http://localhost:" + server.Port + "/truncate"); Wait(() => { tree.ProcessFrame(.001); return response is not null; }); Check(result == HTTPRequestResult.BodyDecompressFailed, "Truncated gzip cannot complete successfully.");
            var folder = Path.Combine(Path.GetTempPath(), "e2d-http-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); var path = Path.Combine(folder, "download.bin");
            try { File.WriteAllText(path, "sentinel"); response = null; request.DownloadFile = path; request.Request("http://localhost:" + server.Port + "/truncate"); Wait(() => { tree.ProcessFrame(.001); return response is not null; }); Check(result == HTTPRequestResult.BodyDecompressFailed && File.ReadAllText(path) == "sentinel", "Failed download preserves destination."); response = null; request.Request("http://localhost:" + server.Port + "/gzip"); Wait(() => { tree.ProcessFrame(.001); return response is not null; }); Check(result == HTTPRequestResult.Success && response!.Length == 0 && File.ReadAllBytes(path).SequenceEqual(bytes) && Directory.GetFiles(folder).Length == 1, "Decoded atomic file download."); }
            finally { Directory.Delete(folder, true); }
            request.DownloadFile = ""; request.Timeout = .001; response = null; request.Request("http://localhost:" + server.Port + "/gzip"); tree.ProcessFrame(.002); Check(result == HTTPRequestResult.Timeout && response is not null, "Scene timeout releases operation.");
            response = null; request.Request("http://localhost:" + server.Port + "/gzip"); request.CancelRequest(); tree.ProcessFrame(.1); Check(response is null, "Cancellation suppresses completion.");
        }
        using var plain = new Server((headers, body) => headers.StartsWith("GET /loop ") ? Reply([], "Location: /loop\r\n", 302) : Reply("plain"u8.ToArray()));
        var reentryRoot = new Node(); var reentry = new HTTPRequest { MaxRedirects = 0 }; reentryRoot.AddChild(reentry); using (var tree = new SceneTree(reentryRoot))
        {
            var count = 0; reentry.RequestCompleted += (result, code, headers, body) => { count++; if (count == 1) { Check(result == HTTPRequestResult.RedirectLimitReached, "Redirect limit."); reentry.Request("http://localhost:" + plain.Port + "/plain"); } else Check(result == HTTPRequestResult.Success, "Completion can reenter a fresh request."); };
            reentry.Request("http://localhost:" + plain.Port + "/loop"); Wait(() => { tree.ProcessFrame(.001); return count == 2; });
            using var packed = new PackedScene(); packed.Pack(reentry); using var copy = (HTTPRequest)packed.Instantiate(); Check(copy.MaxRedirects == 0 && copy.AcceptGZIP && copy.GetHTTPClientStatus() == HTTPStatus.Disconnected, "Request node stores configuration without operations.");
            reentry.Request("http://localhost:" + plain.Port + "/plain"); reentryRoot.RemoveChild(reentry); tree.ProcessFrame(.1); Check(count == 2 && reentry.GetHTTPClientStatus() == HTTPStatus.Disconnected, "Tree exit cancels without stale completion."); reentry.Dispose();
        }
        using var detached = new HTTPRequest(); Reject<InvalidOperationException>(() => detached.Request("http://localhost"));
    }
    private static void ProxiesAndTLS()
    {
        using var forward = new Server((headers, body) => { Check(headers.StartsWith("GET http://destination.invalid/proxy HTTP/1.1"), "Forward proxy uses absolute request target."); return Reply("proxy"u8.ToArray()); });
        using (var client = new HTTPClient()) { client.SetHTTPProxy("127.0.0.1", forward.Port); client.ConnectToHost("destination.invalid"); Wait(() => { client.Poll(); return client.GetStatus() == HTTPStatus.Connected; }); client.Request(HTTPMethod.Get, "/proxy", []); Wait(() => { client.Poll(); return client.HasResponse(); }); Check(ReadAll(client) == "proxy", "Native forward proxy avoids target DNS."); }
        using var rsa = RSA.Create(2048); var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1); var names = new SubjectAlternativeNameBuilder(); names.AddDnsName("localhost"); names.AddIpAddress(IPAddress.Loopback); request.CertificateExtensions.Add(names.Build()); using var identity = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
        using var trust = new Certificate(); trust.LoadFromString(identity.ExportCertificatePem()); using var options = TLSOptions.Client(trust);
        using var secure = new Server((headers, body) => Reply("secure"u8.ToArray()), identity);
        using (var client = new HTTPClient()) { client.ConnectToHost("https://localhost", secure.Port, options); Wait(() => { client.Poll(); return client.GetStatus() == HTTPStatus.Connected; }); client.Request(HTTPMethod.Get, "/", []); Wait(() => { client.Poll(); return client.HasResponse(); }); Check(ReadAll(client) == "secure", "Independent SslStream HTTPS server."); }
        using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var tunnelPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        var tunnel = Task.Run(async () => { using var downstream = await listener.AcceptTcpClientAsync(); using var downstreamStream = downstream.GetStream(); var (head, body) = await ReadRequest(downstreamStream); Check(head.StartsWith("CONNECT localhost:" + secure.Port + " HTTP/1.1"), "HTTPS proxy CONNECT authority."); using var upstream = new TcpClient(); await upstream.ConnectAsync(IPAddress.Loopback, secure.Port); using var upstreamStream = upstream.GetStream(); await downstreamStream.WriteAsync("HTTP/1.1 200 Established\r\n\r\n"u8.ToArray()); await Task.WhenAll(downstreamStream.CopyToAsync(upstreamStream), upstreamStream.CopyToAsync(downstreamStream)); });
        using (var client = new HTTPClient()) { client.SetHTTPSProxy("127.0.0.1", tunnelPort); client.ConnectToHost("localhost", secure.Port, options); Wait(() => { client.Poll(); return client.GetStatus() == HTTPStatus.Connected; }); client.Request(HTTPMethod.Get, "/through-tunnel", []); Wait(() => { client.Poll(); return client.HasResponse(); }); Check(ReadAll(client) == "secure", "CONNECT followed by verified target TLS and HTTP."); }
        Wait(() => tunnel.IsCompleted); tunnel.GetAwaiter().GetResult();
        var root = new Node(); var node = new HTTPRequest { UseThreads = true }; root.AddChild(node); using var tree = new SceneTree(root); byte[]? answer = null; HTTPRequestResult result = default; node.SetTLSOptions(options); node.RequestCompleted += (r, code, headers, data) => { answer = data; result = r; }; node.Request("https://localhost:" + secure.Port + "/node"); Wait(() => { tree.ProcessFrame(.001); return answer is not null; }); Check(result == HTTPRequestResult.Success && System.Text.Encoding.UTF8.GetString(answer!) == "secure", "Scene worker HTTPS integration.");
    }
    private static void Wait(Func<bool> condition) { var start = Environment.TickCount64; while (!condition()) { if (Environment.TickCount64 - start > 10000) throw new TimeoutException("HTTP test deadline."); Thread.Yield(); } }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
