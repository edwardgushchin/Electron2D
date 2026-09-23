using Electron2D;

internal static class NodeTreeDiagnosticsTests
{
    internal static void Run()
    {
        var root = new Probe("Root");
        var first = new Probe("A");
        var grandchild = new Probe("A1");
        var last = new Probe("B");
        root.AddChild(first); first.AddChild(grandchild); root.AddChild(last);

        Check(root.HasNode("A/A1") && root.HasNode("/Root/A/A1") && !root.HasNode("A/Absent"), "Node path presence.");
        Check(root.GetTreeString() == ".\nA\nA/A1\nB\n", "Relative tree paths and order.");
        Check(root.GetTreeStringPretty() == " ┖╴Root\n    ┠╴A\n    ┃  ┖╴A1\n    ┖╴B\n", "Indented tree glyphs and branches.");
        using (var output = new StringWriter())
        {
            var prior = Console.Out;
            try
            {
                Console.SetOut(output);
                root.PrintTree(); root.PrintTreePretty();
            }
            finally { Console.SetOut(prior); }
            Check(output.ToString() == root.GetTreeString() + Environment.NewLine + root.GetTreeStringPretty() + Environment.NewLine,
                "Printing uses the same strings and one trailing line break.");
        }

        using (var tree = new SceneTree(root))
        {
            Check(last.IsGreaterThan(first) && last.IsGreaterThan(grandchild) && grandchild.IsGreaterThan(first) &&
                  !first.IsGreaterThan(last) && !first.IsGreaterThan(first), "Attached depth-first order.");
            root.MoveChild(last, 0);
            Check(!last.IsGreaterThan(first) && first.IsGreaterThan(last), "Order reflects sibling moves.");
            using var detached = new Probe("Detached");
            Reject<InvalidOperationException>(() => detached.IsGreaterThan(first));
            Reject<InvalidOperationException>(() => System.Threading.Tasks.Task.Run(() => root.GetTreeString()).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => System.Threading.Tasks.Task.Run(() => root.HasNode("A")).GetAwaiter().GetResult());

            var visited = new List<string>();
            foreach (var probe in new[] { root, first, grandchild, last }) probe.Visited = visited;
            first.CustomNotification = () => root.RemoveChild(last);
            grandchild.CustomNotification = () => throw new InvalidOperationException("Injected descendant failure.");
            Reject<AggregateException>(() => root.PropagateNotification(9100));
            Check(visited.SequenceEqual(["Root", "B", "A", "A1"]) && ReferenceEquals(last.Parent, root),
                "Propagation is parent-first, blocks structural changes and continues after callback failures.");
            first.CustomNotification = null; grandchild.CustomNotification = null; visited.Clear();
            root.PropagateNotification(9100);
            Check(visited.SequenceEqual(["Root", "B", "A", "A1"]), "Propagation guard clears after failure.");
            first.CustomNotification = () => grandchild.Dispose();
            Reject<AggregateException>(() => root.PropagateNotification(9100));
            Check(!grandchild.IsDisposed && ReferenceEquals(grandchild.Parent, first),
                "A callback cannot partially dispose a child during propagation.");
            first.CustomNotification = null;
            root.MoveChild(last, 1);
        }
        Console.WriteLine("Node tree paths, ordering, diagnostics and notification propagation passed.");
    }

    private sealed class Probe : Node
    {
        internal Probe(string name) => Name = name;
        internal List<string>? Visited;
        internal Action? CustomNotification;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != 9100) return;
            Visited?.Add(Name);
            CustomNotification?.Invoke();
        }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
