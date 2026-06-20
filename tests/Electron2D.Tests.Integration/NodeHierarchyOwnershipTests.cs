using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class NodeHierarchyOwnershipTests
{
    [Fact]
    public void PublicApiUsesGodotLikeParentAccessAndOwner()
    {
        var nodeType = typeof(Electron2D.Node);

        Assert.Null(nodeType.GetProperty("Parent"));
        Assert.NotNull(nodeType.GetMethod("GetParent", Type.EmptyTypes));
        Assert.NotNull(nodeType.GetProperty("Owner"));
        Assert.NotNull(typeof(Electron2D.Object).GetMethod("IsQueuedForDeletion", Type.EmptyTypes));
    }

    [Fact]
    public void ChildOrderIndexAndMoveChildStayConsistent()
    {
        var parent = new Electron2D.Node { Name = "Parent" };
        var first = new Electron2D.Node { Name = "First" };
        var second = new Electron2D.Node { Name = "Second" };
        var third = new Electron2D.Node { Name = "Third" };

        parent.AddChild(first);
        parent.AddChild(second);
        parent.AddChild(third);

        Assert.Same(parent, first.GetParent());
        Assert.Same(first, parent.GetChild(0));
        Assert.Same(third, parent.GetChild(-1));
        Assert.Equal(1, second.GetIndex());

        parent.MoveChild(third, 0);

        Assert.Same(third, parent.GetChild(0));
        Assert.Same(first, parent.GetChild(1));
        Assert.Same(second, parent.GetChild(2));
        Assert.Equal(0, third.GetIndex());
        Assert.Equal(2, second.GetIndex());
        Assert.Null(parent.GetChild(3));
        Assert.Null(parent.GetChild(-4));
    }

    [Fact]
    public void AddChildAndReparentRejectCycles()
    {
        var root = new Electron2D.Node { Name = "Root" };
        var parent = new Electron2D.Node { Name = "Parent" };
        var child = new Electron2D.Node { Name = "Child" };
        var grandchild = new Electron2D.Node { Name = "Grandchild" };

        Assert.Throws<InvalidOperationException>(() => root.AddChild(root));

        root.AddChild(parent);
        parent.AddChild(child);
        child.AddChild(grandchild);

        Assert.Throws<InvalidOperationException>(() => grandchild.AddChild(parent));
        Assert.Throws<InvalidOperationException>(() => parent.Reparent(grandchild));
    }

    [Fact]
    public void OwnerMustBeAncestorAndRemoveChildPreservesReusableSubtree()
    {
        var tree = new Electron2D.SceneTree();
        var parent = new Electron2D.Node { Name = "Parent" };
        var child = new Electron2D.Node { Name = "Child" };
        var grandchild = new Electron2D.Node { Name = "Grandchild" };
        parent.AddChild(child);
        child.AddChild(grandchild);
        tree.Root.AddChild(parent);

        child.Owner = tree.Root;
        grandchild.Owner = child;

        Assert.Same(tree.Root, child.Owner);
        Assert.Same(child, grandchild.Owner);
        Assert.Throws<InvalidOperationException>(() => parent.Owner = child);

        tree.Root.RemoveChild(parent);

        Assert.True(Electron2D.Object.IsInstanceValid(parent));
        Assert.Null(parent.GetParent());
        Assert.Null(child.Owner);
        Assert.Same(child, grandchild.Owner);

        tree.Root.AddChild(parent);

        Assert.Same(tree.Root, parent.GetParent());
        Assert.True(parent.IsInsideTree());
    }

    [Fact]
    public void ReparentPreservesOwnerOnlyWhenOwnerRemainsAncestor()
    {
        var tree = new Electron2D.SceneTree();
        var firstParent = new Electron2D.Node { Name = "FirstParent" };
        var secondParent = new Electron2D.Node { Name = "SecondParent" };
        var child = new Electron2D.Node { Name = "Child" };
        var orphanParent = new Electron2D.Node { Name = "OrphanParent" };
        firstParent.AddChild(child);
        tree.Root.AddChild(firstParent);
        tree.Root.AddChild(secondParent);
        child.Owner = tree.Root;

        child.Reparent(secondParent);

        Assert.Same(secondParent, child.GetParent());
        Assert.Same(tree.Root, child.Owner);
        Assert.True(child.IsInsideTree());

        child.Reparent(orphanParent);

        Assert.Same(orphanParent, child.GetParent());
        Assert.Null(child.Owner);
        Assert.False(child.IsInsideTree());
        Assert.Null(child.GetTree());
    }

    [Fact]
    public void QueueFreeDeletesSubtreeAfterCurrentTraversalAndKeepsSiblingsRunning()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var deleting = new QueueFreeOnProcessNode("Deleting", events);
        var descendant = new ProcessRecordingNode("Descendant", events);
        var sibling = new ProcessRecordingNode("Sibling", events);
        deleting.AddChild(descendant);
        tree.Root.AddChild(deleting);
        tree.Root.AddChild(sibling);
        events.Clear();

        tree.ProcessFrame(0.25d);

        Assert.Equal(
            new[]
            {
                "Deleting:_Process:queued:True",
                "Descendant:_Process",
                "Sibling:_Process",
                "Descendant:_ExitTree",
                "Deleting:_ExitTree"
            },
            events);
        Assert.False(Electron2D.Object.IsInstanceValid(deleting));
        Assert.False(Electron2D.Object.IsInstanceValid(descendant));
        Assert.True(Electron2D.Object.IsInstanceValid(sibling));
        Assert.Same(sibling, tree.Root.GetChild(0));
        Assert.Equal(1, tree.Root.GetChildCount());
    }

    private sealed class ProcessRecordingNode : Electron2D.Node
    {
        private readonly List<string> _events;

        public ProcessRecordingNode(string name, List<string> events)
        {
            Name = name;
            _events = events;
        }

        public override void _Process(double delta)
        {
            _events.Add($"{Name}:_Process");
        }

        public override void _ExitTree()
        {
            _events.Add($"{Name}:_ExitTree");
        }
    }

    private sealed class QueueFreeOnProcessNode : Electron2D.Node
    {
        private readonly List<string> _events;

        public QueueFreeOnProcessNode(string name, List<string> events)
        {
            Name = name;
            _events = events;
        }

        public override void _Process(double delta)
        {
            QueueFree();
            QueueFree();
            _events.Add($"{Name}:_Process:queued:{IsQueuedForDeletion()}");
        }

        public override void _ExitTree()
        {
            _events.Add($"{Name}:_ExitTree");
        }
    }
}
