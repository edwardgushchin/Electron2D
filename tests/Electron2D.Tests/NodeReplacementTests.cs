using Electron2D;

internal static class NodeReplacementTests
{
    internal static void Run()
    {
        DetachedReplacement();
        AttachedReplacement();
        SceneLocalResources();
        FailedChildTransfer();
        CallbackTopologyChange();
        DisposalDuringRemoval();
        DisposalDuringNotification();
        Validation();
        Console.WriteLine("Node subtree replacement, ownership and failure checks passed.");
    }

    private static void DetachedReplacement()
    {
        using var root = new Node { Name = "Root" };
        var first = new Node { Name = "First" };
        var old = new Node { Name = "Old" };
        var last = new Node { Name = "Last" };
        root.AddChild(first); root.AddChild(old); root.AddChild(last);
        old.Owner = root;
        old.AddToGroup("stored", persistent: true);
        old.AddToGroup("runtime");
        var direct = new Node { Name = "Direct", UniqueNameInOwner = true };
        var nested = new Node { Name = "Nested" };
        old.AddChild(direct); direct.Owner = old;
        direct.AddChild(nested); nested.Owner = root;
        var replacement = new Node { Name = "Replacement" };
        var existing = new Node { Name = "Existing" };
        replacement.AddChild(existing);
        var notified = false;
        old.ReplacingBy += received =>
        {
            notified = ReferenceEquals(received, replacement) && old.Parent is null &&
                       ReferenceEquals(replacement.Parent, root) && old.ChildCount == 1 &&
                       replacement.ChildCount == 1;
        };

        old.ReplaceBy(replacement, keepGroups: true);
        Check(notified && root.Children.SequenceEqual([first, replacement, last]) &&
              replacement.Children.SequenceEqual([existing, direct]) &&
              ReferenceEquals(replacement.Owner, root) && ReferenceEquals(direct.Owner, replacement) &&
              ReferenceEquals(nested.Owner, root) && root.GetNode("Replacement/%Direct") == direct &&
              old.Parent is null && old.ChildCount == 0 && !old.IsDisposed &&
              replacement.IsInGroup("stored") && replacement.IsInGroup("runtime"),
            "A detached replacement retains sibling order, appends children and remaps ownership and groups.");
        using var packed = new PackedScene();
        packed.Pack(root);
        using var copy = packed.Instantiate();
        Check(copy.GetNode("Replacement").IsInGroup("stored") &&
              !copy.GetNode("Replacement").IsInGroup("runtime"),
            "Copied persistent membership survives packed-scene instantiation.");
        old.Dispose();
    }

    private static void AttachedReplacement()
    {
        using var root = new Node { Name = "Root" };
        var old = new Node { Name = "Scene" };
        var child = new Node { Name = "Child" };
        root.AddChild(old); old.AddChild(child);
        old.Owner = root; child.Owner = old;
        old.AddToGroup("runtime");
        using var tree = new SceneTree(root);
        tree.CurrentScene = old;
        tree.EditedSceneRoot = old;
        var replacement = new Node { Name = "Scene" };
        var notified = false;
        old.ReplacingBy += node =>
        {
            notified = ReferenceEquals(node, replacement) && ReferenceEquals(node.Tree, tree) &&
                       old.Tree is null && ReferenceEquals(child.Parent, old);
            throw new ApplicationException("replacement observer failed");
        };
        Reject<AggregateException>(() => old.ReplaceBy(replacement));
        Check(notified && root.Children.SequenceEqual([replacement]) &&
              ReferenceEquals(child.Parent, replacement) && ReferenceEquals(child.Owner, replacement) &&
              ReferenceEquals(replacement.Tree, tree) && old.Parent is null && old.Tree is null &&
              tree.CurrentScene is null && tree.EditedSceneRoot is null &&
              old.IsInGroup("runtime") && !replacement.IsInGroup("runtime"),
            "A failing replacement observer does not strand children or tree selection.");
        tree.CurrentScene = replacement;
        old.Dispose();
    }

