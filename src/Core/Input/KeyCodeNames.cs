using System.Text;

namespace Electron2D;

internal static class KeyCodeNames
{
    private static readonly Dictionary<string, Key> Names = CreateNames();
    private static Dictionary<string, Key> CreateNames()
    {
        var names = new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in Enum.GetValues<Key>())
            if (code is not (Key.None or Key.Special)) names.TryAdd(Format(code), code);
        return names;
    }
    internal static Key Find(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var names = Names.GetAlternateLookup<ReadOnlySpan<char>>();
        var rest = text.AsSpan(); var last = rest.LastIndexOf('+');
        var code = names.TryGetValue(rest[(last + 1)..], out var value) ? value : Key.None;
        while (last >= 0)
        {
            var end = rest.IndexOf('+'); var part = rest[..end];
            if (names.TryGetValue(part, out var modifier))
                code |= (Key)(modifier switch { Key.Shift => KeyModifierMask.Shift, Key.Control => KeyModifierMask.Control, Key.Alt => KeyModifierMask.Alt, Key.Meta => KeyModifierMask.Meta, _ => (KeyModifierMask)0 });
            rest = rest[(end + 1)..]; last = rest.IndexOf('+');
        }
        return code;
    }
    internal static bool HasUnicode(int code) => code is not (
        >= (int)Key.Escape and <= (int)Key.F35
        or (int)Key.Menu
        or (int)Key.Hyper
        or (int)Key.Help
        or >= (int)Key.Back and <= (int)Key.VolumeUp
        or >= (int)Key.MediaPlay and <= (int)Key.JisKana);

    internal static string Format(Key key)
    {
        var raw = (int)key;
        var modifiers = (KeyModifierMask)(raw & (int)KeyModifierMask.ModifierMask);
        var code = (Key)(raw & (int)KeyModifierMask.CodeMask);
        var parts = new List<string>(6);
        if ((modifiers & KeyModifierMask.CommandOrControl) != 0 && !OperatingSystem.IsMacOS()) parts.Add("Ctrl");
        if ((modifiers & KeyModifierMask.Control) != 0) parts.Add("Ctrl");
        if ((modifiers & KeyModifierMask.Alt) != 0) parts.Add(OperatingSystem.IsMacOS() ? "Option" : "Alt");
        if ((modifiers & KeyModifierMask.Shift) != 0) parts.Add("Shift");
        if ((modifiers & KeyModifierMask.CommandOrControl) != 0 && OperatingSystem.IsMacOS()) parts.Add("Command");
        if ((modifiers & KeyModifierMask.Meta) != 0)
            parts.Add(OperatingSystem.IsMacOS() ? "Command" : OperatingSystem.IsWindows() ? "Windows" : "Meta");

        if (code == Key.None)
            return string.Join("+", parts);
        if (code is >= Key.Key0 and <= Key.Key9)
            parts.Add(((char)(int)code).ToString());
        else if (code is >= Key.Keypad0 and <= Key.Keypad9)
            parts.Add($"Kp {(int)code - (int)Key.Keypad0}");
        else
            parts.Add(code switch
            {
                Key.Control => "Ctrl",
                Key.Meta => OperatingSystem.IsMacOS() ? "Command" : OperatingSystem.IsWindows() ? "Windows" : "Meta",
                Key.Alt => OperatingSystem.IsMacOS() ? "Option" : "Alt",
                Key.KeypadEnter => "Kp Enter",
                Key.SystemRequest => "SysReq",
                Key.KeypadMultiply => "Kp Multiply",
                Key.KeypadDivide => "Kp Divide",
                Key.KeypadSubtract => "Kp Subtract",
                Key.KeypadPeriod => "Kp Period",
                Key.KeypadAdd => "Kp Add",
                Key.Standby => "StandBy",
                Key.OpenUrl => "OpenURL",
                Key.Keyboard => "On-screen keyboard",
                Key.JisEisu => "JIS Eisu",
                Key.JisKana => "JIS Kana",
                Key.Exclamation => "Exclam",
                Key.QuoteDouble => "QuoteDbl",
                Key.ParenthesisLeft => "ParenLeft",
                Key.ParenthesisRight => "ParenRight",
                Key.AsciiCircumflex => "AsciiCircum",
                Key.Backslash => "BackSlash",
                Key.Underscore => "UnderScore",
                Key.Special => "\uFFFD",
                _ => Enum.GetName(code) ?? (Rune.IsValid((int)code) ? new Rune((int)code).ToString() : "\uFFFD"),
            });
        return string.Join("+", parts);
    }
}
