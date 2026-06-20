using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class NodePathResolutionTests
{
    [Fact]
    public void NodePathParsesNamesSubnamesAndAbsoluteFlag()
    {
        var path = new Electron2D.NodePath("/root/World/Player:position:x");

        Assert.True(path.IsAbsolute());
        Assert.False(path.IsEmpty());
        Assert.Equal(3, path.GetNameCount());
        Assert.Equal("root", path.GetName(0));
        Assert.Equal("World", path.GetName(1));
        Assert.Equal("Player", path.GetName(2));
        Assert.Equal(2, path.GetSubnameCount());
        Assert.Equal("position", path.GetSubname(0));
        Assert.Equal("x", path.GetSubname(1));
        Assert.Equal("/root/World/Player:position:x", path.ToString());
        Assert.Equal(path, (Electron2D.NodePath)"/root/World/Player:position:x");
    }

    [Fact]
    public void GetNodeResolvesRelativeSelfParentChildAndSiblingPaths()
    {
        var tree = CreateTree();
        var world = tree.Root.GetNode("World");
        var player = world.GetNode("Player");
        var sword = player.GetNode("Sword");
        var shield = world.GetNode("Shield");

        Assert.Same(sword, player.GetNode("Sword"));
        Assert.Same(player, player.GetNode("."));
        Assert.Same(world, player.GetNode(".."));
        Assert.Same(sword, world.GetNode("Player/Sword"));
        Assert.Same(shield, sword.GetNode("../../Shield"));
    }

    [Fact]
    public void GetNodeResolvesAbsolutePathsFromSceneTreeRoot()
    {
        var tree = CreateTree();
        var sword = tree.Root.GetNode("World/Player/Sword");

        Assert.Same(tree.Root, sword.GetNode("/root"));
        Assert.Same(sword, tree.Root.GetNode("/root/World/Player/Sword"));
        Assert.Same(sword, sword.GetNode("/root/World/Player/Sword:ignored:subname"));
    }

    [Fact]
    public void MissingPathReturnsNullOrThrowsDependingOnApi()
    {
        var tree = CreateTree();
        var world = tree.Root.GetNode("World");
        var orphan = new Electron2D.Node { Name = "Orphan" };

        Assert.Null(world.GetNodeOrNull("Missing"));
        Assert.Null(world.GetNodeOrNull("../Missing"));
        Assert.Null(orphan.GetNodeOrNull("/root"));
        Assert.Throws<InvalidOperationException>(() =>
        {
            world.GetNode("Missing");
        });
    }

    [Fact]
    public void RenamedNodesResolveOnlyThroughCurrentName()
    {
        var parent = new Electron2D.Node { Name = "Parent" };
        var child = new Electron2D.Node { Name = "OldName" };
        parent.AddChild(child);

        Assert.Same(child, parent.GetNode("OldName"));

        child.Name = "NewName";

        Assert.Null(parent.GetNodeOrNull("OldName"));
        Assert.Same(child, parent.GetNode("NewName"));
    }

    private static Electron2D.SceneTree CreateTree()
    {
        var tree = new Electron2D.SceneTree();
        var world = new Electron2D.Node { Name = "World" };
        var player = new Electron2D.Node { Name = "Player" };
        var sword = new Electron2D.Node { Name = "Sword" };
        var shield = new Electron2D.Node { Name = "Shield" };
        player.AddChild(sword);
        world.AddChild(player);
        world.AddChild(shield);
        tree.Root.AddChild(world);
        return tree;
    }
}
