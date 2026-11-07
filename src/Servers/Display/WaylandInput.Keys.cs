namespace Electron2D;

internal sealed partial class WaylandInput
{
    private static readonly Dictionary<Key, uint> PhysicalKeyCodes = BuildPhysicalKeyCodes();

    // The Wayland wire uses Linux evdev key numbers. XKB adds eight to those numbers.
    private static Dictionary<Key, uint> BuildPhysicalKeyCodes()
    {
        ReadOnlySpan<Key> standard =
        [
            Key.Escape, Key.Key1, Key.Key2, Key.Key3, Key.Key4, Key.Key5, Key.Key6, Key.Key7, Key.Key8, Key.Key9,
            Key.Key0, Key.Minus, Key.Equal, Key.Backspace, Key.Tab, Key.Q, Key.W, Key.E, Key.R, Key.T,
            Key.Y, Key.U, Key.I, Key.O, Key.P, Key.BraceLeft, Key.BraceRight, Key.Enter, Key.Control, Key.A,
            Key.S, Key.D, Key.F, Key.G, Key.H, Key.J, Key.K, Key.L, Key.Semicolon, Key.Apostrophe,
            Key.Section, Key.Shift, Key.Backslash, Key.Z, Key.X, Key.C, Key.V, Key.B, Key.N, Key.M,
            Key.Comma, Key.Period, Key.Slash, Key.None, Key.KeypadMultiply, Key.Alt, Key.Space, Key.CapsLock,
            Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10,
            Key.NumLock, Key.ScrollLock, Key.Keypad7, Key.Keypad8, Key.Keypad9, Key.KeypadSubtract,
            Key.Keypad4, Key.Keypad5, Key.Keypad6, Key.KeypadAdd, Key.Keypad1, Key.Keypad2, Key.Keypad3,
            Key.Keypad0, Key.KeypadPeriod,
        ];
        var keys = new Dictionary<Key, uint>
        {
            [Key.QuoteLeft] = 86,
            [Key.F11] = 87,
            [Key.F12] = 88,
            [Key.JisEisu] = 94,
            [Key.JisKana] = 93,
            [Key.KeypadEnter] = 96,
            [Key.KeypadDivide] = 98,
            [Key.Print] = 99,
            [Key.Home] = 102,
            [Key.Up] = 103,
            [Key.PageUp] = 104,
            [Key.Left] = 105,
            [Key.Right] = 106,
            [Key.End] = 107,
            [Key.Down] = 108,
            [Key.PageDown] = 109,
            [Key.Insert] = 110,
            [Key.Delete] = 111,
            [Key.VolumeMute] = 113,
            [Key.VolumeDown] = 114,
            [Key.VolumeUp] = 115,
            [Key.Pause] = 119,
            [Key.Meta] = 125,
            [Key.Menu] = 127,
            [Key.Help] = 138,
            [Key.Standby] = 142,
            [Key.LaunchMail] = 155,
            [Key.Favorites] = 156,
            [Key.Back] = 158,
            [Key.Forward] = 159,
            [Key.MediaNext] = 163,
            [Key.MediaPlay] = 164,
            [Key.MediaPrevious] = 165,
            [Key.MediaStop] = 166,
            [Key.MediaRecord] = 167,
            [Key.HomePage] = 172,
            [Key.Refresh] = 173,
            [Key.Search] = 217,
        };
        for (var i = 0; i < standard.Length; i++)
            if (standard[i] != Key.None)
                keys.Add(standard[i], (uint)i + 1);
        for (var i = 0; i < 23; i++)
            keys.Add((Key)((int)Key.F13 + i), (uint)i + 183);
        return keys;
    }

    internal static Key ExtendedFunctionKey(ushort raw) =>
        raw is >= 195 and <= 205 ? (Key)((int)Key.F25 + raw - 195) : Key.None;

    internal static Key MapSymbol(uint symbol, bool label)
    {
        var special = MapSpecialSymbol(symbol);
        if (!label && special != Key.None)
            return special;
        var unicode = SymbolToUTF32(UpperSymbol(symbol));
        if (unicode >= 0x20 && unicode != 0x7f && (label || unicode <= 0x7e || unicode is 0xa5 or 0xa7))
            return (Key)unicode;
        return special;
    }

    private static Key MapSpecialSymbol(uint symbol)
    {
        if (symbol is >= 0xffbe and <= 0xffe0)
            return (Key)((uint)Key.F1 + symbol - 0xffbe);
        if (symbol is >= 0xffb0 and <= 0xffb9)
            return (Key)((uint)Key.Keypad0 + symbol - 0xffb0);

        return symbol switch
        {
            0xff08 => Key.Backspace,
            0xff09 or 0xff89 => Key.Tab,
            0xfe20 => Key.Backtab,
            0xff0b or 0xff9d => Key.Clear,
            0xff0d => Key.Enter,
            0xff13 or 0xff6b => Key.Pause,
            0xff14 => Key.ScrollLock,
            0xff15 => Key.SystemRequest,
            0xff1b => Key.Escape,
            0xff50 or 0xff95 => Key.Home,
            0xff51 or 0xff96 => Key.Left,
            0xff52 or 0xff97 => Key.Up,
            0xff53 or 0xff98 => Key.Right,
            0xff54 or 0xff99 => Key.Down,
            0xff55 or 0xff9a => Key.PageUp,
            0xff56 or 0xff9b => Key.PageDown,
            0xff57 or 0xff9c => Key.End,
            0xff61 => Key.Print,
            0xff63 or 0xff9e => Key.Insert,
            0xff67 => Key.Menu,
            0xff6a => Key.Help,
            0xff7f => Key.NumLock,
            0xff8d => Key.KeypadEnter,
            0xff9f or 0xffff => Key.Delete,
            0xffaa => Key.KeypadMultiply,
            0xffab => Key.KeypadAdd,
            0xffac or 0xffae => Key.KeypadPeriod,
            0xffad => Key.KeypadSubtract,
            0xffaf => Key.KeypadDivide,
            0xffbd => Key.Equal,
            0xffe1 or 0xffe2 => Key.Shift,
            0xffe3 or 0xffe4 => Key.Control,
            0xffe5 => Key.CapsLock,
            0xffe7 or 0xffe8 or 0xffeb or 0xffec => Key.Meta,
            0xffe9 or 0xffea or 0xfe03 => Key.Alt,
            0xff30 => Key.JisEisu,
            0xff2d or 0xff2e => Key.JisKana,
            0x1008ff11 => Key.VolumeDown,
            0x1008ff12 => Key.VolumeMute,
            0x1008ff13 => Key.VolumeUp,
            0x1008ff14 => Key.MediaPlay,
            0x1008ff15 => Key.MediaStop,
            0x1008ff16 => Key.MediaPrevious,
            0x1008ff17 => Key.MediaNext,
            0x1008ff18 => Key.HomePage,
            0x1008ff19 => Key.LaunchMail,
            0x1008ff1b => Key.Search,
            0x1008ff1c => Key.MediaRecord,
            0x1008ff26 => Key.Back,
            0x1008ff27 => Key.Forward,
            0x1008ff28 => Key.Stop,
            0x1008ff29 => Key.Refresh,
            0x1008ff2a => Key.Standby,
            0x1008ff30 => Key.Favorites,
            _ => Key.None,
        };
    }
}
