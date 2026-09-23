using Electron2D;

internal static class RemoteTransformTests
{
    internal static void Run()
    {
        TransferAndFlags();
        CacheLifetimeAndCycles();
        PackedStateAndFailure();
        Console.WriteLine("Remote transform checks passed.");
    }

    private static void TransferAndFlags()
    {
        var root = new Entity { Name = "root", Position = new(10, 0) };
        var sourceParent = new Entity { Name = "source", Position = new(5, 0) };
        var targetParent = new Entity { Name = "target-parent", Position = new(20, 0) };
        var remote = new RemoteTransform { Name = "remote", Position = new(2, 3), RemotePath = "../../target-parent/target" };
        var target = new Entity { Name = "target", Position = new(8, 8) };
        root.AddChild(sourceParent); root.AddChild(targetParent);
        sourceParent.AddChild(remote); targetParent.AddChild(target);
        using var tree = new SceneTree(root);
        tree.ProcessFrame(0);
        Check(target.GlobalTransform.IsEqualApprox(remote.GlobalTransform), "Initial queued global update.");
        Check(remote.NotifyTransformChanges && !remote.NotifyLocalTransformChanges, "Global notification policy.");

        remote.UseGlobalCoordinates = false;
        Check(target.Transform.IsEqualApprox(remote.Transform) && !remote.NotifyTransformChanges &&
              remote.NotifyLocalTransformChanges, "Local policy and immediate switch update.");
        remote.Position = new(4, 5);
        Check(target.Position == new Vector2(4, 5) && target.GlobalPosition != remote.GlobalPosition,
            "Local updates ignore distinct parent placements.");

        target.Position = new(40, 50);
        remote.UpdatePosition = false;
        remote.Rotation = 0.4f;
        Check(target.Position == new Vector2(40, 50) && Electron2D.Mathf.IsEqualApprox(target.Rotation, remote.Rotation),
            "Independent position and rotation switches.");
        remote.UpdateRotation = false;
        target.Skew = -0.3f;
        remote.Skew = 0.2f;
        Check(Electron2D.Mathf.IsEqualApprox(target.Skew, -0.3f), "Disabled rotation preserves the target skew basis.");
        remote.UpdateRotation = true;
        Check(Electron2D.Mathf.IsEqualApprox(target.Skew, 0.2f), "Enabling rotation copies skew independently of position.");
        remote.UpdateRotation = false;
        target.Scale = new(3, 4);
        remote.Scale = new(2, 5);
        Check(target.Position == new Vector2(40, 50) && target.Scale.IsEqualApprox(new Vector2(2, 5)),
            "Scale updates preserve disabled position and rotation.");

        remote.UpdateScale = false;
        var before = target.Transform;
        remote.Transform = new Transform(0.8f, new Vector2(7, 8), 0.2f, new Vector2(9, 10));
        Check(target.Transform == before, "All-disabled policy leaves the target unchanged.");
        remote.UpdatePosition = true;
        Check(target.Position == new Vector2(9, 10), "Re-enabling a component pushes immediately.");
        remote.UseGlobalCoordinates = true;
        tree.ProcessFrame(0);
        Check(target.GlobalPosition == remote.GlobalPosition, "Global mode applies position across different parents.");
    }

    private static void CacheLifetimeAndCycles()
    {
        var root = new Entity { Name = "root" };
        var remote = new RemoteTransform { Name = "remote", RemotePath = "../late" };
        root.AddChild(remote);
        using var tree = new SceneTree(root);
        var late = new Entity { Name = "late", Position = new(99, 99) };
        root.AddChild(late);
        remote.ForceUpdateCache();
        Check(late.Position == new Vector2(99, 99), "Cache refresh does not transfer state.");
        remote.Position = new(3, 4);
        tree.ProcessFrame(0);
        Check(late.Position == new Vector2(3, 4), "Refreshed target receives later changes.");
        root.RemoveChild(late);
        remote.Position = new(5, 6);
        tree.ProcessFrame(0);
        Check(late.Position == new Vector2(3, 4), "Detached target is not updated.");
        root.AddChild(late);
        remote.ForceUpdateCache();
        remote.RemotePath = "..";
        Check(root.Position == Vector2.Zero && remote.GetConfigurationWarnings().Length > 0,
            "Ancestor target is rejected with a warning.");
        remote.RemotePath = ".";
        Check(remote.GetConfigurationWarnings().Length > 0, "Self target is rejected.");
        var child = new Entity { Name = "child", Position = new(10, 11) };
        remote.AddChild(child);
        remote.RemotePath = "child";
        remote.Position = new(20, 21);
        tree.ProcessFrame(0);
        Check(child.Position == new Vector2(10, 11), "Descendant target is rejected.");

        var other = new RemoteTransform { Name = "other" };
        root.AddChild(other);
        remote.RemotePath = "../other";
        other.RemotePath = "../remote";
        remote.Position = new(12, 13);
        other.Position = new(14, 15);
        tree.ProcessFrame(0);
        Check(remote.Position == new Vector2(12, 13), "Reciprocal remote links do not loop.");
        late.Dispose();
    }

    private static void PackedStateAndFailure()
    {
        using var root = new Node { Name = "root" };
        var remote = new RemoteTransform
        {
            Name = "remote",
            RemotePath = "../target",
            UseGlobalCoordinates = false,
            UpdatePosition = false,
            UpdateRotation = false,
            UpdateScale = true,
        };
        var target = new Entity { Name = "target" };
        root.AddChild(remote); root.AddChild(target);
        remote.Owner = root; target.Owner = root;
        using var packed = new PackedScene();
        packed.Pack(root);
        using var copy = packed.Instantiate();
        var restored = copy.GetNode<RemoteTransform>("remote");
        Check(restored.RemotePath == remote.RemotePath && !restored.UseGlobalCoordinates &&
              !restored.UpdatePosition && !restored.UpdateRotation && restored.UpdateScale,
            "Packed scene restores the exact type and policy.");

        using var tree = new SceneTree(root);
        remote.UpdatePosition = true;
        remote.UpdateScale = false;
        target.NotifyLocalTransformChanges = true;
        var reentered = false;
        Action<CanvasItem> reenter = _ =>
        {
            if (reentered) return;
            reentered = true;
            remote.Position = new(10, 11);
        };
        target.LocalTransformChanged += reenter;
        remote.Position = new(6, 7);
        Check(target.Position == new Vector2(10, 11), "A newer callback mutation wins after reentry.");
        target.LocalTransformChanged -= reenter;
        target.LocalTransformChanged += _ => throw new InvalidOperationException("expected");
        Reject<InvalidOperationException>(() => remote.Position = new(8, 9));
        Check(target.Position == new Vector2(8, 9), "Callback failure occurs after target commit.");
        Reject<InvalidOperationException>(() => Task.Run(() => remote.RemotePath = "../target").GetAwaiter().GetResult());
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
