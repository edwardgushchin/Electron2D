using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;

internal static class AccessibilityContextTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private sealed class Group : Control
    {
        internal string Suffix = "Row one";
        internal Node? Last;
        internal Action<Node>? DuringGet;
        protected override string OnGetAccessibilityContainerName(Node node) { Last = node; DuringGet?.Invoke(node); return Suffix; }
    }
    private sealed class ContextButton : Button
    {
        internal string Help = "Use arrow keys";
        internal Action? DuringHelp;
        internal int Reads;
        protected override string OnAccessibilityGetContextualInfo() { Reads++; DuringHelp?.Invoke(); return Help; }
    }
    private sealed class BadBinding : InputEvent
    {
        public override string AsText() => throw new InvalidOperationException("Injected shortcut description failure");
        protected override InputEvent CreateEventInstance() => new BadBinding();
    }
    private sealed class Forwarder : Control { internal string Query(Node node) => OnGetAccessibilityContainerName(node); }
    internal static void RunManaged()
    {
        using var outer = new Group(); var inner = new Forwarder(); var child = new Node(); outer.AddChild(inner); inner.AddChild(child);
        Check(inner.Query(child) == "Row one" && outer.Last == inner, "Default context forwards the immediate container at each level.");
        outer.RemoveChild(inner); using var boundary = new Node(); boundary.AddChild(inner);
        Check(inner.Query(child) == "", "Non-Control parents end container inheritance.");
        try { inner.Query(null!); throw new Exception("Null accepted"); } catch (ArgumentNullException) { _checks++; }
        try { AccessibilityServer.UpdateSetShortcut(default, null!); throw new Exception("Null shortcut accepted"); } catch (ArgumentNullException) { _checks++; }
        try { AccessibilityServer.UpdateSetClassName(default, null!); throw new Exception("Null class accepted"); } catch (ArgumentNullException) { _checks++; }
        try { AccessibilityServer.UpdateSetRoleDescription(default, null!); throw new Exception("Null role accepted"); } catch (ArgumentNullException) { _checks++; }
        Console.WriteLine($"PASS {_checks} managed contextual navigation contracts.");
    }
    internal static void RunNative(string backend)
    {
        RunManaged(); ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Context Probe"); ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var key = new InputEventKey { Keycode = Key.M, ControlPressed = true }; using var shortcut = new Shortcut { Events = [key] };
        var window = new Window { Size = new(760, 460), GUIEmbedSubwindows = true };
        var group = new Group { Size = new(350, 160), Position = new(10, 10) }; var other = new Group { Suffix = "Other", Position = new(380, 10), Size = new(350, 160) };
        var inner = new Forwarder { Size = new(320, 140) }; var button = new ContextButton { Text = "Move", Shortcut = shortcut, Size = new(280, 60), FocusMode = FocusMode.All };
        var victimGroup = new Group { Suffix = "Slot", Position = new(10, 180), Size = new(200, 60) }; var victim = new ContextButton { Text = "Victim", Size = new(180, 50) }; victimGroup.AddChild(victim);
        var status = new Label { Text = "Ready", Position = new(10, 330), Size = new(700, 100) };
        group.AddChild(inner); inner.AddChild(button); window.AddChild(group); window.AddChild(other); window.AddChild(victimGroup); window.AddChild(status);
        var menu = new PopupMenu { Transparent = false, TransparentBG = false, Name = "Actions" }; menu.AddItem("Menu action", 1, (Key)((int)Key.F7 | (int)KeyModifierMask.Control)); window.AddChild(menu);
        Process? client = null; Task<string>? output = null, error = null; RID controller = default; var clientDone = false; var complete = false; var clicks = 0; var post = 0; var measuring = false; long before = 0, idle = 0, active = 0; var idleReads = 0; var captured = false; using var texture = window.GetTexture();
        button.Pressed += () => clicks++;
        void Command(int id)
        {
            var element = button.GetAccessibilityElement();
            switch (id)
            {
                case 0: key.Keycode = Key.N; status.Text = "First edit"; break;
                case 1: group.Suffix = "Row two"; group.QueueAccessibilityUpdate(); button.Help = "Use Enter"; button.QueueAccessibilityUpdate(); status.Text = "Context changed"; break;
                case 2:
                    key.Keycode = Key.F8; key.ControlPressed = false; using (var input = new InputEventKey { Keycode = Key.F8, Pressed = true }) window.PushInput(input, true);
                    using (var release = new InputEventKey { Keycode = Key.F8, Pressed = false }) window.PushInput(release, true); Check(clicks == 1, "Published shortcut invokes the real button input path."); status.Text = "Binding changed"; break;
                case 3: AccessibilityServer.UpdateSetShortcut(element, "Alt+X"); AccessibilityServer.UpdateSetClassName(element, "custom-button"); AccessibilityServer.UpdateSetRoleDescription(element, "delivery action"); status.Text = "Override"; break;
                case 4: key.Keycode = Key.F9; button.QueueAccessibilityUpdate(); status.Text = "Borrowed edited"; break;
                case 5: AccessibilityServer.UpdateSetShortcut(element, ""); AccessibilityServer.UpdateSetClassName(element, ""); AccessibilityServer.UpdateSetRoleDescription(element, ""); status.Text = "Cleared"; break;
                case 13: AccessibilityServer.UpdateSetName(element, "Manual name"); status.Text = "Name override"; break;
                case 6: group.Suffix = " \0😀 "; group.QueueAccessibilityUpdate(); button.Help = new string('λ', 3000); button.QueueAccessibilityUpdate(); AccessibilityServer.UpdateSetExtraInfo(element, "extra"); status.Text = "Unicode"; break;
                case 12: group.Suffix = new string('λ', 3000); group.QueueAccessibilityUpdate(); status.Text = "Long name"; break;
                case 7: group.RemoveChild(inner); other.AddChild(inner); status.Text = "Reparented"; break;
                case 8: other.RemoveChild(inner); var boundary = new Node(); other.AddChild(boundary); boundary.AddChild(inner); status.Text = "Boundary"; break;
                case 9: victimGroup.DuringGet = node => node.Dispose(); victimGroup.QueueAccessibilityUpdate(); status.Text = "Retire queued"; break;
                case 10: menu.Position = new(460, 220); menu.Popup(); status.Text = "Menu visible"; break;
                case 11: menu.SetItemAccelerator(0, Key.F6); status.Text = "Menu edited"; break;
                case 99: menu.Hide(); inner.Parent!.RemoveChild(inner); other.AddChild(inner); AccessibilityServer.UpdateSetExtraInfo(button.GetAccessibilityElement(), "context"); complete = true; status.Text = "Measuring"; break;
                default: throw new InvalidOperationException("Unexpected context command");
            }
        }
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "Requested renderer and native bridge execute.");
            controller = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Button); AccessibilityServer.UpdateSetName(controller, "Context controller"); AccessibilityServer.UpdateSetShortcut(controller, "Ctrl+C"); AccessibilityServer.UpdateSetShortcut(button.GetAccessibilityElement(), "Before first query");
            AccessibilityServer.UpdateAddAction(controller, AAction.Custom, (int id) => Command(id)); foreach (var id in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 99 }) AccessibilityServer.UpdateAddCustomAction(controller, id, "command-" + id);
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/context.py")); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ => { measuring = clientDone && complete && post >= 16 && (post < 80 || post >= 96 && post < 160); if (measuring) before = GC.GetAllocatedBytesForCurrentThread(); if (complete && post >= 80) button.AccessibilityName = (post & 1) == 0 ? "Move one" : "Move two"; };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (post < 80) idle += bytes; else active += bytes; }
            if (!captured && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } capture) { captured = true; Directory.CreateDirectory(capture); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(capture, "context-navigation-" + backend + ".png")); }
            if (!complete || !clientDone) return;
            if (post == 16) idleReads = button.Reads;
            if (post == 79) Check(button.Reads == idleReads, "Unchanged frames do not invoke contextual hooks.");
            if (++post == 160) Check(idle == 0 && active == 0, $"Prepared context idle/active managed bytes {idle}/{active}.");
        }
        var started = Stopwatch.GetTimestamp();
        try { Check(Engine.Run((_, _) => { if (!clientDone && client?.HasExited == true) { Console.Write(output!.GetAwaiter().GetResult()); Console.Write(error!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && complete, "Independent native context client completes."); clientDone = true; } if (clientDone && post >= 160) window.Tree!.Quit(); if (Stopwatch.GetElapsedTime(started).TotalSeconds > 70) throw new TimeoutException("Native context client timeout."); }, window) == 0, "Context host closes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); }
        Check(victim.IsDisposed && !AccessibilityServer.HasElement(controller), "Context callback retirement and teardown release identities.");
        RunFailure(); RunShortcutFailure(); Console.WriteLine($"PASS {_checks} native contextual/help/shortcut contracts on {backend}; 64 idle/64 active {idle}/{active} managed bytes.");
    }
    private static void RunFailure()
    {
        var window = new Window { Size = new(180, 100) }; var button = new ContextButton { DuringHelp = () => throw new InvalidOperationException("Injected context failure") }; window.AddChild(button); var failed = false;
        try { Engine.Run((_, tick) => { if (tick > 10) throw new Exception("Context failure missing"); }, window); }
        catch (AggregateException errors) { failed = errors.Flatten().InnerExceptions.Any(e => e is InvalidOperationException { Message: "Injected context failure" }); }
        Check(failed && !AccessibilityServer.IsSupported(), "Context callback errors propagate and the display retires.");
        var fresh = new Window { Size = new(120, 80) }; fresh.Ready += _ => fresh.Tree!.Quit(); Check(Engine.Run(fresh) == 0, "Native display reopens after context failure.");
    }
    private static void RunShortcutFailure()
    {
        using var input = new BadBinding(); using var shortcut = new Shortcut { Events = [input] };
        var window = new Window { Size = new(240, 120) }; window.AddChild(new Button { Text = "Failing shortcut", Shortcut = shortcut, ShortcutInTooltip = false, Size = new(220, 80) });
        Process? client = null; Task<string>? output = null, error = null;
        window.Ready += _ => { var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/context.py")); start.ArgumentList.Add("--binding-failure"); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync(); };
        var started = Stopwatch.GetTimestamp(); var failed = false;
        try { Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 15) throw new TimeoutException("Binding failure client timeout"); }, window); }
        catch (AggregateException errors) { failed = errors.Flatten().InnerExceptions.Any(e => e is InvalidOperationException { Message: "Injected shortcut description failure" }); }
        finally { if (client is { HasExited: false } && !client.WaitForExit(5000)) client.Kill(true); if (output is not null) Console.Write(output.GetAwaiter().GetResult()); if (error is not null) Console.Write(error.GetAwaiter().GetResult()); Check(failed && client?.ExitCode == 0, "Shortcut description failure reaches the native wire and owner."); client?.Dispose(); }
        var fresh = new Window { Size = new(120, 80) }; fresh.Ready += _ => fresh.Tree!.Quit(); Check(Engine.Run(fresh) == 0, "Display reopens after a binding description failure.");
    }

}
