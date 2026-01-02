namespace Electron2D;

/// <summary>
/// Событие ввода, унифицированное для пайплайна движка.
/// </summary>
/// <remarks>
/// Code использует движковый KeyCode; маппинг с платформы выполняется в Runtime.
/// </remarks>
public readonly struct InputEvent(InputEventType type, ulong timestamp, KeyCode code, float x = 0f, float y = 0f)
{
    public InputEventType Type { get; } = type;
    public KeyCode Code { get; } = code;
    public float X { get; } = x;
    public float Y { get; } = y;
    public ulong Timestamp { get; } = timestamp;
}