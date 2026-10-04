using Electron2D;

internal static class ControlTooltipTests
{
    internal static void Run()
    {
        var settings = ProjectSettings.Service;
        var delay = ProjectSettings.Get(ProjectSettings.TooltipDelaySeconds);
        var root = new TestViewport();
        var owner = new TooltipProbe { Name = "Owner", Size = new(100, 50), TooltipText = "  Tooltip text  " };
        root.AddChild(owner);
        using var tree = new SceneTree(root);
        using var motion = new InputEventMouseMotion { Position = new(20, 20) };
        try
        {
            ProjectSettings.Set(ProjectSettings.TooltipDelaySeconds, .25);
            root.PushInput(motion, true); tree.ProcessFrame(.25);
            Check(tree.TooltipPanel is null, "Tooltip waits until the unscaled delay is passed.");
            tree.ProcessFrame(.01);
            var panel = tree.TooltipPanel ?? throw new InvalidOperationException("Delayed tooltip was not created.");
            Check(panel.GetChild(0) is Label { Text: "Tooltip text", ThemeTypeVariation: "TooltipLabel" } && owner.CustomText == "Tooltip text", "Trimmed untranslated text reaches the hook and default label.");
            Check(panel.Position == new Vector2(30, 30) && panel.Size.X > 0 && panel.Size.Y > 0, "Tooltip geometry uses the cursor offset and real content minimum.");
            Check(panel.GetMouseFilterWithOverride() == MouseFilter.Ignore && panel.GetFocusModeWithOverride() == FocusMode.None, "Tooltip presentation is pointer-transparent and cannot steal focus.");
            for (var i = 0; i < 64; i++) tree.ProcessFrame(.01);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) tree.ProcessFrame(.01);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "A warmed visible tooltip adds no managed allocations to process frames.");
            owner.TooltipText = "changed"; root.PushInput(motion, true);
            Check(tree.TooltipPanel is null, "Text changes cancel stale content on pointer delivery.");
            tree.FlushDeferred(); Check(panel.IsDisposed, "Canceled presentation is disposed at the safe deletion point.");
            tree.ProcessFrame(.26); Check(tree.TooltipPanel?.GetChild(0) is Label { Text: "changed" }, "Changed content is re-created after the delay.");
            using var key = new InputEventKey { Keycode = Key.F12, Pressed = true };
            root.PushInput(key, true); Check(tree.TooltipPanel is not null, "An unrelated key leaves the tooltip visible.");
            key.Keycode = Key.Escape;
            root.PushInput(key, true); Check(tree.TooltipPanel is null, "The ui_cancel action dismisses a tooltip.");
            owner.Custom = new Control { Visible = false }; root.PushInput(motion, true); tree.ProcessFrame(.26);
            Check(owner.Custom.IsDisposed && tree.TooltipPanel is null, "An invisible custom result suppresses display and transfers ownership for cleanup.");
            owner.Custom = new Control { CustomMinimumSize = new(40, 30), FocusMode = FocusMode.All };
            motion.Position = new(30, 20); root.PushInput(motion, true); tree.ProcessFrame(.26);
            Check(ReferenceEquals(tree.TooltipPanel!.GetChild(0), owner.Custom), "A detached custom control is actually displayed.");
            owner.Custom.GrabFocus(); Check(!owner.Custom.HasFocus(), "Even a custom tooltip child cannot grab focus.");
            owner.Visible = false; tree.FlushDeferred(); Check(tree.TooltipPanel is null && owner.Custom.IsDisposed, "Hiding the target releases its tooltip and custom content.");
            owner.Custom = null; owner.Visible = true; owner.TooltipText = "edge"; owner.Position = new(260, 190); owner.Size = new(60, 50);
            motion.Position = new(310, 230); root.PushInput(motion, true); tree.ProcessFrame(.26);
            panel = tree.TooltipPanel!;
            Check(panel.Position.X < motion.Position.X && panel.Position.Y < motion.Position.Y && panel.Position.X + panel.Size.X <= 320 && panel.Position.Y + panel.Size.Y <= 240, "Near edges the tooltip flips around the pointer into the viewport.");
            owner.TooltipText = "\0\u0001\u00a0tip\u00a0 \t";
            root.PushInput(motion, true); tree.ProcessFrame(.26);
            Check(tree.TooltipPanel?.GetChild(0) is Label { Text: "\u00a0tip\u00a0" }, "Tooltip edge stripping removes ASCII controls and spaces while preserving nonbreaking Unicode spaces.");
        }
        finally { ProjectSettings.Set(ProjectSettings.TooltipDelaySeconds, delay); }
        TeardownAndReentry();
        Console.WriteLine("Control tooltips verify delay, content hooks, real layout, input transparency, ownership, cancellation, edge placement and warmed frames.");
    }

    private static void TeardownAndReentry()
    {
        for (var phase = 0; phase < 3; phase++)
        {
            var root = new TestViewport();
            var owner = new TooltipProbe { Name = "Owner", Size = new(100, 60), TooltipText = "tip" };
            owner.AddChild(new Node { Name = "Tooltip" });
            var content = phase == 2 ? new CancelOnResize(owner) : new Control { CustomMinimumSize = new(40, 30) };
            owner.Custom = content; root.AddChild(owner);
            using var tree = new SceneTree(root);
            using var motion = new InputEventMouseMotion { Position = new(20, 20) };
            root.PushInput(motion, true); tree.ProcessFrame(1);
            if (phase == 0)
            {
                Check(tree.TooltipPanel is not null, "Tooltip host chooses a unique name beside existing children.");
                tree.Dispose(); Check(content.IsDisposed, "Ordinary SceneTree teardown releases custom tooltip content.");
            }
            else if (phase == 1)
            {
                root.RemoveChild(owner); tree.FlushDeferred();
                Check(content.IsDisposed && tree.TooltipPanel is null, "Removing a live owner schedules safe tooltip cleanup after tree exit.");
                owner.Dispose();
            }
            else Check(tree.TooltipPanel is null && content.IsDisposed, "Resize-triggered cancellation invalidates the older tooltip fit before any later property writes.");
        }
    }

    private sealed class CancelOnResize(Control owner) : Control
    {
        protected override void OnReady()
        {
            CustomMinimumSize = new(40, 30);
            var panel = (Control)Parent!; panel.Size = new(100, 100);
            panel.Resized += () => owner.Visible = false;
        }
    }

    private sealed class TooltipProbe : Control
    {
        internal Control? Custom;
        internal string CustomText = string.Empty;
        protected override Control? OnMakeCustomTooltip(string forText) { CustomText = forText; return Custom; }
    }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(Vector2.Zero, new(320, 240)); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
