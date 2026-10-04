using Electron2D;

internal static class GUIButtonRoutingTests
{
    internal static void Run()
    {
        InputMap.LoadFromProjectSettings();
        TouchCapture(); PositionalTransforms(); PausedInput(); MouseCapture();
        Console.WriteLine("GUI button routing verifies per-contact touch capture/cancellation, outside release, gestures, transformed drag, parent bubbling, processing gates and multi-button mouse capture.");
    }
    private static void TouchCapture()
    {
        var root = new VP(); var a = new ButtonProbe { Name = "A", Position = new(10, 10), Size = new(30, 20) }; var b = new ButtonProbe { Name = "B", Position = new(70, 10), Size = new(30, 20) };
        root.AddChild(a); root.AddChild(b); using var tree = new SceneTree(root);
        using var one = Touch(1, true, new(15, 15)); using var two = Touch(2, true, new(75, 15)); root.PushInput(one, true); root.PushInput(two, true);
        Check(a.ButtonPressed && b.ButtonPressed && a.LastPosition == new Vector2(5, 5) && b.LastPosition == new Vector2(5, 5), "Independent contacts capture independent controls in local coordinates.");
        two.Pressed = false; root.PushInput(two, true); Check(b.Activations == 1 && a.ButtonPressed, "Releasing one contact preserves the other contact's held state.");
        using var drag = new InputEventScreenDrag { Index = 1, Position = new(200, 15), Relative = new(185, 0), Velocity = new(370, 0) }; root.PushInput(drag, true);
        one.Pressed = false; one.Position = new(75, 15); root.PushInput(one, true);
        Check(a.Activations == 0 && !a.ButtonPressed && b.Activations == 1 && a.LastPosition == new Vector2(65, 5), "Outside release routes to the original contact owner and cannot activate the control under the new position.");
        one.Index = 3; one.Position = new(15, 15); one.Pressed = true; root.PushInput(one, true); one.Canceled = true; root.PushInput(one, true);
        Check(!a.ButtonPressed && a.Activations == 0, "Cancellation releases the contact without activation."); one.Canceled = false; one.Pressed = false;
        one.Index = 4; one.Pressed = true; root.PushInput(one, true); two.Index = 5; two.Position = new(15, 15); two.Pressed = true; root.PushInput(two, true); two.Pressed = false; root.PushInput(two, true);
        Check(a.ButtonPressed, "A button ignores a second contact even when the viewport captures both indices to it."); one.Pressed = false; root.PushInput(one, true); Check(a.Activations == 1, "Only the first owned touch completes the button activation.");
        one.Index = 6; one.Pressed = true; root.PushInput(one, true); a.Visible = false; a.Visible = true; var received = a.Touches; one.Pressed = false; root.PushInput(one, true);
        Check(a.Touches == received && !a.ButtonPressed, "Hide drops touch capture even when the same control reappears before release.");
        drag.Index = 99; drag.Position = new(75, 15); root.PushInput(drag, true); Check(b.Drags == 1 && b.LastPosition == new Vector2(5, 5), "Uncaptured drag falls back to current hit testing.");
        one.Index = 7; one.Pressed = true; root.PushInput(one, true); a.Dispose(); one.Pressed = false; root.PushInput(one, true); Check(b.Activations == 1, "A disposed capture target does not redirect its release to another button.");
    }
    private static void PositionalTransforms()
    {
        var root = new VP(); var parent = new InputProbe { Name = "Parent", Position = new(100, 80), Size = new(100, 100), MouseFilter = MouseFilter.Pass }; var middle = new Entity { Position = new(5, 5) }; var child = new InputProbe { Name = "Child", Position = new(10, 10), Scale = new(2, 2), Size = new(20, 20), MouseFilter = MouseFilter.Pass };
        root.AddChild(parent); parent.AddChild(middle); middle.AddChild(child); using var tree = new SceneTree(root);
        using var touch = Touch(-7, true, new(125, 105)); root.PushInput(touch, true);
        Check(child.LastPosition == new Vector2(5, 5) && parent.LastPosition == new Vector2(25, 25), "Pointer propagation traverses non-Control canvas parents and converts coordinates for each control.");
        using var drag = new InputEventScreenDrag { Index = -7, Position = new(127, 109), Relative = new(6, 10), Velocity = new(12, 20), ScreenRelative = new(9, 11), ScreenVelocity = new(19, 21) }; root.PushInput(drag, true);
        Check(child.LastPosition == new Vector2(6, 7) && child.Relative == new Vector2(3, 5) && child.Velocity == new Vector2(6, 10) && parent.Relative == new Vector2(6, 10) && parent.Velocity == new Vector2(12, 20), "Captured drag transforms local vectors with each control's inverse basis.");
        Check(child.ScreenRelative == drag.ScreenRelative && child.ScreenVelocity == drag.ScreenVelocity && drag.Position == new Vector2(127, 109) && child.LastEvent!.IsDisposed, "Global drag vectors and caller events stay unchanged while temporary local copies are retired after callbacks.");
        using var pan = new InputEventPanGesture { Position = new(125, 105), Delta = new(4, 6) }; child.MouseFilter = MouseFilter.Stop; root.PushInput(pan, true);
        Check(child.Gestures == 1 && parent.Gestures == 1 && child.LastPosition == new Vector2(5, 5) && parent.LastPosition == new Vector2(25, 25) && child.Delta == pan.Delta && root.Unhandled > 0, "Gestures hit-test, preserve source gesture deltas and are not automatically consumed by pointer stop filters.");
        child.Fail = true; touch.Pressed = false; Reject<AggregateException>(() => root.PushInput(touch, true)); child.Fail = false;
        var before = child.Touches; root.PushInput(touch, true); Check(child.Touches == before, "A failing release callback still clears its capture before delivery.");
        using var identity = new IdentityGesture { Position = new(125, 105) }; root.PushInput(identity, true);
        Check(!identity.IsDisposed && ReferenceEquals(child.LastEvent, identity), "A custom transformation which returns its borrowed event does not transfer disposal ownership to GUI routing.");
    }
    private static void PausedInput()
    {
        var root = new VP(); var a = new ButtonProbe { Name = "Paused", Position = new(10, 10), Size = new(30, 20) }; var b = new ButtonProbe { Name = "Always", Position = new(70, 10), Size = new(30, 20), ProcessMode = ProcessMode.Always, ToggleMode = true, ButtonPressed = true }; root.AddChild(a); root.AddChild(b); using var tree = new SceneTree(root);
        using var touch = Touch(10, true, new(15, 15)); root.PushInput(touch, true); Check(a.ButtonPressed, "The pausable control begins holding before pause."); tree.Paused = true; Check(!a.ButtonPressed && b.ButtonPressed, "Pause cancels transient state while retaining toggle selection and Always eligibility.");
        touch.Pressed = false; root.PushInput(touch, true); touch.Index = 11; touch.Pressed = true; root.PushInput(touch, true); touch.Pressed = false; root.PushInput(touch, true); Check(a.Activations == 0, "Touch input cannot activate a paused control.");
        using var down = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = new(15, 15), Pressed = true }; using var up = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = new(15, 15) }; root.PushInput(down, true); root.PushInput(up, true); a.GrabFocus(); using var key = new InputEventKey { Keycode = Key.Space, Pressed = true }; root.PushInput(key, true); key.Pressed = false; root.PushInput(key, true); Check(a.Activations == 0, "Mouse and focused keyboard GUI delivery respect CanProcess.");
        touch.Index = 12; touch.Position = new(75, 15); touch.Pressed = true; root.PushInput(touch, true); touch.Pressed = false; root.PushInput(touch, true); Check(b.Activations == 1 && !b.ButtonPressed, "Always-mode controls continue processing while the tree is paused.");
        tree.Paused = false; touch.Index = 13; touch.Position = new(15, 15); touch.Pressed = true; root.PushInput(touch, true); touch.Pressed = false; root.PushInput(touch, true); Check(a.Activations == 1, "A fresh contact after resume is not blocked by the paused contact index.");
        touch.Index = 14; touch.Pressed = true; root.PushInput(touch, true); a.ProcessMode = ProcessMode.Disabled; touch.Pressed = false; root.PushInput(touch, true); a.ProcessMode = ProcessMode.Inherit; touch.Index = 15; touch.Pressed = true; root.PushInput(touch, true); touch.Pressed = false; root.PushInput(touch, true); Check(a.Activations == 2, "Disabling processing during a held contact cancels it and permits a fresh contact after re-enabling.");
    }
    private static void MouseCapture()
    {
        var root = new VP(); var a = new InputProbe { Name = "A", Size = new(30, 20) }; var b = new InputProbe { Name = "B", Position = new(60, 0), Size = new(30, 20) }; root.AddChild(a); root.AddChild(b); using var tree = new SceneTree(root);
        using var left = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = new(5, 5), Pressed = true }; using var right = new InputEventMouseButton { ButtonIndex = MouseButton.Right, Position = new(65, 5), Pressed = true }; root.PushInput(left, true); root.PushInput(right, true);
        Check(a.MouseButtons == 2 && b.MouseButtons == 0, "A second mouse button keeps the original capture owner."); left.Pressed = false; left.Position = new(65, 5); root.PushInput(left, true); using var motion = new InputEventMouseMotion { Position = new(65, 5), ButtonMask = MouseButtonMask.Right }; root.PushInput(motion, true);
        Check(a.Motions == 1 && b.Motions == 0, "Releasing one mouse button keeps capture for the remaining held button."); right.Pressed = false; root.PushInput(right, true); motion.ButtonMask = MouseButtonMask.None; root.PushInput(motion, true); Check(b.Motions == 1, "The last release frees capture and restores hit-tested motion.");
    }
    private static InputEventScreenTouch Touch(int index, bool pressed, Vector2 position) => new() { Index = index, Pressed = pressed, Position = position };
    private sealed class IdentityGesture : InputEventGesture
    {
        protected override InputEvent CreateEventInstance() => new IdentityGesture();
    }
    private sealed class VP : Viewport
    {
        internal int Unhandled; internal VP() { UnhandledInputEnabled = true; }
        public override Rect2 GetVisibleRect() => new(0, 0, 320, 240);
        protected override void OnUnhandledInput(InputEvent input) => Unhandled++;
    }
    private sealed class ButtonProbe : BaseButton
    {
        internal int Activations, Touches, Drags; internal Vector2 LastPosition;
        protected override void OnPressed() => Activations++;
        protected override void OnGUIInput(InputEvent input)
        {
            if (input is InputEventScreenTouch t) { Touches++; LastPosition = t.Position; }
            if (input is InputEventScreenDrag d) { Drags++; LastPosition = d.Position; }
            base.OnGUIInput(input);
        }
    }
    private sealed class InputProbe : Control
    {
        internal int Touches, Gestures, MouseButtons, Motions; internal Vector2 LastPosition, Relative, Velocity, ScreenRelative, ScreenVelocity, Delta; internal InputEvent? LastEvent; internal bool Fail;
        protected override void OnGUIInput(InputEvent input)
        {
            LastEvent = input;
            switch (input)
            {
                case InputEventScreenTouch t: Touches++; LastPosition = t.Position; break;
                case InputEventScreenDrag d: LastPosition = d.Position; Relative = d.Relative; Velocity = d.Velocity; ScreenRelative = d.ScreenRelative; ScreenVelocity = d.ScreenVelocity; break;
                case InputEventPanGesture p: Gestures++; LastPosition = p.Position; Delta = p.Delta; break;
                case InputEventMouseButton b: MouseButtons++; LastPosition = b.Position; break;
                case InputEventMouseMotion m: Motions++; LastPosition = m.Position; break;
            }
            if (Fail) throw new ApplicationException("expected routed callback failure");
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
