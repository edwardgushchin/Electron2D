using SDL3;

namespace Electron2D.Inputs;

public enum Scancode
{
    Unknown = SDL.Scancode.Unknown,

    A = SDL.Scancode.A,
    B = SDL.Scancode.B,
    C = SDL.Scancode.C,
    D = SDL.Scancode.D,
    E = SDL.Scancode.E,
    F = SDL.Scancode.F,
    G = SDL.Scancode.G,
    H = SDL.Scancode.H,
    I = SDL.Scancode.I,
    J = SDL.Scancode.J,
    K = SDL.Scancode.K,
    L = SDL.Scancode.L,
    M = SDL.Scancode.M,
    N = SDL.Scancode.N,
    O = SDL.Scancode.O,
    P = SDL.Scancode.P,
    Q = SDL.Scancode.Q,
    R = SDL.Scancode.R,
    S = SDL.Scancode.S,
    T = SDL.Scancode.T,
    U = SDL.Scancode.U,
    V = SDL.Scancode.V,
    W = SDL.Scancode.W,
    X = SDL.Scancode.X,
    Y = SDL.Scancode.Y,
    Z = SDL.Scancode.Z,

    Alpha1 = SDL.Scancode.Alpha1,
    Alpha2 = SDL.Scancode.Alpha2,
    Alpha3 = SDL.Scancode.Alpha3,
    Alpha4 = SDL.Scancode.Alpha4,
    Alpha5 = SDL.Scancode.Alpha5,
    Alpha6 = SDL.Scancode.Alpha6,
    Alpha7 = SDL.Scancode.Alpha7,
    Alpha8 = SDL.Scancode.Alpha8,
    Alpha9 = SDL.Scancode.Alpha9,
    Alpha0 = SDL.Scancode.Alpha0,

    Return = SDL.Scancode.Return,
    Escape = SDL.Scancode.Escape,
    Backspace = SDL.Scancode.Backspace,
    Tab = SDL.Scancode.Tab,
    Space = SDL.Scancode.Space,

    Minus = SDL.Scancode.Minus,
    Equals = SDL.Scancode.Equals,
    Leftbracket = SDL.Scancode.Leftbracket,
    Rightbracket = SDL.Scancode.Rightbracket,

    /// <summary>
    /// Located at the lower left of the return
    /// key on ISO keyboards and at the right end
    /// of the QWERTY row on ANSI keyboards.
    /// Produces REVERSE SOLIDUS (backslash) and
    /// VERTICAL LINE in a US layout, REVERSE
    /// SOLIDUS and VERTICAL LINE in a UK Mac
    /// layout, NUMBER SIGN and TILDE in a UK
    /// Windows layout, DOLLAR SIGN and POUND SIGN
    /// in a Swiss German layout, NUMBER SIGN and
    /// APOSTROPHE in a German layout, GRAVE
    /// ACCENT and POUND SIGN in a French Mac
    /// layout, and ASTERISK and MICRO SIGN in a
    /// French Windows layout.
    /// </summary>
    Backslash = SDL.Scancode.Backslash,

    /// <summary>
    /// ISO USB keyboards actually use this code
    /// instead of 49 for the same key, but all
    /// OSes I've seen treat the two codes
    /// identically. So, as an implementor, unless
    /// your keyboard generates both of those
    /// codes and your OS treats them differently,
    /// you should generate BACKSLASH
    /// instead of this code. As a user, you
    /// should not rely on this code because SDL
    /// will never generate it with most (all?)
    /// keyboards.
    /// </summary>
    NonUshash = SDL.Scancode.NonUshash,
    Semicolon = SDL.Scancode.Semicolon,
    Apostrophe = SDL.Scancode.Apostrophe,

