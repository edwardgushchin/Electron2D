namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Returns the most recently opened active native popup, or InvalidWindowId.</summary>
    /// <returns>A live native window identity; embedded popup windows do not enter the native popup stack.</returns>
    /// <remarks>Hidden and retired popup lifetimes are omitted. The query runs on the display owner thread.</remarks>
    /// <exception cref="InvalidOperationException">No native display is active or the caller is off-owner.</exception>
    public static int WindowGetActivePopup() => RequireService().WindowGetActivePopupCore();

    /// <summary>Returns the window's popup safe rectangle in desktop coordinates.</summary>
    /// <param name="windowID">A live native window identity.</param>
    /// <returns>The configured rectangle, or an empty rectangle before configuration.</returns>
    /// <remarks>Empty areas do not prevent outside-click dismissal. Control-owned safe areas follow their native parent.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The window is absent or retired.</exception>
    /// <exception cref="NotSupportedException">A control-owned area requires unavailable global desktop coordinates.</exception>
    /// <exception cref="InvalidOperationException">No native display is active or the caller is off-owner.</exception>
    public static Rect2i WindowGetPopupSafeRect(int windowID) => RequireService().WindowGetPopupSafeRectCore(windowID);

    /// <summary>Configures a desktop rectangle in which pointer presses do not dismiss a native popup.</summary>
    /// <param name="windowID">A live native window identity.</param>
    /// <param name="rectangle">The safe desktop area; an empty area clears it.</param>
    /// <remarks>Wayland cannot locate a desktop rectangle; nonempty public requests fail explicitly there.
    /// Runtime controls use parent-client areas without fabricating desktop coordinates. Safe areas are runtime state.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The window is absent or retired, or the size is negative.</exception>
    /// <exception cref="NotSupportedException">The backend cannot supply global desktop coordinates.</exception>
    /// <exception cref="InvalidOperationException">No native display is active or the caller is off-owner.</exception>
    public static void WindowSetPopupSafeRect(int windowID, Rect2i rectangle) => RequireService().WindowSetPopupSafeRectCore(windowID, rectangle);
}
