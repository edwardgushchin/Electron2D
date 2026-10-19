namespace Electron2D;

/// <summary>Selects the common WebSocket and WebRTC message payload contract.</summary>
public enum PacketWriteMode
{
    /// <summary>Payload bytes must be valid UTF-8 text.</summary>
    Text = 0,
    /// <summary>Payload bytes are arbitrary binary data.</summary>
    Binary = 1
}
