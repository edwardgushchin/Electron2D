namespace Electron2D;

/// <summary>Identifies sample loop traversal.</summary>
public enum AudioLoopMode
{
    /// <summary>Play once.</summary>
    Disabled = 0,
    /// <summary>Wrap forward at the loop boundary.</summary>
    Forward = 1,
    /// <summary>Reflect direction at both loop boundaries.</summary>
    PingPong = 2,
    /// <summary>Traverse the loop backward.</summary>
    Backward = 3
}
