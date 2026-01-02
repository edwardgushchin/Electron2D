namespace Electron2D;

/// <summary>
/// Тип события ввода (клавиатура/мышь/геймпад и т.п.).
/// </summary>
public enum InputEventType
{
    // Keyboard / text
    KeyDown,
    KeyUp,
    TextEditing,
    TextInput,
    KeymapChanged,
    KeyboardAdded,
    KeyboardRemoved,
    TextEditingCandidates,
    ScreenKeyboardShown,
    ScreenKeyboardHidden,

    // Mouse
    MouseMotion,
    MouseButtonDown,
    MouseButtonUp,
    MouseWheel,
    MouseAdded,
    MouseRemoved,

    // Gamepad
    GamepadAxisMotion,
    GamepadButtonDown,
    GamepadButtonUp,
    GamepadAdded,
    GamepadRemoved,
    GamepadRemapped,
    GamepadTouchpadDown,
    GamepadTouchpadMotion,
    GamepadTouchpadUp,
    GamepadSensorUpdate,
    GamepadUpdateComplete,
    GamepadSteamHandleUpdated
}