using SDL3;

namespace Electron2D;

public enum Keycode : uint
{
    /// <summary>
    /// 0
    /// </summary>
    Unknown = SDL.Keycode.Unknown,
    
    /// <summary>
    /// <c>\r</c>
    /// </summary>
    Return = SDL.Keycode.Return,
    
    /// <summary>
    /// <c>\x1B</c>
    /// </summary>
    Escape = SDL.Keycode.Escape,
    
    /// <summary>
    /// <c>\b</c>
    /// </summary>
    Backspace = SDL.Keycode.Backspace,
    
    /// <summary>
    /// <c>\t</c>
    /// </summary>
    Tab = SDL.Keycode.Tab,
    
    /// <summary>
    /// ' '
    /// </summary>
    Space = SDL.Keycode.Space,
    
    /// <summary>
    /// <c>!</c>
    /// </summary>
    Exclaim = SDL.Keycode.Exclaim,
    
    /// <summary>
    /// <c>"</c>
    /// </summary>
    DblApostrophe = SDL.Keycode.DblApostrophe,
    
    /// <summary>
    /// <c>#</c>
    /// </summary>
    Hash = SDL.Keycode.Hash,
    
    /// <summary>
    /// <c>$</c>
    /// </summary>
    Dollar = SDL.Keycode.Dollar,
    
    /// <summary>
    /// <c>%</c>
    /// </summary>
    Percent = SDL.Keycode.Percent,
    
    /// <summary>
    /// <c>&amp;</c>
    /// </summary>
    Ampersand = SDL.Keycode.Ampersand,
    
    /// <summary>
    /// <c>\</c>
    /// </summary>
    Apostrophe = SDL.Keycode.Apostrophe,
    
    /// <summary>
    /// <c>(</c>
    /// </summary>
    LeftParen = SDL.Keycode.LeftParen,
    
    /// <summary>
    /// <c>)</c>
    /// </summary>
    RightParen = SDL.Keycode.RightParen,
    
    /// <summary>
    /// <c>*</c>
    /// </summary>
    Asterisk = SDL.Keycode.Asterisk,
    
    /// <summary>
    /// <c>+</c>
    /// </summary>
    Plus = SDL.Keycode.Plus,
    
    /// <summary>
    /// <c>,</c>
    /// </summary>
    Comma = SDL.Keycode.Comma,
    
    /// <summary>
    /// <c>-</c>
    /// </summary>
    Minus = SDL.Keycode.Minus,
    
    /// <summary>
    /// <c>.</c>
    /// </summary>
    Period = SDL.Keycode.Period,
    
    /// <summary>
    /// <c>/</c>
    /// </summary>
    Slash = SDL.Keycode.Slash,
    
    /// <summary>
    /// <c>0</c>
    /// </summary>
    Alpha0 = SDL.Keycode.Alpha0,
    
    /// <summary>
    /// <c>1</c>
    /// </summary>
    Alpha1 = SDL.Keycode.Alpha1,
    
    /// <summary>
    /// <c>2</c>
    /// </summary>
    Alpha2 = SDL.Keycode.Alpha2,
    
    /// <summary>
    /// <c>3</c>
    /// </summary>
    Alpha3 = SDL.Keycode.Alpha3,
    
    /// <summary>
    /// <c>4</c>
    /// </summary>
    Alpha4 = SDL.Keycode.Alpha4,
    
    /// <summary>
    /// <c>5</c>
    /// </summary>
    Alpha5 = SDL.Keycode.Alpha5,
    
    /// <summary>
    /// <c>6</c>
    /// </summary>
    Alpha6 = SDL.Keycode.Alpha6,
    
    /// <summary>
    /// <c>7</c>
    /// </summary>
    Alpha7 = SDL.Keycode.Alpha7,
    
    /// <summary>
    /// <c>8</c>
    /// </summary>
    Alpha8 = SDL.Keycode.Alpha8,
    
