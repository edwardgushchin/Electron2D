using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class PackedSceneTests
{
    [Fact]
    public void PackStoresOwnedSubtreeAndInstantiateRemapsOwners()
    {
        var root = new Electron2D.Node { Name = "Main" };
        var owned = new Electron2D.Node { Name = "Owned" };
        var grandOwned = new Electron2D.Node { Name = "GrandOwned" };
        var runtimeOnly = new Electron2D.Node { Name = "RuntimeOnly" };
        root.AddChild(owned);
        owned.AddChild(grandOwned);
        root.AddChild(runtimeOnly);
        owned.Owner = root;
        grandOwned.Owner = owned;
        owned.AddToGroup("persisted", persistent: true);
        owned.AddToGroup("transient");

        var scene = new Electron2D.PackedScene();

        Assert.False(scene.CanInstantiate());
        Assert.Equal(Electron2D.Error.Ok, scene.Pack(root));
        Assert.True(scene.CanInstantiate());

        var instance = Assert.IsType<Electron2D.Node>(scene.Instantiate());

        Assert.NotSame(root, instance);
        Assert.Equal("Main", instance.Name);
        Assert.Equal(1, instance.GetChildCount());

        var clonedOwned = Assert.IsType<Electron2D.Node>(instance.GetChild(0));
        Assert.NotSame(owned, clonedOwned);
        Assert.Equal("Owned", clonedOwned.Name);
        Assert.Same(instance, clonedOwned.Owner);
        Assert.True(clonedOwned.IsInGroup("persisted"));
        Assert.True(clonedOwned.IsGroupPersistent("persisted"));
        Assert.False(clonedOwned.IsInGroup("transient"));

        var clonedGrandOwned = Assert.IsType<Electron2D.Node>(clonedOwned.GetChild(0));
        Assert.Equal("GrandOwned", clonedGrandOwned.Name);
        Assert.Same(clonedOwned, clonedGrandOwned.Owner);
    }

    [Fact]
    public void PackReplacesPreviousStoredScene()
    {
        var first = new Electron2D.Node { Name = "First" };
        var second = new Electron2D.Node { Name = "Second" };
        var scene = new Electron2D.PackedScene();

        Assert.Equal(Electron2D.Error.Ok, scene.Pack(first));
        Assert.Equal(Electron2D.Error.Ok, scene.Pack(second));

        var instance = Assert.IsType<Electron2D.Node>(scene.Instantiate());
        Assert.Equal("Second", instance.Name);
    }

    [Fact]
    public void ChangeSceneToPackedDefersSceneReplacementToNextHostPass()
    {
        var tree = new Electron2D.SceneTree();
        var firstScene = PackNode("First");
        var secondScene = PackNode("Second");

        Assert.Equal(Electron2D.Error.Ok, tree.ChangeSceneToPacked(firstScene));
        Assert.Null(tree.CurrentScene);
        Assert.Equal(0, tree.Root.GetChildCount());

        tree.ProcessFrame(0.0d);

        var firstInstance = Assert.IsType<Electron2D.Node>(tree.CurrentScene);
        Assert.Equal("First", firstInstance.Name);
        Assert.Same(firstInstance, tree.Root.GetChild(0));

        Assert.Equal(Electron2D.Error.Ok, tree.ChangeSceneToPacked(secondScene));
        Assert.Null(tree.CurrentScene);
        Assert.Equal(0, tree.Root.GetChildCount());
        Assert.True(Electron2D.Object.IsInstanceValid(firstInstance));

        tree.ProcessFrame(0.0d);

        Assert.False(Electron2D.Object.IsInstanceValid(firstInstance));
        var secondInstance = Assert.IsType<Electron2D.Node>(tree.CurrentScene);
        Assert.Equal("Second", secondInstance.Name);
        Assert.Same(secondInstance, tree.Root.GetChild(0));
    }

    [Fact]
    public void InvalidPackedSceneDoesNotChangeCurrentScene()
    {
        var tree = new Electron2D.SceneTree();
        var firstScene = PackNode("First");
        var invalidScene = new Electron2D.PackedScene();

        Assert.Equal(Electron2D.Error.Ok, tree.ChangeSceneToPacked(firstScene));
        tree.ProcessFrame(0.0d);
        var firstInstance = tree.CurrentScene;

        Assert.Equal(Electron2D.Error.InvalidParameter, tree.ChangeSceneToPacked(invalidScene));

        Assert.Same(firstInstance, tree.CurrentScene);
        Assert.Same(firstInstance, tree.Root.GetChild(0));
    }

    private static Electron2D.PackedScene PackNode(string name)
    {
        var node = new Electron2D.Node { Name = name };
        var scene = new Electron2D.PackedScene();
        Assert.Equal(Electron2D.Error.Ok, scene.Pack(node));
        return scene;
    }
}
