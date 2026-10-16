using Electron2D;

public static partial class BackendChecks
{
    public static void RunNodeMembership(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("Membership", static () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("Membership");
            using var shape = new CircleShape { Radius = 1 };
            var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            var left = new Entity { Name = "Left", Position = new(10, 4), Rotation = .3f };
            var right = new Entity { Name = "Right", Position = new(-7, 3), Rotation = -.2f };
            root.AddChild(left); root.AddChild(right);
            var defects = new List<string>();
            foreach (var node in new Node[] { new Node(), new Entity { Position = new(2, 3) }, new Control { Position = new(3, 4) }, new RigidBody { GravityScale = 0 } })
            {
                left.AddChild(node); node.Owner = root;
                if (node is RigidBody body) body.AddChild(new CollisionShape { Shape = shape });
                var global = node is CanvasItem item ? item.GetGlobalTransform() : Transform.Identity;
                var failure = new ApplicationException("Expected old-parent removal callback failure");
                Action<Node, Node> removed = (_, child) => { if (ReferenceEquals(child, node)) throw failure; };
                left.ChildRemoved += removed;
                var error = CaptureCopy(() => node.Reparent(right));
                left.ChildRemoved -= removed;
                var placed = ReferenceEquals(node.Parent, right) && ReferenceEquals(node.Tree, tree) && ReferenceEquals(node.Owner, root);
                var pose = node is Control control ? (control.GlobalPosition - global.Origin).Length() < .001f : node is CanvasItem canvas ? canvas.GetGlobalTransform().IsEqualApprox(global) : true;
                if (error is not AggregateException aggregate || !aggregate.Flatten().InnerExceptions.Contains(failure) || !placed || !pose)
                    defects.Add(node.GetType().Name + ": removal errors must preserve reparent completion, owner and placement");
                if (node is RigidBody physical && placed)
                    Check(PhysicsServer.BodyGetSpace(physical.GetRID()) == world.Space, "Failed callback cannot discard the selected physical binding");
                node.Dispose();
            }
            foreach (var node in new Node[] { new Node(), new Entity(), new Control() })
            {
                Exception? guard = null;
                node.TreeEntered += current => guard = CaptureCopy(() => current.Reparent(left));
                left.AddChild(node);
                if (guard is not InvalidOperationException) defects.Add(node.GetType().Name + ": same-parent reparent must preserve lifecycle guards");
                node.Dispose();
            }
            var busy = new LifecycleNode { Name = "Busy" }; root.AddChild(busy);
            var mover = new Node { Name = "BlockedMover" }; left.AddChild(mover); mover.Owner = root;
            busy.Lifecycle = (_, what) =>
            {
                if (what != 4543) return;
                var error = CaptureCopy(() => mover.Reparent(busy));
                if (error is not InvalidOperationException || !ReferenceEquals(mover.Parent, left) || !ReferenceEquals(mover.Owner, root))
                    defects.Add("Destination propagation must reject before removing the old hierarchy");
            };
            busy.PropagateNotification(4543); busy.Lifecycle = null; mover.Dispose(); busy.Dispose();
            var unowned = new Node(); var ownedChild = new Node(); unowned.AddChild(ownedChild); left.AddChild(unowned); ownedChild.Owner = root;
            unowned.Reparent(right);
            if (ownedChild.Owner is not null) defects.Add("An unowned reparent root does not preserve outside descendant ownership");
            unowned.Dispose();
            var branch = new Node(); var ordinary = new Node(); var internalChild = new Node();
            branch.AddChild(ordinary); branch.AddChild(internalChild, internalMode: Node.InternalMode.Front); left.AddChild(branch);
            branch.Owner = ordinary.Owner = internalChild.Owner = root;
            branch.Reparent(right);
            if (!ReferenceEquals(branch.Owner, root) || !ReferenceEquals(ordinary.Owner, root) || internalChild.Owner is not null)
                defects.Add("Reparent restores its shared outside owner only through ordinary descendants");
            branch.Dispose();
            MembershipDetachedOwnership();
            MembershipOperations(root, tree, left, right, world, shape, backend);
            MembershipWarmFrames(root, tree, left, world, shape);
            MembershipArchive(backend, threaded);
            foreach (var defect in defects) Console.Error.WriteLine("MEMBERSHIP RED: " + defect);
            Check(defects.Count == 0, "Node membership/reparent regression failures: " + defects.Count);
            Console.WriteLine($"Node membership {backend?.ToString() ?? "registered"}, threaded={threaded}: ownership, failure completion, preflight, snapshots and fresh scene; 128 moving full frames 0/0 B.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }
    private static void MembershipDetachedOwnership()
    {
        var owner = new Node(); var child = new Node { Name = "Borrowed", UniqueNameInOwner = true };
        owner.AddChild(child); child.Owner = owner;
        owner.RemoveChild(child);
        Check(child.Parent is null && child.Owner == owner && owner.GetNode("%Borrowed") == child, "Detached removal retains authoring owner and its unique-name registration");
        using var other = new Node(); other.AddChild(child);
        Check(child.Owner == owner && owner.GetNode("%Borrowed") == child, "Detached insertion does not silently retarget a borrowed owner");
        owner.Dispose(); Check(child.Owner is null && !child.IsDisposed && child.Parent == other && child.UniqueNameInOwner, "Owner disposal clears borrowed metadata without disposing a separately parented node");
        var survivor = new Node(); var weak = RetiredMembershipOwner(survivor); Collect();
        Check(!weak.TryGetTarget(out _) && survivor.Owner is null && !survivor.IsDisposed, "A detached authoring owner is borrowed and does not survive solely through Owner metadata"); survivor.Dispose();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference<Node> RetiredMembershipOwner(Node survivor)
    {
        var owner = new Node(); owner.AddChild(survivor); survivor.Owner = owner; owner.RemoveChild(survivor);
        Check(survivor.Owner == owner, "Detached owner remains observable while externally alive");
        return new WeakReference<Node>(owner);
    }

    private sealed class MembershipCaptureNode : Node
    {
        internal Action? DuringCapture;
        protected override Func<Node> CreateSceneInstanceFactory()
        { DuringCapture?.Invoke(); return Create; }
        private static Node Create() => new MembershipCaptureNode();
    }

    private static void MembershipOperations(SubViewport root, SceneTree tree, Entity left, Entity right, World world, Shape shape, PhysicsServer.Backend? backend)
    {
        using var outside = new Node(); var child = new Node(); outside.AddChild(child);
        Check(outside.Parent is null && child.Parent == outside && !outside.IsInsideTree && !child.IsInsideTree && child.Tree is null && outside.IsAncestorOf(child) && !outside.IsAncestorOf(outside), "Detached membership is structural and ancestry is strict");
        Reject<ArgumentNullException>(() => outside.IsAncestorOf(null!));
        var inherited = new Node(); child.AddChild(inherited); child.Owner = outside; inherited.Owner = child;
        Check(outside.RemoveChild(child) && child.Parent is null && child.Owner == outside && inherited.Owner == child && !outside.RemoveChild(child), "Removal reports direct membership, retains detached authoring ownership and local owners without disposal");
        Check(!child.IsDisposed && !inherited.IsDisposed, "Removal keeps the entire hierarchy alive");
        child.Dispose(); Check(child.Parent is null && child.Tree is null && !child.IsInsideTree && !outside.IsAncestorOf(child), "Stored membership remains empty after disposal");
        Reject<ObjectDisposedException>(() => _ = child.Owner); Reject<ObjectDisposedException>(() => child.IsAncestorOf(outside));
        foreach (var node in new Node[] { new Node(), new Entity { Position = new(4, 2), Rotation = .4f, Scale = new(2, 1) }, new Control { Position = new(5, 7), Rotation = .2f }, new RigidBody { GravityScale = 0 } })
        {
            left.AddChild(node); node.Owner = root;
            var local = node is CanvasItem canvas ? canvas.GetTransform() : Transform.Identity;
            node.Reparent(right, false);
            Check(node.Parent == right && node.Tree == tree && node.Owner == root && (node is not CanvasItem moved || moved.GetTransform().IsEqualApprox(local)), "Explicit false keeps local placement through the virtual Node family");
            var changes = 0; Action<Node> changed = _ => changes++; right.ChildOrderChanged += changed;
            node.Reparent(right); right.ChildOrderChanged -= changed;
            Check(changes == 0 && node.Parent == right && node.Owner == root, "Same-parent reparent is a validated no-op");
            Reject<ArgumentException>(() => node.Reparent(node)); Reject<InvalidOperationException>(() => right.Reparent(node));
            using var disposed = new Node(); disposed.Dispose(); Reject<ObjectDisposedException>(() => node.Reparent(disposed));
            Check(node.Parent == right && node.Owner == root, "Invalid destinations preserve hierarchy and owner");
            Reject<ArgumentException>(() => node.Owner = node); Reject<ArgumentException>(() => node.Owner = left);
            Check(node.Owner == root, "Typed invalid-owner assignments preserve the prior owner");
            var p = node.Parent; var t = node.Tree; var o = node.Owner; Exception? foreign = null;
            var observer = new Thread(() =>
            {
                try { Check(node.Parent == p && node.Tree == t && node.IsInsideTree && node.Owner == o && root.IsAncestorOf(node), "Coordinated foreign membership reads observe the same identities"); Reject<InvalidOperationException>(() => node.Owner = o); Reject<InvalidOperationException>(() => node.Reparent(p!)); }
                catch (Exception error) { foreign = error; }
            }); observer.Start(); observer.Join(); Check(foreign is null, "Foreign observation preserves mutation guards: " + foreign);
            node.Dispose();
        }
        var notify = new LifecycleNode(); var order = new List<int>();
        notify.Lifecycle = (_, what) => { if (what is Node.NotificationChildOrderChanged or Node.NotificationChildOrderChanged + 1000) order.Add(what); };
        notify.ChildOrderChanged += _ => order.Add(2000);
        var first = new Node(); var second = new Node();
        notify.AddChild(first); CheckOrder(); notify.AddChild(second); CheckOrder(); notify.MoveChild(first, 1); CheckOrder();
        notify.MoveChild(first, 1); Check(order.Count == 0, "Unchanged child order is silent");
        Check(notify.RemoveChild(first), "Direct removal succeeds"); CheckOrder(); Check(!notify.RemoveChild(first) && order.Count == 0, "Absent removal is silent");
        root.AddChild(notify); notify.AddChild(first); CheckOrder(); notify.RemoveChild(first); CheckOrder(); first.Dispose(); notify.Dispose();
        var faultedOrder = new LifecycleNode(); root.AddChild(faultedOrder); var inserted = new Node();
        var notificationFailure = new ApplicationException("child order notification"); var eventFailure = new ApplicationException("child order signal"); var signaled = false;
        faultedOrder.Lifecycle = (_, what) => { if (what == Node.NotificationChildOrderChanged) throw notificationFailure; };
        Action<Node> signal = _ => { signaled = true; throw eventFailure; }; faultedOrder.ChildOrderChanged += signal;
        var orderFailure = CaptureCopy(() => faultedOrder.AddChild(inserted));
        faultedOrder.Lifecycle = null; faultedOrder.ChildOrderChanged -= signal;
        Check(signaled && orderFailure is AggregateException orderErrors && orderErrors.Flatten().InnerExceptions.Contains(notificationFailure) && orderErrors.Flatten().InnerExceptions.Contains(eventFailure) && inserted.Parent == faultedOrder && inserted.Tree == tree,
            "Child order signal still runs after a numeric notification failure, preserving both original errors and committed structure"); faultedOrder.Dispose();
        var capture = new MembershipCaptureNode(); var captured = new Node(); capture.AddChild(captured); captured.Owner = capture; left.AddChild(capture);
        capture.DuringCapture = () =>
        {
            Reject<InvalidOperationException>(() => captured.Owner = capture);
            Reject<InvalidOperationException>(() => capture.RemoveChild(captured));
            Reject<InvalidOperationException>(() => captured.Reparent(capture));
            Check(captured.Parent == capture && captured.Owner == capture && captured.Tree == tree, "Capture rejects hierarchy and owner mutation before changing state");
        };
        using (var packed = new PackedScene()) { packed.Pack(capture); using var copy = packed.Instantiate(); Check(copy.Tree is null && copy.GetChild(0).Owner == copy, "Packed instances reconstruct structural ownership independently of live tree membership"); }
        capture.DuringCapture = null; capture.Dispose();
        var pruned = new Node { Name = "PrunedOwned", UniqueNameInOwner = true }; var prunedChild = new Node(); pruned.AddChild(prunedChild); left.AddChild(pruned); pruned.Owner = prunedChild.Owner = root;
        var structuralOwner = false; var exitedOwner = false;
        Action<Node, Node> inspectRemoval = (_, node) =>
        {
            if (node != pruned) return;
            Check(pruned.Parent is null && pruned.Tree is null && pruned.Owner == root && prunedChild.Owner == root && root.GetNode("%PrunedOwned") == pruned, "Structural removal callbacks retain the departing authoring owner until final exit cleanup"); structuralOwner = true;
        };
        pruned.TreeExited += _ => { Check(pruned.Owner is null && prunedChild.Owner is null && root.GetNodeOrNull("%PrunedOwned") is null, "TreeExited observes completed unreachable-owner and unique registration cleanup"); exitedOwner = true; };
        left.ChildRemoved += inspectRemoval; left.RemoveChild(pruned); left.ChildRemoved -= inspectRemoval;
        Check(structuralOwner && exitedOwner, "Both owner lifetime boundaries execute"); pruned.Dispose();
        var failedBranch = new Node(); var failed = new Entity { Position = new(2, 1) }; failedBranch.AddChild(failed); left.AddChild(failedBranch); failedBranch.Owner = failed.Owner = root;
        var failures = new[] { new ApplicationException("exiting"), new ApplicationException("exited"), new ApplicationException("removed"), new ApplicationException("added") };
        Action<Node> exiting = _ => throw failures[0], exited = _ => throw failures[1];
        failedBranch.TreeExiting += exiting; failedBranch.TreeExited += exited;
        Action<Node, Node> removed = (_, node) => { if (node == failedBranch) throw failures[2]; }; left.ChildRemoved += removed;
        Action<Node, Node> added = (_, node) => { if (node == failedBranch) throw failures[3]; }; right.ChildAdded += added;
        var result = CaptureCopy(() => failedBranch.Reparent(right)); left.ChildRemoved -= removed; right.ChildAdded -= added;
        Check(result is AggregateException errors && failures.All(item => errors.Flatten().InnerExceptions.Contains(item)) && failedBranch.Parent == right && failedBranch.Owner == root && failed.Owner == root, "Failures from both halves retain original error identities after owner restoration");
        failedBranch.TreeExiting -= exiting; failedBranch.TreeExited -= exited; failedBranch.Dispose();
        var unavailable = new Node(); root.AddChild(unavailable); var survivor = new Node(); left.AddChild(survivor);
        Action<Node, Node> retire = (_, node) => { if (node == survivor) unavailable.Dispose(); }; left.ChildRemoved += retire;
        var lateError = CaptureCopy(() => survivor.Reparent(unavailable)); left.ChildRemoved -= retire;
        Check(lateError is AggregateException late && late.Flatten().InnerExceptions.Any(item => item is ObjectDisposedException) && survivor.Parent is null && survivor.Tree is null && !survivor.IsDisposed, "A callback-retired destination is not resurrected and leaves a usable detached receiver with an explicit error"); survivor.Dispose();
        using var secondWorld = backend is { } selected ? new World(selected) : new World("Membership");
        var secondRoot = new SubViewport { World = secondWorld }; using var secondTree = new SceneTree(secondRoot);
        var physical = new RigidBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
        physical.AddChild(new CollisionShape { Shape = shape }); left.AddChild(physical); physical.Owner = root;
        var rid = physical.GetRID(); var pose = physical.GlobalTransform;
        physical.Reparent(secondRoot);
        Check(physical.Parent == secondRoot && physical.Tree == secondTree && physical.Owner is null && physical.GetRID() == rid && physical.GetWorld() == secondWorld && PhysicsServer.BodyGetSpace(rid) == secondWorld.Space && physical.GlobalTransform.IsEqualApprox(pose), "Cross-tree physical reparent transfers selected space while retaining RID/global pose and clearing unreachable owner");
        physical.LinearVelocity = Vector2.Right; var initial = physical.GlobalPosition.X; secondTree.PhysicsFrame(.1);
        Check(Math.Abs(physical.GlobalPosition.X - initial - .1f) < .001f, "Transferred body actually solves in the receiving space");
        physical.Reparent(left); Check(physical.GetRID() == rid && PhysicsServer.BodyGetSpace(rid) == world.Space && physical.Tree == tree, "Cross-tree reentry restores the original selected physical space"); physical.Dispose();

        void CheckOrder()
        { Check(order.SequenceEqual(new[] { Node.NotificationChildOrderChanged, Node.NotificationChildOrderChanged + 1000, 2000 }), "ChildOrderChanged follows complete numeric delivery once per actual child mutation"); order.Clear(); }
    }

    private static void MembershipWarmFrames(SubViewport root, SceneTree tree, Entity parent, World world, Shape shape)
    {
        var body = new RigidBody { Name = "MembershipMover", GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace, LinearVelocity = Vector2.Right };
        body.AddChild(new CollisionShape { Shape = shape }); parent.AddChild(body); body.Owner = root;
        var sleeper = new RigidBody { GravityScale = 0, Sleeping = true }; parent.AddChild(sleeper); sleeper.Owner = root;
        var worker = new LifecycleWorkerBody { GravityScale = 0, Sleeping = true, ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread }; parent.AddChild(worker); worker.Owner = root;
        var workerCalls = 0;
        worker.Tick = () =>
        {
            Check(worker.Parent == parent && worker.Tree == tree && worker.IsInsideTree && worker.Owner == root && root.IsAncestorOf(worker), "Worker membership reads preserve borrowed object identities across processing groups");
            if (workerCalls++ == 0) { Reject<InvalidOperationException>(() => worker.Owner = root); Reject<InvalidOperationException>(() => worker.Reparent(parent)); }
        };
        var callbacks = 0; PhysicsServer.BodySetForceIntegrationCallback(body.GetRID(), _ => callbacks++);
        for (var i = 0; i < 128; i++) Frame();
        var start = body.GlobalPosition.X; var before = callbacks; var rid = body.GetRID(); Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0 && Math.Abs(body.GlobalPosition.X - start - 1.28f) < .002f && callbacks == before + 128 && body.GetRID() == rid && workerCalls == 256 && sleeper.Sleeping && worker.Sleeping,
            $"Membership warmed real frames, callbacks, RID, sleeping and worker observations: {owner}/{all} B; motion={body.GlobalPosition.X - start}, callbacks={callbacks - before}");
        worker.Tick = null; worker.Dispose(); sleeper.Dispose(); body.Dispose();
        void Frame()
        {
            Check(body.Parent == parent && body.Tree == tree && body.IsInsideTree && body.Owner == root && root.IsAncestorOf(body) && body.GetWorld() == world, "Prepared membership queries retain physical ownership");
            body.Owner = parent; Check(body.Owner == parent && body.GetWorld() == world, "Changed authoring owner does not retarget physical ownership"); body.Owner = root;
            body.Reparent(parent); sleeper.Reparent(parent); sleeper.Owner = root;
            tree.PhysicsFrame(.01);
        }
    }

    private static void MembershipArchive(PhysicsServer.Backend? backend, bool threaded)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-membership-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            using var root = new Node { Name = "Saved" }; var left = new Entity { Name = "Left", Position = new(8, 2) }; var right = new Entity { Name = "Right", Position = new(-4, 6) };
            root.AddChild(left); root.AddChild(right); left.Owner = right.Owner = root;
            using var disk = new CircleShape { Radius = 1 };
            var body = new RigidBody { Name = "Body", GravityScale = 0, LinearVelocity = new(6, 0), CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
            body.AddChild(new CollisionShape { Name = "Shape", Shape = disk }); left.AddChild(body); body.Owner = body.GetChild(0).Owner = root;
            body.Reparent(right); Check(body.Owner == root && body.GetChild(0).Owner == root, "Reparented ordinary descendants retain their shared reachable archive owner");
            using var packed = new PackedScene(); packed.Pack(root); var path = System.IO.Path.Combine(directory, "membership.e2dscene"); ResourceSaver.Save(packed, path);
            var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(System.IO.Path.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name + ".dll"));
            start.ArgumentList.Add("--node-membership-child"); start.ArgumentList.Add(path); if (threaded) start.ArgumentList.Add("--threaded"); if (backend is { } selected) start.ArgumentList.Add(selected == PhysicsServer.Backend.GPU ? "--gpu" : "--cpu");
            using var child = System.Diagnostics.Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); child.WaitForExit(); throw new TimeoutException("Fresh membership consumer"); }
            Check(child.ExitCode == 0 && output.Result.Contains("Fresh node membership passed"), "Fresh membership scene: " + output.Result + error.Result);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    public static void ReadNodeMembership(string path, PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("Membership", static () => new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("Membership");
        using var packed = ResourceLoader.Load<PackedScene>(path)!; var scene = packed.Instantiate();
        var body = scene.GetNode<RigidBody>("Right/Body");
        Check(body.Parent == scene.GetNode("Right") && body.Owner == scene && body.GetChild(0).Owner == scene && !body.IsInsideTree && body.Tree is null && scene.IsAncestorOf(body), "Fresh scene reconstructs identities, hierarchy and owner graph before activation");
        var root = new SubViewport { World = world }; root.AddChild(scene); using var tree = new SceneTree(root);
        Check(body.Tree == tree && body.IsInsideTree && body.GetWorld() == world && world.PhysicsBackendName == (backend?.ToString() ?? "Membership"), "Fresh scene joins the exact selected physical runtime");
        var rid = body.GetRID(); var start = body.GlobalPosition.X;
        for (var i = 0; i < 16; i++) tree.PhysicsFrame(1d / 60);
        Check(body.GetRID() == rid && Math.Abs(body.GlobalPosition.X - start - 1.6f) < .01f, "Fresh owner-preserved physical scene actually integrates");
        Console.WriteLine("Fresh node membership passed");
    }

}