    /// <summary>
    /// Located in the top left corner (on both ANSI
    /// and ISO keyboards). Produces GRAVE ACCENT and
    /// TILDE in a US Windows layout and in US and UK
    /// Mac layouts on ANSI keyboards, GRAVE ACCENT
    /// and NOT SIGN in a UK Windows layout, SECTION
    /// SIGN and PLUS-MINUS SIGN in US and UK Mac
    /// layouts on ISO keyboards, SECTION SIGN and
    /// DEGREE SIGN in a Swiss German layout (Mac:
    /// only on ISO keyboards), CIRCUMFLEX ACCENT and
    /// DEGREE SIGN in a German layout (Mac: only on
    /// ISO keyboards), SUPERSCRIPT TWO and TILDE in a
    /// French Windows layout, COMMERCIAL AT and
    /// NUMBER SIGN in a French Mac layout on ISO
    /// keyboards, and LESS-THAN SIGN and GREATER-THAN
    /// SIGN in a Swiss German, German, or French Mac
    /// layout on ANSI keyboards.
    /// </summary>
    Grave = SDL.Scancode.Grave,
    Comma = SDL.Scancode.Comma,
    Period = SDL.Scancode.Period,
    Slash = SDL.Scancode.Slash,

    Capslock = SDL.Scancode.Capslock,

    F1 = SDL.Scancode.F1,
    F2 = SDL.Scancode.F2,
    F3 = SDL.Scancode.F3,
    F4 = SDL.Scancode.F4,
    F5 = SDL.Scancode.F5,
    F6 = SDL.Scancode.F6,
    F7 = SDL.Scancode.F7,
    F8 = SDL.Scancode.F8,
    F9 = SDL.Scancode.F9,
    F10 = SDL.Scancode.F10,
    F11 = SDL.Scancode.F11,
    F12 = SDL.Scancode.F12,

    Printscreen = SDL.Scancode.Printscreen,
    Scrolllock = SDL.Scancode.Scrolllock,
    Pause = SDL.Scancode.Pause,

    /// <summary>
    /// insert on PC, help on some Mac keyboards (but
    /// does send code 73, not 117)
    /// </summary>
    Insert = SDL.Scancode.Insert,
    Home = SDL.Scancode.Home,
    Pageup = SDL.Scancode.Pageup,
    Delete = SDL.Scancode.Delete,
    End = SDL.Scancode.End,
    Pagedown = SDL.Scancode.Pagedown,
    Right = SDL.Scancode.Right,
    Left = SDL.Scancode.Left,
    Down = SDL.Scancode.Down,
    Up = SDL.Scancode.Up,

    /// <summary>
    /// num lock on PC, clear on Mac keyboards
    /// </summary>
    NumLockClear = SDL.Scancode.NumLockClear,
    KpDivide = SDL.Scancode.KpDivide,
    KpMultiply = SDL.Scancode.KpMultiply,
    KpMinus = SDL.Scancode.KpMinus,
    KpPlus = SDL.Scancode.KpPlus,
    KpEnter = SDL.Scancode.KpEnter,
    Kp1 = SDL.Scancode.Kp1,
    Kp2 = SDL.Scancode.Kp2,
    Kp3 = SDL.Scancode.Kp3,
    Kp4 = SDL.Scancode.Kp4,
    Kp5 = SDL.Scancode.Kp5,
    Kp6 = SDL.Scancode.Kp6,
    Kp7 = SDL.Scancode.Kp7,
    Kp8 = SDL.Scancode.Kp8,
    Kp9 = SDL.Scancode.Kp9,
    Kp0 = SDL.Scancode.Kp0,
    KpPeriod = SDL.Scancode.KpPeriod,

    /// <summary>
    /// This is the additional key that ISO
    /// keyboards have over ANSI ones,
    /// located between left shift and Y.
    /// Produces GRAVE ACCENT and TILDE in a
    /// US or UK Mac layout, REVERSE SOLIDUS
    /// (backslash) and VERTICAL LINE in a
    /// US or UK Windows layout, and
    /// LESS-THAN SIGN and GREATER-THAN SIGN
    /// in a Swiss German, German, or French
    /// layout.
    /// </summary>
    NonUsbackslash = SDL.Scancode.NonUsbackslash,

    /// <summary>
    /// windows contextual menu, compose
    /// </summary>
    Application = SDL.Scancode.Application,

    /// <summary>
    /// The USB document says this is a status flag,
    /// not a physical key - but some Mac keyboards
    /// do have a power key.
    /// </summary>
    Power = SDL.Scancode.Power,
    KpEquals = SDL.Scancode.KpEquals,
    F13 = SDL.Scancode.F13,
    F14 = SDL.Scancode.F14,
    F15 = SDL.Scancode.F15,
    F16 = SDL.Scancode.F16,
    F17 = SDL.Scancode.F17,
    F18 = SDL.Scancode.F18,
    F19 = SDL.Scancode.F19,
    F20 = SDL.Scancode.F20,
    F21 = SDL.Scancode.F21,
    F22 = SDL.Scancode.F22,
    F23 = SDL.Scancode.F23,
    F24 = SDL.Scancode.F24,
    Execute = SDL.Scancode.Execute,

