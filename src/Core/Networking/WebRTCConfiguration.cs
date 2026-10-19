namespace Electron2D;

/// <summary>Configures a copied set of ICE discovery/relay servers for a peer.</summary>
public sealed class WebRTCConfiguration
{
    /// <summary>Creates an empty host-candidate configuration.</summary>
    public WebRTCConfiguration() { }
    /// <summary>Gets or initializes STUN/TURN endpoints; an empty list uses host candidates.</summary>
    /// <value>Caller-owned entries copied during Initialize.</value>
    public IReadOnlyList<WebRTCICEServer> ICEServers { get; init; } = [];
}
/// <summary>Describes STUN/TURN URLs and optional relay credentials.</summary>
public sealed class WebRTCICEServer
{
    /// <summary>Creates an empty ICE endpoint group.</summary>
    public WebRTCICEServer() { }
    /// <summary>Gets or initializes one or more discovery/relay URLs.</summary>
    /// <value>STUN, STUNS, TURN or TURNS URLs without embedded null characters.</value>
    public IReadOnlyList<string> URLs { get; init; } = [];
    /// <summary>Gets or initializes an optional relay username.</summary>
    /// <value>Null when no username is needed.</value>
    public string? Username { get; init; }
    /// <summary>Gets or initializes an optional relay password.</summary>
    /// <value>Null when no password is needed; never included in diagnostics.</value>
    public string? Credential { get; init; }
}
/// <summary>Configures a copied data-channel creation request.</summary>
public sealed class WebRTCDataChannelOptions
{
    /// <summary>Creates reliable ordered in-band channel options.</summary>
    public WebRTCDataChannelOptions() { }
    /// <summary>Gets or initializes whether both peers explicitly negotiate the same channel.</summary>
    /// <value>False initially; true requires ID.</value>
    public bool Negotiated { get; init; }
    /// <summary>Gets or initializes an optional stream identity.</summary>
    /// <value>Null for automatic assignment, or zero through 65534.</value>
    public int? ID { get; init; }
    /// <summary>Gets or initializes whether delivery preserves ordering.</summary>
    /// <value>True initially, independently of retransmission reliability.</value>
    public bool Ordered { get; init; } = true;
    /// <summary>Gets or initializes a partial-reliability lifetime in milliseconds.</summary>
    /// <value>Null for no lifetime limit; mutually exclusive with MaxRetransmits.</value>
    public int? MaxPacketLifeTime { get; init; }
    /// <summary>Gets or initializes a partial-reliability retransmission count.</summary>
    /// <value>Null for no count limit; mutually exclusive with MaxPacketLifeTime.</value>
    public int? MaxRetransmits { get; init; }
    /// <summary>Gets or initializes an application subprotocol label.</summary>
    /// <value>Empty initially.</value>
    public string Protocol { get; init; } = string.Empty;
}
