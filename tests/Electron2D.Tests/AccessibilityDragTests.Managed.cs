using Electron2D;
using Path = System.IO.Path;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;

internal static partial class AccessibilityDragTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private sealed class Source : Control
    {
        internal readonly DragPayload<string> Payload = new("Package");
        internal Control? Preview;
        internal Action? DuringGet;
        internal bool Empty;
        internal int Begins, Ends, Unhandled;
        internal bool Successful;
        protected override DragPayload? OnGetDragData(Vector2 atPosition)
        {
            Check(atPosition == AccessibilityDragPositionForTest && GetViewport()!.IsGUIDragging(), "Non-positional source runs during prepared drag ownership.");
            Preview = new Label("Package") { Size = new(100, 28) }; SetDragPreview(Preview); GetViewport()!.SetGUIDragDescription("Package"); DuringGet?.Invoke(); return Empty ? null : Payload;
        }
        protected override void OnUnhandledInput(InputEvent inputEvent) => Unhandled++;
        protected override void OnNotification(int what)
        { base.OnNotification(what); if (what == NotificationDragBegin) Begins++; if (what == NotificationDragEnd) { Ends++; Successful = IsDragSuccessful(); } }
    }
    private sealed class Target : Control
    {
        internal bool Accept = true, Fail;
        internal int Drops;
        internal Action? DuringCan;
        protected override bool OnCanDropData(Vector2 atPosition, DragPayload payload)
        { Check(atPosition == AccessibilityDragPositionForTest, "Non-positional target receives the sentinel."); DuringCan?.Invoke(); return Accept && payload is DragPayload<string>; }
        protected override void OnDropData(Vector2 atPosition, DragPayload payload)
        { Check(GetViewport()!.IsGUIDragging() && atPosition == AccessibilityDragPositionForTest, "Drop sees the live transaction."); Drops++; if (Fail) throw new InvalidOperationException("Injected drop failure"); }
    }
    private static readonly Vector2 AccessibilityDragPositionForTest = new(float.PositiveInfinity, float.PositiveInfinity);
    internal static void RunManaged()
    {
        Check(InputMap.HasAction("ui_accessibility_drag_and_drop") && ProjectSettings.Get(ProjectSettings.InputUIAccessibilityDragAndDrop).Bindings.Length == 0, "Permanent keyboard drag action starts unbound.");
        Reject<ArgumentNullException>(() => AccessibilityServer.UpdateAddAction(default, AAction.Custom, (Action<int>)null!));
        Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateAddAction(default, AAction.Click, (int _) => { }));
        Reject<ArgumentNullException>(() => AccessibilityServer.UpdateAddCustomAction(default, 1, null!));
        Reject<ArgumentNullException>(() => AccessibilityServer.UpdateSetExtraInfo(default, null!));
        VerifyTransactions(); VerifyTextSources(); VerifyBinding();
        Console.WriteLine($"PASS {_checks} managed accessible drag/drop, sentinel, ownership, keyboard and text contracts.");
    }
    private static void VerifyTransactions()
    {
        var host = new SubViewport { Size = new(640, 480) }; var source = new Source { MouseFilter = MouseFilter.Ignore, FocusMode = FocusMode.All }; var target = new Target { MouseFilter = MouseFilter.Ignore };
        host.AddChild(source); host.AddChild(target); using var tree = new SceneTree(host);
        Reject<InvalidOperationException>(() => target.AccessibilityDrop()); source.AccessibilityDrag();
        Check(host.IsGUIDragging() && ReferenceEquals(host.GetGUIDragData(), source.Payload), "Public source starts a typed drag without pointer capture.");
        Reject<InvalidOperationException>(() => source.AccessibilityDrag()); target.AccessibilityDrop();
        Check(!host.IsGUIDragging() && host.IsGUIDragSuccessful() && source.Successful && source.Preview!.IsDisposed && source.Begins == 1 && source.Ends == 1 && target.Drops == 1, "Accepted ignored-mouse target completes once and retires preview.");
        target.Accept = false; source.AccessibilityDrag(); target.AccessibilityDrop(); Check(!host.IsGUIDragSuccessful() && target.Drops == 1 && source.Preview!.IsDisposed, "Rejected target ends without consuming data.");
        target.Accept = true; target.Fail = true; source.AccessibilityDrag(); Reject<AggregateException>(() => target.AccessibilityDrop()); Check(!host.IsGUIDragging() && !host.IsGUIDragSuccessful() && source.Preview!.IsDisposed, "Drop failure finalizes without success.");
        target.Fail = false; var retiringTarget = new Target(); host.AddChild(retiringTarget); retiringTarget.DuringCan = retiringTarget.Dispose; source.AccessibilityDrag(); retiringTarget.AccessibilityDrop(); Check(!host.IsGUIDragging() && !host.IsGUIDragSuccessful() && source.Preview!.IsDisposed, "Target disposal during acceptance still finalizes the transaction.");
        target.Fail = false; source.Empty = true; source.AccessibilityDrag(); Check(!host.IsGUIDragging() && source.Preview!.IsDisposed && host.GetGUIDragDescription() != "Package", "Null data rolls back prepared preview and description."); source.Empty = false;
        source.DuringGet = () => { source.Preview!.Disposed += _ => throw new InvalidOperationException("Injected preview failure"); throw new InvalidOperationException("Injected source failure"); };
        try { source.AccessibilityDrag(); throw new InvalidOperationException("Missing aggregate"); } catch (AggregateException errors) { Check(errors.Flatten().InnerExceptions.Count == 2 && source.Preview!.IsDisposed && !host.IsGUIDragging(), "Original producer and preview failure both survive rollback."); }
        source.DuringGet = () => host.RemoveChild(source); source.AccessibilityDrag(); Check(!host.IsGUIDragging() && source.Preview!.IsDisposed, "Source departure during preparation rolls back.");
        Reject<InvalidOperationException>(() => source.AccessibilityDrag()); source.Dispose();
        var parent = new Target { MouseFilter = MouseFilter.Stop }; var child = new Target { Accept = false, MouseFilter = MouseFilter.Pass }; parent.AddChild(child); host.AddChild(parent); var fallback = new Source(); host.AddChild(fallback); fallback.AccessibilityDrag(); child.AccessibilityDrop(); Check(parent.Drops == 1, "Accessible drop retains ancestor fallback.");
    }
    private static void VerifyTextSources()
    {
        var host = new SubViewport { Size = new(640, 480) }; var line = new LineEdit { Text = "A😀B" }; var target = new LineEdit { Text = "XY" }; var text = new TextEdit { Text = "red\nblue" }; var code = new CodeEdit { Text = "[]" };
        foreach (var node in new Control[] { line, target, text, code }) host.AddChild(node); using var tree = new SceneTree(host);
        line.Select(1, 2); target.CaretColumn = 1; line.AccessibilityDrag(); Check(host.GetGUIDragData() is DragPayload<string> { Value: "😀" }, "Keyboard selection produces a scalar text payload."); target.AccessibilityDrop();
        Check(line.Text == "AB" && target.Text == "X😀Y", "Accessible line drop moves a complete scalar selection to the caret.");
        line.Text = "A😀B"; line.Select(1, 2); line.CaretColumn = 3; line.AccessibilityDrag(); line.AccessibilityDrop(); Check(line.Text == "AB😀", "Self drop preserves scalar offsets and moves once.");
        text.Select(0, 0, 0, 3); code.SetCaretColumn(1); text.AccessibilityDrag(); code.AccessibilityDrop(); Check(text.Text == "\nblue" && code.Text == "[red]", "Multiline selection moves into inherited CodeEdit caret behavior."); code.Undo(); Check(code.Text == "[]", "Drop is one ordinary undo operation.");
        line.Text = "hidden"; line.SelectAll(); line.Secret = true; line.AccessibilityDrag(); Check(!host.IsGUIDragging(), "Secret source never supplies text."); line.Secret = false;
        line.Editable = false; line.SelectAll(); target.CaretColumn = 0; line.AccessibilityDrag(); target.AccessibilityDrop(); Check(line.Text == "hidden" && target.Text.StartsWith("hidden", StringComparison.Ordinal), "Read-only source copies without deleting data.");
        line.Editable = true; line.Text = "ABCDE"; line.SelectAll(); target.MaxLength = 2; target.Text = "X"; target.CaretColumn = 1; line.AccessibilityDrag(); target.AccessibilityDrop();
        Check(line.Text == "ABCDE" && target.Text == "X" && !host.IsGUIDragSuccessful(), $"Capacity rejection preserves the whole source payload: source={line.Text}, target={target.Text}, success={host.IsGUIDragSuccessful()}.");
        target.SetDragForwarding(null, (_, _) => true, null); line.SelectAll(); line.AccessibilityDrag(); Reject<AggregateException>(() => target.AccessibilityDrop()); Check(line.Text == "ABCDE" && target.Text == "X" && !host.IsGUIDragSuccessful(), "Forwarded acceptance cannot bypass intrinsic capacity validation."); target.SetDragForwarding(null, null, null);
        text.Editable = false; text.SetDragForwarding(null, (_, _) => true, null); line.SelectAll(); line.AccessibilityDrag(); Reject<AggregateException>(() => text.AccessibilityDrop()); Check(line.Text == "ABCDE" && !host.IsGUIDragSuccessful(), "Forwarded acceptance cannot mutate a read-only editor or consume its source."); text.SetDragForwarding(null, null, null);
    }
    private static void VerifyBinding()
    {
        var old = ProjectSettings.Get(ProjectSettings.InputUIAccessibilityDragAndDrop);
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-drag-settings-" + Guid.NewGuid().ToString("N")); var project = Path.Combine(folder, "project"); var user = Path.Combine(folder, "user"); Directory.CreateDirectory(project); Directory.CreateDirectory(user);
        try
        {
            using (var settings = new ProjectSettingsRegistry(project, user))
            { settings.Set(ProjectSettings.InputUIAccessibilityDragAndDrop, new InputActionSettings { Bindings = [new() { Kind = InputBindingKind.Key, Keycode = Key.F8 }] }); settings.Save(); }
            using var loaded = new ProjectSettingsRegistry(project, user); loaded.Load();
            var restored = loaded.Get(ProjectSettings.InputUIAccessibilityDragAndDrop); Check(restored.Bindings is [{ Keycode: Key.F8 }], "Permanent keyboard drag action survives project-file restoration.");
            Reject<InvalidOperationException>(() => loaded.Unregister(ProjectSettings.InputUIAccessibilityDragAndDrop));
            ProjectSettings.Set(ProjectSettings.InputUIAccessibilityDragAndDrop, restored); InputMap.LoadFromProjectSettings();
            var host = new SubViewport { Size = new(640, 480) }; var source = new Source { FocusMode = FocusMode.All }; var target = new Target { FocusMode = FocusMode.All }; host.AddChild(source); host.AddChild(target); using var tree = new SceneTree(host);
            source.GrabFocus(); using var key = new InputEventKey { Keycode = Key.F8, Pressed = true }; host.PushInput(key, true); Check(host.IsGUIDragging() && source.Unhandled == 0, "Configured key starts the focused producer after GUI delivery.");
            target.GrabFocus(); host.PushInput(key, true); Check(target.Drops == 1 && host.IsGUIDragSuccessful(), "Configured key drops on the current focus.");
            source.AccessibilityDrag(); using var release = new InputEventAction { Action = "ui_cancel", Pressed = false }; host.PushInput(release, true); Check(host.IsGUIDragging(), "Cancel release does not cancel an active drag."); using var cancel = new InputEventAction { Action = "ui_cancel", Pressed = true }; host.PushInput(cancel, true); Check(!host.IsGUIDragging(), "Cancel press retires keyboard drag.");
        }
        finally { ProjectSettings.Set(ProjectSettings.InputUIAccessibilityDragAndDrop, old); InputMap.LoadFromProjectSettings(); Directory.Delete(folder, true); }
    }
}
