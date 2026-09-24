using System.Reflection;
using Electron2D;

internal static class DisplayServerDialogTests
{
    public static void Run()
    {
        if ((int)DisplayServer.FileDialogMode.OpenFile != 0 ||
            (int)DisplayServer.FileDialogMode.OpenFiles != 1 ||
            (int)DisplayServer.FileDialogMode.OpenDirectory != 2 ||
            (int)DisplayServer.FileDialogMode.OpenAny != 3 ||
            (int)DisplayServer.FileDialogMode.SaveFile != 4)
            throw new Exception("File dialog mode identities changed.");

        using var display = DisplayServer.Open("Dialog validation", new Vector2i(320, 240), hidden: true);
        Action<bool, IReadOnlyList<string>, int> unused = (_, _, _) => { };
        Expect<ArgumentException>(() => display.DialogShow("title", "body", [], _ => { }));
        Expect<ArgumentException>(() => display.DialogShow("title", "body", ["OK", ""], _ => { }));
        Expect<ArgumentNullException>(() => display.DialogShow("title", "body", ["OK"], null!));
        Expect<NotSupportedException>(() => display.FileDialogShow("", "", "", false,
            DisplayServer.FileDialogMode.OpenAny, [], unused));
        Expect<ArgumentException>(() => display.FileDialogShow("", "", "", true,
            DisplayServer.FileDialogMode.OpenFile, ["not-an-extension"], unused));
        Expect<NotSupportedException>(() => display.FileDialogShow("", "", "", false,
            DisplayServer.FileDialogMode.OpenFile, [";Images;image/png"], unused));
        Expect<ArgumentOutOfRangeException>(() => display.FileDialogShow("", "", "", false,
            (DisplayServer.FileDialogMode)99, [], unused));
        Expect<ArgumentOutOfRangeException>(() => display.FileDialogShow("", "", "", false,
            DisplayServer.FileDialogMode.OpenFile, [], unused, 8));
        Expect<ArgumentException>(() => display.FileDialogShow("", "", "", false,
            DisplayServer.FileDialogMode.OpenFile, ["not-an-extension"], unused));
        Expect<InvalidOperationException>(() => Task.Run(() => display.FileDialogShow("", "", "", false,
            DisplayServer.FileDialogMode.OpenAny, [], unused)).GetAwaiter().GetResult());

        // Inject a copied native completion. A real chooser cannot be driven portably by the dummy video driver.
        var serverType = typeof(DisplayServer);
        var completionType = serverType.GetNestedType("DialogCompletion", BindingFlags.NonPublic)!;
        var completionConstructor = completionType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0];
        var queueCompletion = serverType.GetMethod("QueueDialogCompletion", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var pending = serverType.GetField("_pendingNativeDialogs", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var called = false;
        Action<bool, IReadOnlyList<string>, int> callback = (accepted, paths, _) =>
        {
            called = !accepted && paths.Count == 0;
        };
        pending.SetValue(display, 1);
        var completion = completionConstructor.Invoke([callback, Array.Empty<string>(), 0, "injected native failure"]);
        queueCompletion.Invoke(display, [completion]);
        Expect<InvalidOperationException>(display.Dispose);
        Expect<AggregateException>(display.ProcessEvents);
        if (!called)
            throw new Exception("An asynchronous native failure must still complete the typed callback.");
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception($"Expected {typeof(T).Name}.");
    }
}
