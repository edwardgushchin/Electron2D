namespace Electron2D;

/// <summary>
/// Событие ввода, унифицированное для пайплайна движка.
/// </summary>
/// <remarks>
/// Code использует движковый KeyCode; маппинг с платформы выполняется в Runtime.
/// </remarks>
public readonly struct KeyboardEvent(KeyboardEventType type, ulong timestamp, KeyCode code)
{
    public KeyboardEventType Type { get; } = type;
    public KeyCode Code { get; } = code;
    public ulong Timestamp { get; } = timestamp;
}