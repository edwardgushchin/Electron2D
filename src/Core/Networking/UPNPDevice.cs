using System.Globalization;
using System.Net;
namespace Electron2D;

/// <summary>Stores a discovered device and executes synchronous Internet Gateway SOAP commands.</summary>
/// <remarks>Calls and disposal require the constructing thread. Network commands are allocating cold work,
/// bounded to five seconds and one MiB per HTTP exchange. Collection membership borrows this object.
/// Configuration is caller-authored; a discovered OK status follows actual description/connection validation.</remarks>
public class UPNPDevice : ElectronObject
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private string _descriptionURL = "", _serviceType = "", _controlURL = "", _igdServiceType = "", _ourAddress = "";
    private IGDStatus _status = IGDStatus.UnknownError;
    /// <summary>Creates an unassessed device with empty endpoint metadata.</summary>
    public UPNPDevice() { }
    internal void CheckDevice() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("UPNP device access requires its constructing thread."); }
    /// <summary>Gets or sets the description URL.</summary><value>Empty initially; discovered devices retain the advertised URL.</value>
    public string DescriptionURL { get { CheckDevice(); return _descriptionURL; } set { CheckDevice(); ArgumentNullException.ThrowIfNull(value); _descriptionURL = value; } }
    /// <summary>Gets or sets the SSDP search-target identity.</summary><value>Empty initially.</value>
    public string ServiceType { get { CheckDevice(); return _serviceType; } set { CheckDevice(); ArgumentNullException.ThrowIfNull(value); _serviceType = value; } }
    /// <summary>Gets or sets the absolute SOAP control URL.</summary><value>Empty initially; commands require an HTTP URL.</value>
    public string IGDControlURL { get { CheckDevice(); return _controlURL; } set { CheckDevice(); ArgumentNullException.ThrowIfNull(value); _controlURL = value; } }
    /// <summary>Gets or sets the WANIPConnection or WANPPPConnection service identity.</summary><value>Empty initially; used in SOAPAction and the request namespace.</value>
    public string IGDServiceType { get { CheckDevice(); return _igdServiceType; } set { CheckDevice(); ArgumentNullException.ThrowIfNull(value); _igdServiceType = value; } }
    /// <summary>Gets or sets the local address routed to the description endpoint.</summary><value>Empty initially; used as the internal client of a port mapping.</value>
    public string IGDOurAddress { get { CheckDevice(); return _ourAddress; } set { CheckDevice(); ArgumentNullException.ThrowIfNull(value); _ourAddress = value; } }
    /// <summary>Gets or sets the gateway assessment.</summary><value>UnknownError initially; OK marks a usable gateway.</value>
    public IGDStatus IGDStatus { get { CheckDevice(); return _status; } set { CheckDevice(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _status = value; } }
    /// <summary>Reports whether this device's assessment is OK.</summary><returns>The current status projection; does not perform network work.</returns>
    public bool IsValidGateway() { CheckDevice(); return _status == IGDStatus.OK; }
    /// <summary>Queries the external IP address synchronously.</summary><returns>A normalized IP literal, or empty on gateway/control/network failure.</returns>
    public string QueryExternalAddress()
    {
        CheckDevice(); if (!IsValidGateway()) return ""; var result = UPNPProtocol.SOAP(_controlURL, _igdServiceType, "GetExternalIPAddress", [], out var response);
        var value = UPNPProtocol.Value(response, "NewExternalIPAddress"); return result == UPNPResult.Success && IPAddress.TryParse(value, out var address) ? address.ToString() : "";
    }
    /// <summary>Requests a mapping to this device's configured local client.</summary><param name="port">External port, one through 65535.</param><param name="portInternal">Internal port, zero uses the external port.</param><param name="description">XML-escaped label; empty uses Electron2D.</param><param name="protocol">Exactly UDP or TCP.</param><param name="duration">Nonnegative lease seconds; zero requests a permanent lease.</param><returns>Typed local validation or gateway outcome.</returns><remarks>Gateways may reject conflicts or leases. No automatic renewal/removal is performed.</remarks>
    public UPNPResult AddPortMapping(int port, int portInternal = 0, string description = "", string protocol = "UDP", int duration = 0)
    {
        CheckDevice(); if (!IsValidGateway()) return UPNPResult.InvalidGateway; if (port is < 1 or > 65535 || portInternal is < 0 or > 65535) return UPNPResult.InvalidPort;
        if (protocol is not ("UDP" or "TCP")) return UPNPResult.InvalidProtocol; if (duration < 0) return UPNPResult.InvalidDuration;
        if (description is null || !IPAddress.TryParse(_ourAddress, out _)) return UPNPResult.InvalidArguments;
        return UPNPProtocol.SOAP(_controlURL, _igdServiceType, "AddPortMapping", [("NewRemoteHost", ""), ("NewExternalPort", Number(port)), ("NewProtocol", protocol), ("NewInternalPort", Number(portInternal == 0 ? port : portInternal)), ("NewInternalClient", _ourAddress), ("NewEnabled", "1"), ("NewPortMappingDescription", description.Length == 0 ? "Electron2D" : description), ("NewLeaseDuration", Number(duration))], out _);
    }
    /// <summary>Requests deletion of an external port/protocol mapping.</summary><param name="port">One through 65535.</param><param name="protocol">Exactly UDP or TCP.</param><returns>Typed validation or gateway outcome.</returns><remarks>The explicit control metadata may be used even when status is not OK.</remarks>
    public UPNPResult DeletePortMapping(int port, string protocol = "UDP")
    {
        CheckDevice(); if (port is < 1 or > 65535) return UPNPResult.InvalidPort; if (protocol is not ("UDP" or "TCP")) return UPNPResult.InvalidProtocol;
        return UPNPProtocol.SOAP(_controlURL, _igdServiceType, "DeletePortMapping", [("NewRemoteHost", ""), ("NewExternalPort", Number(port)), ("NewProtocol", protocol)], out _);
    }
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); CheckDevice(); }
}
