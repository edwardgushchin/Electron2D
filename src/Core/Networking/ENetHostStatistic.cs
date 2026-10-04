namespace Electron2D;

/// <summary>Identifies the HostStatistic domain of an ENet transport.</summary>
public enum ENetHostStatistic
{
    /// <summary>Counts transmitted bytes.</summary>
    TotalSentData = 0,
    /// <summary>Counts transmitted datagrams.</summary>
    TotalSentPackets = 1,
    /// <summary>Counts received bytes.</summary>
    TotalReceivedData = 2,
    /// <summary>Counts received datagrams.</summary>
    TotalReceivedPackets = 3,
}
