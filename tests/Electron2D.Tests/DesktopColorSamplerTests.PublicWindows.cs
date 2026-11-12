using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;

internal static partial class DesktopColorSamplerTests
{
    internal static void RunWindowsPublicPicker(string[] args) => WithWindowsActor(args, (actor, _) => WindowsPublicPicker(actor));

    private static void WindowsPublicPicker(nint actor)
    {
        var point = ScreenPoint(actor, new(115, 52));
        var red = ScreenPoint(actor, new(32, 52));
        var owner = Environment.CurrentManagedThreadId;
        var calls = new List<(bool Accepted, Color Color)>();
        var nestedStarted = false;
        var refusedCallback = false;
        var closedCallback = false;

        using (var hidden = DisplayServer.Open("Hidden picker test", new(300, 180), hidden: true))
        {
            Check(DisplayServer.HasFeature(DisplayServer.Feature.NativeColorPicker), "The Windows display exposes a picker provider.");
            Check(!DisplayServer.ColorPicker((_, _) => refusedCallback = true), "A hidden display cannot start an interactive picker.");
        }

        using var display = DisplayServer.Open("Windows public color picker", new(420, 250));
        DisplayServer.WindowSetPosition(new(45, 60));
        DisplayServer.WindowSetFlag(WindowFlag.AlwaysOnTop, true);
        DisplayServer.WindowMoveToForeground();
        var window = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
        var client = ScreenPoint(window, new(100, 100));
        Until(() => WindowFromPoint(new(client)) == window, "The test display is visible at its native client point.");
        Click(client, 0x0002, 0x0004);
        Until(() => DisplayServer.WindowIsFocused(), () =>
            $"The test display did not gain focus. Active window is the fixture: {GetForegroundWindow() == window}.");
        Until(() => DisplayServer.ScreenGetPixel(point) == Colors.Green, () =>
            $"The actor pixel at {point} is {DisplayServer.ScreenGetPixel(point)}. Native window at point is the actor: {WindowFromPoint(new(point)) == actor}.");

        DisplayServer.MouseSetMode(MouseMode.Hidden);
        Check(!DisplayServer.ColorPicker((_, _) => refusedCallback = true), "An invisible pointer cannot start a desktop selection.");
        DisplayServer.MouseSetMode(MouseMode.Visible);

        Move(point);
        Check(DisplayServer.ColorPicker((accepted, color) =>
        {
            Record(accepted, color);
            nestedStarted = DisplayServer.ColorPicker(Record);
        }), "Start the Windows picker through the public display API.");
        Check(!DisplayServer.ColorPicker((_, _) => refusedCallback = true), "One foreground selection owns the pointer.");
        var previewPoint = point + new Vector2i(14, 14);
        Until(() =>
        {
            var preview = WindowFromPoint(new(previewPoint));
            return preview != window && GetWindowThreadProcessId(preview, out var pid) != 0 &&
                pid == (uint)Environment.ProcessId && ReadSwatch(preview) == 0x0000FF00;
        }, "The picker presents the external green pixel in its owned native swatch.");
        Check(DisplayServer.WindowIsFocused() && calls.Count == 0, "The native swatch does not activate or complete the request.");

        Click(previewPoint, 0x0002, 0x0004);
        Move(red);
        Until(() => calls.Count == 1, "Deliver the selected color to the owner.");
        Check(calls[0] == (true, Colors.Green) && nestedStarted && Hits(actor) == 0,
            "Selection through the swatch uses the underlying release position, consumes the native pair and retires before reentrant open.");

        Click(red, 0x0008, 0x0010);
        Until(() => calls.Count == 2, "Deliver right-click cancellation.");
        Check(calls[1] == (false, Colors.Black) && Hits(actor) == 0, "Right-click cancels without passing through the native pair.");

        Check(DisplayServer.ColorPicker(Record), "Start a new selection for Escape.");
        keybd_event(0x1b, 0, 0, 0);
        try
        {
            Until(() => calls.Count == 3, "Deliver Escape cancellation.");
            Check(calls[2] == (false, Colors.Black) && !Input.IsPhysicalKeyPressed(Key.Escape), "The picker owns the Escape key press.");
        }
        finally { keybd_event(0x1b, 0, 2, 0); }
        DisplayServer.ProcessEvents();

        Check(DisplayServer.ColorPicker(Record), "Start a new selection for focus loss.");
        DisplayServer.WindowSetMode(WindowMode.Minimized);
        Until(() => calls.Count == 4, "Cancel when the application leaves the foreground.");
        Check(calls[3] == (false, Colors.Black), "Losing focus reports cancellation once.");

        DisplayServer.WindowSetMode(WindowMode.Windowed);
        DisplayServer.WindowMoveToForeground();
        Click(ScreenPoint(window, new(100, 100)), 0x0002, 0x0004);
        Until(() => DisplayServer.WindowIsFocused(), "Restore the display's focus.");

        Check(DisplayServer.ColorPicker((_, _) => throw new InvalidOperationException("Win32 picker callback probe")), "Start a selection with a throwing callback.");
        Click(point, 0x0002, 0x0004);
        try
        {
            Until(() => false, "The event drain reports the callback failure.");
            throw new InvalidOperationException("Expected the picker callback to fail.");
        }
        catch (AggregateException error) when (error.InnerExceptions.Any(e => e.Message == "Win32 picker callback probe")) { }
        Check(DisplayServer.ColorPicker((_, _) => closedCallback = true), "Callback failure retires the picker before reporting the error.");
        display.Dispose();
        Check(!closedCallback && !refusedCallback && calls.Count == 4, "Shutdown retires pending requests without delivery.");
        Click(point, 0x0002, 0x0004);
        Check(SpinWait.SpinUntil(() => Hits(actor) == 2, TimeSpan.FromSeconds(3)), "The foreign window receives a full click after display shutdown.");
        Console.WriteLine("Windows native ColorPicker: foreground guards, external pixels, native preview, three cancellation routes, reentry, callback failure and hook retirement passed.");

        void Record(bool accepted, Color color)
        {
            Check(Environment.CurrentManagedThreadId == owner, "The callback runs on the display owner thread.");
            calls.Add((accepted, color));
        }
    }

    private static uint ReadSwatch(nint window)
    {
        var dc = GetDC(window);
        if (dc == 0) return uint.MaxValue;
        try { return GetPixel(dc, 14, 14); }
        finally { _ = ReleaseDC(window, dc); }
    }

    [LibraryImport(User32)] private static partial nint GetForegroundWindow();
    [LibraryImport(User32)] private static partial nint GetDC(nint window);
    [LibraryImport(User32)] private static partial int ReleaseDC(nint window, nint dc);
    [LibraryImport("gdi32.dll")] private static partial uint GetPixel(nint dc, int x, int y);

    private static void Until(Func<bool> condition, string failure) => Until(condition, () => failure);

    private static void Until(Func<bool> condition, Func<string> failure)
    {
        var timer = Stopwatch.StartNew();
        do
        {
            DisplayServer.ProcessEvents();
            if (condition()) return;
            Thread.Sleep(10);
        } while (timer.Elapsed < TimeSpan.FromSeconds(6));
        throw new InvalidOperationException(failure());
    }
}
