using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using Flag = Electron2D.AccessibilityServer.AccessibilityFlags;

internal static partial class AccessibilityDragTests
{
    internal static void RunNative(string backend)
    {
        RunManaged();
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Native Drag Probe"); ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(760, 540), GUIEmbedSubwindows = true };
        var source = new Source { AccessibilityName = "Inventory", MouseFilter = MouseFilter.Ignore, FocusMode = FocusMode.All, Position = new(12, 12), Size = new(200, 40) };
        var target = new Target { AccessibilityName = "Destination", MouseFilter = MouseFilter.Ignore, FocusMode = FocusMode.All, Position = new(260, 12), Size = new(220, 40) };
        var line = new LineEdit { AccessibilityName = "Source line", Text = "A😀B", Position = new(12, 80), Size = new(220, 34) };
        var field = new LineEdit { AccessibilityName = "Target line", Text = "XY", Position = new(260, 80), Size = new(220, 34) };
        var text = new TextEdit { AccessibilityName = "Source editor", Text = "red\nblue", Position = new(12, 140), Size = new(220, 100) };
        var code = new CodeEdit { AccessibilityName = "Target editor", Text = "[]", Position = new(260, 140), Size = new(220, 100) };
        var status = new Label { Text = "Ready", Position = new(12, 400), Size = new(700, 90) };
        foreach (var node in new Control[] { source, target, line, field, text, code, status }) window.AddChild(node);
        Process? client = null; Task<string>? output = null, error = null; RID controller = default, metadata = default, guarded = default;
        var genericCount = 0; var minCount = 0; var maxCount = 0; var complete = false; var clientDone = false; var post = 0; var measuring = false; var captured = false; long before = 0, idle = 0, active = 0;
        using var texture = window.GetTexture();
        void Command(int id)
        {
            switch (id)
            {
                case 10: source.AccessibilityDrag(); status.Text = "Package active"; break;
                case 11: target.AccessibilityDrop(); Check(target.Drops > 0 && source.Successful, "Native custom command completes ordinary typed transfer."); status.Text = "Package delivered"; break;
                case 12: source.GrabFocus(); using (var input = new InputEventAction { Action = "ui_accessibility_drag_and_drop", Pressed = true }) window.PushInput(input, true); status.Text = "Keyboard active"; break;
                case 13: target.GrabFocus(); using (var input = new InputEventAction { Action = "ui_accessibility_drag_and_drop", Pressed = true }) window.PushInput(input, true); Check(window.IsGUIDragSuccessful(), "Native command reaches the keyboard drop route."); status.Text = "Keyboard delivered"; break;
                case 14: window.CancelGUIDrag(); status.Text = "Cancelled"; break;
                case 15: source.Empty = true; try { source.AccessibilityDrag(); } finally { source.Empty = false; } Check(!window.IsGUIDragging() && source.Preview!.IsDisposed, "Native null producer rolls back."); status.Text = "Null rejected"; break;
                case 16: target.Accept = false; target.QueueAccessibilityUpdate(); status.Text = "Destination blocked"; break;
                case 17: target.Accept = true; target.QueueAccessibilityUpdate(); status.Text = "Destination accepted"; break;
                case 18:
                    line.Select(1, 2); field.CaretColumn = 1; line.AccessibilityDrag(); field.AccessibilityDrop(); Check(line.Text == "AB" && field.Text == "X😀Y", "Native line transport preserves scalar semantics."); status.Text = "Line delivered"; break;
                case 19:
                    text.Select(0, 0, 0, 3); code.SetCaretColumn(1); text.AccessibilityDrag(); code.AccessibilityDrop(); Check(text.Text == "\nblue" && code.Text == "[red]", "Native editor transport reaches CodeEdit."); status.Text = "Editor delivered"; break;
                case 20:
                    line.Text = "ABCDE"; line.SelectAll(); field.MaxLength = 2; field.Text = "X"; field.CaretColumn = 1; line.AccessibilityDrag(); field.AccessibilityDrop(); Check(line.Text == "ABCDE" && field.Text == "X" && !window.IsGUIDragSuccessful(), "Native capacity rejection prevents source loss."); status.Text = "Capacity rejected"; break;
                case 50: AccessibilityServer.UpdateSetName(metadata, "  A\0😀  "); AccessibilityServer.UpdateSetExtraInfo(metadata, " \0Z "); status.Text = "Metadata unicode"; break;
                case 51: AccessibilityServer.UpdateSetExtraInfo(metadata, new string('λ', 3000)); status.Text = "Metadata long"; break;
                case 52: AccessibilityServer.UpdateSetExtraInfo(metadata, ""); status.Text = "Metadata cleared"; break;
                case 53: AccessibilityServer.UpdateAddCustomAction(controller, int.MinValue, "custom-min-renamed"); status.Text = "Action renamed"; break;
                case 54: AccessibilityServer.UpdateSetFlag(guarded, Flag.Disabled, false); break;
                case 55: Check(genericCount == 1 && minCount == 1 && maxCount == 1, "Native custom IDs remain exact and disabled requests do not invoke."); status.Text = "Custom IDs checked"; break;
                case int.MinValue: minCount++; break;
                case int.MaxValue: maxCount++; break;
                case 99: if (window.IsGUIDragging()) window.CancelGUIDrag(); source.AccessibilityDrag(); complete = true; status.Text = "Complete"; break;
                default: throw new InvalidOperationException("Unexpected custom action ID " + id);
            }
        }
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "Actual requested canvas backend and native bridge execute.");
            var root = window.GetAccessibilityElement(); controller = AccessibilityServer.CreateSubElement(root, Role.Button); AccessibilityServer.UpdateSetName(controller, "Transfer controller"); AccessibilityServer.UpdateSetBounds(controller, new(12, 300, 300, 40));
            AccessibilityServer.UpdateAddAction(controller, AAction.Custom, (int id) => Command(id));
            foreach (var (id, label) in new (int, string)[] { (10, "grab"), (11, "drop"), (12, "keyboard-grab"), (13, "keyboard-drop"), (14, "cancel"), (15, "null-grab"), (16, "block"), (17, "accept"), (18, "line"), (19, "editor"), (20, "capacity"), (50, "unicode"), (51, "long"), (52, "clear"), (53, "rename"), (54, "enable"), (55, "check-ids"), (int.MinValue, "custom-min"), (int.MaxValue, "custom-max"), (99, "finish") }) AccessibilityServer.UpdateAddCustomAction(controller, id, label);
            AccessibilityServer.UpdateAddCustomAction(controller, 10, "");
            metadata = AccessibilityServer.CreateSubElement(root, Role.StaticText); AccessibilityServer.UpdateSetName(metadata, "  Metadata  "); AccessibilityServer.UpdateSetExtraInfo(metadata, "  context  ");
            guarded = AccessibilityServer.CreateSubElement(root, Role.Button); AccessibilityServer.UpdateSetName(guarded, "Guarded custom"); AccessibilityServer.UpdateAddAction(guarded, AAction.Custom, (int _) => genericCount++); AccessibilityServer.UpdateAddCustomAction(guarded, -7, "guarded-action"); AccessibilityServer.UpdateSetFlag(guarded, Flag.Disabled, true);
            var missing = AccessibilityServer.CreateSubElement(root, Role.Button); AccessibilityServer.UpdateSetName(missing, "Unbound custom"); AccessibilityServer.UpdateAddCustomAction(missing, 5, "unbound-action");
            var retiring = AccessibilityServer.CreateSubElement(root, Role.Button); AccessibilityServer.UpdateSetName(retiring, "Retiring custom"); AccessibilityServer.UpdateAddCustomAction(retiring, 7, "retire"); AccessibilityServer.UpdateAddAction(retiring, AAction.Custom, (int _) => AccessibilityServer.FreeElement(retiring));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateAddAction(controller, AAction.Custom, () => { }));
            Task.Run(() => Reject<InvalidOperationException>(() => AccessibilityServer.UpdateSetExtraInfo(controller, "foreign"))).GetAwaiter().GetResult();
            var start = Client(); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && post >= 16 && (post < 80 || post >= 96 && post < 160); if (measuring) before = GC.GetAllocatedBytesForCurrentThread();
                if (complete && post >= 80) { var alternate = (post & 1) == 0; source.AccessibilityName = alternate ? "Inventory one" : "Inventory two"; target.Accept = alternate; target.QueueAccessibilityUpdate(); AccessibilityServer.UpdateSetExtraInfo(controller, alternate ? "context one" : "context two"); }
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (post < 80) idle += bytes; else active += bytes; }
            if (!captured && window.IsGUIDragging() && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } capture) { captured = true; Directory.CreateDirectory(capture); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(capture, "accessible-drag-" + backend + ".png")); }
            if (!complete) return; post++; if (post == 160) { Check(idle == 0 && active == 0, $"Prepared drag semantic idle={idle}, active={active} managed bytes."); }
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) => { if (!clientDone && client?.HasExited == true) { Console.Write(output!.GetAwaiter().GetResult()); Console.Write(error!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && complete && captured, "Independent native custom/drag client completes."); clientDone = true; } if (clientDone && complete && post >= 160) window.Tree!.Quit(); if (Stopwatch.GetElapsedTime(started).TotalSeconds > 80) throw new TimeoutException("Native drag client timed out."); }, window) == 0, "Native drag host closes.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); }
        RunCustomFailure(); Console.WriteLine($"PASS {_checks} native custom actions and accessible drag contracts on {backend}; 64 idle/64 active {idle}/{active} managed bytes.");
    }
    private static ProcessStartInfo Client(string? mode = null)
    { var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/drag.py")); if (mode is not null) start.ArgumentList.Add(mode); return start; }
    private static void RunCustomFailure()
    {
        var window = new Window { Size = new(140, 80) }; Process? client = null; Task<string>? output = null, error = null; RID producer = default;
        window.Ready += _ => { producer = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Button); AccessibilityServer.UpdateSetName(producer, "Failing custom"); AccessibilityServer.UpdateAddCustomAction(producer, int.MinValue, "fail"); AccessibilityServer.UpdateAddAction(producer, AAction.Custom, (int id) => throw new InvalidOperationException("Injected custom failure " + id)); client = Process.Start(Client("--failure"))!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync(); };
        var started = Stopwatch.GetTimestamp(); var failed = false;
        try { Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 15) throw new TimeoutException("Custom failure client timed out."); }, window); }
        catch (AggregateException errors) { var failures = errors.Flatten().InnerExceptions; failed = failures.Count == 1 && failures[0] is InvalidOperationException { Message: "Injected custom failure -2147483648" }; }
        finally { if (client is { HasExited: false } && !client.WaitForExit(5000)) client.Kill(true); if (output is not null) Console.Write(output.GetAwaiter().GetResult()); if (error is not null) Console.Write(error.GetAwaiter().GetResult()); Check(failed && client?.ExitCode == 0 && !AccessibilityServer.HasElement(producer), "Custom failure replies and retires native producer."); client?.Dispose(); }
        var fresh = new Window { Size = new(100, 60) }; fresh.Ready += _ => fresh.Tree!.Quit(); Check(Engine.Run(fresh) == 0, "Display reopens after custom failure.");
    }
}
