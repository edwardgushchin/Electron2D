using Electron2D;

public static partial class BackendChecks
{
    private sealed class CanvasNoticeProbe : Entity
    {
        internal bool Observe;
        internal int Notices, LastThread;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (Observe && what is NotificationEnterCanvas or NotificationExitCanvas or NotificationEnabled or NotificationDisabled)
            { Notices++; LastThread = Environment.CurrentManagedThreadId; }
        }
    }
    private sealed class CanvasGroup
    {
        internal readonly CanvasLayer Layer = new();
        internal readonly TransformWriter Writer = new() { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        internal readonly CanvasNoticeProbe Probe = new();
        internal readonly PhysicsBody[] Bodies;
        internal readonly Area[] Areas;
        internal readonly TileMapLayer Tiles;
        internal readonly Vector2 Origin;
        internal readonly RID[] TileRIDs = new RID[2];
        internal int Phase;
        internal CanvasGroup(Node root, Shape shape, TileSet set, Vector2 origin)
        {
            Origin = origin;
            Bodies = [new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(2, 0), LinearDampMode = RigidBody.DampMode.Replace }, new StaticBody(), new CharacterBody(), new AnimatableBody { SyncToPhysics = false }, new PhysicalBone { AutoConfigureJoint = false },
                new RigidBody { ProcessMode = ProcessMode.Disabled, DisableMode = CollisionDisableMode.Remove, GravityScale = 0 },
                new RigidBody { ProcessMode = ProcessMode.Disabled, DisableMode = CollisionDisableMode.MakeStatic, GravityScale = 0 },
                new RigidBody { ProcessMode = ProcessMode.Disabled, DisableMode = CollisionDisableMode.KeepActive, GravityScale = 0, CanSleep = false }];
            for (var i = 0; i < Bodies.Length; i++)
            {
                var body = Bodies[i]; body.Position = origin + new Vector2(100 + 25 * i, 100); body.CollisionLayer = 1; body.CollisionMask = 0;
                body.AddChild(new CollisionShape { Shape = shape }); Writer.AddChild(body);
            }
            Areas = [new Area { Position = origin + new Vector2(100, 180), CollisionLayer = 2 }, new Area { Position = origin + new Vector2(140, 180), CollisionLayer = 2, ProcessMode = ProcessMode.Disabled, DisableMode = CollisionDisableMode.MakeStatic }];
            foreach (var area in Areas) { area.AddChild(new CollisionShape { Shape = shape }); Writer.AddChild(area); }
            Tiles = new() { TileSet = set, PhysicsQuadrantSize = 1, Position = origin }; Tiles.SetCell(default, 3, Vector2i.Zero); Tiles.SetCell(new(2, 0), 3, Vector2i.Zero);
            Writer.AddChild(Tiles); Writer.AddChild(Probe); Layer.AddChild(Writer); root.AddChild(Layer);
        }
    }
    public static void RunGroupCanvas(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("GroupCanvas", () => solver = new Solver());
        try
        {
            if (poolOwner) { var task = WorkerThreadPool.AddTask(() => GroupCanvas(backend, threaded, () => solver)); WorkerThreadPool.WaitForTaskCompletion(task); }
            else GroupCanvas(backend, threaded, () => solver);
            using var detached = new NotificationPolicyProbe(); Check(detached.GetCanvasLayerNode() is null, "Detached membership is empty");
            using var packed = new PackedScene(); detached.Capturing = () => Check(detached.GetCanvasLayerNode() is null, "Logical canvas membership remains capture-readable"); packed.Pack(detached); detached.Capturing = null;
            detached.Dispose(); Reject<ObjectDisposedException>(() => detached.GetCanvasLayerNode());
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
    }
    private static void GroupCanvas(PhysicsServer.Backend? backend, bool threaded, Func<Solver?> implementation)
    {
        using var world = backend is { } selected ? new World(selected) : new World("GroupCanvas");
        using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var atlas = new TileSetAtlasSource { Texture = texture }; using var set = new TileSet(); set.AddPhysicsLayer(); set.SetPhysicsLayerCollisionLayer(0, 4); set.AddSource(atlas, 3);
        atlas.CreateTile(default); var data = atlas.GetTileData(default, 0); data.AddCollisionPolygon(0); data.SetCollisionPolygonPoints(0, 0, [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]);
        var a = new CanvasGroup(root, circle, set, default); var b = new CanvasGroup(root, circle, set, new(500, 0));
        var collector = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread, ProcessThreadGroupOrder = -1 }; root.AddChild(collector);
        using var tree = new SceneTree(root); a.Tiles.UpdateInternals(); b.Tiles.UpdateInternals();
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[4]; using var together = new Barrier(2);
        var cold = true; var observing = true; var id = a.Bodies[0].InstanceID; var rid = a.Bodies[0].GetRID(); var owner = Environment.CurrentManagedThreadId;
        var solver = implementation(); var identities = new List<ulong>();
        try
        {
            foreach (var group in new[] { a, b }) for (var i = 0; i < 2; i++)
                { query.Position = group.Origin + new Vector2(8 + 32 * i, 8); query.CollisionMask = 4; query.CanvasInstanceID = group.Layer.InstanceID; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1, "Both generated quadrants exist"); group.TileRIDs[i] = hits[0].ColliderRID; }
            if (solver is not null) solver.IdentityHook = target => { if (target == rid) { Check(Environment.CurrentManagedThreadId == owner, "Registered identity changes publish on the scene owner"); identities.Add(PhysicsServer.BodyGetCanvasInstanceID(target)); } };
            a.Writer.Write = () => Write(a, b); b.Writer.Write = () => Write(b, a);
            collector.Write = () => { if (observing) { Verify(a); Verify(b); } };
            tree.PhysicsFrame(.01); Check(a.Writer.ThreadID != b.Writer.ThreadID, "Independent canvas groups actually overlap"); cold = false;
            if (solver is not null) Check(identities.TakeLast(3).SequenceEqual(new ulong[] { 0, a.Layer.InstanceID, 0 }), "Exit/enter/exit identity calls retain per-object order");
            tree.ProcessFrame(.01);
            if (solver is not null)
            {
                var failure = new ApplicationException("Expected canvas identity failure"); var once = true;
                solver.IdentityHook = target => { if (target == rid && once) { once = false; throw failure; } };
                var tick = PhysicsServer.SpaceGetTick(world.Space); var error = CaptureCopy(() => tree.PhysicsFrame(.01));
                Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(failure) && PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "Queued identity failure continues later publications and releases the selected numerical step");
                solver.IdentityHook = null; Verify(a); Verify(b);
            }
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01); Collect(); for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            var start = PhysicsServer.SpaceGetTick(world.Space); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == start + 128, $"Canvas/participation complete physics/query intervals allocated {own}/{all} B");
            Check(a.Bodies[0].InstanceID == id && a.Bodies[0].GetRID() == rid && world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null, "Canvas publication preserves identity and explicit backend selection");
            observing = false;
            using var done = new ManualResetEventSlim(); var task = WorkerThreadPool.AddTask(() => { try { Reject<InvalidOperationException>(() => a.Probe.GetCanvasLayerNode()); Guard(a); } finally { done.Set(); } }, highPriority: true);
            Check(done.Wait(TimeSpan.FromSeconds(5)), "An ordinary worker completes before owner assistance"); WorkerThreadPool.WaitForTaskCompletion(task);
            Console.WriteLine($"Group canvas {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: logical membership, immediate notifications, ordered body/area/tile canvas filters, disable policies and 128 complete physics/query intervals at {own}/{all} B passed.");
        }
        finally { if (solver is not null) solver.IdentityHook = null; }
        void Write(CanvasGroup own, CanvasGroup other)
        {
            if (cold && !together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Canvas groups did not overlap");
            Check(ReferenceEquals(own.Probe.GetCanvasLayerNode(), own.Layer) && ReferenceEquals(other.Probe.GetCanvasLayerNode(), other.Layer), "Own and foreign actual canvas membership is readable in an active group");
            if (cold) Guard(other);
            own.Phase++; own.Bodies[0].Position = own.Origin + new Vector2(100 + (own.Phase & 1), 100);
            own.Probe.Observe = true; var before = own.Probe.Notices;
            own.Writer.PropagateNotification(CanvasItem.NotificationExitCanvas); own.Writer.PropagateNotification(Node.NotificationDisabled); own.Writer.PropagateNotification(Node.NotificationEnabled); own.Writer.PropagateNotification(CanvasItem.NotificationEnterCanvas);
            if ((own.Phase & 1) != 0) own.Writer.PropagateNotification(CanvasItem.NotificationExitCanvas);
            Check(own.Probe.Notices == before + 4 + (own.Phase & 1) && own.Probe.LastThread == own.Writer.ThreadID, "User notification delivery remains immediate without owner replay"); own.Probe.Observe = false;
        }
        void Guard(CanvasGroup group)
        {
            foreach (var node in new Node[] { group.Bodies[0], group.Areas[0], group.Tiles })
                foreach (var notice in new[] { CanvasItem.NotificationExitCanvas, CanvasItem.NotificationEnterCanvas })
                    Reject<InvalidOperationException>(() => node.Notify(notice));
            foreach (var node in new CollisionObject[] { group.Bodies[0], group.Areas[0] })
                foreach (var notice in new[] { Node.NotificationDisabled, Node.NotificationEnabled })
                    Reject<InvalidOperationException>(() => node.Notify(notice));
        }
        void Verify(CanvasGroup group)
        {
            var canvas = (group.Phase & 1) == 0 ? group.Layer.InstanceID : 0;
            for (var i = 0; i < group.Bodies.Length; i++)
            {
                var body = group.Bodies[i]; var active = i != 5;
                Check(PhysicsServer.BodyGetCanvasInstanceID(body.GetRID()) == canvas && PhysicsServer.BodyGetSpace(body.GetRID()) == (active ? world.Space : default), "Body canvas identity and effective participation agree after join");
                Point(body, body.GetRID(), body.GlobalPosition, false, active && i != 4, 1, canvas, group.Layer.InstanceID);
            }
            Check(PhysicsServer.BodyGetMode(group.Bodies[6].GetRID()) == PhysicsServer.BodyMode.Static && PhysicsServer.BodyGetMode(group.Bodies[7].GetRID()) == PhysicsServer.BodyMode.Rigid,
                "Manual Enabled/Disabled preserves effective MakeStatic and KeepActive policies");
            foreach (var area in group.Areas) { Check(PhysicsServer.AreaGetCanvasInstanceID(area.GetRID()) == canvas && PhysicsServer.AreaGetSpace(area.GetRID()) == world.Space, "Area remains an attached sensor in MakeStatic"); Point(area, area.GetRID(), area.GlobalPosition, true, true, 2, canvas, group.Layer.InstanceID); }
            for (var i = 0; i < 2; i++) { Check(PhysicsServer.BodyGetCanvasInstanceID(group.TileRIDs[i]) == canvas, "Every original generated body receives its own queued identity"); Point(group.Tiles, group.TileRIDs[i], group.Origin + new Vector2(8 + 32 * i, 8), false, true, 4, canvas, group.Layer.InstanceID); }
        }
        void Point(Node node, RID target, Vector2 point, bool area, bool active, uint mask, ulong canvas, ulong layer)
        {
            query.Position = point; query.CollisionMask = mask; query.CollideWithAreas = area; query.CollideWithBodies = !area; query.CanvasInstanceID = canvas;
            var count = world.DirectSpaceState.IntersectPoint(query, hits);
            if (count != (active ? 1 : 0) || active && (hits[0].ColliderRID != target || hits[0].ColliderObject != node)) throw new InvalidOperationException($"Matching canvas query: {node.ClassName} at {point}, canvas {canvas}, active {active}, count {count}, target {target}");
            query.CanvasInstanceID = canvas == 0 ? layer : 0; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 0, "Selected backend query applies the published canvas identity");
        }
    }
}