    /// <summary>
    /// <c>9</c>
    /// </summary>
    Alpha9 = SDL.Keycode.Alpha9,
    
    /// <summary>
    /// <c>:</c>
    /// </summary>
    Colon = SDL.Keycode.Colon,
    
    /// <summary>
    /// <c>;</c>
    /// </summary>
    Semicolon = SDL.Keycode.Semicolon,
    
    /// <summary>
    /// <c>&lt;</c>
    /// </summary>
    Less = SDL.Keycode.Less,
    
    /// <summary>
    /// <c>=</c>
    /// </summary>
    Equals = SDL.Keycode.Equals,
    
    /// <summary>
    /// <c>&gt;</c>
    /// </summary>
    Greater = SDL.Keycode.Greater,
    
    /// <summary>
    /// <c>?</c>
    /// </summary>
    Question = SDL.Keycode.Question,
    
    /// <summary>
    /// <c>@</c>
    /// </summary>
    At = SDL.Keycode.At, 
    
    /// <summary>
    /// <c>[</c>
    /// </summary>
    LeftBracket = SDL.Keycode.LeftBracket, 
    
    /// <summary>
    /// <c>\</c>
    /// </summary>
    Backslash = SDL.Keycode.Backslash, 
    
    /// <summary>
    /// <c>]</c>
    /// </summary>
    RightBracket = SDL.Keycode.RightBracket,
    
    /// <summary>
    /// <c>^</c>
    /// </summary>
    Caret = SDL.Keycode.Caret,
    
    /// <summary>
    /// <c>_</c>
    /// </summary>
    Underscore = SDL.Keycode.Underscore, 
    
    /// <summary>
    /// <c>`</c>
    /// </summary>
    Grave = SDL.Keycode.Grave,
    
    /// <summary>
    /// <c>a</c>
    /// </summary>
    A = SDL.Keycode.A,
    
    /// <summary>
    /// <c>b</c>
    /// </summary>
    B = SDL.Keycode.B,
    
    /// <summary>
    /// <c>c</c>
    /// </summary>
    C = SDL.Keycode.C,
    
    /// <summary>
    /// <c>d</c>
    /// </summary>
    D = SDL.Keycode.D,
    
    /// <summary>
    /// <c>e</c>
    /// </summary>
    E = SDL.Keycode.E,
    
    /// <summary>
    /// <c>f</c>
    /// </summary>
    F = SDL.Keycode.F,
    
    /// <summary>
    /// <c>g</c>
    /// </summary>
    G = SDL.Keycode.G,
    
    /// <summary>
    /// <c>h</c>
    /// </summary>
    H = SDL.Keycode.H,
    
    /// <summary>
    /// <c>i</c>
    /// </summary>
    I = SDL.Keycode.I,
    
    /// <summary>
    /// <c>j</c>
    /// </summary>
    J = SDL.Keycode.J,
    
    /// <summary>
    /// <c>k</c>
    /// </summary>
    K = SDL.Keycode.K,
    
    /// <summary>
    /// <c>l</c>
    /// </summary>
    L = SDL.Keycode.L,
    
    /// <summary>
    /// <c>m</c>
    /// </summary>
    M = SDL.Keycode.M,
    
    /// <summary>
    /// <c>n</c>
    /// </summary>
    N = SDL.Keycode.N,
    
    /// <summary>
    /// <c>o</c>
    /// </summary>
    O = SDL.Keycode.O,
    
    /// <summary>
    /// <c>p</c>
    /// </summary>
    P = SDL.Keycode.P,
    
    /// <summary>
    /// <c>q</c>
    /// </summary>
    Q = SDL.Keycode.Q,
    
    /// <summary>
    /// <c>r</c>
    /// </summary>
    R = SDL.Keycode.R,
    
    /// <summary>
    /// <c>s</c>
    /// </summary>
    S = SDL.Keycode.S,
    
    /// <summary>
    /// <c>t</c>
    /// </summary>
    T = SDL.Keycode.T,
    
