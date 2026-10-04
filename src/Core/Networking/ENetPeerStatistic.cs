namespace Electron2D;

/// <summary>Identifies the PeerStatistic domain of an ENet transport.</summary>
public enum ENetPeerStatistic
{
    /// <summary>Mean reliable-packet loss scaled by PacketLossScale.</summary>
    PacketLoss = 0,
    /// <summary>Variance of reliable-packet loss.</summary>
    PacketLossVariance = 1,
    /// <summary>Native monotonic timestamp of the last loss update.</summary>
    PacketLossEpoch = 2,
    /// <summary>Mean reliable-packet round-trip time in milliseconds.</summary>
    RoundTripTime = 3,
    /// <summary>Round-trip-time variance in milliseconds.</summary>
    RoundTripTimeVariance = 4,
    /// <summary>Last reliable-packet round-trip time in milliseconds.</summary>
    LastRoundTripTime = 5,
    /// <summary>Last recorded round-trip-time variance.</summary>
    LastRoundTripTimeVariance = 6,
    /// <summary>Current scaled unreliable-packet throttle.</summary>
    PacketThrottle = 7,
    /// <summary>Current scaled throttle limit.</summary>
    PacketThrottleLimit = 8,
    /// <summary>Native throttle sequence counter.</summary>
    PacketThrottleCounter = 9,
    /// <summary>Native monotonic timestamp of the last throttle update.</summary>
    PacketThrottleEpoch = 10,
    /// <summary>Configured throttle acceleration.</summary>
    PacketThrottleAcceleration = 11,
    /// <summary>Configured throttle deceleration.</summary>
    PacketThrottleDeceleration = 12,
    /// <summary>Configured throttle measurement interval in milliseconds.</summary>
    PacketThrottleInterval = 13,
}
