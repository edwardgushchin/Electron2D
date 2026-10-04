using Electron2D;

internal static class CanvasLifecycleTests
{
    internal static void Run()
    {
        Membership();
        Visibility();
        Failures();
        Drawing();
        Console.WriteLine("Canvas lifecycle checks passed.");
    }

    private static void Membership()
    {
        var log = new List<string>();
        var root = new Node();
        var parent = new Probe { Name = "parent" }; var child = new Probe { Name = "child" };
        root.AddChild(parent); parent.AddChild(child);
        foreach (var item in new[] { parent, child })
        {
            item.Notified = id => { if (id is CanvasItem.NotificationEnterCanvas or CanvasItem.NotificationExitCanvas) log.Add($"{item.Name}:{id}:{item.TopLevel}"); };
            item.VisibilityChanged += n => log.Add($"{n.Name}:visible:{n.IsVisibleInTree}");
            item.Entering = () => log.Add(item.Name + ":enter");
            item.Exiting = () => log.Add(item.Name + ":exit");
        }
        using var tree = new SceneTree(root);
        Check(log.SequenceEqual(new[] { "parent:32:False", "parent:visible:True", "parent:enter", "child:32:False", "child:visible:True", "child:enter" }), "Entry order and visible activation.");
        log.Clear(); parent.TopLevel = true; parent.TopLevel = true;
        Check(log.SequenceEqual(new[] { "parent:33:False", "parent:32:True" }), "TopLevel only rebinds itself once, preserving old/new notification state.");
        log.Clear(); parent.Notify(Node.NotificationEnterTree); parent.Notify(Node.NotificationExitTree);
        Check(log.SequenceEqual(new[] { "parent:enter", "parent:exit" }) && parent.IsVisibleInTree, "Manual tree notifications do not alter canvas membership.");
        log.Clear(); parent.Notify(CanvasItem.NotificationVisibilityChanged);
        Check(log.SequenceEqual(new[] { "parent:visible:True" }), "Manual visibility notifications project to the typed signal without changing visibility.");
        log.Clear(); root.RemoveChild(parent);
        Check(log.SequenceEqual(new[] { "child:exit", "child:33:False", "parent:exit", "parent:33:True" }) && !parent.IsVisibleInTree && !child.IsVisibleInTree, "Exit is child-first, without Hidden or visibility signals.");
        log.Clear(); root.AddChild(parent);
        Check(log.Count == 6 && parent.IsVisibleInTree && child.IsVisibleInTree, "Reattachment restores membership and visibility.");
    }

    private static void Visibility()
    {
        var log = new List<string>();
        var window = new Node(); var parent = new Probe { Name = "parent" };
        var child = new Probe { Name = "child", TopLevel = true }; var hidden = new Probe { Name = "hidden", Visible = false };
        var neutral = new Node { Name = "neutral" }; var independent = new Probe { Name = "independent" };
        window.AddChild(parent); parent.AddChild(child); parent.AddChild(hidden); parent.AddChild(neutral); neutral.AddChild(independent);
        foreach (var item in new[] { parent, child, hidden, independent })
        {
            item.Notified = id => { if (id == CanvasItem.NotificationVisibilityChanged) log.Add(item.Name + ":notify"); };
            item.VisibilityChanged += n => log.Add(n.Name + ":event");
            item.Hidden += n => log.Add(n.Name + ":hidden");
        }
        using var tree = new SceneTree(window);
        log.Clear(); parent.Hide();
        Check(log.SequenceEqual(new[] { "parent:notify", "parent:event", "parent:hidden", "child:notify", "child:event", "child:hidden" }), "Hidden follows visibility and stops at locally hidden/neutral branches; TopLevel inherits it.");
        Check(!child.IsVisibleInTree && independent.IsVisibleInTree, "Effective visibility matches its canvas boundary.");
        log.Clear(); hidden.Show();
        Check(log.SequenceEqual(new[] { "hidden:notify", "hidden:event" }) && !hidden.IsVisibleInTree, "Local change under hidden parent only notifies itself.");
        log.Clear(); parent.Show();
        Check(log.Count == 6 && hidden.IsVisibleInTree && child.IsVisibleInTree, "Showing a parent notifies visible direct descendants.");
        Action<CanvasItem> restore = n => { if (!n.Visible) n.Show(); };
        parent.VisibilityChanged += restore;
        parent.Hide();
        Check(parent.IsVisibleInTree && child.IsVisibleInTree && hidden.IsVisibleInTree, "Reentrant visibility callbacks cannot leave stale descendant state.");
        parent.VisibilityChanged -= restore;
        Reject<InvalidOperationException>(() => Task.Run(parent.Hide).GetAwaiter().GetResult());
        using var detached = new Probe(); var events = 0; var hiddenEvents = 0;
        detached.VisibilityChanged += _ => events++; detached.Hidden += _ => hiddenEvents++;
        detached.Hide();
        Check(events == 1 && hiddenEvents == 0, "Detached local visibility changes are not hidden-in-tree transitions.");
    }

