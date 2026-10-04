namespace Electron2D;

/// <summary>Identifies the EventType domain of an ENet transport.</summary>
public enum ENetEventType
{
    /// <summary>Represents a transport failure; Service reports the underlying exception.</summary>
    Error = -1,
    /// <summary>No queued event became available.</summary>
    None = 0,
    /// <summary>A peer completed connection negotiation.</summary>
    Connect = 1,
    /// <summary>A peer completed or timed out disconnection.</summary>
    Disconnect = 2,
    /// <summary>A complete packet was queued on the associated peer.</summary>
    Receive = 3,
}
