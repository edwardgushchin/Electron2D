using Electron2D;

try { Alerts.Run(args.Length == 0 ? "managed" : args[0]); return 0; }
catch (Exception error) { Console.Error.WriteLine(error); return 1; }

internal static class Alerts
{
    internal static void Run(string profile)
    {
        Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable && Engine.MainLoop is null, "A fresh host has no native or engine service.");
        Reject<ArgumentNullException>(() => OS.Alert(null!));
        Reject<ArgumentNullException>(() => OS.Alert("body", null!));
        Reject<ArgumentException>(() => OS.Alert("prefix\0hidden"));
        Reject<ArgumentException>(() => OS.Alert("body", "prefix\0hidden"));
        if (profile != "native") ValidateEngineOwner();
        if (profile == "managed") { Console.WriteLine("PASS native alert arguments and lifecycle owner; no native dialog requested."); return; }
        if (profile == "failure") { Failure(); return; }
        Check(profile == "native", "Known consumer profile.");
        Ready("before", "Alert!"); OS.Alert("До запуска: русский αβ\nSecond line — native message.");
        Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable && Engine.MainLoop is null, "Pre-startup modal presentation does not publish engine services.");
        using (var loop = new EmptyLoop())
        {
            Engine.Start(loop);
            try
            {
                Reject<InvalidOperationException>(() => Task.Run(() => OS.Alert("wrong owner")).GetAwaiter().GetResult());
                Ready("manual", "E2D manual αβ"); OS.Alert("Manual lifecycle owner", "E2D manual αβ");
            }
            finally { Engine.Stop(); }
        }
        using (var display = DisplayServer.Open("E2D alert parent", new(128, 96), hidden: true))
        {
            ValidateDisplay();
            Reject<InvalidOperationException>(() => Task.Run(() => OS.Alert("wrong display owner")).GetAwaiter().GetResult());
            Ready("owned", "E2D owned αβ"); OS.Alert("Owned native message\nUTF-8: русский αβ", "E2D owned αβ");
            var called = false;
            Ready("callback", "E2D callback");
            DisplayServer.DialogShow("E2D callback", "Close this native dialog", ["Cancel", "Proceed"], selected =>
            {
                Check(selected == -1, $"Native window close preserves the cancellation index; actual={selected}."); called = true;
                Ready("nested", "E2D nested"); OS.Alert("The owner callback can open the next alert.", "E2D nested");
            });
            Check(called && DisplayServer.IsAvailable, "Native callback, nested presentation and display lifetime survive.");
            DisplayServer.ProcessEvents();
        }
        Ready("after", "E2D after"); OS.Alert("After display shutdown", "E2D after");
        Ready("empty", ""); OS.Alert("", "");
        Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable && Engine.MainLoop is null, "All native modal sessions retire without creating a renderer.");
        Console.WriteLine("PASS seven native dialogs: pre-startup, manual owner, display parent, cancellation callback, nested, after shutdown and empty text/title.");
    }

    private static void ValidateEngineOwner()
    {
        using var loop = new EmptyLoop(); Engine.Start(loop);
        try { Reject<InvalidOperationException>(() => Task.Run(() => OS.Alert("wrong owner")).GetAwaiter().GetResult()); }
        finally { Engine.Stop(); }
        Check(!DisplayServer.IsAvailable && Engine.MainLoop is null, "Validation and manual lifecycle retirement do not create a display.");
    }

    private static void Failure()
    {
        for (var i = 0; i < 2; i++) Reject<InvalidOperationException>(() => OS.Alert("headless failure"));
        using (var display = DisplayServer.Open("Headless alert owner", new(64, 64), hidden: true))
        {
            ValidateDisplay();
            Reject<InvalidOperationException>(() => OS.Alert("owned headless failure"));
            DisplayServer.ProcessEvents(); Check(DisplayServer.IsAvailable, "Failed native presentation preserves the active display.");
        }
        Reject<InvalidOperationException>(() => OS.Alert("after failure"));
        Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Failed modal requests leave no service lifetime.");
        Console.WriteLine("PASS explicit native failure before/during/after display; validation, repeated failure and cleanup.");
    }

    private static void ValidateDisplay()
    {
        Reject<ArgumentException>(() => DisplayServer.DialogShow("prefix\0hidden", "body", ["OK"], _ => { }));
        Reject<ArgumentException>(() => DisplayServer.DialogShow("title", "prefix\0hidden", ["OK"], _ => { }));
        for (var i = 0; i < 16; i++) Reject<ArgumentException>(() => DisplayServer.DialogShow("title", "body", ["first marshaled", "prefix\0hidden"], _ => { }));
        Reject<ArgumentException>(() => DisplayServer.DialogShow("title", "body", ["OK", ""], _ => { }));
        Reject<ArgumentNullException>(() => DisplayServer.DialogShow("title", "body", ["OK"], null!));
    }

    private sealed class EmptyLoop : MainLoop { }
    private static void Ready(string phase, string title) { Console.WriteLine($"READY\t{phase}\t{title}"); Console.Out.Flush(); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
