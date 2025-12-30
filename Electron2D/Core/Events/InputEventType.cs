namespace Electron2D;

public enum InputEventType
{
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
    
    MouseMotion,
    MouseButtonDown,
    MouseButtonUp,
    MouseWheel,
    MouseAdded,
    MouseRemoved,
    
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
    GamepadSteamHandleUpdated,
}