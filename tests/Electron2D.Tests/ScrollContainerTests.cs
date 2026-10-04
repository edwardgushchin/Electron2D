using Electron2D;

internal static class ScrollContainerTests
{
    internal static void Run()
    {
        var settings = ProjectSettings.Service;
        var previousDeadzone = ProjectSettings.Get(ProjectSettings.DefaultScrollDeadzone);
        try
        {
            ProjectSettings.Set(ProjectSettings.DefaultScrollDeadzone, 7);
            using var configured = new ScrollContainer();
            Check(configured.ScrollDeadzone == 7,
                "A new container samples the typed project deadzone.");
        }
        finally { ProjectSettings.Set(ProjectSettings.DefaultScrollDeadzone, previousDeadzone); }
        VerifyModes();
        VerifyFailedBegin();
        VerifyInertia();
        using (var empty = new ScrollContainer())
            Check(empty.GetConfigurationWarnings().Length == 1,
                "An empty scroll container reports the single-content recommendation.");
        var viewport = new TestViewport();
        var scroll = new ScrollContainer { Name = "Scroll", Position = new(20, 20), Size = new(100, 100), MouseFilter = MouseFilter.Pass };
        var content = new Control { Name = "Content", CustomMinimumSize = new(200, 250), MouseFilter = MouseFilter.Pass };
        scroll.AddChild(content);
        content.Owner = scroll;
        content.AddChild(new Control { Name = "Blocking", Size = new(70, 70), MouseFilter = MouseFilter.Stop });
        var focusTarget = new Control { Name = "FocusTarget", Position = new(0, 180), Size = new(20, 20), FocusMode = FocusMode.All };
        content.AddChild(focusTarget);
        viewport.AddChild(scroll);
        Check(scroll.ChildCount == 1 && scroll.GetChildCount(true) == 6 &&
              scroll.GetHScrollBar().Parent == scroll && scroll.GetVScrollBar().Parent == scroll &&
              scroll.ClipContents && !scroll.PropagateMaximumSize && scroll.GetConfigurationWarnings().Length == 0,
            "Scroll content remains an ordinary child while five live implementation children are hidden.");
        using (var packed = new PackedScene())
        {
            packed.Pack(scroll);
            using var copy = (ScrollContainer)packed.Instantiate();
            Check(copy.GetChildCount() == 1 && copy.GetChildCount(true) == 6 &&
                  copy.GetChild(0).Name == "Content" && copy.GetVScrollBar().Parent == copy,
                "Packed scenes recreate internal bars without serializing duplicate implementation children.");
        }
        using (var tree = new SceneTree(viewport))
        {
            tree.FlushDeferred();
            Check(scroll.GetHScrollBar().Visible && scroll.GetVScrollBar().Visible &&
                  scroll.GetHScrollBar().Page > 0 && scroll.GetVScrollBar().Page > 0,
                "Oversized content produces usable bars on both axes.");
            using var wheel = new InputEventMouseButton
            {
                Position = new(30, 30),
                Pressed = true,
                ButtonIndex = MouseButton.WheelDown
            };
            viewport.PushInput(wheel, inLocalCoordinates: true);
            tree.FlushDeferred();
            Check(scroll.ScrollVertical > 0 && content.Position.Y < 0,
                "A vertical wheel step changes the visible content offset.");
            scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Disabled;
            tree.FlushDeferred();
            Check(scroll.GetVScrollBar().Visible == false && scroll.GetMinimumSize().Y >= 250,
                "Disabling an axis hides its bar and restores the content minimum.");
            scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
            tree.FlushDeferred();
            Check(scroll.GetVScrollBar().Visible && scroll.ScrollVertical == 0,
                "Restoring automatic mode keeps the offset clamped after disabled-mode content fitting.");

            scroll.ScrollHorizontalByDefault = true;
            var verticalBeforeSwap = scroll.ScrollVertical;
            viewport.PushInput(wheel, inLocalCoordinates: true);
            Check(scroll.ScrollHorizontal > 0 && scroll.ScrollVertical == verticalBeforeSwap,
                "The vertical wheel follows the horizontal-by-default axis choice.");
            wheel.ShiftPressed = true;
            viewport.PushInput(wheel, inLocalCoordinates: true);
            Check(scroll.ScrollVertical > verticalBeforeSwap,
                "Shift reverses the configured default wheel axis.");
            wheel.ShiftPressed = false;
            scroll.ScrollHorizontalByDefault = false;
            scroll.ScrollHorizontal = 0;
            scroll.ScrollVertical = 0;

            var starts = 0; var ends = 0;
            scroll.ScrollStarted += () => starts++;
            scroll.ScrollEnded += () => ends++;
            scroll.ScrollDeadzone = 12;
            Input.EmulateTouchFromMouse = true;
            try
            {
                using var press = new InputEventMouseButton { Position = new(30, 50), ButtonIndex = MouseButton.Left, Pressed = true };
                using var move = new InputEventMouseMotion { Position = new(30, 40), Relative = new(0, -10) };
                using var movePastDeadzone = new InputEventMouseMotion { Position = new(30, 35), Relative = new(0, -5) };
                using var release = new InputEventMouseButton { Position = new(30, 35), ButtonIndex = MouseButton.Left, Pressed = false };
                viewport.PushInput(press, inLocalCoordinates: true);
                viewport.PushInput(move, inLocalCoordinates: true);
                Check(starts == 0 && scroll.ScrollVertical == 0,
                    "A touch drag inside the configured deadzone leaves content and signals unchanged.");
                viewport.PushInput(movePastDeadzone, inLocalCoordinates: true);
                Check(starts == 1 && scroll.ScrollVertical > 0, "Touch-style mouse motion crosses the deadzone and scrolls content.");
                tree.ProcessFrame(0);
                tree.ProcessFrame(1e-300);
                Check(double.IsFinite(scroll.GetVScrollBar().Value),
                    "Zero and subnormal frame steps keep touch velocity and range state finite.");
                viewport.PushInput(release, inLocalCoordinates: true);
                Check(ends == 1, "Releasing an undriven touch drag ends the begun scroll exactly once.");
            }
            finally { Input.EmulateTouchFromMouse = false; scroll.ScrollDeadzone = 0; }

            scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
            scroll.HintMode = ScrollContainer.ScrollHintMode.All;
            scroll.ScrollVertical = 0;
            tree.FlushDeferred();
            var leadingHint = (Control)scroll.GetChild(1, includeInternal: true);
            var trailingHint = (Control)scroll.GetChild(2, includeInternal: true);
            Check(!leadingHint.Visible && trailingHint.Visible,
                "A vertical-only overflow displays the trailing hint at the beginning of the range.");
            scroll.ScrollVertical = int.MaxValue;
            tree.FlushDeferred();
            Check(leadingHint.Visible && !trailingHint.Visible,
                "At the end of the range the leading hint replaces the trailing hint.");
            scroll.HintMode = ScrollContainer.ScrollHintMode.BottomAndRight;
            Check(!leadingHint.Visible && !trailingHint.Visible,
                "The trailing-only hint policy hides an unavailable trailing edge.");
            scroll.HintMode = ScrollContainer.ScrollHintMode.TopAndLeft;
            Check(leadingHint.Visible && !trailingHint.Visible,
                "The leading-only hint policy keeps the remaining reachable edge.");
            scroll.ScrollVertical = 0;
            scroll.FollowFocus = true;
            focusTarget.GrabFocus();
            Check(scroll.ScrollVertical > 0,
                "Following focus accounts for a pending content rearrangement immediately.");
            tree.FlushDeferred();
            Check(scroll.ScrollVertical > 0,
                "Following focus retains the visible descendant after deferred layout.");
            scroll.DrawFocusBorder = true;
            tree.FlushDeferred();
            var focusPanel = (PanelContainer)scroll.GetChild(5, includeInternal: true);
            Check(ReferenceEquals(focusPanel.GetThemeStyleBox("panel"), scroll.GetThemeStyleBox("focus")),
                "The separate focus panel borrows the scroll container's focus style.");
        }
        Console.WriteLine("Scroll container internals, packing, overflow, wheel, modes, touch, focus and hint checks passed.");
    }

