using System.Diagnostics;
using System.Globalization;
using Electron2D;
using SDL3;
using Path = System.IO.Path;

internal static class X11CaptionButtonTests
{
    internal static void Run(string output)
    {
        Directory.CreateDirectory(output);
        using var root = new Window { Title = "Electron2D X11 caption", Size = new(460, 220), GUIEmbedSubwindows = false };
        using var child = new Window
        {
            Title = "Electron2D X11 child",
            Size = new(320, 160),
            MaximizeDisabled = true,
            Visible = false,
        };
        using var popup = new Popup { Size = new(100, 80) };
        root.AddChild(child);
        root.AddChild(popup);
        Verify(root, () =>
        {
            Require(SDL.GetCurrentVideoDriver() == "x11", "The caption check requires SDL X11.");
            CheckTitleVisibilityRefusal(root);
            Observe(root, "project-both-disabled", minimize: false, maximize: false);
            Exercise(root, "root", capture: true);
            child.Show();
            CheckTitleVisibilityRefusal(child);
            Observe(child, "child-open", minimize: true, maximize: false);
            Exercise(child, "child", capture: false);
            child.MaximizeDisabled = true;
            var previousID = child.GetWindowID();
            child.Hide();
            child.Show();
            Require(previousID != child.GetWindowID(), "Child recreation must allocate a new window ID.");
            Observe(child, "child-reopened", minimize: true, maximize: false);
            popup.Show();
            Require(!popup.IsEmbedded() &&
                DisplayServer.WindowGetFlag(WindowFlag.MinimizeDisabled, popup.GetWindowID()) &&
                DisplayServer.WindowGetFlag(WindowFlag.MaximizeDisabled, popup.GetWindowID()),
                "The native popup must retain its fixed caption policy.");
            popup.Hide();
            Console.WriteLine("Native popup retained its fixed caption policy.");
        });

        using var scene = new PackedScene();
        using var authored = new Window { MinimizeDisabled = false, MaximizeDisabled = false };
        scene.Pack(authored);
        using var restored = (Window)scene.Instantiate();
        restored.Size = new(460, 220);
        restored.GUIEmbedSubwindows = true;
        using var embedded = new Window { Size = new(100, 80), MinimizeDisabled = true, MaximizeDisabled = true };
        restored.AddChild(embedded);
        Verify(restored, () =>
        {
            Require(embedded.IsEmbedded() && embedded.MinimizeDisabled && embedded.MaximizeDisabled,
                "Embedded caption policies must stay local to the embedded window.");
            Observe(restored, "packed-explicit-false-with-embedded-child", minimize: true, maximize: true);
        });

        // Avoid measuring a detached theme title before testing the native startup path.
        using var unsupported = new Window { Borderless = true, KeepTitleVisible = true };
        RequireTitleMeasurementUnavailable(() => Engine.RunProject(unsupported));
        Require(unsupported.IsDisposed && !DisplayServer.IsAvailable, "Rejected title policy must retire the root and display.");
        Console.WriteLine("X11 KeepTitleVisible rejected at root startup and on live root and child windows.");

        void Exercise(Window window, string name, bool capture)
        {
            var id = window.GetWindowID();
            var xid = GetXID(window);
            var otherHints = ReadOtherHints(xid);
            window.MinimizeDisabled = false;
            window.MaximizeDisabled = false;
            Observe(window, name + "-both-enabled", minimize: true, maximize: true, capture: capture);
            window.MinimizeDisabled = true;
            Observe(window, name + "-minimize-disabled", minimize: false, maximize: true, capture: capture);
            DisplayServer.WindowSetFlag(WindowFlag.MaximizeDisabled, true, id);
            Require(DisplayServer.WindowGetFlag(WindowFlag.MaximizeDisabled, id) && !window.IsMaximizeAllowed(),
                name + ": DisplayServer did not apply the maximize restriction.");
            window.MaximizeDisabled = true;
            Observe(window, name + "-both-disabled", minimize: false, maximize: false);
            window.MinimizeDisabled = false;
            Observe(window, name + "-maximize-disabled", minimize: true, maximize: false, capture: capture);
            Require(ReadOtherHints(xid) == otherHints, name + ": caption policy changed other window hints.");

            window.Borderless = true;
            window.Borderless = false;
            Observe(window, name + "-frame-restored", minimize: true, maximize: false);
            window.Unresizable = true;
            window.MaximizeDisabled = false;
            Observe(window, name + "-fixed-size", minimize: true, maximize: true, resizable: false);
            window.Unresizable = false;
            SDL.PumpEvents();
            Observe(window, name + "-reset", minimize: true, maximize: true);
        }

        void Observe(Window window, string label, bool minimize, bool maximize, bool resizable = true, bool capture = false)
        {
            var id = window.GetWindowID();
            var xid = GetXID(window);
            Require(window.MinimizeDisabled == !minimize && window.MaximizeDisabled == !maximize &&
                DisplayServer.WindowGetFlag(WindowFlag.MinimizeDisabled, id) == !minimize &&
                DisplayServer.WindowGetFlag(WindowFlag.MaximizeDisabled, id) == !maximize &&
                window.IsMaximizeAllowed() == (maximize && resizable), label + ": client policy mismatch.");

            WaitUntil(() => Command("xprop", "-id", xid, "_NET_FRAME_EXTENTS").Contains('='),
                label + ": the manager did not decorate the window.");
            var actions = Command("xprop", "-id", xid, "_NET_WM_ALLOWED_ACTIONS");
            Require(actions.Contains("_NET_WM_ACTION_CLOSE", StringComparison.Ordinal) &&
                actions.Contains("_NET_WM_ACTION_MOVE", StringComparison.Ordinal), label + ": close or move was lost.");

            var hints = Command("xprop", "-id", xid, "-f", "_MOTIF_WM_HINTS", "32c", "_MOTIF_WM_HINTS");
            var fields = hints.Split('=', 2)[1].Split(',');
            Require(fields.Length == 5, label + ": malformed Motif hints.");
            var valid = uint.Parse(fields[0], CultureInfo.InvariantCulture);
            var functions = uint.Parse(fields[1], CultureInfo.InvariantCulture);
            var inverted = (functions & 1) != 0;
            Require(Allows(8) == minimize && Allows(16) == maximize, label + ": " + hints.Trim());
            Require((valid & 2) != 0 && uint.Parse(fields[2], CultureInfo.InvariantCulture) == 1,
                label + ": Motif decoration request was lost.");
            Command("xdotool", "windowraise", xid, "windowfocus", xid);
            var numericXID = Convert.ToUInt64(xid, 16).ToString(CultureInfo.InvariantCulture);
            WaitUntil(() => Command("xdotool", "getwindowfocus").Trim() == numericXID,
                label + ": the test could not focus window " + xid);
            if (capture)
            {
                Command("import", "-window", xid, "-frame", Path.Combine(output, label + ".png"));
            }
            // Xfwm4 can keep the mapped _NET_WM_ALLOWED_ACTIONS property after applying live hints.
            TryShortcut("alt+F10", "_NET_WM_STATE_MAXIMIZED_HORZ", maximize && resizable);
            TryShortcut("alt+F9", "_NET_WM_STATE_HIDDEN", minimize);
            Console.WriteLine($"{label} {xid}: {hints.Trim()} | {actions.Trim()} | shortcut minimize={minimize}, maximize={maximize && resizable}");

            bool Allows(uint function) => (valid & 1) == 0 || inverted != ((functions & function) != 0);

            void TryShortcut(string key, string state, bool allowed)
            {
                bool IsActive() => Command("xprop", "-id", xid, "_NET_WM_STATE").Contains(state, StringComparison.Ordinal);

                Require(!IsActive(), label + ": unexpected initial state " + state);
                Command("xdotool", "key", "--clearmodifiers", key);
                if (allowed)
                {
                    WaitUntil(IsActive, label + ": expected the manager to allow " + key);
                    if (key == "alt+F9")
                        Command("xdotool", "windowmap", xid);
                    else
                        Command("xdotool", "key", "--clearmodifiers", key);
                    WaitUntil(() => !IsActive(), label + ": could not restore " + state);
                    Command("xdotool", "windowfocus", xid);
                }
                else
                {
                    Thread.Sleep(250);
                    Require(!IsActive(), label + ": expected the manager to refuse " + key);
                }
            }
        }
    }

