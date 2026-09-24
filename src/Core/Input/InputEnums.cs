namespace Electron2D;

/// <summary>Identifies logical, physical, and printable keyboard keys.</summary>
/// <remarks>
/// Printable values use their Unicode scalar values. Special keys occupy the range beginning at
/// <see cref="Special"/>. Modifier bits are represented separately by <see cref="KeyModifierMask"/>.
/// </remarks>
public enum Key
{
    /// <summary>Identifies no key.</summary>
    None = 0,
    /// <summary>Marks the beginning of the non-Unicode special-key range.</summary>
    Special = 1 << 22,
    /// <summary>Identifies Escape.</summary>
    Escape = Special | 0x01,
    /// <summary>Identifies Tab.</summary>
    Tab = Special | 0x02,
    /// <summary>Identifies reverse Tab.</summary>
    Backtab = Special | 0x03,
    /// <summary>Identifies Backspace.</summary>
    Backspace = Special | 0x04,
    /// <summary>Identifies Enter or Return.</summary>
    Enter = Special | 0x05,
    /// <summary>Identifies the numeric-keypad Enter key.</summary>
    KeypadEnter = Special | 0x06,
    /// <summary>Identifies Insert.</summary>
    Insert = Special | 0x07,
    /// <summary>Identifies Delete.</summary>
    Delete = Special | 0x08,
    /// <summary>Identifies Pause.</summary>
    Pause = Special | 0x09,
    /// <summary>Identifies Print Screen.</summary>
    Print = Special | 0x0A,
    /// <summary>Identifies System Request.</summary>
    SystemRequest = Special | 0x0B,
    /// <summary>Identifies Clear.</summary>
    Clear = Special | 0x0C,
    /// <summary>Identifies Home.</summary>
    Home = Special | 0x0D,
    /// <summary>Identifies End.</summary>
    End = Special | 0x0E,
    /// <summary>Identifies the left arrow.</summary>
    Left = Special | 0x0F,
    /// <summary>Identifies the up arrow.</summary>
    Up = Special | 0x10,
    /// <summary>Identifies the right arrow.</summary>
    Right = Special | 0x11,
    /// <summary>Identifies the down arrow.</summary>
    Down = Special | 0x12,
    /// <summary>Identifies Page Up.</summary>
    PageUp = Special | 0x13,
    /// <summary>Identifies Page Down.</summary>
    PageDown = Special | 0x14,
    /// <summary>Identifies Shift.</summary>
    Shift = Special | 0x15,
    /// <summary>Identifies Control.</summary>
    Control = Special | 0x16,
    /// <summary>Identifies Meta, Command, Windows, or Super.</summary>
    Meta = Special | 0x17,
    /// <summary>Identifies Alt or Option.</summary>
    Alt = Special | 0x18,
    /// <summary>Identifies Caps Lock.</summary>
    CapsLock = Special | 0x19,
    /// <summary>Identifies Num Lock.</summary>
    NumLock = Special | 0x1A,
    /// <summary>Identifies Scroll Lock.</summary>
    ScrollLock = Special | 0x1B,
    /// <summary>Identifies function key F1.</summary>
    F1 = Special | 0x1C,
    /// <summary>Identifies function key F2.</summary>
    F2 = Special | 0x1D,
    /// <summary>Identifies function key F3.</summary>
    F3 = Special | 0x1E,
    /// <summary>Identifies function key F4.</summary>
    F4 = Special | 0x1F,
    /// <summary>Identifies function key F5.</summary>
    F5 = Special | 0x20,
    /// <summary>Identifies function key F6.</summary>
    F6 = Special | 0x21,
    /// <summary>Identifies function key F7.</summary>
    F7 = Special | 0x22,
    /// <summary>Identifies function key F8.</summary>
    F8 = Special | 0x23,
    /// <summary>Identifies function key F9.</summary>
    F9 = Special | 0x24,
    /// <summary>Identifies function key F10.</summary>
    F10 = Special | 0x25,
    /// <summary>Identifies function key F11.</summary>
    F11 = Special | 0x26,
    /// <summary>Identifies function key F12.</summary>
    F12 = Special | 0x27,
    /// <summary>Identifies function key F13.</summary>
    F13 = Special | 0x28,
    /// <summary>Identifies function key F14.</summary>
    F14 = Special | 0x29,
    /// <summary>Identifies function key F15.</summary>
    F15 = Special | 0x2A,
    /// <summary>Identifies function key F16.</summary>
    F16 = Special | 0x2B,
    /// <summary>Identifies function key F17.</summary>
    F17 = Special | 0x2C,
    /// <summary>Identifies function key F18.</summary>
    F18 = Special | 0x2D,
    /// <summary>Identifies function key F19.</summary>
    F19 = Special | 0x2E,
    /// <summary>Identifies function key F20.</summary>
    F20 = Special | 0x2F,
    /// <summary>Identifies function key F21.</summary>
    F21 = Special | 0x30,
    /// <summary>Identifies function key F22.</summary>
    F22 = Special | 0x31,
    /// <summary>Identifies function key F23.</summary>
    F23 = Special | 0x32,
    /// <summary>Identifies function key F24.</summary>
    F24 = Special | 0x33,
    /// <summary>Identifies function key F25.</summary>
    F25 = Special | 0x34,
    /// <summary>Identifies function key F26.</summary>
    F26 = Special | 0x35,
    /// <summary>Identifies function key F27.</summary>
    F27 = Special | 0x36,
    /// <summary>Identifies function key F28.</summary>
    F28 = Special | 0x37,
    /// <summary>Identifies function key F29.</summary>
    F29 = Special | 0x38,
    /// <summary>Identifies function key F30.</summary>
    F30 = Special | 0x39,
    /// <summary>Identifies function key F31.</summary>
    F31 = Special | 0x3A,
    /// <summary>Identifies function key F32.</summary>
    F32 = Special | 0x3B,
    /// <summary>Identifies function key F33.</summary>
    F33 = Special | 0x3C,
    /// <summary>Identifies function key F34.</summary>
    F34 = Special | 0x3D,
    /// <summary>Identifies function key F35.</summary>
    F35 = Special | 0x3E,
    /// <summary>Identifies the context-menu key.</summary>
    Menu = Special | 0x42,
    /// <summary>Identifies Hyper.</summary>
    Hyper = Special | 0x43,
    /// <summary>Identifies Help.</summary>
    Help = Special | 0x45,
    /// <summary>Identifies browser Back.</summary>
    Back = Special | 0x48,
    /// <summary>Identifies browser Forward.</summary>
    Forward = Special | 0x49,
    /// <summary>Identifies browser Stop.</summary>
    Stop = Special | 0x4A,
    /// <summary>Identifies browser Refresh.</summary>
    Refresh = Special | 0x4B,
    /// <summary>Identifies volume down.</summary>
    VolumeDown = Special | 0x4C,
    /// <summary>Identifies volume mute.</summary>
    VolumeMute = Special | 0x4D,
    /// <summary>Identifies volume up.</summary>
    VolumeUp = Special | 0x4E,
    /// <summary>Identifies media play.</summary>
    MediaPlay = Special | 0x54,
    /// <summary>Identifies media stop.</summary>
    MediaStop = Special | 0x55,
    /// <summary>Identifies previous media.</summary>
    MediaPrevious = Special | 0x56,
    /// <summary>Identifies next media.</summary>
    MediaNext = Special | 0x57,
    /// <summary>Identifies media record.</summary>
    MediaRecord = Special | 0x58,
    /// <summary>Identifies the home-page key.</summary>
    HomePage = Special | 0x59,
    /// <summary>Identifies favorites.</summary>
    Favorites = Special | 0x5A,
    /// <summary>Identifies search.</summary>
    Search = Special | 0x5B,
    /// <summary>Identifies standby.</summary>
    Standby = Special | 0x5C,
    /// <summary>Identifies open URL.</summary>
    OpenUrl = Special | 0x5D,
    /// <summary>Identifies launch mail.</summary>
    LaunchMail = Special | 0x5E,
    /// <summary>Identifies launch media.</summary>
    LaunchMedia = Special | 0x5F,
    /// <summary>Identifies application-launch key zero.</summary>
    Launch0 = Special | 0x60,
    /// <summary>Identifies application-launch key one.</summary>
    Launch1 = Special | 0x61,
    /// <summary>Identifies application-launch key two.</summary>
    Launch2 = Special | 0x62,
    /// <summary>Identifies application-launch key three.</summary>
    Launch3 = Special | 0x63,
    /// <summary>Identifies application-launch key four.</summary>
    Launch4 = Special | 0x64,
    /// <summary>Identifies application-launch key five.</summary>
    Launch5 = Special | 0x65,
    /// <summary>Identifies application-launch key six.</summary>
    Launch6 = Special | 0x66,
    /// <summary>Identifies application-launch key seven.</summary>
    Launch7 = Special | 0x67,
    /// <summary>Identifies application-launch key eight.</summary>
    Launch8 = Special | 0x68,
    /// <summary>Identifies application-launch key nine.</summary>
    Launch9 = Special | 0x69,
    /// <summary>Identifies application-launch key A.</summary>
    LaunchA = Special | 0x6A,
    /// <summary>Identifies application-launch key B.</summary>
    LaunchB = Special | 0x6B,
    /// <summary>Identifies application-launch key C.</summary>
    LaunchC = Special | 0x6C,
    /// <summary>Identifies application-launch key D.</summary>
    LaunchD = Special | 0x6D,
    /// <summary>Identifies application-launch key E.</summary>
    LaunchE = Special | 0x6E,
    /// <summary>Identifies application-launch key F.</summary>
    LaunchF = Special | 0x6F,
    /// <summary>Identifies the globe key.</summary>
    Globe = Special | 0x70,
    /// <summary>Identifies the on-screen-keyboard key.</summary>
    Keyboard = Special | 0x71,
    /// <summary>Identifies the Japanese alphanumeric key.</summary>
    JisEisu = Special | 0x72,
    /// <summary>Identifies the Japanese kana key.</summary>
    JisKana = Special | 0x73,
    /// <summary>Identifies numeric-keypad multiplication.</summary>
    KeypadMultiply = Special | 0x81,
    /// <summary>Identifies numeric-keypad division.</summary>
    KeypadDivide = Special | 0x82,
    /// <summary>Identifies numeric-keypad subtraction.</summary>
    KeypadSubtract = Special | 0x83,
    /// <summary>Identifies the numeric-keypad decimal separator.</summary>
    KeypadPeriod = Special | 0x84,
    /// <summary>Identifies numeric-keypad addition.</summary>
    KeypadAdd = Special | 0x85,
    /// <summary>Identifies numeric-keypad zero.</summary>
    Keypad0 = Special | 0x86,
    /// <summary>Identifies numeric-keypad one.</summary>
    Keypad1 = Special | 0x87,
    /// <summary>Identifies numeric-keypad two.</summary>
    Keypad2 = Special | 0x88,
    /// <summary>Identifies numeric-keypad three.</summary>
    Keypad3 = Special | 0x89,
    /// <summary>Identifies numeric-keypad four.</summary>
    Keypad4 = Special | 0x8A,
    /// <summary>Identifies numeric-keypad five.</summary>
    Keypad5 = Special | 0x8B,
    /// <summary>Identifies numeric-keypad six.</summary>
    Keypad6 = Special | 0x8C,
    /// <summary>Identifies numeric-keypad seven.</summary>
    Keypad7 = Special | 0x8D,
    /// <summary>Identifies numeric-keypad eight.</summary>
    Keypad8 = Special | 0x8E,
    /// <summary>Identifies numeric-keypad nine.</summary>
    Keypad9 = Special | 0x8F,
    /// <summary>Identifies an unknown key.</summary>
    Unknown = Special | 0x7FFFFF,
    /// <summary>Identifies Space.</summary>
    Space = 0x0020,
    /// <summary>Identifies <c>!</c>.</summary>
    Exclamation = 0x0021,
    /// <summary>Identifies a double quote.</summary>
    QuoteDouble = 0x0022,
    /// <summary>Identifies <c>#</c>.</summary>
    NumberSign = 0x0023,
    /// <summary>Identifies <c>$</c>.</summary>
    Dollar = 0x0024,
    /// <summary>Identifies <c>%</c>.</summary>
    Percent = 0x0025,
    /// <summary>Identifies <c>&amp;</c>.</summary>
    Ampersand = 0x0026,
    /// <summary>Identifies an apostrophe.</summary>
    Apostrophe = 0x0027,
    /// <summary>Identifies <c>(</c>.</summary>
    ParenthesisLeft = 0x0028,
    /// <summary>Identifies <c>)</c>.</summary>
    ParenthesisRight = 0x0029,
    /// <summary>Identifies <c>*</c>.</summary>
    Asterisk = 0x002A,
    /// <summary>Identifies <c>+</c>.</summary>
    Plus = 0x002B,
    /// <summary>Identifies <c>,</c>.</summary>
    Comma = 0x002C,
    /// <summary>Identifies <c>-</c>.</summary>
    Minus = 0x002D,
    /// <summary>Identifies <c>.</c>.</summary>
    Period = 0x002E,
    /// <summary>Identifies <c>/</c>.</summary>
    Slash = 0x002F,
    /// <summary>Identifies digit zero.</summary>
    Key0 = 0x0030,
    /// <summary>Identifies digit one.</summary>
    Key1 = 0x0031,
    /// <summary>Identifies digit two.</summary>
    Key2 = 0x0032,
    /// <summary>Identifies digit three.</summary>
    Key3 = 0x0033,
    /// <summary>Identifies digit four.</summary>
    Key4 = 0x0034,
    /// <summary>Identifies digit five.</summary>
    Key5 = 0x0035,
    /// <summary>Identifies digit six.</summary>
    Key6 = 0x0036,
    /// <summary>Identifies digit seven.</summary>
    Key7 = 0x0037,
    /// <summary>Identifies digit eight.</summary>
    Key8 = 0x0038,
    /// <summary>Identifies digit nine.</summary>
    Key9 = 0x0039,
    /// <summary>Identifies <c>:</c>.</summary>
    Colon = 0x003A,
    /// <summary>Identifies <c>;</c>.</summary>
    Semicolon = 0x003B,
    /// <summary>Identifies <c>&lt;</c>.</summary>
    Less = 0x003C,
    /// <summary>Identifies <c>=</c>.</summary>
    Equal = 0x003D,
    /// <summary>Identifies <c>&gt;</c>.</summary>
    Greater = 0x003E,
    /// <summary>Identifies <c>?</c>.</summary>
    Question = 0x003F,
    /// <summary>Identifies <c>@</c>.</summary>
    At = 0x0040,
    /// <summary>Identifies Latin A.</summary>
    A = 0x0041,
    /// <summary>Identifies Latin B.</summary>
    B = 0x0042,
    /// <summary>Identifies Latin C.</summary>
    C = 0x0043,
    /// <summary>Identifies Latin D.</summary>
    D = 0x0044,
    /// <summary>Identifies Latin E.</summary>
    E = 0x0045,
    /// <summary>Identifies Latin F.</summary>
    F = 0x0046,
    /// <summary>Identifies Latin G.</summary>
    G = 0x0047,
    /// <summary>Identifies Latin H.</summary>
    H = 0x0048,
    /// <summary>Identifies Latin I.</summary>
    I = 0x0049,
    /// <summary>Identifies Latin J.</summary>
    J = 0x004A,
    /// <summary>Identifies Latin K.</summary>
    K = 0x004B,
    /// <summary>Identifies Latin L.</summary>
    L = 0x004C,
    /// <summary>Identifies Latin M.</summary>
    M = 0x004D,
    /// <summary>Identifies Latin N.</summary>
    N = 0x004E,
    /// <summary>Identifies Latin O.</summary>
    O = 0x004F,
    /// <summary>Identifies Latin P.</summary>
    P = 0x0050,
    /// <summary>Identifies Latin Q.</summary>
    Q = 0x0051,
    /// <summary>Identifies Latin R.</summary>
    R = 0x0052,
    /// <summary>Identifies Latin S.</summary>
    S = 0x0053,
    /// <summary>Identifies Latin T.</summary>
    T = 0x0054,
    /// <summary>Identifies Latin U.</summary>
    U = 0x0055,
    /// <summary>Identifies Latin V.</summary>
    V = 0x0056,
    /// <summary>Identifies Latin W.</summary>
    W = 0x0057,
    /// <summary>Identifies Latin X.</summary>
    X = 0x0058,
    /// <summary>Identifies Latin Y.</summary>
    Y = 0x0059,
    /// <summary>Identifies Latin Z.</summary>
    Z = 0x005A,
    /// <summary>Identifies <c>[</c>.</summary>
    BracketLeft = 0x005B,
    /// <summary>Identifies a backslash.</summary>
    Backslash = 0x005C,
    /// <summary>Identifies <c>]</c>.</summary>
    BracketRight = 0x005D,
    /// <summary>Identifies <c>^</c>.</summary>
    AsciiCircumflex = 0x005E,
    /// <summary>Identifies <c>_</c>.</summary>
    Underscore = 0x005F,
    /// <summary>Identifies a grave accent.</summary>
    QuoteLeft = 0x0060,
    /// <summary>Identifies <c>{</c>.</summary>
    BraceLeft = 0x007B,
    /// <summary>Identifies <c>|</c>.</summary>
    Bar = 0x007C,
    /// <summary>Identifies <c>}</c>.</summary>
    BraceRight = 0x007D,
    /// <summary>Identifies <c>~</c>.</summary>
    AsciiTilde = 0x007E,
    /// <summary>Identifies the yen sign.</summary>
    Yen = 0x00A5,
    /// <summary>Identifies the section sign.</summary>
    Section = 0x00A7,
}

