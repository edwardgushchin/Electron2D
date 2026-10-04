using Electron2D;

internal static class BaseButtonTests
{
    internal static void Run()
    {
        InputMap.LoadFromProjectSettings();
        VerifyStatesAndEdges(); VerifyTouchAndActions(); VerifyGroups(); VerifyFailureAndReentry();
        VerifyShortcutsAndTooltip(); VerifyLifecycleAndPacking(); VerifyWarmInput(); VerifyHoverBuffers();
        Console.WriteLine("Base buttons verify input edges, draw states, touch/action/shortcuts, groups, hooks/errors/reentry, lifecycle, packing and warmed zero allocations.");
    }
    private static void VerifyStatesAndEdges()
    {
        using var b = new Probe { Size = new(30, 20) }; var order = new List<string>(); b.Log = order;
        b.ButtonDown += () => order.Add("down"); b.ButtonUp += () => order.Add("up"); b.Pressed += () => order.Add("pressed"); b.Toggled += v => order.Add("toggle:" + v);
        Check(typeof(BaseButton).IsAbstract && !b.Disabled && !b.ToggleMode && !b.ButtonPressed && b.FocusMode == FocusMode.All && b.ActionMode == ButtonActionMode.ButtonRelease && b.ButtonMask == MouseButtonMask.Left && b.ShortcutFeedback && b.ShortcutInTooltip && !b.KeepPressedOutside && b.ButtonGroup is null && b.Shortcut is null, "Base defaults are the source defaults.");
        using var down = Mouse(true); using var up = Mouse(false); using var outside = new InputEventMouseMotion { Position = new(40, 10) };
        b.Send(down); Check(order.Count == 0, "A raw mouse press requires hover."); b.Enter(); b.Send(down);
        Check(b.ButtonPressed && b.GetDrawMode() == BaseButton.DrawMode.Pressed && order.SequenceEqual(["down"]), "Release action starts a momentary press without activation.");
        b.Send(outside); Check(b.ButtonPressed && b.GetDrawMode() == BaseButton.DrawMode.Normal, "Momentary pressed reports the attempt while outside appearance is normal.");
        b.KeepPressedOutside = true; Check(b.GetDrawMode() == BaseButton.DrawMode.Pressed, "KeepPressedOutside changes appearance only."); b.Send(up);
        Check(order.SequenceEqual(["down", "up"]) && !b.ButtonPressed, "Releasing outside never activates even with pressed appearance.");
        order.Clear(); b.Send(down); b.Send(up);
        Check(order.SequenceEqual(["down", "hook:pressed", "pressed", "up"]), "Release activation orders down, hook, pressed, up.");
        b.ToggleMode = true; b.ActionMode = ButtonActionMode.ButtonPress; order.Clear(); b.Send(down);
        Check(b.ButtonPressed && b.GetDrawMode() == BaseButton.DrawMode.HoverPressed && order.SequenceEqual(["down", "hook:True", "toggle:True", "hook:pressed", "pressed"]), "Press-mode toggle commits immediately, releases its press attempt and retains hover decoration.");
        b.Send(up); Check(order[^1] == "up", "Press-mode toggle still balances the eventual release.");
        order.Clear(); b.ButtonPressed = false; Check(order.SequenceEqual(["hook:False", "toggle:False"]), "Programmatic toggle has no Pressed event.");
        order.Clear(); b.SetPressedNoSignal(true); Check(order.Count == 0 && b.ButtonPressed, "Silent state mutation bypasses all hooks and signals.");
        b.ActionMode = ButtonActionMode.ButtonRelease; b.Send(down); Check(b.GetDrawMode() == BaseButton.DrawMode.Normal, "Holding a selected toggle previews its inverse."); b.Send(up); Check(!b.ButtonPressed, "Release toggles off.");
        b.ActionMode = (ButtonActionMode)42; order.Clear(); b.Send(down); b.Send(up); Check(order.SequenceEqual(["down", "up"]), "Raw unknown action modes retain down/up without activating.");
        b.ToggleMode = false; b.ActionMode = ButtonActionMode.ButtonRelease; b.ButtonMask = MouseButtonMask.Right;
        order.Clear(); b.Send(down); b.Send(up); Check(order.Count == 0, "Unmasked mouse buttons are ignored.");
        using var rightDown = Mouse(true, MouseButton.Right); using var rightUp = Mouse(false, MouseButton.Right); b.Send(rightDown); b.Send(rightUp); Check(order.Contains("pressed"), "The selected non-left mouse button activates.");
        b.Disabled = true; Check(b.GetDrawMode() == BaseButton.DrawMode.Disabled && b.IsHovered(), "Disabled visual state takes precedence without clearing hover.");
    }
    private static void VerifyTouchAndActions()
    {
        using var b = new Probe { Size = new(20, 20), ToggleMode = true }; var pressed = 0; b.Pressed += () => pressed++;
        using var touch = new InputEventScreenTouch { Index = 4, Position = new(5, 5), Pressed = true }; using var other = new InputEventScreenTouch { Index = 5, Position = new(5, 5), Pressed = false };
        b.Send(touch); b.Send(other); Check(!b.ButtonPressed && pressed == 0, "An unrelated touch cannot release the active contact.");
        using var drag = new InputEventScreenDrag { Index = 4, Position = new(30, 5) }; b.Send(drag); touch.Pressed = false; b.Send(touch); Check(pressed == 0, "Dragging touch outside suppresses activation.");
        touch.Pressed = true; b.Send(touch); touch.Pressed = false; b.Send(touch); Check(b.ButtonPressed && pressed == 1, "A matching touch release activates without mouse hover.");
        touch.Device = InputEvent.DeviceIdEmulation; touch.Pressed = true; b.Send(touch); touch.Pressed = false; b.Send(touch); Check(pressed == 1, "Emulated touch does not double-activate mouse input.");
        using var action = new InputEventAction { Action = "ui_accept", Pressed = true }; b.Send(action); action.Pressed = false; b.Send(action); Check(!b.ButtonPressed && pressed == 2, "Synthetic ui_accept has the same press/release semantics without hover.");
        using var key = new InputEventKey { Keycode = Key.Space, Pressed = true, Echo = true }; b.Send(key); Check(pressed == 2, "Keyboard repeat echoes do not activate.");
        b.ActionMode = ButtonActionMode.ButtonPress; touch.Device = 0; touch.Pressed = true; b.Send(touch); touch.Index = 9; b.Send(touch);
        Check(pressed == 4, "Press-mode touch releases its contact immediately, accepting a subsequent tap even if the first release was consumed.");
    }
    private static void VerifyGroups()
    {
        using var group = new ButtonGroup(); using var a = new Probe { ToggleMode = true, ButtonGroup = group }; using var b = new Probe { ToggleMode = true, ButtonGroup = group };
        var events = new List<string>(); a.Toggled += v => events.Add("a:" + v); b.Toggled += v => events.Add("b:" + v); group.Pressed += button => events.Add(ReferenceEquals(button, a) ? "group:a" : "group:b");
        Check(group.ResourceLocalToScene && !group.AllowUnpress && group.GetButtons().Length == 2 && group.GetPressedButton() is null, "Groups are scene-local and start with no forced selection.");
        a.ButtonPressed = true; events.Clear(); b.ButtonPressed = true;
        Check(!a.ButtonPressed && b.ButtonPressed && ReferenceEquals(group.GetPressedButton(), b) && events.SequenceEqual(["a:False", "group:b", "b:True"]), "Selecting a group member unpresses peers before group and local signals.");
        using var down = new InputEventAction { Action = "ui_accept", Pressed = true }; using var up = new InputEventAction { Action = "ui_accept", Pressed = false };
        events.Clear(); b.Send(down); b.Send(up); Check(b.ButtonPressed && events.SequenceEqual(["group:b", "b:True"]), "Disallowed user unpress retains selection and still emits the activation toggle phase.");
        group.AllowUnpress = true; events.Clear(); b.Send(down); b.Send(up); Check(!b.ButtonPressed && events.SequenceEqual(["group:b", "b:False"]), "AllowUnpress permits empty selection while still signalling the active button.");
        a.SetPressedNoSignal(true); b.SetPressedNoSignal(true); Check(a.ButtonPressed && b.ButtonPressed, "Silent assignment deliberately bypasses group exclusivity.");
        group.AllowUnpress = false; b.ButtonPressed = false; a.ButtonPressed = false; Check(group.GetPressedButton() is null, "Explicit programmatic false can empty a disallow-unpress group.");
        var snapshot = group.GetButtons(); a.ButtonGroup = null; Check(snapshot.Length == 2 && group.GetButtons().Length == 1, "GetButtons is a copied membership snapshot.");
        using var copy = (ButtonGroup)group.Duplicate(); Check(copy.ResourceLocalToScene && !copy.AllowUnpress && copy.GetButtons().Length == 0, "Duplicating a group copies settings but no runtime members.");
        b.Dispose(); Check(group.GetButtons().Length == 0, "Disposal removes the node from its borrowed group.");
    }
    private static void VerifyFailureAndReentry()
    {
        using var b = new Probe { Size = new(20, 20) }; b.Enter(); using var down = Mouse(true); using var up = Mouse(false);
        var count = 0; Action fail = () => throw new ApplicationException("down failure"); b.ButtonDown += fail; b.Pressed += () => count++;
        Reject<AggregateException>(() => b.Send(down)); b.Send(up); Check(count == 1 && !b.ButtonPressed, "A failed down observer retains the committed press and permits release activation."); b.ButtonDown -= fail;
        b.PressedHook = () => throw new ApplicationException("hook failure"); b.Send(down); Reject<AggregateException>(() => b.Send(up));
        Check(count == 2 && !b.ButtonPressed, "A failed hook still delivers its event and finishes the release."); b.PressedHook = null;
        Action disable = () => b.Disabled = true; b.ButtonDown += disable; b.Send(down); Check(b.Disabled && !b.ButtonPressed && count == 2, "Disabling in ButtonDown cancels the stale outer activation."); b.ButtonDown -= disable; b.Disabled = false;
        using var group = new ButtonGroup(); using var a = new Probe { ToggleMode = true, ButtonGroup = group }; using var c = new Probe { ToggleMode = true, ButtonGroup = group }; using var d = new Probe { ToggleMode = true, ButtonGroup = group };
        a.SetPressedNoSignal(true); d.SetPressedNoSignal(true); a.Toggled += v => { if (!v) { c.ButtonPressed = false; d.ButtonPressed = true; } };
        c.ButtonPressed = true; Check(!c.ButtonPressed && d.ButtonPressed, "Peer callback reentry preserves the newer group winner instead of the stale outer unpress loop.");
        using var throwingGroup = new ButtonGroup(); using var x = new Probe { ToggleMode = true, ButtonGroup = throwingGroup }; using var y = new Probe { ToggleMode = true, ButtonGroup = throwingGroup };
        x.ButtonPressed = true; x.Toggled += _ => throw new ApplicationException("peer failure"); var yToggled = 0; y.Toggled += _ => yToggled++;
        Reject<AggregateException>(() => y.ButtonPressed = true); Check(!x.ButtonPressed && y.ButtonPressed && yToggled == 1, "A peer callback error cannot suppress the remaining group/local phases.");
        using var mode = new Probe { ToggleMode = true, ButtonPressed = true }; mode.Toggled += v => { if (!v) mode.ToggleMode = true; };
        mode.ToggleMode = false; Check(mode.ToggleMode && !mode.ButtonPressed, "A reentrant mode choice from the unpress callback supersedes the outer mode change.");
        using var doomed = new Probe { Size = new(20, 20) }; doomed.Enter(); doomed.ButtonDown += () => doomed.Dispose(); doomed.Send(down); Check(doomed.IsDisposed, "Disposal during down safely cancels remaining phases.");
    }
    private static void VerifyShortcutsAndTooltip()
    {
        using var key = new InputEventKey { Keycode = Key.F6 }; using var shortcut = new Shortcut { ResourceName = "Launch", Events = [key] };
        var root = new TestViewport(); var b = new Probe { Size = new(30, 20), Shortcut = shortcut }; root.AddChild(b); using var tree = new SceneTree(root);
        var count = 0; b.Pressed += () => count++; using var press = new InputEventKey { Keycode = Key.F6, Pressed = true }; root.PushInput(press, true);
        Check(count == 1 && root.Unhandled == 0 && b.GetDrawMode() == BaseButton.DrawMode.HoverPressed, "The actual shortcut stage handles input and starts visual feedback without GUI focus.");
        tree.ProcessFrame(ProjectSettings.GetWithOverride(ProjectSettings.ButtonShortcutFeedbackHighlightTime)); Check(b.GetDrawMode() == BaseButton.DrawMode.HoverPressed, "Feedback remains on its exact zero deadline.");
        tree.ProcessFrame(.001); Check(b.GetDrawMode() != BaseButton.DrawMode.HoverPressed, "Feedback ends after the unscaled deadline is crossed.");
        press.Echo = true; root.PushInput(press, true); press.Echo = false; b.Disabled = true; root.PushInput(press, true); Check(count == 1, "Echo and disabled shortcuts are ignored."); b.Disabled = false;
        b.ShortcutFeedback = false; root.PushInput(press, true); Check(count == 2 && b.GetDrawMode() != BaseButton.DrawMode.HoverPressed, "Disabling feedback retains executable shortcut activation.");
        using (var tooltip = b.Tooltip("Details")) Check(tooltip is Label label && label.Text == "Launch (F6)\nDetails" && label.ThemeTypeVariation == "TooltipLabel" && label.AutoTranslateMode == NodeAutoTranslateMode.Disabled, "Shortcut tooltip includes its translated name/events and the supplied description.");
        using (var tooltip = b.Tooltip("launch")) Check(tooltip is Label label && !label.Text.Contains('\n'), "Case-insensitive duplicate tooltip names are not repeated.");
        using var custom = new Control(); b.CustomTooltip = custom; Check(ReferenceEquals(b.Tooltip("raw"), custom) && b.TooltipArgument == "raw", "A custom tooltip sees the unannotated string and takes precedence."); b.CustomTooltip = null;
        b.ShortcutInTooltip = false; Check(b.Tooltip("Details") is null, "Tooltip annotation can be disabled independently."); b.Shortcut = null; Check(!b.ShortcutInputEnabled, "Removing the shortcut disables its input stage.");
    }
    private static void VerifyLifecycleAndPacking()
    {
        using var group = new ButtonGroup(); using var shortcut = new Shortcut(); var root = new Node(); var a = new Probe { Name = "A", ToggleMode = true, ButtonPressed = true, ButtonGroup = group, Shortcut = shortcut, ButtonMask = MouseButtonMask.Right, ActionMode = ButtonActionMode.ButtonPress, KeepPressedOutside = true }; var b = new Probe { Name = "B", ToggleMode = true, ButtonGroup = group };
        root.AddChild(a); root.AddChild(b); a.Owner = root; b.Owner = root; using var tree = new SceneTree(root); using var scene = new PackedScene(); scene.Pack(root);
        using var first = scene.Instantiate(); using var second = scene.Instantiate(); var fa = (Probe)first.GetNode("A"); var fb = (Probe)first.GetNode("B"); var sa = (Probe)second.GetNode("A");
        Check(fa.ButtonPressed && fa.ActionMode == ButtonActionMode.ButtonPress && fa.ButtonMask == MouseButtonMask.Right && fa.KeepPressedOutside && ReferenceEquals(fa.ButtonGroup, fb.ButtonGroup) && !ReferenceEquals(fa.ButtonGroup, sa.ButtonGroup) && !ReferenceEquals(fa.ButtonGroup, group), "Packed typed state preserves aliases within a scene and isolates scene-local groups between instances.");
        Check(fa.ButtonGroup!.GetButtons().Length == 2 && fa.Shortcut is not null && fa.FocusMode == FocusMode.All, "Instancing registers only its own members and restores full-focus defaults and shortcut state.");
        a.ActionMode = ButtonActionMode.ButtonRelease; a.ToggleMode = false; a.ButtonMask = MouseButtonMask.Left; a.Enter(); using var down = Mouse(true); a.Send(down);
        a.Visible = false; Check(!a.ButtonPressed && !a.IsHovered(), "Hide cancels momentary input and hover."); a.Visible = true; a.Enter(); a.Send(down); var ups = 0; a.ButtonUp += () => ups++; a.Notify(Control.NotificationFocusExit); Check(!a.ButtonPressed && ups == 1, "Focus exit balances a held input.");
        Reject<InvalidOperationException>(() => Task.Run(() => a.Disabled = true).GetAwaiter().GetResult());
        using var disposed = new ButtonGroup(); disposed.Dispose(); Reject<ObjectDisposedException>(() => a.ButtonGroup = disposed);
        Check(ReferenceEquals(a.ButtonGroup, group), "A rejected disposed group does not alter membership.");
        var capture = new CaptureProbe { DuringCapture = () => Reject<InvalidOperationException>(() => a.ButtonPressed = true) }; root.AddChild(capture); capture.Owner = root; scene.Pack(root);
        fa.ButtonGroup!.Dispose(); sa.ButtonGroup!.Dispose();
    }
    private static void VerifyWarmInput()
    {
        using var group = new ButtonGroup(); var root = new TestViewport(); var a = new Probe { Name = "A", Size = new(40, 30), ToggleMode = true, ButtonGroup = group }; var b = new Probe { Name = "B", Position = new(60, 0), Size = new(40, 30), ToggleMode = true, ButtonGroup = group };
        root.AddChild(a); root.AddChild(b); using var tree = new SceneTree(root);
        using var motion = new InputEventMouseMotion(); using var press = Mouse(true); using var release = Mouse(false);
        void PointerCycle(int i)
        {
            var p = new Vector2((i & 1) == 0 ? 10 : 70, 10); motion.Position = p; press.Position = p; release.Position = p;
            root.PushInput(motion, true); root.PushInput(press, true); root.PushInput(release, true);
        }
        for (var i = 0; i < 64; i++) PointerCycle(i); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) PointerCycle(i);
        var routed = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(ReferenceEquals(group.GetPressedButton(), b), "Real routed pointer input retains complete hover, capture, release and group activation.");
        void CopyCycle(int i)
        {
            var target = (i & 1) == 0 ? a : b;
            using var m = (InputEventMouse)target.MakeInputLocal(motion); m.GlobalPosition = target.GetCanvasTransform().AffineInverse() * motion.Position;
            using var d = (InputEventMouse)target.MakeInputLocal(press); d.GlobalPosition = target.GetCanvasTransform().AffineInverse() * press.Position;
            using var u = (InputEventMouse)target.MakeInputLocal(release); u.GlobalPosition = target.GetCanvasTransform().AffineInverse() * release.Position;
        }
        for (var i = 0; i < 64; i++) CopyCycle(i); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) CopyCycle(i);
        var copies = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(routed == copies, $"Full pointer dispatch allocates {routed} bytes; its independent required local-event copying baseline allocates {copies} bytes.");
        using var binding = new InputEventKey { Keycode = Key.F8 }; using var shortcut = new Shortcut { Events = [binding] };
        using var key = new InputEventKey { Keycode = Key.F8, Pressed = true }; a.Shortcut = shortcut; a.ShortcutFeedback = false;
        a.Enter(); b.Enter(); press.Position = release.Position = new(5, 5);
        void ButtonCycle(int i)
        {
            var target = (i & 1) == 0 ? a : b; target.Send(press); target.Send(release); root.PushInput(key, true);
        }
        for (var i = 0; i < 64; i++) ButtonCycle(i); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) ButtonCycle(i);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0 && ReferenceEquals(group.GetPressedButton(), a), $"Warmed reused local button/group events and viewport-routed shortcuts allocated {allocated} managed bytes.");
        Console.WriteLine($"Button input allocation boundary:64 routed mouse cycles={routed} B for owned positional event copies;64 reused local button/group plus routed shortcut cycles={allocated} B.");
    }
    private static void VerifyHoverBuffers()
    {
        var root = new TestViewport { InputEnabled = true, HandlePointer = true }; var a = new Control { Name = "A", Size = new(30, 20) }; var b = new Control { Name = "B", Position = new(60, 0), Size = new(30, 20) };
        root.AddChild(a); root.AddChild(b); using var tree = new SceneTree(root);
        using var left = new InputEventMouseMotion { Position = new(10, 10) }; using var right = new InputEventMouseMotion { Position = new(70, 10) };
        var nested = false; var enters = 0; a.MouseEntered += () => enters++; b.MouseEntered += () => enters++;
        a.MouseExited += () => { if (nested) root.PushInput(right, true); };
        void Cycle()
        {
            root.PushInput(left, true); nested = true; tree.ClearGUIHover(); nested = false;
        }
        for (var i = 0; i < 64; i++) Cycle(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Cycle();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0 && enters == 256, $"Prepared hover chains including nested input from an exit callback allocate {allocated} bytes and preserve all enter transitions.");
    }
    private static InputEventMouseButton Mouse(bool pressed, MouseButton button = MouseButton.Left) => new() { ButtonIndex = button, Position = new(5, 5), Pressed = pressed };
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static T Reject<T>(Action action) where T : Exception { try { action(); } catch (T e) { return e; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
    private sealed class TestViewport : Viewport
    {
        internal int Unhandled;
        internal bool HandlePointer;
        internal TestViewport() { UnhandledInputEnabled = true; }
        public override Rect2 GetVisibleRect() => new(0, 0, 200, 100);
        protected override void OnInput(InputEvent input) { if (HandlePointer && input is InputEventMouse) Tree!.SetInputAsHandled(); }
        protected override void OnUnhandledInput(InputEvent input) { Unhandled++; }
    }
    private sealed class Probe : BaseButton
    {
        internal List<string>? Log; internal Action? PressedHook; internal Control? CustomTooltip; internal string? TooltipArgument;
        internal void Send(InputEvent input) => OnGUIInput(input);
        internal void Enter() => DispatchNotification(NotificationMouseEnter);
        internal Control? Tooltip(string text) => CreateTooltipControl(text);
        protected override void OnPressed() { Log?.Add("hook:pressed"); PressedHook?.Invoke(); }
        protected override void OnToggled(bool value) => Log?.Add("hook:" + value);
        protected override Control? OnMakeCustomTooltip(string text) { TooltipArgument = text; return CustomTooltip; }
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new Probe();
    }
    private sealed class CaptureProbe : Node
    {
        private static readonly PropertyDescriptor<CaptureProbe, int> Captured = new("Capture", n => { n.DuringCapture?.Invoke(); return 0; }, (n, _) => n.EnsureMutable(), _ => 0, stored: true);
        internal Action? DuringCapture;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(Captured);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new CaptureProbe();
    }
}
