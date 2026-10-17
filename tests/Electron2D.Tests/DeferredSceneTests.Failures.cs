using Electron2D;
using static TextureArrayTests;

internal static partial class DeferredSceneTests
{
    private static void RunFailures()
    {
        using var root = new Node { Name = "Root" };
        var pending = new InstancePlaceholder { Name = "Pending" }; root.AddChild(pending);
        using var tree = new SceneTree(root); using var empty = new PackedScene();
        Reject<InvalidOperationException>(() => pending.CreateInstance(replace: true, customScene: empty));
        Check(pending.Parent == root && root.GetChildCount() == 1 && !pending.IsQueuedForDeletion, "Empty template failure preserves the original hierarchy.");
        Task.Run(() => Reject<InvalidOperationException>(() => pending.CreateInstance(customScene: empty))).GetAwaiter().GetResult();
        Task.Run(() => Reject<InvalidOperationException>(() => pending.GetStoredPropertyNames())).GetAwaiter().GetResult();
        Reject<KeyNotFoundException>(() => pending.GetStoredValue<int>("absent"));
        Reject<ArgumentException>(() => pending.GetStoredValue<int>(" "));
        pending.Dispose(); Reject<ObjectDisposedException>(() => pending.CreateInstance(customScene: empty));
        var live = new InstancePlaceholder { Name = "Live" }; root.AddChild(live);
        using var actor = new Actor { Name = "Actor" }; using var template = new PackedScene(); template.Pack(actor);
        _creating = () => Reject<InvalidOperationException>(() => live.CreateInstance(customScene: template));
        try { using var created = live.CreateInstance(customScene: template)!; }
        finally { _creating = null; }
        _creating = () => throw new ArgumentException("Injected factory failure.");
        try { Reject<ArgumentException>(() => live.CreateInstance(replace: true, customScene: template)); }
        finally { _creating = null; }
        Check(live.Parent == root && !live.IsQueuedForDeletion && root.GetChildCount() == 1, "Factory failure and reentry leave no published partial instance.");
        live.InitializePending("", [MissingResourceData.Capture(nameof(Entity.Position), new Vector2(3, 4))]);
        using var incompatible = new PackedScene(); using var plain = new Node(); incompatible.Pack(plain);
        Reject<InvalidOperationException>(() => live.CreateInstance(replace: true, customScene: incompatible));
        Check(live.Parent == root && !live.IsQueuedForDeletion, "An incompatible custom root fails before replacement.");
        _ready = () =>
        {
            Reject<InvalidOperationException>(() => live.CreateInstance(customScene: template));
            throw new ArgumentException("Injected Ready failure.");
        };
        try { Reject<Exception>(() => live.CreateInstance(replace: true, customScene: template)); }
        finally { _ready = null; }
        Check(live.Parent is null && live.IsQueuedForDeletion && root.GetChildCount() == 1 && root.GetChild(0) is Actor,
            "Ready failure retains committed replacement and queues its removed placeholder.");
        tree.FlushDeferred(); Check(live.IsDisposed, "Failed lifecycle delivery still retires the replaced placeholder.");
        var front = new Node { Name = "Front" }; var back = new Node { Name = "Back" };
        root.AddChild(front, internalMode: Node.InternalMode.Front); root.AddChild(back, internalMode: Node.InternalMode.Back);
        var rollback = new InstancePlaceholder { Name = "Rollback" }; root.AddChild(rollback); rollback.Owner = root;
        var index = rollback.GetIndex();
        void DestroyPrepared(Node _) => _lastCreated!.Dispose();
        rollback.TreeExited += DestroyPrepared;
        try { Reject<Exception>(() => rollback.CreateInstance(replace: true, customScene: template)); }
        finally { rollback.TreeExited -= DestroyPrepared; _lastCreated = null; }
        Check(rollback.Parent == root && rollback.Owner == root && rollback.GetIndex() == index && !rollback.IsQueuedForDeletion &&
            root.GetChild(0, includeInternal: true) == front && root.GetChild(root.GetChildCount(includeInternal: true) - 1, includeInternal: true) == back,
            "Pre-attachment replacement failure restores owner and visible order around internal children.");
    }
}
