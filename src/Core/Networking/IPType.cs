namespace Electron2D;

/// <summary>Selects the address family requested from the host resolver.</summary>
public enum IPType
{
    /// <summary>No specific family; uses the host's unspecified-family policy.</summary>
    None = 0,
    /// <summary>Internet protocol version four only.</summary>
    IPv4 = 1,
    /// <summary>Internet protocol version six only.</summary>
    IPv6 = 2,
    /// <summary>Any supported address family.</summary>
    Any = 3,
}

/// <summary>Reports a retained hostname queue slot's resolution phase.</summary>
public enum ResolverStatus
{
    /// <summary>The slot has not been assigned or has been erased.</summary>
    None = 0,
    /// <summary>The host resolver has not completed.</summary>
    Waiting = 1,
    /// <summary>One or more addresses are available.</summary>
    Done = 2,
    /// <summary>The lookup failed or returned no addresses in the requested family.</summary>
    Error = 3,
}