    /// <summary>
    /// <c>u</c>
    /// </summary>
    U = SDL.Keycode.U,
    
    /// <summary>
    /// <c>v</c>
    /// </summary>
    V = SDL.Keycode.V,
    
    /// <summary>
    /// <c>w</c>
    /// </summary>
    W = SDL.Keycode.W,
    
    /// <summary>
    /// <c>x</c>
    /// </summary>
    X = SDL.Keycode.X,
    
    /// <summary>
    /// <c>y</c>
    /// </summary>
    Y = SDL.Keycode.Y,
    
    /// <summary>
    /// <c>z</c>
    /// </summary>
    Z = SDL.Keycode.Z,
    
    /// <summary>
    /// <c>{</c>
    /// </summary>
    LeftBrace = SDL.Keycode.LeftBrace,
    
    /// <summary>
    /// <c>|</c>
    /// </summary>
    Pipe = SDL.Keycode.Pipe,
    
    /// <summary>
    /// <c>}</c>
    /// </summary>
    RightBrace = SDL.Keycode.RightBrace,
    
    /// <summary>
    /// <c>~</c>
    /// </summary>
    Tilde = SDL.Keycode.Tilde,
    
    /// <summary>
    /// <c>\x7F</c>
    /// </summary>
    Delete = SDL.Keycode.Delete,
    
    /// <summary>
    /// <c>±</c>
    /// </summary>
    PlusMinus = SDL.Keycode.PlusMinus,
    
    /// <summary>
    /// Capslock)
    /// </summary>
    Capslock = SDL.Keycode.Capslock,
    
    /// <summary>
    /// F1
    /// </summary>
    F1 = SDL.Keycode.F1,
    
    /// <summary>
    /// F2
    /// </summary>
    F2 = SDL.Keycode.F2,
    
    /// <summary>
    /// F3
    /// </summary>
    F3 = SDL.Keycode.F3,
    
    /// <summary>
    /// F4
    /// </summary>
    F4 = SDL.Keycode.F4,
    
    /// <summary>
    /// F5
    /// </summary>
    F5 = SDL.Keycode.F5,
    
    /// <summary>
    /// F6
    /// </summary>
    F6 = SDL.Keycode.F6,
    
    /// <summary>
    /// F7
    /// </summary>
    F7 = SDL.Keycode.F7,
    
    /// <summary>
    /// F8
    /// </summary>
    F8 = SDL.Keycode.F8,
    
    /// <summary>
    /// F9
    /// </summary>
    F9 = SDL.Keycode.F9,
    
    /// <summary>
    /// F10
    /// </summary>
    F10 = SDL.Keycode.F10,
    
    /// <summary>
    /// F11
    /// </summary>
    F11 = SDL.Keycode.F11,
    
    /// <summary>
    /// F12
    /// </summary>
    F12 = SDL.Keycode.F12,
    
    /// <summary>
    /// PrintScreen
    /// </summary>
    PrintScreen = SDL.Keycode.PrintScreen,
    
    /// <summary>
    /// ScrollLock
    /// </summary>
    ScrolLlock = SDL.Keycode.ScrolLlock,
    
    /// <summary>
    /// Pause
    /// </summary>
    Pause = SDL.Keycode.Pause,
    
    /// <summary>
    /// Insert
    /// </summary>
    Insert = SDL.Keycode.Insert,
    
    /// <summary>
    /// Home
    /// </summary>
    Home = SDL.Keycode.Home,
    
    /// <summary>
    /// Pageup
    /// </summary>
    Pageup = SDL.Keycode.Pageup,
    
    /// <summary>
    /// End
    /// </summary>
    End = SDL.Keycode.End,
    
    /// <summary>
    /// Pagedown
    /// </summary>
    Pagedown = SDL.Keycode.Pagedown,
    
    /// <summary>
    /// Right
    /// </summary>
    Right = SDL.Keycode.Right,
    