    private static void CheckTitleVisibilityRefusal(Window window)
    {
        var size = window.Size;
        RequireTitleMeasurementUnavailable(() => window.KeepTitleVisible = true);
        Require(!window.KeepTitleVisible && window.Size == size, "Rejected title policy must preserve the live window.");
    }

    private static void RequireTitleMeasurementUnavailable(Action operation)
    {
        try
        {
            operation();
        }
        catch (NotSupportedException error)
        {
            Require(error.Message == "Native title measurement requires a top-level Win32 window.",
                "Expected the native caption measurement guard.");
            return;
        }
        throw new InvalidOperationException("X11 must reject native title measurement.");
    }

    private static string GetXID(Window window) =>
        $"0x{DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, window.GetWindowID()):x}";

    private static string ReadOtherHints(string xid) =>
        Command("xprop", "-id", xid, "WM_PROTOCOLS", "WM_NORMAL_HINTS", "_NET_WM_WINDOW_TYPE");

    private static void WaitUntil(Func<bool> predicate, string failure)
    {
        var timer = Stopwatch.StartNew();
        while (!predicate())
        {
            Require(timer.Elapsed < TimeSpan.FromSeconds(3), failure);
            Thread.Sleep(20);
        }
    }

    private static void Verify(Window window, Action check)
    {
        window.Ready += _ =>
        {
            RenderingServer.FramePostDraw += () =>
            {
                check();
                window.Tree!.Quit();
            };
        };
        Require(Engine.RunProject(window) == 0 && window.IsDisposed, "Caption project must present and retire.");
    }

    private static string Command(string file, params string[] arguments)
    {
        var start = new ProcessStartInfo(file) { RedirectStandardOutput = true, UseShellExecute = false };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Cannot start {file}.");
        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Require(process.ExitCode == 0, $"{file} exited with code {process.ExitCode}.");
        return result;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
