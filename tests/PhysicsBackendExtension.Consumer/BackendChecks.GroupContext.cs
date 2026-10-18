using Electron2D;

public static partial class BackendChecks
{
    private const int GroupContextNotification = 77131;
    private sealed class GroupContextProbe : Node
    {
        internal Action? Execute;
        internal Action? NestedTask;
        internal Action<int>? NestedGroup;
        internal Action<Node>? Call;
        internal Action<Node, int>? Set;
        internal Action? Fail;
        internal Action<int>? FailGroup;
        internal int WorkerThread, OwnerThread, Deliveries, Hash, NestedCalls;
        internal bool VerifyGuards, RejectMetadata;
        internal Exception? Failure;
        internal double ExpectedDelta;
        protected override void OnPhysicsProcess(double delta) { ExpectedDelta = delta; Execute!(); }
        protected override void ValidateMutation()
        { base.ValidateMutation(); if (RejectMetadata) throw new InvalidOperationException("Expected metadata veto"); }
        protected override void OnNotification(int what)
        { base.OnNotification(what); if (what == GroupContextNotification) Record(3); }
        internal void Record(int value)
        {
            Check(Environment.CurrentManagedThreadId == WorkerThread && PhysicsProcessDeltaTime == ExpectedDelta,
                "Thread-safe message delivery requires the actual scene group");
            Deliveries++; Hash = unchecked(Hash * 31 + value);
        }
        internal void Nested(bool group)
        {
            Check(Environment.CurrentManagedThreadId == WorkerThread && WorkerThread != OwnerThread,
                "One retained worker actually helps the nested pool callback");
            Check(group ? WorkerThreadPool.GetCallerGroupID() > 0 && WorkerThreadPool.GetCallerTaskID() == -1 :
                WorkerThreadPool.GetCallerTaskID() > 0 && WorkerThreadPool.GetCallerGroupID() == -1, "Nested callback keeps its own public pool identity");
            NestedCalls++;
            if (VerifyGuards)
            {
                Check(PhysicsProcessDeltaTime == ExpectedDelta && IsNodeReady, "Same-thread helper callbacks retain their active scene group");
                PhysicsProcessEnabled = true;
                ProcessThreadGroupOrder = -1; ProcessThreadMessages = Node.ProcessThreadMessageFlags.Physics;
            }
            var count = Deliveries;
            CallThreadSafe(Call!); SetThreadSafe(Set!, 2); NotifyThreadSafe(GroupContextNotification);
            CallDeferredThreadGroup(Call!); SetDeferredThreadGroup(Set!, 2); NotifyDeferredThreadGroup(GroupContextNotification);
            Check(Deliveries == count + 3, "Same-thread nested work delivers the three safe methods immediately and queues the three deferred methods");
        }
        internal void Outer()
        {
            WorkerThread = Environment.CurrentManagedThreadId;
            Check(WorkerThread != OwnerThread && WorkerThreadPool.GetCallerGroupID() > 0 && PhysicsProcessDeltaTime == ExpectedDelta,
                "The scene callback owns a real worker group invocation");
            var task = WorkerThreadPool.AddTask(NestedTask!); WorkerThreadPool.WaitForTaskCompletion(task); Restored();
            var group = WorkerThreadPool.AddGroupTask(NestedGroup!, 2, tasksNeeded: 1); WorkerThreadPool.WaitForGroupTaskCompletion(group); Restored();
            if (Failure is { } expected)
            {
                task = WorkerThreadPool.AddTask(Fail!);
                try { WorkerThreadPool.WaitForTaskCompletion(task); throw new InvalidOperationException("Expected nested pool failure"); }
                catch (ApplicationException caught) { Check(ReferenceEquals(expected, caught), "Original nested task error propagates"); }
                Restored();
                group = WorkerThreadPool.AddGroupTask(FailGroup!, 2, tasksNeeded: 1);
                try { WorkerThreadPool.WaitForGroupTaskCompletion(group); throw new InvalidOperationException("Expected nested group failure"); }
                catch (ApplicationException caught) { Check(ReferenceEquals(expected, caught), "Nested group drains every index before preserving its original error"); }
                Restored();
            }
        }
        private void Restored()
        {
            Check(PhysicsProcessDeltaTime == ExpectedDelta && IsNodeReady && WorkerThreadPool.GetCallerGroupID() > 0,
                "The enclosing group capability is restored after nested completion or failure");
            PhysicsProcessEnabled = PhysicsProcessEnabled;
            ProcessThreadGroupOrder = ProcessThreadGroupOrder == -2 ? -1 : -2;
            ProcessThreadMessages = ProcessThreadMessages == Node.ProcessThreadMessageFlags.Physics ? Node.ProcessThreadMessageFlags.Process : Node.ProcessThreadMessageFlags.Physics;
            var before = Deliveries; CallThreadSafe<Node>(static node => ((GroupContextProbe)node).Record(4));
            Check(Deliveries == before + 1, "A genuine current-group thread-safe call remains immediate after nested work");
        }
    }
    public static void RunGroupContext(PhysicsServer.Backend? backend, bool threaded)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 1); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GroupContext", static () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("GroupContext");
            using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 1 };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
            var probe = new GroupContextProbe { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1, OwnerThread = Environment.CurrentManagedThreadId };
            probe.Execute = probe.Outer; probe.NestedTask = () => probe.Nested(false); probe.NestedGroup = _ => probe.Nested(true);
            probe.Call = static node => ((GroupContextProbe)node).Record(1); probe.Set = static (node, value) => ((GroupContextProbe)node).Record(value);
            probe.Fail = () => { probe.Nested(false); throw probe.Failure!; }; root.AddChild(probe);
            probe.FailGroup = index => { probe.Nested(true); if (index == 0) throw probe.Failure!; };
            var gather = new GroupGather { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread, ProcessThreadGroupOrder = -1 };
            gather.Collect = () => { Check(probe.Deliveries == probe.NestedCalls * 6 + (probe.Failure is null ? 2 : 4), "Main collection follows nested completion and full post-group message delivery"); body.LinearVelocity = new(6, 0); };
            root.AddChild(gather); using var tree = new SceneTree(root);
            GroupPolicy(tree, probe, gather, body);
            probe.VerifyGuards = true; probe.Failure = new ApplicationException("nested callback failure"); tree.PhysicsFrame(.01);
            Check(probe.NestedCalls == 6 && probe.Deliveries == 40 && probe.Hash == -777769728, "Regular, indexed and both failed waits complete on the same worker without leaking access");
            var outside = WorkerThreadPool.AddTask(() =>
            {
                Reject<InvalidOperationException>(() => probe.ProcessThreadGroupOrder = -1);
                Reject<InvalidOperationException>(() => probe.ProcessThreadMessages = Node.ProcessThreadMessageFlags.Physics);
                Reject<InvalidOperationException>(() => _ = probe.PhysicsProcessDeltaTime);
            });
            WorkerThreadPool.WaitForTaskCompletion(outside);
            probe.Failure = null; probe.VerifyGuards = false;
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2]; var rid = body.GetRID(); var identity = body.InstanceID;
            for (var i = 0; i < 256; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
            var tick = PhysicsServer.SpaceGetTick(world.Space); var x = body.Position.X; var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(); owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && body.Position.X > x,
                $"Group invocation full physics frames {owner}/{all} B");
            Check(world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null, "Nested contexts preserve actual physics selection");
            Console.WriteLine($"Group context {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: own-group order/masks, next-frame ordering, message-only lanes, nested regular/group/error context, immediate/deferred delivery and 128 complete physics/query intervals at 0/0 B passed.");
            void Frame()
            {
                probe.Deliveries = probe.Hash = probe.NestedCalls = 0; var before = body.Position.X; tree.PhysicsFrame(.01); query.Position = body.GlobalPosition;
                Check(probe.NestedCalls == 3 && probe.Deliveries == 20 && probe.Hash == -528754496 && MathF.Abs(body.Position.X - before - .06f) < .0001f &&
                    body.InstanceID == identity && body.GetRID() == rid && world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rid,
                    "Actual nested worker, queued/current-group delivery, owner publication, physical motion and identity");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
    }
    private sealed class GroupGather : Node
    {
        internal Action? Collect;
        protected override void OnPhysicsProcess(double delta) => Collect!();
    }
    private static void GroupPolicy(SceneTree tree, GroupContextProbe probe, GroupGather gather, RigidBody body)
    {
        var execute = probe.Execute; var collect = gather.Collect; var order = new List<int>();
        var neutral = new Node(); var spatial = new Entity(); var control = new Control(); var area = new Area();
        probe.AddChild(neutral); probe.AddChild(spatial); probe.AddChild(control); probe.AddChild(area); body.Reparent(probe);
        Node[] members = [probe, neutral, spatial, control, area, body, body.GetChild(0)];
        var rid = body.GetRID(); var identity = body.InstanceID;
        probe.ProcessThreadGroupOrder = -2; gather.ProcessThreadGroupOrder = 0;
        gather.Collect = () => order.Add(2);
        probe.Execute = () =>
        {
            order.Add(1);
            foreach (var node in members)
            {
                node.ProcessThreadGroupOrder = int.MinValue; node.ProcessThreadMessages = Node.ProcessThreadMessageFlags.All;
                var properties = node.GetPropertyList();
                var priority = (PropertyDescriptor<Node, int>)properties.Single(p => p.Name == nameof(Node.ProcessThreadGroupOrder));
                var messages = (PropertyDescriptor<Node, Node.ProcessThreadMessageFlags>)properties.Single(p => p.Name == nameof(Node.ProcessThreadMessages));
                priority.SetValue(node, int.MaxValue); messages.SetValue(node, Node.ProcessThreadMessageFlags.Physics);
                Check(priority.IsStored && messages.IsStored && priority.GetValue(node) == int.MaxValue && messages.GetValue(node) == Node.ProcessThreadMessageFlags.Physics,
                    "Own-group metadata and stored descriptors work on neutral, spatial, UI and physical descendants");
                Reject<ArgumentOutOfRangeException>(() => node.ProcessThreadMessages = (Node.ProcessThreadMessageFlags)4);
                Check(node.ProcessThreadMessages == Node.ProcessThreadMessageFlags.Physics, "Invalid mask preserves the previous value");
            }
            Reject<InvalidOperationException>(() => gather.ProcessThreadGroupOrder = 8);
            Reject<InvalidOperationException>(() => gather.ProcessThreadMessages = Node.ProcessThreadMessageFlags.All);
            Reject<InvalidOperationException>(() => probe.ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread);
            Reject<InvalidOperationException>(() => body.LinearVelocity = new(1, 0));
            Reject<InvalidOperationException>(() => body.Position = new(1, 0));
            probe.RejectMetadata = true;
            Reject<InvalidOperationException>(() => probe.ProcessThreadGroupOrder = probe.ProcessThreadGroupOrder);
            Reject<InvalidOperationException>(() => probe.ProcessThreadMessages = probe.ProcessThreadMessages);
            Check(probe.ProcessThreadGroupOrder == int.MaxValue && probe.ProcessThreadMessages == Node.ProcessThreadMessageFlags.Physics,
                "Unchanged metadata writes still run the derived validation hook before mutation");
            probe.RejectMetadata = false;
            probe.ProcessThreadGroupOrder = 2;
        };
        tree.PhysicsFrame(.01);
        Check(order.SequenceEqual([1, 2]) && body.GetRID() == rid && body.InstanceID == identity,
            "Worker metadata writes preserve captured frame order and physical identity");
        order.Clear(); probe.Execute = () => order.Add(1); tree.PhysicsFrame(.01);
        Check(order.SequenceEqual([2, 1]), "A changed group order takes effect on the next captured frame");
        probe.Execute = () => { probe.ProcessThreadMessages = Node.ProcessThreadMessageFlags.Process; probe.PhysicsProcessEnabled = false; };
        tree.PhysicsFrame(.01); probe.Execute = null;
        var delivered = 0; var worker = 0;
        probe.CallDeferredThreadGroup<GroupContextProbe>(node =>
        {
            delivered++; worker = Environment.CurrentManagedThreadId;
            node.ProcessThreadMessages = Node.ProcessThreadMessageFlags.Physics; node.ProcessThreadGroupOrder = -2;
        });
        tree.PhysicsFrame(.01); Check(delivered == 0, "Worker-selected idle-only mask leaves the inactive physics queue pending");
        tree.ProcessFrame(.01);
        Check(delivered == 1 && worker != Environment.CurrentManagedThreadId && probe.ProcessThreadMessages == Node.ProcessThreadMessageFlags.Physics,
            "A message-only worker may update its own mask and next-frame order");
        probe.CallDeferredThreadGroup<GroupContextProbe>(_ => delivered++);
        tree.ProcessFrame(.01); Check(delivered == 1, "The new physics-only mask suppresses inactive idle delivery");
        tree.PhysicsFrame(.01); Check(delivered == 2, "The updated mask services the next eligible physics lane");
        var task = WorkerThreadPool.AddTask(() =>
        {
            Reject<InvalidOperationException>(() => probe.ProcessThreadGroupOrder = 9);
            Reject<InvalidOperationException>(() => probe.ProcessThreadMessages = 0);
            Reject<InvalidOperationException>(() => _ = probe.PhysicsProcessDeltaTime);
        });
        WorkerThreadPool.WaitForTaskCompletion(task);
        var detached = new Node();
        detached.ProcessThreadGroupOrder = int.MinValue; detached.ProcessThreadMessages = Node.ProcessThreadMessageFlags.All;
        detached.Dispose();
        Reject<ObjectDisposedException>(() => detached.ProcessThreadGroupOrder = 0);
        Reject<ObjectDisposedException>(() => detached.ProcessThreadMessages = 0);
        Reject<ObjectDisposedException>(() => _ = detached.ProcessThreadGroupOrder);
        Reject<ObjectDisposedException>(() => _ = detached.ProcessThreadMessages);
        using var source = new ProcessingCaptureNode(); using var packed = new PackedScene();
        source.Capturing = () =>
        {
            Reject<InvalidOperationException>(() => source.ProcessThreadGroupOrder = 0);
            Reject<InvalidOperationException>(() => source.ProcessThreadMessages = 0);
        };
        packed.Pack(source); source.Capturing = null;
        using var copy = packed.Instantiate();
        Check(copy.ProcessThreadGroupOrder == 0 && copy.ProcessThreadMessages == 0, "Capture rejects unchanged group metadata writes and releases the barrier");
        probe.ProcessThreadGroupOrder = gather.ProcessThreadGroupOrder = -1;
        probe.PhysicsProcessEnabled = true; probe.Execute = execute; gather.Collect = collect;
    }
}
