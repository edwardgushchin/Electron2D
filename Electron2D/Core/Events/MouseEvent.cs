namespace Electron2D;

public class MouseEvent(MouseEventType type, ulong timestamp, MouseButton button, float x = 0f, float y = 0f)
{
    public MouseEventType Type { get; } = type;
    public MouseButton Button { get; } = button;
    public float X { get; } = x;
    public float Y { get; } = y;
    public ulong Timestamp { get; } = timestamp;
}