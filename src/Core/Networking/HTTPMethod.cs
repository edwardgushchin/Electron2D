namespace Electron2D;

/// <summary>Identifies the method domain used by HTTP operations.</summary>
public enum HTTPMethod
{
    /// <summary>Opens an authority-form tunnel; successful responses preserve following transport bytes.</summary>
    Connect = 7,
    /// <summary>Requests removal of the target resource.</summary>
    Delete = 4,
    /// <summary>Retrieves the target representation.</summary>
    Get = 0,
    /// <summary>Retrieves response metadata without consuming a response body.</summary>
    Head = 1,
    /// <summary>Bounds the method domain; this sentinel cannot dispatch a request.</summary>
    Max = 9,
    /// <summary>Queries communication options, including the asterisk target.</summary>
    Options = 5,
    /// <summary>Applies a partial change with caller-supplied body bytes.</summary>
    Patch = 8,
    /// <summary>Submits caller-supplied content to the target.</summary>
    Post = 2,
    /// <summary>Replaces the target representation with caller-supplied content.</summary>
    Put = 3,
    /// <summary>Requests a diagnostic loopback exchange.</summary>
    Trace = 6,
}
