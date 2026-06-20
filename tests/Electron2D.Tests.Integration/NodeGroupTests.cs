using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class NodeGroupTests
{
    [Fact]
    public void NodeGroupsTrackLocalMembershipAndPersistence()
    {
        var node = new Electron2D.Node();

        node.AddToGroup("enemies");
        node.AddToGroup("persistent", persistent: true);
        node.AddToGroup("persistent");

        Assert.True(node.IsInGroup("enemies"));
        Assert.True(node.IsInGroup("persistent"));
        Assert.False(node.IsInGroup("missing"));
        Assert.False(node.IsGroupPersistent("enemies"));
        Assert.True(node.IsGroupPersistent("persistent"));
        Assert.Equal(new[] { "enemies", "persistent" }, node.GetGroups().OrderBy(group => group).ToArray());

        node.RemoveFromGroup("enemies");

        Assert.False(node.IsInGroup("enemies"));
        Assert.False(node.IsGroupPersistent("enemies"));
        Assert.Equal(new[] { "persistent" }, node.GetGroups());
        Assert.Throws<ArgumentException>(() => node.AddToGroup(" "));
    }

    [Fact]
    public void SceneTreeGroupQueriesUseInsideTreeNodesInHierarchyOrder()
    {
        var tree = new Electron2D.SceneTree();
        var parent = new Electron2D.Node { Name = "Parent" };
        var child = new Electron2D.Node { Name = "Child" };
        var sibling = new Electron2D.Node { Name = "Sibling" };
        var orphan = new Electron2D.Node { Name = "Orphan" };
        parent.AddToGroup("actors");
        child.AddToGroup("actors", persistent: true);
        sibling.AddToGroup("actors");
        orphan.AddToGroup("actors");
        parent.AddChild(child);
        tree.Root.AddChild(parent);
        tree.Root.AddChild(sibling);

        Assert.Equal(new[] { parent, child, sibling }, tree.GetNodesInGroup("actors"));
        Assert.Same(parent, tree.GetFirstNodeInGroup("actors"));
        Assert.Equal(3, tree.GetNodeCountInGroup("actors"));
        Assert.True(tree.HasGroup("actors"));

        tree.Root.RemoveChild(parent);

        Assert.Equal(new[] { sibling }, tree.GetNodesInGroup("actors"));
        Assert.True(child.IsInGroup("actors"));
        Assert.True(child.IsGroupPersistent("actors"));

        tree.Root.AddChild(parent);

        Assert.Equal(new[] { sibling, parent, child }, tree.GetNodesInGroup("actors"));
    }

    [Fact]
    public void CallGroupInvokesMatchingMethodInHierarchyOrder()
    {
        var calls = new List<string>();
        var tree = new Electron2D.SceneTree();
        var first = new GroupCallableNode("First", calls);
        var passive = new Electron2D.Node { Name = "Passive" };
        var second = new GroupCallableNode("Second", calls);
        first.AddToGroup("receivers");
        passive.AddToGroup("receivers");
        second.AddToGroup("receivers");
        tree.Root.AddChild(first);
        tree.Root.AddChild(passive);
        tree.Root.AddChild(second);

        tree.CallGroup("receivers", nameof(GroupCallableNode.Record), "tick");

        Assert.Equal(new[] { "First:tick", "Second:tick" }, calls);
    }

    private sealed class GroupCallableNode : Electron2D.Node
    {
        private readonly List<string> _calls;

        public GroupCallableNode(string name, List<string> calls)
        {
            Name = name;
            _calls = calls;
        }

        public void Record(string value)
        {
            _calls.Add($"{Name}:{value}");
        }
    }
}
