using Electron2D;

internal static class TreeChangeChecks
{
    internal static void Run()
    {
        using var root = new Node { Name = "Root" }; using var tree = new SceneTree(root);
        var branch = new Node { Name = "Branch" }; var leaf = new Node { Name = "Leaf" }; branch.AddChild(leaf); root.AddChild(branch);
        var order = new List<string>(); Node? removed = null;
        tree.NodeRemoved += (t, n) => { removed = n; order.Add("removed:" + n.Name); };
        tree.TreeChanged += t => { order.Add("changed:" + t.NodeCount + ":" + (removed?.Tree == t)); };
        root.ChildRemoved += (_, _) => order.Add("structural");
        root.RemoveChild(branch);
        Check(string.Join(",", order) == "removed:Leaf,changed:2:True,removed:Branch,changed:1:True,structural", "exit change ordering: " + string.Join(",", order));
        branch.Dispose();
        PathsAndReentry();
        Console.WriteLine("SceneTree change checks passed.");
    }
    private static void PathsAndReentry()
    {
        using var root = new Node { Name = "Root" }; using var tree = new SceneTree(root);
        var left = new Node { Name = "Left" }; var right = new Node { Name = "Right" }; var item = new Node { Name = "Item" };
        root.AddChild(left); root.AddChild(right); left.AddChild(item);
        var branchAPI = new SceneMultiplayer(); tree.SetMultiplayer(branchAPI, "/Root/Left");
        var fallback = tree.GetMultiplayer(); Check(item.Multiplayer == branchAPI && item.GetPath() == "/Root/Left/Item", "prime path consumers");
        var changes = 0; tree.TreeChanged += _ => changes++;
        tree.SetBlockSignals(true); item.Reparent(right);
        Check(changes == 0 && item.Multiplayer == fallback && item.GetPath() == "/Root/Right/Item" && root.GetNode("Right/Item") == item, "blocked signals preserve path invalidation");
        tree.SetBlockSignals(false);
        var order = new List<string>(); item.Renamed += _ => order.Add("node"); tree.NodeRenamed += (_, _) => order.Add("tree-node"); tree.TreeChanged += _ => order.Add("changed");
        item.Name = "NewItem";
        Check(string.Join(",", order) == "node,tree-node,changed" && item.GetPath() == "/Root/Right/NewItem", "rename ordering and path");
        var nested = false; Action<SceneTree> reenter = _ => { if (!nested) { nested = true; root.Name = "NewRoot"; } };
        tree.TreeChanged += reenter; changes = 0; item.Name = "Again"; tree.TreeChanged -= reenter;
        Check(changes == 2 && item.GetPath() == "/NewRoot/Right/Again", "nested tree change delivery and paths");
        order.Clear(); root.ChildOrderChanged += _ => order.Add("order");
        Action<SceneTree> fail = _ => throw new InvalidOperationException("controlled change failure"); tree.TreeChanged += fail;
        try { root.MoveChild(right, 0); throw new Exception("missing change failure"); } catch (AggregateException) { }
        tree.TreeChanged -= fail;
        Check(order.SequenceEqual(new[] { "changed", "order" }) && root.GetChild(0) == right, "move publishes change before child order and continues after failure");
        tree.SetMultiplayer(null, "/Root/Left"); branchAPI.Dispose();
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
