using Electron2D;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

internal static class UPNPTests
{
    private const string WAN = "urn:schemas-upnp-org:service:WANIPConnection:1";
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run()
    {
        using var collection = new UPNP(); using var device = new UPNPDevice(); Check(collection.DiscoverLocalPort == 0 && !collection.DiscoverIPV6 && collection.DiscoverMulticastIF == "" && device.IGDStatus == IGDStatus.UnknownError && !device.IsValidGateway(), "UPNP defaults.");
        Check(collection.AddPortMapping(0) == UPNPResult.NoGateway && device.AddPortMapping(0) == UPNPResult.InvalidGateway && device.DeletePortMapping(0) == UPNPResult.InvalidPort, "Validation precedence.");
        collection.AddDevice(device); collection.AddDevice(device); collection.SetDevice(1, device); collection.RemoveDevice(0); Check(collection.GetDeviceCount() == 1 && ReferenceEquals(collection.GetDevice(0), device), "Ordered borrowed membership."); Check(collection.Discover(-1) == UPNPResult.InvalidParameter && collection.GetDeviceCount() == 1, "Invalid discover preserves collection."); Reject<ArgumentOutOfRangeException>(() => collection.DiscoverLocalPort = 65536); Reject<ArgumentOutOfRangeException>(() => collection.GetDevice(2)); Reject<ArgumentNullException>(() => collection.AddDevice(null!));
        Reject<InvalidOperationException>(() => Task.Run(collection.ClearDevices).GetAwaiter().GetResult()); Reject<InvalidOperationException>(() => Task.Run(device.Dispose).GetAwaiter().GetResult()); Check(!device.IsDisposed, "Wrong-thread disposal does not poison device.");
        using var fixture = new Gateway(false, true); collection.DiscoverMulticastIF = "127.0.0.1"; Check(collection.Discover(100) == UPNPResult.Success && collection.GetDeviceCount() == 1, "Native SSDP reply parse/dedup/filter."); var gateway = collection.GetGateway(); Check(gateway is not null && gateway.IGDOurAddress == "127.0.0.1" && gateway.IGDControlURL == fixture.URL + "control" && gateway.ServiceType.Contains("InternetGatewayDevice"), "Description URLBase, local route and SOAP connected assessment.");
        Check(collection.QueryExternalAddress() == "203.0.113.7", "Independent external-address SOAP reply."); Check(collection.AddPortMapping(41234, description: "Café <one> & two", duration: 121) == UPNPResult.Success, "Escaped typed mapping command."); var mapping = fixture.Requests.Last(x => x.Action == "AddPortMapping"); Check(mapping.Values["NewInternalPort"] == "41234" && mapping.Values["NewRemoteHost"] == "" && mapping.Values["NewInternalClient"] == "127.0.0.1" && mapping.Values["NewProtocol"] == "UDP" && mapping.Values["NewEnabled"] == "1" && mapping.Values["NewLeaseDuration"] == "121" && mapping.Values["NewPortMappingDescription"] == "Café <one> & two", "Independent SOAP request oracle.");
        Check(collection.DeletePortMapping(41234) == UPNPResult.Success && fixture.Requests.Last().Action == "DeletePortMapping", "Deletion executes.");
        Check(gateway!.AddPortMapping(0) == UPNPResult.InvalidPort && gateway.AddPortMapping(1, 65536) == UPNPResult.InvalidPort && gateway.AddPortMapping(1, protocol: "udp") == UPNPResult.InvalidProtocol && gateway.AddPortMapping(1, duration: -1) == UPNPResult.InvalidDuration && gateway.AddPortMapping(1, description: "bad\0") == UPNPResult.InvalidArguments, "Mapping bounds and XML validation.");
        foreach (var (fault, expected) in new (int, UPNPResult)[] { (402, UPNPResult.InvalidArguments), (403, UPNPResult.NotAuthorized), (606, UPNPResult.NotAuthorized), (501, UPNPResult.ActionFailed), (714, UPNPResult.NoSuchEntryInArray), (715, UPNPResult.SourceIPWildcardNotPermitted), (716, UPNPResult.ExternalPortWildcardNotPermitted), (718, UPNPResult.ConflictWithOtherMapping), (724, UPNPResult.SamePortValuesRequired), (725, UPNPResult.OnlyPermanentLeaseSupported), (726, UPNPResult.RemoteHostMustBeWildcard), (727, UPNPResult.ExternalPortMustBeWildcard), (728, UPNPResult.NoPortMapsAvailable), (729, UPNPResult.ConflictWithOtherMechanism), (732, UPNPResult.InternalPortWildcardNotPermitted), (733, UPNPResult.InconsistentParameters), (999, UPNPResult.UnknownError) }) { fixture.Fault = fault; Check(gateway.DeletePortMapping(41234) == expected, "SOAP fault mapping " + fault); }
        fixture.Fault = 0; fixture.Mode = 1; Check(gateway.DeletePortMapping(41234) == UPNPResult.InvalidResponse, "Wrong-action reply rejects."); fixture.Mode = 2; Check(gateway.DeletePortMapping(41234) == UPNPResult.InvalidResponse, "DTD rejects without external entity access."); fixture.Mode = 3; Check(gateway.DeletePortMapping(41234) == UPNPResult.InvalidResponse, "Declared aggregate response budget rejects."); fixture.Mode = 6; Check(gateway.DeletePortMapping(41234) == UPNPResult.HTTPError, "Non-SOAP HTTP failure."); fixture.Mode = 4; Check(gateway.QueryExternalAddress() == "", "Non-IP external address rejects."); fixture.Mode = 0;
        fixture.Connected = false; var disconnected = collection.Discover(150); Check(disconnected == UPNPResult.Success && collection.GetGateway() is null && collection.GetDeviceCount() == 1 && collection.GetDevice(0).IGDStatus == IGDStatus.Disconnected, "Actual disconnected gateway: " + disconnected + " count=" + collection.GetDeviceCount() + " status=" + (collection.GetDeviceCount() == 0 ? "absent" : collection.GetDevice(0).IGDStatus.ToString())); fixture.Connected = true;
        fixture.DescriptionMode = 1; Check(collection.Discover(150) == UPNPResult.Success && collection.GetDevice(0).IGDStatus == IGDStatus.UnknownDevice, "Malformed description rejects."); fixture.DescriptionMode = 2; Check(collection.Discover(150) == UPNPResult.Success && collection.GetDevice(0).IGDStatus == IGDStatus.HTTPEmpty, "Empty description classification."); fixture.DescriptionMode = 3; Check(collection.Discover(150) == UPNPResult.Success && collection.GetDevice(0).IGDStatus == IGDStatus.NoIGD, "Non-IGD description."); fixture.DescriptionMode = 4; Check(collection.Discover(150) == UPNPResult.Success && collection.GetDevice(0).IGDStatus == IGDStatus.InvalidControl, "Absent control URL."); fixture.DescriptionMode = 0;
        Check(collection.Discover(150, deviceFilter: "not-present") == UPNPResult.Success && collection.GetDeviceCount() == 0, "Nonmatching filter preserves discovery success."); fixture.Silent = true; Check(collection.Discover(30) == UPNPResult.NoDevices, "No-device deadline."); fixture.Silent = false;
        collection.AddDevice(gateway); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { if (collection.GetDeviceCount() != 1 || !ReferenceEquals(collection.GetGateway(), gateway) || !gateway.IsValidGateway() || gateway.IGDStatus != IGDStatus.OK) throw new InvalidOperationException("UPNP metadata reuse."); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Metadata queries allocate zero managed bytes."); collection.ClearDevices(); Check(!gateway.IsDisposed && gateway.QueryExternalAddress() == "203.0.113.7", "Clearing memberships preserves borrowed device."); gateway.Dispose();
        using (var stalled = new Gateway(false, false)) { stalled.Mode = 5; using var bounded = new UPNPDevice { IGDStatus = IGDStatus.OK, IGDControlURL = stalled.URL + "control", IGDServiceType = WAN }; var deadline = System.Diagnostics.Stopwatch.StartNew(); Check(bounded.QueryExternalAddress() == "" && deadline.Elapsed.TotalSeconds is >= 4.9 and < 6.0, "Stalled HTTP exchange respects five-second deadline."); }
        if (Socket.OSSupportsIPv6) { using var ipv6 = new Gateway(true, false); using var d6 = new UPNPDevice { IGDStatus = IGDStatus.OK, IGDControlURL = ipv6.URL + "control", IGDServiceType = WAN, IGDOurAddress = "::1" }; Check(d6.QueryExternalAddress() == "203.0.113.7" && d6.AddPortMapping(41234, protocol: "TCP") == UPNPResult.Success, "Native IPv6 SOAP commands."); ipv6.Verify(); }
        var root = new Node(); using (var tree = new SceneTree(root)) { using var publicDevice = new UPNPDevice { IGDStatus = IGDStatus.OK, IGDControlURL = fixture.URL + "control", IGDServiceType = WAN, IGDOurAddress = "127.0.0.1" }; using var api = new UPNP(); api.AddDevice(publicDevice); var ran = false; root.AddChild(new Probe(api, () => ran = true)); tree.ProcessFrame(0); Check(ran, "Public Node/SceneTree command workflow."); }
        fixture.Verify(); Console.WriteLine("UPNP native SSDP/discovery, independent IPv4/IPv6 SOAP/mappings/faults, bounded XML, lifetime and public scene checks passed; 64 metadata cycles: 0 managed bytes.");
    }
    private sealed class Probe(UPNP api, Action completed) : Node { protected override void OnEnterTree() => ProcessEnabled = true; protected override void OnProcess(double delta) { Check(api.QueryExternalAddress() == "203.0.113.7", "Scene gateway query."); ProcessEnabled = false; completed(); } }
    private sealed class Gateway : IDisposable
    {
        private readonly TcpListener _tcp;
        private readonly Socket? _udp;
        private readonly Task _httpTask, _udpTask;
        private volatile bool _stop;
        internal volatile bool Silent, Connected = true;
        internal volatile int Fault, Mode, DescriptionMode;
        internal readonly ConcurrentQueue<(string Action, Dictionary<string, string> Values)> Requests = new();
        private readonly ConcurrentQueue<Exception> _errors = new();
        internal string URL { get; }
        internal Gateway(bool ipv6, bool discover)
        {
            _tcp = new TcpListener(ipv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback, 0); _tcp.Start(); URL = "http://" + (ipv6 ? "[::1]" : "127.0.0.1") + ":" + ((IPEndPoint)_tcp.LocalEndpoint).Port + "/";
            if (discover) { _udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); _udp.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); _udp.Bind(new IPEndPoint(IPAddress.Any, 1900)); _udp.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250"), IPAddress.Loopback)); _udp.ReceiveTimeout = 100; }
            _httpTask = Task.Run(HTTP); _udpTask = discover ? Task.Run(SSDP) : Task.CompletedTask;
        }
        private void SSDP()
        {
            var bytes = new byte[4096]; EndPoint peer = new IPEndPoint(IPAddress.Any, 0);
            while (!_stop) try { var n = _udp!.ReceiveFrom(bytes, ref peer); var search = Encoding.ASCII.GetString(bytes, 0, n); Check(search.StartsWith("M-SEARCH * HTTP/1.1\r\n") && search.Contains("MAN: \"ssdp:discover\"") && search.Contains("MX: 1"), "Independent SSDP request."); if (Silent) continue; var reply = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nlocation: " + URL + "desc.xml\r\nST: urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\nUSN: uuid:independent\r\n\r\n"); _udp.SendTo("bad\r\n"u8, peer); _udp.SendTo(reply, peer); _udp.SendTo(reply, peer); }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut || _stop) { }
                catch (ObjectDisposedException) when (_stop) { }
                catch (Exception e) { _errors.Enqueue(e); break; }
        }
        private void HTTP()
        {
            var responseMode = 0;
            while (!_stop) try
                {
                    using var client = _tcp.AcceptTcpClient(); client.ReceiveTimeout = 3000; using var stream = client.GetStream(); var header = new StringBuilder(); while (!header.ToString().EndsWith("\r\n\r\n")) { var b = stream.ReadByte(); if (b < 0) throw new IOException("Short request."); header.Append((char)b); }
                    var fields = header.ToString().Split("\r\n"); var length = fields.Where(x => x.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)).Select(x => int.Parse(x.Split(':')[1])).FirstOrDefault(); var body = new byte[length]; stream.ReadExactly(body); responseMode = Mode; string result; var code = 200;
                    if (fields[0].StartsWith("GET ")) result = DescriptionMode == 1 ? "<broken" : DescriptionMode == 2 ? "" : "<root xmlns=\"urn:schemas-upnp-org:device-1-0\"><URLBase>" + URL + "base/</URLBase><device><serviceList>" + (DescriptionMode == 3 ? "" : "<service><serviceType>urn:schemas-upnp-org:service:WANCommonInterfaceConfig:1</serviceType></service>") + "<service><serviceType>" + WAN + "</serviceType>" + (DescriptionMode == 4 ? "" : "<controlURL>../control</controlURL>") + "</service></serviceList></device></root>";
                    else
                    {
                        XNamespace env = "http://schemas.xmlsoap.org/soap/envelope/"; var request = XDocument.Parse(Encoding.UTF8.GetString(body)).Root!.Element(env + "Body")!.Elements().Single(); var action = request.Name.LocalName; Check(fields.Contains("SOAPAction: \"" + WAN + "#" + action + "\""), "Independent SOAPAction header."); Requests.Enqueue((action, request.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value)));
                        var content = action == "GetStatusInfo" ? "<NewConnectionStatus>" + (Connected ? "Connected" : "Disconnected") + "</NewConnectionStatus>" : action == "GetExternalIPAddress" ? "<NewExternalIPAddress>" + (responseMode == 4 ? "not an IP" : "203.0.113.7") + "</NewExternalIPAddress>" : "";
                        result = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><u:" + (responseMode == 1 ? "wrong" : action + "Response") + " xmlns:u=\"" + WAN + "\">" + content + "</u:" + (responseMode == 1 ? "wrong" : action + "Response") + "></s:Body></s:Envelope>";
                        if (Fault > 0) { code = 500; result = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><s:Fault><detail><UPnPError xmlns=\"urn:schemas-upnp-org:control-1-0\"><errorCode>" + Fault + "</errorCode></UPnPError></detail></s:Fault></s:Body></s:Envelope>"; }
                        if (responseMode == 6) { code = 500; result = "service unavailable"; }
                        if (responseMode == 2) result = "<!DOCTYPE x [<!ENTITY e SYSTEM 'http://127.0.0.1:1/private'>]><x>&e;</x>";
                    }
                    if (responseMode == 5) Thread.Sleep(5500); var payload = Encoding.UTF8.GetBytes(result); var reply = Encoding.ASCII.GetBytes("HTTP/1.1 " + code + " OK\r\nContent-Length: " + (responseMode == 3 ? 1048577 : payload.Length) + "\r\nConnection: close\r\n\r\n"); stream.Write(reply); for (var i = 0; i < payload.Length; i += 7) stream.Write(payload.AsSpan(i, Math.Min(7, payload.Length - i)));
                }
                catch (SocketException) when (_stop) { }
                catch (ObjectDisposedException) when (_stop) { }
                catch (IOException) when (responseMode is 3 or 5 || _stop) { }
                catch (Exception e) { _errors.Enqueue(e); break; }
        }
        internal void Verify() { if (!_errors.IsEmpty) throw new AggregateException(_errors); }
        public void Dispose() { _stop = true; _tcp.Stop(); _udp?.Dispose(); Check(Task.WaitAll([_httpTask, _udpTask], 4000), "Fixture shutdown."); Verify(); }
    }
}