    /// <summary>
    /// AL Integrated Help Center
    /// </summary>
    Help = SDL.Scancode.Help,

    /// <summary>
    /// Menu (show menu)
    /// </summary>
    Menu = SDL.Scancode.Menu,
    Select = SDL.Scancode.Select,

    /// <summary>
    /// AC Stop
    /// </summary>
    Stop = SDL.Scancode.Stop,

    /// <summary>
    /// AC Redo/Repeat
    /// </summary>
    Again = SDL.Scancode.Again,

    /// <summary>
    /// AC Undo
    /// </summary>
    Undo = SDL.Scancode.Undo,

    /// <summary>
    /// AC Cut
    /// </summary>
    Cut = SDL.Scancode.Cut,

    /// <summary>
    /// AC Copy
    /// </summary>
    Copy = SDL.Scancode.Copy,

    /// <summary>
    /// AC Paste
    /// </summary>
    Paste = SDL.Scancode.Paste,

    /// <summary>
    /// AC Find
    /// </summary>
    Find = SDL.Scancode.Find,
    Mute = SDL.Scancode.Mute,
    VolumeUp = SDL.Scancode.VolumeUp,
    VolumeDown = SDL.Scancode.VolumeDown,


    /*
     not sure whether there's a reason to enable these
     LOCKINGCAPSLOCK = 130,
     LOCKINGNUMLOCK = 131,
     LOCKINGSCROLLLOCK = 132,
    */

    KpComma = SDL.Scancode.KpComma,
    KpEqualsAs400 = SDL.Scancode.KpEqualsAs400,

    /// <summary>
    /// used on Asian keyboards, see
    /// footnotes in USB doc
    /// </summary>
    International1 = SDL.Scancode.International1,
    International2 = SDL.Scancode.International2,

    /// <summary>
    /// Yen
    /// </summary>
    International3 = SDL.Scancode.International3,
    International4 = SDL.Scancode.International4,
    International5 = SDL.Scancode.International5,
    International6 = SDL.Scancode.International6,
    International7 = SDL.Scancode.International7,
    International8 = SDL.Scancode.International8,
    International9 = SDL.Scancode.International9,

    /// <summary>
    /// Hangul/English toggle
    /// </summary>
    Lang1 = SDL.Scancode.Lang1,

    /// <summary>
    /// Hanja conversion
    /// </summary>
    Lang2 = SDL.Scancode.Lang2,

    /// <summary>
    /// Katakana
    /// </summary>
    Lang3 = SDL.Scancode.Lang3,

    /// <summary>
    /// Hiragana
    /// </summary>
    Lang4 = SDL.Scancode.Lang4,

    /// <summary>
    /// Zenkaku/Hankaku
    /// </summary>
    Lang5 = SDL.Scancode.Lang5,

    /// <summary>
    /// reserved
    /// </summary>
    Lang6 = SDL.Scancode.Lang6,

    /// <summary>
    /// reserved
    /// </summary>
    Lang7 = SDL.Scancode.Lang7,

    /// <summary>
    /// reserved
    /// </summary>
    Lang8 = SDL.Scancode.Lang8,

    /// <summary>
    /// reserved
    /// </summary>
    Lang9 = SDL.Scancode.Lang9,

    /// <summary>
    /// Erase-Eaze
    /// </summary>
    AltErase = SDL.Scancode.AltErase,
    SysReq = SDL.Scancode.SysReq,

    /// <summary>
    /// AC Cancel
    /// </summary>
    Cancel = SDL.Scancode.Cancel,
    Clear = SDL.Scancode.Clear,
    Prior = SDL.Scancode.Prior,
    Return2 = SDL.Scancode.Return2,
    Separator = SDL.Scancode.Separator,
    Out = SDL.Scancode.Out,
    Oper = SDL.Scancode.Oper,
    ClearAgain = SDL.Scancode.ClearAgain,
    CrSel = SDL.Scancode.CrSel,
    ExSel = SDL.Scancode.ExSel,

