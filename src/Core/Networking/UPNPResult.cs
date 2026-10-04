namespace Electron2D;

/// <summary>Identifies UPNP discovery/control outcomes.</summary>
public enum UPNPResult
{
    /// <summary>UPNP command or discovery was successful.</summary>
    Success = 0,
    /// <summary>Not authorized to use the command on the UPNP device. May be returned when the user disabled UPNP on their router.</summary>
    NotAuthorized = 1,
    /// <summary>No port mapping was found for the given port, protocol combination on the given UPNP device.</summary>
    PortMappingNotFound = 2,
    /// <summary>Inconsistent parameters.</summary>
    InconsistentParameters = 3,
    /// <summary>No such entry in array. May be returned if a given port, protocol combination is not found on a UPNP device.</summary>
    NoSuchEntryInArray = 4,
    /// <summary>The action failed.</summary>
    ActionFailed = 5,
    /// <summary>The UPNP device does not allow wildcard values for the source IP address.</summary>
    SourceIPWildcardNotPermitted = 6,
    /// <summary>The UPNP device does not allow wildcard values for the external port.</summary>
    ExternalPortWildcardNotPermitted = 7,
    /// <summary>The UPNP device does not allow wildcard values for the internal port.</summary>
    InternalPortWildcardNotPermitted = 8,
    /// <summary>The remote host value must be a wildcard.</summary>
    RemoteHostMustBeWildcard = 9,
    /// <summary>The external port value must be a wildcard.</summary>
    ExternalPortMustBeWildcard = 10,
    /// <summary>No port maps are available. May also be returned if port mapping functionality is not available.</summary>
    NoPortMapsAvailable = 11,
    /// <summary>Conflict with other mechanism. May be returned instead of ConflictWithOtherMapping if a port mapping conflicts with an existing one.</summary>
    ConflictWithOtherMechanism = 12,
    /// <summary>Conflict with an existing port mapping.</summary>
    ConflictWithOtherMapping = 13,
    /// <summary>External and internal port values must be the same.</summary>
    SamePortValuesRequired = 14,
    /// <summary>Only permanent leases are supported. Do not use the duration parameter when adding port mappings.</summary>
    OnlyPermanentLeaseSupported = 15,
    /// <summary>Invalid gateway.</summary>
    InvalidGateway = 16,
    /// <summary>Invalid port.</summary>
    InvalidPort = 17,
    /// <summary>Invalid protocol.</summary>
    InvalidProtocol = 18,
    /// <summary>Invalid duration.</summary>
    InvalidDuration = 19,
    /// <summary>Invalid arguments.</summary>
    InvalidArguments = 20,
    /// <summary>Invalid response.</summary>
    InvalidResponse = 21,
    /// <summary>Invalid parameter.</summary>
    InvalidParameter = 22,
    /// <summary>HTTP error.</summary>
    HTTPError = 23,
    /// <summary>Socket error.</summary>
    SocketError = 24,
    /// <summary>Error allocating memory.</summary>
    MemoryAllocationError = 25,
    /// <summary>No gateway available. You may need to call Discover first, or discovery didn't detect any valid IGDs (InternetGatewayDevices).</summary>
    NoGateway = 26,
    /// <summary>No devices available. You may need to call Discover first, or discovery didn't detect any valid UPNP devices.</summary>
    NoDevices = 27,
    /// <summary>Unknown error.</summary>
    UnknownError = 28,
}
