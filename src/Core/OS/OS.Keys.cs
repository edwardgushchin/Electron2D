namespace Electron2D;

public sealed partial class OS
{
    /// <summary>Formats a key identity and its embedded modifier flags.</summary>
    /// <param name="code">The key identity, optionally combined with KeyModifierMask flags.</param>
    /// <returns>Platform-specific modifier names followed by the named key or a Unicode character; empty for None.</returns>
    /// <remarks>Does not query the active keyboard layout or translate the names. Invalid Unicode values use the replacement character.</remarks>
    public static string GetKeycodeString(Key code) => Service.GetKeycodeStringCore(code);
    private string GetKeycodeStringCore(Key code) => KeyCodeNames.Format(code);

    /// <summary>Looks up a named key with optional plus-separated modifier names.</summary>
    /// <param name="text">The case-insensitive platform-specific name or modifier combination.</param>
    /// <returns>The known final key with recognized prefix modifiers; unknown final names contribute None.</returns>
    /// <remarks>Unknown prefixes are ignored. Names are not trimmed or localized; unnamed Unicode characters are not parsed.</remarks>
    /// <exception cref="ArgumentNullException">The text is null.</exception>
    public static Key FindKeycodeFromString(string text) => Service.FindKeycodeFromStringCore(text);
    private Key FindKeycodeFromStringCore(string text) => KeyCodeNames.Find(text);

    /// <summary>Classifies whether a key identity is character-capable.</summary>
    /// <param name="code">The unmodified integer key identity.</param>
    /// <returns>False for navigation, editing, modifiers, function and designated system keys; true otherwise, including keypad characters.</returns>
    /// <remarks>This is a key classification, not Unicode scalar validation or a keyboard-layout/text-input query. Modifier bits are not removed.</remarks>
    public static bool IsKeycodeUnicode(int code) => Service.IsKeycodeUnicodeCore(code);
    private bool IsKeycodeUnicodeCore(int code) => KeyCodeNames.HasUnicode(code);
}
