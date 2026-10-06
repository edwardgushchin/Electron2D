using Electron2D;

internal static class ColorPickerTests
{
    internal static void Run()
    {
        Palette();
        using var detached = new ColorPicker(); Check(detached.Color == Colors.White && detached.EditAlpha && detached.EditIntensity && !detached.DeferredMode && detached.GetChildCount() == 0, "Picker defaults and owned children.");
        var root = new SubViewport { Size = new(1100, 900), GUIEmbedSubwindows = true }; var picker = new ColorPicker { Name = "Picker", Position = new(20, 20), Size = new(600, 700) }; root.AddChild(picker); using var tree = new SceneTree(root); Frames(tree);
        var events = 0; picker.ColorChanged += _ => events++;
        picker.Color = new(.2f, .4f, .6f, .5f); Check(events == 0, "Programmatic color setter is silent.");
        var values = Enumerable.Range(0, 5).Select(i => (SpinBox)Find(picker, "Value" + i)).ToArray();
        values[0].Value = 255; Check(Math.Abs(picker.Color.R - 1) < .01 && events == 1, "Shared numeric RGB channel edits real color.");
        picker.ColorMode = ColorPicker.ColorModeType.HSV; values[0].Value = 120; values[1].Value = 100; values[2].Value = 100; Check(picker.Color.G > .99 && picker.Color.R < .01 && picker.Color.B < .01, "HSV channels produce green.");
        picker.ColorMode = ColorPicker.ColorModeType.Linear; values[0].Value = .25; Check(Math.Abs(picker.Color.R - new Color(.25f, 0, 0).LinearToSRGB().R) < .002, "Linear channels convert to nonlinear sRGB.");
        picker.ColorMode = ColorPicker.ColorModeType.OKHSL; values[0].Value = 210; values[1].Value = 60; values[2].Value = 70; var expected = Color.FromOKHSL(210f / 360, .6f, .7f, picker.Color.A); Check(picker.Color.IsEqualApprox(expected), "Perceptual channels use existing color math.");
        picker.Color = new(.3f, .5f, .7f, .5f); values[4].Value = 1; var exposure = new Color(.3f, .5f, .7f, .5f).SRGBToLinear(); exposure.R *= 2; exposure.G *= 2; exposure.B *= 2; Check(picker.Color.IsEqualApprox(exposure.LinearToSRGB()), "Intensity doubles linear light, preserving alpha.");
        var preserved = picker.Color; picker.EditIntensity = false; Check(picker.Color.IsEqualApprox(preserved), "Disabling intensity preserves the selected HDR color."); picker.EditAlpha = false;
        var text = (LineEdit)Find(picker, "ColorText"); text.GrabFocus(); text.Edit(); text.Text = "#ff000080"; Enter(root); Frames(tree); Check(picker.Color.R == 1 && picker.Color.G == 0 && Math.Abs(picker.Color.A - .5) < .01, "Text submission preserves alpha when editing hidden.");
        text.GrabFocus(); text.Edit(); text.Text = "Color(pow(2,1), 0.25, 0.5, 0.75)"; Enter(root); Frames(tree); Check(picker.Color.R == 2 && Math.Abs(picker.Color.A - .5) < .01, "Numeric Color constructor evaluates scalar fields."); var before = picker.Color; text.GrabFocus(); text.Edit(); text.Text = "Color(1,broken,3)"; Enter(root); Frames(tree); Check(picker.Color == before, "Malformed color constructor preserves value.");
        text.GrabFocus(); text.Edit(); text.Text = "Color(1,2,3,)"; Enter(root); Frames(tree); Check(picker.Color == before, "Trailing color constructor argument is rejected.");
        picker.EditIntensity = true; picker.Color = new Color(float.MaxValue, 1, 0); Check(picker.Color.R == float.MaxValue && values[4].Value > 100, "HDR normalization widens arithmetic before exposure division.");
        var valid = picker.Color; Reject<ArgumentException>(() => picker.Color = new Color(float.NaN, 1, 1)); Check(picker.Color == valid, "Nonfinite color input rejects before mutation.");
        picker.Color = Colors.White; Reject<AggregateException>(() => values[4].Value = 10000); Check(picker.Color == Colors.White && values[4].Value == 0, "Overflowing exposure preserves color and restores the committed field.");
        picker.EditAlpha = true; picker.EditIntensity = true; picker.Color = Colors.White;
        foreach (var shape in Enum.GetValues<ColorPicker.PickerShapeType>())
        {
            picker.PickerShape = shape; Frames(tree); if (shape == ColorPicker.PickerShapeType.None) continue;
            var surface = (Control)Find(picker, "Surface"); var point = surface.Size * new Vector2(.65f, .45f); Click(root, surface.GetGlobalTransformWithCanvas() * point, true); Click(root, surface.GetGlobalTransformWithCanvas() * point, false); Frames(tree); Check(picker.Color.IsFinite(), "Every surface routes finite color edits: " + shape);
        }
        picker.PickerShape = ColorPicker.PickerShapeType.HSVRectangle; picker.DeferredMode = true; picker.Color = Colors.White; Frames(tree); var box = (Control)Find(picker, "Surface"); var at = box.GetGlobalTransformWithCanvas() * (box.Size * new Vector2(.5f, .5f)); var eventBefore = events;
        Click(root, at, true); Check(events == eventBefore && picker.Color != Colors.White, "Deferred surface updates local color without event on press."); Click(root, at, false); Check(events == eventBefore + 1, "Deferred surface publishes at release.");
        box.GrabFocus(); var colorBefore = picker.Color; using (var input = new InputEventAction { Action = "ui_right", Pressed = true }) root.PushInput(input, true); Check(picker.Color != colorBefore, "Keyboard surface editing executes.");
        picker.PickerShape = ColorPicker.PickerShapeType.HSVWheel; picker.Color = Colors.White; box.GrabFocus();
        foreach (var action in new[] { "ui_accept", "ui_up", "ui_accept", "ui_right" }) { using var input = new InputEventAction { Action = action, Pressed = true }; root.PushInput(input, true); }
        Check(Math.Abs(picker.Color.S - .01) < .0001, "Wheel keyboard navigation switches to the square and preserves one-percent stepping."); picker.PickerShape = ColorPicker.PickerShapeType.HSVRectangle;
        picker.Color = Colors.White; picker.DeferredMode = true; Action<Color> failed = _ => throw new InvalidOperationException("color observer"); picker.ColorChanged += failed;
        Click(root, at, true); Reject<AggregateException>(() => Click(root, at, false)); picker.ColorChanged -= failed; Check(picker.GetRecentPresets().Contains(picker.Color), "Failed release observer preserves color and required recent swatch.");
        Swatches(root, picker, tree); PaletteUI(picker, tree); PickerButton(root, tree); Packing(picker);
        Console.WriteLine("ColorPicker modes, shapes, numeric/text input, intensity, deferred gestures, swatches, owned popup and palette scenes passed.");
    }
    private static void Palette()
    {
        using var palette = new ColorPalette(); var input = new[] { Colors.Red, new Color(2, -1, .5f, .4f), Colors.Red }; var changes = 0; palette.Changed += _ => changes++; palette.Colors = input; input[0] = Colors.Blue; var copy = palette.Colors; copy[1] = Colors.Black; Check(palette.Colors[0] == Colors.Red && palette.Colors[1].R == 2 && changes == 0, "Palette owns arrays, retains order/HDR/duplicates and silent assignment.");
        using var duplicate = (ColorPalette)palette.Duplicate(); duplicate.Colors = [Colors.Green]; Check(palette.Colors.Length == 3, "Palette duplicate owns independent state."); Reject<ArgumentNullException>(() => palette.Colors = null!);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-palette-" + Guid.NewGuid() + ".e2dres"); try { ResourceSaver.Save(palette, path); using var loaded = ResourceLoader.Load<ColorPalette>(path, ResourceLoader.CacheMode.Ignore); Check(loaded.Colors.SequenceEqual(palette.Colors), "Palette typed resource file roundtrip."); } finally { File.Delete(path); }
    }
    private static void Swatches(Viewport root, ColorPicker picker, SceneTree tree)
    {
        foreach (var color in picker.GetPresets()) picker.ErasePreset(color); picker.AddPreset(Colors.Red); picker.AddPreset(Colors.Blue); picker.AddPreset(Colors.Red); Check(picker.GetPresets().SequenceEqual(new[] { Colors.Blue, Colors.Red }), "Existing preset moves to tail.");
        for (var i = 0; i < 12; i++) picker.AddRecentPreset(new Color(i / 12f, .2f, .3f)); Check(picker.GetRecentPresets().Length == 9, "Recent colors retain nine oldest-first entries.");
        var added = 0; var removed = 0; picker.PresetAdded += _ => added++; picker.PresetRemoved += _ => removed++; Frames(tree); ClickControl(root, (Control)Find(picker, "Add")); Check(added == 1, "User add swatch publishes typed event.");
        var grid = Find(picker, "Presets"); var swatch = (Control)grid.GetChild(1); var point = swatch.GetGlobalTransformWithCanvas() * (swatch.Size / 2); using (var input = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Right, Pressed = true }) root.PushInput(input, true); Check(removed == 1, "Right-click user removal publishes typed event.");
        var second = (Control)grid.GetChild(1); second.GrabFocus(); var remaining = picker.GetPresets().Length; using (var key = new InputEventKey { Keycode = Key.Delete, Pressed = true }) root.PushInput(key, true); Check(picker.GetPresets().Length == remaining - 1, "Registered Delete action removes the focused swatch.");
        picker.CanAddSwatches = false; Check(((BaseButton)Find(picker, "Add")).Disabled, "Swatch policy disables user addition."); picker.AddPreset(Colors.Green); Check(picker.GetPresets().Contains(Colors.Green), "Programmatic swatches remain available."); picker.CanAddSwatches = true;
    }
    private static void PaletteUI(ColorPicker picker, SceneTree tree)
    {
        var menu = (MenuButton)Find(picker, "PaletteMenu"); var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-picker-palette-" + Guid.NewGuid() + ".e2dres");
        void Command(int index) { menu.ShowPopup(); menu.GetPopup().SetFocusedItem(index); using var input = new InputEventAction { Action = "ui_accept", Pressed = true }; menu.GetPopup().PushInput(input, true); Frames(tree); }
        void Accept(FileDialog dialog) { var button = dialog.GetOKButton(); button.GrabFocus(); var point = button.GetGlobalTransformWithCanvas() * (button.Size / 2); using var down = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = true }; using var up = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left }; dialog.PushInput(down, true); dialog.PushInput(up, true); Frames(tree); }
        try
        {
            var colors = picker.GetPresets(); Command(0); var dialog = (FileDialog)Find(picker, "_palette_file"); Check(dialog.Visible, "Palette Save command opens the connected file browser."); dialog.CurrentPath = path; Accept(dialog); Check(File.Exists(path), "Palette file browser saves the actual swatches resource.");
            foreach (var color in picker.GetPresets()) picker.ErasePreset(color); Command(2); dialog.CurrentPath = path; Accept(dialog); Check(picker.GetPresets().SequenceEqual(colors), "Palette Load command restores swatches through typed file decoding.");
        }
        finally { File.Delete(path); }
    }
    private static void PickerButton(Viewport root, SceneTree tree)
    {
        var button = new ColorPickerButton { Name = "ColorButton", Position = new(760, 50), Size = new(150, 40) }; root.AddChild(button); var created = 0; var closed = 0; button.PickerCreated += () => created++; button.PopupClosed += () => closed++; var picker = button.GetPicker(); Check(created == 1 && ReferenceEquals(picker, button.GetPicker()) && button.ToggleMode, "Picker button lazy creation is stable."); Reject<InvalidOperationException>(picker.Dispose); Reject<InvalidOperationException>(button.GetPopup().Dispose); Frames(tree); ClickControl(root, button); Frames(tree); Check(button.GetPopup().Visible && button.ButtonPressed, "Routed button opens real embedded color popup."); button.GetPopup().Hide(); Check(closed == 1 && !button.ButtonPressed, "Closing resets toggle and publishes PopupClosed.");
        ClickControl(root, button); Frames(tree); var text = (LineEdit)Find(picker, "ColorText"); text.Text = "00ff00"; text.Edit(); using (var input = new InputEventKey { Keycode = Key.Enter, Pressed = true }) button.GetPopup().PushInput(input, true); Frames(tree); Check(button.Color.G > .99 && !button.GetPopup().Visible, "Popup acceptance commits the focused text before closing.");
        var opening = button.Color; ClickControl(root, button); Frames(tree); var red = (SpinBox)Find(picker, "Value0"); red.Value = 255; using (var input = new InputEventAction { Action = "ui_cancel", Pressed = true }) button.GetPopup().PushInput(input, true); Frames(tree); Check(button.Color == opening && !button.GetPopup().Visible, "Escape restores the popup opening color."); button.Dispose();
    }
    private static void Packing(ColorPicker source)
    {
        source.PickerShape = ColorPicker.PickerShapeType.OKHSRectangle; source.ColorMode = ColorPicker.ColorModeType.OKHSL; using var scene = new PackedScene(); scene.Pack(source); using var copy = (ColorPicker)scene.Instantiate(); Check(copy.PickerShape == source.PickerShape && copy.ColorMode == source.ColorMode && copy.Color == source.Color, "Picker stored policies/color and concrete factory roundtrip.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-color-" + Guid.NewGuid() + ".e2dscene"); try { ResourceSaver.Save(scene, path); var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(ColorPickerTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_COLOR_PICKER"); start.Environment["ELECTRON2D_TEST_COLOR_PICKER_CHILD"] = path; using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh color scene."); } Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh color scene passed"), error.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path) { using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var picker = (ColorPicker)scene.Instantiate(); var root = new SubViewport { Size = new(800, 800) }; root.AddChild(picker); using var tree = new SceneTree(root); Frames(tree); var field = ((SpinBox)Find(picker, "Value0")).GetLineEdit(); field.Text = "120"; ((SpinBox)Find(picker, "Value0")).Apply(); Check(Math.Abs(picker.Color.OKHSLH - 1f / 3) < .001, "Fresh picker executes public numeric channel workflow."); Console.WriteLine("Fresh color scene passed"); }
    internal static Node Find(Node root, string name) { if (root.Name == name) return root; for (var i = 0; i < root.GetChildCount(true); i++) { var result = FindOrNull(root.GetChild(i, true), name); if (result != null) return result; } throw new InvalidOperationException("Missing color control " + name); }
    private static Node? FindOrNull(Node root, string name) { if (root.Name == name) return root; for (var i = 0; i < root.GetChildCount(true); i++) { var result = FindOrNull(root.GetChild(i, true), name); if (result != null) return result; } return null; }
    private static void Frames(SceneTree tree) { for (var i = 0; i < 6; i++) tree.ProcessFrame(.01); }
    private static void Enter(Viewport root) { using var key = new InputEventKey { Keycode = Key.Enter, Pressed = true }; root.PushInput(key, true); }
    private static void Click(Viewport root, Vector2 point, bool pressed) { using var input = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = pressed }; root.PushInput(input, true); }
    private static void ClickControl(Viewport root, Control control) { var point = control.GetGlobalTransformWithCanvas() * (control.Size / 2); Click(root, point, true); Click(root, point, false); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
