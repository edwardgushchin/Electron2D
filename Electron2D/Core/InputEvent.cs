namespace Electron2D;

public readonly struct InputEvent(InputEventType type, int code, float valueX = 0f, float valueY = 0f)
{
    public InputEventType Type { get; } = type;
    public int Code { get; } = code;
    public float X { get; } = valueX;
    public float Y { get; } = valueY;
}