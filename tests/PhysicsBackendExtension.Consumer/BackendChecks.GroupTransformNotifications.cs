using Electron2D;

public static partial class BackendChecks
{
    private sealed class NotificationPolicyProbe : Entity
    {
        internal int LocalNotices, GlobalNotices;
        internal Action? Capturing;
        protected override Func<Node> CreateSceneInstanceFactory() { Capturing?.Invoke(); return Create; }
        private static Node Create() => new NotificationPolicyProbe();
        protected override void OnNotification(int what)
        {
            if (what == NotificationLocalTransformChanged) LocalNotices++;
            if (what == NotificationTransformChanged) GlobalNotices++;
            base.OnNotification(what);
        }
    }
    private sealed class NotificationGroup
    {
        internal readonly TransformWriter Writer;
        internal readonly Vector2 Origin;
        internal readonly CanvasItem[] Members;
        internal readonly RigidBody Body;
        internal readonly Camera Camera;
        internal readonly TileMapLayer Tiles;
        internal readonly int[] Locals, Globals;
        internal bool Active, Synchronize;
        internal Action<CanvasItem>? Hook;
        internal NotificationGroup(SubViewport root, Shape shape, TileSet set, Vector2 origin, uint layer, Barrier barrier)
        {
            Origin = origin; Writer = new() { Position = origin, ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
            Body = new() { Position = new(100, 100), GravityScale = 0, CanSleep = false, CollisionLayer = layer, CollisionMask = 0, LinearDampMode = RigidBody.DampMode.Replace };
            var circle = new CollisionShape { Shape = shape }; var polygon = new CollisionPolygon { Polygon = [new(-1, -1), new(1, -1), new(0, 1)] };
            Body.AddChild(circle); Body.AddChild(polygon);
            Camera = new() { ProcessCallback = ProcessPhase.Physics, LimitEnabled = false };
            Tiles = new() { Position = new(-200, 100), TileSet = set }; Tiles.SetCell(default, 3, Vector2i.Zero);
            Members = [new NotificationPolicyProbe(), new Entity(), new Sprite(), new MeshInstance(), new Bone(), new StaticBody(), new CharacterBody(), new AnimatableBody { SyncToPhysics = false }, new Area(), Body, circle, polygon, new Control(), Camera, Tiles];
            Locals = new int[Members.Length]; Globals = new int[Members.Length];
            foreach (var node in Members) if (node.Parent is null) Writer.AddChild(node);
            for (var i = 0; i < Members.Length; i++)
            {
                var index = i;
                Members[i].LocalTransformChanged += n =>
                {
                    if (!Active) return;
                    Check(Environment.CurrentManagedThreadId == Writer.ThreadID, "Local policy delivery keeps the current group thread"); Locals[index]++;
                    if (n is NotificationPolicyProbe probe) Check(probe.LocalNotices >= Locals[index], "Numeric local notification precedes its typed event");
                };
                Members[i].TransformChanged += n =>
                {
                    if (!Active) return;
                    Check(Environment.CurrentManagedThreadId == Writer.ThreadID, "Forced global delivery keeps the current group thread"); Globals[index]++;
                    if (index == 0 && Synchronize) { Synchronize = false; Check(barrier.SignalAndWait(TimeSpan.FromSeconds(5)), "Forced callbacks from independent groups hold no common scene gate"); }
                    if (n is NotificationPolicyProbe probe) Check(probe.GlobalNotices >= Globals[index], "Numeric global notification precedes its typed event");
                    Hook?.Invoke(n);
                };
            }
            root.AddChild(Writer);
        }
    }
    public static void RunGroupTransformNotifications(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GroupTransformNotifications", static () => new Solver());
        try
        {
            NotificationPolicyLifetime();
            if (poolOwner) { var task = WorkerThreadPool.AddTask(() => GroupTransformNotifications(backend, threaded, true)); WorkerThreadPool.WaitForTaskCompletion(task); }
            else GroupTransformNotifications(backend, threaded, false);
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
    }
    private static void NotificationPolicyLifetime()
    {
        using var source = new NotificationPolicyProbe { NotifyLocalTransformChanges = true, NotifyTransformChanges = true }; using var packed = new PackedScene(); var captures = 0;
        source.Capturing = () =>
        {
            captures++; Check(source.NotifyLocalTransformChanges && source.NotifyTransformChanges, "Notification policies remain readable during capture");
            Reject<InvalidOperationException>(() => source.NotifyTransformChanges = false); Reject<InvalidOperationException>(() => source.NotifyLocalTransformChanges = false); Reject<InvalidOperationException>(source.ForceUpdateTransform);
        };
        packed.Pack(source); source.Capturing = null; using var copy = (NotificationPolicyProbe)packed.Instantiate();
        Check(captures > 0 && source.NotifyTransformChanges && source.NotifyLocalTransformChanges && !copy.NotifyTransformChanges && !copy.NotifyLocalTransformChanges, "Capture rejects metadata mutation and notification policies remain transient");
        Reject<InvalidOperationException>(source.ForceUpdateTransform); source.Dispose();
        Reject<ObjectDisposedException>(() => _ = source.NotifyTransformChanges); Reject<ObjectDisposedException>(() => _ = source.NotifyLocalTransformChanges);
        Reject<ObjectDisposedException>(() => source.NotifyTransformChanges = true); Reject<ObjectDisposedException>(() => source.NotifyLocalTransformChanges = true); Reject<ObjectDisposedException>(source.ForceUpdateTransform);
    }
    private static void GroupTransformNotifications(PhysicsServer.Backend? backend, bool threaded, bool poolOwner)
    {
        using var world = backend is { } selected ? new World(selected) : new World("GroupTransformNotifications");
        using var root = new SubViewport { World = world, Size = new(128, 80) }; using var shape = new CircleShape { Radius = .5f };
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var atlas = new TileSetAtlasSource { Texture = texture }; using var set = new TileSet(); set.AddPhysicsLayer(); set.SetPhysicsLayerCollisionLayer(0, 4); set.AddSource(atlas, 3);
        using var navigation = new NavigationPolygon { AgentRadius = 0 }; navigation.SetVertices([new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]); navigation.AddPolygon([0, 1, 2, 3]); navigation.AddOutline([new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]); set.AddNavigationLayer();
        atlas.CreateTile(default); var tileData = atlas.GetTileData(default, 0); tileData.SetNavigationPolygon(0, navigation); tileData.AddCollisionPolygon(0); tileData.SetCollisionPolygonPoints(0, 0, [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]);
        using var together = new Barrier(2); var a = new NotificationGroup(root, shape, set, Vector2.Zero, 1, together); var b = new NotificationGroup(root, shape, set, new(500, 0), 2, together);
        var collect = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread, ProcessThreadGroupOrder = -1 }; root.AddChild(collect);
        using var tree = new SceneTree(root); tree.PhysicsInterpolation = true; a.Tiles.UpdateInternals(); b.Tiles.UpdateInternals();
        var region = NavigationServer.MapGetRegions(a.Tiles.GetNavigationMap()).Single(r => NavigationServer.RegionGetOwnerID(r) == a.Tiles.InstanceID);
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[4]; var cold = true; var failing = false;
        var rid = a.Body.GetRID(); var identity = a.Body.InstanceID; var tileRID = default(RID); var target = Vector2.Zero;
        a.Writer.Write = () => Write(a, b); b.Writer.Write = () => Write(b, a);
        collect.Write = () =>
        {
            target = a.Body.GlobalPosition; query.Position = target; query.CollisionMask = 1;
            Check(world.DirectSpaceState.IntersectPoint(query, hits) == 2 && hits[0].ColliderRID == rid && hits[1].ColliderRID == rid, "Owner queries publish forced worker body and child-shape poses before numerical integration");
            query.Position = a.Tiles.GlobalTransform * new Vector2(8, 8); query.CollisionMask = 4;
            if (!failing)
            {
                var tileCount = world.DirectSpaceState.IntersectPoint(query, hits);
                if (tileCount != 1 || hits[0].ColliderObject != a.Tiles) throw new InvalidOperationException($"Forced tile publication: {tileCount} hits at {query.Position}, logical {a.Tiles.GlobalPosition}, owner {hits[0].ColliderObject?.InstanceID}/{a.Tiles.InstanceID}");
                Check(NavigationServer.RegionGetTransform(region).Origin.DistanceTo(query.Position) < .0001f, "Forced tile notification publishes its navigation region with stable identity before owner observation");
                if (tileRID == default) tileRID = hits[0].ColliderRID; else Check(tileRID == hits[0].ColliderRID, "Tile publication retains the generated physical identity");
                Check(a.Camera.GetScreenCenterPosition().DistanceTo(a.Camera.GlobalPosition) < .0001f, "Forced camera publication updates its viewport after the worker join");
            }
            a.Body.LinearVelocity = new(4, 0);
        };
        a.Synchronize = b.Synchronize = true; Frame(); Check(a.Writer.ThreadID != b.Writer.ThreadID, "The forced-notification profile executes two independent callbacks concurrently"); cold = false;
        Frame(idle: true);
        var expectedFailure = new ApplicationException("Expected uncaught forced observer failure"); var throwOnce = true;
        a.Hook = _ => { if (throwOnce) { throwOnce = false; throw expectedFailure; } }; failing = true;
        var failedTick = PhysicsServer.SpaceGetTick(world.Space); var failedFrame = CaptureCopy(() => tree.PhysicsFrame(.01)); a.Hook = null; failing = false;
        Check(failedFrame is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(expectedFailure) && PhysicsServer.SpaceGetTick(world.Space) == failedTick + 1,
            "Uncaught forced observer failure joins every worker and still releases spaces before the selected numerical step");
        query.Position = a.Tiles.GlobalTransform * new Vector2(8, 8); query.CollisionMask = 4;
        Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderObject == a.Tiles, "Normal owner notification phases finish pending tile work after the earlier observer failure");
        using var outsideDone = new ManualResetEventSlim();
        var outside = WorkerThreadPool.AddTask(() =>
        {
            try { Reject<InvalidOperationException>(() => _ = a.Body.NotifyTransformChanges); Reject<InvalidOperationException>(() => _ = a.Body.NotifyLocalTransformChanges); Reject<InvalidOperationException>(() => a.Body.NotifyTransformChanges = true); Reject<InvalidOperationException>(() => a.Body.NotifyLocalTransformChanges = true); Reject<InvalidOperationException>(a.Body.ForceUpdateTransform); }
            finally { outsideDone.Set(); }
        }, highPriority: true);
        Check(outsideDone.Wait(TimeSpan.FromSeconds(5)), "An ordinary job completes without scene-owner helper rights"); WorkerThreadPool.WaitForTaskCompletion(outside);
        for (var i = 0; i < 128; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
        var iteration = NavigationServer.MapGetIterationID(a.Tiles.GetNavigationMap());
        var tick = PhysicsServer.SpaceGetTick(world.Space); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(); own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && NavigationServer.MapGetIterationID(a.Tiles.GetNavigationMap()) == iteration, $"Forced transform notifications full physics frames {own}/{all} B");
        Check(world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null, "Notification publication keeps the explicitly selected implementation");
        Console.WriteLine($"Group transform notifications {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}/{(poolOwner ? "pool-host" : "ordinary-host")}: policies, synchronous force/reentry/error delivery, camera/tile publication and 128 complete physics/query intervals at 0/0 B passed.");
        void Write(NotificationGroup ownGroup, NotificationGroup foreign)
        {
            ownGroup.Active = true;
            try
            {
                ownGroup.Writer.Position = ownGroup.Origin;
                foreach (var item in ownGroup.Members) { item.NotifyLocalTransformChanges = true; item.NotifyTransformChanges = true; _ = item.GetGlobalTransform(); }
                ownGroup.Writer.Position += Vector2.Right;
                for (var i = 0; i < ownGroup.Members.Length; i++)
                {
                    var item = ownGroup.Members[i]; var local = ownGroup.Locals[i]; var global = ownGroup.Globals[i];
                    Check(item.NotifyLocalTransformChanges && item.NotifyTransformChanges, "Own and foreign policy reads use processing observation rights");
                    _ = foreign.Members[i].NotifyTransformChanges; _ = foreign.Members[i].NotifyLocalTransformChanges;
                    _ = item.PhysicsInterpolationMode; _ = foreign.Members[i].PhysicsInterpolationMode;
                    Check(item.IsPhysicsInterpolatedAndEnabled() == item.IsPhysicsInterpolated(), "Policy queries observe inherited active interpolation on the group"); _ = foreign.Members[i].IsPhysicsInterpolatedAndEnabled();
                    item.NotifyTransformChanges = false;
                    if (item is Entity entity) entity.Position = entity.Position;
                    Check(ownGroup.Locals[i] == local + (item is Entity ? 1 : 0), "Enabled equal local assignments notify synchronously");
                    item.ForceUpdateTransform(); item.ForceUpdateTransform(); Check(ownGroup.Globals[i] == global + 1, "Disabling retains one pending entry and force consumes it exactly once");
                    item.NotifyLocalTransformChanges = false;
                    if (item is Entity muted) muted.Position = muted.Position;
                    Check(ownGroup.Locals[i] == local + (item is Entity ? 1 : 0), "Disabling local policy suppresses later local delivery");
                    item.NotifyTransformChanges = true; ownGroup.Writer.Position += Vector2.Right;
                    item.NotifyTransformChanges = false; item.ForceUpdateTransform(); item.ForceUpdateTransform();
                    Check(ownGroup.Globals[i] == global + 2, "Enabling resolves invalidation and the next changed ancestor queues one fresh entry");
                    item.NotifyTransformChanges = true;
                }
                if (cold) Guard(ownGroup, foreign);
            }
            finally { ownGroup.Active = false; }
        }
        void Guard(NotificationGroup ownGroup, NotificationGroup foreign)
        {
            Reject<InvalidOperationException>(() => foreign.Body.NotifyLocalTransformChanges = foreign.Body.NotifyLocalTransformChanges);
            Reject<InvalidOperationException>(() => foreign.Body.NotifyTransformChanges = foreign.Body.NotifyTransformChanges); Reject<InvalidOperationException>(foreign.Body.ForceUpdateTransform);
            var probe = (Entity)ownGroup.Members[0]; var reentered = false;
            ownGroup.Hook = n => { if (!ReferenceEquals(n, probe) || reentered) return; reentered = true; probe.GetGlobalTransform(); probe.Position += Vector2.Right; probe.ForceUpdateTransform(); };
            probe.GetGlobalTransform(); probe.Position += Vector2.Right; probe.ForceUpdateTransform(); ownGroup.Hook = null; Check(reentered, "Removing before callbacks permits explicit recursive forcing");
            var expected = new ApplicationException("Expected forced observer failure"); ownGroup.Hook = n => { if (ReferenceEquals(n, probe)) throw expected; };
            probe.GetGlobalTransform(); probe.Position += Vector2.Right; var error = CaptureCopy(probe.ForceUpdateTransform); ownGroup.Hook = null;
            Check(ReferenceEquals(error, expected), "Forced observer propagates the original exception immediately"); probe.ForceUpdateTransform();
            var before = ownGroup.Writer.NotifyTransformChanges; ownGroup.Writer.RejectChanges = true;
            Reject<InvalidOperationException>(() => ownGroup.Writer.NotifyTransformChanges = !before); Reject<InvalidOperationException>(() => ownGroup.Writer.NotifyLocalTransformChanges = true); Reject<InvalidOperationException>(ownGroup.Writer.ForceUpdateTransform);
            ownGroup.Writer.RejectChanges = false; Check(ownGroup.Writer.NotifyTransformChanges == before, "Derived validation veto preserves notification policy");
            Reject<InvalidOperationException>(tree.Dispose); Reject<InvalidOperationException>(() => world.DirectSpaceState.IntersectPoint(query, hits));
        }
        void Frame(bool idle = false)
        {
            if (idle) tree.ProcessFrame(.01); else tree.PhysicsFrame(.01);
            if (a.Body.InstanceID != identity || a.Body.GetRID() != rid || a.Body.GlobalPosition.DistanceTo(target + (idle ? Vector2.Zero : new Vector2(.04f, 0))) >= .001f) throw new InvalidOperationException($"Notification motion: actual {a.Body.GlobalPosition}, target {target}, velocity {a.Body.LinearVelocity}, idle {idle}, tick {PhysicsServer.SpaceGetTick(world.Space)}");
        }
    }
}