    private static void VerifyModes()
    {
        var viewport = new TestViewport();
        var scroll = new ScrollContainer { Size = new(100, 100) };
        var content = new Control { CustomMinimumSize = new(50, 50), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        scroll.AddChild(content);
        viewport.AddChild(scroll);
        using var tree = new SceneTree(viewport);
        tree.FlushDeferred();
        Check(!scroll.GetHScrollBar().Visible && !scroll.GetVScrollBar().Visible,
            "Auto mode hides bars for fitting content.");
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.ShowAlways;
        tree.FlushDeferred();
        Check(scroll.GetHScrollBar().Visible, "ShowAlways exposes a bar without overflow.");
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Reserve;
        tree.FlushDeferred();
        Check(!scroll.GetHScrollBar().Visible && content.Size.Y < 100,
            "Reserve hides the bar yet leaves room for its thickness.");
        content.CustomMinimumSize = new(200, 50);
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        tree.FlushDeferred();
        Check(!scroll.GetHScrollBar().Visible, "ShowNever keeps an overflowing bar hidden.");
        scroll.ScrollHorizontal = 40;
        tree.FlushDeferred();
        Check(content.Position.X < 0, "ShowNever still permits programmatic scrolling.");
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.MaximizeFirst;
        scroll.CustomMaximumSize = new(120, -1);
        tree.FlushDeferred();
        Check(scroll.GetMinimumSize().X <= 120,
            "MaximizeFirst bounds its preferred content minimum by the configured maximum.");
    }

    private static void VerifyFailedBegin()
    {
        var viewport = new TestViewport();
        var scroll = new ScrollContainer { Position = new(10, 10), Size = new(100, 100) };
        scroll.AddChild(new Control { CustomMinimumSize = new(80, 220), MouseFilter = MouseFilter.Pass });
        viewport.AddChild(scroll);
        using var tree = new SceneTree(viewport);
        tree.FlushDeferred();
        var started = 0; var ended = 0;
        scroll.ScrollStarted += () => { started++; throw new InvalidOperationException("observer"); };
        scroll.ScrollEnded += () => ended++;
        Input.EmulateTouchFromMouse = true;
        try
        {
            using var press = new InputEventMouseButton { Position = new(20, 30), ButtonIndex = MouseButton.Left, Pressed = true };
            using var first = new InputEventMouseMotion { Position = new(20, 20), Relative = new(0, -10) };
            using var second = new InputEventMouseMotion { Position = new(20, 10), Relative = new(0, -10) };
            using var release = new InputEventMouseButton { Position = new(20, 10), ButtonIndex = MouseButton.Left, Pressed = false };
            viewport.PushInput(press, inLocalCoordinates: true);
            try { viewport.PushInput(first, inLocalCoordinates: true); throw new InvalidOperationException("Expected the start callback failure."); }
            catch (AggregateException) { }
            Check(started == 1, "A failing start callback runs once after the committed deadzone transition.");
            viewport.PushInput(second, inLocalCoordinates: true);
            Check(scroll.ScrollVertical > 0 && started == 1,
                "The same gesture can continue after a failed start callback without replaying it.");
            viewport.PushInput(release, inLocalCoordinates: true);
            Check(ended == 1, "A failed begin still receives exactly one end phase.");
        }
        finally { Input.EmulateTouchFromMouse = false; }
    }

    private static void VerifyInertia()
    {
        var viewport = new TestViewport();
        var scroll = new ScrollContainer { Position = new(10, 10), Size = new(100, 100) };
        scroll.AddChild(new Control { CustomMinimumSize = new(200, 300), MouseFilter = MouseFilter.Pass });
        viewport.AddChild(scroll);
        using var tree = new SceneTree(viewport);
        tree.FlushDeferred();
        var ended = 0;
        scroll.ScrollEnded += () => ended++;
        Input.EmulateTouchFromMouse = true;
        try
        {
            using var press = new InputEventMouseButton { Position = new(50, 50), ButtonIndex = MouseButton.Left, Pressed = true };
            using var move = new InputEventMouseMotion { Position = new(30, 40), Relative = new(-20, -10) };
            using var release = new InputEventMouseButton { Position = new(30, 40), ButtonIndex = MouseButton.Left, Pressed = false };
            viewport.PushInput(press, inLocalCoordinates: true);
            viewport.PushInput(move, inLocalCoordinates: true);
            tree.ProcessFrame(.016);
            viewport.PushInput(release, inLocalCoordinates: true);
            var horizontalEdge = scroll.GetHScrollBar().MaxValue - scroll.GetHScrollBar().Page;
            var reachedHorizontalEdge = false;
            var verticalAtEdge = 0d;
            for (var frame = 0; frame < 96 && ended == 0; frame++)
            {
                tree.ProcessFrame(.016);
                if (!reachedHorizontalEdge && scroll.GetHScrollBar().Value >= horizontalEdge)
                {
                    reachedHorizontalEdge = true;
                    verticalAtEdge = scroll.GetVScrollBar().Value;
                }
                if (reachedHorizontalEdge)
                    Check(scroll.GetHScrollBar().Value == horizontalEdge,
                        "An axis that hits its range edge cannot reverse while the other axis coasts.");
            }
            Check(reachedHorizontalEdge && scroll.GetVScrollBar().Value > verticalAtEdge && ended == 1,
                "The remaining axis continues after its peer hits an edge, then inertia ends once.");
        }
        finally { Input.EmulateTouchFromMouse = false; }
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