    private static void Failures()
    {
        var root = new Node(); var item = new Probe(); root.AddChild(item);
        item.Notified = id => { if (id == CanvasItem.NotificationEnterCanvas) throw new InvalidOperationException("entry"); };
        Reject<AggregateException>(() => new SceneTree(root));
        Check(item.Tree is null && !item.IsVisibleInTree && !item.IsDisposed, "Failed activation rolls canvas membership back.");
        item.Notified = null;
        using var tree = new SceneTree(root);
        var entries = 0;
        item.Notified = id =>
        {
            if (id == CanvasItem.NotificationExitCanvas) throw new InvalidOperationException("exit");
            if (id == CanvasItem.NotificationEnterCanvas) entries++;
        };
        Reject<AggregateException>(() => item.TopLevel = true);
        Check(item.TopLevel && entries == 1 && item.IsVisibleInTree, "Rebinding completes after exit callback failure.");
        Reject<AggregateException>(() => root.RemoveChild(item));
        Check(item.Tree is null && !item.IsVisibleInTree, "Exit callback failure does not retain membership.");
        item.Notified = null; root.AddChild(item);
        item.Notified = id => { if (id == CanvasItem.NotificationVisibilityChanged) throw new InvalidOperationException("visibility"); };
        var child = new Probe { Name = "child" }; item.AddChild(child);
        var hidden = 0; item.Hidden += _ => hidden++; child.Hidden += _ => hidden++;
        Reject<AggregateException>(item.Hide);
        Check(hidden == 2 && !child.IsVisibleInTree, "Visibility failure does not skip hidden signals or children.");
        item.Notified = null;
    }

