using Electron2D;

public static partial class BackendChecks
{
    private sealed class GroupTransformParent : Entity
    {
        internal Barrier? Readers;
        internal Exception? Failure;
        internal int Calls;
        public override Transform GetTransform()
        {
            Interlocked.Increment(ref Calls);
            if (Readers is { } readers) Check(readers.SignalAndWait(TimeSpan.FromSeconds(5)), "Concurrent transform overrides run outside the cache publication lock");
            if (Failure is { } error) throw error;
            return base.GetTransform();
        }
    }
    private sealed class GroupTransformReader : Node
    {
        internal Action? Read;
        internal int ThreadID, Count;
        protected override void OnPhysicsProcess(double delta) { ThreadID = Environment.CurrentManagedThreadId; Read!(); Count++; }
    }
    private static readonly Action<Entity>[] GroupTransformReads =
    [
        node => _ = node.Transform, node => _ = node.GetTransform(), node => _ = node.Position,
        node => _ = node.Rotation, node => _ = node.RotationDegrees, node => _ = node.Scale, node => _ = node.Skew,
        node => _ = node.GlobalTransform, node => _ = node.GetGlobalTransform(), node => _ = node.GlobalPosition,
        node => _ = node.GlobalRotation, node => _ = node.GlobalRotationDegrees, node => _ = node.GlobalScale, node => _ = node.GlobalSkew,
        node => _ = node.ToGlobal(Vector2.One), node => _ = node.ToLocal(Vector2.One), node => _ = node.GetAngleTo(Vector2.One),
        node => _ = node.GetRelativeTransformToParent(node)
    ];
    public static void RunGroupTransforms(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GroupTransforms", static () => new Solver());
        try
        {
            if (poolOwner) { var job = WorkerThreadPool.AddTask(() => GroupTransforms(backend, threaded, true)); WorkerThreadPool.WaitForTaskCompletion(job); }
            else GroupTransforms(backend, threaded, false);
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
    }
    private static void GroupTransforms(PhysicsServer.Backend? backend, bool threaded, bool poolOwner)
    {
        using var world = backend is { } selected ? new World(selected) : new World("GroupTransforms");
        using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 1 };
        var parent = new GroupTransformParent { Transform = new(.3f, new(-2, 3), .2f, new(50, 20)), NotifyTransformChanges = true };
        var middle = new Entity { Transform = new(-.2f, new(2, .5f), -.1f, new(4, 6)) };
        var leaf = new Entity { Transform = new(.5f, new(1.2f, 1.5f), .15f, new(3, 8)) };
        root.AddChild(parent); parent.AddChild(middle); middle.AddChild(leaf);
        var top = new Entity { Position = new(7, 11), TopLevel = true }; parent.AddChild(top);
        var neutral = new Node(); var broken = new Entity { Position = new(13, 17) }; parent.AddChild(neutral); neutral.AddChild(broken);
        var control = new Control { Position = new(5, 6), Size = new(40, 20), Rotation = .2f, Scale = new(1.3f, 1.1f), PivotOffset = new(3, 4), PivotOffsetRatio = new(.25f, .5f), OffsetTransformEnabled = true, OffsetTransformPosition = new(2, 3), OffsetTransformPositionRatio = new(.1f, .2f) };
        var singular = new Entity { Transform = new(new(1, 0), new(2, 0), Vector2.Zero) }; parent.AddChild(singular);
        var dead = new Entity(); dead.Dispose();
        var underControl = new Entity { Position = new(19, 23) }; parent.AddChild(control); control.AddChild(underControl);
        var a = new GroupTransformReader { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var b = new GroupTransformReader { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        root.AddChild(a); root.AddChild(b);
        var first = new RigidBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, Position = new(-20, -20) };
        var second = new RigidBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, Position = new(20, 20) };
        first.AddChild(new CollisionShape { Shape = shape }); second.AddChild(new CollisionShape { Shape = shape }); a.AddChild(first); b.AddChild(second);
        Entity[] targets = [leaf, top, broken, underControl, first, second]; var locals = new Transform[targets.Length]; var globals = new Transform[targets.Length];
        Transform controlLocal = default, controlGlobal = default, relative = default; Vector2 pivot = default;
        var owner = Environment.CurrentManagedThreadId; var notices = 0; var errors = false;
        parent.TransformChanged += _ => { Check(Environment.CurrentManagedThreadId == owner, "Transform notifications remain on the scene owner"); notices++; };
        var collector = new GroupTransformReader { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread, ProcessThreadGroupOrder = -1 };
        collector.Read = () =>
        {
            if (!errors) Check(a.Count == b.Count && a.Count > 0, "Both real parallel groups finish their reads before main physics collection");
            first.LinearVelocity = new(4, 0); second.LinearVelocity = new(0, 5);
            parent.Position = parent.Position;
        };
        root.AddChild(collector); using var tree = new SceneTree(root);
        var guardChecks = true;
        a.Read = () => Read(first, second); b.Read = () => Read(second, first);
        using var barrier = new Barrier(2);
        Snapshot(); parent.Position = parent.Position; parent.ForceUpdateTransform(); var beforeNotices = notices; var calls = parent.Calls;
        parent.Readers = barrier; tree.PhysicsFrame(.01); parent.Readers = null;
        Check(a.ThreadID != b.ThreadID && parent.Calls == calls + 2, "Two dirty global queries compute before publishing one shared cache");
        parent.ForceUpdateTransform(); Check(notices == beforeNotices + 1, "Worker reads clear global invalidation so the next equal owner assignment queues a new notification");
        guardChecks = false;
        var failure = new ApplicationException("Expected transform getter failure"); parent.Failure = failure; parent.Position = parent.Position; errors = true;
        var tick = PhysicsServer.SpaceGetTick(world.Space);
        var caught = CaptureCopy(() => tree.PhysicsFrame(.01));
        Check(caught is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(failure) && PhysicsServer.SpaceGetTick(world.Space) == tick + 1,
            "Getter failures join frame errors without dropping physical integration");
        parent.Failure = null; errors = false;
        using var outsideDone = new ManualResetEventSlim();
        var outside = WorkerThreadPool.AddTask(() =>
        {
            try
            {
                Check(Environment.CurrentManagedThreadId != owner, "Ordinary read rejection is exercised on a non-owner worker");
                foreach (var read in GroupTransformReads) Reject<InvalidOperationException>(() => read(leaf));
                Reject<InvalidOperationException>(() => control.GetTransform()); Reject<InvalidOperationException>(() => control.GetCombinedPivotOffset());
            }
            finally { outsideDone.Set(); }
        }, highPriority: true);
        Check(outsideDone.Wait(TimeSpan.FromSeconds(5)), "A non-owner ordinary job completes before the pooled scene owner helps work");
        WorkerThreadPool.WaitForTaskCompletion(outside);
        control.OffsetTransformVisualOnly = true;
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2]; var rid = first.GetRID(); var identity = first.InstanceID;
        for (var i = 0; i < 256; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
        tick = PhysicsServer.SpaceGetTick(world.Space); var x = first.Position.X; var allocated = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(); allocated = GC.GetAllocatedBytesForCurrentThread() - allocated; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(allocated == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && first.Position.X > x, $"Worker transform full physics frames {allocated}/{all} B");
        Check(world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null, "Read-only transforms preserve actual selected physics");
        Console.WriteLine($"Group transforms {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}/{(poolOwner ? "pool-host" : "ordinary-host")}: parallel own/foreign logical reads, shared cache, control ancestry, notifications/errors and 128 complete physics/query intervals at 0/0 B passed.");
        void Snapshot()
        {
            for (var i = 0; i < targets.Length; i++) { locals[i] = targets[i].Transform; globals[i] = targets[i].GlobalTransform; }
            controlLocal = control.GetTransform(); controlGlobal = control.GetGlobalTransform(); pivot = control.GetCombinedPivotOffset(); relative = leaf.GetRelativeTransformToParent(parent);
        }
        void Read(RigidBody own, RigidBody foreign)
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var node = targets[i]; var local = locals[i]; var global = globals[i];
                Check(node.Transform.IsEqualApprox(local) && node.GetTransform().IsEqualApprox(local) && node.Position.IsEqualApprox(local.Origin) &&
                    Mathf.IsEqualApprox(node.Rotation, local.Rotation) && Mathf.IsEqualApprox(node.RotationDegrees, Mathf.RadToDeg(local.Rotation)) && node.Scale.IsEqualApprox(local.Scale) && Mathf.IsEqualApprox(node.Skew, local.Skew), "All local spatial observations work in own and foreign groups");
                Check(node.GlobalTransform.IsEqualApprox(global) && node.GetGlobalTransform().IsEqualApprox(global) && node.GlobalPosition.IsEqualApprox(global.Origin) &&
                    Mathf.IsEqualApprox(node.GlobalRotation, global.Rotation) && Mathf.IsEqualApprox(node.GlobalRotationDegrees, Mathf.RadToDeg(global.Rotation)) && node.GlobalScale.IsEqualApprox(global.Scale) && Mathf.IsEqualApprox(node.GlobalSkew, global.Skew), "All global spatial observations preserve the owner result");
                Check(node.ToGlobal(new(2, 3)).IsEqualApprox(global * new Vector2(2, 3)) && node.ToLocal(global * new Vector2(2, 3)).DistanceTo(new(2, 3)) < .0001f &&
                    Mathf.Abs(Mathf.AngleDifference(node.GetAngleTo(global * new Vector2(2, 3)), (new Vector2(2, 3) * local.Scale).Angle())) < .0001f && node.GetRelativeTransformToParent(node) == Transform.Identity,
                    "Coordinate/angle/self-relative queries retain numeric semantics");
            }
            Check(leaf.GetRelativeTransformToParent(parent).IsEqualApprox(relative) && top.GlobalTransform == top.Transform && broken.GlobalTransform == broken.Transform,
                "Relative, TopLevel and neutral-boundary chains retain their separate meanings");
            Check(control.GetTransform().IsEqualApprox(controlLocal) && control.GetGlobalTransform().IsEqualApprox(controlGlobal) && control.GetCombinedPivotOffset() == pivot &&
                control.GetRect().Position.IsEqualApprox(controlLocal.Origin) && control.GetGlobalRect().Position.IsEqualApprox(controlGlobal.Origin), "Control pivot/offset/rectangle observations support mixed ancestry");
            if (guardChecks) Guard(own, foreign);
        }
        void Guard(RigidBody own, RigidBody foreign)
        {
            own.Position = own.Position; Reject<InvalidOperationException>(() => foreign.Position = foreign.Position); Reject<InvalidOperationException>(() => leaf.Translate(Vector2.One));
            Reject<InvalidOperationException>(() => leaf.GlobalTransform = leaf.GlobalTransform); Reject<InvalidOperationException>(() => parent.ForceUpdateTransform());
            Reject<InvalidOperationException>(() => first.LinearVelocity = Vector2.Zero);
            foreach (var read in GroupTransformReads) Reject<ObjectDisposedException>(() => read(dead));
            Reject<InvalidOperationException>(() => singular.ToLocal(Vector2.One)); Reject<InvalidOperationException>(() => singular.GetAngleTo(Vector2.One));
            Reject<ArgumentNullException>(() => leaf.GetRelativeTransformToParent(null!)); Reject<ArgumentException>(() => leaf.GetRelativeTransformToParent(broken));
            Reject<ArgumentOutOfRangeException>(() => leaf.ToLocal(new(float.NaN, 0))); Reject<ArgumentOutOfRangeException>(() => leaf.GetAngleTo(new(float.PositiveInfinity, 0)));
        }

        void Frame()
        {
            Snapshot(); parent.Position = parent.Position; parent.ForceUpdateTransform(); var oldNotices = notices; var oldX = first.Position.X;
            tree.PhysicsFrame(.01); parent.ForceUpdateTransform(); query.Position = first.GlobalPosition;
            Check(notices == oldNotices + 1 && MathF.Abs(first.Position.X - oldX - .04f) < .0001f && first.InstanceID == identity && first.GetRID() == rid &&
                world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rid, "Full parallel observation/barrier/publication/physics/query identity interval");
        }
    }
}
