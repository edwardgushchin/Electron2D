using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private bool UsesX11KeyboardLayouts => OperatingSystem.IsLinux() && _videoDriver == "x11";

    private static DisplayServer RequireKeyboardLayouts()
    {
        var display = RequireService();
        display.EnsureOwner();
        if (!display.UsesWindowsInput && !display.UsesX11KeyboardLayouts)
            throw new NotSupportedException("Keyboard layout enumeration and activation require a Windows or X11 display.");
        return display;
    }

    private nint X11KeyboardConnection() => WindowGetNativeHandleCore(HandleType.DisplayHandle);

    /// <summary>Gets the number of input layouts currently available to the native desktop.</summary>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off the owner thread, or native enumeration failed.</exception>
    /// <exception cref="NotSupportedException">The active backend does not enumerate input layouts.</exception>
    public static int KeyboardGetLayoutCount()
    {
        var display = RequireKeyboardLayouts();
        return display.UsesX11KeyboardLayouts
            ? X11KeyboardLayouts.Count(display.X11KeyboardConnection())
            : WindowsKeyboardLayouts.Count();
    }

    /// <summary>Gets the active input layout's index in the current native list, or -1 if it is absent.</summary>
    /// <remarks>Requery the list after activation or an operating-system layout change. Indices do not identify layouts across changes.</remarks>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off the owner thread, or native enumeration failed.</exception>
    /// <exception cref="NotSupportedException">The active backend does not enumerate input layouts.</exception>
    public static int KeyboardGetCurrentLayout()
    {
        var display = RequireKeyboardLayouts();
        return display.UsesX11KeyboardLayouts
            ? X11KeyboardLayouts.Current(display.X11KeyboardConnection())
            : WindowsKeyboardLayouts.Current();
    }

    /// <summary>Gets the input language of an indexed native layout, or an empty string when unknown.</summary>
    /// <param name="index">An index in the current native layout list.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the current layout list.</exception>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off the owner thread, or native enumeration failed.</exception>
    /// <exception cref="NotSupportedException">The active backend does not enumerate input layouts.</exception>
    public static string KeyboardGetLayoutLanguage(int index)
    {
        var display = RequireKeyboardLayouts();
        return display.UsesX11KeyboardLayouts
            ? X11KeyboardLayouts.Language(display.X11KeyboardConnection(), index)
            : WindowsKeyboardLayouts.Language(index);
    }

    /// <summary>Gets the localized name of an indexed native layout, or an empty string when unknown.</summary>
    /// <param name="index">An index in the current native layout list.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the current layout list.</exception>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off the owner thread, or native enumeration failed.</exception>
    /// <exception cref="NotSupportedException">The active backend does not enumerate input layouts.</exception>
    public static string KeyboardGetLayoutName(int index)
    {
        var display = RequireKeyboardLayouts();
        if (OperatingSystem.IsWindows())
            return WindowsKeyboardLayouts.Name(index);
        return X11KeyboardLayouts.Name(display.X11KeyboardConnection(), index);
    }

    /// <summary>Activates an existing Windows input locale or locks an X11 keyboard group.</summary>
    /// <param name="index">An index in the current native layout list.</param>
    /// <remarks>The new layout is available to physical-key and label queries when the call returns. On X11 the group lock applies to the connected X server, and active group modifiers can still change the effective layout. A held key retains its original logical identity and label until release.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the current layout list.</exception>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off the owner thread, or native enumeration or activation failed.</exception>
    /// <exception cref="NotSupportedException">The active backend does not activate input layouts.</exception>
    public static void KeyboardSetCurrentLayout(int index)
    {
        var display = RequireKeyboardLayouts();
        if (display.UsesX11KeyboardLayouts)
        {
            X11KeyboardLayouts.Activate(display.X11KeyboardConnection(), index);
            // SDL consumes the XKB group notification before a physical-key lookup can read its map.
            SDL.PumpEvents();
        }
        else
            WindowsKeyboardLayouts.Activate(index, display.WindowGetNativeHandleCore(HandleType.WindowHandle));
    }
}
