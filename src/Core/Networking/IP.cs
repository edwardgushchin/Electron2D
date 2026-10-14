using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Globalization;

namespace Electron2D;

/// <summary>Provides process-wide hostname resolution, bounded queued lookups and network-interface snapshots.</summary>
/// <remarks>Static operations address one retained object registered with Engine. Calls are thread safe; native DNS
/// and interface enumeration are cold host work. Queue IDs remain occupied after completion until explicitly erased.
/// Address snapshots belong to the caller. The process-wide object cannot be disposed.</remarks>
public sealed partial class IP : ElectronObject
{
    /// <summary>Identifies failure to reserve a resolver queue slot.</summary>
    public const int ResolverInvalidID = -1;
    /// <summary>Maximum simultaneous retained queue items, including completed items.</summary>
    public const int ResolverMaxQueries = 256;
    private static readonly IP SharedInstance = new(NativeResolve);
    private readonly object _gate = new();
    private readonly Dictionary<(string Host, IPType Type), string[]> _cache = [];
    private readonly ResolveItem[] _items = new ResolveItem[ResolverMaxQueries];
    private readonly Func<string, IPType, CancellationToken, Task<IPAddress[]>> _backend;
    private ulong _cacheRevision;
    private sealed class ResolveItem
    {
        internal ulong Generation;
        internal ResolverStatus Status;
        internal string[] Addresses = [];
        internal CancellationTokenSource? Cancellation;
        internal Task? Work;
    }
    private IP(Func<string, IPType, CancellationToken, Task<IPAddress[]>> backend)
    { ArgumentNullException.ThrowIfNull(backend); _backend = backend; for (var i = 0; i < _items.Length; i++) _items[i] = new(); }
    internal static IP Service => SharedInstance;
    internal static IP CreateIsolatedResolver(Func<string, IPType, CancellationToken, Task<IPAddress[]>> backend) => new(backend);
    private static void Validate(string host, IPType type)
    { ArgumentException.ThrowIfNullOrEmpty(host); if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type)); }
    private static AddressFamily Family(IPType type) => type switch { IPType.IPv4 => AddressFamily.InterNetwork, IPType.IPv6 => AddressFamily.InterNetworkV6, _ => AddressFamily.Unspecified };
    private static Task<IPAddress[]> NativeResolve(string host, IPType type, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (IPAddress.TryParse(host, out var address)) return Task.FromResult(new[] { address });
        if (OperatingSystem.IsBrowser()) throw new PlatformNotSupportedException("Native hostname resolution is unavailable on this host.");
        return Dns.GetHostAddressesAsync(host, Family(type), token);
    }
    private static string[] Normalize(IPAddress[] addresses, IPType type)
    {
        var family = Family(type); var result = new List<string>(addresses.Length);
        foreach (var address in addresses)
        {
            if (address.AddressFamily is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6) || family != AddressFamily.Unspecified && address.AddressFamily != family) continue;
            var text = address.ToString(); if (!result.Contains(text, StringComparer.Ordinal)) result.Add(text);
        }
        return [.. result];
    }
    private async Task<string[]> Lookup(string host, IPType type, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        ulong revision; lock (_gate) { ThrowIfDisposed(); if (_cache.TryGetValue((host, type), out var cached)) return cached; revision = _cacheRevision; }
        var result = Normalize(await _backend(host, type, token).ConfigureAwait(false), type);
        lock (_gate) { if (!IsDisposed && revision == _cacheRevision && result.Length != 0) _cache[(host, type)] = result; }
        return result;
    }
    internal string[] ResolveHostnameAddressesCore(string host, IPType type)
    {
        Validate(host, type); try { return (string[])Lookup(host, type, CancellationToken.None).GetAwaiter().GetResult().Clone(); }
        catch (SocketException) { return []; }
    }
    internal string ResolveHostnameCore(string host, IPType type)
    {
        Validate(host, type); lock (_gate) { ThrowIfDisposed(); if (_cache.TryGetValue((host, type), out var cached)) return cached[0]; }
        try { var values = Lookup(host, type, CancellationToken.None).GetAwaiter().GetResult(); return values.Length == 0 ? "" : values[0]; }
        catch (SocketException) { return ""; }
    }
    internal async Task<IPAddress[]> ResolveTransportAddresses(string host, CancellationToken token)
    {
        Validate(host, IPType.Any); var values = await Lookup(host, IPType.Any, token).ConfigureAwait(false);
        if (values.Length == 0) throw new SocketException((int)SocketError.HostNotFound);
        var result = new IPAddress[values.Length]; for (var i = 0; i < values.Length; i++) result[i] = IPAddress.Parse(values[i]); return result;
    }
    internal int ResolveHostnameQueueItemCore(string host, IPType type)
    {
        Validate(host, type); ResolveItem item; ulong generation, cacheRevision; CancellationTokenSource cancel; int id;
        lock (_gate)
        {
            ThrowIfDisposed(); for (id = 0; id < _items.Length && _items[id].Status != ResolverStatus.None; id++) { }
            if (id == _items.Length) return ResolverInvalidID;
            item = _items[id]; generation = ++item.Generation; item.Addresses = []; item.Work = null; cacheRevision = _cacheRevision;
            if (_cache.TryGetValue((host, type), out var cached)) { item.Addresses = cached; item.Status = ResolverStatus.Done; return id; }
            item.Status = ResolverStatus.Waiting; item.Cancellation = cancel = new();
        }
        var work = CompleteLookup(item, generation, cacheRevision, host, type, cancel);
        lock (_gate) { if (item.Generation == generation) item.Work = work; }
        return id;
    }
    private async Task CompleteLookup(ResolveItem item, ulong generation, ulong cacheRevision, string host, IPType type, CancellationTokenSource cancel)
    {
        string[] result = []; try { result = Normalize(await _backend(host, type, cancel.Token).ConfigureAwait(false), type); }
        catch (Exception) { }
        finally
        {
            lock (_gate)
            {
                if (!IsDisposed && item.Generation == generation && item.Status == ResolverStatus.Waiting)
                {
                    item.Addresses = result; item.Status = result.Length == 0 ? ResolverStatus.Error : ResolverStatus.Done; item.Cancellation = null;
                    if (result.Length != 0 && cacheRevision == _cacheRevision) _cache[(host, type)] = result;
                }
            }
            cancel.Dispose();
        }
    }
    private ResolveItem Item(int id)
    { ThrowIfDisposed(); if ((uint)id >= ResolverMaxQueries) throw new ArgumentOutOfRangeException(nameof(id)); return _items[id]; }
    internal Task GetResolveItemCompletionCore(int id) { lock (_gate) return Item(id).Work ?? Task.CompletedTask; }
    internal ResolverStatus GetResolveItemStatusCore(int id) { lock (_gate) return Item(id).Status; }
    internal string GetResolveItemAddressCore(int id) { lock (_gate) { var item = Item(id); return item.Status == ResolverStatus.Done ? item.Addresses[0] : ""; } }
    internal string[] GetResolveItemAddressesCore(int id) { lock (_gate) { var item = Item(id); return item.Status == ResolverStatus.Done ? (string[])item.Addresses.Clone() : []; } }
    internal void EraseResolveItemCore(int id)
    {
        CancellationTokenSource? cancel; lock (_gate) { var item = Item(id); item.Generation++; item.Status = ResolverStatus.None; item.Addresses = []; item.Work = null; cancel = item.Cancellation; item.Cancellation = null; }
        Cancel(cancel);
    }
    private static void Cancel(CancellationTokenSource? cancel)
    { try { cancel?.Cancel(); } catch (ObjectDisposedException) { } }
    internal void ClearCacheCore(string host)
    {
        ArgumentNullException.ThrowIfNull(host); lock (_gate)
        {
            ThrowIfDisposed(); _cacheRevision++;
            if (host.Length == 0) _cache.Clear(); else for (var type = IPType.None; type <= IPType.Any; type++) _cache.Remove((host, type));
        }
    }
    internal IPInterfaceInfo[] GetLocalInterfacesCore()
    {
        ThrowIfDisposed();
        if (OperatingSystem.IsBrowser()) throw new PlatformNotSupportedException("Native network interfaces are unavailable on this host.");
        var adapters = NetworkInterface.GetAllNetworkInterfaces(); var result = new IPInterfaceInfo[adapters.Length];
        for (var i = 0; i < adapters.Length; i++)
        {
            var adapter = adapters[i]; var properties = adapter.GetIPProperties();
            var index = adapter.Supports(NetworkInterfaceComponent.IPv4) ? properties.GetIPv4Properties().Index : adapter.Supports(NetworkInterfaceComponent.IPv6) ? properties.GetIPv6Properties().Index : 0;
            var addresses = properties.UnicastAddresses.Where(a => a.Address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6).Select(a => a.Address.ToString()).ToArray();
            result[i] = new(adapter.Id, adapter.Name, index.ToString(CultureInfo.InvariantCulture), addresses);
        }
        return result;
    }
    /// <inheritdoc />
    /// <remarks>The retained process-wide service has process lifetime.</remarks>
    protected override void ValidateDisposal()
    { if (ReferenceEquals(this, SharedInstance)) throw new InvalidOperationException("The process-wide IP service cannot be disposed."); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        var cancellations = new List<CancellationTokenSource>(); lock (_gate) { _cache.Clear(); foreach (var item in _items) { item.Generation++; item.Status = ResolverStatus.None; item.Addresses = []; if (item.Cancellation is { } cancel) cancellations.Add(cancel); item.Cancellation = null; } }
        try { foreach (var cancel in cancellations) Cancel(cancel); } finally { base.Dispose(disposing); }
    }
}
