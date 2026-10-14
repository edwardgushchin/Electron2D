using Electron2D;
using System.Diagnostics;

internal static class TouchControlTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Near(float actual, float expected, string message) => Check(MathF.Abs(actual - expected) < .001f, message + ": " + actual);
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Touch(Viewport root, int index, bool pressed, Vector2 position, bool canceled = false)
    { using var input = new InputEventScreenTouch { Index = index, Pressed = pressed, Position = position, Canceled = canceled }; root.PushInput(input, true); }
    private static void Drag(Viewport root, int index, Vector2 position)
    { using var input = new InputEventScreenDrag { Index = index, Position = position }; root.PushInput(input, true); }
    private sealed class Actions : Node
    {
        internal readonly List<(string Action, bool Pressed)> Events = [];
        internal Action<InputEventAction>? Callback;
        protected override void OnInput(InputEvent input) { if (input is InputEventAction action) { Events.Add((action.Action, action.Pressed)); Callback?.Invoke(action); } }
    }
    internal static void RunManaged()
    {
        InputMap.AddAction("touch-test"); InputMap.AddAction("touch-other"); InputMap.AddAction("touch-shared");
        try
        {
            using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
            var root = new SubViewport { Size = new(400, 240) }; var a = new TouchScreenButton { Name = "A", TextureNormal = texture, Action = "touch-test", Position = new(4, 4) }; var b = new TouchScreenButton { Name = "B", TextureNormal = texture, Action = "touch-test", Position = new(40, 4) };
            var joystick = new VirtualJoystick { Name = "Joystick", Position = new(100, 20), Size = new(100, 100) }; var actions = new Actions { InputEnabled = true }; root.AddChild(a); root.AddChild(b); root.AddChild(joystick); root.AddChild(actions);
            using var tree = new SceneTree(root); tree.ProcessFrame(.01);
            Check(!a.IsPressed() && a.ShapeCentered && a.ShapeVisible && !a.PassbyPress && a.VisibilityMode == TouchButtonVisibility.Always && joystick.JoystickSize == 100 && joystick.TipSize == 50, "Public defaults.");
            Touch(root, -1, true, new(8, 8)); Check(a.IsPressed() && Input.IsActionPressed("touch-test") && actions.Events.Contains(("touch-test", true)), "Signed contact: pressed=" + a.IsPressed() + ", action=" + Input.IsActionPressed("touch-test") + ", input=" + a.InputEnabled + ", tree=" + a.IsInsideTree + ", visible=" + a.IsVisibleInTree + ", events=" + actions.Events.Count);
            Touch(root, 3, true, new(44, 8)); Input.ActionPress("touch-test"); Touch(root, -1, false, new(8, 8)); Check(Input.IsActionPressed("touch-test") && !a.IsPressed() && b.IsPressed(), "One source release preserves peer and manual action.");
            Touch(root, 3, false, new(44, 8)); Check(Input.IsActionPressed("touch-test"), "Physical/manual contribution survives last virtual release."); Input.ActionRelease("touch-test"); Check(!Input.IsActionPressed("touch-test"), "All contributions release independently.");
            Touch(root, 0, true, new(8, 8)); a.Action = "touch-other"; Check(!Input.IsActionPressed("touch-test") && Input.IsActionPressed("touch-other"), "Live action rebinding retires the old source."); a.Visible = false; Check(!a.IsPressed() && !Input.IsActionPressed("touch-other"), "Hide releases held source."); a.Visible = true; a.Action = "touch-test";
            a.PassbyPress = true; Drag(root, 9, new(8, 8)); Check(a.IsPressed(), "Passby entry presses."); Drag(root, 9, new(30, 30)); Check(!a.IsPressed(), "Passby exit releases.");
            using var key = new InputEventKey { PhysicalKeycode = Key.Space, Pressed = true }; InputMap.ActionAddEvent("touch-test", key); Input.ParseInputEvent(key); Touch(root, 0, true, new(8, 8)); Touch(root, 0, false, new(8, 8)); Check(Input.IsActionPressed("touch-test"), "Physical key survives virtual source release."); key.Pressed = false; Input.ParseInputEvent(key); Check(!Input.IsActionPressed("touch-test"), "Physical release clears final producer."); InputMap.ActionEraseEvent("touch-test", key);
            var count = actions.Events.Count; Touch(root, 0, true, new(8, 8)); actions.Callback = input => { if (input.Action == "touch-test" && !input.Pressed) a.Action = "touch-shared"; }; a.Action = "touch-other"; actions.Callback = null;
            Check(a.Action == "touch-shared" && Input.IsActionPressed("touch-shared") && !Input.IsActionPressed("touch-other") && actions.Events.Skip(count).Count(e => e == ("touch-shared", true)) == 1, "Reentrant action rebinding commits later selection and emits one press."); Touch(root, 0, false, new(8, 8)); a.Action = "touch-test";
            using var emptyMask = new BitMap(); var descriptor = (PropertyDescriptor<BitMap, byte[]>)emptyMask.GetPropertyList().Single(p => p.Name == "_Mask"); Reject<InvalidDataException>(() => descriptor.SetValue(emptyMask, new byte[7])); Check(emptyMask.GetSize() == Vector2i.Zero, "Malformed storage preserves empty mask.");
            using var mask = new BitMap(); mask.Create(new(16, 16)); mask.SetBit(2, 2, true); a.Bitmask = mask;
            Touch(root, 1, true, new(8, 8)); Check(!a.IsPressed(), "Bitmap restricts texture fallback."); Touch(root, 1, true, new(6, 6)); Check(a.IsPressed(), "Bitmap set pixel presses."); Touch(root, 1, false, new(6, 6));
            using var circle = new CircleShape { Radius = 2 }; a.Shape = circle; Touch(root, 1, true, new(12, 12)); Check(a.IsPressed(), "Centered shape joins the bitmap hit union."); Touch(root, 1, false, new(12, 12));
            a.VisibilityMode = TouchButtonVisibility.TouchscreenOnly; if (DisplayServer.Service?.IsTouchscreenAvailableCore() != true) { Touch(root, 1, true, new(12, 12)); Check(!a.IsPressed(), "Unavailable touchscreen-only button ignores input."); }
            a.VisibilityMode = TouchButtonVisibility.Always;
            Touch(root, 1, true, new(12, 12)); Touch(root, 1, true, new(12, 12), canceled: true); Check(!a.IsPressed() && !Input.IsActionPressed("touch-test"), "Canceled pressed contact releases.");
            var releases = 0; var taps = 0; var flicks = 0; var canceledFlicks = 0; Vector2 released = default;
            joystick.Released += vector => { releases++; released = vector; Check(!Input.IsActionPressed("ui_right"), "Released callback observes retired source."); }; joystick.Tapped += () => taps++; joystick.Flicked += _ => flicks++; joystick.FlickCanceled += () => canceledFlicks++;
            Touch(root, 2, true, new(150, 70)); Drag(root, 2, new(175, 70)); Near(Input.GetActionStrength("ui_right"), .5f, "Half-radius analog strength");
            Drag(root, 2, new(150, 70)); Check(canceledFlicks == 1 && !Input.IsActionPressed("ui_right"), "Return to dead zone cancels flick and retires action."); Touch(root, 2, false, new(150, 70)); Check(taps == 0 && flicks == 0 && releases == 1, "Canceled movement is neither tap nor flick.");
            joystick.JoystickMode = VirtualJoystickMode.Dynamic; Touch(root, 2, true, new(110, 30)); Drag(root, 2, new(135, 30)); Near(Input.GetActionStrength("ui_right"), .5f, "Dynamic base starts at contact"); Touch(root, 2, false, new(135, 30)); Check(flicks == 1 && released.X > .49f, "Release/flick retain final vector.");
            Touch(root, 2, true, new(110, 30)); Touch(root, 2, false, new(110, 30)); Check(taps == 1, "Stationary release taps."); Touch(root, 2, true, new(110, 30)); Touch(root, 2, false, new(110, 30), canceled: true); Check(taps == 1 && flicks == 1, "Canceled contact has no gesture outcome.");
            joystick.JoystickMode = VirtualJoystickMode.Following; Touch(root, 2, true, new(110, 30)); Drag(root, 2, new(190, 30)); Near(Input.GetActionStrength("ui_right"), 1, "Following clamp"); Drag(root, 2, new(190, 30)); Near(Input.GetActionStrength("ui_right"), 1, "Following base remains under clamp");
            joystick.ClampzoneRatio = 0; Drag(root, 2, new(195, 30)); Check(Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").IsFinite() && !Input.IsActionPressed("ui_right"), "Zero clamp produces finite zero output."); Touch(root, 2, false, new(195, 30)); joystick.ClampzoneRatio = 1;
            joystick.ActionRight = joystick.ActionDown = "touch-shared"; Touch(root, 2, true, new(110, 30)); Drag(root, 2, new(135, 30)); Check(Input.IsActionPressed("touch-shared"), "Duplicate axis names retain independent nonzero slot."); joystick.Visible = false; Check(!Input.IsActionPressed("touch-shared"), "Joystick hide releases every slot."); joystick.Visible = true; joystick.ActionRight = "ui_right"; joystick.ActionDown = "ui_down";
            Action hide = () => joystick.Visible = false; joystick.Pressed += hide; Touch(root, 2, true, new(110, 30)); Check(!Input.IsActionPressed("ui_right"), "Reentrant press hide cannot leave held actions."); joystick.Pressed -= hide; joystick.Visible = true;
            Touch(root, 2, true, new(110, 30)); Drag(root, 2, new(135, 30)); tree.Paused = true; Check(!Input.IsActionPressed("ui_right"), "Pause retires virtual sources."); tree.Paused = false;
            Reject<ArgumentOutOfRangeException>(() => joystick.ClampzoneRatio = float.MaxValue); Reject<ArgumentOutOfRangeException>(() => joystick.InitialOffsetRatio = new(float.MaxValue, float.MaxValue));
            Reject<ArgumentOutOfRangeException>(() => joystick.JoystickSize = float.NaN); Reject<ArgumentOutOfRangeException>(() => joystick.VisibilityMode = (JoystickVisibility)99); Reject<ArgumentOutOfRangeException>(() => a.VisibilityMode = (TouchButtonVisibility)99);
            Task.Run(() => { Reject<InvalidOperationException>(() => _ = joystick.JoystickSize); Reject<InvalidOperationException>(() => a.Action = "x"); }).GetAwaiter().GetResult();
            var guard = root.BeginSceneCapture(); try { Reject<InvalidOperationException>(() => joystick.TipSize = 40); Reject<InvalidOperationException>(() => a.PassbyPress = false); } finally { Node.EndSceneCapture(guard); }
            Action failRelease = () => throw new InvalidOperationException("release observer");
            a.Released += failRelease; Touch(root, 0, true, new(12, 12)); Reject<AggregateException>(() => Touch(root, 0, false, new(12, 12))); Check(!a.IsPressed() && !Input.IsActionPressed("touch-test"), "Throwing release observer cannot retain button contribution."); a.Released -= failRelease;
            Action<Vector2> failJoystick = _ => throw new InvalidOperationException("joystick observer"); joystick.Released += failJoystick;
            Touch(root, 2, true, new(110, 30)); Drag(root, 2, new(135, 30)); Reject<AggregateException>(() => Touch(root, 2, false, new(135, 30))); Check(!Input.IsActionPressed("ui_right"), "Throwing joystick release cannot retain axes."); joystick.Released -= failJoystick;
            joystick.JoystickMode = VirtualJoystickMode.Fixed; joystick.DeadzoneRatio = .25f; Touch(root, 2, true, new(150, 70)); Drag(root, 2, new(156, 70)); Check(!Input.IsActionPressed("ui_right"), "Dead zone suppresses motion."); Drag(root, 2, new(175, 70)); Near(Input.GetActionStrength("ui_right"), 1f / 3, "Dead zone rescaling"); Touch(root, 2, false, new(175, 70)); joystick.DeadzoneRatio = 0; joystick.JoystickMode = VirtualJoystickMode.Following;
            Touch(root, 0, true, new(12, 12)); a.InputEnabled = false; tree.ProcessFrame(.01); Check(!a.IsPressed() && !Input.IsActionPressed("touch-test"), "Disabled button input retires at the next internal frame."); a.InputEnabled = true;
            Touch(root, 2, true, new(110, 30)); Drag(root, 2, new(135, 30)); joystick.MouseFilter = MouseFilter.Ignore; tree.ProcessFrame(.01); Check(!Input.IsActionPressed("ui_right"), "Ignored pointer policy retires joystick contribution."); joystick.MouseFilter = MouseFilter.Stop;
            Touch(root, 0, true, new(12, 12)); Touch(root, 2, true, new(110, 30)); Drag(root, 2, new(135, 30)); root.GUIDisableInput = true; tree.ProcessFrame(.01); Check(!a.IsPressed() && !Input.IsActionPressed("ui_right") && !Input.IsActionPressed("touch-test"), "Viewport input disable retires all virtual sources."); root.GUIDisableInput = false;
            tree.EditedSceneRoot = root; Touch(root, 0, true, new(12, 12)); Touch(root, 2, true, new(110, 30)); Drag(root, 2, new(135, 30)); Check(!a.IsPressed() && !Input.IsActionPressed("ui_right"), "Authoring subtree cannot publish gameplay actions."); tree.EditedSceneRoot = null;
            SaveAndReload(root, a, b, joystick);
            a.Dispose(); Check(!texture.IsDisposed && !mask.IsDisposed && !circle.IsDisposed, "Disposal preserves borrowed resources.");
            Console.WriteLine("Touch controls managed: source aggregation, queued actions, masks/shapes/passby, joystick modes/scaling/gestures, cancellation/failure/lifetime, guards and fresh scene storage passed.");
        }
        finally { Input.ActionRelease("touch-test"); Input.ActionRelease("touch-other"); Input.ActionRelease("touch-shared"); InputMap.EraseAction("touch-test"); InputMap.EraseAction("touch-other"); InputMap.EraseAction("touch-shared"); }
    }
    internal static void RunArchive(string path)
    {
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var copy = loaded.Instantiate();
        var a = (TouchScreenButton)copy.GetNode("A"); var b = (TouchScreenButton)copy.GetNode("B"); var joystick = (VirtualJoystick)copy.GetNode("Joystick");
        Check(a.Action == "touch-test" && a.PassbyPress && a.Bitmask is { } mask && mask.GetSize() == new Vector2i(16, 16) && mask.GetBit(2, 2) && !mask.GetBit(3, 2) && a.Shape is CircleShape && !a.IsPressed() && joystick.JoystickMode == VirtualJoystickMode.Following && ReferenceEquals(a.TextureNormal, b.TextureNormal), "Exact scene node/resources, shared textures and no runtime contact state.");
    }
    private static void SaveAndReload(SubViewport root, TouchScreenButton a, TouchScreenButton b, VirtualJoystick joystick)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-touch-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        a.Owner = b.Owner = joystick.Owner = root;
        try
        {
            using var scene = new PackedScene(); scene.Pack(root); ResourceSaver.Save(scene, path, SaverFlags.BundleResources);
            RunArchive(path);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add(typeof(TouchControlTests).Assembly.Location); start.ArgumentList.Add("--touch-archive"); start.ArgumentList.Add(path); start.Environment.Remove("ELECTRON2D_TEST_TOUCH");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh touch scene deadline."); }
            Check(child.ExitCode == 0, "Fresh process: " + output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
    }
}
