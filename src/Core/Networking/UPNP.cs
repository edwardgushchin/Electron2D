using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
namespace Electron2D;

/// <summary>Discovers local Internet Gateway Devices and delegates synchronous control commands.</summary>
/// <remarks>Calls/disposal require the constructing thread. Discovery clears membership after parameter
/// validation and borrows returned devices; clearing/disposal does not dispose them. Network operations
/// allocate and block explicitly; use a dedicated caller thread when gameplay cannot wait.</remarks>
public class UPNP : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private readonly List<UPNPDevice> _devices = [];
    private string _interface = "";
    private int _port;
    private bool _ipv6;
    /// <summary>Creates an empty discovery/control collection.</summary>
    public UPNP() { }
    private void Check() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("UPNP access requires its constructing thread."); }
    /// <summary>Gets or sets the discovery interface.</summary><value>Empty selects the system default; a native interface name/ID or IP literal selects an interface.</value>
    public string DiscoverMulticastIF { get { Check(); return _interface; } set { Check(); ArgumentNullException.ThrowIfNull(value); _interface = value; } }
    /// <summary>Gets or sets the discovery source port.</summary><value>Zero selects an ephemeral port; one selects 1900; otherwise zero through 65535.</value>
    public int DiscoverLocalPort { get { Check(); return _port; } set { Check(); NetworkSockets.Port(value); _port = value; } }
    /// <summary>Gets or sets whether discovery uses IPv6.</summary><value>False initially. IPv6 sends site-local and link-local SSDP on the selected native interface.</value>
    public bool DiscoverIPV6 { get { Check(); return _ipv6; } set { Check(); _ipv6 = value; } }
    /// <summary>Returns the ordered membership count.</summary><returns>Device count, including explicitly added entries.</returns>
    public int GetDeviceCount() { Check(); return _devices.Count; }
    /// <summary>Returns a borrowed device at an index.</summary><param name="index">Zero-based existing index.</param><returns>The live device.</returns><exception cref="ArgumentOutOfRangeException">Index is absent.</exception>
    public UPNPDevice GetDevice(int index) { Check(); var result = _devices[index]; result.CheckDevice(); return result; }
    /// <summary>Appends a borrowed live device.</summary><param name="device">Device belonging to the same owner thread; duplicates are permitted.</param>
    public void AddDevice(UPNPDevice device) { Check(); ArgumentNullException.ThrowIfNull(device); device.CheckDevice(); _devices.Add(device); }
    /// <summary>Replaces one membership without disposing either device.</summary><param name="index">Existing zero-based index.</param><param name="device">Borrowed live replacement on this thread.</param>
    public void SetDevice(int index, UPNPDevice device) { Check(); ArgumentNullException.ThrowIfNull(device); device.CheckDevice(); _devices[index] = device; }
    /// <summary>Removes one membership without disposing the device.</summary><param name="index">Existing zero-based index.</param>
    public void RemoveDevice(int index) { Check(); _devices.RemoveAt(index); }
    /// <summary>Clears memberships while preserving caller-held device lifetimes.</summary>
    public void ClearDevices() { Check(); _devices.Clear(); }
    /// <summary>Finds the first live device assessed as a valid gateway.</summary><returns>A borrowed device or null; performs no network operation.</returns>
    public UPNPDevice? GetGateway() { Check(); foreach (var device in _devices) if (!device.IsDisposed && device.IsValidGateway()) return device; return null; }
    /// <summary>Queries the first valid gateway synchronously.</summary><returns>A normalized external address or empty on failure/no gateway.</returns>
    public string QueryExternalAddress() { Check(); return GetGateway()?.QueryExternalAddress() ?? ""; }
    /// <summary>Requests a port mapping through the first valid gateway.</summary><param name="port">External port.</param><param name="portInternal">Zero uses external port.</param><param name="description">Label; empty selects Electron2D.</param><param name="protocol">UDP or TCP.</param><param name="duration">Nonnegative seconds, zero permanent.</param><returns>NoGateway or the device's typed outcome.</returns>
    public UPNPResult AddPortMapping(int port, int portInternal = 0, string description = "", string protocol = "UDP", int duration = 0) { Check(); return GetGateway()?.AddPortMapping(port, portInternal, description, protocol, duration) ?? UPNPResult.NoGateway; }
    /// <summary>Deletes a mapping through the first valid gateway.</summary><param name="port">External port.</param><param name="protocol">UDP or TCP.</param><returns>NoGateway or the device's typed outcome.</returns>
    public UPNPResult DeletePortMapping(int port, string protocol = "UDP") { Check(); return GetGateway()?.DeletePortMapping(port, protocol) ?? UPNPResult.NoGateway; }
    /// <summary>Replaces memberships by synchronous SSDP discovery and gateway assessment.</summary><param name="timeout">Nonnegative response wait in milliseconds, 2000 initially.</param><param name="ttl">Multicast TTL/hops, zero through 255.</param><param name="deviceFilter">Case-sensitive search-target substring; empty retains all discovered targets.</param><returns>Discovery result; Success may contain no matching devices or only invalid gateways.</returns><remarks>Invalid parameters preserve memberships. Description/status requests have independent five-second deadlines; the wait does not bound the whole operation. Native responses are deduplicated by location and target, up to 256 identities; each description/SOAP body is at most one MiB. Browser raw sockets are unavailable.</remarks>
    public UPNPResult Discover(int timeout = 2000, int ttl = 2, string deviceFilter = "InternetGatewayDevice")
    {
        Check(); if (timeout < 0 || ttl is < 0 or > 255 || deviceFilter is null) return UPNPResult.InvalidParameter; if (OperatingSystem.IsBrowser()) throw new PlatformNotSupportedException("UPNP discovery requires native sockets."); _devices.Clear();
        try
        {
            var family = _ipv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork; using var socket = NetworkSockets.Create(family, SocketType.Dgram); if (_ipv6) socket.DualMode = false;
            var (local, scope) = Interface(family); socket.Bind(new IPEndPoint(local, _port == 1 ? 1900 : _port));
            if (_ipv6) { socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, ttl); socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, scope); }
            else { socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, ttl); if (!local.Equals(IPAddress.Any)) socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, local.GetAddressBytes()); }
            var common = deviceFilter.Length == 0 || new[] { "InternetGatewayDevice", "WANIPConnection", "WANPPPConnection", "rootdevice" }.Any(deviceFilter.Contains);
            var targets = common ? new[] { "urn:schemas-upnp-org:device:InternetGatewayDevice:1", "urn:schemas-upnp-org:service:WANIPConnection:1", "urn:schemas-upnp-org:service:WANPPPConnection:1", "upnp:rootdevice" } : ["ssdp:all"];
            var sent = false; foreach (var address in _ipv6 ? new[] { new IPAddress(IPAddress.Parse("ff05::c").GetAddressBytes(), scope), new IPAddress(IPAddress.Parse("ff02::c").GetAddressBytes(), scope) } : [IPAddress.Parse("239.255.255.250")])
            {
                foreach (var target in targets)
                {
                    var host = _ipv6 ? "[" + address.ToString().Split('%')[0] + "]:1900" : "239.255.255.250:1900"; var request = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.1\r\nHOST: " + host + "\r\nMAN: \"ssdp:discover\"\r\nMX: " + Math.Clamp((timeout + 999L) / 1000, 1, 5) + "\r\nST: " + target + "\r\n\r\n");
                    try { socket.SendTo(request, new IPEndPoint(address, 1900)); sent = true; } catch (SocketException) when (_ipv6) { }
                }
            }
            if (!sent) return UPNPResult.SocketError; var deadline = Stopwatch.GetTimestamp() + (long)timeout * Stopwatch.Frequency / 1000; var identities = new HashSet<(string URL, string Type)>(); var descriptions = new List<(string URL, string Type)>(); var packet = new byte[65536]; EndPoint sender = new IPEndPoint(_ipv6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
            while (Stopwatch.GetTimestamp() <= deadline)
            {
                if (!socket.Poll(1000, SelectMode.SelectRead)) continue;
                try { var count = socket.ReceiveFrom(packet, ref sender); if (!SSDP(packet.AsSpan(0, count), out var url, out var type) || !identities.Add((url, type))) continue; if (identities.Count > 256) return UPNPResult.MemoryAllocationError; descriptions.Add((url, type)); }
                catch (SocketException error) when (NetworkSockets.Busy(error.SocketErrorCode)) { }
            }
            if (descriptions.Count == 0) return UPNPResult.NoDevices;
            foreach (var description in descriptions) if (deviceFilter.Length == 0 || description.Type.Contains(deviceFilter, StringComparison.Ordinal)) { var device = new UPNPDevice { DescriptionURL = description.URL, ServiceType = description.Type }; UPNPProtocol.Describe(device); _devices.Add(device); }
            return UPNPResult.Success;
        }
        catch (SocketException) { return UPNPResult.SocketError; }
        catch (NetworkInformationException) { return UPNPResult.SocketError; }
        catch (ArgumentException) { return UPNPResult.InvalidParameter; }
    }
    private (IPAddress Address, int Scope) Interface(AddressFamily family)
    {
        if (_interface.Length == 0 && family == AddressFamily.InterNetwork) return (IPAddress.Any, 0);
        if (IPAddress.TryParse(_interface, out var literal) && family == AddressFamily.InterNetwork) { if (literal.AddressFamily != family) throw new ArgumentException("Discovery interface address family mismatch."); return (literal, 0); }
        foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (item.OperationalStatus != OperationalStatus.Up || (_interface.Length == 0 && item.NetworkInterfaceType == NetworkInterfaceType.Loopback)) continue; var properties = item.GetIPProperties(); var addresses = properties.UnicastAddresses.Where(x => x.Address.AddressFamily == family).Select(x => x.Address).ToArray();
            if (addresses.Length == 0 || (_interface.Length > 0 && item.Name != _interface && item.Id != _interface && !addresses.Any(x => x.ToString() == _interface))) continue;
            return (addresses[0], family == AddressFamily.InterNetworkV6 ? properties.GetIPv6Properties().Index : 0);
        }
        throw new ArgumentException("Discovery interface is unavailable.");
    }
    private static bool SSDP(ReadOnlySpan<byte> bytes, out string url, out string type)
    {
        url = type = ""; if (bytes.Length > 16384) return false; var lines = Encoding.ASCII.GetString(bytes).Split("\r\n"); if (lines.Length < 3 || lines[0] is not ("HTTP/1.1 200 OK" or "HTTP/1.0 200 OK")) return false;
        foreach (var line in lines.Skip(1)) { if (line.Length == 0) break; var colon = line.IndexOf(':'); if (colon < 1) return false; var name = line[..colon]; var value = line[(colon + 1)..].Trim(); if (name.Equals("location", StringComparison.OrdinalIgnoreCase)) { if (url.Length > 0) return false; url = value; } else if (name.Equals("st", StringComparison.OrdinalIgnoreCase)) { if (type.Length > 0) return false; type = value; } }
        return type.Length > 0 && UPNPProtocol.URL(url, out _);
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); Check(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) _devices.Clear(); base.Dispose(disposing); }
}
