using Electron2D;
using System.Net;
using System.Net.Sockets;

internal static class IPTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static TaskCompletionSource<IPAddress[]> Pending() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private sealed class SceneResolverClient : Node
    {
        private readonly TCPServer _server = new(); private readonly StreamPeerTCP _client = new(); private StreamPeerTCP? _accepted; private int _query = -1; private bool _sent;
        internal bool Complete;
        internal SceneResolverClient() { ProcessEnabled = true; }
        protected override void OnReady() { _server.Listen(0); _query = IP.ResolveHostnameQueueItem("localhost"); }
        protected override void OnProcess(double delta)
        {
            if (Complete) return;
            if (_query >= 0) { var status = IP.GetResolveItemStatus(_query); if (status == ResolverStatus.Waiting) return; Check(status == ResolverStatus.Done && IP.GetResolveItemAddress(_query).Length != 0, "Scene resolver completes."); _client.ConnectToHost("localhost", _server.GetLocalPort()); IP.EraseResolveItem(_query); _query = -1; }
            _client.Poll(); if (_accepted is null && _server.IsConnectionAvailable()) _accepted = _server.TakeConnection();
            if (!_sent && _client.GetStatus() == StreamSocketStatus.Connected) { _client.PutU32(0x12345678); _sent = true; }
            if (_accepted is not null) { _accepted.Poll(); if (_accepted.GetAvailableBytes() >= 4) { Check(_accepted.GetU32() == 0x12345678, "Resolved native TCP wire."); Complete = true; } }
        }
        protected override void Dispose(bool disposing)
        { try { if (_query >= 0) IP.EraseResolveItem(_query); _accepted?.Dispose(); _client.Dispose(); _server.Dispose(); } finally { base.Dispose(disposing); } }
    }
    internal static void Run()
    {
        var calls = 0;
        using (var resolver = IP.CreateIsolatedResolver((host, type, token) => { calls++; return Task.FromResult(new[] { IPAddress.Loopback, IPAddress.IPv6Loopback, IPAddress.Loopback }); }))
        {
            Check(resolver.ResolveHostnameAddressesCore("host", IPType.Any).SequenceEqual(new[] { "127.0.0.1", "::1" }) && calls == 1, "Distinct host ordering.");
            var copy = resolver.ResolveHostnameAddressesCore("host", IPType.Any); copy[0] = "changed"; Check(resolver.ResolveHostnameCore("host", IPType.Any) == "127.0.0.1" && calls == 1, "Cache snapshots cannot mutate retained addresses.");
            Check(resolver.ResolveHostnameAddressesCore("host", IPType.IPv6).SequenceEqual(new[] { "::1" }) && calls == 2, "Family-specific cache."); resolver.ClearCacheCore("other"); resolver.ResolveHostnameCore("host", IPType.Any); Check(calls == 2, "Unrelated cache clear preserves existing entry."); resolver.ClearCacheCore("host"); resolver.ResolveHostnameCore("host", IPType.Any); Check(calls == 3, "All family entries cleared.");
            var ids = new int[IP.ResolverMaxQueries]; for (var i = 0; i < ids.Length; i++) { ids[i] = resolver.ResolveHostnameQueueItemCore("host", IPType.Any); Check(ids[i] == i && resolver.GetResolveItemStatusCore(i) == ResolverStatus.Done, "Cached queue fills smallest available ID."); }
            Check(resolver.ResolveHostnameQueueItemCore("host", IPType.Any) == IP.ResolverInvalidID, "Completed slots still consume capacity."); resolver.EraseResolveItemCore(17); Check(resolver.GetResolveItemStatusCore(17) == ResolverStatus.None && resolver.GetResolveItemAddressCore(17) == "", "Erased state."); Check(resolver.ResolveHostnameQueueItemCore("host", IPType.Any) == 17, "ID reuse.");
            for (var i = 0; i < 64; i++) Cycle();
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Cycle(); Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "128 prepared cache/queue/status/string/erase cycles allocate zero owner bytes.");
            void Cycle() { resolver.EraseResolveItemCore(17); var id = resolver.ResolveHostnameQueueItemCore("host", IPType.Any); Check(resolver.GetResolveItemStatusCore(id) == ResolverStatus.Done && resolver.GetResolveItemAddressCore(id) == "127.0.0.1" && resolver.ResolveHostnameCore("host", IPType.Any) == "127.0.0.1", "Prepared cache cycle."); }
            Reject<ArgumentOutOfRangeException>(() => resolver.GetResolveItemStatusCore(-1)); Reject<ArgumentOutOfRangeException>(() => resolver.EraseResolveItemCore(256)); Reject<ArgumentOutOfRangeException>(() => resolver.ResolveHostnameCore("host", (IPType)99));
        }
        var old = Pending(); var newer = Pending();
        using (var resolver = IP.CreateIsolatedResolver((host, type, token) => host == "old" ? old.Task : newer.Task))
        {
            var id = resolver.ResolveHostnameQueueItemCore("old", IPType.Any); var completion = resolver.GetResolveItemCompletionCore(id);
            Check(resolver.GetResolveItemStatusCore(id) == ResolverStatus.Waiting && resolver.GetResolveItemAddressesCore(id).Length == 0, "Waiting is observable without blocking."); resolver.EraseResolveItemCore(id);
            Check(resolver.ResolveHostnameQueueItemCore("new", IPType.Any) == id, "Pending ID reuses safely."); newer.SetResult(new[] { IPAddress.Parse("192.0.2.2") }); resolver.GetResolveItemCompletionCore(id).GetAwaiter().GetResult(); old.SetResult(new[] { IPAddress.Parse("192.0.2.1") }); completion.GetAwaiter().GetResult();
            Check(resolver.GetResolveItemAddressCore(id) == "192.0.2.2", "Late completion cannot publish into reused slot.");
        }
        var pending = Pending(); var lookups = 0;
        using (var resolver = IP.CreateIsolatedResolver((host, type, token) => { lookups++; return lookups == 1 ? pending.Task : Task.FromResult(new[] { IPAddress.Loopback }); }))
        {
            var id = resolver.ResolveHostnameQueueItemCore("cleared", IPType.Any); resolver.ClearCacheCore(""); pending.SetResult(new[] { IPAddress.IPv6Loopback }); resolver.GetResolveItemCompletionCore(id).GetAwaiter().GetResult(); Check(resolver.GetResolveItemAddressCore(id) == "::1", "Cache clearing preserves pending queue result.");
            Check(resolver.ResolveHostnameCore("cleared", IPType.Any) == "127.0.0.1" && lookups == 2, "Older completion cannot repopulate cleared cache.");
        }
        using (var resolver = IP.CreateIsolatedResolver((host, type, token) => Task.FromException<IPAddress[]>(new SocketException((int)SocketError.HostNotFound))))
        { Check(resolver.ResolveHostnameCore("missing", IPType.Any) == "", "Blocking lookup failure is empty."); var id = resolver.ResolveHostnameQueueItemCore("missing", IPType.Any); resolver.GetResolveItemCompletionCore(id).GetAwaiter().GetResult(); Check(resolver.GetResolveItemStatusCore(id) == ResolverStatus.Error && resolver.GetResolveItemAddressCore(id) == "", "Queued failure is Error."); }
        Check(Engine.HasSingleton("IP") && ReferenceEquals(Engine.GetSingleton("IP"), IP.Service), "Retained service registration."); Reject<InvalidOperationException>(() => Engine.GetSingleton("IP").Dispose()); Reject<InvalidOperationException>(() => Engine.UnregisterSingleton("IP"));
        IP.ClearCache(); Check(IP.ResolveHostname("127.0.0.1", IPType.IPv4) == "127.0.0.1" && IP.ResolveHostname("::1", IPType.IPv6) == "::1", "Native literals."); Check(IP.ResolveHostname("127.0.0.1", IPType.IPv6) == "", "Literal family mismatch.");
        Check(IP.ResolveHostnameAddresses("localhost", IPType.IPv4).Any(a => IPAddress.IsLoopback(IPAddress.Parse(a))), "Native localhost IPv4 resolution."); var queue = IP.ResolveHostnameQueueItem("localhost", IPType.IPv6); IP.Service.GetResolveItemCompletionCore(queue).GetAwaiter().GetResult(); Check(IP.GetResolveItemStatus(queue) == ResolverStatus.Done && IP.GetResolveItemAddresses(queue).All(a => IPAddress.Parse(a).AddressFamily == AddressFamily.InterNetworkV6), "Native queued IPv6 DNS."); IP.EraseResolveItem(queue);
        var interfaces = IP.GetLocalInterfaces(); Check(interfaces.Length > 0 && interfaces.Any(i => i.Addresses.Any(a => IPAddress.IsLoopback(IPAddress.Parse(a)))) && interfaces.All(i => int.TryParse(i.Index, out _)), "Actual host interfaces and typed indexes."); Check(IP.GetLocalAddresses().All(a => IPAddress.TryParse(a, out _)), "Native address snapshot.");
        Task.Run(() => Check(IP.ResolveHostname("127.0.0.1") == "127.0.0.1", "Static service is thread safe.")).GetAwaiter().GetResult();
        IP.ClearCache(); using (var client = new SceneResolverClient()) using (var tree = new SceneTree(client))
        { var deadline = System.Diagnostics.Stopwatch.StartNew(); while (!client.Complete && deadline.Elapsed < TimeSpan.FromSeconds(10)) { tree.ProcessFrame(.01); Thread.Sleep(1); } Check(client.Complete, "Public scene queue/cache/TCP request executes."); }
        IP.ClearCache(); Console.WriteLine("IP resolver: cache/family/snapshots, 256 slots, late completion/clear races, error/cancellation/lifetime, native IPv4/IPv6/interfaces and 128 warm cache/queue cycles=0 owner B passed.");
    }
}
