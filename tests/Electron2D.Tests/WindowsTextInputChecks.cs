using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Electron2D;

internal static unsafe class WindowsTextInputChecks
{
    private const string Initial = "Исходный 🌙 e\u0301", Edited = "Новый 🌌 e\u0301";
    private const string Prompt = "<plain> & text", Ready = "Electron2D.WindowsTextInputFixture.Ready";
    private static string _actor = "";
    private static int _checks;
    private static (string Input, string OK, string Cancel) _labels = ("Input", "OK", "Cancel");
    [ThreadStatic] private static ModalProbe? _modal;

    internal static void Run(string profile, string actor)
    {
        _actor = actor;
        if (profile is "gpu" or "compatibility")
            Scene(profile);
        else
            Raw(profile == "unavailable");
        Console.WriteLine($"Windows text input: {_checks} checks passed; mode={profile}.");
    }

    private static void Raw(bool unavailable)
    {
        Reject<InvalidOperationException>(() => DisplayServer.DialogInputText("", "", "", _ => { }));
        using var display = DisplayServer.Open("Electron2D text input parent", new(360, 250));
        Check(DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogInput) != unavailable, "Feature follows the actual Windows video driver.");
        Reject<ArgumentNullException>(() => DisplayServer.DialogInputText(null!, "", "", _ => { }));
        Reject<ArgumentNullException>(() => DisplayServer.DialogInputText("", null!, "", _ => { }));
        Reject<ArgumentNullException>(() => DisplayServer.DialogInputText("", "", null!, _ => { }));
        Reject<ArgumentNullException>(() => DisplayServer.DialogInputText("", "", "", null!));
        Reject<ArgumentException>(() => DisplayServer.DialogInputText("title\0", "", "", _ => { }));
        Reject<ArgumentException>(() => DisplayServer.DialogInputText("", "prompt\0", "", _ => { }));
        Reject<ArgumentException>(() => DisplayServer.DialogInputText("", "", "input\0", _ => { }));
        Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.DialogInputText("", "", "", _ => { })).GetAwaiter().GetResult());
        if (unavailable)
        {
            Reject<NotSupportedException>(() => DisplayServer.DialogInputText("", "", "", _ => throw new Exception("Unavailable callback.")));
            return;
        }

        Reject<ArgumentException>(() => DisplayServer.DialogInputText("invalid", "", "bad\ud800text", _ => { }));
        Reject<ArgumentException>(() => DisplayServer.DialogInputText("invalid", "", "two\r\nlines", _ => { }));
        var owner = Environment.CurrentManagedThreadId;
        using var translation = new Translation { Locale = "ru" };
        translation.AddMessage("Input", "Ввод", "native text dialog");
        translation.AddMessage("OK", "Принять", "native text dialog");
        translation.AddMessage("Cancel", "Отмена", "native text dialog");
        TranslationServer.Culture = CultureInfo.GetCultureInfo("ru");
        TranslationServer.AddTranslation(translation);
        _labels = ("Ввод", "Принять", "Отмена");

        PendingFileDialog();
        foreach (var action in new[] { "enter", "accept", "cancel", "escape", "close" })
        {
            var calls = 0;
            var replacement = action == "accept" ? "" : Edited;
            Show(display, action, Initial, Prompt, replacement, text =>
            {
                Check(text == replacement && Environment.CurrentManagedThreadId == owner, "Confirmation copies the exact UTF-16 text on the owner.");
                calls++;
                DisplayServer.ProcessEvents();
            });
            Check(calls == (action is "enter" or "accept" ? 1 : 0), "Exactly one callback only on confirmation.");
        }
        var longInput = new string('x', 70000) + "🚀";
        var longPrompt = Prompt + "\n" + new string('a', 4000) + "\nlast line";
        Show(display, "accept", longInput, longPrompt, "@unchanged", text => Check(text == longInput, "Native edit preserves long UTF-16 input."),
            "@long-input", "@long-description");
        Reject<FormatException>(() => Show(display, "accept", Initial, Prompt, Edited, _ => throw new FormatException("Owned callback failure.")));
        Show(display, "accept", Initial, "", Edited, _ =>
        {
            var nested = 0;
            Show(display, "accept", "", "", Edited, text => { Check(text == Edited, "Callback reentry gets its own native dialog."); nested++; });
            Check(nested == 1, "Reentrant confirmation completes once.");
        });
        Show(display, "accept", Initial, Prompt, Edited, _ => display.Dispose());
        Check(!DisplayServer.IsAvailable, "Callback can dispose the display after modal retirement.");
        TranslationServer.RemoveTranslation(translation);
    }

    private static void PendingFileDialog()
    {
        var title = "Electron2D input overlap " + Guid.NewGuid().ToString("N");
        using var actor = Launch(title, "file-cancel", "", "", "");
        var complete = false;
        DisplayServer.FileDialogShow(title, System.IO.Path.GetTempPath(), "", false, FileDialogMode.OpenFile, [],
            (ok, paths, _) => { Check(!ok && paths.Count == 0, "The separate native chooser cancels."); complete = true; });
        Reject<InvalidOperationException>(() => DisplayServer.DialogInputText("", "", "", _ => { }));
        var deadline = Environment.TickCount64 + 20000;
        while (!complete && Environment.TickCount64 < deadline)
        {
            DisplayServer.ProcessEvents();
            Thread.Sleep(5);
        }
        Finish(actor);
        Check(complete, "File chooser retirement releases the modal admission guard.");
    }

    private static void Scene(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Size = new(320, 240), Title = "Electron2D text input scene" };
        root.AddChild(new ColorRect { Size = new(320, 240), Color = Colors.Red });
        var frames = 0;
        var calls = 0;
        root.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        void Drawn()
        {
            if (++frames == 1)
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested native renderer is active.");
                Show(null, "enter", Initial, Prompt, Edited, text =>
                {
                    Check(text == Edited, "The running scene receives the edited native text.");
                    calls++;
                    Reject<InvalidOperationException>(DisplayServer.ProcessEvents);
                });
            }
            else
            {
                using var image = root.GetTexture().GetImage();
                Check(image != null && image.GetPixel(8, 8) == Colors.Red && calls == 1, "The next rendered frame resumes with the expected pixels.");
                RenderingServer.FramePostDraw -= Drawn;
                root.Tree!.Quit();
            }
        }
        Check(Engine.Run(root) == 0 && frames == 2 && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable,
            "The scene and native services retire normally.");
    }

    private static void Show(DisplayServer? display, string action, string initial, string description, string replacement,
        Action<string> callback, string? probeInitial = null, string? probeDescription = null)
    {
        var title = "Electron2D input fixture " + Guid.NewGuid().ToString("N");
        var parent = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
        using var actor = Launch(title, action, probeInitial ?? initial, probeDescription ?? description, replacement);
        var state = new ModalProbe(title, display, parent);
        var previous = _modal;
        _modal = state;
        var timer = SetTimer(0, 0, 10, &ObserveModal);
        try
        {
            Check(timer != 0, "Installed native owner-thread test timer.");
            DisplayServer.DialogInputText(title, description, initial, text =>
            {
                Check(IsWindow(state.Dialog) == 0 && IsWindowEnabled(parent) != 0, "Callback follows native dialog destruction and parent re-enable.");
                callback(text);
            });
        }
        finally
        {
            if (timer != 0) KillTimer(0, timer);
            _modal = previous;
            if (state.Dialog != 0)
            {
                Finish(actor);
                Check(state.Guards && state.Error == null, "Owner guard probe completed: " + state.Error);
                Check(IsWindow(state.Dialog) == 0, "The native dialog is retired.");
            }
            else if (!actor.HasExited)
                actor.Kill(true);
        }
        Check(state.Dialog != 0, "The native dialog was presented.");
    }

    private sealed class ModalProbe(string title, DisplayServer? display, nint parent)
    {
        internal readonly string Title = title;
        internal readonly DisplayServer? Display = display;
        internal readonly nint Parent = parent;
        internal readonly long Deadline = Environment.TickCount64 + 22000;
        internal nint Dialog;
        internal bool Guards;
        internal Exception? Error;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void ObserveModal(nint window, uint message, nuint timer, uint ticks)
    {
        var state = _modal;
        if (state == null) return;
        var dialog = FindWindowW("#32770", state.Title);
        if (dialog == 0 || GetWindowThreadProcessId(dialog, out var pid) == 0 || pid != Environment.ProcessId)
            return;
        if (Environment.TickCount64 >= state.Deadline)
        {
            state.Error ??= new TimeoutException("The owned dialog did not terminate.");
            PostMessageW(dialog, 0x0010, 0, 0);
            return;
        }
        if (state.Guards || state.Error != null) return;
        state.Dialog = dialog;
        try
        {
            Check(IsWindowEnabled(state.Parent) == 0, "The Win32 modal loop disables the exact parent.");
            if (state.Display != null) Reject<InvalidOperationException>(state.Display.Dispose);
            Reject<InvalidOperationException>(DisplayServer.ProcessEvents);
            Reject<InvalidOperationException>(DisplayServer.ForceProcessAndDropEvents);
            Reject<InvalidOperationException>(() => DisplayServer.DialogInputText("", "", "", _ => { }));
            Reject<InvalidOperationException>(() => DisplayServer.DialogShow("", "", ["OK"], _ => { }));
            Reject<InvalidOperationException>(() => OS.Alert("Unexpected alert."));
            Reject<InvalidOperationException>(() => DisplayServer.FileDialogShow("", "", "", false, FileDialogMode.OpenFile, [], (_, _, _) => { }));
            state.Guards = true;
        }
        catch (Exception error) { state.Error = error; }
        if (SetPropW(dialog, Ready, 1) == 0)
        {
            state.Error ??= new InvalidOperationException("Could not signal the SDK fixture.");
            PostMessageW(dialog, 0x0010, 0, 0);
        }
    }

    private static Process Launch(string title, string action, string initial, string prompt, string replacement)
    {
        var start = new ProcessStartInfo(_actor)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { Environment.ProcessId.ToString(CultureInfo.InvariantCulture), title, action,
            initial, prompt, replacement, _labels.Input, _labels.OK, _labels.Cancel })
            start.ArgumentList.Add(argument);
        return Process.Start(start) ?? throw new InvalidOperationException("Could not start the native SDK fixture.");
    }

    private static void Finish(Process actor)
    {
        if (!actor.WaitForExit(5000))
        {
            actor.Kill(true);
            throw new TimeoutException("The owned SDK fixture timed out.");
        }
        Console.Write(actor.StandardOutput.ReadToEnd());
        var error = actor.StandardError.ReadToEnd();
        Check(actor.ExitCode == 0, "Independent native SDK fixture failed: " + error);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }

    private static void Reject<T>(Action operation) where T : Exception
    {
        try { operation(); }
        catch (T) { _checks++; return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    [DllImport("user32.dll")] private static extern nuint SetTimer(nint window, nuint id, uint interval, delegate* unmanaged[Stdcall]<nint, uint, nuint, uint, void> callback);
    [DllImport("user32.dll")] private static extern int KillTimer(nint window, nuint id);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint FindWindowW(string kind, string title);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out int pid);
    [DllImport("user32.dll")] private static extern int IsWindow(nint window);
    [DllImport("user32.dll")] private static extern int IsWindowEnabled(nint window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int SetPropW(nint window, string name, nint value);
    [DllImport("user32.dll")] private static extern int PostMessageW(nint window, uint message, nuint wParam, nint lParam);
}
