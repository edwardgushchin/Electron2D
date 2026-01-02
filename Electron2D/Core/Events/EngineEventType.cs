namespace Electron2D;

/// <summary>
/// Тип события движка (канал Engine).
/// </summary>
public enum EngineEventType
{
    QuitRequested,
    Terminating,
    LowMemory,
    WillEnterBackground,
    DidEnterBackground,
    WillEnterForeground,
    DidEnterForeground,
    LocaleChanged,
    SystemThemeChanged
}