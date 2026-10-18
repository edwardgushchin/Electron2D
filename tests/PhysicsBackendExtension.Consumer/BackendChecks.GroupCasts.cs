using Electron2D;

public static partial class BackendChecks
{
    private sealed class CastNoticeProbe : Node
    {
        internal int Notices, ThreadID;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationInternalPhysicsProcess) { Notices++; ThreadID = Environment.CurrentManagedThreadId; }
        }
    }
    private sealed class CastGroup
    {
        internal readonly TransformWriter Writer = new() { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        internal readonly RigidBody Actor;
        internal readonly StaticBody Target;
        internal readonly RayCast Ray = new() { Name = "Ray", Enabled = false, TargetPosition = new(0, 60) };
        internal readonly ShapeCast Cast = new() { Name = "Cast", Enabled = false, TargetPosition = new(0, 60), MaxResults = 2 };
        internal readonly NotificationPolicyProbe Capture = new();
        internal readonly Node Assigned = new();
        internal readonly CastNoticeProbe Probe = new();
        internal readonly RID[] Exclusions = new RID[5];
        internal readonly Vector2 Origin;
        internal Vector2 RayPoint, RayNormal, CastPoint, CastNormal;
        internal float Safe, Unsafe;
        internal int Phase;
        internal CastGroup(Node root, Shape bodyShape, Shape queryShape, Vector2 origin)
        {
            Origin = origin; Actor = new() { Position = origin + new Vector2(100, 100), GravityScale = 0, CanSleep = false, CollisionMask = 0 };
            Target = new() { Position = origin + new Vector2(100, 130) };
            Actor.AddChild(new CollisionShape { Shape = bodyShape }); Target.AddChild(new CollisionShape { Shape = bodyShape }); Cast.Shape = queryShape;
            Actor.AddChild(Ray); Actor.AddChild(Cast); Actor.AddChild(Capture); Actor.AddChild(Probe); Ray.Owner = Cast.Owner = Capture.Owner = Actor;
            Writer.AddChild(Actor); root.AddChild(Writer); root.AddChild(Target); root.AddChild(Assigned);
            for (var i = 0; i < Exclusions.Length; i++) Exclusions[i] = PhysicsServer.BodyCreate();
        }
    }
    public static void RunGroupCasts(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GroupCasts", () => new Solver());
        try
        {
            if (poolOwner) { var job = WorkerThreadPool.AddTask(() => GroupCasts(backend, threaded)); WorkerThreadPool.WaitForTaskCompletion(job); }
            else GroupCasts(backend, threaded);
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
    }
    private static void GroupCasts(PhysicsServer.Backend? backend, bool threaded)
    {
        using var world = backend is { } selected ? new World(selected) : new World("GroupCasts"); using var root = new SubViewport { World = world };
        using var geometry = new CircleShape { Radius = 3 }; using var queryShape = new CircleShape { Radius = 1 }; using var alternate = new CircleShape { Radius = .75f };
        var a = new CastGroup(root, geometry, queryShape, default); var b = new CastGroup(root, geometry, queryShape, new(500, 0));
        var collector = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread }; root.AddChild(collector);
        using var tree = new SceneTree(root); using var together = new Barrier(2); var cold = true; var observing = true;
        try
        {
            foreach (var group in new[] { a, b }) { PhysicsServer.BodyAttachObject(group.Target.GetRID(), group.Assigned); Sample(group); }
            a.Writer.Write = () => Write(a, b); b.Writer.Write = () => Write(b, a); collector.Write = () => { if (observing) { Sample(a); Sample(b); } };
            tree.PhysicsFrame(.01); Check(a.Writer.ThreadID != b.Writer.ThreadID, "Independent cast metadata groups overlap"); cold = false;
            using (var packed = new PackedScene())
            {
                a.Capture.Capturing = () => { Held(a); Reject<InvalidOperationException>(() => a.Ray.Enabled = false); Reject<InvalidOperationException>(() => a.Cast.MaxResults = 2); };
                packed.Pack(a.Actor); a.Capture.Capturing = null; using var copy = packed.Instantiate();
                Check(!copy.GetNode<RayCast>("Ray").Enabled && !copy.GetNode<RayCast>("Ray").IsColliding() && copy.GetNode<ShapeCast>("Cast").Shape == a.Cast.Shape && copy.GetNode<ShapeCast>("Cast").GetCollisionCount() == 0,
                    "Packing preserves authored cast options and borrowed shape, without cached results");
            }
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01); Collect(); for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            var tick = PhysicsServer.SpaceGetTick(world.Space); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128, $"Grouped cast frames allocated {own}/{all} B");
            observing = false; using var done = new ManualResetEventSlim(); var job = WorkerThreadPool.AddTask(() =>
            {
                try { Reject<InvalidOperationException>(() => a.Ray.IsColliding()); Reject<InvalidOperationException>(() => a.Cast.GetCollisionCount()); Guards(a); }
                finally { done.Set(); }
            }, highPriority: true);
            Check(done.Wait(TimeSpan.FromSeconds(5)), "Ordinary worker rejection completes without scene owner assistance"); WorkerThreadPool.WaitForTaskCompletion(job);
            a.Ray.Dispose(); a.Cast.Dispose(); Reject<ObjectDisposedException>(() => a.Ray.GetColliderRID()); Reject<ObjectDisposedException>(() => a.Cast.GetCollisionCount());
            Check(world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null, "Explicit backend selection remains unchanged");
            Console.WriteLine($"Group casts {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: cached results, complete metadata/exception edits, owner-only sampling, packing/lifetime and 128 complete physics/query intervals at {own}/{all} B passed.");
        }
        finally { foreach (var group in new[] { a, b }) foreach (var rid in group.Exclusions) PhysicsServer.FreeRID(rid); }
        void Write(CastGroup group, CastGroup other)
        {
            Held(group);
            if (cold) Cold(group, other);
            group.Phase++; group.Actor.Position = group.Origin + new Vector2(100 + (group.Phase & 1), 100);
            group.Ray.TargetPosition = new(0, 60); group.Cast.TargetPosition = new(0, 60); group.Cast.Shape = (group.Phase & 1) == 0 ? queryShape : alternate;
            group.Ray.CollisionMask = group.Cast.CollisionMask = 1; group.Ray.SetCollisionMaskValue(32, true); group.Cast.SetCollisionMaskValue(32, true);
            Check(group.Ray.GetCollisionMaskValue(1) && group.Ray.GetCollisionMaskValue(32) && group.Cast.GetCollisionMaskValue(1) && group.Cast.GetCollisionMaskValue(32), "Both complete layer-bit projections remain usable");
            group.Ray.SetCollisionMaskValue(32, false); group.Cast.SetCollisionMaskValue(32, false);
            group.Ray.HitFromInside = true; group.Ray.HitFromInside = false; group.Cast.Margin = .1f; group.Cast.Margin = 0; group.Cast.MaxResults = 1; group.Cast.MaxResults = 2;
            group.Ray.CollideWithAreas = group.Cast.CollideWithAreas = true; group.Ray.CollideWithAreas = group.Cast.CollideWithAreas = false;
            group.Ray.CollideWithBodies = group.Cast.CollideWithBodies = false; group.Ray.CollideWithBodies = group.Cast.CollideWithBodies = true;
            foreach (var rid in group.Exclusions) { group.Ray.AddExceptionRID(rid); group.Cast.AddExceptionRID(rid); }
            foreach (var rid in group.Exclusions) { group.Ray.RemoveExceptionRID(rid); group.Cast.RemoveExceptionRID(rid); }
            group.Ray.AddException(group.Target); group.Cast.AddException(group.Target); group.Ray.RemoveException(group.Target); group.Cast.RemoveException(group.Target);
            group.Ray.ClearExceptions(); group.Cast.ClearExceptions(); group.Ray.ExcludeParent = group.Cast.ExcludeParent = false; group.Ray.ExcludeParent = group.Cast.ExcludeParent = true;
            group.Ray.Enabled = group.Cast.Enabled = true; group.Ray.Enabled = group.Cast.Enabled = false;
            Check(!group.Ray.IsColliding() && !group.Cast.IsColliding() && group.Ray.GetColliderRID() == group.Target.GetRID() && group.Cast.GetColliderRID(0) == group.Target.GetRID(), "Disable clears only flags, preserving held contact identities");
        }
        void Cold(CastGroup group, CastGroup other)
        {
            Held(other); var results = group.Cast.CollisionResult;
            Check(results.Length == 1 && results[0].ColliderObject == group.Assigned && results[0].Collider == group.Target, "Copied cached results retain sampled object and physical scene owner");
            Guards(other); Reject<InvalidOperationException>(() => group.Ray.ForceRaycastUpdate()); Reject<InvalidOperationException>(() => group.Cast.ForceShapecastUpdate());
            if (!together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Cast groups did not overlap");
            Reject<ArgumentOutOfRangeException>(() => group.Ray.TargetPosition = new(float.NaN, 0));
            Reject<ArgumentOutOfRangeException>(() => group.Cast.Margin = float.NaN);
            Reject<ArgumentOutOfRangeException>(() => group.Cast.GetColliderRID(1));
            var previous = group.Cast.Shape; group.Cast.Shape = null; Check(group.Cast.Shape is null, "Null query geometry remains an authored option"); group.Cast.Shape = previous;
            using var expired = new CircleShape(); expired.Dispose(); Reject<ObjectDisposedException>(() => group.Cast.Shape = expired);
            Check(group.Cast.Shape == previous && group.Cast.Margin == 0 && group.Ray.TargetPosition == new Vector2(0, 60), "Rejected edits preserve complete prior cast options");
            group.Ray.Enabled = group.Cast.Enabled = true;
            var error = CaptureCopy(() => group.Actor.PropagateNotification(Node.NotificationInternalPhysicsProcess));
            Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Count == 2 && aggregate.Flatten().InnerExceptions.All(e => e is InvalidOperationException) && group.Probe.Notices == 1 && group.Probe.ThreadID == Environment.CurrentManagedThreadId,
                "Reserved numerical sampling rejects immediately while later notification receivers still run on this group");
            group.Ray.Enabled = group.Cast.Enabled = false;
        }
        void Held(CastGroup group)
        {
            Check(group.Ray.IsColliding() && group.Ray.GetCollider() == group.Assigned && group.Ray.GetColliderRID() == group.Target.GetRID() && group.Ray.GetColliderShape() == 0 && group.Ray.GetCollisionPoint() == group.RayPoint && group.Ray.GetCollisionNormal() == group.RayNormal,
                "Ray cache is observable without numerical access");
            Check(group.Cast.IsColliding() && group.Cast.GetCollisionCount() == 1 && group.Cast.GetCollider(0) == group.Assigned && group.Cast.GetColliderRID(0) == group.Target.GetRID() && group.Cast.GetColliderShape(0) == 0 && group.Cast.GetCollisionPoint(0) == group.CastPoint && group.Cast.GetCollisionNormal(0) == group.CastNormal && group.Cast.GetClosestCollisionSafeFraction() == group.Safe && group.Cast.GetClosestCollisionUnsafeFraction() == group.Unsafe,
                "Complete held shape-cast results are observable without numerical access");
        }
        void Sample(CastGroup group)
        {
            group.Ray.ForceRaycastUpdate(); group.Cast.ForceShapecastUpdate();
            Check(group.Ray.IsColliding() && group.Ray.GetColliderRID() == group.Target.GetRID() && group.Cast.GetCollisionCount() == 1 && group.Cast.GetColliderRID(0) == group.Target.GetRID(), "Owner sampling sees worker options and exact target/parent exclusion");
            group.RayPoint = group.Ray.GetCollisionPoint(); group.RayNormal = group.Ray.GetCollisionNormal(); group.CastPoint = group.Cast.GetCollisionPoint(0); group.CastNormal = group.Cast.GetCollisionNormal(0);
            group.Safe = group.Cast.GetClosestCollisionSafeFraction(); group.Unsafe = group.Cast.GetClosestCollisionUnsafeFraction();
            Check(group.Safe > .3f && group.Unsafe < .6f && group.Safe <= group.Unsafe, "Selected swept contact lies near the analytic 26/60 travel fraction");
        }
        void Guards(CastGroup group)
        {
            Reject<InvalidOperationException>(() => group.Ray.Enabled = false); Reject<InvalidOperationException>(() => group.Cast.Enabled = false);
            Reject<InvalidOperationException>(() => group.Ray.AddExceptionRID(default)); Reject<InvalidOperationException>(() => group.Cast.ClearExceptions());
        }
    }
}
