using Electron2D;

internal static class DisplayServerClipboardTests
{
    public static void Run()
    {
        using var display = DisplayServer.Open("Clipboard contract", new Vector2i(160, 120), hidden: true);

        display.ClipboardSet(string.Empty);
        Check(!display.ClipboardHas() && display.ClipboardGet() == string.Empty,
            "An empty clipboard reports no text.");

        const string clipboard = "Clipboard Ω 世界";
        const string primary = "Primary λ 日本";
        display.ClipboardSet(clipboard);
        Check(display.ClipboardHas() && display.ClipboardGet() == clipboard,
            "Nonempty UTF-8 clipboard text round-trips.");

        display.ClipboardSetPrimary(primary);
        Check(display.ClipboardGetPrimary() == primary && display.ClipboardGet() == clipboard,
            "The primary selection is independent of the clipboard.");

        display.ClipboardSetPrimary(string.Empty);
        Check(display.ClipboardGetPrimary() == string.Empty && display.ClipboardGet() == clipboard,
            "Clearing the primary selection leaves the clipboard unchanged.");

        Expect<ArgumentNullException>(() => display.ClipboardSet(null!),
            "Null clipboard text is rejected.");
        Expect<ArgumentNullException>(() => display.ClipboardSetPrimary(null!),
            "Null primary-selection text is rejected.");
        Expect<InvalidOperationException>(() => Task.Run(display.ClipboardGet).GetAwaiter().GetResult(),
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
