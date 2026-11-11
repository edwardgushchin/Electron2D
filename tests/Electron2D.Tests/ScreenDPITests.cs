using System.Globalization;
using System.Runtime.InteropServices;
using Electron2D;

internal static partial class ScreenDPITests
{
    internal static void Run(string profile, string[] args)
    {
        Reject(() => DisplayServer.ScreenGetDPI());
        using (DisplayServer.Open("Screen DPI test", new(64, 64), hidden: true))
        {
            var count = DisplayServer.GetScreenCount();
            Check(count > 0, "An active display is required.");
            foreach (var invalid in new[] { count, int.MaxValue, int.MinValue })
                Check(DisplayServer.ScreenGetDPI(invalid) == 96, $"Invalid selector {invalid} must fall back to 96 DPI.");
            Reject(() => Task.Run(() => DisplayServer.ScreenGetDPI()).GetAwaiter().GetResult());

            switch (profile)
            {
                case "windows":
                    Check(DisplayServer.GetName() == "Windows", "Use the native Win32 display driver.");
                    foreach (var selector in Enumerable.Range(0, count).Concat(new[]
                    {
                        DisplayServer.ScreenOfMainWindow, DisplayServer.ScreenPrimary,
                        DisplayServer.ScreenWithKeyboardFocus, DisplayServer.ScreenWithMouseFocus,
                    }))
                    {
                        var origin = DisplayServer.ScreenGetPosition(selector);
                        var size = DisplayServer.ScreenGetSize(selector);
                        var monitor = MonitorFromPoint(new Point(origin.X + size.X / 2, origin.Y + size.Y / 2), 0);
                        Check(monitor != 0, $"Monitor selector {selector} resolves to a native monitor.");
                        Check(GetDpiForMonitor(monitor, 0, out var x, out var y) >= 0 && x > 0 && y > 0, "Win32 reports monitor DPI.");
                        var expected = (int)(((ulong)x + y) / 2);
                        Check(DisplayServer.ScreenGetDPI(selector) == expected, $"Win32 effective DPI for selector {selector} must match.");
                        Console.WriteLine($"Win32 selector {selector}, effective DPI {expected}.");
                    }
                    break;
                case "x11":
                    Check(DisplayServer.GetName() == "X11" && count == 1, "Use an isolated single-output X11 fixture.");
                    var expectedDPI = int.Parse(args[0], CultureInfo.InvariantCulture);
                    Check(expectedDPI != 96 && DisplayServer.ScreenGetScale() == 1f, "The calibrated density must differ from the default and the content scale.");
                    Check(DisplayServer.ScreenGetDPI() == expectedDPI && DisplayServer.ScreenGetDPI(0) == expectedDPI &&
                        DisplayServer.ScreenGetDPI(DisplayServer.ScreenPrimary) == expectedDPI, $"X11 must report the calibrated {expectedDPI} DPI.");
                    Console.WriteLine($"X11 physical DPI {expectedDPI} at content scale 1.");
                    break;
                case "fallback":
                    Check(DisplayServer.ScreenGetDPI() == 96 && DisplayServer.ScreenGetDPI(0) == 96, "Unavailable density must fall back to 96.");
                    Console.WriteLine($"Fallback DPI 96 ({DisplayServer.GetName()}).");
                    break;
                default:
                    throw new ArgumentException("Unknown screen DPI test profile.", nameof(profile));
            }
        }
        Reject(() => DisplayServer.ScreenGetDPI());
        Console.WriteLine($"Screen DPI passed ({profile}).");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Screen DPI requires a live display on its opening thread.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Point(int X, int Y);

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromPoint(Point point, uint flags);

    [LibraryImport("shcore.dll")]
    private static partial int GetDpiForMonitor(nint monitor, int type, out uint dpiX, out uint dpiY);
}