/// <summary>Defines modifier bits that can be combined with a <see cref="Key"/> value.</summary>
[Flags]
public enum KeyModifierMask
{
    /// <summary>Contains every non-modifier key-code bit.</summary>
    CodeMask = (1 << 23) - 1,
    /// <summary>Contains every modifier bit.</summary>
    ModifierMask = 0x7F << 24,
    /// <summary>Requests Command on macOS and Control on other platforms.</summary>
    CommandOrControl = 1 << 24,
    /// <summary>Identifies Shift.</summary>
    Shift = 1 << 25,
    /// <summary>Identifies Alt or Option.</summary>
    Alt = 1 << 26,
    /// <summary>Identifies Meta, Command, Windows, or Super.</summary>
    Meta = 1 << 27,
    /// <summary>Identifies Control.</summary>
    Control = 1 << 28,
    /// <summary>Identifies a numeric-keypad key.</summary>
    Keypad = 1 << 29,
    /// <summary>Identifies a group-layout switch.</summary>
    GroupSwitch = 1 << 30,
}

/// <summary>Identifies the physical side of a duplicated keyboard key.</summary>
public enum KeyLocation
{
    /// <summary>The key is not side-specific.</summary>
    Unspecified = 0,
    /// <summary>The key is on the left side.</summary>
    Left = 1,
    /// <summary>The key is on the right side.</summary>
    Right = 2,
}

