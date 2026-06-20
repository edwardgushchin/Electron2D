namespace Electron2D;

#region VSyncMode

/// <summary>
/// Режим вертикальной синхронизации (логическая политика).
/// </summary>
public enum VSyncMode
{
    Disabled = 0,
    Enabled = 1,
    Adaptive = -1,
}

#endregion