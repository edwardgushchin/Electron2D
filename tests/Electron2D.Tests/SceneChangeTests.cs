using Electron2D;

internal static class SceneChangeTests
{
    internal static void Run()
    {
        var events = new List<string>();
        var host = new Node { Name = "Host" };
        var overlay = new Node { Name = "Overlay" };
        host.AddChild(overlay);
        using (var tree = new SceneTree(host))
        {
            var first = new Probe("First", events);
            host.AddChild(first);
            Check(tree.CurrentScene is null, "The tree has no selected scene initially.");
            tree.CurrentScene = first;
            Check(tree.GetPropertyList().Any(item => item.Name == nameof(SceneTree.CurrentScene)), "Selected scene is inspectable.");
            Reject<ArgumentException>(() => tree.CurrentScene = new Node());
            Reject<InvalidOperationException>(() => tree.ChangeSceneToNode(overlay));

            tree.SceneChanged += sender =>
            {
                Check(ReferenceEquals(sender, tree) && tree.CurrentScene?.IsInsideTree == true,
                    "SceneChanged follows entry and can observe the selected scene.");
                events.Add("changed");
            };
            first.Exiting = () => Reject<InvalidOperationException>(tree.Dispose);
            var second = new Probe("Second", events);
            tree.ChangeSceneToNode(second);
            Check(tree.CurrentScene is null && first.Parent is null && !first.IsDisposed && second.Parent is null,
                "The old scene exits immediately and the new scene waits for a safe point.");
            tree.FlushDeferred();
            Check(first.IsDisposed && ReferenceEquals(tree.CurrentScene, second) && ReferenceEquals(second.Parent, host) &&
                  events.IndexOf("dispose:First") < events.IndexOf("enter:Second") &&
                  events.IndexOf("ready:Second") < events.IndexOf("changed") && ReferenceEquals(overlay.Parent, host),
                "Old scene disposal precedes new entry, readiness precedes the event, and other root children remain.");

            events.Clear();
            var superseded = new Probe("Superseded", events);
            var third = new Probe("Third", events);
            tree.ChangeSceneToNode(superseded);
            tree.ChangeSceneToNode(third);
            tree.FlushDeferred();
            Check(second.IsDisposed && superseded.IsDisposed && ReferenceEquals(tree.CurrentScene, third) &&
                  !events.Contains("enter:Superseded") && events.Count(item => item == "changed") == 1,
                "The latest pending scene wins and the replaced node is disposed without entry.");

            using var template = new Node { Name = "Packed" };
            var leaf = new Node { Name = "Leaf" };
            template.AddChild(leaf); leaf.Owner = template;
            using var packed = new PackedScene(); packed.Pack(template);
            tree.ChangeSceneToPacked(packed);
            tree.FlushDeferred();
            Check(third.IsDisposed && tree.CurrentScene?.Name == "Packed" && tree.CurrentScene.HasNode("Leaf"),
                "A packed scene becomes an independent active hierarchy.");
            using var empty = new PackedScene();
            Reject<InvalidOperationException>(() => tree.ChangeSceneToPacked(empty));
            Check(tree.CurrentScene?.Name == "Packed", "A failed instantiation preserves the active scene.");

            var selected = tree.CurrentScene!;
            tree.CurrentScene = null;
            Check(ReferenceEquals(selected.Parent, host) && !selected.IsDisposed, "Clearing selection does not remove a child.");
            tree.CurrentScene = selected;
            tree.UnloadCurrentScene();
            Check(selected.IsDisposed && tree.CurrentScene is null && ReferenceEquals(overlay.Parent, host),
                "Unload disposes only the selected scene.");

            var manual = new Node { Name = "Manual" };
            host.AddChild(manual); tree.CurrentScene = manual;
            tree.NodeRemoved += (_, node) => { if (ReferenceEquals(node, manual)) Check(tree.CurrentScene is null, "Removal clears selection before its event."); };
            host.RemoveChild(manual);
            Check(tree.CurrentScene is null && !manual.IsDisposed, "External removal returns ownership to the caller.");
            manual.Dispose();
            using var offThread = new Node { Name = "OffThread" };
            Reject<InvalidOperationException>(() => System.Threading.Tasks.Task.Run(() => tree.ChangeSceneToNode(offThread)).GetAwaiter().GetResult());
        }

        var pendingHost = new Node { Name = "Host" };
        using (var tree = new SceneTree(pendingHost))
        {
            var pending = new Node { Name = "Pending" };
            tree.ChangeSceneToNode(pending);
            tree.Dispose();
            Check(pending.IsDisposed, "Tree disposal releases a scene that never reached the safe point.");
        }

        var failureHost = new Node { Name = "Host" };
        using (var tree = new SceneTree(failureHost))
        {
            var pending = new Node { Name = "Pending" };
            tree.ChangeSceneToNode(pending);
            var blocker = new Node { Name = "Pending" };
            failureHost.AddChild(blocker);
            Reject<AggregateException>(tree.FlushDeferred);
            Check(pending.IsDisposed && tree.CurrentScene is null && ReferenceEquals(blocker.Parent, failureHost),
                "A failed attachment disposes the accepted scene without disturbing an unrelated child.");
        }

        var callbackHost = new Node { Name = "Host" };
        using (var tree = new SceneTree(callbackHost))
        {
            var previous = new Probe("Previous", []);
            callbackHost.AddChild(previous); tree.CurrentScene = previous;
            previous.Exiting = () => throw new ApplicationException("Exit failure");
            var next = new Node { Name = "Next" };
            Reject<AggregateException>(() => tree.ChangeSceneToNode(next));
            Check(previous.Parent is null && !previous.IsDisposed && next.Parent is null,
                "An exit callback failure does not leave the accepted replacement attached early.");
            tree.FlushDeferred();
            Check(previous.IsDisposed && ReferenceEquals(tree.CurrentScene, next),
                "An accepted replacement completes after an exit callback failure.");
        }

        var entryHost = new Node { Name = "Host" };
        using (var tree = new SceneTree(entryHost))
        {
            var changed = false;
            tree.SceneChanged += _ => changed = true;
            var failedEntry = new Probe("FailedEntry", []) { Entering = () => throw new ApplicationException("Enter failure") };
            tree.ChangeSceneToNode(failedEntry);
            Reject<AggregateException>(tree.FlushDeferred);
            Check(ReferenceEquals(tree.CurrentScene, failedEntry) && failedEntry.IsInsideTree &&
                  !failedEntry.IsNodeReady && !changed,
                "An attached scene whose entry failed remains owned but does not emit SceneChanged before ready.");
        }

        var frameHost = new Node { Name = "Host" };
        using (var tree = new SceneTree(frameHost))
        {
            var previous = new Probe("FrameScene", []);
            frameHost.AddChild(previous); tree.CurrentScene = previous;
            previous.ProcessEnabled = true;
            previous.Processing = () => tree.ChangeSceneToNode(new Node { Name = "FrameNext" });
            tree.ProcessFrame(0);
            Check(previous.IsDisposed && tree.CurrentScene?.Name == "FrameNext",
                "A change requested during node processing completes at that frame's safe point.");
            tree.SceneChanged += _ => throw new ApplicationException("Scene observer failure");
            tree.ChangeSceneToNode(new Node { Name = "AfterFailure" });
            Reject<AggregateException>(tree.FlushDeferred);
            Check(tree.CurrentScene?.Name == "AfterFailure" && tree.CurrentScene.IsInsideTree,
                "A scene observer failure does not roll back the attached scene.");
        }
        Console.WriteLine("Scene change ownership, safe points, lifecycle and failure checks passed.");
    }

    private sealed class Probe : Node
    {
        private readonly List<string> _events;
        private readonly string _name;
        internal Action? Exiting;
        internal Action? Entering;
        internal Action? Processing;
        internal Probe(string name, List<string> events) { Name = name; _name = name; _events = events; }
        protected override void OnEnterTree() { base.OnEnterTree(); _events.Add("enter:" + _name); Entering?.Invoke(); }
        protected override void OnReady() { base.OnReady(); _events.Add("ready:" + _name); }
        protected override void OnExitTree() { base.OnExitTree(); _events.Add("exit:" + _name); Exiting?.Invoke(); }
        protected override void OnProcess(double delta) { base.OnProcess(delta); Processing?.Invoke(); }
        protected override void Dispose(bool disposing) { _events.Add("dispose:" + _name); base.Dispose(disposing); }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
