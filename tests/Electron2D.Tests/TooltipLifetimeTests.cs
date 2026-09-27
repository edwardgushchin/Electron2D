using Electron2D;

internal static class TooltipLifetimeTests
{
    internal static void Run()
    {
        var previous = ProjectSettings.Instance.Get(ProjectSettings.TooltipDelaySeconds);
        ProjectSettings.Instance.Set(ProjectSettings.TooltipDelaySeconds, .01);
        try
        {
            for (var mode = 0; mode < 3; mode++)
            {
                var root = new TestViewport(); var owner = new Owner(mode) { Size = new(100, 50), TooltipText = "custom" };
                Node? existing = null; if (mode == 2) { existing = new Node { Name = "Tooltip" }; owner.AddChild(existing); }
                root.AddChild(owner); var tree = new SceneTree(root); using var motion = new InputEventMouseMotion { Position = new(10, 10) };
                try
                {
                    root.PushInput(motion, true); tree.ProcessFrame(.02);
                    if (mode == 1) Check(!owner.Visible && tree.TooltipPanel is null && owner.Content!.IsDisposed, "A size callback can hide the owner and cancel presentation without accessing disposed tooltip geometry.");
                    else Check(tree.TooltipPanel is not null && !owner.Content!.IsDisposed, "The custom tooltip is shown before teardown.");
                    if (existing is not null) Check(ReferenceEquals(owner.GetNode("Tooltip"), existing) && !existing.IsDisposed, "Tooltip presentation preserves an existing child with the conventional tooltip name.");
                }
                finally { tree.Dispose(); }
                Check(owner.Content!.IsDisposed, "Tree teardown disposes owned tooltip content without attempting child disposal while its parent exits.");
            }
        }
        finally { ProjectSettings.Instance.Set(ProjectSettings.TooltipDelaySeconds, previous); }
        Console.WriteLine("Tooltip callback cancellation, child-name coexistence and live presentation teardown passed.");
    }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 320, 240); }
    private sealed class Owner(int mode) : Control
    {
        internal Control? Content;
        protected override Control? OnMakeCustomTooltip(string text) => Content = new ContentNode(this, mode) { CustomMinimumSize = new(40, 30) };
    }
    private sealed class ContentNode(Control owner, int mode) : Control
    {
        protected override void OnReady()
        {
            if (mode != 1) return;
            var panel = (Control)Parent!; panel.Size = new(100, 100); panel.Resized += () => owner.Visible = false;
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
