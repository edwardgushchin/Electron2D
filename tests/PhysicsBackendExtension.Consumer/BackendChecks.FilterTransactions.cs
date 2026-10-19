using Electron2D;

public static partial class BackendChecks
{
    public static void RunFilterTransactions(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("FilterTransactions", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("FilterTransactions");
            using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 3 }; using var identity = new Node();
            CollisionObject?[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(),
                new RigidBody { GravityScale = 0, CanSleep = false, MaxContactsReported = 4,
                    LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace }, new PhysicalBone(), new Area(), null, null];
            var rids = new RID[8]; var observers = new RID[8]; var peers = new StaticBody[8];
            var events = new int[8]; var areaEvents = new int[8];
            for (var i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] is { } node)
                {
                    node.Position = Origin(i); node.CollisionMask = 4;
                    node.AddChild(new CollisionShape { Shape = circle, Position = new(-10, 0) });
                    node.AddChild(new CollisionShape { Shape = circle, Disabled = true });
                    node.AddChild(new CollisionShape { Shape = circle, Position = new(10, 0) }); root.AddChild(node);
                }
                peers[i] = new StaticBody { Position = Origin(i) + new Vector2(IsArea(i) ? 10 : 30, 0), CollisionLayer = 4, CollisionMask = 0 };
                peers[i].AddChild(new CollisionShape { Shape = circle }); root.AddChild(peers[i]);
            }
            var followerObserver = new Area { Position = Origin(4) + new Vector2(10, 0), CollisionLayer = 0 };
            followerObserver.AddChild(new CollisionShape { Shape = circle }); root.AddChild(followerObserver);
            var followerEntries = 0; followerObserver.BodyEntered += _ => followerEntries++;
            var moving = new RigidBody
            {
                Position = new(0, 500),
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                LinearVelocity = new(6, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Shape = circle }); root.AddChild(moving);
            using var tree = new SceneTree(root);
            const ulong canvas = 0xFEDCBA9876543210;
            using var point = new PhysicsPointQueryParameters { CanvasInstanceID = canvas, CollisionMask = 1, CollideWithAreas = true };
            using var motion = new PhysicsTestMotionParameters { Motion = new(40, 0), Margin = 0 };
            using var result = new PhysicsTestMotionResult(); var hits = new PhysicsPointResult[4];
            var faults = 0; var shapeRebuilds = 0; var callbacks = 0;
            try
            {
                for (var i = 0; i < rids.Length; i++)
                {
                    rids[i] = nodes[i]?.GetRID() ?? (IsArea(i) ? PhysicsServer.AreaCreate() : PhysicsServer.BodyCreate());
                    if (nodes[i] is null)
                    {
                        if (IsArea(i))
                        {
                            PhysicsServer.AreaSetTransform(rids[i], new(0, Origin(i))); PhysicsServer.AreaSetCollisionMask(rids[i], 4);
                            PhysicsServer.AreaAddShape(rids[i], circle.GetRID(), new(0, new Vector2(-10, 0)));
                            PhysicsServer.AreaAddShape(rids[i], circle.GetRID(), disabled: true);
                            PhysicsServer.AreaAddShape(rids[i], circle.GetRID(), new(0, new Vector2(10, 0)));
                            PhysicsServer.AreaSetSpace(rids[i], world.Space); PhysicsServer.AreaSetMonitorable(rids[i], true); PhysicsServer.AreaAttachObject(rids[i], identity);
                        }
                        else
                        {
                            PhysicsServer.BodySetMode(rids[i], PhysicsServer.BodyMode.Static); PhysicsServer.BodySetTransform(rids[i], new(0, Origin(i)));
                            PhysicsServer.BodySetCollisionMask(rids[i], 4);
                            PhysicsServer.BodyAddShape(rids[i], circle.GetRID(), new(0, new Vector2(-10, 0)));
                            PhysicsServer.BodyAddShape(rids[i], circle.GetRID(), disabled: true);
                            PhysicsServer.BodyAddShape(rids[i], circle.GetRID(), new(0, new Vector2(10, 0)));
                            PhysicsServer.BodySetSpace(rids[i], world.Space); PhysicsServer.BodyAttachObject(rids[i], identity);
                        }
                    }
                    if (IsArea(i)) PhysicsServer.AreaAttachCanvasInstanceID(rids[i], canvas); else PhysicsServer.BodyAttachCanvasInstanceID(rids[i], canvas);
                    var index = i;
                    observers[i] = PhysicsServer.AreaCreate(); PhysicsServer.AreaSetCollisionLayer(observers[i], 0);
                    PhysicsServer.AreaSetTransform(observers[i], new(0, Origin(i) + new Vector2(10, 0)));
                    PhysicsServer.AreaAddShape(observers[i], circle.GetRID()); PhysicsServer.AreaSetSpace(observers[i], world.Space);
                    Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int> observe = (_, rid, _, remote, local) =>
                    {
                        Check(rid == rids[index] && remote == 2 && local == 0, "Filter transitions retain the target RID and disabled logical index gap"); events[index]++;
                    };
                    PhysicsServer.AreaSetMonitorCallback(observers[i], observe); PhysicsServer.AreaSetAreaMonitorCallback(observers[i], observe);
                    if (IsArea(i)) PhysicsServer.AreaSetMonitorCallback(rids[i], (_, rid, _, remote, local) =>
                    {
                        Check(rid == peers[index].GetRID() && remote == 0 && local == 2, "Receiver masks retain exact numerical Area membership"); areaEvents[index]++;
                    });
                }
                Step();
                Check(world.PhysicsBackendName == (backend?.ToString() ?? "FilterTransactions"), "The real selected implementation owns filter updates");
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                if (solver is not null)
                {
                    solver.ShapeHook = _ => shapeRebuilds++;
                    for (var i = 0; i < rids.Length; i++)
                        for (var field = 0; field < 2; field++)
                            for (var entry = 0; entry < (nodes[i] is null ? 1 : 3); entry++)
                                for (var path = 0; path < 2; path++)
                                    for (var failures = 1; failures <= 2; failures++)
                                    {
                                        checkpoint.Capture();
                                        var id = rids[i]; var attempts = 0;
                                        var apply = new ApplicationException("filter-apply"); var retry = new ApplicationException("filter-retry");
                                        solver.FilterHook = candidate =>
                                        {
                                            if (candidate != id || ++attempts > failures) return;
                                            Authored(i, 1, 4);
                                            solver.Bodies[id].Layer = solver.Bodies[id].Mask = 0;
                                            if (attempts == 1) throw apply; throw retry;
                                        };
                                        var tick = PhysicsServer.SpaceGetTick(world.Space); var before = events.Sum() + areaEvents.Sum();
                                        var errors = ReleaseErrors(() => Edit(i, field, entry, 0));
                                        Check(errors.Length == 1 && ReferenceEquals(errors[0], apply) && attempts == 1, "Failed filter assignment preserves its original error without hidden replay");
                                        Authored(i, 1, 4);
                                        if (failures == 2)
                                        {
                                            errors = ReleaseErrors(() => Recover(i, path != 0));
                                            Check(errors.Length == 1 && ReferenceEquals(errors[0], retry), "A pending numerical filter retries and propagates before query publication");
                                            Authored(i, 1, 4);
                                        }
                                        Recover(i, path != 0); Query(i, 1, 4); solver.FilterHook = null;
                                        Check(attempts == failures + 1 && PhysicsServer.SpaceGetTick(world.Space) == tick + (path == 0 ? 0UL : 1UL), "Only a successfully prepared physical interval advances the solver tick");
                                        Check(solver.Bodies[id].Layer == (Suppressed(i) ? 0u : 1u) && solver.Bodies[id].Mask == (Suppressed(i) ? 0u : 4u), "Repair uses the current effective filter, including inactive physical bones");
                                        checkpoint.Restore(); Step();
                                        Check(events.Sum() + areaEvents.Sum() == before && shapeRebuilds == 0, "Failed filters preserve sensor history and repair without recompiling geometry");
                                        faults++;
                                    }
                }
                var ownerThread = Environment.CurrentManagedThreadId; var filterCalls = 0; var numericalCalls = 0;
                if (solver is not null) solver.FilterPolicyHook = (rid, layer, mask, wake) =>
                {
                    var i = Array.IndexOf(rids, rid); if (i < 0) return;
                    Check(Environment.CurrentManagedThreadId == ownerThread && wake == !IsArea(i), "Filter publication keeps owner threading and Body/Area wake policy");
                    Check((layer == 0 || layer == 1) && (mask == 0 || mask == 4), "The selected filter receives complete effective layer/mask bits"); filterCalls++;
                };
                var edits = 0;
                for (var i = 0; i < rids.Length; i++)
                    for (var field = 0; field < 2; field++)
                        for (var entry = 0; entry < (nodes[i] is null ? 1 : 3); entry++)
                        {
                            var observed = events[i]; var received = areaEvents[i];
                            Edit(i, field, entry, 0); Authored(i, field == 0 ? 0u : 1u, field == 1 ? 0u : 4u);
                            Query(i, field == 0 ? 0u : 1u, field == 1 ? 0u : 4u); Step();
                            Check(events[i] - observed == (field == 0 && !Suppressed(i) ? 1 : 0) && areaEvents[i] - received == (field == 1 && IsArea(i) ? 1 : 0), $"Committed filter departure: role={i}, field={field}, entry={entry}, observed={events[i] - observed}, received={areaEvents[i] - received}");
                            Edit(i, field, entry, field == 0 ? 1u : 4u); Query(i, 1, 4); Step();
                            Check(events[i] - observed == (field == 0 && !Suppressed(i) ? 2 : 0) && areaEvents[i] - received == (field == 1 && IsArea(i) ? 2 : 0), "Restored authored filters publish one entry without duplicate effects"); edits++;
                        }
                Check(shapeRebuilds == 0, "Successful filters also retain geometry compilation identity");
                Check(solver is null || filterCalls == edits * 2, "Every ordinary filter assignment reaches the selected update exactly once");
                var observedEvents = events.Sum() + areaEvents.Sum();
                checkpoint.Capture(); Step(); checkpoint.Restore(); Step();
                Check(events.Sum() + areaEvents.Sum() == observedEvents, "Private replay after live filter edits preserves observer history without duplicate effects");
                if (solver is not null) solver.ShapeHook = null;
                foreach (var responds in new[] { false, true })
                {
                    PhysicsServer.BodySetCollisionLayer(rids[3], 0); PhysicsServer.BodySetCollisionMask(rids[3], responds ? 4u : 0u);
                    PhysicsServer.BodySetTransform(rids[3], new(0, Origin(3) + new Vector2(15, 0))); PhysicsServer.BodySetLinearVelocity(rids[3], new(6, 0)); Step();
                    var state = PhysicsServer.BodyGetDirectState(rids[3])!;
                    Check(responds ? MathF.Abs(state.LinearVelocity.X) < .01f && state.GetContactCount() == 1 &&
                        state.GetContactLocalShape(0) == 2 && state.GetContactImpulse(0).Length() > 5
                        : MathF.Abs(state.LinearVelocity.X - 6) < .001f && state.GetContactCount() == 0 && MathF.Abs(state.Transform.Origin.X - Origin(3).X - 15.1f) < .001f,
                        "A zero-layer body responds only through its own mask, with a real impulse and preserved logical contact index");
                    PhysicsServer.BodySetCollisionLayer(rids[3], 1); PhysicsServer.BodySetCollisionMask(rids[3], 4);
                    PhysicsServer.BodySetTransform(rids[3], new(0, Origin(3))); PhysicsServer.BodySetLinearVelocity(rids[3], Vector2.Zero);
                    PhysicsServer.BodySetAngularVelocity(rids[3], 0); Step();
                }
                if (solver is not null) solver.StepHook = () =>
                {
                    Check((Environment.CurrentManagedThreadId != ownerThread) == threaded, "Filter input reads use the captured numerical lane");
                    for (var i = 0; i < rids.Length; i++) Authored(i, 1, 4); numericalCalls++;
                };
                var movingRID = moving.GetRID(); PhysicsServer.BodySetForceIntegrationCallback(movingRID, _ => callbacks++);
                for (var i = 0; i < 128; i++) Frame();
                var callbackStart = callbacks; var x = moving.Position.X; var filterStart = filterCalls; var numericalStart = numericalCalls;
                Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0 && callbacks == callbackStart + 128 && moving.GetRID() == movingRID && Math.Abs(moving.Position.X - x - 12.8f) < .01f &&
                    (solver is null || filterCalls - filterStart == 2048 && numericalCalls - numericalStart == 128),
                    $"Filter transactions: {owner}/{all} B, force callbacks={callbacks - callbackStart}, motion={moving.Position.X - x}");
                Console.WriteLine($"Filter transactions {backend?.ToString() ?? "registered"}, threaded={threaded}: eight roles, {edits} ordinary edits, {faults} failed assignments; no shape rebuilds; 128 full frames {owner}/{all} B.");

                void Edit(int i, int field, int entry, uint value)
                {
                    var node = nodes[i];
                    if (node is not null && entry == 0) { if (field == 0) node.CollisionLayer = value; else node.CollisionMask = value; }
                    else if (node is not null && entry == 2) { if (field == 0) node.SetCollisionLayerValue(1, value != 0); else node.SetCollisionMaskValue(3, value != 0); }
                    else if (IsArea(i)) { if (field == 0) PhysicsServer.AreaSetCollisionLayer(rids[i], value); else PhysicsServer.AreaSetCollisionMask(rids[i], value); }
                    else { if (field == 0) PhysicsServer.BodySetCollisionLayer(rids[i], value); else PhysicsServer.BodySetCollisionMask(rids[i], value); }
                }
                void Authored(int i, uint layer, uint mask)
                {
                    Check((IsArea(i) ? PhysicsServer.AreaGetCollisionLayer(rids[i]) : PhysicsServer.BodyGetCollisionLayer(rids[i])) == layer &&
                        (IsArea(i) ? PhysicsServer.AreaGetCollisionMask(rids[i]) : PhysicsServer.BodyGetCollisionMask(rids[i])) == mask &&
                        (nodes[i] is not { } node || node.CollisionLayer == layer && node.CollisionMask == mask), "Scene/server authoring retains exactly the committed layer and mask");
                    Check((IsArea(i) ? PhysicsServer.AreaGetCanvasInstanceID(rids[i]) : PhysicsServer.BodyGetCanvasInstanceID(rids[i])) == canvas &&
                        (IsArea(i) ? PhysicsServer.AreaGetObjectInstanceID(rids[i]) : PhysicsServer.BodyGetObjectInstanceID(rids[i])) == (nodes[i]?.InstanceID ?? identity.InstanceID), "Filter failure retains full canvas and assigned object identity");
                }
                void Query(int i, uint layer, uint mask)
                {
                    point.Position = Origin(i) + new Vector2(10, 0); var count = world.DirectSpaceState.IntersectPoint(point, hits);
                    Check(count == (layer != 0 && !Suppressed(i) ? 1 : 0) && (count == 0 || hits[0].ColliderRID == rids[i] && hits[0].ShapeIndex == 2), "Point queries observe repaired numerical layers and logical slot identity");
                    if (IsArea(i)) return;
                    motion.From = new(0, Origin(i)); var found = PhysicsServer.BodyTestMotion(rids[i], motion, result);
                    Check(found == (mask != 0 && !Suppressed(i)) && (!found || result.GetColliderRID() == peers[i].GetRID() && result.GetCollisionLocalShape() == 2), "Body motion observes the repaired effective numerical mask");
                }
                void Frame()
                {
                    for (var i = 0; i < rids.Length; i++) { Edit(i, 0, 1, 1); Edit(i, 1, 1, 4); Query(i, 1, 4); }
                    Step();
                }
                void Step()
                {
                    tree.PhysicsFrame(1d / 60);
                    Check(!followerObserver.HasOverlappingBodies() && followerEntries == 0 && events[4] == 0,
                        "Scene and raw Area receivers exclude inactive physical bone fixtures from snapshots and events");
                }
                void Recover(int i, bool step) { if (step) Step(); else Query(i, 1, 4); }
            }
            finally
            {
                if (solver is not null) { solver.FilterHook = null; solver.FilterPolicyHook = null; solver.ShapeHook = null; solver.StepHook = null; }
                for (var i = 0; i < rids.Length; i++)
                    if (rids[i] != default && IsArea(i)) PhysicsServer.AreaSetMonitorCallback(rids[i], null);
                foreach (var observer in observers) if (observer != default) PhysicsServer.FreeRID(observer);
                for (var i = 6; i < rids.Length; i++) if (rids[i] != default) PhysicsServer.FreeRID(rids[i]);
            }
            bool Suppressed(int i) => nodes[i] is PhysicalBone bone && !bone.IsSimulatingPhysics();
            static bool IsArea(int i) => i is 5 or 7;
            static Vector2 Origin(int i) => new(i * 100, 0);
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }
}
