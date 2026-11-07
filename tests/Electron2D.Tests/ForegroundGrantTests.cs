using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Electron2D;

internal static partial class ForegroundGrantTests
{
    internal static void Run(string profile, string[] args)
    {
        Reject<InvalidOperationException>(() => DisplayServer.EnableForStealingFocus(OS.GetProcessID()));
        if (profile == "unsupported")
        {
            using var dummy = DisplayServer.Open("Unsupported foreground grant", new(64, 64), hidden: true);
            Reject<NotSupportedException>(() => DisplayServer.EnableForStealingFocus(OS.GetProcessID()));
            Console.WriteLine("Unsupported foreground grant passed.");
            return;
        }

        Check(profile == "windows" && OperatingSystem.IsWindows() && args.Length == 1,
            "Provide the native foreground recipient on an open Windows desktop.");
        VerifyHandoff(args[0]);
        Reject<InvalidOperationException>(() => DisplayServer.EnableForStealingFocus(OS.GetProcessID()));
        Console.WriteLine("Windows foreground grant passed.");
    }

    private static void VerifyHandoff(string recipient)
    {
        Check(GetCursorPos(out var cursor) != 0, "Save the desktop pointer position.");
        using var display = DisplayServer.Open("Electron2D foreground donor", new(280, 160));
        var owner = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
        var screen = DisplayServer.ScreenGetPosition(DisplayServer.ScreenPrimary);
        var size = DisplayServer.ScreenGetSize(DisplayServer.ScreenPrimary);
        Check(size.X >= 720 && size.Y >= 300, "The display has room for both fixture windows.");
        DisplayServer.WindowSetPosition(screen + new Vector2i(60, 85));
        DisplayServer.WindowSetFlag(WindowFlag.AlwaysOnTop, true);
        var releases = 0;
        DisplayServer.GetWindowEvents().Input += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) releases++;
        };

        void ClickDonor()
        {
            var previous = releases;
            Click(owner);
            WaitUntil(() => releases != previous, "The donor received the click.");
        }

        var start = new ProcessStartInfo(recipient) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true };
        start.ArgumentList.Add((screen.X + 400).ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add((screen.Y + 85).ToString(CultureInfo.InvariantCulture));
        using var child = Process.Start(start) ?? throw new InvalidOperationException("Could not launch the foreground recipient.");
        try
        {
            var target = nint.Parse(ReadReply(child), CultureInfo.InvariantCulture);
            Check(GetWindowThreadProcessId(target, out var actualPID) != 0 && actualPID == (uint)child.Id,
                "The recipient HWND belongs to the independently launched process.");

            ClickDonor();
            WaitUntil(() => GetForegroundWindow() == owner, "The Electron2D donor owns the foreground.");
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.EnableForStealingFocus(0));
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.EnableForStealingFocus(-1));
            Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.EnableForStealingFocus(child.Id)).GetAwaiter().GetResult());
            Check(Reject<Win32Exception>(() => DisplayServer.EnableForStealingFocus(int.MaxValue)).NativeErrorCode != 0,
                "Windows reports an error for a nonexistent process.");

            Check(!Activate(child) && GetForegroundWindow() == owner, "The recipient cannot activate before the grant.");
            DisplayServer.EnableForStealingFocus(child.Id);
            Check(GetForegroundWindow() == owner, "Granting permission leaves the foreground unchanged.");

            ClickDonor();
            Check(!Activate(child) && GetForegroundWindow() == owner, "Windows revokes the grant on new input to the donor.");

            DisplayServer.EnableForStealingFocus(child.Id);
            Check(Activate(child), "The recipient requests foreground activation successfully after the grant.");
            WaitUntil(() => GetForegroundWindow() == target, "The native foreground HWND changes to the recipient.");
            Console.WriteLine("Independent recipient denied before grant and after revocation, then acquired foreground.");
        }
        finally
        {
            child.StandardInput.Close();
            if (!child.WaitForExit(4000)) { child.Kill(); child.WaitForExit(4000); }
            _ = SetCursorPos(cursor.X, cursor.Y);
        }
    }

    private static bool Activate(Process child)
    {
        child.StandardInput.WriteLine("activate");
        return ReadReply(child) switch
        {
            "1" => true,
            "0" => false,
            var reply => throw new InvalidOperationException($"Unexpected recipient reply: {reply}"),
        };
    }

    private static string ReadReply(Process child)
    {
        var line = child.StandardOutput.ReadLineAsync();
        WaitUntil(() => line.IsCompleted, "The foreground recipient responds.");
        return line.GetAwaiter().GetResult() ?? throw new InvalidOperationException("The foreground recipient exited.");
    }

    private static void WaitUntil(Func<bool> predicate, string message)
    {
        var wait = Stopwatch.StartNew();
        while (!predicate() && wait.Elapsed < TimeSpan.FromSeconds(4))
        {
            DisplayServer.ProcessEvents();
            Thread.Sleep(5);
        }
        Check(predicate(), message);
    }

    private static unsafe void Click(nint window)
    {
        Check(GetClientRect(window, out var rect) != 0, "Get the donor client area.");
        var point = new Point { X = (rect.Left + rect.Right) / 2, Y = (rect.Top + rect.Bottom) / 2 };
        Check(ClientToScreen(window, ref point) != 0 && WindowFromPoint(point) == window, "The click is over the owned fixture.");
        Check(SetCursorPos(point.X, point.Y) != 0, "Move the pointer to the donor.");
        MouseInputEvent* input = stackalloc MouseInputEvent[2];
        input[0] = new() { Mouse = new() { Flags = 0x0002 } };
        input[1] = new() { Mouse = new() { Flags = 0x0004 } };
        Check(SendInput(2, input, sizeof(MouseInputEvent)) == 2, "Send a real desktop click to the donor.");
    }

    private static T Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T error) { return error; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Check(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException(message);
    }

    private const string User32 = "user32.dll";

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { internal int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput { internal int X, Y; internal uint Data, Flags, Time; internal nuint Extra; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputEvent { internal uint Type; internal MouseInput Mouse; }

    [LibraryImport(User32)] private static partial nint GetForegroundWindow();
    [LibraryImport(User32)] private static partial uint GetWindowThreadProcessId(nint window, out uint pid);
    [LibraryImport(User32)] private static partial int GetClientRect(nint window, out Rect rect);
    [LibraryImport(User32)] private static partial int ClientToScreen(nint window, ref Point point);
    [LibraryImport(User32)] private static partial nint WindowFromPoint(Point point);
    [LibraryImport(User32)] private static partial int GetCursorPos(out Point point);
    [LibraryImport(User32)] private static partial int SetCursorPos(int x, int y);
    [LibraryImport(User32)] private static unsafe partial uint SendInput(uint count, MouseInputEvent* input, int size);
}
