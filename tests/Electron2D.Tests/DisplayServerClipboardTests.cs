using Electron2D;

internal static class DisplayServerClipboardTests
{
    public static void Run()
    {
        using var display = DisplayServer.Open("Clipboard contract", new Vector2i(160, 120), hidden: true);

        DisplayServer.ClipboardSet(string.Empty);
        Check(!DisplayServer.ClipboardHas() && DisplayServer.ClipboardGet() == string.Empty,
            "An empty clipboard reports no text.");

        const string clipboard = "Clipboard Ω 世界";
        const string primary = "Primary λ 日本";
        DisplayServer.ClipboardSet(clipboard);
        Check(DisplayServer.ClipboardHas() && DisplayServer.ClipboardGet() == clipboard,
            "Nonempty UTF-8 clipboard text round-trips.");

        DisplayServer.ClipboardSetPrimary(primary);
        Check(DisplayServer.ClipboardGetPrimary() == primary && DisplayServer.ClipboardGet() == clipboard,
            "The primary selection is independent of the clipboard.");

        DisplayServer.ClipboardSetPrimary(string.Empty);
        Check(DisplayServer.ClipboardGetPrimary() == string.Empty && DisplayServer.ClipboardGet() == clipboard,
            "Clearing the primary selection leaves the clipboard unchanged.");

        Expect<ArgumentNullException>(() => DisplayServer.ClipboardSet(null!),
            "Null clipboard text is rejected.");
        Expect<ArgumentNullException>(() => DisplayServer.ClipboardSetPrimary(null!),
            "Null primary-selection text is rejected.");
        Expect<InvalidOperationException>(() => Task.Run(DisplayServer.ClipboardGet).GetAwaiter().GetResult(),
            "Clipboard reads are confined to the opening thread.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Expect<T>(Action action, string message) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
