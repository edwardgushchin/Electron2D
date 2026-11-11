using Electron2D;

internal static class EditorProcessModeTests
{
    internal static void Run()
    {
        var oldHint = Engine.IsEditorHint;
        try
        {
            Engine.IsEditorHint = true;
            var root = new Node();
            var branch = new ModeProbe();
            var inherited = new ModeProbe();
            var independent = new ModeProbe { ProcessMode = ProcessMode.Always };
            root.AddChild(branch);
            branch.AddChild(inherited);
            branch.AddChild(independent);
            using var tree = new SceneTree(root);
            tree.EditedSceneRoot = branch;

            var delivered = 0;
            var nodeEvents = 0;
            Action<SceneTree> observe = sender =>
            {
                Check(ReferenceEquals(sender, tree), "The scene tree is the sender.");
                delivered++;
            };
            Action<SceneTree> verifyTransition = _ =>
            {
                Check(!branch.CanProcess() && !inherited.CanProcess() && independent.CanProcess(),
                    "Observers see the new inherited policy and unchanged explicit child policy.");
                Check(branch.DisabledCount == 1 && inherited.DisabledCount == 1 && independent.DisabledCount == 0,
                    "Process transition notifications finish before the tree event.");
                Check(nodeEvents == 0, "The tree event precedes the selected node's editor-state event.");
            };
            tree.TreeProcessModeChanged += observe;
            tree.TreeProcessModeChanged += verifyTransition;
            branch.EditorStateChanged += _ => nodeEvents++;

            branch.ProcessMode = ProcessMode.Disabled;
            Check(delivered == 1 && nodeEvents == 1, "One change publishes once for the whole affected branch.");
            tree.TreeProcessModeChanged -= verifyTransition;

            branch.ProcessMode = ProcessMode.Disabled;
            Reject<ArgumentOutOfRangeException>(() => branch.ProcessMode = (ProcessMode)int.MaxValue);
            Reject<InvalidOperationException>(() => root.ProcessMode = ProcessMode.Inherit);
            tree.Paused = true;
            tree.Paused = false;
            using var detached = new Node { ProcessMode = ProcessMode.Disabled };
            Check(delivered == 1 && nodeEvents == 1, "Equal, invalid, detached and tree pause changes do not publish.");

            root.ProcessMode = ProcessMode.Always;
            Check(delivered == 2 && nodeEvents == 1, "An attached change outside the edited subtree still publishes.");
            Engine.IsEditorHint = false;
            branch.ProcessMode = ProcessMode.Inherit;
            Check(delivered == 2 && inherited.CanProcess(), "The runtime keeps the mode transition without the editor tree event.");

            Engine.IsEditorHint = true;
            tree.SetBlockSignals(true);
            branch.ProcessMode = ProcessMode.Disabled;
            tree.SetBlockSignals(false);
            Check(delivered == 2 && !inherited.CanProcess(), "Blocking tree signals preserves committed processing.");

            var failure = new InvalidOperationException("observer failure");
            var laterCalls = 0;
            Action<SceneTree> throwing = _ => throw failure;
            Action<SceneTree> later = _ => laterCalls++;
            tree.TreeProcessModeChanged += throwing;
            tree.TreeProcessModeChanged += later;
            var before = nodeEvents;
            var aggregate = Reject<AggregateException>(() => branch.ProcessMode = ProcessMode.Inherit);
            Check(aggregate.InnerExceptions.Count == 1 && ReferenceEquals(aggregate.InnerExceptions[0], failure),
                "The setter preserves the observer failure.");
            Check(laterCalls == 0 && nodeEvents == before + 1 && inherited.CanProcess(),
                "The invocation stops at a failure, then the node event runs with the policy committed.");
            tree.TreeProcessModeChanged -= throwing;
            tree.TreeProcessModeChanged -= later;

            before = delivered;
            inherited.OnDisabled = () => branch.RemoveChild(inherited);
            inherited.ProcessMode = ProcessMode.Disabled;
            Check(inherited.Tree is null && delivered == before, "A transition cannot publish for a retired attachment.");
            inherited.Dispose();
            tree.TreeProcessModeChanged -= observe;
        }
        finally { Engine.IsEditorHint = oldHint; }

        Console.WriteLine("PASS editor process mode ordering, inheritance, event scope, blocking, callback failure and detachment.");
    }

    private sealed class ModeProbe : Node
    {
        internal int DisabledCount { get; private set; }
        internal Action? OnDisabled { get; set; }

        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationDisabled) return;
            DisabledCount++;
            OnDisabled?.Invoke();
        }
    }

    private static T Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T error) { return error; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