    /// <summary>
    /// Left
    /// </summary>
    Left = SDL.Keycode.Left,
    
    /// <summary>
    /// Down
    /// </summary>
    Down = SDL.Keycode.Down,
    
    /// <summary>
    /// Up
    /// </summary>
    Up = SDL.Keycode.Up,
    
    /// <summary>
    /// NumLockClear
    /// </summary>
    NumLockClear = SDL.Keycode.NumLockClear,
    
    /// <summary>
    /// KpDivide
    /// </summary>
    KpDivide = SDL.Keycode.KpDivide,
    
    /// <summary>
    /// KpMultiply
    /// </summary>
    KpMultiply = SDL.Keycode.KpMultiply,
    
    /// <summary>
    /// KpMinus
    /// </summary>
    KpMinus = SDL.Keycode.KpMinus,
    
    /// <summary>
    /// KpPlus
    /// </summary>
    KpPlus = SDL.Keycode.KpPlus,
    
    /// <summary>
    /// KpEnter
    /// </summary>
    KpEnter = SDL.Keycode.KpEnter,
    
    /// <summary>
    /// Kp1
    /// </summary>
    Kp1 = SDL.Keycode.Kp1,
    
    /// <summary>
    /// Kp2
    /// </summary>
    Kp2 = SDL.Keycode.Kp2,
    
    /// <summary>
    /// Kp3
    /// </summary>
    Kp3 = SDL.Keycode.Kp3,
    
    /// <summary>
    /// Kp4
    /// </summary>
    Kp4 = SDL.Keycode.Kp4,
    
    /// <summary>
    /// Kp5
    /// </summary>
    Kp5 = SDL.Keycode.Kp5,
    
    /// <summary>
    /// Kp6
    /// </summary>
    Kp6 = SDL.Keycode.Kp6,
    
    /// <summary>
    /// Kp7
    /// </summary>
    Kp7 = SDL.Keycode.Kp7,
    
    /// <summary>
    /// Kp8
    /// </summary>
    Kp8 = SDL.Keycode.Kp8,
    
    /// <summary>
    /// Kp9
    /// </summary>
    Kp9 = SDL.Keycode.Kp9,
    
    /// <summary>
    /// Kp0
    /// </summary>
    Kp0 = SDL.Keycode.Kp0,
    
    /// <summary>
    /// KpPeriod
    /// </summary>
    KpPeriod = SDL.Keycode.KpPeriod,
    
    /// <summary>
    /// Application
    /// </summary>
    Application = SDL.Keycode.Application,
    
    /// <summary>
    /// Power
    /// </summary>
    Power = SDL.Keycode.Power,
    
    /// <summary>
    /// KpEquals
    /// </summary>
    KpEquals = SDL.Keycode.KpEquals,
    
    /// <summary>
    /// F13
    /// </summary>
    F13 = SDL.Keycode.F13,
    
    /// <summary>
    /// F14
    /// </summary>
    F14 = SDL.Keycode.F14,
    
    /// <summary>
    /// F15
    /// </summary>
    F15 = SDL.Keycode.F15,
    
    /// <summary>
    /// F16
    /// </summary>
    F16 = SDL.Keycode.F16,
    
    /// <summary>
    /// F17
    /// </summary>
    F17 = SDL.Keycode.F17,
    
    /// <summary>
    /// F18
    /// </summary>
    F18 = SDL.Keycode.F18,
    
    /// <summary>
    /// F19
    /// </summary>
    F19 = SDL.Keycode.F19,
    
    /// <summary>
    /// F20
    /// </summary>
    F20 = SDL.Keycode.F20,
    
    /// <summary>
    /// F21
    /// </summary>
    F21 = SDL.Keycode.F21,
    
    /// <summary>
    /// F22
    /// </summary>
    F22 = SDL.Keycode.F22,
    
    /// <summary>
    /// F23
    /// </summary>
    F23 = SDL.Keycode.F23,
    
