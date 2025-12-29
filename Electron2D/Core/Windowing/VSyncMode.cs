namespace Electron2D;

/// <summary>
/// Режим вертикальной синхронизации (логическая политика).
/// </summary>
public enum VSyncMode
{
    Disabled = 0,
    Enabled  = 1,
    Adaptive = -1,
}