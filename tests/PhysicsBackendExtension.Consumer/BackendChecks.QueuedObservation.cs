using Electron2D;

public static partial class BackendChecks
{
    public static void RunQueuedObservation(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("DeletionObservation", static () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("DeletionObservation");
            Check(world.PhysicsBackendName == (backend?.ToString() ?? "DeletionObservation") && world.PhysicsFallbackReason is null, "Observation uses the selected world without fallback");
            ObserveDeletionFamily(world); ObserveDeletionQueues(world); ObserveDeletionFailures(world); ObserveDeletionRaces(world); ObserveTransferredDeletionClaim(world); ObserveDeletionFrames(world);
            DeletionOrder(world); DeletionBoundary(world); DeletionFamily(world); DeletionRaces(world); DeletionReferences(world); DeletionWarmFrames(world);
            Console.WriteLine($"Queued observation {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: inherited descriptors, explicit/cancellable/claimed requests, discard, races and full frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private sealed class ObservedDeletionObject : ElectronObject
    {
        internal bool PreDeleteQueued, CleanupQueued, Reject, Fail;
        internal int Cleanups;
        protected override void ValidateDisposal()
        {
            base.ValidateDisposal();
            if (Reject) throw new InvalidOperationException("Rejected disposal");
        }
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationPreDelete) return;
            PreDeleteQueued = IsQueuedForDeletion;
            if (Fail) throw new InvalidOperationException("Failed notification");
        }
        protected override void Dispose(bool disposing)
        {
            CleanupQueued = IsQueuedForDeletion; Cleanups++; base.Dispose(disposing);
        }
    }

    private sealed class ObservedDeletionNode : Node
    {
        internal bool Reject, ExitQueued;
        protected override void ValidateDisposal()
        {
            base.ValidateDisposal();
            if (Reject) throw new InvalidOperationException("Rejected node disposal");
        }
        protected override void OnExitTree() { ExitQueued = IsQueuedForDeletion; base.OnExitTree(); }
    }

    private sealed class ObservedDeletionPhysicsBody : RigidBody
    {
        internal bool IntegrationQueued;
        protected override void IntegrateForces(PhysicsDirectBodyState state)
        {
            IntegrationQueued = IsQueuedForDeletion;
            base.IntegrateForces(state);
        }
    }

    private static void ObserveDeletionFamily(World world)
    {
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        ElectronObject[] family = [new ObservedDeletionObject(), new CircleShape(), new PhysicsMaterial(), new Node(), new Entity(), new Control(), new StaticBody(), new AnimatableBody(), new RigidBody(), new CharacterBody(), new PhysicalBone(), new Area(), new CollisionShape()];
        foreach (var value in family)
        {
            if (value is Node node) root.AddChild(node);
            var descriptor = (PropertyDescriptor<ElectronObject, bool>)value.GetPropertyList().Single(p => p.Name == nameof(ElectronObject.IsQueuedForDeletion));
            Check(descriptor.IsReadOnly && !descriptor.IsStored && !descriptor.TryGetRevertValue(value, out _) && !descriptor.GetValue(value), "Every role inherits one readonly nonstored observation without a revert value");
            tree.QueueDelete(value); tree.QueueDelete(value);
            Check(value.IsQueuedForDeletion && descriptor.GetValue(value), "Explicit duplicate requests publish on every engine-object role");
        }
        Task.Run(() => Check(family.All(o => o.IsQueuedForDeletion), "Foreign-thread observation reads local queued state for every role without backend access")).GetAwaiter().GetResult();
        tree.FlushDeferred(); Check(family.All(o => o.IsDisposed && !o.IsQueuedForDeletion), "Terminal disposal clears every inherited observation despite duplicate entries");
        var observed = (ObservedDeletionObject)family[0];
        Check(observed.PreDeleteQueued && observed.CleanupQueued && observed.Cleanups == 1, "Explicit deletion stays observable through pre-delete and cleanup, with at-most-once disposal");
        var direct = new ObservedDeletionObject(); direct.Dispose();
        Check(!direct.PreDeleteQueued && !direct.CleanupQueued && !direct.IsQueuedForDeletion, "Direct disposal without a queue never manufactures a request");
        var parent = new ObservedDeletionNode(); var child = new ObservedDeletionNode(); parent.AddChild(child); root.AddChild(parent);
        parent.QueueFree(); Check(parent.IsQueuedForDeletion && !child.IsQueuedForDeletion, "Only the requested parent is marked"); tree.FlushDeferred();
        Check(parent.ExitQueued && !child.ExitQueued && !parent.IsQueuedForDeletion, "A claimed request remains observable during departure without marking owned children");
        var both = new Node(); root.AddChild(both); both.QueueFree(); tree.QueueDelete(both);
        Check(both.CancelFree() && both.IsQueuedForDeletion && !both.CancelFree(), "Cancellation releases only the node request, leaving an independent explicit request visible");
        tree.FlushDeferred(); Check(both.IsDisposed && !both.IsQueuedForDeletion, "The independent request still deletes the node");
        using var circle = new CircleShape { Radius = 1 };
        var physical = new ObservedDeletionPhysicsBody { GravityScale = 0, CanSleep = false };
        physical.AddChild(new CollisionShape { Shape = circle }); root.AddChild(physical);
        tree.QueueDelete(physical); tree.PhysicsFrame(.01);
        Check(physical.IntegrationQueued && physical.IsDisposed && !physical.IsQueuedForDeletion, "Integration observes the pending explicit request before the full frame's deletion phase");
    }

    private static void ObserveDeletionQueues(World world)
    {
        using var root = new SubViewport { World = world }; using var otherRoot = new Node();
        using var tree = new SceneTree(root); var other = new SceneTree(otherRoot);
        var shared = new ObservedDeletionObject(); tree.QueueDelete(shared); other.QueueDelete(shared); other.Dispose();
        Check(!shared.IsDisposed && shared.IsQueuedForDeletion, "Discarding one tree releases only its own accepted request");
        tree.FlushDeferred(); Check(shared.IsDisposed && shared.Cleanups == 1 && !shared.IsQueuedForDeletion, "The other tree retains its independent request");
        var discarded = new ObservedDeletionObject();
        using (var discardRoot = new Node())
        using (var discardTree = new SceneTree(discardRoot)) { discardTree.QueueDelete(discarded); discardTree.QueueDelete(discarded); }
        Check(!discarded.IsDisposed && !discarded.IsQueuedForDeletion, "Finalization discards duplicate requests without disposing an unowned target or stranding its observation"); discarded.Dispose();
        var first = new ObservedDeletionObject(); var later = new ObservedDeletionObject(); first.Disposed += _ => tree.QueueDelete(later);
        tree.QueueDelete(first); tree.FlushDeferred();
        Check(first.IsDisposed && later.IsQueuedForDeletion && !later.IsDisposed, "A callback-published request waits for the next captured deletion batch");
        tree.FlushDeferred(); Check(later.IsDisposed && !later.IsQueuedForDeletion, "The next captured batch releases its request");
        var moved = new Node(); using var destinationRoot = new Node(); using var destination = new SceneTree(destinationRoot);
        root.AddChild(moved); tree.QueueDelete(moved); root.RemoveChild(moved); destinationRoot.AddChild(moved);
        var rejected = ObserveRejectedDeletion(tree);
        Check(rejected && !moved.IsDisposed && !moved.IsQueuedForDeletion, "An explicit request rejected after owner transfer releases its observation");
        var pending = new ObservedDeletionObject(); tree.QueueDelete(pending); pending.Dispose(); tree.FlushDeferred();
        Check(pending.IsDisposed && pending.Cleanups == 1 && !pending.IsQueuedForDeletion, "Direct disposal wins safely over an accepted pending entry");
    }

    private static void ObserveDeletionFailures(World world)
    {
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        var rejected = new ObservedDeletionObject { Reject = true }; tree.QueueDelete(rejected);
        var failed = ObserveRejectedDeletion(tree);
        Check(failed && !rejected.IsDisposed && !rejected.IsQueuedForDeletion, "Validation failure releases the executed request and leaves the target live");
        rejected.Reject = false; tree.QueueDelete(rejected); tree.FlushDeferred(); Check(rejected.IsDisposed && rejected.PreDeleteQueued, "A rejected target can accept and execute a later request");
        var rejectedNode = new ObservedDeletionNode { Reject = true }; root.AddChild(rejectedNode); rejectedNode.QueueFree();
        failed = ObserveRejectedDeletion(tree);
        Check(failed && rejectedNode.ExitQueued && !rejectedNode.IsDisposed && !rejectedNode.IsQueuedForDeletion, "Failed claimed node deletion releases the consumed claim");
        rejectedNode.Reject = false; root.AddChild(rejectedNode); rejectedNode.QueueFree(); tree.FlushDeferred();
        Check(rejectedNode.IsDisposed && !rejectedNode.IsQueuedForDeletion, "The released claim permits a fresh generation after validation failure");
        var failing = new ObservedDeletionObject { Fail = true }; var survivor = new ObservedDeletionObject();
        tree.QueueDelete(failing); tree.QueueDelete(survivor); failed = ObserveRejectedDeletion(tree);
        Check(failed && failing.PreDeleteQueued && failing.CleanupQueued && failing.IsDisposed && survivor.IsDisposed && !failing.IsQueuedForDeletion, "Callback failure retains observation during cleanup and does not skip a sibling or strand a request");
        var closed = new SceneTree(new Node()); closed.Dispose(); var untouched = new ObservedDeletionObject();
        failed = false; try { closed.QueueDelete(untouched); } catch (ObjectDisposedException) { failed = true; }
        Check(failed && !untouched.IsQueuedForDeletion, "A closed tree cannot publish a pending mark"); untouched.Dispose();
    }

    private static bool ObserveRejectedDeletion(SceneTree tree)
    {
        try { tree.FlushDeferred(); return false; }
        catch (AggregateException error) when (error.Flatten().InnerExceptions.All(e => e is InvalidOperationException)) { return true; }
    }

    private static void ObserveDeletionRaces(World world)
    {
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        for (var iteration = 0; iteration < 64; iteration++)
        {
            var target = new ObservedDeletionObject();
            var producer = Task.Run(() =>
            {
                for (var i = 0; i < 64; i++)
                {
                    try { tree.QueueDelete(target); }
                    catch (ObjectDisposedException) { break; }
                }
            });
            while (!producer.IsCompleted) { tree.FlushDeferred(); Thread.Yield(); }
            producer.GetAwaiter().GetResult(); tree.FlushDeferred(); if (!target.IsDisposed) target.Dispose();
            Check(target.Cleanups == 1 && !target.IsQueuedForDeletion, "Concurrent explicit publication and disposal cannot strand a mark or dispose twice");
        }
    }

    private static void ObserveTransferredDeletionClaim(World world)
    {
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        var node = new Node(); root.AddChild(node); node.QueueFree(); root.RemoveChild(node);
        using var claimed = new ManualResetEventSlim(); using var resume = new ManualResetEventSlim();
        var worker = Task.Run(() =>
        {
            using var otherWorld = new World(PhysicsServer.Backend.CPU);
            using var otherRoot = new SubViewport { World = otherWorld }; using var other = new SceneTree(otherRoot);
            otherRoot.AddChild(node);
            node.TreeExiting += _ => { claimed.Set(); if (!resume.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException("Transferred claim release"); };
            other.FlushDeferred();
        });
        try
        {
            Check(claimed.Wait(TimeSpan.FromSeconds(20)), "The destination owner claims the transferred generation");
            Check(node.IsQueuedForDeletion, "The destination claim is visible before stale work runs");
            tree.FlushDeferred();
            Check(node.IsQueuedForDeletion, "A stale owner that did not claim this generation cannot release the destination's active claim");
        }
        finally { resume.Set(); worker.GetAwaiter().GetResult(); }
        Check(node.IsDisposed && !node.IsQueuedForDeletion, "Only the destination finishes the transferred claim");
    }

    private static void ObserveDeletionFrames(World world)
    {
        using var circle = new CircleShape { Radius = 1 };
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        var body = CreateDeletionBody(circle, 0); root.AddChild(body);
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        var prepared = Enumerable.Range(0, 256).Select(_ => new ObservedDeletionObject()).ToArray();
        for (var i = 0; i < 128; i++) Frame(prepared[i]);
        Collect(); var ticks = body.Ticks; var integrations = body.Integrations;
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 128; i < 256; i++) Frame(prepared[i]);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0 && body.Ticks - ticks == 128 && body.Integrations - integrations == 128, $"Prepared explicit deletion plus simulation/query in 128 full frames: {ownerBytes}/{allBytes} B");
        Console.WriteLine($"Queued observation warm frames {world.PhysicsBackendName}: 128 full frames, owner/all-thread={ownerBytes}/{allBytes} B.");
        void Frame(ObservedDeletionObject value)
        {
            tree.QueueDelete(value); Check(value.IsQueuedForDeletion && !body.IsQueuedForDeletion, "A generic request is visible before the safe point without marking the active body");
            var x = body.Position.X; tree.PhysicsFrame(.01);
            Check(value.IsDisposed && value.PreDeleteQueued && value.CleanupQueued && !value.IsQueuedForDeletion && body.Position.X > x, "The generic request retires while the live body integrates");
            point.Position = body.GlobalPosition;
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && ReferenceEquals(hits[0].Collider, body), "Public point queries retain the active collider across explicit deletion");
        }
    }
}
