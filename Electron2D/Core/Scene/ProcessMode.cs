namespace Electron2D;

/// <summary>
/// Режим обработки узла (как учитывать паузу и наследование режима).
/// </summary>
public enum ProcessMode
{
    Inherit = 0,
    Pausable,
    WhenPaused,
    Always,
    Disabled
}