/// <summary>Identifies mouse buttons and wheel directions.</summary>
public enum MouseButton
{
    /// <summary>Identifies no button.</summary>
    None = 0,
    /// <summary>Identifies the primary left button.</summary>
    Left = 1,
    /// <summary>Identifies the secondary right button.</summary>
    Right = 2,
    /// <summary>Identifies the middle button.</summary>
    Middle = 3,
    /// <summary>Identifies an upward wheel step.</summary>
    WheelUp = 4,
    /// <summary>Identifies a downward wheel step.</summary>
    WheelDown = 5,
    /// <summary>Identifies a leftward wheel step.</summary>
    WheelLeft = 6,
    /// <summary>Identifies a rightward wheel step.</summary>
    WheelRight = 7,
    /// <summary>Identifies the first auxiliary thumb button.</summary>
    XButton1 = 8,
    /// <summary>Identifies the second auxiliary thumb button.</summary>
    XButton2 = 9,
}

/// <summary>Defines simultaneously held mouse-button bits.</summary>
[Flags]
public enum MouseButtonMask
{
    /// <summary>No mouse button is held.</summary>
    None = 0,
    /// <summary>The left button is held.</summary>
    Left = 1 << 0,
    /// <summary>The right button is held.</summary>
    Right = 1 << 1,
    /// <summary>The middle button is held.</summary>
    Middle = 1 << 2,
    /// <summary>The first auxiliary thumb button is held.</summary>
    XButton1 = 1 << 7,
    /// <summary>The second auxiliary thumb button is held.</summary>
    XButton2 = 1 << 8,
}

