using Electron2D;

internal static class ScrollInternalNodeTests
{
    internal static void Run()
    {
        VerifyPartitionMoves();
        using var parent = new Node { Name = "Root" };
        var front = new Probe { Name = "Front", ProcessEnabled = true };
        var first = new Node { Name = "First" };
        var last = new Node { Name = "Last" };
        var back = new Probe { Name = "Back", ProcessEnabled = true };
        parent.AddChild(first);
        parent.AddChild(front, Node.InternalMode.Front);
        parent.AddChild(back, Node.InternalMode.Back);
        parent.AddChild(last);
        first.Owner = parent;
        last.Owner = parent;
        Check(parent.GetChildren().SequenceEqual([first, last]) && parent.Children.SequenceEqual([first, last]) &&
              parent.ChildCount == 2 && parent.GetChildCount() == 2 &&
              parent.GetChildren(true).SequenceEqual([front, first, last, back]) && parent.GetChildCount(true) == 4,
            "Public ordinary children exclude both internal partitions, while the full view preserves scene order.");
        Check(first.GetIndex() == 0 && last.GetIndex() == 1 && front.GetIndex(true) == 0 && back.GetIndex(true) == 3 &&
              ReferenceEquals(parent.GetChild(-1), last) && ReferenceEquals(parent.GetChild(-1, true), back),
            "Ordinary and complete indices resolve their own partitions, including negative indices.");
        Reject<InvalidOperationException>(() => back.GetIndex());
        Reject<ArgumentOutOfRangeException>(() => parent.MoveChild(back, 2));
        parent.MoveChild(last, 0);
        Check(parent.GetChildren().SequenceEqual([last, first]) && parent.GetChildren(true).SequenceEqual([front, last, first, back]),
            "Reordering an ordinary child cannot move it across an internal partition.");

        using var packed = new PackedScene();
        packed.Pack(parent);
        using var copy = packed.Instantiate();
        Check(copy.GetChildren().Select(node => node.Name).SequenceEqual(["Last", "First"]) && copy.GetChildCount(true) == 2,
            "Scene capture omits constructor-owned internal descendants.");
        parent.RemoveChild(back);
        using var other = new Node();
        other.AddChild(back);
        Check(other.GetChildCount() == 1 && back.GetIndex() == 0, "A removed internal child can be added as an ordinary child.");
        other.RemoveChild(back);
        parent.AddChild(back, Node.InternalMode.Back);
        using (var tree = new SceneTree(parent))
        {
            tree.ProcessFrame(0.016);
            Check(front.Frames == 1 && back.Frames == 1, "Internal descendants participate in scene processing.");
        }
        Console.WriteLine("Internal child partitioning, processing and scene capture checks passed.");
    }

    private static void VerifyPartitionMoves()
    {
        using var parent = new Node();
        var frontA = new Node { Name = "FrontA" };
        var frontB = new Node { Name = "FrontB" };
        var ordinary = new Node { Name = "Ordinary" };
        var backA = new Node { Name = "BackA" };
        var backB = new Node { Name = "BackB" };
        parent.AddChild(frontA, Node.InternalMode.Front);
        parent.AddChild(frontB, Node.InternalMode.Front);
        parent.AddChild(ordinary);
        parent.AddChild(backA, Node.InternalMode.Back);
        parent.AddChild(backB, Node.InternalMode.Back);
        parent.MoveChild(frontB, 0);
        parent.MoveChild(backB, 0);
        Check(parent.GetChildren(true).SequenceEqual([frontB, frontA, ordinary, backB, backA]),
            "Front and back reordering stays inside the corresponding internal partition.");
        frontB.AddSibling(new Node { Name = "FrontSibling" });
        Check(parent.GetChild(1, true).Name == "FrontSibling" && parent.GetChildCount() == 1,
            "Adding a sibling to an internal child retains its partition.");
        Reject<ArgumentOutOfRangeException>(() => parent.MoveChild(frontA, 3));
        Reject<ArgumentOutOfRangeException>(() => parent.MoveChild(backA, 2));
        using var destination = new Node();
        backA.Reparent(destination);
        Check(parent.GetChildCount(true) == 5 && destination.GetChildCount() == 1 && backA.GetIndex() == 0,
            "Reparenting an internal child makes it ordinary under the new parent.");
    }

    private sealed class Probe : Node
    {
        internal int Frames;
        protected override void OnProcess(double delta) => Frames++;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
