using Electron2D;
using System.Diagnostics;
using Image = Electron2D.Image;

internal static class StatusIndicatorChecks
{
    private static int _checks, _runs;
    private static string _oracle = "", _output = "";
    private const string Tooltip = "Electron2D status 日本語";
    internal static void Run(string oracle, string output)
    {
        _oracle = oracle; _output = output; System.IO.Directory.CreateDirectory(output);
        Reject<InvalidOperationException>(() => DisplayServer.CreateStatusIndicator(null, "absent"));
        int stale; var activate = Environment.GetEnvironmentVariable("ELECTRON2D_STATUS_MENU_ACTIVATE") == "1";
        using (var display = DisplayServer.Open("Status indicator qualification", new(320, 240), hidden: !activate))
        {
            Check(DisplayServer.HasFeature(DisplayServer.Feature.StatusIndicator) && DisplayServer.InvalidIndicatorId == -1, "Actual Windows Shell capability and absent identity.");
            using var pixels = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); pixels.Fill(Colors.Red);
            using var icon = ImageTexture.CreateFromImage(pixels);
            var calls = new List<MouseButton>(); var callbackThread = 0; var position = Vector2i.Zero;
            var id = DisplayServer.CreateStatusIndicator(icon, Tooltip, (button, at) => { calls.Add(button); position = at; callbackThread = Environment.CurrentManagedThreadId; });
            var ownerThread = Environment.CurrentManagedThreadId;
            Check(id > 0 && DisplayServer.StatusIndicatorGetRect(id).HasArea(), "Positive ID and actual native registration rectangle.");
            calls.Clear(); var post = Native(id, "post:207");
            Check(calls.SequenceEqual(new[] { MouseButton.Middle }) && post.Contains($"cursor={position.X},{position.Y}"), "Scoped channel control preserves button and physical desktop cursor position.");
            Reject<ArgumentNullException>(() => DisplayServer.StatusIndicatorSetTooltip(id, null!));
            Reject<ArgumentException>(() => DisplayServer.StatusIndicatorSetTooltip(id, "bad\0tip"));
            Reject<ArgumentNullException>(() => DisplayServer.CreateStatusIndicator(null, null!));
            Reject<ArgumentException>(() => DisplayServer.CreateStatusIndicator(null, "bad\0tip"));
            Reject<ArgumentException>(() => DisplayServer.StatusIndicatorGetRect(-1));
            Reject<ArgumentException>(() => DisplayServer.StatusIndicatorSetIcon(0, null));
            var badMenu = NativeMenu.CreateMenu(); NativeMenu.FreeMenu(badMenu); Reject<ArgumentException>(() => DisplayServer.StatusIndicatorSetMenu(id, badMenu));
            Check(Task.Run(() => { try { DisplayServer.StatusIndicatorGetRect(id); return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult(), "Owner-thread enforcement.");

            using var unreadable = new PlaceholderTexture();
            Reject<NotSupportedException>(() => DisplayServer.StatusIndicatorSetIcon(id, unreadable));
            using var custom = new ReadableIcon(); custom.Read = () => { Reject<InvalidOperationException>(() => display.Dispose()); Reject<InvalidOperationException>(() => DisplayServer.DeleteStatusIndicator(id)); };
            DisplayServer.StatusIndicatorSetIcon(id, custom); Check(custom.Reads == 1, "Virtual image preparation reserves native lifetime and status mutation.");
            custom.Read = null; custom.EmitChanged(); DisplayServer.ProcessEvents(); Check(custom.Reads == 2, "Changed borrowed icon refreshes once on the owner pump.");
            custom.Fail = true; custom.EmitChanged(); Reject<AggregateException>(DisplayServer.ProcessEvents);
            custom.Fail = false; DisplayServer.ProcessEvents(); Check(custom.Reads == 4, "Failed automatic refresh retains ownership and retries successfully.");
            custom.Dispose(); DisplayServer.ProcessEvents(); Check(DisplayServer.StatusIndicatorGetRect(id).HasArea(), "Disposed source clears image without deleting the Shell registration.");
            Reject<ObjectDisposedException>(() => DisplayServer.StatusIndicatorSetIcon(id, custom));
            DisplayServer.StatusIndicatorSetIcon(id, null); DisplayServer.StatusIndicatorSetIcon(id, icon);
            pixels.Fill(Colors.Transparent); icon.Update(pixels); DisplayServer.ProcessEvents(); Check(DisplayServer.StatusIndicatorGetRect(id).HasArea(), "Fully transparent icon preserves registration.");
            pixels.Fill(Colors.Blue); icon.Update(pixels); DisplayServer.ProcessEvents();

            var menu = NativeMenu.CreateMenu(); var selected = 0; NativeMenu.AddItem(menu, "Status command 日本語", () => selected++);
            var parent = NativeMenu.CreateMenu(); var child = NativeMenu.CreateMenu(); NativeMenu.AddSubmenuItem(parent, "child", child);
            Reject<InvalidOperationException>(() => DisplayServer.StatusIndicatorSetMenu(id, child));
            DisplayServer.StatusIndicatorSetMenu(id, menu); var second = DisplayServer.CreateStatusIndicator(null, "Second indicator"); DisplayServer.StatusIndicatorSetMenu(second, menu);
            Reject<InvalidOperationException>(() => NativeMenu.AddSubmenuItem(parent, "bound", menu));
            Check(Native(id, activate ? "postmenu" : "cancelmenu").Contains("native_menu=Status command 日本語") && selected == (activate ? 1 : 0), "Scoped right-button channel opens the real bound OS menu and follows its selection/cancellation profile.");
            DisplayServer.DeleteStatusIndicator(second); Reject<InvalidOperationException>(() => NativeMenu.AddSubmenuItem(parent, "still bound", menu));
            DisplayServer.StatusIndicatorSetMenu(id, default); NativeMenu.AddSubmenuItem(parent, "detached", menu); NativeMenu.SetItemSubmenu(parent, 1, default);
            DisplayServer.StatusIndicatorSetMenu(id, menu); NativeMenu.FreeMenu(menu);
            calls.Clear(); Native(id, "post:204"); Check(calls.SequenceEqual(new[] { MouseButton.Right }), "Freeing a bound menu detaches it and restores right-button callback delivery.");
            NativeMenu.FreeMenu(parent); NativeMenu.FreeMenu(child);

            calls.Clear(); Native(id, "post:201", drop: true); Check(calls.Count == 0, "Drop-input pump discards status presses.");
            DisplayServer.StatusIndicatorSetCallback(id, null); Native(id, "post:201"); Check(calls.Count == 0, "Nullable callback clears activation.");
            Snapshot(id, display);
            DisplayServer.StatusIndicatorSetCallback(id, (_, _) => { calls.Add(MouseButton.Left); throw new InvalidOperationException("callback control"); });
            Native(id, "post:201", expectedFailure: true); Check(calls.Count == 1, "Callback failure is reported after crossing the native boundary safely.");
            DisplayServer.StatusIndicatorSetCallback(id, (_, _) => calls.Add(MouseButton.Right)); Native(id, "post:204"); Check(calls.Count == 2, "Event pump remains usable after callback failure.");

            calls.Clear(); DisplayServer.StatusIndicatorSetCallback(id, (button, at) => { calls.Add(button); position = at; callbackThread = Environment.CurrentManagedThreadId; });
            var inspection = Native(id, "inspect"); Check(inspection.Contains("own name " + Tooltip), "Independent Shell Unicode tooltip.");
            Native(id, "invoke"); Check(calls.SequenceEqual(new[] { MouseButton.Left }) && callbackThread == ownerThread, "Actual Shell UI Automation invocation delivers one owner-thread left press.");
            var longTip = new string('a', 126) + "😀tail";
            DisplayServer.StatusIndicatorSetTooltip(id, longTip); Native(id, "recover"); Check(Native(id, "inspect", new string('a', 126)).Contains("own name " + new string('a', 126)), "Current tooltip survives re-registration without splitting a surrogate pair.");
            DisplayServer.StatusIndicatorSetTooltip(id, Tooltip); Native(id, "recover");
            Native(id, "delete"); Check(!DisplayServer.StatusIndicatorGetRect(id).HasArea(), "Missing Shell registration returns an empty rectangle.");
            Reject<IOException>(() => DisplayServer.StatusIndicatorSetTooltip(id, "must not commit"));
            using var replacement = new ReadableIcon(); Reject<IOException>(() => DisplayServer.StatusIndicatorSetIcon(id, replacement));
            Native(id, "recover"); Check(DisplayServer.StatusIndicatorGetRect(id).HasArea(), "Scoped TaskbarCreated recovers the retained ID after external removal.");
            Check(Native(id, "inspect").Contains("own name " + Tooltip), "Failed tooltip update preserved the original text for recovery.");
            replacement.EmitChanged(); DisplayServer.ProcessEvents(); Check(replacement.Reads == 1, "Failed native icon replacement unsubscribes the rejected source.");
            var removed = DisplayServer.CreateStatusIndicator(null, "external removal"); Native(removed, "delete"); Reject<IOException>(() => DisplayServer.DeleteStatusIndicator(removed)); Reject<ArgumentException>(() => DisplayServer.StatusIndicatorGetRect(removed));
            DisplayServer.DeleteStatusIndicator(id); Native(id, "absent"); Check(!icon.IsDisposed, "Indicator retirement does not dispose borrowed image resources."); Reject<ArgumentException>(() => DisplayServer.DeleteStatusIndicator(id));
            stale = DisplayServer.CreateStatusIndicator(null, "retirement");
        }
        using (var display = DisplayServer.Open("Status indicator qualification", new(320, 240), hidden: true))
        {
            Reject<ArgumentException>(() => DisplayServer.StatusIndicatorGetRect(stale));
            var next = DisplayServer.CreateStatusIndicator(null, "new session"); Check(next > stale, "IDs do not rebind a later display session."); DisplayServer.DeleteStatusIndicator(next);
        }
        Console.WriteLine($"Status indicators: {_checks} public checks; {_runs} independent Shell/channel inspections.");
    }
    private static void Snapshot(int id, DisplayServer display)
    {
        var menu = NativeMenu.CreateMenu(); NativeMenu.AddItem(menu, "queue control"); var old = 0; var replacement = 0;
        DisplayServer.StatusIndicatorSetCallback(id, (_, _) => { old++; Reject<InvalidOperationException>(() => display.Dispose()); Reject<InvalidOperationException>(DisplayServer.ProcessEvents); });
        using (var driver = Start(id, "queueclose", Tooltip, out _))
        {
            NativeMenu.SetPopupCloseCallback(menu, () => DisplayServer.StatusIndicatorSetCallback(id, (_, _) => replacement++)); NativeMenu.Popup(menu, new(240, 180));
            Finish(driver); DisplayServer.ProcessEvents();
        }
        Check(old == 2 && replacement == 0, "Queued native presses retain accepted delegates across replacement and reject pump/disposal reentry.");
        DisplayServer.StatusIndicatorSetCallback(id, (_, _) => { old++; throw new InvalidOperationException("batch control"); });
        using (var driver = Start(id, "queueclose", Tooltip, out _))
        {
            NativeMenu.SetPopupCloseCallback(menu, null); NativeMenu.Popup(menu, new(240, 180)); Finish(driver); Reject<AggregateException>(DisplayServer.ProcessEvents);
        }
        Check(old == 4, "Failure in one queued callback does not skip the next accepted press.");
        var doomed = DisplayServer.CreateStatusIndicator(null, "queued retirement", (_, _) => replacement++);
        using (var driver = Start(doomed, "queueclose", "queued retirement", out _))
        {
            NativeMenu.SetPopupCloseCallback(menu, () => DisplayServer.DeleteStatusIndicator(doomed)); NativeMenu.Popup(menu, new(240, 180)); Finish(driver); DisplayServer.ProcessEvents();
        }
        Check(replacement == 0, "Deletion suppresses already accepted presses."); NativeMenu.FreeMenu(menu);
    }
    internal static void Unavailable()
    {
        using var display = DisplayServer.Open("Unsupported status host", new(64, 64), hidden: true);
        Check(!DisplayServer.HasFeature(DisplayServer.Feature.StatusIndicator), "Unsupported host capability."); Reject<NotSupportedException>(() => DisplayServer.CreateStatusIndicator(null, "unavailable")); Console.WriteLine("Unsupported status-indicator host rejected before native entry.");
    }
    private sealed class ReadableIcon : Texture
    {
        internal int Reads; internal bool Fail; internal Action? Read;
        public override int GetWidth() => 16;
        public override int GetHeight() => 16;
        public override Image? GetImage() { Reads++; Read?.Invoke(); if (Fail) throw new InvalidOperationException("image failure control"); var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.Red); return image; }
    }
    private static Process Start(int id, string action, string tooltip, out string stem)
    {
        stem = System.IO.Path.Combine(_output, (++_runs).ToString()); var start = new ProcessStartInfo(_oracle) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var value in new[] { Environment.ProcessId.ToString(), id.ToString(), action, stem, tooltip }) start.ArgumentList.Add(value);
        return Process.Start(start) ?? throw new Exception("Native oracle did not start.");
    }
    private static void Finish(Process driver) { if (!driver.WaitForExit(15000)) { driver.Kill(); throw new Exception("Native oracle did not finish."); } Check(driver.ExitCode == 0, "Independent Shell inspector completed with exit " + driver.ExitCode); }
    private static string Native(int id, string action, string tooltip = Tooltip, bool drop = false, bool expectedFailure = false)
    {
        using var driver = Start(id, action, tooltip, out var stem); var timer = Stopwatch.StartNew(); var failed = false;
        while (!driver.HasExited)
        {
            if (timer.Elapsed.TotalSeconds > 20) { driver.Kill(); throw new Exception("Native inspector timed out: " + stem); }
            try { if (drop) DisplayServer.ForceProcessAndDropEvents(); else DisplayServer.ProcessEvents(); } catch (AggregateException) { if (!expectedFailure) throw; failed = true; }
            Thread.Sleep(10);
        }
        Finish(driver);
        try { if (drop) DisplayServer.ForceProcessAndDropEvents(); else DisplayServer.ProcessEvents(); } catch (AggregateException) { if (!expectedFailure) throw; failed = true; }
        if (expectedFailure) Check(failed, "Expected callback aggregate.");
        return File.ReadAllText(stem + ".txt");
    }
    private static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception(reason); }
    private static void Reject<T>(Action action) where T : Exception { _checks++; try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
}
