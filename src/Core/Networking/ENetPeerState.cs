namespace Electron2D;

/// <summary>Identifies the PeerState domain of an ENet transport.</summary>
public enum ENetPeerState
{
    /// <summary>Native peer slot is detached.</summary>
    Disconnected = 0,
    /// <summary>An outgoing connection is negotiating.</summary>
    Connecting = 1,
    /// <summary>An incoming connection acknowledgement is pending.</summary>
    AcknowledgingConnect = 2,
    /// <summary>A connection awaits local negotiation progress.</summary>
    ConnectionPending = 3,
    /// <summary>Successful negotiation awaits local notification.</summary>
    ConnectionSucceeded = 4,
    /// <summary>Application packets can be exchanged.</summary>
    Connected = 5,
    /// <summary>Outgoing packets drain before disconnect.</summary>
    DisconnectLater = 6,
    /// <summary>An outgoing disconnect awaits acknowledgement.</summary>
    Disconnecting = 7,
    /// <summary>An incoming disconnect awaits acknowledgement.</summary>
    AcknowledgingDisconnect = 8,
    /// <summary>Disconnection awaits local notification.</summary>
    Zombie = 9,
}
