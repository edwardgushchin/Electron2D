using Electron2D;

internal static class ScrollBarTests
{
    internal static void Run()
    {
        var viewport = new TestViewport();
        var bar = new HScrollBar { Name = "Bar", Position = new(10, 10), Size = new(120, 20), MaxValue = 200, Page = 40 };
        var tiny = new HScrollBar { Name = "Tiny", Position = new(150, 10), Size = new(8, 8), MaxValue = 200, Page = 40 };
        viewport.AddChild(bar);
        viewport.AddChild(tiny);
        Check(bar.Step == 0 && bar.CustomStep == -1 && bar.FocusMode == FocusMode.Accessibility &&
              bar.SizeFlagsHorizontal == Control.SizeFlags.Fill && bar.SizeFlagsVertical == Control.SizeFlags.ShrinkBegin,
            "The horizontal bar has continuous values and the expected orientation and focus defaults.");
        using var tree = new SceneTree(viewport);
        var order = new List<string>();
        bar.ValueChanged += _ => order.Add("value");
        bar.Scrolling += () => order.Add("scrolling");
        bar.Value = 10;
        Check(order.SequenceEqual(["value"]), "Programmatic range assignment does not emit Scrolling.");
        order.Clear();
        using (var wheel = new InputEventMouseButton { Position = new(20, 18), ButtonIndex = MouseButton.WheelDown, Pressed = true })
            viewport.PushInput(wheel, inLocalCoordinates: true);
        Check(bar.Value == 15 && order.SequenceEqual(["value", "scrolling"]),
            "A wheel step uses one eighth of Page and reports Scrolling after the inherited value signal.");
        bar.FocusMode = FocusMode.All;
        bar.CustomStep = 3;
        bar.GrabFocus();
        using (var right = new InputEventKey { Keycode = Key.Right, Pressed = true })
        {
            viewport.PushInput(right, inLocalCoordinates: true);
            Check(bar.Value == 18, "The orientation-matched arrow uses CustomStep.");
            bar.LayoutDirection = LayoutDirection.RTL;
            viewport.PushInput(right, inLocalCoordinates: true);
            Check(bar.Value == 21, "Bar direction stays left-to-right under RTL layout.");
        }
        bar.LayoutDirection = LayoutDirection.LTR;
        bar.Position = new(10, 10);
        using (var click = new InputEventMouseButton { Position = new(105, 18), ButtonIndex = MouseButton.Left, Pressed = true })
            viewport.PushInput(click, inLocalCoordinates: true);
        Check(bar.Value == 61, "A track click beyond the thumb advances one page.");
        using (var release = new InputEventMouseButton { Position = new(105, 18), ButtonIndex = MouseButton.Left, Pressed = false })
            viewport.PushInput(release, inLocalCoordinates: true);
        var tinyInputs = 0;
        tiny.GUIInput += _ => tinyInputs++;
        using (var tinyPress = new InputEventMouseButton { Position = new(154, 14), ButtonIndex = MouseButton.Left, Pressed = true })
            viewport.PushInput(tinyPress, inLocalCoordinates: true);
        Check(tinyInputs == 1, "A prior wheel press does not capture later clicks on another control.");
        var vertical = new VScrollBar { Name = "Vertical", Position = new(200, 10), Size = new(20, 100) };
        viewport.AddChild(vertical);
        Check(vertical.Step == 0 && vertical.FocusMode == FocusMode.Accessibility &&
              vertical.SizeFlagsHorizontal == Control.SizeFlags.ShrinkBegin && vertical.SizeFlagsVertical == Control.SizeFlags.Fill,
            "Vertical construction fixes the opposite size flags while preserving continuous values.");
        using (var motion = new InputEventMouseMotion { Position = new(157, 14), Relative = new(3, 0) })
            viewport.PushInput(motion, inLocalCoordinates: true);
        Check(double.IsFinite(tiny.Value), "A zero-length thumb track does not divide by zero during dragging.");
        Console.WriteLine("Scroll bar defaults, wheel ordering and degenerate drag checks passed.");
    }

    private sealed class TestViewport : Viewport
    {
        public override Rect2 GetVisibleRect() => new(0, 0, 500, 500);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
