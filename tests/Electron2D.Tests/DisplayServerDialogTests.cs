using System.Reflection;
using Electron2D;

internal static class DisplayServerDialogTests
{
    public static void Run()
    {
        if ((int)FileDialogMode.OpenFile != 0 ||
            (int)FileDialogMode.OpenFiles != 1 ||
            (int)FileDialogMode.OpenDirectory != 2 ||
            (int)FileDialogMode.OpenAny != 3 ||
            (int)FileDialogMode.SaveFile != 4)
            throw new Exception("File dialog mode identities changed.");

        using var display = DisplayServer.Open("Dialog validation", new Vector2i(320, 240), hidden: true);
        Action<bool, IReadOnlyList<string>, int> unused = (_, _, _) => { };
        Expect<ArgumentException>(() => DisplayServer.DialogShow("title", "body", [], _ => { }));
        Expect<ArgumentException>(() => DisplayServer.DialogShow("title", "body", ["OK", ""], _ => { }));
        Expect<ArgumentNullException>(() => DisplayServer.DialogShow("title", "body", ["OK"], null!));
        Expect<NotSupportedException>(() => DisplayServer.FileDialogShow("", "", "", false,
            FileDialogMode.OpenAny, [], unused));
        Expect<ArgumentException>(() => DisplayServer.FileDialogShow("", "", "", true,
            FileDialogMode.OpenFile, ["not-an-extension"], unused));
        Expect<NotSupportedException>(() => DisplayServer.FileDialogShow("", "", "", false,
            FileDialogMode.OpenFile, [";Images;image/png"], unused));
        Expect<ArgumentOutOfRangeException>(() => DisplayServer.FileDialogShow("", "", "", false,
            (FileDialogMode)99, [], unused));
        Expect<ArgumentOutOfRangeException>(() => DisplayServer.FileDialogShow("", "", "", false,
            FileDialogMode.OpenFile, [], unused, 8));
        Expect<ArgumentException>(() => DisplayServer.FileDialogShow("", "", "", false,
            FileDialogMode.OpenFile, ["not-an-extension"], unused));
        Expect<InvalidOperationException>(() => Task.Run(() => DisplayServer.FileDialogShow("", "", "", false,
            FileDialogMode.OpenAny, [], unused)).GetAwaiter().GetResult());

        VerifyFileDialogNativeLifecycle();

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
        Expect<AggregateException>(DisplayServer.ProcessEvents);
        if (!called)
            throw new Exception("An asynchronous native failure must still complete the typed callback.");
    }

    private static void VerifyFileDialogNativeLifecycle()
    {
        using var temp = DirAccess.CreateTemp("electron2d-native-picker"); var root = new SubViewport { Size = new(800, 600), GUIEmbedSubwindows = true }; var dialog = new FileDialog { Access = FileDialogAccess.FileSystem, CurrentDir = temp.GetCurrentDir(), FileMode = FileDialogMode.OpenAny, UseNativeDialog = true }; root.AddChild(dialog); using var tree = new SceneTree(root);
        Expect<NotSupportedException>(() => dialog.Visible = true); Expect<NotSupportedException>(() => dialog.PopupCentered()); Expect<NotSupportedException>(() => dialog.PopupCenteredRatio(.5f)); Expect<NotSupportedException>(() => dialog.PopupCenteredClamped(new(400, 250))); Expect<NotSupportedException>(() => dialog.Popup(new Rect2i(new(10, 10), new(400, 250)))); Expect<NotSupportedException>(dialog.PopupFileDialog); Check(!dialog.Visible, "Unsupported native mode leaves custom visibility uncommitted across every presentation path.");
        dialog.AddOption("Custom", [], 1); dialog.PopupFileDialog(); Check(dialog.Visible, "Native-file-extra absence selects the executable custom-option fallback."); dialog.Hide(); dialog.OptionCount = 0; dialog.UseNativeDialog = false;
        var type = typeof(FileDialog); var pending = type.GetField("_nativePending", BindingFlags.NonPublic | BindingFlags.Instance)!; var completed = type.GetMethod("NativeCompleted", BindingFlags.NonPublic | BindingFlags.Instance)!.CreateDelegate<Action<FileDialogMode, bool, IReadOnlyList<string>, int, string[]>>(dialog); var canceled = 0; dialog.Canceled += () => canceled++; dialog.CurrentFile = "old.txt"; pending.SetValue(dialog, true); Expect<InvalidOperationException>(dialog.Dispose); completed(FileDialogMode.OpenFile, false, [], -1, []); Check(canceled == 1 && dialog.CurrentFile.Length == 0, "Copied native cancellation clears filename, releases pending ownership and emits cancel.");
        var paths = new[] { System.IO.Path.Combine(temp.GetCurrentDir(), "a.txt"), System.IO.Path.Combine(temp.GetCurrentDir(), "b.txt") }; IReadOnlyList<string>? result = null; dialog.FilesSelected += value => result = value; pending.SetValue(dialog, true); completed(FileDialogMode.OpenFiles, true, paths, -1, []); paths[0] = "mutated"; Check(result != null && result[0].EndsWith("a.txt", StringComparison.Ordinal) && dialog.CurrentFile == "a.txt", "Copied native completion projects current path and owned multi-result snapshot.");
        string? saved = null; dialog.FileSelected += value => saved = value; pending.SetValue(dialog, true); completed(FileDialogMode.SaveFile, true, [System.IO.Path.Combine(temp.GetCurrentDir(), "saved")], 1, ["*.txt;Text", "*.png;Image"]); Check(saved != null && saved.EndsWith("saved.txt", StringComparison.Ordinal), "Native save uses captured filter-index identity and appends the selected extension.");
        var nativeFilters = type.GetMethod("NativeFilters", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [new[] { "*.txt;Text", "*.png;Image" }]) as string[]; Check(nativeFilters != null && nativeFilters.Length == 4 && nativeFilters[0].StartsWith("*.txt,*.png", StringComparison.Ordinal) && nativeFilters[^1] == "*;All Files", "Native synthetic filters preserve custom chooser indices.");
        Console.WriteLine("FileDialog native presentation guards, custom fallback and injected owner-callback lifecycle passed; physical chooser remains unverified.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

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