/// <summary>Identifies standardized game-controller axes.</summary>
public enum JoyAxis
{
    /// <summary>Identifies an invalid axis.</summary>
    Invalid = -1,
    /// <summary>Identifies the left stick horizontal axis.</summary>
    LeftX = 0,
    /// <summary>Identifies the left stick vertical axis.</summary>
    LeftY = 1,
    /// <summary>Identifies the right stick horizontal axis.</summary>
    RightX = 2,
    /// <summary>Identifies the right stick vertical axis.</summary>
    RightY = 3,
    /// <summary>Identifies the left trigger axis.</summary>
    TriggerLeft = 4,
    /// <summary>Identifies the right trigger axis.</summary>
    TriggerRight = 5,
    /// <summary>Marks the count of standardized SDL controller axes.</summary>
    SdlMax = 6,
    /// <summary>Marks the conventional raw axis count and an accepted event-axis sentinel.</summary>
    Max = 10,
}

/// <summary>Identifies standardized and extended game-controller buttons.</summary>
public enum JoyButton
{
    /// <summary>Identifies an invalid button.</summary>
    Invalid = -1,
    /// <summary>Identifies the bottom face button.</summary>
    A = 0,
    /// <summary>Identifies the right face button.</summary>
    B = 1,
    /// <summary>Identifies the left face button.</summary>
    X = 2,
    /// <summary>Identifies the top face button.</summary>
    Y = 3,
    /// <summary>Identifies Back or Select.</summary>
    Back = 4,
    /// <summary>Identifies the guide or home button.</summary>
    Guide = 5,
    /// <summary>Identifies Start.</summary>
    Start = 6,
    /// <summary>Identifies the left-stick button.</summary>
    LeftStick = 7,
    /// <summary>Identifies the right-stick button.</summary>
    RightStick = 8,
    /// <summary>Identifies the left shoulder button.</summary>
    LeftShoulder = 9,
    /// <summary>Identifies the right shoulder button.</summary>
    RightShoulder = 10,
    /// <summary>Identifies D-pad up.</summary>
    DpadUp = 11,
    /// <summary>Identifies D-pad down.</summary>
    DpadDown = 12,
    /// <summary>Identifies D-pad left.</summary>
    DpadLeft = 13,
    /// <summary>Identifies D-pad right.</summary>
    DpadRight = 14,
    /// <summary>Identifies the first miscellaneous controller button.</summary>
    Misc1 = 15,
    /// <summary>Identifies paddle one.</summary>
    Paddle1 = 16,
    /// <summary>Identifies paddle two.</summary>
    Paddle2 = 17,
    /// <summary>Identifies paddle three.</summary>
    Paddle3 = 18,
    /// <summary>Identifies paddle four.</summary>
    Paddle4 = 19,
    /// <summary>Identifies the touchpad button.</summary>
    Touchpad = 20,
    /// <summary>Identifies the second miscellaneous controller button.</summary>
    Misc2 = 21,
    /// <summary>Identifies the third miscellaneous controller button.</summary>
    Misc3 = 22,
    /// <summary>Identifies the fourth miscellaneous controller button.</summary>
    Misc4 = 23,
    /// <summary>Identifies the fifth miscellaneous controller button.</summary>
    Misc5 = 24,
    /// <summary>Identifies the sixth miscellaneous controller button.</summary>
    Misc6 = 25,
    /// <summary>Marks the count of standardized SDL controller buttons.</summary>
    SdlMax = 26,
    /// <summary>Marks the conventional raw button count; event values can exceed it.</summary>
    Max = 128,
}