    private static void Validation()
    {
        using var root = new Node { Name = "Root" };
        var old = new Node { Name = "Old" };
        var conflict = new Node { Name = "Conflict" };
        root.AddChild(old); root.AddChild(conflict);
        Reject<ArgumentNullException>(() => old.ReplaceBy(null!));
        Reject<ArgumentException>(() => old.ReplaceBy(old));
        Reject<InvalidOperationException>(() => old.ReplaceBy(conflict));
        using var collidingName = new Node { Name = "Conflict" };
        Reject<InvalidOperationException>(() => old.ReplaceBy(collidingName));
        var child = new Node { Name = "Child" };
        old.AddChild(child);
        using var replacement = new Node { Name = "Replacement" };
        replacement.AddChild(new Node { Name = "Child" });
        Reject<InvalidOperationException>(() => old.ReplaceBy(replacement));
        Check(ReferenceEquals(old.Parent, root) && ReferenceEquals(child.Parent, old) &&
              replacement.Parent is null, "Invalid replacements leave the original hierarchy intact.");
        using var tree = new SceneTree(root);
        Reject<InvalidOperationException>(() => root.ReplaceBy(new Node { Name = "NewRoot" }));
        Exception? wrongThread = null;
        Task.Run(() =>
        {
            try { old.ReplaceBy(new Node { Name = "WrongThread" }); }
            catch (Exception error) { wrongThread = error; }
        }).GetAwaiter().GetResult();
        Check(wrongThread is InvalidOperationException && ReferenceEquals(old.Parent, root),
            "An attached replacement requires the scene owner thread before mutation.");
    }

    private static void SceneLocalResources()
    {
        using var source = new PackedTestNode { Name = "Source" };
        using var data = new PackedTestResource { ResourceLocalToScene = true };
        source.Data = data;
        using var packed = new PackedScene();
        packed.Pack(source);
        var instance = (PackedTestNode)packed.Instantiate();
        var local = instance.Data!;
        Check(ReferenceEquals(local.GetLocalScene(), instance),
            "An instantiated local resource initially belongs to its scene root.");
        using var replacement = new PackedTestNode { Name = "Replacement" };
        instance.ReplaceBy(replacement);
        instance.Dispose();
        Check(!local.IsDisposed && ReferenceEquals(local.GetLocalScene(), replacement),
            "Replacing a packed scene root transfers local-resource ownership and association.");
        replacement.Dispose();
        Check(local.IsDisposed, "The replacement disposes transferred local resources exactly once.");
    }

    private static void FailedChildTransfer()
    {
        using var root = new Node { Name = "Root" };
        var old = new Node { Name = "Old" };
        var child = new Node { Name = "Child" };
        root.AddChild(old); old.AddChild(child); child.Owner = old;
        var replacement = new Node { Name = "Replacement" };
        old.ReplacingBy += node => node.AddChild(new Node { Name = "Child" });
        Reject<AggregateException>(() => old.ReplaceBy(replacement));
        Check(ReferenceEquals(replacement.Parent, root) && ReferenceEquals(child.Parent, old) &&
              ReferenceEquals(child.Owner, old),
            "A callback-created name collision returns an untransferred child to the original node.");
        old.Dispose();
    }

    private static void CallbackTopologyChange()
    {
        using var root = new Node { Name = "Root" };
        var old = new Node { Name = "Old" };
        root.AddChild(old);
        var replacement = new Node { Name = "Replacement" };
        old.ReplacingBy += _ => root.AddChild(old);
        Reject<AggregateException>(() => old.ReplaceBy(replacement));
        Check(ReferenceEquals(old.Parent, root) && ReferenceEquals(replacement.Parent, root),
            "A callback that reattaches the old node cannot report successful replacement.");
    }

    private static void DisposalDuringRemoval()
    {
        using var root = new Node { Name = "Root" };
        var old = new Node { Name = "Old" };
        root.AddChild(old);
        using var replacement = new Node { Name = "Replacement" };
        root.ChildRemoved += (_, removed) => removed.Dispose();
        Reject<AggregateException>(() => old.ReplaceBy(replacement));
        Check(old.IsDisposed && replacement.Parent is null && root.ChildCount == 0,
            "Disposal by a removal callback aborts replacement without reporting success.");
    }

    private static void DisposalDuringNotification()
    {
        var old = new Node { Name = "Old" };
        using var replacement = new Node { Name = "Replacement" };
        old.ReplacingBy += _ => old.Dispose();
        Reject<AggregateException>(() => old.ReplaceBy(replacement));
        Check(old.IsDisposed && replacement.Parent is null,
            "Disposal by a detached replacement observer aborts the operation.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
