using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
namespace Electron2D;

internal static class UPNPProtocol
{
    internal const int MaxBody = 1048576;
    private const string Envelope = "http://schemas.xmlsoap.org/soap/envelope/";
    internal static bool URL(string text, out Uri uri) => Uri.TryCreate(text, UriKind.Absolute, out uri!) && uri.Scheme == "http" && uri.UserInfo.Length == 0 && uri.Fragment.Length == 0 && uri.Port > 0;
    internal static bool WAN(string type) => type is "urn:schemas-upnp-org:service:WANIPConnection:1" or "urn:schemas-upnp-org:service:WANIPConnection:2" or "urn:schemas-upnp-org:service:WANPPPConnection:1";
    internal static string? Value(XElement? parent, string name) { var values = parent?.Elements().Where(x => x.Name.LocalName == name).Take(2).ToArray(); return values?.Length == 1 && !values[0].HasElements ? values[0].Value.Trim() : null; }
    internal static XDocument XML(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, false); using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxBody });
        while (reader.Read()) if (reader.Depth > 64) throw new XmlException("Device XML exceeds the depth budget.");
        stream.Position = 0; using var load = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxBody }); return XDocument.Load(load);
    }
    internal static byte[] HTTP(Uri uri, string? action, string? body, out int code, out string localAddress)
    {
        using var http = new HTTPClient(); var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 5; http.ConnectToHost(uri.DnsSafeHost, uri.Port);
        void Poll() { if (Stopwatch.GetTimestamp() >= deadline) throw new IOException("UPNP HTTP exchange timed out."); http.Poll(); Thread.Yield(); }
        while (http.GetStatus() != HTTPStatus.Connected) Poll(); localAddress = (http.Connection as StreamPeerTCP)?.LocalAddress ?? "";
        http.Request(action is null ? HTTPMethod.Get : HTTPMethod.Post, uri.PathAndQuery, action is null ? ["Connection: close"] : ["Content-Type: text/xml; charset=\"utf-8\"", "SOAPAction: \"" + action + "\"", "Connection: close"], body ?? "");
        while (!http.HasResponse()) Poll(); code = (int)http.GetResponseCode(); if (http.GetResponseBodyLength() > MaxBody) throw new InvalidDataException("UPNP response exceeds one MiB.");
        using var output = new MemoryStream(); Span<byte> buffer = stackalloc byte[4096];
        while (http.GetStatus() == HTTPStatus.Body) { if (Stopwatch.GetTimestamp() >= deadline) throw new IOException("UPNP HTTP body timed out."); var read = http.ReadResponseBodyChunk(buffer); if (output.Length + read > MaxBody) throw new InvalidDataException("UPNP response exceeds one MiB."); output.Write(buffer[..read]); if (read == 0) { http.Poll(); Thread.Yield(); } }
        return output.ToArray();
    }
    internal static UPNPResult SOAP(string url, string service, string method, (string Name, string Value)[] arguments, out XElement? response)
    {
        var statusCode = 0; response = null; if (!URL(url, out var uri) || !WAN(service)) return UPNPResult.InvalidArguments;
        try
        {
            var request = new XElement(XName.Get("Envelope", Envelope), new XAttribute(XNamespace.Xmlns + "s", Envelope), new XAttribute(XName.Get("encodingStyle", Envelope), "http://schemas.xmlsoap.org/soap/encoding/"), new XElement(XName.Get("Body", Envelope), new XElement(XName.Get(method, service), new XAttribute(XNamespace.Xmlns + "u", service), arguments.Select(x => new XElement(x.Name, x.Value)))));
            var body = request.ToString(SaveOptions.DisableFormatting); if (Encoding.UTF8.GetByteCount(body) > MaxBody) return UPNPResult.InvalidArguments;
            var bytes = HTTP(uri, service + "#" + method, body, out var code, out _); statusCode = code; var doc = XML(bytes); var envelope = doc.Root; if (envelope?.Name != XName.Get("Envelope", Envelope)) return UPNPResult.InvalidResponse;
            var bodies = envelope.Elements(XName.Get("Body", Envelope)).ToArray(); if (bodies.Length != 1 || bodies[0].Elements().Count() != 1) return UPNPResult.InvalidResponse;
            var payload = bodies[0].Elements().Single(); if (payload.Name == XName.Get("Fault", Envelope))
            {
                var errors = payload.Descendants(XName.Get("errorCode", "urn:schemas-upnp-org:control-1-0")).ToArray(); return errors.Length == 1 && int.TryParse(errors[0].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var fault) ? Fault(fault) : UPNPResult.InvalidResponse;
            }
            if (code != 200) return UPNPResult.HTTPError; if (payload.Name != XName.Get(method + "Response", service)) return UPNPResult.InvalidResponse; response = payload; return UPNPResult.Success;
        }
        catch (XmlException) { return statusCode is not (0 or 200) ? UPNPResult.HTTPError : UPNPResult.InvalidResponse; }
        catch (InvalidDataException) { return UPNPResult.InvalidResponse; }
        catch (ArgumentException) { return UPNPResult.InvalidArguments; }
        catch (IOException) { return UPNPResult.HTTPError; }
        catch (System.Net.Sockets.SocketException) { return UPNPResult.HTTPError; }
    }
    internal static UPNPResult Fault(int code) => code switch
    {
        402 => UPNPResult.InvalidArguments,
        403 or 606 => UPNPResult.NotAuthorized,
        501 => UPNPResult.ActionFailed,
        714 => UPNPResult.NoSuchEntryInArray,
        715 => UPNPResult.SourceIPWildcardNotPermitted,
        716 => UPNPResult.ExternalPortWildcardNotPermitted,
        718 => UPNPResult.ConflictWithOtherMapping,
        724 => UPNPResult.SamePortValuesRequired,
        725 => UPNPResult.OnlyPermanentLeaseSupported,
        726 => UPNPResult.RemoteHostMustBeWildcard,
        727 => UPNPResult.ExternalPortMustBeWildcard,
        728 => UPNPResult.NoPortMapsAvailable,
        729 => UPNPResult.ConflictWithOtherMechanism,
        732 => UPNPResult.InternalPortWildcardNotPermitted,
        733 => UPNPResult.InconsistentParameters,
        _ => UPNPResult.UnknownError
    };
    internal static void Describe(UPNPDevice device)
    {
        if (!URL(device.DescriptionURL, out var uri)) { device.IGDStatus = IGDStatus.HTTPError; return; }
        try
        {
            var bytes = HTTP(uri, null, null, out var code, out var local); if (code != 200) { device.IGDStatus = IGDStatus.HTTPError; return; }
            if (bytes.Length == 0) { device.IGDStatus = IGDStatus.HTTPEmpty; return; }
            var xml = XML(bytes); XNamespace ns = "urn:schemas-upnp-org:device-1-0"; if (xml.Root?.Name != ns + "root") { device.IGDStatus = IGDStatus.UnknownDevice; return; }
            if (!xml.Descendants(ns + "serviceType").Any(x => x.Value.StartsWith("urn:schemas-upnp-org:service:WANCommonInterfaceConfig:", StringComparison.Ordinal))) { device.IGDStatus = IGDStatus.NoIGD; return; }
            var baseText = Value(xml.Root, "URLBase"); var root = string.IsNullOrEmpty(baseText) ? uri : URL(baseText, out var urlBase) ? urlBase : throw new XmlException("Invalid description base URL."); var found = false;
            foreach (var service in xml.Descendants(ns + "service"))
            {
                var type = Value(service, "serviceType"); if (type is null || !WAN(type)) continue; found = true; var control = Value(service, "controlURL"); if (string.IsNullOrEmpty(control) || !Uri.TryCreate(root, control, out var endpoint) || !URL(endpoint.AbsoluteUri, out _)) continue;
                var result = SOAP(endpoint.AbsoluteUri, type, "GetStatusInfo", [], out var state);
                if (result != UPNPResult.Success || Value(state, "NewConnectionStatus") != "Connected") { device.IGDStatus = IGDStatus.Disconnected; continue; }
                var externalResult = SOAP(endpoint.AbsoluteUri, type, "GetExternalIPAddress", [], out var external); if (externalResult != UPNPResult.Success || !IPAddress.TryParse(Value(external, "NewExternalIPAddress"), out var address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) { device.IGDStatus = IGDStatus.Disconnected; continue; }
                device.IGDControlURL = endpoint.AbsoluteUri; device.IGDServiceType = type; device.IGDOurAddress = local; device.IGDStatus = IGDStatus.OK; return;
            }
            if (device.IGDStatus != IGDStatus.Disconnected) device.IGDStatus = found ? IGDStatus.InvalidControl : IGDStatus.NoIGD;
        }
        catch (XmlException) { device.IGDStatus = IGDStatus.UnknownDevice; }
        catch (InvalidDataException) { device.IGDStatus = IGDStatus.UnknownDevice; }
        catch (IOException) { device.IGDStatus = IGDStatus.HTTPError; }
        catch (System.Net.Sockets.SocketException) { device.IGDStatus = IGDStatus.HTTPError; }
    }
}