    Kp00 = SDL.Scancode.Kp00,
    Kp000 = SDL.Scancode.Kp000,
    ThousandsSeparator = SDL.Scancode.ThousandsSeparator,
    DecimalSeparator = SDL.Scancode.DecimalSeparator,
    CurrencyUnit = SDL.Scancode.CurrencyUnit,
    CurrencySubunit = SDL.Scancode.CurrencySubunit,
    KpLeftParen = SDL.Scancode.KpLeftParen,
    KpRightParen = SDL.Scancode.KpRightParen,
    KpLeftBrace = SDL.Scancode.KpLeftBrace,
    KpRightBrace = SDL.Scancode.KpRightBrace,
    KpTab = SDL.Scancode.KpTab,
    KpBackspace = SDL.Scancode.KpBackspace,
    KpA = SDL.Scancode.KpA,
    KpB = SDL.Scancode.KpB,
    KpC = SDL.Scancode.KpC,
    KpD = SDL.Scancode.KpD,
    KpE = SDL.Scancode.KpE,
    KpF = SDL.Scancode.KpF,
    KpXor = SDL.Scancode.KpXor,
    KpPower = SDL.Scancode.KpPower,
    KpPercent = SDL.Scancode.KpPercent,
    KpLess = SDL.Scancode.KpLess,
    KpGreater = SDL.Scancode.KpGreater,
    KpAmpersand = SDL.Scancode.KpAmpersand,
    KpDblAmpersand = SDL.Scancode.KpDblAmpersand,
    KpVerticalBar = SDL.Scancode.KpVerticalBar,
    KpDblVerticalBar = SDL.Scancode.KpDblVerticalBar,
    KpColon = SDL.Scancode.KpColon,
    KpHash = SDL.Scancode.KpHash,
    KpSpace = SDL.Scancode.KpSpace,
    KpAt = SDL.Scancode.KpAt,
    KpExClam = SDL.Scancode.KpExClam,
    KpMemStore = SDL.Scancode.KpMemStore,
    KpMemRecall = SDL.Scancode.KpMemRecall,
    KpMemClear = SDL.Scancode.KpMemClear,
    KpMemAdd = SDL.Scancode.KpMemAdd,
    KpMemSubtract = SDL.Scancode.KpMemSubtract,
    KpMemMultiply = SDL.Scancode.KpMemMultiply,
    KpMemDivide = SDL.Scancode.KpMemDivide,
    KpPlusMinus = SDL.Scancode.KpPlusMinus,
    KpClear = SDL.Scancode.KpClear,
    KpClearEntry = SDL.Scancode.KpClearEntry,
    KpBinary = SDL.Scancode.KpBinary,
    KpOctal = SDL.Scancode.KpOctal,
    KpDecimal = SDL.Scancode.KpDecimal,
    KpHexadecimal = SDL.Scancode.KpHexadecimal,

    LCtrl = SDL.Scancode.LCtrl,
    LShift = SDL.Scancode.LShift,

    /// <summary>
    /// alt, option
    /// </summary>
    LAlt = SDL.Scancode.LAlt,

    /// <summary>
    /// windows, command (apple), meta
    /// </summary>
    LGUI = SDL.Scancode.LGUI,
    RCtrl = SDL.Scancode.RCtrl,
    RShift = SDL.Scancode.RShift,

    /// <summary>
    /// alt gr, option
    /// </summary>
    RAlt = SDL.Scancode.RAlt,

    /// <summary>
    /// windows, command (apple), meta
    /// </summary>
    RGUI = SDL.Scancode.RGUI,

    /// <summary>
    /// I'm not sure if this is really not covered
    /// by any of the above, but since there's a
    /// special SDL_KMOD_MODE for it I'm adding it here
    /// </summary>
    Mode = SDL.Scancode.Mode,

    /// <summary>
    /// Sleep
    /// </summary>
    Sleep = SDL.Scancode.Sleep,

    /// <summary>
    /// Wake
    /// </summary>
    Wake = SDL.Scancode.Wake,

    /// <summary>
    /// Channel Increment
    /// </summary>
    ChannelIncrement = SDL.Scancode.ChannelIncrement,

