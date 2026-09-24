using Electron2D;

internal static class NodeUniqueNameTests
{
    internal static void Run()
    {
        using var root = new Node { Name = "Root" };
        var left = new Node { Name = "Left" };
        var right = new Node { Name = "Right" };
        root.AddChild(left); root.AddChild(right);
        left.Owner = right.Owner = root;
        var unique = new Node { Name = "Unique", UniqueNameInOwner = true };
        left.AddChild(unique);
        unique.Owner = root;
        var nested = new Node { Name = "Nested", UniqueNameInOwner = true };
        unique.AddChild(nested);
        nested.Owner = root;

        Check(root.GetNode("%Unique") == unique && right.GetNode("%Unique") == unique &&
              right.GetNode("%Unique/%Nested") == nested && right.GetNodeOrNull("/Root/%Nested") is null,
            "Unique paths resolve through the common owner; detached absolute paths are unavailable.");
        Check(right.GetPathTo(unique, useUniquePath: true) == "%Unique" &&
              right.GetPathTo(nested, useUniquePath: true) == "%Nested" &&
              unique.GetPathTo(right, useUniquePath: true) == "%Unique/../../Right" &&
              unique.GetPathTo(right) == "../../Right",
            "Unique path construction follows target and source shortcut rules without changing the default path.");
        Check(unique.GetNode(unique.GetPathTo(right, useUniquePath: true)) == right,
            "A source-side unique path remains resolvable.");
        using var unrelated = new Node { Name = "Unrelated" };
        Reject<InvalidOperationException>(() => right.GetPathTo(unrelated, useUniquePath: true));

        var scene = new Node { Name = "Scene" };
        left.AddChild(scene);
        scene.Owner = root;
        var local = new Node { Name = "Local", UniqueNameInOwner = true };
        scene.AddChild(local);
        local.Owner = scene;
        Check(scene.GetNode("%Local") == local && right.GetNodeOrNull("%Local") is null &&
              !right.HasNode("%Local"), "Unique lookup stays within the current owner scope.");

        var first = new Node { Name = "Same" };
        left.AddChild(first);
        first.Owner = root;
        first.UniqueNameInOwner = true;
        var second = new Node { Name = "Same" };
        right.AddChild(second);
        second.Owner = root;
        second.UniqueNameInOwner = true;
        Check(!second.UniqueNameInOwner && right.GetNode("%Same") == first,
            "A later duplicate claim loses without replacing the first owner-scoped name.");
        first.Name = "Renamed";
        Check(root.GetNodeOrNull("%Same") is null && root.GetNode("%Renamed") == first &&
              !second.UniqueNameInOwner, "Renaming releases the old claim without promoting a failed claimant.");
        second.UniqueNameInOwner = true;
        Check(root.GetNode("%Same") == second, "The claimant can retry after the conflict is removed.");

        var delayed = new Node { Name = "Renamed", UniqueNameInOwner = true };
        right.AddChild(delayed);
        delayed.Owner = root;
        Check(!delayed.UniqueNameInOwner, "Assigning an owner also checks a pending unique claim.");
        first.Name = "Same";
        Check(!first.UniqueNameInOwner && root.GetNode("%Same") == second,
            "Renaming into an existing owner-scoped claim revokes the renamed node's flag.");
        first.Name = "Renamed";
        first.UniqueNameInOwner = true;
        left.RemoveChild(first);
        Check(first.Owner is null && first.UniqueNameInOwner && root.GetNodeOrNull("%Renamed") is null,
            "Detaching releases owner lookup while retaining the node's configured flag.");
        left.AddChild(first);
        first.Owner = root;
        Check(root.GetNode("%Renamed") == first, "A restored owner reacquires the unique name.");

        using var packed = new PackedScene();
        packed.Pack(root);
        using var copy = packed.Instantiate();
        var copyUnique = copy.GetNode("Left/Unique");
        Check(copyUnique.UniqueNameInOwner && copy.GetNode("%Unique") == copyUnique &&
              copy.GetNode("Right").GetNode("%Same") == copy.GetNode("Right/Same"),
            "PackedScene restores unique flags after owner assignments.");
        using var nestedScene = new PackedScene();
        nestedScene.Pack(scene);
        using var nestedCopy = nestedScene.Instantiate();
        Check(nestedCopy.GetNode("%Local") == nestedCopy.GetNode("Local"),
            "An owned nested scene retains its own unique-name scope.");

        var attachedRoot = new Node { Name = "Attached" };
        var attachedUnique = new Node { Name = "Before", UniqueNameInOwner = true };
        attachedRoot.AddChild(attachedUnique);
        attachedUnique.Owner = attachedRoot;
        using (var tree = new SceneTree(attachedRoot))
        {
            Check(attachedRoot.GetNode("/Attached/%Before") == attachedUnique &&
                  attachedRoot.GetNodeOrNull("/%Before") is null,
                "Absolute unique paths require an active root-name segment.");
            Exception? lookupFailure = null;
            Exception? mutationFailure = null;
            Task.Run(() =>
            {
                try { attachedRoot.GetNodeOrNull("%Unique"); }
                catch (Exception error) { lookupFailure = error; }
                try { attachedRoot.UniqueNameInOwner = true; }
                catch (Exception error) { mutationFailure = error; }
            }).GetAwaiter().GetResult();
            Check(lookupFailure is InvalidOperationException && mutationFailure is InvalidOperationException,
                "Attached unique lookup and mutation require the scene owner thread.");
            attachedUnique.Renamed += _ => throw new ApplicationException("observer failed");
            Reject<AggregateException>(() => attachedUnique.Name = "After");
            Check(attachedRoot.GetNode("%After") == attachedUnique && attachedRoot.GetNodeOrNull("%Before") is null,
                "A failing rename callback observes committed unique lookup without retaining the old name.");
        }

        Console.WriteLine("Node owner-scoped unique paths and packed state checks passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
