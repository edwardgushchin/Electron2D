namespace Electron2D;

/// <summary>
/// Событие движка (канал Engine), передаваемое через <c>EventSystem</c>.
/// </summary>
/// <param name="Type">Тип события.</param>
public readonly record struct EngineEvent(EngineEventType Type);