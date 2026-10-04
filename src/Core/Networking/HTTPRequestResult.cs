namespace Electron2D;

/// <summary>Identifies the result domain used by HTTP operations.</summary>
public enum HTTPRequestResult
{
    /// <summary>Compressed content is malformed, truncated or fails checksum verification.</summary>
    BodyDecompressFailed = 8,
    /// <summary>Wire or decoded body size exceeds the configured limit.</summary>
    BodySizeLimitExceeded = 7,
    /// <summary>The remote or proxy transport could not connect.</summary>
    CantConnect = 2,
    /// <summary>The remote or proxy hostname could not resolve.</summary>
    CantResolve = 3,
    /// <summary>Response body framing is invalid or ends prematurely.</summary>
    ChunkedBodySizeMismatch = 1,
    /// <summary>The active connection failed during transfer.</summary>
    ConnectionError = 4,
    /// <summary>The sibling download staging file could not be opened.</summary>
    DownloadFileCantOpen = 10,
    /// <summary>Download writing or final destination replacement failed.</summary>
    DownloadFileWriteError = 11,
    /// <summary>The peer ended the exchange before a response was available.</summary>
    NoResponse = 6,
    /// <summary>Following another redirect would exceed the configured limit.</summary>
    RedirectLimitReached = 12,
    /// <summary>Request preparation or dispatch failed.</summary>
    RequestFailed = 9,
    /// <summary>A complete response was received; its HTTP status may still describe a server/client error.</summary>
    Success = 0,
    /// <summary>The sampled scene-process timeout elapsed.</summary>
    Timeout = 13,
    /// <summary>TLS establishment or certificate validation failed.</summary>
    TLSHandshakeError = 5,
}
