namespace Electron2D;

public readonly record struct WindowEvent(WindowEventType Type, ulong Timestamp, uint WindowId, int Data1 = 0, int Data2 = 0);