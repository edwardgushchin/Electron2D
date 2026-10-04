namespace Electron2D;

/// <summary>Selects ENet packet reliability, ordering and fragmentation behavior.</summary>
[Flags]
public enum ENetPacketFlags
{
    /// <summary>Uses ordinary sequenced unreliable delivery.</summary>
    None = 0,
    /// <summary>Retransmits and orders the packet.</summary>
    Reliable = 1,
    /// <summary>Permits delivery without sequencing.</summary>
    Unsequenced = 2,
    /// <summary>Allows unreliable fragments instead of promoting fragmented data to reliable delivery.</summary>
    UnreliableFragment = 8
}
/// <summary>Contains one typed ENet service event; packet payloads are read from the associated peer.</summary>
/// <param name="Type">Event domain.</param><param name="Peer">Borrowed source peer or null for None.</param><param name="Data">Connection/disconnection data.</param><param name="Channel">Native channel for Receive, otherwise zero.</param>
public readonly record struct ENetEvent(ENetEventType Type, ENetPacketPeer? Peer, uint Data, int Channel);
