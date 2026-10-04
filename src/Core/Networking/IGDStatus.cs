namespace Electron2D;

/// <summary>Identifies a discovered Internet Gateway Device assessment.</summary>
public enum IGDStatus
{
    /// <summary>OK.</summary>
    OK = 0,
    /// <summary>HTTP error.</summary>
    HTTPError = 1,
    /// <summary>Empty HTTP response.</summary>
    HTTPEmpty = 2,
    /// <summary>Returned response contained no URLs.</summary>
    NoUrls = 3,
    /// <summary>Not a valid IGD.</summary>
    NoIGD = 4,
    /// <summary>Disconnected.</summary>
    Disconnected = 5,
    /// <summary>Unknown device.</summary>
    UnknownDevice = 6,
    /// <summary>Invalid control.</summary>
    InvalidControl = 7,
    /// <summary>Memory allocation error.</summary>
    AllocationError = 8,
    /// <summary>Unknown error.</summary>
    UnknownError = 9,
}
