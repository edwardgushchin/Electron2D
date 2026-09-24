namespace Electron2D;

/// <summary>Selects which existing size components an offset preset preserves.</summary>
public enum ControlLayoutPresetMode
{
    /// <summary>Use intrinsic minimum width and height.</summary>
    MinSize = 0,
    /// <summary>Keep the current width and use intrinsic minimum height.</summary>
    KeepWidth = 1,
    /// <summary>Use intrinsic minimum width and keep the current height.</summary>
    KeepHeight = 2,
    /// <summary>Keep the current width and height.</summary>
    KeepSize = 3
}
