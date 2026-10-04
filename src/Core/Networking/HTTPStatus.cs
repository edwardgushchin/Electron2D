namespace Electron2D;

/// <summary>Identifies the status domain used by HTTP operations.</summary>
public enum HTTPStatus
{
    /// <summary>Response headers are parsed; response body progress is caller-driven.</summary>
    Body = 7,
    /// <summary>All attempted transport connections failed.</summary>
    CantConnect = 4,
    /// <summary>DNS lookup produced no usable address or failed.</summary>
    CantResolve = 2,
    /// <summary>The transport is ready for a new request or a completed bodyless response.</summary>
    Connected = 5,
    /// <summary>TCP, proxy negotiation or TLS establishment is progressing.</summary>
    Connecting = 3,
    /// <summary>Request transmission or response framing/transport failed.</summary>
    ConnectionError = 8,
    /// <summary>No active transport connection is attached.</summary>
    Disconnected = 0,
    /// <summary>Request transmission or response-header parsing is progressing.</summary>
    Requesting = 6,
    /// <summary>An asynchronous hostname lookup is pending.</summary>
    Resolving = 1,
    /// <summary>TLS establishment or certificate validation failed.</summary>
    TLSHandshakeError = 9,
}