    /// <summary>
    /// F24
    /// </summary>
    F24 = SDL.Keycode.F24,
    
    /// <summary>
    /// Execute
    /// </summary>
    Execute = SDL.Keycode.Execute,
    
    /// <summary>
    /// Help
    /// </summary>
    Help = SDL.Keycode.Help,
    
    /// <summary>
    /// Menu
    /// </summary>
    Menu = SDL.Keycode.Menu,
    
    /// <summary>
    /// Select
    /// </summary>
    Select = SDL.Keycode.Select,
    
    /// <summary>
    /// Stop
    /// </summary>
    Stop = SDL.Keycode.Stop,
    
    /// <summary>
    /// Again
    /// </summary>
    Again = SDL.Keycode.Again,
    
    /// <summary>
    /// Undo
    /// </summary>
    Undo = SDL.Keycode.Undo,
    
    /// <summary>
    /// Cut
    /// </summary>
    Cut = SDL.Keycode.Cut,
    
    /// <summary>
    /// Copy
    /// </summary>
    Copy = SDL.Keycode.Copy,
    
    /// <summary>
    /// Paste
    /// </summary>
    Paste = SDL.Keycode.Paste,
    
    /// <summary>
    /// Find
    /// </summary>
    Find = SDL.Keycode.Find,
    
    /// <summary>
    /// Mute
    /// </summary>
    Mute = SDL.Keycode.Mute,
    
    /// <summary>
    /// VolumeUp
    /// </summary>
    VolumeUp = SDL.Keycode.VolumeUp,
    
    /// <summary>
    /// VolumeDown
    /// </summary>
    VolumeDown = SDL.Keycode.VolumeDown,
    
    /// <summary>
    /// KpComma
    /// </summary>
    KpComma = SDL.Keycode.KpComma,
    
    /// <summary>
    /// KpEqualsAs400
    /// </summary>
    KpEqualAas400 = SDL.Keycode.KpEqualAas400,
    
    /// <summary>
    /// AltErase
    /// </summary>
    AltErase = SDL.Keycode.AltErase,
    
    /// <summary>
    /// SysReq
    /// </summary>
    SysReq = SDL.Keycode.SysReq,
    
    /// <summary>
    /// Cancel
    /// </summary>
    Cancel = SDL.Keycode.Cancel,
    
    /// <summary>
    /// Clear
    /// </summary>
    Clear = SDL.Keycode.Clear,
    
    /// <summary>
    /// Prior
    /// </summary>
    Prior = SDL.Keycode.Prior,
    
    /// <summary>
    /// Return2
    /// </summary>
    Return2 = SDL.Keycode.Return2,
    
    /// <summary>
    /// Separator
    /// </summary>
    Separator = SDL.Keycode.Separator,
    
    /// <summary>
    /// Out
    /// </summary>
    Out = SDL.Keycode.Out,
    
    /// <summary>
    /// Oper
    /// </summary>
    Oper = SDL.Keycode.Oper,
    
    /// <summary>
    /// ClearAgain
    /// </summary>
    ClearAgain = SDL.Keycode.ClearAgain,
    
    /// <summary>
    /// CrSel
    /// </summary>
    CrSel = SDL.Keycode.CrSel,
    
    /// <summary>
    /// ExSel
    /// </summary>
    ExSel = SDL.Keycode.ExSel,
    
    /// <summary>
    /// Kp00
    /// </summary>
    Kp00 = SDL.Keycode.Kp00,
    
    /// <summary>
    /// Kp000
    /// </summary>
    Kp000 = SDL.Keycode.Kp000,
    
    /// <summary>
    /// ThousandsSeparator
    /// </summary>
    ThousandsSeparator = SDL.Keycode.ThousandsSeparator,
    
    /// <summary>
    /// DecimalSeparator
    /// </summary>
    DecimalSeparator = SDL.Keycode.DecimalSeparator,
    
    /// <summary>
    /// CurrencyUnit
    /// </summary>
    CurrenCyUnit = SDL.Keycode.CurrenCyUnit,
    
