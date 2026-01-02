using SDL3;

namespace Electron2D;

/// <summary>
/// Событие ввода (клавиатура/прочие устройства), унифицированное для внутреннего пайплайна.
/// </summary>
/// <remarks>
/// В текущей версии событие хранит <see cref="KeyCode"/> (полученный из <see cref="SDL.Scancode"/>),
/// а также два дополнительных числовых значения (<see cref="X"/>, <see cref="Y"/>) для аналоговых/координатных данных.
/// </remarks>
public readonly struct InputEvent(InputEventType type, ulong timestamp, SDL.Scancode code, float x = 0f, float y = 0f)
{
    public InputEventType Type { get; } = type;
    public KeyCode Code { get; } = (KeyCode)code;
    public float X { get; } = x;
    public float Y { get; } = y;
    
    public ulong Timestamp { get; } = timestamp;
}