namespace Electron2D;

/// <summary>
/// Тип события окна (канал Window).
/// </summary>
public enum WindowEventType
{
    Shown,
    Hidden,
    Exposed,
    Moved,
    Resized,
    PixelSizeChanged,
    MetalViewResized,
    Minimized,
    Maximized,
    Restored,
    MouseEnter,
    MouseLeave,
    FocusGained,
    FocusLost,
    CloseRequested,
    HitTest,
    ICCProfChanged,
    DisplayChanged,
    DisplayScaleChanged,
    SafeAreaChanged,
    Occluded,
    EnterFullscreen,
    LeaveFullscreen,
    Destroyed,
    HDRStateChanged
}