    /// <summary>
    /// CurrencySubunit
    /// </summary>
    CurrenCySubunit = SDL.Keycode.CurrenCySubunit,
    
    /// <summary>
    /// KpLeftParen
    /// </summary>
    KpLeftParen = SDL.Keycode.KpLeftParen,
    
    /// <summary>
    /// KpRightParen
    /// </summary>
    KpRightParen = SDL.Keycode.KpRightParen,
    
    /// <summary>
    /// KpLeftBrace
    /// </summary>
    KpLeftBrace = SDL.Keycode.KpLeftBrace,
    
    /// <summary>
    /// KpRightBrace
    /// </summary>
    KpRightBrace = SDL.Keycode.KpRightBrace,
    
    /// <summary>
    /// KpTab
    /// </summary>
    KpTab = SDL.Keycode.KpTab,
    
    /// <summary>
    /// KpBackspace
    /// </summary>
    KpBackspace = SDL.Keycode.KpBackspace,
    
    /// <summary>
    /// KpA
    /// </summary>
    KpA = SDL.Keycode.KpA,
    
    /// <summary>
    /// KpB
    /// </summary>
    KpB = SDL.Keycode.KpB,
    
    /// <summary>
    /// KpC
    /// </summary>
    KpC = SDL.Keycode.KpC,
    
    /// <summary>
    /// KpD
    /// </summary>
    KpD = SDL.Keycode.KpD,
    
    /// <summary>
    /// KpE
    /// </summary>
    KpE = SDL.Keycode.KpE,
    
    /// <summary>
    /// KpF
    /// </summary>
    KpF = SDL.Keycode.KpF,
    
    /// <summary>
    /// KpXor
    /// </summary>
    KpXor = SDL.Keycode.KpXor,
    
    /// <summary>
    /// KpPower
    /// </summary>
    KpPower = SDL.Keycode.KpPower,
    
    /// <summary>
    /// KpPercent
    /// </summary>
    KpPercent = SDL.Keycode.KpPercent,
    
    /// <summary>
    /// KpLess
    /// </summary>
    KpLess = SDL.Keycode.KpLess,
    
    /// <summary>
    /// KpGreater
    /// </summary>
    KpGreater = SDL.Keycode.KpGreater,
    
    /// <summary>
    /// KpAmpersand
    /// </summary>
    KpAmpersand = SDL.Keycode.KpAmpersand,
    
    /// <summary>
    /// KpDblAmpersand
    /// </summary>
    KpDblAmpersand = SDL.Keycode.KpDblAmpersand,
    
    /// <summary>
    /// KpVerticalBar
    /// </summary>
    KpVerticalBar = SDL.Keycode.KpVerticalBar,
    
    /// <summary>
    /// KpDBLVERTICALBAR
    /// </summary>
    KpDblVerticalBar = SDL.Keycode.KpDblVerticalBar,
    
    /// <summary>
    /// KpDblVerticalBar
    /// </summary>
    KpColon = SDL.Keycode.KpColon,
    
    /// <summary>
    /// KpHash
    /// </summary>
    KpHash = SDL.Keycode.KpHash,
    
    /// <summary>
    /// KpSpace
    /// </summary>
    KpSpace = SDL.Keycode.KpSpace,
    
    /// <summary>
    /// KpAt
    /// </summary>
    KpAt = SDL.Keycode.KpAt,
    
    /// <summary>
    /// KpExClam
    /// </summary>
    KpExClam = SDL.Keycode.KpExClam,
    
    /// <summary>
    /// KpMemStore
    /// </summary>
    KpMemStore = SDL.Keycode.KpMemStore,
    
    /// <summary>
    /// KpMemRecall
    /// </summary>
    KpMemRecall = SDL.Keycode.KpMemRecall,
    
    /// <summary>
    /// KpMemClear
    /// </summary>
    KpMemClear = SDL.Keycode.KpMemClear,
    