    /// <summary>
    /// Channel Decrement
    /// </summary>
    ChannelDecrement = SDL.Scancode.ChannelDecrement,

    /// <summary>
    /// Play
    /// </summary>
    MediaPlay = SDL.Scancode.MediaPlay,

    /// <summary>
    /// Pause
    /// </summary>
    MediaPause = SDL.Scancode.MediaPause,

    /// <summary>
    /// Record
    /// </summary>
    MediaRecord = SDL.Scancode.MediaRecord,

    /// <summary>
    /// Fast Forward
    /// </summary>
    MediaFastForward = SDL.Scancode.MediaFastForward,

    /// <summary>
    /// Rewind
    /// </summary>
    MediaRewind = SDL.Scancode.MediaRewind,

    /// <summary>
    /// Next Track
    /// </summary>
    MediaNextTrack = SDL.Scancode.MediaNextTrack,

    /// <summary>
    /// Previous Track
    /// </summary>
    MediaPreviousTrack = SDL.Scancode.MediaPreviousTrack,

    /// <summary>
    /// Stop
    /// </summary>
    MediaStop = SDL.Scancode.MediaStop,

    /// <summary>
    /// Eject
    /// </summary>
    MediaEject = SDL.Scancode.MediaEject,

    /// <summary>
    /// Play / Pause
    /// </summary>
    MediaPlayPause = SDL.Scancode.MediaPlayPause,

    /// <summary>
    /// Media Select
    /// </summary>
    MediaSelect = SDL.Scancode.MediaSelect,

    /// <summary>
    /// AC New
    /// </summary>
    ACNew = SDL.Scancode.ACNew,

    /// <summary>
    /// AC Open
    /// </summary>
    ACOpen = SDL.Scancode.ACOpen,

    /// <summary>
    /// AC Close
    /// </summary>
    ACClose = SDL.Scancode.ACClose,

    /// <summary>
    /// AC Exit
    /// </summary>
    ACExit = SDL.Scancode.ACExit,

    /// <summary>
    /// AC Save
    /// </summary>
    ACSave = SDL.Scancode.ACSave,

    /// <summary>
    /// AC Print
    /// </summary>
    ACPrint = SDL.Scancode.ACPrint,

    /// <summary>
    /// AC Properties
    /// </summary>
    ACProperties = SDL.Scancode.ACProperties,

    /// <summary>
    /// AC Search
    /// </summary>
    ACSearch = SDL.Scancode.ACSearch,

    /// <summary>
    /// AC Home
    /// </summary>
    ACHome = SDL.Scancode.ACHome,

    /// <summary>
    /// AC Back
    /// </summary>
    ACBack = SDL.Scancode.ACBack,

    /// <summary>
    /// AC Forward
    /// </summary>
    ACForward = SDL.Scancode.ACForward,

    /// <summary>
    /// AC Stop
    /// </summary>
    ACStop = SDL.Scancode.ACStop,

    /// <summary>
    /// AC Refresh
    /// </summary>
    ACRefresh = SDL.Scancode.ACRefresh,

    /// <summary>
    /// AC Bookmarks
    /// </summary>
    ACBookmarks = SDL.Scancode.ACBookmarks,


    /// <summary>
    /// Usually situated below the display on phones and
    /// used as a multi-function feature key for selecting
    /// a software defined function shown on the bottom left
    /// of the display.
    /// </summary>
    SoftLeft = SDL.Scancode.SoftLeft,

    /// <summary>
    /// Usually situated below the display on phones and
    /// used as a multi-function feature key for selecting
    /// a software defined function shown on the bottom right
    /// of the display.
    /// </summary>
    SoftRight = SDL.Scancode.SoftRight,

    /// <summary>
    /// Used for accepting phone calls.
    /// </summary>
    Call = SDL.Scancode.Call,

    /// <summary>
    /// Used for rejecting phone calls.
    /// </summary>
    EndCall = SDL.Scancode.EndCall,

    /// <summary>
    /// 400-500 reserved for dynamic keycodes
    /// </summary>
    Reserved = SDL.Scancode.Reserved,

    /// <summary>
    /// not a key, just marks the number of scancodes for array bounds
    /// </summary>
    Count = SDL.Scancode.Count,
}