    private static void Drawing()
    {
        using var root = new Node(); var node = new Probe(); root.AddChild(node);
        using var tree = new SceneTree(root);
        var detachedDraws = 0;
        using var detached = new Probe { Name = "detached", Drawing = () => detachedDraws++ };
        detached.PrepareCanvas(); detached.QueueRedraw(); detached.PrepareCanvas();
        Check(detachedDraws == 1, "Detached QueueRedraw does nothing.");
        root.AddChild(detached); detached.PrepareCanvas();
        Check(detachedDraws == 2, "Attachment requests fresh recording.");
        var log = new List<int>();
        node.Notified = id => { if (id == CanvasItem.NotificationDraw) { log.Add(1); node.DrawRect(new Rect2(0, 0, 8, 8), Colors.Red); } };
        node.Draw += n => { log.Add(2); n.DrawRect(new Rect2(8, 0, 8, 8), Colors.Green); n.QueueRedraw(); };
        node.Drawing = () => { log.Add(3); node.DrawRect(new Rect2(16, 0, 8, 8), Colors.Blue); };
        node.PrepareCanvas(); node.PrepareCanvas();
        Check(log.SequenceEqual(new[] { 1, 2, 3 }), "Notification, signal and override share one drawing scope; in-scope QueueRedraw coalesces.");
        Reject<InvalidOperationException>(() => node.DrawRect(default, Colors.White));
        node.QueueRedraw(); node.Drawing = () => Reject<InvalidOperationException>(node.PrepareCanvas);
        node.PrepareCanvas();
        for (var stage = 0; stage < 3; stage++)
        {
            var failing = new Probe { Name = "failing" }; root.AddChild(failing);
            var fail = true;
            Action record = () => { failing.DrawRect(new Rect2(0, 0, 8, 8), Colors.White); if (fail) throw new InvalidOperationException("draw"); };
            failing.Notified = id => { if (id == CanvasItem.NotificationDraw && stage == 0) record(); };
            failing.Draw += _ => { if (stage == 1) record(); };
            failing.Drawing = () => { if (stage == 2) record(); };
            Reject<InvalidOperationException>(failing.PrepareCanvas);
            var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
            failing.AppendCanvas(vertices, batches, failing.GetGlobalTransform());
            Check(vertices.Count == 0 && batches.Count == 0, "Failed recording clears partial commands.");
            Reject<InvalidOperationException>(() => failing.DrawRect(default, Colors.White));
            fail = false; failing.PrepareCanvas(); failing.AppendCanvas(vertices, batches, failing.GetGlobalTransform());
            Check(vertices.Count > 0, "A failed recording can retry with its dirty flag retained.");
            root.RemoveChild(failing); failing.Dispose();
        }
    }

    internal sealed class Probe : Entity
    {
        internal Action<int>? Notified;
        internal Action? Entering, Exiting, Drawing;
        protected override void OnNotification(int what) { Notified?.Invoke(what); base.OnNotification(what); }
        protected override void OnEnterTree() => Entering?.Invoke();
        protected override void OnExitTree() => Exiting?.Invoke();
        protected override void OnDraw() => Drawing?.Invoke();
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasLifecycle(string backend)
    {
        var window = new Window { Size = new(64, 64) }; var node = new CanvasLifecycleTests.Probe(); window.AddChild(node);
        var neutral = new Node { Name = "neutral" }; var independent = new Entity();
        window.AddChild(neutral); neutral.AddChild(independent);
        var draws = 0; var frames = 0; var hidden = 0;
        node.Hidden += _ => hidden++;
        node.Notified = id => { if (id == CanvasItem.NotificationDraw) node.DrawRect(new Rect2(0, 0, 8, 8), Colors.Red); };
        node.Draw += n => n.DrawRect(new Rect2(8, 0, 8, 8), Colors.Green);
        node.Drawing = () => { draws++; node.DrawRect(new Rect2(16, 0, 8, 8), Colors.Blue); node.QueueRedraw(); };
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = renderer.Readback();
                Pixel(image, 4, 4, Colors.Red); Pixel(image, 12, 4, Colors.Green); Pixel(image, 20, 4, Colors.Blue);
                if (++frames == 1) { node.Hide(); node.Show(); }
                if (frames == 2) { window.RemoveChild(node); window.AddChild(node); }
                if (frames == 3) node.TopLevel = true;
                if (frames == 4)
                {
                    window.Hide();
                    Check(!node.IsVisibleInTree && !independent.IsVisibleInTree, "Window hides all canvas roots.");
                    window.Show();
                    Check(independent.IsVisibleInTree && hidden == 2, "Window restores neutral-root visibility and raises hidden once.");
                }
                if (frames == 6) { Check(draws == 5, "Visibility, reattachment and TopLevel trigger redraw; in-draw requests do not."); window.Tree!.Quit(); }
            };
        };
        Engine.Run(window); Released(window);
        Console.WriteLine($"Canvas lifecycle pixel checks passed: {backend}.");
    }
}