    /// <summary>
    /// KpMemAdd
    /// </summary>
    KpMemAdd = SDL.Keycode.KpMemAdd,
    
    /// <summary>
    /// KpMemSubtract
    /// </summary>
    KpMemSubtract = SDL.Keycode.KpMemSubtract,
    
    /// <summary>
    /// KpMemMultiply
    /// </summary>
    KpMemMultiply = SDL.Keycode.KpMemMultiply,
    
    /// <summary>
    /// KpMemDivide
    /// </summary>
    KpMemDivide = SDL.Keycode.KpMemDivide,
    
    /// <summary>
    /// KpPlusMinus)
    /// </summary>
    KpPlusMinus = SDL.Keycode.KpPlusMinus,
    
    /// <summary>
    /// KpClear)
    /// </summary>
    KpClear = SDL.Keycode.KpClear,
    
    /// <summary>
    /// KpClearEntry
    /// </summary>
    KpClearEntry = SDL.Keycode.KpClearEntry,
    
    /// <summary>
    /// KpBinary
    /// </summary>
    KpBinary = SDL.Keycode.KpBinary,
    
    /// <summary>
    /// KpOctal
    /// </summary>
    KpOctal = SDL.Keycode.KpOctal,
    
    /// <summary>
    /// KpDecimal
    /// </summary>
    KpDecimal = SDL.Keycode.KpDecimal,
    
    /// <summary>
    /// KpHexadecimal
    /// </summary>
    KpHexadecimal = SDL.Keycode.KpHexadecimal,
    
    /// <summary>
    /// LCtrl
    /// </summary>
    LCtrl = SDL.Keycode.LCtrl,
    
    /// <summary>
    /// LShift
    /// </summary>
    LShift = SDL.Keycode.LShift,
    
    /// <summary>
    /// LAlt
    /// </summary>
    LAlt = SDL.Keycode.LAlt,
    
    /// <summary>
    /// LGUI
    /// </summary>
    LGui = SDL.Keycode.LGui,
    
    /// <summary>
    /// RCtrl
    /// </summary>
    RCtrl = SDL.Keycode.RCtrl,
    
    /// <summary>
    /// RShift
    /// </summary>
    RShift = SDL.Keycode.RShift,
    
    /// <summary>
    /// RAlt
    /// </summary>
    RAlt = SDL.Keycode.RAlt,
    
    /// <summary>
    /// RGui
    /// </summary>
    RGUI = SDL.Keycode.RGUI,
    
    /// <summary>
    /// Mode
    /// </summary>
    Mode = SDL.Keycode.Mode,
    
    /// <summary>
    /// Sleep
    /// </summary>
    Sleep = SDL.Keycode.Sleep,
    
    /// <summary>
    /// Wake
    /// </summary>
    Wake = SDL.Keycode.Wake,
    
    /// <summary>
    /// ChannelIncrement
    /// </summary>
    ChannelIncrement = SDL.Keycode.ChannelIncrement,
    
    /// <summary>
    /// ChannelDecrement
    /// </summary>
    ChannelDecrement = SDL.Keycode.ChannelDecrement,
    
    /// <summary>
    /// MediaPlay
    /// </summary>
    MediaPlay = SDL.Keycode.MediaPlay,
    
    /// <summary>
    /// MediaPause
    /// </summary>
    MediaPause = SDL.Keycode.MediaPause,
    
    /// <summary>
    /// MediaRecord
    /// </summary>
    MediaRecord = SDL.Keycode.MediaRecord,
    
    /// <summary>
    /// MediaFastForward
    /// </summary>
    MediaFastForward = SDL.Keycode.MediaFastForward, 
    
    /// <summary>
    /// MediaRewind
    /// </summary>
    MediaRewind = SDL.Keycode.MediaRewind,
    
    /// <summary>
    /// MediaNextTrack
    /// </summary>
    MediaNextTrack = SDL.Keycode.MediaNextTrack,
    
