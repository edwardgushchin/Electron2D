namespace Electron2D;

/// <summary>Standard JSON-RPC 2.0 protocol errors.</summary>
public enum JSONRPCErrorCode
{
    /// <summary>The input text is not valid JSON.</summary>
    ParseError = -32700,
    /// <summary>The document is not a valid request or batch.</summary>
    InvalidRequest = -32600,
    /// <summary>The requested method is not registered.</summary>
    MethodNotFound = -32601,
    /// <summary>The parameters do not match the registered schema.</summary>
    InvalidParams = -32602,
    /// <summary>A handler or result conversion failed.</summary>
    InternalError = -32603,
}
