using System.Diagnostics;
using Electron2D;
using Live = Electron2D.AccessibilityServer.AccessibilityLiveMode;
using PopupKind = Electron2D.AccessibilityServer.AccessibilityPopupType;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
using Flag = Electron2D.AccessibilityServer.AccessibilityFlags;
using Path = System.IO.Path;

internal static partial class AccessibilityLiveTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunNative(string backend)
    {
        if (backend.EndsWith("-native", StringComparison.Ordinal)) { RunPopupSurfaces(backend[..^7]); return; }
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Native Live Probe"); ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        VerifyStoredPolicy();
        var window = new Window { Size = new(960, 660), GUIEmbedSubwindows = true };
        var status = new Label { Text = "Status ready", AccessibilityName = "Status label", AccessibilityLive = Live.Polite, Position = new(8, 8), Size = new(350, 34) };
        var alert = new Label { Text = "Alert ready", AccessibilityName = "Alert label", AccessibilityLive = Live.Assertive, Position = new(8, 50), Size = new(350, 34) };
        var silent = new Label { Text = "Off ready", AccessibilityName = "Silent label", Position = new(8, 90), Size = new(350, 34) };
        var hidden = new Label { Text = "Hidden ready", AccessibilityName = "Hidden label", AccessibilityLive = Live.Polite, Position = new(8, 130), Size = new(350, 34) };
        var numeric = new HSlider { AccessibilityName = "Live number", AccessibilityLive = Live.Polite, MinValue = 0, MaxValue = 10, Step = .5, Position = new(8, 170), Size = new(300, 28) };
        var hint = new Button("Hint owner") { AccessibilityName = "Hint owner", TooltipText = "Actual tooltip text", MouseFilter = MouseFilter.Ignore, Position = new(8, 215), Size = new(250, 36) };
        var line = new LineEdit { AccessibilityName = "Editable line", Text = "Editable text", Position = new(400, 8), Size = new(360, 36) };
        var secret = new LineEdit { AccessibilityName = "Secret line", Text = "original secret", Secret = true, AccessibilityLive = Live.Assertive, Position = new(400, 54), Size = new(360, 36) };
        var edit = new TextEdit { AccessibilityName = "Editor", Text = "Editor text", Position = new(400, 100), Size = new(360, 120) };
        var rich = new RichTextLabel { AccessibilityName = "Rich", Text = "Rich text", Position = new(400, 230), Size = new(360, 60), SelectionEnabled = true, ContextMenuEnabled = true };
        var choices = new OptionButton { AccessibilityName = "Choices", Position = new(8, 275), Size = new(300, 36) };
        for (var i = 0; i < 5; i++) choices.AddItem("Choice " + i, 100 + i);
        choices.SetItemDisabled(3, true); choices.GetPopup().Title = "Choice menu"; choices.Select(0);
        var sub = new PopupMenu { Name = "CommandsSubmenu", Title = "Submenu" }; sub.AddItem("Nested command", 700);
        var command = new MenuButton("Commands") { AccessibilityName = "Commands", Position = new(8, 325), Size = new(300, 36) };
        var menu = command.GetPopup(); menu.Title = "Command menu"; menu.HideOnItemSelection = false; menu.HideOnCheckableItemSelection = false;
        menu.AddCheckItem("Checked command", 10); menu.SetItemChecked(0, true); menu.AddRadioCheckItem("Radio command", 11); menu.SetItemChecked(1, true);
        menu.AddSubmenuNodeItem("Nested", sub, 12); menu.AddSeparator("Group"); menu.AddItem("Disabled command", 13); menu.SetItemDisabled(4, true);
        var next = new Button("Next live phase") { AccessibilityName = "Next live phase", Position = new(8, 585), Size = new(280, 36) };
        foreach (var node in new Control[] { status, alert, silent, hidden, numeric, hint, line, secret, edit, rich, choices, command, next }) window.AddChild(node);
        Check(!line.IsMenuVisible(), "Visibility query does not create or show a menu.");
        var lineMenu = line.GetMenu(); Check(ReferenceEquals(lineMenu, line.GetMenu()), "Line menu has stable borrowed identity.");
        Check(lineMenu.GetItemSubmenuNode(lineMenu.GetItemIndex((int)TextMenuAction.SubmenuTextDirection)) is not null && lineMenu.GetItemSubmenuNode(lineMenu.GetItemIndex((int)TextMenuAction.SubmenuInsertUCC)) is not null, "Both typed submenu identifiers address real owned menus.");
        lineMenu.Title = "Line context"; edit.GetMenu().Title = "Editor context"; rich.GetMenu().Title = "Rich context";
        edit.GetMenu().AddItem("Custom editor command", 1001); rich.GetMenu().RemoveItem(rich.GetMenu().GetItemIndex((int)RichTextLabel.MenuItems.Copy)); rich.GetMenu().AddItem("Custom rich command", 1002);
        lineMenu.AddItem("Custom line command", 1000); var custom = 0; edit.GetMenu().IDPressed += id => { if (id == 1001) custom++; }; rich.GetMenu().IDPressed += id => { if (id == 1002) custom++; }; lineMenu.IDPressed += id => { if (id == 1000) custom++; };
        Reject<InvalidOperationException>(() => lineMenu.Dispose()); Reject<ArgumentOutOfRangeException>(() => status.AccessibilityLive = (Live)9);
        using (var copy = (Label)status.Duplicate()) Check(copy.AccessibilityLive == Live.Polite, "Duplication preserves the authored live policy.");
        var phase = 0; var lineChanges = 0; var selected = 0; var commands = 0; var genericActions = 0;
        line.TextChanged += _ => lineChanges++; choices.ItemSelected += _ => selected++; menu.IDPressed += _ => commands++;
        RID manual = default, retiring = default, pending = default, inherited = default, suppressed = default, modeRegion = default, modeChild = default;
        next.Pressed += () =>
        {
            phase++;
            if (phase == 1)
            {
                status.Text = "Status intermediate"; status.Text = "Status final"; alert.Text = "Alert final"; numeric.Value = 7.5;
                silent.Text = "SILENT MUST NOT ANNOUNCE"; hidden.Visible = false; hidden.Text = "HIDDEN MUST NOT ANNOUNCE";
                secret.Text = "SECRET MUST NOT ANNOUNCE"; AccessibilityServer.UpdateSetValue(manual, "Manual final"); AccessibilityServer.UpdateSetValue(inherited, "Inherited final"); AccessibilityServer.UpdateSetValue(suppressed, "INHERITED OFF MUST NOT ANNOUNCE");
                AccessibilityServer.UpdateSetValue(modeChild, "GHOST CANCELLED CONTENT"); AccessibilityServer.UpdateSetLive(modeRegion, Live.Off); AccessibilityServer.UpdateSetLive(modeRegion, Live.Polite);
                AccessibilityServer.UpdateSetName(pending, "RETIRED MUST NOT ANNOUNCE"); AccessibilityServer.FreeElement(pending);
            }
            if (phase == 2)
            {
                Check(line.Text == "" && lineChanges == 1 && custom == 3 && line.TextDirection == TextDirection.RTL, "Menu editing and custom entries execute without erasing customization.");
                Check(choices.Selected == 2 && selected == 1 && commands >= 1 && genericActions == 3, "Popup activation reuses ordinary application events.");
                AccessibilityServer.UpdateSetLive(status.GetAccessibilityElement(), Live.Off); status.Text = "OVERRIDE MUST NOT ANNOUNCE";
                line.ContextMenuEnabled = edit.ContextMenuEnabled = rich.ContextMenuEnabled = false;
                choices.GetPopup().SetItemText(1, "Choice renamed");
            }
            if (phase == 3)
            {
                status.AccessibilityLive = Live.Assertive; status.Text = "Status restored";
                line.Text = "Read only"; line.Editable = false; line.ContextMenuEnabled = true;
            }
            if (phase == 4) { choices.RemoveItem(0); }
            if (phase == 5) { Check(!AccessibilityServer.HasElement(retiring), "Reentrant tooltip callback retires its producer."); }
            if (phase == 6)
            {
                Check(line.Text == "Read only" && lineChanges == 1 && choices.Selected == 0, "Read-only menu guards and shifted item identity preserve source state.");
                menu.Hide(); choices.GetPopup().Hide(); lineMenu.Hide(); edit.GetMenu().Hide(); rich.GetMenu().Hide();
            }
        };
        Process? client = null; Task<string>? output = null, error = null; using var texture = window.GetTexture();
        var complete = false; var post = 0; var frames = 0; var measuring = false; var captured = false; long before = 0, idle = 0, active = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "Actual native renderer and bridge are available.");
            manual = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.StaticText); AccessibilityServer.UpdateSetName(manual, "Manual live"); AccessibilityServer.UpdateSetLive(manual, Live.Polite);
            var generic = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Button); AccessibilityServer.UpdateSetName(generic, "Generic popup"); AccessibilityServer.UpdateSetBounds(generic, new(400, 350, 180, 30)); AccessibilityServer.UpdateSetPopupType(generic, PopupKind.Tree); AccessibilityServer.UpdateSetTooltip(generic, "Generic hint");
            foreach (var action in new[] { AAction.ShowTooltip, AAction.HideTooltip, AAction.ShowContextMenu }) AccessibilityServer.UpdateAddAction(generic, action, () => genericActions++);
            var dialog = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Button); AccessibilityServer.UpdateSetName(dialog, "Dialog popup"); AccessibilityServer.UpdateSetPopupType(dialog, PopupKind.Dialog);
            retiring = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Button); AccessibilityServer.UpdateSetName(retiring, "Retiring tooltip"); AccessibilityServer.UpdateAddAction(retiring, AAction.ShowTooltip, () => AccessibilityServer.FreeElement(retiring));
            var region = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Container); AccessibilityServer.UpdateSetName(region, "Inherited region"); AccessibilityServer.UpdateSetLive(region, Live.Polite);
            inherited = AccessibilityServer.CreateSubElement(region, Role.StaticText); AccessibilityServer.UpdateSetValue(inherited, "Inherited ready");
            suppressed = AccessibilityServer.CreateSubElement(region, Role.StaticText); AccessibilityServer.UpdateSetLive(suppressed, Live.Off);
            modeRegion = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Container); AccessibilityServer.UpdateSetName(modeRegion, "Mode region"); AccessibilityServer.UpdateSetLive(modeRegion, Live.Polite); modeChild = AccessibilityServer.CreateSubElement(modeRegion, Role.StaticText);
            pending = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.StaticText); AccessibilityServer.UpdateSetLive(pending, Live.Assertive);
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetLive(manual, (Live)3)); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetPopupType(generic, (PopupKind)4)); Reject<ArgumentNullException>(() => AccessibilityServer.UpdateSetTooltip(generic, null!));
            Task.Run(() => Reject<InvalidOperationException>(() => AccessibilityServer.UpdateSetLive(manual, Live.Off))).GetAwaiter().GetResult();
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/live.py")); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && post >= 16 && (post < 80 || post >= 96); if (measuring) before = GC.GetAllocatedBytesForCurrentThread();
                if (complete && post >= 80)
                {
                    var alternate = (post & 1) == 0; status.Text = alternate ? "Active status one" : "Active status two"; numeric.Value = alternate ? 1 : 2;
                    menu.SetFocusedItem(alternate ? 0 : 1); AccessibilityServer.UpdateSetValue(manual, alternate ? "Manual one" : "Manual two");
                }
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (post < 80) idle += bytes; else active += bytes; }
            if (!captured && (line.IsMenuVisible() || choices.GetPopup().Visible) && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } capture)
            { captured = true; Directory.CreateDirectory(capture); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(capture, "live-popup-" + backend + ".png")); }
            if (!complete) return; post++;
            if (post == 160) { Check(idle == 0 && active == 0, $"Live semantic idle={idle}, active={active} managed bytes."); window.Tree!.Quit(); }
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) => { frames++; if (!complete && client?.HasExited == true) { Console.Write(output!.GetAwaiter().GetResult()); Console.Write(error!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && phase == 6, "Independent live client completed all phases."); complete = true; } if (Stopwatch.GetElapsedTime(started).TotalSeconds > 90) throw new TimeoutException("Native live client timed out."); }, window) == 0, "Live host completes.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); }
        RunCallbackFailure(); Console.WriteLine($"PASS {_checks} native live/popup/tooltip contracts on {backend}; {frames} frames, 64 idle/64 active {idle}/{active} managed bytes.");
    }
    private static void VerifyStoredPolicy()
    {
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-live-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var field = new LineEdit { Name = "Stored", Text = "Stored text", AccessibilityLive = Live.Assertive, ContextMenuEnabled = false };
            field.GetMenu().AddItem("Runtime custom entry", 1000); var path = Path.Combine(folder, "live.e2dscene"); ResourceSaver.SaveScene(field, path);
            using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var copy = (LineEdit)scene.Instantiate();
            Check(copy.Text == "Stored text" && copy.AccessibilityLive == Live.Assertive && !copy.ContextMenuEnabled && !copy.IsMenuVisible(), "Archive restores authored policy without live popup state.");
            Check(copy.GetMenu().GetItemIndex(1000) == -1 && !copy.GetAccessibilityElement().IsValid(), "Internal runtime menu customization and native identities stay out of scene storage.");
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void RunCallbackFailure()
    {
        var window = new Window { Size = new(120, 80) }; Process? client = null; Task<string>? output = null, error = null; RID producer = default;
        window.Ready += _ =>
        {
            producer = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Button); AccessibilityServer.UpdateSetName(producer, "Throwing tooltip"); AccessibilityServer.UpdateAddAction(producer, AAction.ShowTooltip, () => throw new InvalidOperationException("Injected native tooltip failure"));
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/live.py")); start.ArgumentList.Add("--failure"); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
        };
        var started = Stopwatch.GetTimestamp(); var failed = false;
        try { Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 15) throw new TimeoutException("Tooltip failure client timed out."); }, window); }
        catch (AggregateException errors) { var failures = errors.Flatten().InnerExceptions; failed = failures.Count == 1 && failures[0] is InvalidOperationException { Message: "Injected native tooltip failure" }; }
        finally
        {
            if (client is { HasExited: false } && !client.WaitForExit(5000)) client.Kill(true); if (output is not null) Console.Write(output.GetAwaiter().GetResult()); if (error is not null) Console.Write(error.GetAwaiter().GetResult()); Check(failed && client?.ExitCode == 0 && !AccessibilityServer.HasElement(producer), "Tooltip failure replies, reaches the owner and retires the producer."); client?.Dispose();
        }
        var fresh = new Window { Size = new(100, 60) }; fresh.Ready += _ => { Check(AccessibilityServer.IsSupported(), "Display reopens after tooltip failure."); fresh.Tree!.Quit(); }; Check(Engine.Run(fresh) == 0, "Fresh display closes cleanly.");
    }
}