    /// <summary>
    /// MediaPreviousTrack
    /// </summary>
    MediaPreviousTrack = SDL.Keycode.MediaPreviousTrack,
    
    /// <summary>
    /// MediaStop
    /// </summary>
    MediaStop = SDL.Keycode.MediaStop, 
    
    /// <summary>
    /// MediaEject
    /// </summary>
    MediaEject = SDL.Keycode.MediaEject, 
    
    /// <summary>
    /// MediaPlayPause
    /// </summary>
    MediaPlayPause = SDL.Keycode.MediaPlayPause, 
    
    /// <summary>
    /// MediaSelect
    /// </summary>
    MediaSelect = SDL.Keycode.MediaSelect, 
    
    /// <summary>
    /// AcNew
    /// </summary>
    AcNew = SDL.Keycode.AcNew, 
    
    /// <summary>
    /// AcOpen
    /// </summary>
    AcOpen = SDL.Keycode.AcOpen, 
    
    /// <summary>
    /// AcClose
    /// </summary>
    AcClose = SDL.Keycode.AcClose, 
    
    /// <summary>
    /// AcExit
    /// </summary>
    AcExit = SDL.Keycode.AcExit, 
    
    /// <summary>
    /// AcSave
    /// </summary>
    AcSave = SDL.Keycode.AcSave, 
    
    /// <summary>
    /// AcPrint
    /// </summary>
    AcPrint = SDL.Keycode.AcPrint, 
    
    /// <summary>
    /// AcProperties
    /// </summary>
    AcProperties = SDL.Keycode.AcProperties, 
    
    /// <summary>
    /// AcSearch
    /// </summary>
    AcSearch = SDL.Keycode.AcSearch,
    
    /// <summary>
    /// AcHome
    /// </summary>
    AcHome = SDL.Keycode.AcHome,
    
    /// <summary>
    /// AcBack
    /// </summary>
    AcBack = SDL.Keycode.AcBack, 
    
    /// <summary>
    /// AcForward
    /// </summary>
    AcForward = SDL.Keycode.AcForward, 
    
    /// <summary>
    /// AcStop
    /// </summary>
    AcStop = SDL.Keycode.AcStop,
    
    /// <summary>
    /// AcRefresh
    /// </summary>
    AcRefresh = SDL.Keycode.AcRefresh,
    
    /// <summary>
    /// AcBookmarks
    /// </summary>
    AcBookmarks = SDL.Keycode.AcBookmarks,
    
    /// <summary>
    /// SoftLeft
    /// </summary>
    SoftLeft = SDL.Keycode.SoftLeft,
    
    /// <summary>
    /// SoftRight
    /// </summary>
    SoftRight = SDL.Keycode.SoftRight,
    
    /// <summary>
    /// Call
    /// </summary>
    Call = SDL.Keycode.Call,
    
    /// <summary>
    /// EndCall
    /// </summary>
    EndCall = SDL.Keycode.EndCall,
    
    /// <summary>
    /// Extended key Left Tab
    /// </summary>
    LeftTab = SDL.Keycode.LeftTab,
    
    /// <summary>
    /// Extended key Level 5 Shift
    /// </summary>
    Level5Shift = SDL.Keycode.Level5Shift,
    
    /// <summary>
    /// Extended key Multi-key Compose
    /// </summary>
    MultiKeyCompose = SDL.Keycode.MultiKeyCompose,
    
    /// <summary>
    /// Extended key Left Meta
    /// </summary>
    LMeta = SDL.Keycode.LMeta,
    
    /// <summary>
    /// Extended key Right Meta
    /// </summary>
    RMeta = SDL.Keycode.RMeta,
    
    /// <summary>
    /// Extended key Left Hyper
    /// </summary>
    LHyper = SDL.Keycode.LHyper,
    
    /// <summary>
    /// Extended key Right Hyper
    /// </summary>
    RHyper = SDL.Keycode.RHyper,
}