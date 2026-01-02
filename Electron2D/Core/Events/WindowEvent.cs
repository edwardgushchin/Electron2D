namespace Electron2D;

/// <summary>
/// Событие окна (канал Window), передаваемое через <c>EventSystem</c>.
/// </summary>
/// <param name="Type">Тип события.</param>
/// <param name="Timestamp">Временная метка события (источник и единицы зависят от backend'а).</param>
/// <param name="WindowId">Идентификатор окна.</param>
/// <param name="Data1">Дополнительные данные события (зависит от <paramref name="Type"/>).</param>
/// <param name="Data2">Дополнительные данные события (зависит от <paramref name="Type"/>).</param>
public readonly record struct WindowEvent(WindowEventType Type, ulong Timestamp, uint WindowId, int Data1 = 0, int Data2 = 0);